"""SAAM Spec Drift Detection — Detects when code and spec diverge.

Computes per-BR-ID content hashes from spec files, compares against hashes
stored on CLAIMS_IMPLEMENTATION / VALIDATED_BY edges in the graph, and reports
drift (code was implemented against a different version of the spec).

Usage:
    # Check a single service
    python3 graph-mcp/scripts/spec_drift.py --service order-service

    # Check all services
    python3 graph-mcp/scripts/spec_drift.py --all

    # Update hashes in graph (after spec edits are intentional)
    python3 graph-mcp/scripts/spec_drift.py --service order-service --update

    # Output as YAML (for CI integration)
    python3 graph-mcp/scripts/spec_drift.py --service order-service --format yaml

Exit codes:
    0 = no drift detected (or --update mode)
    1 = drift detected (some BR-IDs have stale implementations)
    2 = error (Neo4j unavailable, spec files missing)
"""

import argparse
import hashlib
import json
import os
import re
import sys
from pathlib import Path

import yaml

# Neo4j connection
NEO4J_URI = os.environ.get("NEO4J_URI", "bolt://localhost:7687")
NEO4J_USER = os.environ.get("NEO4J_USER", "neo4j")
NEO4J_PASSWORD = os.environ.get("NEO4J_PASSWORD", "saamgraph")

# BR-ID heading pattern in spec files
BR_HEADING_PATTERN = re.compile(r"^###\s+(BR-[A-Z]{2}-[A-Z]{2,4}-\d{2,3})\s*[:\-]", re.MULTILINE)

# Implicit-system layer id patterns (Layer A/B/C) — for hashing the new 02-domain-model sections.
INV_ID_PATTERN = re.compile(r"INV-[A-Z]{2,4}-\d{3}")


def _norm(text: str) -> str:
    """Normalize a spec fragment for stable hashing (strip trailing ws, collapse blank lines)."""
    normalized = "\n".join(line.rstrip() for line in text.splitlines())
    return re.sub(r"\n{3,}", "\n\n", normalized).strip()


def _hash(text: str) -> str:
    return hashlib.sha256(_norm(text).encode("utf-8")).hexdigest()[:16]


def _section_body(content: str, heading_re: str) -> str | None:
    """Return the markdown body under a heading, up to the next same-or-higher-level heading.
    Deeper subheadings are part of the body. None if absent. (Mirrors import_specs._section_body.)"""
    hm = re.search(rf"(#{{1,6}})\s*{heading_re}\b", content, re.IGNORECASE)
    if not hm:
        return None
    level = len(hm.group(1))
    start = hm.end()
    end_m = re.search(rf"\n#{{1,{level}}}\s", content[start:])
    return content[start:start + end_m.start()] if end_m else content[start:]


def compute_implicit_hashes(spec_dir: Path) -> dict[str, str]:
    """Compute content hashes for the implicit-system layer items in 02-domain-model.md.

    Returns a dict keyed by a stable node id:
      - "state:<entity>"       -> hash of that entity's '#### <entity> lifecycle' block (Layer A)
      - "<INV-id>"             -> hash of that invariant's row (Layer A)
      - "dbobj:<name>"         -> hash of that db-object's row (Layer C)
    Absent sections contribute nothing. Used to detect silent edits to state machines /
    invariants / db-objects that the BR-only hash would miss.
    """
    ddl_file = spec_dir / "02-domain-model.md"
    if not ddl_file.exists():
        return {}
    content = ddl_file.read_text(encoding="utf-8")
    hashes: dict[str, str] = {}

    # Layer A — Entity State Model: hash each '#### <entity> lifecycle' block separately
    esm = _section_body(content, r"Entity State Model")
    if esm:
        for m in re.finditer(r"\n#{4}\s+([A-Za-z0-9_]+)\s+lifecycle(.*?)(?=\n#{1,4}\s|\Z)",
                             "\n" + esm, re.DOTALL):
            entity = m.group(1)
            hashes[f"state:{entity}"] = _hash(m.group(0))

    # Layer A — Data Invariants: hash each invariant row (line containing an INV- id)
    inv = _section_body(content, r"Data Invariants")
    if inv:
        for line in inv.splitlines():
            if not line.strip().startswith("|"):
                continue
            idm = INV_ID_PATTERN.search(line)
            if idm:
                hashes[idm.group(0)] = _hash(line)

    # Layer C — Database Logic Objects: hash each object row (keyed by object name in col 1)
    dbo = _section_body(content, r"Database Logic Objects")
    if dbo:
        for line in dbo.splitlines():
            if not line.strip().startswith("|"):
                continue
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if len(cells) < 7:
                continue
            name, kind = cells[0], cells[1].lower()
            if name.lower() in ("name", "") or set(name) <= {"-", ":", " "}:
                continue
            if kind in ("view", "function", "procedure", "trigger"):
                hashes[f"dbobj:{name}"] = _hash(line)

    return hashes


