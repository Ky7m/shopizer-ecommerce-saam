"""Neo4j connection manager for the SAAM Knowledge Graph."""

import os
from contextlib import contextmanager
from typing import Any

from neo4j import GraphDatabase, Driver, Session


NEO4J_URI = os.environ.get("NEO4J_URI", "bolt://localhost:7687")
NEO4J_USER = os.environ.get("NEO4J_USER", "neo4j")
NEO4J_PASSWORD = os.environ.get("NEO4J_PASSWORD", "saamgraph")
NEO4J_DATABASE = os.environ.get("NEO4J_DATABASE", "neo4j")


def _load_env_file():
    """Load .env from the graph-mcp directory if it exists (has dynamic port)."""
    env_path = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "..", ".env")
    if os.path.exists(env_path):
        with open(env_path) as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith("#") and "=" in line:
                    key, value = line.split("=", 1)
                    # .env is the AUTHORITATIVE per-engagement config (holds the actual
                    # mapped port). Override any stale shell value — do NOT use setdefault,
                    # which would let a leftover NEO4J_BOLT_PORT/NEO4J_URI from a previous
                    # engagement silently win and point at the wrong port.
                    os.environ[key.strip()] = value.strip()


# Load .env on import to pick up dynamic port
_load_env_file()

# Derive from NEO4J_BOLT_PORT (set by .env). An explicit NEO4J_URI in .env still wins.
NEO4J_URI = os.environ.get("NEO4J_URI") or f"bolt://localhost:{os.environ.get('NEO4J_BOLT_PORT', '7687')}"


class GraphDB:
    """Manages the Neo4j driver lifecycle and provides query execution."""

    _driver: Driver | None = None

    @classmethod
    def get_driver(cls) -> Driver:
        """Get or create the Neo4j driver (singleton)."""
        if cls._driver is None:
            cls._driver = GraphDatabase.driver(
                NEO4J_URI,
                auth=(NEO4J_USER, NEO4J_PASSWORD),
            )
            cls._driver.verify_connectivity()
        return cls._driver

    @classmethod
    def close(cls) -> None:
        """Close the driver connection."""
        if cls._driver is not None:
            cls._driver.close()
            cls._driver = None

    @classmethod
    @contextmanager
    def session(cls):
        """Context manager for a Neo4j session."""
        driver = cls.get_driver()
        session = driver.session(database=NEO4J_DATABASE)
        try:
            yield session
        finally:
            session.close()

    @classmethod
    def execute_read(cls, query: str, params: dict[str, Any] | None = None) -> list[dict]:
        """Execute a read query and return results as list of dicts."""
        with cls.session() as session:
            result = session.run(query, params or {})
            return [record.data() for record in result]

    @classmethod
    def execute_write(cls, query: str, params: dict[str, Any] | None = None) -> list[dict]:
        """Execute a write query and return results as list of dicts."""
        with cls.session() as session:
            result = session.run(query, params or {})
            return [record.data() for record in result]

    @classmethod
    def execute_write_single(cls, query: str, params: dict[str, Any] | None = None) -> dict | None:
        """Execute a write query and return a single result dict (or None)."""
        results = cls.execute_write(query, params)
        return results[0] if results else None
