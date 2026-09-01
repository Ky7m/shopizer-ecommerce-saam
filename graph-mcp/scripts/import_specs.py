"""Deterministic graph population from spec files on disk.

Parses service spec markdown (01-business-rules.md, 02-domain-model.md,
04-api-contract.yaml) and bulk-imports to Neo4j. This replaces unreliable
agent MCP calls (graph_add_node) that get skipped under context pressure.

The Tracker subagent runs this script instead of making 300+ individual MCP calls.
The Validator subagent checks the result (graph count == spec count).

Usage:
  # Single service:
  uv run --directory graph-mcp python scripts/import_specs.py --service ms-01-gl-service

  # All services:
  uv run --directory graph-mcp python scripts/import_specs.py --all

  # Check only (verify graph matches specs, don't write):
  uv run --directory graph-mcp python scripts/import_specs.py --service ms-01-gl-service --check

Exit codes:
  0 = success (all imported or check passed)
  1 = error (parse failure, Neo4j unavailable)
  2 = check failed (graph count != spec count)
"""

import argparse
import os
import re
import sys
import yaml
from pathlib import Path
from neo4j import GraphDatabase


def _load_env():
    """Load .env from graph-mcp/ directory."""
    env_path = Path(__file__).parent.parent / ".env"
    if env_path.exists():
        for line in env_path.read_text().splitlines():
            line = line.strip()
            if line and not line.startswith("#") and "=" in line:
                key, value = line.split("=", 1)
                # .env is AUTHORITATIVE — override stale shell values (not setdefault)
                os.environ[key.strip()] = value.strip()


_load_env()
NEO4J_URI = os.environ.get("NEO4J_URI") or f"bolt://localhost:{os.environ.get('NEO4J_BOLT_PORT', '7687')}"
NEO4J_USER = os.environ.get("NEO4J_USER", "neo4j")
NEO4J_PASSWORD = os.environ.get("NEO4J_PASSWORD", "saamgraph")

def _br_id_regex() -> str:
    """Read the BR-ID pattern from the single source of truth (saam-calibration.yaml → br_id_pattern).
    NEVER hardcode a divergent pattern. Falls back to the widened union pattern (group segment optional,
    admits both BR-AP-001 and BR-GL-PST-001) only if calibration is unreadable."""
    fallback = r"BR-[A-Z]{2,6}(?:-[A-Z]{2,6})?-[0-9]{2,3}"
    ws_root = Path(__file__).resolve().parent.parent.parent
    try:
        for candidate in (
            ws_root / "core/steering/saam-calibration.yaml",
            ws_root / ".kiro/steering/saam-calibration.yaml",
            ws_root / "dist/kiro-ide/.kiro/steering/saam-calibration.yaml",
            Path("core/steering/saam-calibration.yaml"),
            Path(".kiro/steering/saam-calibration.yaml"),
            Path("dist/kiro-ide/.kiro/steering/saam-calibration.yaml"),
        ):
            if candidate.exists():
                text = candidate.read_text(encoding="utf-8", errors="ignore")
                block = text.split("br_id_pattern:", 1)
                if len(block) == 2:
                    body = block[1]
                    m = re.search(r'regex_tolerant:\s*"([^"]+)"', body) or re.search(r'regex:\s*"([^"]+)"', body)
                    if m:
                        return m.group(1)
                break
    except Exception:
        pass
    return fallback


# BR-ID pattern — sourced from saam-calibration.yaml (single source of truth), NOT hardcoded.
BR_ID_PATTERN = re.compile(_br_id_regex())


def _source_ref_stem(source_ref):
    """Extract a matchable component stem from a BR Source Reference.
    'CustomerService.cs:Validate:45-60' -> 'CustomerService'; 'dbo.bspApplyPayment:...' -> 'bspApplyPayment'.
    Returns None for greenfield/N/A (no legacy source to link)."""
    if not source_ref:
        return None
    ref = str(source_ref).strip()
    if not ref or ref.lower() in ("n/a", "none", "greenfield"):
        return None
    first = ref.split(":", 1)[0].strip().strip("`")
    first = Path(first).stem            # drop file extension
    if "." in first:                    # drop schema prefix (dbo.bspFoo -> bspFoo)
        first = first.split(".")[-1]
    return first or None