def _load_env():
    """Load .env from graph-mcp/ if available."""
    env_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env")
    if os.path.exists(env_path):
        with open(env_path) as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith("#") and "=" in line:
                    key, value = line.split("=", 1)
                    # .env is AUTHORITATIVE — override stale shell values (not setdefault)
                    os.environ[key.strip()] = value.strip()


_load_env()
NEO4J_URI = os.environ.get("NEO4J_URI") or f"bolt://localhost:{os.environ.get('NEO4J_BOLT_PORT', '7687')}"


def compute_spec_hashes(spec_dir: Path) -> dict[str, str]:
    """Compute content hashes for each BR-ID section in 01-business-rules.md.

    Returns: {br_id: sha256_hash}
    """
    rules_file = spec_dir / "01-business-rules.md"
    if not rules_file.exists():
        return {}

    content = rules_file.read_text(encoding="utf-8")

    # Split by BR-ID headings (### BR-XX-YYY-NNN: ...)
    # Each section runs from one heading to the next (or end of file)
    matches = list(BR_HEADING_PATTERN.finditer(content))
    if not matches:
        return {}

    hashes = {}
    for i, match in enumerate(matches):
        br_id = match.group(1)
        start = match.start()
        end = matches[i + 1].start() if i + 1 < len(matches) else len(content)

        section = content[start:end]
        # Normalize: strip trailing whitespace per line, collapse multiple blank lines
        normalized = "\n".join(line.rstrip() for line in section.splitlines())
        normalized = re.sub(r"\n{3,}", "\n\n", normalized).strip()

        section_hash = hashlib.sha256(normalized.encode("utf-8")).hexdigest()[:16]
        hashes[br_id] = section_hash

    return hashes


def find_service_spec_dir(service_name: str) -> Path | None:
    """Find the spec directory for a service."""
    # Try common locations
    candidates = [
        Path(f"spec/microservices/{service_name}"),
        Path(f"spec/microservices/{service_name.replace('-', '_')}"),
    ]
    for candidate in candidates:
        if candidate.exists() and (candidate / "01-business-rules.md").exists():
            return candidate
    return None


def find_all_services() -> list[str]:
    """Find all services with spec directories."""
    spec_root = Path("spec/microservices")
    if not spec_root.exists():
        return []
    return [
        d.name for d in spec_root.iterdir()
        if d.is_dir() and (d / "01-business-rules.md").exists()
    ]


