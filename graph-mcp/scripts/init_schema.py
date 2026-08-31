"""
Initialize Neo4j schema for SAAM Knowledge Graph.

Creates constraints, indexes, and sets up the graph structure.
Run once after Neo4j container starts.

Usage:
    python scripts/init_schema.py
    # or with custom connection:
    NEO4J_URI=bolt://localhost:7687 NEO4J_PASSWORD=saamgraph python scripts/init_schema.py
"""

import os
import sys

from neo4j import GraphDatabase

# Load .env if it exists (has dynamic port from ensure_neo4j.sh)
ENV_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env")
if os.path.exists(ENV_PATH):
    with open(ENV_PATH) as f:
        for line in f:
            line = line.strip()
            if line and not line.startswith("#") and "=" in line:
                key, value = line.split("=", 1)
                # .env is AUTHORITATIVE — override stale shell values (not setdefault)
                os.environ[key.strip()] = value.strip()

NEO4J_URI = os.environ.get("NEO4J_URI") or f"bolt://localhost:{os.environ.get('NEO4J_BOLT_PORT', '7687')}"
NEO4J_HTTP_PORT = os.environ.get("NEO4J_HTTP_PORT", "7474")
NEO4J_USER = os.environ.get("NEO4J_USER", "neo4j")
NEO4J_PASSWORD = os.environ.get("NEO4J_PASSWORD", "saamgraph")

# =============================================================================
# CONSTRAINTS — enforce data integrity at the database level
# =============================================================================

CONSTRAINTS = [
    # Node uniqueness constraints (also create indexes automatically)
    "CREATE CONSTRAINT source_component_cast_id IF NOT EXISTS FOR (n:SourceComponent) REQUIRE n.castId IS UNIQUE",
    "CREATE CONSTRAINT source_table_name IF NOT EXISTS FOR (n:SourceTable) REQUIRE n.name IS UNIQUE",
    "CREATE CONSTRAINT business_rule_br_id IF NOT EXISTS FOR (n:BusinessRule) REQUIRE n.brId IS UNIQUE",
    "CREATE CONSTRAINT service_id IF NOT EXISTS FOR (n:Service) REQUIRE n.serviceId IS UNIQUE",
    "CREATE CONSTRAINT endpoint_path_method IF NOT EXISTS FOR (n:Endpoint) REQUIRE (n.path, n.method, n.service) IS UNIQUE",
    "CREATE CONSTRAINT deviation_id IF NOT EXISTS FOR (n:Deviation) REQUIRE n.deviationId IS UNIQUE",
    "CREATE CONSTRAINT decision_br_id IF NOT EXISTS FOR (n:Decision) REQUIRE n.brId IS UNIQUE",
    # Layer A: invariant / state / lifecycle
    "CREATE CONSTRAINT entity_state_id IF NOT EXISTS FOR (n:EntityState) REQUIRE n.stateId IS UNIQUE",
    "CREATE CONSTRAINT invariant_id IF NOT EXISTS FOR (n:Invariant) REQUIRE n.invariantId IS UNIQUE",
    # Layer B: extensibility engine
    "CREATE CONSTRAINT extension_point_id IF NOT EXISTS FOR (n:ExtensionPoint) REQUIRE n.extPointId IS UNIQUE",
    # Layer C: DB-tier logic + placement
    "CREATE CONSTRAINT db_object_id IF NOT EXISTS FOR (n:DbObject) REQUIRE n.dbObjectId IS UNIQUE",
    "CREATE CONSTRAINT placement_decision_id IF NOT EXISTS FOR (n:PlacementDecision) REQUIRE n.placementId IS UNIQUE",
    # Node existence constraints (required properties)
    "CREATE CONSTRAINT business_rule_statement IF NOT EXISTS FOR (n:BusinessRule) REQUIRE n.statement IS NOT NULL",
    "CREATE CONSTRAINT business_rule_intent IF NOT EXISTS FOR (n:BusinessRule) REQUIRE n.intent IS NOT NULL",
    "CREATE CONSTRAINT business_rule_confidence IF NOT EXISTS FOR (n:BusinessRule) REQUIRE n.confidence IS NOT NULL",
    "CREATE CONSTRAINT service_name IF NOT EXISTS FOR (n:Service) REQUIRE n.name IS NOT NULL",
    "CREATE CONSTRAINT service_port IF NOT EXISTS FOR (n:Service) REQUIRE n.port IS NOT NULL",
    "CREATE CONSTRAINT endpoint_path IF NOT EXISTS FOR (n:Endpoint) REQUIRE n.path IS NOT NULL",
    "CREATE CONSTRAINT endpoint_method IF NOT EXISTS FOR (n:Endpoint) REQUIRE n.method IS NOT NULL",
    "CREATE CONSTRAINT deviation_type IF NOT EXISTS FOR (n:Deviation) REQUIRE n.type IS NOT NULL",
    "CREATE CONSTRAINT deviation_status IF NOT EXISTS FOR (n:Deviation) REQUIRE n.status IS NOT NULL",
]

