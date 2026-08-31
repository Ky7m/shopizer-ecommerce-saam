"""Egress: export ACTIONABLE graph state to a per-service workspace file (orchestrator-run).

This is the outbound half of graph mediation. The orchestrator (which has Neo4j) queries the
enriched graph and materializes a scoped, committed markdown file that a SANDBOXED generation/fix
agent (ATX batch, fix-logic — no Neo4j access) reads as input via its TD reference list.

Delivery is git: the file MUST be committed to the branch, because that is the only channel into
a sandboxed container. It is regenerated every dispatch (never hand-edited — the orchestrator
overwrites it before pushing the branch the agent clones).

CONTENT DISCIPLINE — actionable only. The file contains ONLY state that changes the agent's next
action and that the agent cannot get from the spec files or TEST_RESULTS.json:
  - dead code (annotated but unreachable) -> "wire it, don't re-implement"
  - stubs (reachable but behavioral assertions fail) -> "implement the effect"
  - deviation history (attempts + regressions) -> "don't retry what already failed/regressed"
  - cross-service call shapes (reconciled) -> "call provider with exactly this shape"
  - db-tier placement -> "this logic lives in the DB; call the object, don't re-implement in app code"
  - extension points -> "this rule's value is configurable; call the resolver, don't hardcode it"
  - priority (Core/Critical + low confidence first)
It does NOT duplicate rule statements, logic, or test output — those live in the spec + TEST_RESULTS.

Usage (from workspace root):
  uv run --project graph-mcp python graph-mcp/scripts/graph_context_export.py --service gl-service
  uv run --project graph-mcp python graph-mcp/scripts/graph_context_export.py --all

Exit codes:
  0 = export written (or nothing actionable — writes a minimal file saying so)
  1 = error (Neo4j unavailable, no sourcecode)
"""

import argparse
import json
import os
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


def connect():
    try:
        d = GraphDatabase.driver(NEO4J_URI, auth=(NEO4J_USER, NEO4J_PASSWORD))
        d.verify_connectivity()
        return d
    except Exception as e:
        print(f"ERROR: Cannot connect to Neo4j at {NEO4J_URI}: {e}", file=sys.stderr)
        sys.exit(1)


def _resolve_service_id(session, service_name: str):
    r = session.run("""
        MATCH (s:Service)
        WHERE s.name = $n OR s.serviceId = $n
           OR toLower(replace(s.name,' ','-')) = toLower($n)
        RETURN s.serviceId AS id, s.name AS name LIMIT 1
    """, n=service_name).single()
    return (r["id"], r["name"]) if r else (None, None)