def connect():
    """Connect to Neo4j."""
    try:
        driver = GraphDatabase.driver(NEO4J_URI, auth=(NEO4J_USER, NEO4J_PASSWORD))
        driver.verify_connectivity()
        return driver
    except Exception as e:
        print(f"ERROR: Cannot connect to Neo4j at {NEO4J_URI}: {e}", file=sys.stderr)
        sys.exit(1)


def find_workspace_root():
    """Find the workspace root (parent of graph-mcp/)."""
    return Path(__file__).parent.parent.parent


def find_services(workspace: Path) -> list[str]:
    """Find all service directories under spec/microservices/."""
    spec_dir = workspace / "spec" / "microservices"
    if not spec_dir.exists():
        return []
    return sorted([d.name for d in spec_dir.iterdir() if d.is_dir()])


def parse_business_rules(service_dir: Path) -> list[dict]:
    """Parse BR-IDs from 01-business-rules.md."""
    rules_file = service_dir / "01-business-rules.md"
    if not rules_file.exists():
        return []

    content = rules_file.read_text()
    rules = []

    # Split by BR-ID headings (### BR-XX-YYY-NNN: ...)
    sections = re.split(rf'^###\s+({_br_id_regex()}):', content, flags=re.MULTILINE)

    # sections[0] is preamble, then pairs of (brId, content)
    for i in range(1, len(sections) - 1, 2):
        br_id = sections[i].strip()
        section_content = sections[i + 1] if i + 1 < len(sections) else ""

        rule = {"brId": br_id}

        # Extract Statement
        stmt_match = re.search(r'\*\*Statement[:\s]*\*\*\s*(.+?)(?:\n\*\*|\n###|\Z)', section_content, re.DOTALL)
        if stmt_match:
            rule["statement"] = stmt_match.group(1).strip()[:500]

        # Extract Intent/Type
        intent_match = re.search(r'\*\*(?:Intent|Type)[:\s]*\*\*\s*(\w+)', section_content)
        if intent_match:
            rule["intent"] = intent_match.group(1).strip()

        # Extract Confidence
        conf_match = re.search(r'\*\*Confidence[:\s]*\*\*\s*(\w+)', section_content)
        if conf_match:
            conf_str = conf_match.group(1).strip().lower()
            rule["confidence"] = {"high": 0.9, "medium": 0.7, "low": 0.5}.get(conf_str, 0.7)

        # Extract Source Reference
        src_match = re.search(r'\*\*Source Reference[:\s]*\*\*\s*(.+)', section_content)
        if src_match:
            rule["sourceRef"] = src_match.group(1).strip()[:200]

        # Extract Discovery Method
        disc_match = re.search(r'\*\*Discovery Method[:\s]*\*\*\s*(.+)', section_content)
        if disc_match:
            rule["discoveryMethod"] = disc_match.group(1).strip()[:50]

        # Extract Extension Point annotation(s) (Layer B). The annotation line may be bold
        # (**Extension Point:**) or plain ("- Extension Point:"), and may name >1 id (comma-separated).
        # Grab the annotation line, then pull all EXT-ids from it.
        ext_line = re.search(r'Extension Point[:\s*]*\**\s*(.+)', section_content)
        ext_ids = re.findall(r'EXT-[A-Z]{2,4}-\d{3}', ext_line.group(1)) if ext_line else []
        if ext_ids:
            rule["extensionPoints"] = sorted(set(ext_ids))

        # Extract preservation vectors (if Semantic Preservation table exists)
        vector_match = re.search(
            r'\|\s*Control-flow\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|',
            section_content
        )
        if vector_match:
            rule["srcControlFlow"] = int(vector_match.group(1))
            rule["specControlFlow"] = int(vector_match.group(2))

        # Try all 8 dimensions
        for dim, dim_name in [
            ("DataFlow", "data.?flow"), ("Constants", "constants"),
            ("StateTransitions", "state.?transitions?"), ("Outcomes", "outcomes"),
            ("DataWrites", "data.?writes?"), ("Integrations", "integrations"),
            ("ErrorPaths", "error.?paths?")
        ]:
            dim_match = re.search(
                rf'\|\s*{dim_name}\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|',
                section_content, re.IGNORECASE
            )
            if dim_match:
                rule[f"src{dim}"] = int(dim_match.group(1))
                rule[f"spec{dim}"] = int(dim_match.group(2))

        rules.append(rule)

    return rules


