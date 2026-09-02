"""Graph context construction tools."""

import json
from typing import Any

from mcp.server.fastmcp import FastMCP

from saam_graph.db import GraphDB


def register_context_tools(server: FastMCP) -> None:
    """Register graph context construction tools with the MCP server."""

    @server.tool()
    def graph_implementation_context(serviceId: str, includeRuleLogic: bool = True, brIdGroup: str | None = None) -> str:
        """Get everything needed to implement a service: BR-IDs, tables, endpoints, dependencies."""
        service_result = GraphDB.execute_read(
            "MATCH (s:Service {serviceId: $id}) RETURN s", {"id": serviceId}
        )
        if not service_result:
            return f"ERROR: Service '{serviceId}' not found."

        service = service_result[0]["s"]
        br_filter = f"AND br.brId STARTS WITH '{brIdGroup}'" if brIdGroup else ""

        rules = GraphDB.execute_read(f"""
            MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(s:Service {{serviceId: $id}}) {br_filter}
            OPTIONAL MATCH (d:Decision)-[:DECIDED_AS]->(br)
            RETURN br, d.classification AS classification, d.weight AS weight
            ORDER BY coalesce(br.effectiveConfidence, 0) ASC
        """, {"id": serviceId})

        tables = GraphDB.execute_read("""
            MATCH (s:Service {serviceId: $id})-[:OWNS]->(t:Table) RETURN t ORDER BY t.name
        """, {"id": serviceId})

        endpoints = GraphDB.execute_read("""
            MATCH (s:Service {serviceId: $id})-[:EXPOSES]->(e:Endpoint) RETURN e ORDER BY e.path
        """, {"id": serviceId})

        deps = GraphDB.execute_read("""
            MATCH (s:Service {serviceId: $id})-[r:CALLS]->(dep:Service)
            RETURN dep.name AS name, dep.serviceId AS id, r.protocol AS protocol
        """, {"id": serviceId})

        deviations = GraphDB.execute_read("""
            MATCH (dev:Deviation {service: $id, status: 'OPEN'})
            RETURN dev.deviationId AS devId, dev.type AS type, dev.description AS desc
        """, {"id": serviceId})

        # Build output
        output = f"IMPLEMENTATION CONTEXT: {service.get('name', serviceId)}\n{'=' * 60}\n\n"
        output += f"Confidence: {service.get('_confidence', 'N/A')}\n"
        output += f"Completeness: {service.get('implementationCompleteness', 'N/A')}\n\n"

        active_rules = [r for r in rules if r.get("classification") in ("Core", "Active", None)]
        attention = [r for r in active_rules if (r["br"].get("effectiveConfidence") or 0) < 0.7]

        if attention:
            output += f"ATTENTION NEEDED ({len(attention)} rules, confidence < 0.7):\n"
            for r in attention:
                br = r["br"]
                eff = br.get("effectiveConfidence", 0)
                output += f"  {br.get('brId', '?')} [{br.get('lifecycleState', '?')}] conf={eff:.2f}\n"
                if includeRuleLogic:
                    output += f"    Statement: {br.get('statement', 'N/A')}\n"
            output += "\n"

        output += f"ALL RULES ({len(active_rules)} Active/Core):\n"
        for r in active_rules:
            br = r["br"]
            output += f"  {br.get('brId', '?')} [{br.get('lifecycleState', '?')}] {r.get('classification', 'Active')}\n"
            if includeRuleLogic:
                output += f"    {br.get('statement', '')}\n"

        output += f"\nTABLES ({len(tables)}):\n"
        for t in tables:
            tbl = t["t"]
            output += f"  {tbl.get('name', '?')} (cols: {len(tbl.get('columns', []))})\n"

        output += f"\nENDPOINTS ({len(endpoints)}):\n"
        for e in endpoints:
            ep = e["e"]
            output += f"  {ep.get('method', '?')} {ep.get('path', '?')} -> {ep.get('successStatus', '?')}\n"

        if deps:
            output += f"\nDEPENDENCIES ({len(deps)}):\n"
            for d in deps:
                output += f"  -> {d['name']} ({d['id']}) via {d.get('protocol', '?')}\n"

        if deviations:
            output += f"\nOPEN DEVIATIONS ({len(deviations)}):\n"
            for d in deviations:
                output += f"  [{d['type']}] {d['devId']}: {d['desc']}\n"

        output += "\nNAMING AUTHORITY: 04-api-contract.yaml\n"
        return output

    @server.tool()
    def graph_fix_context(deviationId: str | None = None, brId: str | None = None, service: str | None = None) -> str:
        """Get context for fixing a deviation, bug, or rule. Provide one of: deviationId, brId, or service."""
        if deviationId:
            result = GraphDB.execute_read("""
                MATCH (dev:Deviation {deviationId: $id}) RETURN dev
            """, {"id": deviationId})
            if not result:
                return f"ERROR: Deviation '{deviationId}' not found."
            dev = result[0]["dev"]
            output = f"FIX CONTEXT: {deviationId}\n"
            output += f"  Type: {dev.get('type')}\n  Service: {dev.get('service')}\n"
            output += f"  Spec says: {dev.get('specSays', 'N/A')}\n"
            output += f"  Service does: {dev.get('serviceDoes', 'N/A')}\n"
            output += f"  Fix: {dev.get('fixRecommendation', 'N/A')}\n"
            return output
        elif brId:
            result = GraphDB.execute_read("""
                MATCH (br:BusinessRule {brId: $id})
                OPTIONAL MATCH (br)-[:ASSIGNED_TO]->(s:Service)
                RETURN br, s.name AS service
            """, {"id": brId})
            if not result:
                return f"ERROR: Rule '{brId}' not found."
            br = result[0]["br"]
            output = f"FIX CONTEXT: {brId}\n"
            output += f"  Statement: {br.get('statement')}\n"
            output += f"  Service: {result[0].get('service')}\n"
            output += f"  Lifecycle: {br.get('lifecycleState')}\n"
            output += f"  Confidence: {br.get('effectiveConfidence')}\n"
            return output
        elif service:
            devs = GraphDB.execute_read("""
                MATCH (dev:Deviation {service: $id, status: 'OPEN'})
                RETURN dev.deviationId AS id, dev.type AS type, dev.description AS desc
                ORDER BY dev.type
            """, {"id": service})
            if not devs:
                return f"No open deviations for service '{service}'."
            output = f"OPEN DEVIATIONS: {service} ({len(devs)} items)\n"
            for d in devs:
                output += f"  [{d['type']}] {d['id']}: {d['desc']}\n"
            return output
        return "ERROR: Provide deviationId, brId, or service"

    @server.tool()
    def graph_phase_status(phase: str = "all") -> str:
        """Get completion metrics for a phase or the engagement ('all')."""
        stats = GraphDB.execute_read("""
            OPTIONAL MATCH (sc:SourceComponent) WITH count(sc) AS components
            OPTIONAL MATCH (br:BusinessRule) WITH components, count(br) AS rules
            OPTIONAL MATCH (s:Service) WITH components, rules, count(s) AS services
            OPTIONAL MATCH (ta:TestAssertion) WITH components, rules, services, count(ta) AS tests
            OPTIONAL MATCH (impl:Implementation) WITH components, rules, services, tests, count(impl) AS impls
            OPTIONAL MATCH (dev:Deviation {status: 'OPEN'}) WITH components, rules, services, tests, impls, count(dev) AS devs
            RETURN components, rules, services, tests, impls, devs
        """)
        if not stats:
            return "Graph is empty."
        s = stats[0]
        output = f"ENGAGEMENT STATUS:\n"
        output += f"  Components: {s['components']}, Rules: {s['rules']}, Services: {s['services']}\n"
        output += f"  Tests: {s['tests']}, Implementations: {s['impls']}, Open deviations: {s['devs']}\n"
        return output