def get_graph_hashes(service_name: str) -> dict[str, dict]:
    """Get stored spec hashes from graph for implemented BR-IDs.

    Returns: {br_id: {specHash, lifecycleState, classification}}
    """
    try:
        from neo4j import GraphDatabase
        driver = GraphDatabase.driver(NEO4J_URI, auth=(NEO4J_USER, NEO4J_PASSWORD))
        driver.verify_connectivity()
    except Exception:
        return {}

    results = {}
    with driver.session() as session:
        records = session.run("""
            MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(s:Service)
            WHERE toLower(replace(s.name, ' ', '-')) = toLower($service)
               OR s.serviceId = $service
            OPTIONAL MATCH (br)-[ci:CLAIMS_IMPLEMENTATION]->()
            OPTIONAL MATCH (d:Decision)-[:DECIDED_AS]->(br)
            RETURN br.brId AS brId,
                   br.lifecycleState AS state,
                   br.specHash AS specHash,
                   ci.specHash AS edgeSpecHash,
                   d.classification AS classification
        """, {"service": service_name}).data()

        for record in records:
            br_id = record["brId"]
            # Prefer edge-level hash, fall back to node-level
            stored_hash = record.get("edgeSpecHash") or record.get("specHash")
            results[br_id] = {
                "specHash": stored_hash,
                "lifecycleState": record.get("state"),
                "classification": record.get("classification"),
            }

    driver.close()
    return results


def update_graph_hashes(service_name: str, spec_hashes: dict[str, str]) -> int:
    """Update spec hashes in graph for all BR-IDs of a service.

    Returns: number of nodes updated.
    """
    try:
        from neo4j import GraphDatabase
        driver = GraphDatabase.driver(NEO4J_URI, auth=(NEO4J_USER, NEO4J_PASSWORD))
        driver.verify_connectivity()
    except Exception:
        print("ERROR: Neo4j unavailable — cannot update hashes", file=sys.stderr)
        return 0

    updated = 0
    with driver.session() as session:
        for br_id, spec_hash in spec_hashes.items():
            # Update on BusinessRule node
            result = session.run("""
                MATCH (br:BusinessRule {brId: $brId})
                SET br.specHash = $hash, br._specHashUpdatedAt = datetime()
                RETURN br.brId AS brId
            """, {"brId": br_id, "hash": spec_hash})
            if result.single():
                updated += 1

            # Update on CLAIMS_IMPLEMENTATION edge (if exists)
            session.run("""
                MATCH (br:BusinessRule {brId: $brId})-[ci:CLAIMS_IMPLEMENTATION]->()
                SET ci.specHash = $hash
            """, {"brId": br_id, "hash": spec_hash})

            # Update on VALIDATED_BY edge (if exists)
            session.run("""
                MATCH (br:BusinessRule {brId: $brId})-[v:VALIDATED_BY]->()
                SET v.specHash = $hash
            """, {"brId": br_id, "hash": spec_hash})

    driver.close()
    return updated


def _connect_or_none():
    try:
        from neo4j import GraphDatabase
        driver = GraphDatabase.driver(NEO4J_URI, auth=(NEO4J_USER, NEO4J_PASSWORD))
        driver.verify_connectivity()
        return driver
    except Exception:
        return None


# Maps an implicit-hash key to its graph node label + id-field + id-value.
def _implicit_key_to_node(key: str, service_name: str):
    """Return (label, id_field, id_value) for an implicit-hash key, or None if unmappable."""
    if key.startswith("state:"):
        # Per-entity state machine — stamp the hash on all of that entity's EntityState nodes.
        return ("EntityState", "entity", key[len("state:"):])
    if key.startswith("dbobj:"):
        return ("DbObject", "name", key[len("dbobj:"):])
    if key.startswith("INV-"):
        return ("Invariant", "invariantId", key)
    return None


def update_implicit_hashes(service_name: str, implicit_hashes: dict[str, str]) -> int:
    """Stamp _specHash on EntityState / Invariant / DbObject nodes (Layer A/C drift baseline)."""
    driver = _connect_or_none()
    if not driver:
        print("ERROR: Neo4j unavailable — cannot update implicit hashes", file=sys.stderr)
        return 0
    updated = 0
    with driver.session() as session:
        for key, h in implicit_hashes.items():
            m = _implicit_key_to_node(key, service_name)
            if not m:
                continue
            label, id_field, id_value = m
            # EntityState is per-entity: set on all states of that entity in this service.
            if label == "EntityState":
                r = session.run(
                    "MATCH (n:EntityState {entity: $v, service: $svc}) "
                    "SET n._specHash = $h, n._specHashUpdatedAt = datetime() RETURN count(n) AS c",
                    {"v": id_value, "svc": service_name, "h": h},
                ).single()
                updated += 1 if (r and r["c"]) else 0
            else:
                r = session.run(
                    f"MATCH (n:{label} {{{id_field}: $v}}) "
                    "SET n._specHash = $h, n._specHashUpdatedAt = datetime() RETURN n",
                    {"v": id_value, "h": h},
                ).single()
                updated += 1 if r else 0
    driver.close()
    return updated


