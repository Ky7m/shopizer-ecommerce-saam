"""SessionStart hook script: queries SAAM Knowledge Graph for current engagement state.

Called by the Kiro SessionStart hook. Outputs context as text to stdout.
The output is injected into the agent's context at the start of every session.

Exit codes:
  0 = success (stdout forwarded to agent context)
  1 = graph unavailable (silent — agent proceeds without graph context)
"""

import os
import sys
import json

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


def get_context() -> str:
    """Query the graph and produce a context summary for the agent."""
    try:
        driver = GraphDatabase.driver(NEO4J_URI, auth=(NEO4J_USER, NEO4J_PASSWORD))
        driver.verify_connectivity()
    except Exception:
        # Graph not available — exit silently, agent proceeds without context
        sys.exit(1)

    with driver.session() as session:
        # Get engagement overview
        overview = session.run("""
            OPTIONAL MATCH (sc:SourceComponent) WITH count(sc) AS components
            OPTIONAL MATCH (br:BusinessRule) WITH components, count(br) AS rules
            OPTIONAL MATCH (s:Service) WITH components, rules, count(s) AS services
            OPTIONAL MATCH (ta:TestAssertion) WITH components, rules, services, count(ta) AS tests
            OPTIONAL MATCH (impl:Implementation) WITH components, rules, services, tests, count(impl) AS implementations
            OPTIONAL MATCH (dev:Deviation {status: 'OPEN'}) WITH components, rules, services, tests, implementations, count(dev) AS openDeviations
            RETURN components, rules, services, tests, implementations, openDeviations
        """).single()

        # Get per-service status
        svc_results = session.run("""
            MATCH (s:Service)
            RETURN s.name AS name, s.serviceId AS id, s.priority AS priority,
                   coalesce(s.implementationCompleteness, 0) AS completeness,
                   coalesce(s.testCoverage, 0) AS testCoverage,
                   coalesce(s._confidence, 0) AS confidence
            ORDER BY s.priority, s.name
        """).data()

        # Get open deviations (top 5 by severity)
        dev_results = session.run("""
            MATCH (dev:Deviation {status: 'OPEN'})
            RETURN dev.deviationId AS id, dev.type AS type, dev.service AS service,
                   dev.description AS description
            ORDER BY dev.type, dev.deviationId
            LIMIT 5
        """).data()

        # Get next pending work (rules without implementation)
        pending = session.run("""
            MATCH (d:Decision)-[:DECIDED_AS]->(br:BusinessRule)-[:ASSIGNED_TO]->(s:Service)
            WHERE d.classification IN ['Core', 'Active']
            AND NOT EXISTS { MATCH (br)-[:CLAIMS_IMPLEMENTATION]->() }
            WITH s.name AS service, s.serviceId AS serviceId, s.priority AS priority, count(br) AS pendingRules
            RETURN service, serviceId, pendingRules
            ORDER BY priority, pendingRules DESC
            LIMIT 5
        """).data()

    driver.close()

    # Build context output
    output = []
    output.append("=== SAAM KNOWLEDGE GRAPH — SESSION CONTEXT ===")
    output.append("")

    if overview and overview["rules"] > 0:
        o = overview
        output.append(f"Engagement: {o['components']} source components, {o['rules']} rules, {o['services']} services")
        output.append(f"Implementation: {o['implementations']} methods, {o['tests']} test assertions")
        if o["openDeviations"] > 0:
            output.append(f"Open deviations: {o['openDeviations']} (need attention)")
        output.append("")

        # Service status
        if svc_results:
            output.append("Service Status:")
            for s in svc_results:
                comp = s["completeness"] * 100
                tc = s["testCoverage"] * 100
                conf = s["confidence"]
                indicator = "done" if comp >= 100 and tc >= 100 else f"impl={comp:.0f}%, tests={tc:.0f}%"
                output.append(f"  {s['name']} ({s['id']}): {indicator}, confidence={conf:.2f}")
            output.append("")

        # Pending work
        if pending:
            output.append("Pending implementation:")
            for p in pending:
                output.append(f"  {p['service']} ({p['serviceId']}): {p['pendingRules']} rules awaiting implementation")
            output.append("")

        # Open deviations
        if dev_results:
            output.append("Open deviations (top 5):")
            for d in dev_results:
                output.append(f"  [{d['type']}] {d['id']} ({d['service']}): {d['description'][:80]}")
            output.append("")

        output.append("Graph tools available: graph_implementation_context, graph_fix_context, graph_phase_status, graph_impact_analysis")
        output.append("Use graph_implementation_context(serviceId) before implementing a service.")
        output.append("Use graph_fix_context(deviationId) before fixing a deviation.")
    else:
        output.append("SAAM Graph is connected but empty. Data will be populated as phases execute.")
        output.append("Use graph_bulk_import after completing each phase to populate the graph.")

    output.append("")
    output.append("=== END GRAPH CONTEXT ===")

    return "\n".join(output)


if __name__ == "__main__":
    context = get_context()
    print(context)
