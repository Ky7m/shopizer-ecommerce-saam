"""Implementation Fidelity Audit — reachability + dead-code detection (orchestrator-run).

Detects the "annotated skeleton" defect that every structural gate misses:
a BR-ID annotation on a method that NO route reaches (dead code = false implementation claim).

This is a HEURISTIC reachability check, deliberately stack-agnostic. It does not build a full
call graph (that would be per-language). Instead it uses the strongest cheap signal available
across stacks: is the BR-ID-annotated symbol referenced from a route/endpoint-registration surface,
or transitively from a file that is?

Sets on the graph (orchestrator has Neo4j; sandboxed agents do not):
  - Implementation.reachable = true|false
  - BusinessRule.deadCode = true   (when annotated but unreachable)

DB-TIER EXEMPTION (Layer C): a BR implemented in the database tier (IMPLEMENTS_IN_DB -> DbObject,
or Implementation.tier = db-*) has no reachable app method BY DESIGN — a trigger fires on DML, a
function is called from a thin repository binding. These are reachable-by-design and are NEVER
flagged as dead code; otherwise the audit would tell a fix agent to reimplement db-tier logic in
app code, rebuilding the bottleneck the placement decision rejected.

Usage (from workspace root):
  uv run --project graph-mcp python graph-mcp/scripts/fidelity_audit.py --all
  uv run --project graph-mcp python graph-mcp/scripts/fidelity_audit.py --service gl-service

Exit codes:
  0 = audit ran (results written to graph)
  1 = error (Neo4j unavailable, no sourcecode)

NOTE: This is a REACHABILITY heuristic. Behavioral status (stub vs real) is set separately by
reconcile_validation.py from test results — the two together form the fidelity picture.

HEURISTIC LIMITATION: the route-surface signals (below) cover HTTP frameworks. Non-HTTP entry
surfaces (MCP tool servers, message-queue consumers, scheduled batch jobs) may not be recognized,
producing false "dead code" flags for services whose entry point isn't an HTTP route. For such
services, extend ROUTE_REGISTRATION_TOKENS / ROUTE_SURFACE_HINTS with that stack's registration
signals (e.g. MCP tool registration, @Consumer, cron/schedule decorators). A dead-code flag is a
PROMPT TO VERIFY, not a hard failure — the orchestrator/human confirms before acting.
"""

import argparse
import os
import re
import sys
from datetime import datetime, timezone
from pathlib import Path

from neo4j import GraphDatabase


def _load_env():
    env_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env")
    if os.path.exists(env_path):
        with open(env_path) as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith("#") and "=" in line:
                    key, value = line.split("=", 1)
                    os.environ[key.strip()] = value.strip()


_load_env()
NEO4J_URI = os.environ.get("NEO4J_URI") or f"bolt://localhost:{os.environ.get('NEO4J_BOLT_PORT', '7687')}"
NEO4J_USER = os.environ.get("NEO4J_USER", "neo4j")
NEO4J_PASSWORD = os.environ.get("NEO4J_PASSWORD", "saamgraph")

WORKSPACE_ROOT = Path(__file__).resolve().parent.parent.parent

# Accept both flat IDs (BR-CAT-001) and grouped IDs (BR-CAT-NN-005).
BR_ID_PATTERN = re.compile(r"BR-[A-Z]{2,4}(?:-[A-Z]{2,5})?-[0-9]{2,3}")
SOURCE_EXTENSIONS = {
    ".java", ".kt", ".ts", ".js", ".py", ".cs", ".go", ".rs", ".rb", ".php", ".scala", ".groovy",
}

# Filename / path signals that a file registers routes/endpoints (stack-agnostic set).
# A BR-ID annotated in — or referenced from — one of these is considered reachable.
ROUTE_SURFACE_HINTS = (
    "endpoint", "endpoints", "controller", "controllers", "route", "routes", "router",
    "program.cs", "startup", "main", "app", "handler", "handlers", "api", "resource", "resources",
    "minimalapi", "mapendpoints", "urls", "views",  # framework-varied
)

