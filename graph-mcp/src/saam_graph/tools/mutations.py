"""Graph mutation tools: add_node, add_edge, update_node, bulk_import."""

import json
from datetime import datetime, timezone
from typing import Any

from mcp.server.fastmcp import FastMCP

from saam_graph.db import GraphDB

# IMPORTANT: These sets MUST match saam-graph-schema.yaml.
# If you add a node/edge type to the schema, add it here too.
# Source of truth: graph-mcp/saam-graph-schema.yaml
VALID_NODE_TYPES = {
    "SourceComponent", "SourceTable", "BusinessRule", "Service",
    "Table", "Endpoint", "Field", "Decision", "TestAssertion",
    "Implementation", "Deviation", "PhaseEvent", "Workflow", "Event",
    # Layer A: invariant / state / lifecycle
    "EntityState", "Invariant",
    # Layer B: extensibility engine
    "ExtensionPoint",
    # Layer C: DB-tier logic + placement
    "DbObject", "PlacementDecision",
}

VALID_EDGE_TYPES = {
    "EXTRACTED_FROM", "SOURCE_ACCESSES", "SOURCE_CALLS",
    "ASSIGNED_TO", "OWNS", "EXPOSES", "CALLS", "MAPS_TO",
    "CLAIMS_IMPLEMENTATION", "VALIDATED_BY", "TESTED_BY",
    "DECIDED_AS", "DEVIATES_FROM", "RECONCILED_WITH",
    "TRANSITIVELY_DEPENDS_ON", "CANDIDATE_FOR_REMOVAL",
    "PARTICIPATES_IN", "ORCHESTRATES", "PUBLISHES", "CONSUMES", "TRIGGERS_EVENT",
    # Layer A: invariant / state / lifecycle
    "HAS_STATE", "TRANSITIONS_TO", "CONSTRAINS",
    # Layer B: extensibility engine
    "EXTENDS_VIA", "RESOLVED_BY",
    # Layer C: DB-tier logic + placement
    "IMPLEMENTS_IN_DB", "BOUND_TO", "PLACED_AS",
}


def _get_id_field(node_type: str) -> str:
    """Get the unique ID property name for a given node type."""
    id_fields = {
        "SourceComponent": "castId",
        "SourceTable": "name",
        "BusinessRule": "brId",
        "Service": "serviceId",
        "Table": "name",
        "Endpoint": "path",
        "Field": "name",
        "Decision": "brId",
        "TestAssertion": "testNum",
        "Implementation": "methodName",
        "Deviation": "deviationId",
        "PhaseEvent": "id",
        "Workflow": "workflowId",
        "Event": "eventName",
        "EntityState": "stateId",
        "Invariant": "invariantId",
        "ExtensionPoint": "extPointId",
        "DbObject": "dbObjectId",
        "PlacementDecision": "placementId",
    }
    return id_fields.get(node_type, "id")


def register_mutation_tools(server: FastMCP) -> None:
    """Register graph mutation tools with the MCP server."""

    @server.tool()
    def graph_add_node(nodeType: str, id: str, properties: dict, confidence: float = 0.8, createdBy: str = "agent") -> str:
        """Add a node to the SAAM knowledge graph. Specify the node type, unique identifier, and properties. Provenance is added automatically."""
        if nodeType not in VALID_NODE_TYPES:
            return f"ERROR: Invalid node type '{nodeType}'. Valid: {sorted(VALID_NODE_TYPES)}"

        id_field = _get_id_field(nodeType)
        properties[id_field] = id
        properties["_confidence"] = confidence
        properties["_createdBy"] = createdBy
        properties["_createdAt"] = datetime.now(timezone.utc).isoformat()
        properties["_lastUpdated"] = datetime.now(timezone.utc).isoformat()

        query = f"""
        MERGE (n:{nodeType} {{{id_field}: $id}})
        SET n += $props
        RETURN n
        """
        result = GraphDB.execute_write_single(query, {"id": id, "props": properties})
        if result:
            return f"OK: Created/updated {nodeType} node with {id_field}='{id}' ({len(properties)} properties)"
        return f"ERROR: Failed to create {nodeType} node"

    @server.tool()
    def graph_add_edge(edgeType: str, sourceId: str, sourceType: str, targetId: str, targetType: str, properties: dict | None = None) -> str:
        """Add a relationship (edge) between two existing nodes in the SAAM knowledge graph."""
        if edgeType not in VALID_EDGE_TYPES:
            return f"ERROR: Invalid edge type '{edgeType}'. Valid: {sorted(VALID_EDGE_TYPES)}"

        props = properties or {}
        props["_createdAt"] = datetime.now(timezone.utc).isoformat()

        source_id_field = _get_id_field(sourceType)
        target_id_field = _get_id_field(targetType)

        query = f"""
        MATCH (source:{sourceType} {{{source_id_field}: $sourceId}})
        MATCH (target:{targetType} {{{target_id_field}: $targetId}})
        MERGE (source)-[r:{edgeType}]->(target)
        SET r += $props
        RETURN type(r) AS relType
        """
        result = GraphDB.execute_write_single(query, {"sourceId": sourceId, "targetId": targetId, "props": props})
        if result:
            return f"OK: Created {edgeType} edge from {sourceType}('{sourceId}') to {targetType}('{targetId}')"
        return f"ERROR: Failed to create edge. Check that both nodes exist."

    @server.tool()
    def graph_update_node(nodeType: str, id: str, properties: dict) -> str:
        """Update properties of an existing node. Merges new properties with existing ones."""
        if nodeType not in VALID_NODE_TYPES:
            return f"ERROR: Invalid node type '{nodeType}'"

        id_field = _get_id_field(nodeType)
        properties["_lastUpdated"] = datetime.now(timezone.utc).isoformat()

        query = f"""
        MATCH (n:{nodeType} {{{id_field}: $id}})
        SET n += $props
        RETURN n
        """
        result = GraphDB.execute_write_single(query, {"id": id, "props": properties})
        if result:
            return f"OK: Updated {nodeType} node '{id}' with {len(properties) - 1} properties"
        return f"ERROR: Node not found: {nodeType}('{id}')"

    @server.tool()
    def graph_bulk_import(nodes: list[dict] | None = None, edges: list[dict] | None = None, phase: str = "unknown") -> str:
        """Import multiple nodes and edges at once. Use after completing a phase to populate the graph."""
        nodes = nodes or []
        edges = edges or []
        nodes_created = 0
        edges_created = 0

        for node in nodes:
            r = graph_add_node(
                nodeType=node["nodeType"],
                id=node["id"],
                properties=node.get("properties", {}),
                confidence=node.get("confidence", 0.8),
                createdBy=node.get("createdBy", phase),
            )
            if r.startswith("OK"):
                nodes_created += 1

        for edge in edges:
            r = graph_add_edge(
                edgeType=edge["edgeType"],
                sourceId=edge["sourceId"],
                sourceType=edge["sourceType"],
                targetId=edge["targetId"],
                targetType=edge["targetType"],
                properties=edge.get("properties"),
            )
            if r.startswith("OK"):
                edges_created += 1

        return f"Bulk import complete ({phase}): {nodes_created} nodes, {edges_created} edges created"