# =============================================================================
# INDEXES — optimize query performance for common access patterns
# =============================================================================

INDEXES = [
    # Composite indexes for common lookups
    "CREATE INDEX business_rule_service IF NOT EXISTS FOR (n:BusinessRule) ON (n.service)",
    "CREATE INDEX business_rule_intent IF NOT EXISTS FOR (n:BusinessRule) ON (n.intent)",
    "CREATE INDEX business_rule_confidence IF NOT EXISTS FOR (n:BusinessRule) ON (n.confidence)",
    "CREATE INDEX business_rule_phase IF NOT EXISTS FOR (n:BusinessRule) ON (n.phase)",
    "CREATE INDEX source_component_type IF NOT EXISTS FOR (n:SourceComponent) ON (n.type)",
    "CREATE INDEX source_component_complexity IF NOT EXISTS FOR (n:SourceComponent) ON (n.complexity)",
    "CREATE INDEX source_component_module IF NOT EXISTS FOR (n:SourceComponent) ON (n.module)",
    "CREATE INDEX service_priority IF NOT EXISTS FOR (n:Service) ON (n.priority)",
    "CREATE INDEX table_service IF NOT EXISTS FOR (n:Table) ON (n.service)",
    "CREATE INDEX endpoint_service IF NOT EXISTS FOR (n:Endpoint) ON (n.service)",
    "CREATE INDEX field_schema IF NOT EXISTS FOR (n:Field) ON (n.schema)",
    "CREATE INDEX test_assertion_service IF NOT EXISTS FOR (n:TestAssertion) ON (n.service)",
    "CREATE INDEX test_assertion_status IF NOT EXISTS FOR (n:TestAssertion) ON (n.status)",
    "CREATE INDEX test_assertion_br_id IF NOT EXISTS FOR (n:TestAssertion) ON (n.brId)",
    "CREATE INDEX implementation_service IF NOT EXISTS FOR (n:Implementation) ON (n.service)",
    "CREATE INDEX implementation_reachable IF NOT EXISTS FOR (n:Implementation) ON (n.reachable)",
    "CREATE INDEX business_rule_behavioral IF NOT EXISTS FOR (n:BusinessRule) ON (n.behavioralStatus)",
    "CREATE INDEX business_rule_deadcode IF NOT EXISTS FOR (n:BusinessRule) ON (n.deadCode)",
    "CREATE INDEX deviation_service IF NOT EXISTS FOR (n:Deviation) ON (n.service)",
    "CREATE INDEX deviation_type_idx IF NOT EXISTS FOR (n:Deviation) ON (n.type)",
    "CREATE INDEX deviation_status_idx IF NOT EXISTS FOR (n:Deviation) ON (n.status)",
    "CREATE INDEX decision_classification IF NOT EXISTS FOR (n:Decision) ON (n.classification)",
    "CREATE INDEX decision_weight IF NOT EXISTS FOR (n:Decision) ON (n.weight)",
    # Layer A: invariant / state / lifecycle
    "CREATE INDEX entity_state_service IF NOT EXISTS FOR (n:EntityState) ON (n.service)",
    "CREATE INDEX entity_state_entity IF NOT EXISTS FOR (n:EntityState) ON (n.entity)",
    "CREATE INDEX invariant_service IF NOT EXISTS FOR (n:Invariant) ON (n.service)",
    "CREATE INDEX invariant_tier IF NOT EXISTS FOR (n:Invariant) ON (n.tier)",
    # Layer B: extensibility engine
    "CREATE INDEX extension_point_service IF NOT EXISTS FOR (n:ExtensionPoint) ON (n.service)",
    "CREATE INDEX extension_point_mechanism IF NOT EXISTS FOR (n:ExtensionPoint) ON (n.mechanism)",
    # Layer C: DB-tier logic + placement
    "CREATE INDEX db_object_service IF NOT EXISTS FOR (n:DbObject) ON (n.service)",
    "CREATE INDEX db_object_kind IF NOT EXISTS FOR (n:DbObject) ON (n.kind)",
    "CREATE INDEX implementation_tier IF NOT EXISTS FOR (n:Implementation) ON (n.tier)",
    "CREATE INDEX placement_decision_service IF NOT EXISTS FOR (n:PlacementDecision) ON (n.service)",
    "CREATE INDEX placement_decision_decision IF NOT EXISTS FOR (n:PlacementDecision) ON (n.decision)",
    # Full-text indexes for searching
    "CREATE FULLTEXT INDEX business_rule_text IF NOT EXISTS FOR (n:BusinessRule) ON EACH [n.statement, n.brId]",
    "CREATE FULLTEXT INDEX deviation_text IF NOT EXISTS FOR (n:Deviation) ON EACH [n.description, n.deviationId]",
]