def detect_implicit_drift(service_name: str) -> list[dict]:
    """Detect drift in the implicit-system layer items (Layer A/C) for a service."""
    spec_dir = find_service_spec_dir(service_name)
    if not spec_dir:
        return []
    current = compute_implicit_hashes(spec_dir)
    if not current:
        return []
    driver = _connect_or_none()
    if not driver:
        return []

    drifts = []
    with driver.session() as session:
        for key, current_hash in current.items():
            m = _implicit_key_to_node(key, service_name)
            if not m:
                continue
            label, id_field, id_value = m
            if label == "EntityState":
                rec = session.run(
                    "MATCH (n:EntityState {entity: $v, service: $svc}) "
                    "RETURN n._specHash AS h LIMIT 1",
                    {"v": id_value, "svc": service_name},
                ).single()
            else:
                rec = session.run(
                    f"MATCH (n:{label} {{{id_field}: $v}}) RETURN n._specHash AS h",
                    {"v": id_value},
                ).single()
            if not rec:
                continue  # node not in graph yet
            stored = rec["h"]
            if not stored:
                continue  # never stamped — first time, can't compare
            if stored != current_hash:
                drifts.append({
                    "item": key,
                    "service": service_name,
                    "stored_hash": stored,
                    "current_hash": current_hash,
                    "layer": "A" if (key.startswith("state:") or key.startswith("INV-")) else "C",
                    "drift_type": "implicit_section_changed_after_baseline",
                })
    driver.close()
    return drifts


def detect_drift(service_name: str) -> list[dict]:
    """Detect spec drift for a service.

    Returns list of drifted BR-IDs with context.
    """
    spec_dir = find_service_spec_dir(service_name)
    if not spec_dir:
        print(f"WARNING: No spec directory found for {service_name}", file=sys.stderr)
        return []

    # Compute current spec hashes
    current_hashes = compute_spec_hashes(spec_dir)
    if not current_hashes:
        return []

    # Get stored hashes from graph
    graph_data = get_graph_hashes(service_name)
    if not graph_data:
        # No graph data — can't detect drift (might be before first implementation)
        return []

    drifts = []
    for br_id, current_hash in current_hashes.items():
        graph_info = graph_data.get(br_id)
        if not graph_info:
            continue  # BR-ID not in graph yet (not implemented)

        stored_hash = graph_info.get("specHash")
        if not stored_hash:
            continue  # Never stamped — can't compare (first time)

        if stored_hash != current_hash:
            drifts.append({
                "br_id": br_id,
                "service": service_name,
                "stored_hash": stored_hash,
                "current_hash": current_hash,
                "lifecycle_state": graph_info.get("lifecycleState"),
                "classification": graph_info.get("classification"),
                "drift_type": "spec_changed_after_implementation",
            })

    return drifts


def determine_governance_level(drift: dict) -> str:
    """Determine governance response based on drift + BR classification."""
    classification = drift.get("classification", "Active")
    lifecycle = drift.get("lifecycle_state", "Declared")

    if classification == "Core" or classification == "Critical":
        return "tier3_human_review"
    elif lifecycle == "Passing":
        # Was validated — now spec changed — need re-validation
        return "tier2_revalidation"
    else:
        return "tier2_reconcile"


