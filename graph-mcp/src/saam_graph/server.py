"""SAAM Knowledge Graph MCP Server.

Provides tools for graph mutation, querying, reconciliation, inference,
and agent context construction — all backed by Neo4j.
"""

import asyncio
import logging
import sys

from mcp.server.fastmcp import FastMCP

from saam_graph.db import GraphDB
from saam_graph.tools.mutations import register_mutation_tools
from saam_graph.tools.queries import register_query_tools
from saam_graph.tools.reconciliation import register_reconciliation_tools
from saam_graph.tools.inference import register_inference_tools
from saam_graph.tools.context import register_context_tools

logger = logging.getLogger("saam-graph")


def create_server() -> FastMCP:
    """Create and configure the MCP server with all tool registrations."""
    server = FastMCP("saam-graph")

    # Register all tool groups
    register_mutation_tools(server)
    register_query_tools(server)
    register_reconciliation_tools(server)
    register_inference_tools(server)
    register_context_tools(server)

    return server


def main():
    """Entry point for the saam-graph command."""
    logging.basicConfig(level=logging.INFO, format="%(name)s | %(levelname)s | %(message)s")

    # Verify Neo4j connectivity on startup
    try:
        GraphDB.get_driver()
        logger.info("Connected to Neo4j")
    except Exception as e:
        logger.error("Failed to connect to Neo4j: %s", e)
        logger.error("Is the Neo4j container running? Try: cd graph-mcp && podman compose up -d")
        sys.exit(1)

    server = create_server()
    try:
        server.run()
    finally:
        GraphDB.close()


if __name__ == "__main__":
    main()