def parse_tables(service_dir: Path) -> list[dict]:
    """Parse table names from 02-domain-model.md (CREATE TABLE statements)."""
    ddl_file = service_dir / "02-domain-model.md"
    if not ddl_file.exists():
        return []

    content = ddl_file.read_text()
    tables = []

    # Find CREATE TABLE statements. PostgreSQL specs commonly use schema-qualified
    # names; retain the qualified name so ownership remains unambiguous.
    for match in re.finditer(
        r'CREATE TABLE\s+(?:IF NOT EXISTS\s+)?["\']?'
        r'([A-Za-z_][\w]*(?:\.[A-Za-z_][\w]*)?)["\']?\s*\(',
        content,
        re.IGNORECASE,
    ):
        table_name = match.group(1)
        tables.append({"name": table_name})

    return tables


def parse_db_objects(service_dir: Path, service_name: str) -> list[dict]:
    """Parse DB-tier logic objects from the '### Database Logic Objects' section of
    02-domain-model.md (Layer C).

    Expected: a markdown table with a FIXED column order (parsed positionally, like
    spec/shared/cross-service-contracts.md):

      | Name | Kind | Implements | Enforces Invariant | Migration Order | Binding | Placement |
      | compute_batch_total | function | BR-GL-PST-003 |  | 10 | repo GlRepository.computeTotal -> SELECT compute_batch_total(:id) | P4b:PLACE-004 |
      | v_open_orders | view |  |  | 20 | read model | P4b:PLACE-007 |
      | trg_enforce_balanced | trigger |  | INV-GL-001 | 30 | trigger — no app call | mandatory-db-integrity |

    Only rows under a heading matching 'Database Logic Objects' are parsed. Absent section
    or empty table → [] (services with no db-tier logic, which is the default/common case).
    """
    ddl_file = service_dir / "02-domain-model.md"
    if not ddl_file.exists():
        return []

    content = ddl_file.read_text()

    # Isolate the section body: from the heading to the next heading of the same-or-higher level.
    sec = re.search(
        r'#{2,4}\s*Database Logic Objects\b(.*?)(?:\n#{1,4}\s|\Z)',
        content, re.DOTALL | re.IGNORECASE,
    )
    if not sec:
        return []
    body = sec.group(1)

    valid_kinds = {"view", "function", "procedure", "trigger"}
    objects = []
    for line in body.splitlines():
        if not line.strip().startswith("|"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) < 7:
            continue
        name, kind, implements, enforces, order, binding, placement = cells[:7]
        # Skip header / separator rows
        if name.lower() in ("name", "") or set(name) <= {"-", ":", " "}:
            continue
        kind = kind.lower()
        if kind not in valid_kinds:
            continue
        try:
            migration_order = int(re.sub(r"[^0-9]", "", order) or "0")
        except ValueError:
            migration_order = 0
        objects.append({
            "dbObjectId": f"{service_name}.{kind}.{name}",
            "name": name,
            "kind": kind,
            "implementsBrId": implements if implements and implements.upper() != "N/A" else None,
            "enforcesInvariantId": enforces if enforces and enforces.upper() != "N/A" else None,
            "migrationOrder": migration_order,
            "binding": binding or None,
            "placementProvenance": placement or None,
        })

    return objects