def format_report(service_name: str, drifts: list[dict], format_type: str = "text") -> str:
    """Format drift report."""
    if format_type == "yaml":
        report = {
            "service": service_name,
            "drift_count": len(drifts),
            "drifts": drifts,
        }
        return yaml.dump(report, default_flow_style=False)

    # Text format
    lines = [f"=== Spec Drift Report: {service_name} ===", ""]
    if not drifts:
        lines.append("No drift detected. Spec and implementation are aligned.")
        return "\n".join(lines)

    lines.append(f"DRIFT DETECTED: {len(drifts)} BR-ID(s) have stale implementations")
    lines.append("")

    for drift in drifts:
        governance = determine_governance_level(drift)
        lines.append(f"  {drift['br_id']} [{drift.get('classification', '?')}/{drift.get('lifecycle_state', '?')}]")
        lines.append(f"    Spec hash: {drift['stored_hash']} → {drift['current_hash']}")
        lines.append(f"    Governance: {governance}")
        if governance == "tier3_human_review":
            lines.append(f"    ACTION: Critical rule changed — requires human review before code update")
        elif governance == "tier2_revalidation":
            lines.append(f"    ACTION: Previously-validated rule changed — re-run tests after code update")
        else:
            lines.append(f"    ACTION: Update implementation to match new spec, then validate")
        lines.append("")

    return "\n".join(lines)


def format_implicit_report(service_name: str, drifts: list[dict], format_type: str = "text") -> str:
    """Format the implicit-system layer drift report (Layer A/C)."""
    if format_type == "yaml":
        return yaml.dump({"service": service_name, "implicit_drift_count": len(drifts),
                          "implicit_drifts": drifts}, default_flow_style=False)
    lines = [f"=== Implicit-Layer Drift (A/C): {service_name} ===", ""]
    if not drifts:
        return ""  # only shown when there is drift
    lines.append(f"IMPLICIT-LAYER DRIFT: {len(drifts)} item(s) changed since baseline")
    for d in drifts:
        lines.append(f"  {d['item']} [Layer {d['layer']}]  {d['stored_hash']} -> {d['current_hash']}")
        if d["item"].startswith("state:"):
            lines.append("    ACTION: state machine changed — re-verify closure + re-run illegal-transition tests")
        elif d["item"].startswith("INV-"):
            lines.append("    ACTION: invariant changed — re-check tier + re-run invariant-holds tests (regenerate DB object if db/both)")
        else:
            lines.append("    ACTION: db-object changed — regenerate ordered migration + re-run DB-tier tests")
    lines.append("")
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description="SAAM Spec Drift Detection")
    parser.add_argument("--service", help="Service name to check")
    parser.add_argument("--all", action="store_true", help="Check all services")
    parser.add_argument("--update", action="store_true", help="Update graph hashes (use after intentional spec changes)")
    parser.add_argument("--format", choices=["text", "yaml"], default="text", help="Output format")
    args = parser.parse_args()

    if not args.service and not args.all:
        parser.print_help()
        sys.exit(2)

    services = find_all_services() if args.all else [args.service]

    if args.update:
        # Update mode: stamp current spec hashes into graph
        for service in services:
            spec_dir = find_service_spec_dir(service)
            if not spec_dir:
                print(f"SKIP: No spec directory for {service}")
                continue
            hashes = compute_spec_hashes(spec_dir)
            updated = update_graph_hashes(service, hashes)
            implicit = compute_implicit_hashes(spec_dir)
            imp_updated = update_implicit_hashes(service, implicit) if implicit else 0
            imp_note = f", {imp_updated} implicit-layer items (state/invariant/db-object)" if implicit else ""
            print(f"Updated {updated} BR-ID hashes for {service}{imp_note}")
        sys.exit(0)

    # Detection mode
    all_drifts = []
    for service in services:
        drifts = detect_drift(service)
        implicit_drifts = detect_implicit_drift(service)
        if drifts:
            all_drifts.extend(drifts)
        if implicit_drifts:
            all_drifts.extend(implicit_drifts)
        report = format_report(service, drifts, args.format)
        print(report)
        if implicit_drifts:
            print(format_implicit_report(service, implicit_drifts, args.format))

    if all_drifts:
        sys.exit(1)  # Drift detected
    else:
        sys.exit(0)  # Clean


if __name__ == "__main__":
    main()