# In-code signals that a symbol is wired to a route (method-registration calls).
ROUTE_REGISTRATION_TOKENS = (
    "MapGet", "MapPost", "MapPut", "MapDelete", "MapPatch", "MapGroup",   # ASP.NET minimal
    "[HttpGet", "[HttpPost", "[HttpPut", "[HttpDelete", "[HttpPatch",     # ASP.NET controllers
    "@GetMapping", "@PostMapping", "@RequestMapping", "@PutMapping",       # Spring
    "@app.route", "@router.", "APIRouter", "add_url_rule",                # Flask/FastAPI/Django
    "router.get", "router.post", "app.get", "app.post", "@Get(", "@Post(",# Express/Nest
)


def connect():
    try:
        d = GraphDatabase.driver(NEO4J_URI, auth=(NEO4J_USER, NEO4J_PASSWORD))
        d.verify_connectivity()
        return d
    except Exception as e:
        print(f"ERROR: Cannot connect to Neo4j at {NEO4J_URI}: {e}", file=sys.stderr)
        sys.exit(1)


def _is_route_surface_file(path: Path) -> bool:
    name = path.name.lower()
    parts = [p.lower() for p in path.parts]
    if any(h in name for h in ROUTE_SURFACE_HINTS):
        return True
    if any(h in parts for h in ("controllers", "endpoints", "routes", "api", "features", "resources")):
        return True
    return False


def audit_service(service_dir: Path) -> dict:
    """Return {br_id: reachable_bool} for all BR-IDs annotated in the service.

    A BR-ID is reachable if it is annotated in (or the same file as) code that is wired to a route,
    OR the file is a route-surface file, OR a route-surface file references the annotated symbol's file.
    """
    # 1. Collect BR-ID -> set(files) where annotated
    br_files: dict[str, set[Path]] = {}
    route_surface_files: set[Path] = set()
    all_text_by_file: dict[Path, str] = {}

    for fp in service_dir.rglob("*"):
        if not (fp.is_file() and fp.suffix in SOURCE_EXTENSIONS):
            continue
        try:
            text = fp.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue
        all_text_by_file[fp] = text
        br_ids = set(BR_ID_PATTERN.findall(text))
        for b in br_ids:
            br_files.setdefault(b, set()).add(fp)
        # Is this a route surface? (by filename/path OR by containing route-registration tokens)
        if _is_route_surface_file(fp) or any(tok in text for tok in ROUTE_REGISTRATION_TOKENS):
            route_surface_files.add(fp)

    # 2. Build the set of file stems and declared type names referenced from route-surface files
    # (cheap transitive signal: a service file whose class/module name is mentioned in a route
    # surface is considered wired). Type-name matching handles files such as CatalogServices.cs
    # declaring the injected CatalogService class.
    referenced_stems: set[str] = set()
    referenced_types: set[str] = set()
    declared_types_by_file: dict[Path, set[str]] = {}
    for fp, text in all_text_by_file.items():
        declared_types_by_file[fp] = set(
            re.findall(r"\b(?:class|interface|record|struct|enum)\s+([A-Za-z_]\w*)", text)
        )
    for rf in route_surface_files:
        txt = all_text_by_file.get(rf, "")
        for other in all_text_by_file:
            if other.stem and other.stem in txt:
                referenced_stems.add(other.stem)
        referenced_types.update(
            re.findall(r"\b[A-Za-z_]\w*\b", txt)
        )

    # 3. Decide reachability per BR-ID
    result: dict[str, bool] = {}
    for br, files in br_files.items():
        reachable = False
        for f in files:
            if f in route_surface_files:
                reachable = True
                break
            # annotated file is referenced from a route surface (wired via DI/registration)
            if f.stem in referenced_stems:
                reachable = True
                break
            if declared_types_by_file.get(f, set()) & referenced_types:
                reachable = True
                break
            # the annotated file itself contains a route-registration token
            if any(tok in all_text_by_file.get(f, "") for tok in ROUTE_REGISTRATION_TOKENS):
                reachable = True
                break
        result[br] = reachable
    return result