def _section_body(content: str, heading_re: str) -> str | None:
    """Return the markdown body under a heading matching heading_re, up to the next
    heading of the SAME-OR-HIGHER level (fewer-or-equal '#'). Deeper subheadings (more '#')
    are part of the body. None if the heading is absent.

    Example: a level-3 '### Entity State Model' section includes its '#### <entity>' subblocks,
    and ends at the next '###' / '##' / '#'.
    """
    hm = re.search(rf'(#{{1,6}})\s*{heading_re}\b', content, re.IGNORECASE)
    if not hm:
        return None
    level = len(hm.group(1))
    start = hm.end()
    # Next heading with <= level hashes ends the section.
    end_m = re.search(rf'\n#{{1,{level}}}\s', content[start:])
    return content[start:start + end_m.start()] if end_m else content[start:]


def parse_entity_states(service_dir: Path, service_name: str) -> tuple[list[dict], list[dict]]:
    """Parse the '### Entity State Model' section of 02-domain-model.md (Layer A).

    Returns (states, transitions). The section groups per entity under '#### <Entity> lifecycle',
    each with a '- **States:**' line and a transitions table:
      | From | To | Trigger (BR-ID) | Guard |
    States line may annotate initial/terminal: 'Draft (initial), Posted (terminal)'.
    Absent section → ([], []).
    """
    ddl_file = service_dir / "02-domain-model.md"
    if not ddl_file.exists():
        return [], []
    body = _section_body(ddl_file.read_text(), r'Entity State Model')
    if body is None:
        return [], []

    states: list[dict] = []
    transitions: list[dict] = []
    seen_states: set[tuple] = set()

    # Split into per-entity blocks on '#### <Entity> lifecycle'
    blocks = re.split(r'\n#{4}\s+', "\n" + body)
    for block in blocks:
        ent_m = re.match(r'([A-Za-z0-9_]+)\s+lifecycle', block.strip())
        if not ent_m:
            continue
        entity = ent_m.group(1)

        def _register_state(name: str, initial=False, terminal=False):
            name = name.strip()
            if not name:
                return
            key = (entity, name)
            if key in seen_states:
                return
            seen_states.add(key)
            states.append({
                "stateId": f"{service_name}.{entity}.{name}",
                "entity": entity, "state": name, "service": service_name,
                "isInitial": initial, "isTerminal": terminal,
            })

        # States line: '- **States:** Draft (initial), Validated, Posted (terminal)'
        sm = re.search(r'\*\*States:\*\*\s*(.+)', block)
        if sm:
            for tok in sm.group(1).split(","):
                tok = tok.strip()
                if not tok:
                    continue
                nm = re.sub(r'\s*\(.*?\)\s*', '', tok).strip()
                _register_state(nm, initial="(initial)" in tok.lower(), terminal="(terminal)" in tok.lower())

        # Transitions table rows: | From | To | Trigger | Guard |
        for line in block.splitlines():
            if not line.strip().startswith("|"):
                continue
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if len(cells) < 2:
                continue
            frm, to = cells[0], cells[1]
            if frm.lower() in ("from", "") or set(frm) <= {"-", ":", " "}:
                continue
            trigger = cells[2] if len(cells) > 2 else ""
            guard = cells[3] if len(cells) > 3 else ""
            # ensure both states exist (may not have been on the States line)
            _register_state(frm)
            _register_state(to)
            # extract a BR-ID if present in the trigger cell
            brm = re.search(r'BR-[A-Z]{2}-[A-Z]{2,4}-\d{2,3}', trigger)
            transitions.append({
                "fromId": f"{service_name}.{entity}.{frm}",
                "toId": f"{service_name}.{entity}.{to}",
                "entity": entity,
                "guard": guard or None,
                "triggerBrId": brm.group(0) if brm else None,
            })

    return states, transitions


def parse_invariants(service_dir: Path, service_name: str) -> list[dict]:
    """Parse the '### Data Invariants' section of 02-domain-model.md (Layer A).

    Fixed table: | Invariant ID | Statement | Entity | Kind | Tier |
    Absent section → [].
    """
    ddl_file = service_dir / "02-domain-model.md"
    if not ddl_file.exists():
        return []
    body = _section_body(ddl_file.read_text(), r'Data Invariants')
    if body is None:
        return []

    valid_tier = {"app", "db", "both"}
    invariants = []
    for line in body.splitlines():
        if not line.strip().startswith("|"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) < 5:
            continue
        inv_id, statement, entity, kind, tier = cells[:5]
        if not re.match(r'^INV-[A-Z]{2,4}-\d{3}$', inv_id):
            continue  # skips header/separator and malformed rows
        tier = tier.lower()
        if tier not in valid_tier:
            tier = "both"
        invariants.append({
            "invariantId": inv_id,
            "statement": statement[:500],
            "entity": entity or None,
            "service": service_name,
            "kind": kind.lower() or None,
            "tier": tier,
        })
    return invariants


