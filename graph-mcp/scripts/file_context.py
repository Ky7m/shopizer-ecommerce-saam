"""PreToolUse hook script: provides service context when agent writes to sourcecode/ files.

Called by the harness PreToolUse hook when the agent is about to write/modify files
in the sourcecode/ directory. Receives JSON on stdin with the tool call details.
Extracts the file path, determines which service it belongs to, and returns
relevant BR-IDs, endpoints, and field names from the graph.

Exit codes:
  0 = success (stdout forwarded — contains context for the agent)
  1 = not applicable or graph unavailable (silent pass-through)
"""

import os
import sys
import json
import re

from neo4j import GraphDatabase

NEO4J_URI = os.environ.get("NEO4J_URI", "bolt://localhost:7687")
NEO4J_USER = os.environ.get("NEO4J_USER", "neo4j")
NEO4J_PASSWORD = os.environ.get("NEO4J_PASSWORD", "saamgraph")


def _load_env():
    """Load .env from graph-mcp/ if available (has dynamic port)."""
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


def extract_service_from_path(file_path: str) -> str | None:
    """Extract service name from a file path like sourcecode/order-service/src/..."""
    # Match sourcecode/<service-name>/...
    match = re.search(r"sourcecode/([^/]+)/", file_path)
    if match:
        return match.group(1)
    return None


def get_service_id_from_name(session, service_name: str) -> str | None:
    """Look up service ID from service name (handles kebab-case to service matching)."""
    # Try exact name match first
    result = session.run("""
        MATCH (s:Service)
        WHERE toLower(replace(s.name, ' ', '-')) = toLower($name)
           OR toLower(s.name) = toLower(replace($name, '-', ' '))
           OR s.serviceId = $name
        RETURN s.serviceId AS id, s.name AS name
        LIMIT 1
    """, {"name": service_name}).single()

    return result["id"] if result else None


def get_file_context(file_path: str) -> str | None:
    """Query graph for context relevant to the file being modified."""
    service_name = extract_service_from_path(file_path)
    if not service_name:
        return None

    try:
        driver = GraphDatabase.driver(NEO4J_URI, auth=(NEO4J_USER, NEO4J_PASSWORD))
        driver.verify_connectivity()
    except Exception:
        return None

    with driver.session() as session:
        service_id = get_service_id_from_name(session, service_name)
        if not service_id:
            driver.close()
            return None

        # Get endpoints for the service (contract field names)
        endpoints = session.run("""
            MATCH (s:Service {serviceId: $id})-[:EXPOSES]->(e:Endpoint)
            RETURN e.path AS path, e.method AS method, e.successStatus AS status
            ORDER BY e.path, e.method
        """, {"id": service_id}).data()

        # Get BR-IDs not yet implemented (what the agent should be working on)
        pending_rules = session.run("""
            MATCH (d:Decision)-[:DECIDED_AS]->(br:BusinessRule)-[:ASSIGNED_TO]->(s:Service {serviceId: $id})
            WHERE d.classification IN ['Core', 'Active']
            AND NOT EXISTS { MATCH (br)-[:CLAIMS_IMPLEMENTATION]->() }
            RETURN br.brId AS brId, br.statement AS statement, br.intent AS intent
            ORDER BY br.brId
            LIMIT 10
        """, {"id": service_id}).data()

        # Get open deviations for this service
        deviations = session.run("""
            MATCH (dev:Deviation {service: $id, status: 'OPEN'})
            RETURN dev.deviationId AS id, dev.type AS type, dev.description AS description
            LIMIT 5
        """, {"id": service_id}).data()

        # Get key fields from the contract (most-referenced schema fields)
        fields = session.run("""
            MATCH (s:Service {serviceId: $id})-[:EXPOSES]->(e:Endpoint)
            OPTIONAL MATCH (f:Field {endpoint: e.path})
            RETURN DISTINCT f.name AS fieldName, f.type AS fieldType
            ORDER BY f.name
            LIMIT 20
        """, {"id": service_id}).data()

    driver.close()

    # Build context
    output = []
    output.append(f"[SAAM Graph] Writing to {service_name} (service: {service_id})")

    if endpoints:
        output.append(f"  Endpoints ({len(endpoints)}):")
        for ep in endpoints[:8]:
            output.append(f"    {ep['method']} {ep['path']} -> {ep['status']}")

    if fields:
        field_names = [f["fieldName"] for f in fields if f.get("fieldName")]
        if field_names:
            output.append(f"  Contract fields: {', '.join(field_names[:15])}")

    if pending_rules:
        output.append(f"  Pending rules ({len(pending_rules)}):")
        for r in pending_rules[:5]:
            stmt = r["statement"][:60] + "..." if len(r.get("statement", "")) > 60 else r.get("statement", "")
            output.append(f"    {r['brId']}: {stmt}")

    if deviations:
        output.append(f"  Open deviations ({len(deviations)}):")
        for d in deviations:
            output.append(f"    [{d['type']}] {d['id']}: {d['description'][:60]}")

    output.append("  Reminder: field names MUST come from 04-api-contract.yaml (the naming authority)")

    return "\n".join(output)


if __name__ == "__main__":
    # Read stdin for hook context (JSON with tool call details)
    try:
        stdin_data = json.loads(sys.stdin.read())
    except (json.JSONDecodeError, EOFError):
        sys.exit(1)

    # Extract file path from the tool arguments
    # The tool call could be fs_write, str_replace, or fs_append
    tool_args = stdin_data.get("arguments", {})
    file_path = tool_args.get("path", "") or tool_args.get("targetFile", "")

    if not file_path:
        sys.exit(1)

    # Only activate for sourcecode/ files
    if "sourcecode/" not in file_path:
        sys.exit(1)

    context = get_file_context(file_path)
    if context:
        print(context)
    else:
        sys.exit(1)