# =============================================================================
# PROVENANCE SETUP — metadata node to track graph state
# =============================================================================

PROVENANCE_SETUP = """
MERGE (meta:GraphMetadata {id: 'saam-graph'})
SET meta.schemaVersion = '1.0',
    meta.createdAt = datetime(),
    meta.lastUpdated = datetime(),
    meta.engagement = 'unknown',
    meta.phaseCompleted = 0
RETURN meta
"""


def init_schema():
    """Connect to Neo4j and apply all constraints and indexes."""
    print(f"Connecting to Neo4j at {NEO4J_URI}...")

    driver = GraphDatabase.driver(NEO4J_URI, auth=(NEO4J_USER, NEO4J_PASSWORD))

    try:
        driver.verify_connectivity()
        print("Connected successfully.")
    except Exception as e:
        print(f"ERROR: Cannot connect to Neo4j: {e}")
        print("Is the Neo4j container running? Try: podman compose up -d (or docker compose up -d)")
        sys.exit(1)

    with driver.session() as session:
        # Apply constraints
        print(f"\nApplying {len(CONSTRAINTS)} constraints...")
        for i, constraint in enumerate(CONSTRAINTS, 1):
            try:
                session.run(constraint)
                print(f"  [{i}/{len(CONSTRAINTS)}] OK")
            except Exception as e:
                # Constraint may already exist — that's fine
                if "already exists" in str(e).lower() or "equivalent" in str(e).lower():
                    print(f"  [{i}/{len(CONSTRAINTS)}] Already exists (skipped)")
                else:
                    print(f"  [{i}/{len(CONSTRAINTS)}] ERROR: {e}")

        # Apply indexes
        print(f"\nApplying {len(INDEXES)} indexes...")
        for i, index in enumerate(INDEXES, 1):
            try:
                session.run(index)
                print(f"  [{i}/{len(INDEXES)}] OK")
            except Exception as e:
                if "already exists" in str(e).lower() or "equivalent" in str(e).lower():
                    print(f"  [{i}/{len(INDEXES)}] Already exists (skipped)")
                else:
                    print(f"  [{i}/{len(INDEXES)}] ERROR: {e}")

        # Create metadata node
        print("\nCreating graph metadata node...")
        result = session.run(PROVENANCE_SETUP)
        record = result.single()
        print(f"  Graph metadata: schema v{record['meta']['schemaVersion']}")

    driver.close()
    print("\nSchema initialization complete.")
    print(f"  Neo4j Browser: http://localhost:{NEO4J_HTTP_PORT}")
    print(f"  Bolt endpoint: {NEO4J_URI}")
    print(f"  Credentials: {NEO4J_USER} / {'*' * len(NEO4J_PASSWORD)}")


if __name__ == "__main__":
    init_schema()