def parse_endpoints(service_dir: Path) -> list[dict]:
    """Parse endpoints from 04-api-contract.yaml."""
    contract_file = service_dir / "04-api-contract.yaml"
    if not contract_file.exists():
        return []

    try:
        content = contract_file.read_text()
        contract = yaml.safe_load(content)
    except Exception:
        return []

    if not contract or "paths" not in contract:
        return []

    endpoints = []
    for path, methods in contract.get("paths", {}).items():
        if not isinstance(methods, dict):
            continue
        for method in ["get", "post", "put", "patch", "delete"]:
            if method in methods:
                op = methods[method]
                endpoints.append({
                    "path": path,
                    "method": method.upper(),
                    "operationId": op.get("operationId", ""),
                    "successStatus": 200,  # default
                })

    return endpoints


def import_service(driver, service_name: str, workspace: Path, check_only: bool = False):
    """Import one service's spec data to graph."""
    service_dir = workspace / "spec" / "microservices" / service_name
    if not service_dir.exists():
        print(f"  ERROR: {service_dir} does not exist", file=sys.stderr)
        return False

    rules = parse_business_rules(service_dir)
    tables = parse_tables(service_dir)
    endpoints = parse_endpoints(service_dir)
    db_objects = parse_db_objects(service_dir, service_name)
    states, transitions = parse_entity_states(service_dir, service_name)
    invariants = parse_invariants(service_dir, service_name)

    extras = []
    if db_objects:
        extras.append(f"{len(db_objects)} db-objects")
    if states:
        extras.append(f"{len(states)} states/{len(transitions)} transitions")
    if invariants:
        extras.append(f"{len(invariants)} invariants")
    extra_note = (", " + ", ".join(extras)) if extras else ""
    print(f"  {service_name}: {len(rules)} rules, {len(tables)} tables, {len(endpoints)} endpoints{extra_note}")

    if check_only:
        # Verify graph matches
        with driver.session() as session:
            graph_count = session.run(
                "MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(s:Service) "
                "WHERE s.name = $service OR s.serviceId = $service "
                "RETURN count(br) AS cnt",
                service=service_name
            ).single()["cnt"]

        if graph_count != len(rules):
            print(f"  CHECK FAILED: graph has {graph_count} rules, spec has {len(rules)}")
            return False
        else:
            print(f"  CHECK PASSED: {graph_count} rules match")
            return True

    # Import to graph
    unresolved_refs = []   # (brId, stem) where the Source Reference didn't match a SourceComponent
    with driver.session() as session:
        # Import BR-IDs
        for rule in rules:
            props = {
                "brId": rule["brId"],
                "statement": rule.get("statement", ""),
                "intent": rule.get("intent", "Unknown"),
                "confidence": rule.get("confidence", 0.7),
                "sourceRef": rule.get("sourceRef", ""),
                "discoveryMethod": rule.get("discoveryMethod", "DirectSourceRead"),
                "lifecycleState": "Assigned",
                "phase": "P4",
                "provenanceConfidence": rule.get("confidence", 0.7),
            }
            # Add vector properties if present
            for dim in ["ControlFlow", "DataFlow", "Constants", "StateTransitions",
                        "Outcomes", "DataWrites", "Integrations", "ErrorPaths"]:
                for prefix in ["src", "spec"]:
                    key = f"{prefix}{dim}"
                    if key in rule:
                        props[key] = rule[key]

            session.run(
                "MERGE (br:BusinessRule {brId: $brId}) "
                "SET br += $props",
                brId=rule["brId"], props=props
            )

            # ASSIGNED_TO edge
            session.run(
                "MATCH (br:BusinessRule {brId: $brId}) "
                "MERGE (s:Service {name: $service}) "
                "MERGE (br)-[:ASSIGNED_TO]->(s)",
                brId=rule["brId"], service=service_name
            )

            # EXTRACTED_FROM edge + SourceComponent.extracted flip + p4Intent stamp.
            # THIS IS THE HALF THAT WAS MISSING — without it the graph's two sides (BusinessRule vs the
            # CAST SourceComponent inventory) stay disconnected, so coverage freezes and never moves as
            # services are extracted. Resolve the rule's Source Reference to a SourceComponent and link it.
            src_stem = _source_ref_stem(rule.get("sourceRef"))
            if src_stem:
                linked = session.run(
                    "MATCH (sc:SourceComponent) "
                    "WHERE sc.name = $stem OR sc.name ENDS WITH $stem OR sc.castId ENDS WITH $stem "
                    "WITH sc LIMIT 1 "
                    "MATCH (br:BusinessRule {brId: $brId}) "
                    "MERGE (br)-[:EXTRACTED_FROM]->(sc) "
                    # p4Intent baseline = the current intent CONFIRMED by the P4 source read; a genuine
                    # correction is applied by the agent post-import (see steering 6a). extracted flips true.
                    "SET sc.extracted = true, "
                    "    sc.p4Intent = coalesce(sc.p4Intent, sc.intentCategory), "
                    "    sc.intentCategory = coalesce(sc.p4Intent, sc.intentCategory) "
                    "RETURN sc.castId AS cid",
                    stem=src_stem, brId=rule["brId"]
                ).single()
                if not linked:
                    unresolved_refs.append((rule["brId"], src_stem))

            # EXTENDS_VIA edges (Layer B) — link rule to any extension point it names.
            # MERGE a minimal ExtensionPoint so the edge has a target; Stage 1.8 (extensibility-model.md)
            # enriches the node's properties (mechanism, resolution, decision).
            for ext_id in rule.get("extensionPoints", []):
                session.run(
                    "MERGE (ep:ExtensionPoint {extPointId: $ext}) "
                    "ON CREATE SET ep.service = $service, ep.name = $ext "
                    "WITH ep "
                    "MATCH (br:BusinessRule {brId: $brId}) "
                    "MERGE (br)-[:EXTENDS_VIA]->(ep)",
                    ext=ext_id, brId=rule["brId"], service=service_name
                )

        # Import Tables
        for table in tables:
            session.run(
                "MERGE (t:Table {name: $name}) "
                "SET t.service = $service "
                "WITH t "
                "MATCH (s:Service {name: $service}) "
                "MERGE (s)-[:OWNS]->(t)",
                name=table["name"], service=service_name
            )

        # Import Endpoints
        for ep in endpoints:
            ep_id = f"{ep['method']} {ep['path']}"
            session.run(
                "MERGE (e:Endpoint {path: $path, method: $method}) "
                "SET e.service = $service, e.operationId = $opId "
                "WITH e "
                "MATCH (s:Service {name: $service}) "
                "MERGE (s)-[:EXPOSES]->(e)",
                path=ep["path"], method=ep["method"],
                service=service_name, opId=ep.get("operationId", "")
            )

        # Import DB Objects (Layer C — db-tier logic) + IMPLEMENTS_IN_DB edge
        for obj in db_objects:
            props = {
                "dbObjectId": obj["dbObjectId"],
                "name": obj["name"],
                "kind": obj["kind"],
                "service": service_name,
                "migrationOrder": obj["migrationOrder"],
            }
            for k in ("implementsBrId", "enforcesInvariantId", "binding", "placementProvenance"):
                if obj.get(k):
                    props[k] = obj[k]
            session.run(
                "MERGE (o:DbObject {dbObjectId: $id}) SET o += $props",
                id=obj["dbObjectId"], props=props,
            )
            # Link to the BR it implements (if any)
            if obj.get("implementsBrId"):
                session.run(
                    "MATCH (br:BusinessRule {brId: $brId}) "
                    "MATCH (o:DbObject {dbObjectId: $id}) "
                    "MERGE (br)-[:IMPLEMENTS_IN_DB]->(o)",
                    brId=obj["implementsBrId"], id=obj["dbObjectId"],
                )

        # Import Entity States (Layer A) + HAS_STATE edge (Table -> EntityState)
        for st in states:
            session.run(
                "MERGE (es:EntityState {stateId: $id}) SET es += $props",
                id=st["stateId"], props=st,
            )
            # Link owning table if it exists (table name == entity)
            session.run(
                "MATCH (es:EntityState {stateId: $id}) "
                "OPTIONAL MATCH (t:Table {name: $entity}) "
                "FOREACH (_ IN CASE WHEN t IS NULL THEN [] ELSE [1] END | MERGE (t)-[:HAS_STATE]->(es))",
                id=st["stateId"], entity=st["entity"],
            )

        # Import Transitions (EntityState -> EntityState)
        for tr in transitions:
            props = {}
            if tr.get("guard"):
                props["guard"] = tr["guard"]
            if tr.get("triggerBrId"):
                props["triggerBrId"] = tr["triggerBrId"]
            session.run(
                "MATCH (a:EntityState {stateId: $frm}) "
                "MATCH (b:EntityState {stateId: $to}) "
                "MERGE (a)-[r:TRANSITIONS_TO]->(b) SET r += $props",
                frm=tr["fromId"], to=tr["toId"], props=props,
            )

        # Import Invariants (Layer A) + CONSTRAINS edge (Invariant -> Table)
        for inv in invariants:
            session.run(
                "MERGE (iv:Invariant {invariantId: $id}) SET iv += $props",
                id=inv["invariantId"], props=inv,
            )
            if inv.get("entity"):
                session.run(
                    "MATCH (iv:Invariant {invariantId: $id}) "
                    "OPTIONAL MATCH (t:Table {name: $entity}) "
                    "FOREACH (_ IN CASE WHEN t IS NULL THEN [] ELSE [1] END | MERGE (iv)-[:CONSTRAINS]->(t))",
                    id=inv["invariantId"], entity=inv["entity"],
                )

    if unresolved_refs:
        print(f"    WARN: {len(unresolved_refs)} rules had a Source Reference that did not match any "
              f"SourceComponent (no EXTRACTED_FROM edge). Some may be greenfield; if they should trace to "
              f"source, check the reference format or that the component was ingested at P1 Step 0.")
        for br_id, stem in unresolved_refs[:10]:
            print(f"      {br_id} -> '{stem}'")

    return True