def write_results(driver, service: str, reach: dict) -> dict:
    ts = datetime.now(timezone.utc).isoformat()
    reachable_ct = unreachable_ct = 0
    db_tier_ct = 0
    with driver.session() as session:
        for br_id, is_reachable in reach.items():
            # Only act on BR-IDs that exist in the graph
            exists = session.run(
                "MATCH (br:BusinessRule {brId: $b}) RETURN br.brId AS id", {"b": br_id}
            ).single()
            if not exists:
                continue

            # DB-TIER EXEMPTION (Layer C): a BR whose logic is placed in the database tier has, BY
            # DESIGN, no reachable app method — a trigger fires on DML with "no app call"; a function
            # is invoked from a thin repository binding. The route-surface heuristic cannot see this,
            # so without the exemption we would flag every db-tier BR as dead code and tell a fix agent
            # to reimplement it in app code — the exact bottleneck the placement decision rejected.
            # A BR is db-tier if it has an IMPLEMENTS_IN_DB edge to a DbObject, OR its claimed
            # Implementation.tier is a db-* value.
            db_tier = session.run("""
                MATCH (br:BusinessRule {brId: $b})
                OPTIONAL MATCH (br)-[:IMPLEMENTS_IN_DB]->(o:DbObject)
                OPTIONAL MATCH (br)-[:CLAIMS_IMPLEMENTATION]->(i:Implementation)
                    WHERE i.tier IN ['db-view','db-function','db-proc','db-trigger']
                RETURN (o IS NOT NULL OR i IS NOT NULL) AS isDbTier
            """, b=br_id).single()["isDbTier"]

            if db_tier:
                # Reachable-by-design: never dead code. Mark reachable, record the reason.
                session.run("""
                    MATCH (br:BusinessRule {brId: $b})
                    SET br.deadCode = false, br.behavioralCheckedAt = coalesce(br.behavioralCheckedAt, $ts)
                    WITH br
                    OPTIONAL MATCH (br)-[:CLAIMS_IMPLEMENTATION]->(impl:Implementation)
                    SET impl.reachable = true, impl.reachabilityCheckedAt = $ts,
                        impl.reachabilityNote = 'db-tier: reachable by design (called via binding / fires on DML)'
                """, b=br_id, ts=ts)
                db_tier_ct += 1
                reachable_ct += 1
                continue

            # Update the Implementation node(s) claimed by this BR + the BR deadCode flag
            session.run("""
                MATCH (br:BusinessRule {brId: $b})
                SET br.deadCode = $dead, br.behavioralCheckedAt = coalesce(br.behavioralCheckedAt, $ts)
                WITH br
                OPTIONAL MATCH (br)-[:CLAIMS_IMPLEMENTATION]->(impl:Implementation)
                SET impl.reachable = $reachable, impl.reachabilityCheckedAt = $ts
            """, b=br_id, dead=(not is_reachable), reachable=is_reachable, ts=ts)
            if is_reachable:
                reachable_ct += 1
            else:
                unreachable_ct += 1
    return {"service": service, "reachable": reachable_ct, "dead_code": unreachable_ct, "db_tier": db_tier_ct}


def run_service(driver, svc_dir: Path):
    reach = audit_service(svc_dir)
    res = write_results(driver, svc_dir.name, reach)
    dead = res["dead_code"]
    flag = f"  ⚠ {dead} DEAD-CODE BR-IDs" if dead else "  clean"
    db_note = f" ({res['db_tier']} db-tier exempt)" if res.get("db_tier") else ""
    print(f"  {svc_dir.name}: {res['reachable']} reachable{db_note},{flag}")
    return res


def main():
    parser = argparse.ArgumentParser(description="Implementation fidelity audit (reachability)")
    parser.add_argument("--service", help="Single service under sourcecode/")
    parser.add_argument("--all", action="store_true", help="All services under sourcecode/")
    args = parser.parse_args()

    src_root = WORKSPACE_ROOT / "sourcecode"
    if not src_root.is_dir():
        print(f"ERROR: sourcecode/ not found under {WORKSPACE_ROOT}", file=sys.stderr)
        sys.exit(1)

    driver = connect()
    try:
        if args.all:
            total_dead = 0
            for svc in sorted(p for p in src_root.iterdir() if p.is_dir()):
                res = run_service(driver, svc)
                total_dead += res["dead_code"]
            print(f"\n[Fidelity Audit] Complete. {total_dead} dead-code BR-IDs flagged across all services.")
        elif args.service:
            svc_dir = src_root / args.service
            if not svc_dir.is_dir():
                print(f"ERROR: {svc_dir} not found", file=sys.stderr)
                sys.exit(1)
            run_service(driver, svc_dir)
        else:
            parser.error("Specify --service <name> or --all")
    finally:
        driver.close()


if __name__ == "__main__":
    main()