def build_export(session, service_name: str) -> str:
    service_id, display_name = _resolve_service_id(session, service_name)
    # Predicate reused across queries to match the owning service (by id or name).
    # Every query binds the service node FIRST (WITH s LIMIT 1) to avoid fan-out/leaks.
    svc_pred = "(s.serviceId = $sid OR s.name = $sname)"
    params = {"sid": service_id, "sname": display_name or service_name}

    # 1. Dead code — annotated but unreachable
    dead = session.run(f"""
        MATCH (s:Service) WHERE {svc_pred}
        WITH s LIMIT 1
        MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(s)
        WHERE br.deadCode = true
        RETURN br.brId AS brId, br.statement AS stmt
        ORDER BY br.brId
    """, **params).data()

    # 2. Stubs — reachable but behavioral assertions fail
    stubs = session.run(f"""
        MATCH (s:Service) WHERE {svc_pred}
        WITH s LIMIT 1
        MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(s)
        WHERE br.behavioralStatus IN ['stub','partial']
        RETURN br.brId AS brId, br.behavioralStatus AS status
        ORDER BY br.behavioralStatus, br.brId
    """, **params).data()

    # 3. Deviation history — repeat/regressed items (loop-stop)
    devs = session.run(f"""
        MATCH (s:Service) WHERE {svc_pred}
        WITH s LIMIT 1
        MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(s)
        MATCH (d:Deviation {{brId: br.brId, status: 'OPEN'}})
        WHERE coalesce(d.occurrences,1) > 1 OR coalesce(d.regressedCount,0) > 0
        RETURN d.brId AS brId, coalesce(d.occurrences,1) AS occ,
               coalesce(d.regressedCount,0) AS regressed, d.attemptLog AS attemptLog,
               d.lastReason AS lastReason
        ORDER BY regressed DESC, occ DESC
    """, **params).data()

    # 4. Cross-service calls this service makes — with reconciled shapes
    calls = session.run(f"""
        MATCH (s:Service) WHERE {svc_pred}
        WITH s LIMIT 1
        MATCH (s)-[c:CALLS]->(p:Service)
        WHERE c.requestShape IS NOT NULL OR c.verified IS NOT NULL
        RETURN p.name AS provider, c.endpoints AS endpoints, c.requestShape AS req,
               c.responseShape AS resp, c.verified AS verified
    """, **params).data()

    # 5a. Extension points — rules whose behavior is configurable (do NOT hardcode the value)
    ext_points = session.run(f"""
        MATCH (s:Service) WHERE {svc_pred}
        WITH s LIMIT 1
        MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(s)
        MATCH (br)-[:EXTENDS_VIA]->(ep:ExtensionPoint)
        WHERE coalesce(ep.decision, 'Reproduce') <> 'Drop'
        RETURN br.brId AS brId, ep.extPointId AS ext, ep.mechanism AS mechanism, ep.decision AS decision
        ORDER BY br.brId
    """, **params).data()

    # 5b. DB-tier logic — rules whose logic lives in the database (do NOT reimplement in app code)
    db_tier = session.run(f"""
        MATCH (s:Service) WHERE {svc_pred}
        WITH s LIMIT 1
        MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(s)
        MATCH (br)-[:IMPLEMENTS_IN_DB]->(o:DbObject)
        RETURN br.brId AS brId, o.name AS name, o.kind AS kind, o.binding AS binding
        ORDER BY o.migrationOrder, br.brId
    """, **params).data()

    # 5. Priority — Core/Critical + low-confidence, not yet passing.
    # Bind the service FIRST (single node), then its assigned rules — avoids OR-precedence leaks.
    priority = session.run(f"""
        MATCH (s:Service) WHERE {svc_pred}
        WITH s LIMIT 1
        MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(s)
        WHERE coalesce(br.lifecycleState,'') <> 'Passing'
        OPTIONAL MATCH (dec:Decision)-[:DECIDED_AS]->(br)
        WITH br, dec
        WHERE dec.classification = 'Core' OR dec.weight = 'Critical'
        RETURN br.brId AS brId, dec.weight AS weight, br.effectiveConfidence AS conf
        ORDER BY coalesce(br.effectiveConfidence, 0) ASC LIMIT 15
    """, **params).data()

    ts = datetime.now(timezone.utc).isoformat()
    out = []
    out.append(f"# Graph Context: {display_name or service_name}")
    out.append(f"<!-- GENERATED by graph_context_export.py at {ts} — READ-ONLY, orchestrator-provided.")
    out.append("     Do NOT hand-edit (regenerated every dispatch). NOT a naming authority —")
    out.append("     04-api-contract.yaml + 08-dtos/ remain authoritative. This file is ACTIONABLE")
    out.append("     graph state: what to fix and what NOT to retry. -->")
    out.append("")

    actionable = False

    if dead:
        actionable = True
        out.append("## Dead Code — annotated but UNREACHABLE (fix = WIRE to a route, do NOT re-implement)")
        for d in dead:
            out.append(f"- **{d['brId']}** — annotation present but no endpoint reaches it. Wire it.")
        out.append("")

    if stubs:
        actionable = True
        out.append("## Stubs — reachable but behavioral assertions FAIL (fix = implement the EFFECT per 07-workflows recipe)")
        for s in stubs:
            out.append(f"- **{s['brId']}** [{s['status']}] — returns shape but effect missing (state/amount/event). Implement the recipe.")
        out.append("")

    if devs:
        actionable = True
        out.append("## Deviation History — DO NOT repeat failed approaches (loop-stop)")
        for d in devs:
            attempts = ""
            try:
                log = json.loads(d["attemptLog"]) if d.get("attemptLog") else []
                if log:
                    reasons = "; ".join(a.get("reason", "")[:60] for a in log[-3:])
                    attempts = f" | recent: {reasons}"
            except (ValueError, TypeError):
                pass
            regressed = f", REGRESSED {d['regressed']}x (fix worked then broke — root cause is ELSEWHERE)" if d["regressed"] else ""
            out.append(f"- **{d['brId']}** — seen {d['occ']}x{regressed}{attempts}")
        out.append("")

    if calls:
        actionable = True
        out.append("## Cross-Service Calls — use these EXACT reconciled shapes")
        for c in calls:
            eps = ", ".join(c.get("endpoints") or [])
            v = "verified" if c.get("verified") else "UNVERIFIED (GAP — confirm before relying)"
            out.append(f"- -> **{c['provider']}** {eps} [{v}]")
            if c.get("req"):
                out.append(f"    request: {c['req']}")
            if c.get("resp"):
                out.append(f"    response: {c['resp']}")
        out.append("")

    if ext_points:
        actionable = True
        out.append("## Configurable Rules — behavior comes from the extensibility engine (do NOT hardcode the value; call the resolver)")
        for e in ext_points:
            mech = f" [{e['mechanism']}]" if e.get("mechanism") else ""
            dec = f" ({e['decision']})" if e.get("decision") else ""
            out.append(f"- **{e['brId']}** -> {e['ext']}{mech}{dec} — read the configurable value via the engine resolver, not a constant")
        out.append("")

    if db_tier:
        actionable = True
        out.append("## DB-Tier Logic — lives in the DATABASE (fix = call the DB object via its binding, do NOT re-implement in app code)")
        for d in db_tier:
            binding = f" | binding: {d['binding']}" if d.get("binding") else ""
            out.append(f"- **{d['brId']}** -> {d['kind']} `{d['name']}` — the app method is the CALLER only{binding}")
        out.append("  Reimplementing this in application code rebuilds the bottleneck the placement decision rejected AND double-implements the rule.")
        out.append("")

    if priority:
        actionable = True
        out.append("## Priority — Core/Critical, not yet passing, lowest confidence first")
        for p in priority:
            conf = f"{p['conf']:.2f}" if p.get("conf") is not None else "n/a"
            w = f" [{p['weight']}]" if p.get("weight") else ""
            out.append(f"- **{p['brId']}**{w} conf={conf}")
        out.append("")

    if not actionable:
        out.append("## No actionable graph state")
        out.append("No dead code, stubs, repeated deviations, or unverified cross-service calls for this service.")
        out.append("Proceed from the spec (04-api-contract.yaml + 01-business-rules.md + 07-workflows.md).")
        out.append("")

    return "\n".join(out)


def export_service(driver, svc_dir: Path) -> bool:
    with driver.session() as session:
        content = build_export(session, svc_dir.name)
    target = svc_dir / "_graph-context.md"
    target.write_text(content, encoding="utf-8")
    # Line count as a cheap "how much actionable" signal
    actionable_lines = sum(1 for ln in content.splitlines() if ln.startswith("- "))
    print(f"  {svc_dir.name}: wrote _graph-context.md ({actionable_lines} actionable items)")
    return True


def main():
    parser = argparse.ArgumentParser(description="Export actionable graph state to per-service files")
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
            for svc in sorted(p for p in src_root.iterdir() if p.is_dir()):
                export_service(driver, svc)
            print("\n[Graph Egress] Exported _graph-context.md for all services. Commit + push before dispatch.")
        elif args.service:
            svc_dir = src_root / args.service
            if not svc_dir.is_dir():
                print(f"ERROR: {svc_dir} not found", file=sys.stderr)
                sys.exit(1)
            export_service(driver, svc_dir)
        else:
            parser.error("Specify --service <name> or --all")
    finally:
        driver.close()


if __name__ == "__main__":
    main()