def import_cross_service_contracts(driver, workspace: Path) -> int:
    """Project cross-service contract shapes onto CALLS edges (Class-A knowledge for egress).

    Reads spec/shared/cross-service-contracts.md (produced by the Phase 4 Stage 1.5
    consumer-provider reconciliation gate). Expected: a markdown table with rows like:

      | Consumer | Provider | Endpoint | Request Shape | Response Shape | Status |
      | ap-service | gl-service | POST /distributions/post | {...} | {...} | OK |

    Sets requestShape/responseShape/verified on the CALLS edge (consumer)-[:CALLS]->(provider).
    Idempotent; skips silently if the file is absent (pre-Stage-1.5).
    """
    contracts_file = workspace / "spec" / "shared" / "cross-service-contracts.md"
    if not contracts_file.exists():
        return 0

    content = contracts_file.read_text(encoding="utf-8")
    updated = 0
    with driver.session() as session:
        for line in content.splitlines():
            if not line.strip().startswith("|"):
                continue
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if len(cells) < 6:
                continue
            consumer, provider, endpoint, req_shape, resp_shape, status = cells[:6]
            # Skip header / separator rows
            if consumer.lower() in ("consumer", "") or set(consumer) <= {"-", ":", " "}:
                continue
            verified = status.strip().upper() in ("OK", "RECONCILED", "VERIFIED", "YES")
            r = session.run("""
                MATCH (c:Service), (p:Service)
                WHERE (c.name = $consumer OR c.serviceId = $consumer OR toLower(replace(c.name,' ','-')) = toLower($consumer))
                  AND (p.name = $provider OR p.serviceId = $provider OR toLower(replace(p.name,' ','-')) = toLower($provider))
                MERGE (c)-[rel:CALLS]->(p)
                SET rel.requestShape = $req,
                    rel.responseShape = $resp,
                    rel.verified = $verified,
                    rel.endpoints = coalesce(rel.endpoints, []) +
                        CASE WHEN NOT $endpoint IN coalesce(rel.endpoints, []) THEN [$endpoint] ELSE [] END,
                    rel.protocol = coalesce(rel.protocol, 'REST')
                RETURN rel
            """, consumer=consumer, provider=provider, endpoint=endpoint,
                req=req_shape[:500], resp=resp_shape[:500], verified=verified).single()
            if r:
                updated += 1
    if updated:
        print(f"Cross-service contracts: {updated} CALLS edges annotated with shapes")
    return updated


