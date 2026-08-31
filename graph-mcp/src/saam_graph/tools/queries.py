"""Graph query tools: query_nodes, traverse, impact_analysis, cypher."""

import json
from typing import Any

from mcp.server import MCPServer

from saam_graph.db import GraphDB
from saam_graph.tools.mutations import VALID_NODE_TYPES, VALID_EDGE_TYPES, _get_id_field


def register_query_tools(server: MCPServer) -> None:
    """Register graph query tools with the MCP server."""

    @server.tool()
    def graph_query_nodes(nodeType: str, filters: dict | None = None, limit: int = 50, orderBy: str | None = None) -> str:
        """Find nodes by type and optional property filters. Returns matching nodes."""
        filters = filters or {}
        limit = min(limit, 200)

        where_clauses = []
        params: dict[str, Any] = {}
        for i, (key, value) in enumerate(filters.items()):
            param_name = f"f{i}"
            where_clauses.append(f"n.{key} = ${param_name}")
            params[param_name] = value

        where_str = f"WHERE {' AND '.join(where_clauses)}" if where_clauses else ""
        order_clause = ""
        if orderBy:
            if orderBy.startswith("-"):
                order_clause = f"ORDER BY n.{orderBy[1:]} DESC"
            else:
                order_clause = f"ORDER BY n.{orderBy} ASC"

        query = f"MATCH (n:{nodeType}) {where_str} RETURN n {order_clause} LIMIT {limit}"
        results = GraphDB.execute_read(query, params)
        nodes = [r["n"] for r in results]

        if not nodes:
            return f"No {nodeType} nodes found matching filters: {filters}"

        output = f"Found {len(nodes)} {nodeType} node(s):\n\n"
        for node in nodes:
            display = {k: v for k, v in node.items() if not k.startswith("_")}
            output += f"  {json.dumps(display, default=str)}\n"
        return output

    @server.tool()
    def graph_traverse(startNodeType: str, startNodeId: str, direction: str = "outgoing", edgeTypes: list[str] | None = None, maxHops: int = 2) -> str:
        """Traverse relationships from a starting node, returning connected nodes up to N hops away."""
        id_field = _get_id_field(startNodeType)
        max_hops = min(maxHops, 5)

        if edgeTypes:
            rel_pattern = f"[r:{' | '.join(edgeTypes)}*1..{max_hops}]"
        else:
            rel_pattern = f"[r*1..{max_hops}]"

        if direction == "incoming":
            pattern = f"(start)<-{rel_pattern}-(end)"
        elif direction == "both":
            pattern = f"(start)-{rel_pattern}-(end)"
        else:
            pattern = f"(start)-{rel_pattern}->(end)"

        query = f"""
        MATCH (start:{startNodeType} {{{id_field}: $startId}})
        MATCH path = {pattern}
        WHERE start <> end
        RETURN DISTINCT end, length(path) AS hops
        ORDER BY hops ASC
        LIMIT 50
        """
        results = GraphDB.execute_read(query, {"startId": startNodeId})

        if not results:
            return f"No connected nodes found from {startNodeType}('{startNodeId}') within {max_hops} hops"

        output = f"Traversal from {startNodeType}('{startNodeId}'), {direction}, max {max_hops} hops:\n\n"
        for r in results:
            node = r["end"]
            display = {k: v for k, v in node.items() if not k.startswith("_")}
            output += f"  [{r['hops']} hop(s)] {json.dumps(display, default=str)}\n"
        return output

    @server.tool()
    def graph_impact_analysis(nodeType: str, nodeId: str) -> str:
        """Find all nodes affected by a change to the given node."""
        id_field = _get_id_field(nodeType)

        if nodeType == "BusinessRule":
            query = """
                MATCH (br:BusinessRule {brId: $id})
                OPTIONAL MATCH (br)-[:ASSIGNED_TO]->(s:Service)
                OPTIONAL MATCH (br)-[:CLAIMS_IMPLEMENTATION]->(impl:Implementation)
                OPTIONAL MATCH (br)-[:TESTED_BY]->(test:TestAssertion)
                OPTIONAL MATCH (br)<-[:DEVIATES_FROM]-(dev:Deviation)
                RETURN count(DISTINCT s) AS services, count(DISTINCT impl) AS implementations,
                       count(DISTINCT test) AS tests, count(DISTINCT dev) AS deviations
            """
        elif nodeType == "Service":
            query = """
                MATCH (s:Service {serviceId: $id})
                OPTIONAL MATCH (s)<-[:ASSIGNED_TO]-(br:BusinessRule)
                OPTIONAL MATCH (s)-[:OWNS]->(t:Table)
                OPTIONAL MATCH (s)-[:EXPOSES]->(e:Endpoint)
                RETURN count(DISTINCT br) AS rules, count(DISTINCT t) AS tables, count(DISTINCT e) AS endpoints
            """
        else:
            query = f"""
                MATCH (n:{nodeType} {{{id_field}: $id}})
                OPTIONAL MATCH (n)-[r]->(downstream)
                RETURN count(downstream) AS downstream
            """

        results = GraphDB.execute_read(query, {"id": nodeId})
        if not results:
            return f"Node not found: {nodeType}('{nodeId}')"

        return f"Impact analysis for {nodeType}('{nodeId}'):\n{json.dumps(results[0], default=str)}"

    @server.tool()
    def graph_cypher(query: str, params: dict | None = None) -> str:
        """Execute a raw Cypher query. Use for complex queries not covered by other tools."""
        params = params or {}
        query_upper = query.upper().strip()
        if any(kw in query_upper for kw in ["DELETE", "DETACH", "DROP"]) and "RETURN" not in query_upper:
            return "ERROR: Destructive query blocked. Use mutation tools instead."

        try:
            results = GraphDB.execute_read(query, params)
        except Exception as e:
            return f"ERROR: {e}"

        if not results:
            return "Query returned no results."

        output = f"Query returned {len(results)} result(s):\n\n"
        for i, record in enumerate(results[:50], 1):
            output += f"  [{i}] {json.dumps(record, default=str)}\n"
        return output