def main():
    parser = argparse.ArgumentParser(description="Import service specs to Neo4j graph")
    parser.add_argument("--service", help="Single service to import")
    parser.add_argument("--all", action="store_true", help="Import all services")
    parser.add_argument("--check", action="store_true", help="Check only (don't write)")
    args = parser.parse_args()

    if not args.service and not args.all:
        parser.error("Specify --service <name> or --all")

    workspace = find_workspace_root()
    driver = connect()

    try:
        before_extracted = 0
        if not args.check:
            before_extracted = driver.session().run(
                "MATCH (sc:SourceComponent) WHERE sc.extracted = true RETURN count(sc) AS n"
            ).single()["n"]

        if args.all:
            services = find_services(workspace)
            print(f"Found {len(services)} services")
        else:
            services = [args.service]

        success = True
        total_rules = 0
        for svc in services:
            result = import_service(driver, svc, workspace, check_only=args.check)
            if not result:
                success = False

        if not args.check:
            # Project cross-service contract shapes onto CALLS edges (after all services exist)
            if args.all:
                import_cross_service_contracts(driver, workspace)

            # Run inferences
            print("\nRunning graph inferences...")
            with driver.session() as session:
                # Count total imported
                total = session.run("MATCH (br:BusinessRule) RETURN count(br) AS cnt").single()["cnt"]
                print(f"Total BR nodes in graph: {total}")

            # SELF-CHECK — the guard that catches the silent-disconnect bug (the SourceComponent-linking
            # half not being written). Fails loud so it surfaces on import, not at QC.
            with driver.session() as session:
                after_extracted = session.run(
                    "MATCH (sc:SourceComponent) WHERE sc.extracted = true RETURN count(sc) AS n"
                ).single()["n"]
                biz = session.run(
                    "MATCH (sc:SourceComponent) WHERE coalesce(sc.businessLayer,true)=true RETURN count(sc) AS n"
                ).single()["n"]
                orphan = session.run(
                    "MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(s:Service) "
                    "WHERE s.name IN $services "
                    "AND trim(coalesce(br.sourceRef, '')) <> '' "
                    "AND NOT (br)-[:EXTRACTED_FROM]->() "
                    "RETURN count(DISTINCT br) AS n",
                    services=services,
                ).single()["n"]
            cov = (after_extracted / biz * 100) if biz else 0
            print("\n=== IMPORT SELF-CHECK ===")
            print(f"  SourceComponent.extracted: {before_extracted} -> {after_extracted} "
                  f"(delta {after_extracted - before_extracted})")
            print(f"  Coverage (extracted / businessLayer): {after_extracted}/{biz} = {cov:.1f}%")
            print(f"  BusinessRules without EXTRACTED_FROM: {orphan} (greenfield rules are expected here)")
            if orphan > 0:
                print("  FAIL: source-backed rules are missing EXTRACTED_FROM edges — the "
                      "SourceComponent-linking half is incomplete. Fix the resolver or the source "
                      "reference format and re-run; do NOT hand-edit the graph or use a partial importer.",
                      file=sys.stderr)
                success = False

        if not success:
            sys.exit(2)

    finally:
        driver.close()


if __name__ == "__main__":
    main()
