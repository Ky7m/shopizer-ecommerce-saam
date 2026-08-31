// =============================================================================
// SAAM Knowledge Graph — Neo4j Schema Initialization
// Run in Neo4j Browser or via cypher-shell for manual setup
// Equivalent to scripts/init_schema.py but as raw Cypher
// =============================================================================

// --- UNIQUENESS CONSTRAINTS (also create indexes) ---

CREATE CONSTRAINT source_component_cast_id IF NOT EXISTS FOR (n:SourceComponent) REQUIRE n.castId IS UNIQUE;
CREATE CONSTRAINT source_table_name IF NOT EXISTS FOR (n:SourceTable) REQUIRE n.name IS UNIQUE;
CREATE CONSTRAINT business_rule_br_id IF NOT EXISTS FOR (n:BusinessRule) REQUIRE n.brId IS UNIQUE;
CREATE CONSTRAINT service_id IF NOT EXISTS FOR (n:Service) REQUIRE n.serviceId IS UNIQUE;
CREATE CONSTRAINT endpoint_path_method IF NOT EXISTS FOR (n:Endpoint) REQUIRE (n.path, n.method, n.service) IS UNIQUE;
CREATE CONSTRAINT deviation_id IF NOT EXISTS FOR (n:Deviation) REQUIRE n.deviationId IS UNIQUE;
CREATE CONSTRAINT decision_br_id IF NOT EXISTS FOR (n:Decision) REQUIRE n.brId IS UNIQUE;
CREATE CONSTRAINT entity_state_id IF NOT EXISTS FOR (n:EntityState) REQUIRE n.stateId IS UNIQUE;
CREATE CONSTRAINT invariant_id IF NOT EXISTS FOR (n:Invariant) REQUIRE n.invariantId IS UNIQUE;
CREATE CONSTRAINT extension_point_id IF NOT EXISTS FOR (n:ExtensionPoint) REQUIRE n.extPointId IS UNIQUE;
CREATE CONSTRAINT db_object_id IF NOT EXISTS FOR (n:DbObject) REQUIRE n.dbObjectId IS UNIQUE;
CREATE CONSTRAINT placement_decision_id IF NOT EXISTS FOR (n:PlacementDecision) REQUIRE n.placementId IS UNIQUE;

// --- EXISTENCE CONSTRAINTS (required properties) ---

CREATE CONSTRAINT business_rule_statement IF NOT EXISTS FOR (n:BusinessRule) REQUIRE n.statement IS NOT NULL;
CREATE CONSTRAINT business_rule_intent IF NOT EXISTS FOR (n:BusinessRule) REQUIRE n.intent IS NOT NULL;
CREATE CONSTRAINT business_rule_confidence IF NOT EXISTS FOR (n:BusinessRule) REQUIRE n.confidence IS NOT NULL;
CREATE CONSTRAINT service_name IF NOT EXISTS FOR (n:Service) REQUIRE n.name IS NOT NULL;
CREATE CONSTRAINT service_port IF NOT EXISTS FOR (n:Service) REQUIRE n.port IS NOT NULL;
CREATE CONSTRAINT endpoint_path IF NOT EXISTS FOR (n:Endpoint) REQUIRE n.path IS NOT NULL;
CREATE CONSTRAINT endpoint_method IF NOT EXISTS FOR (n:Endpoint) REQUIRE n.method IS NOT NULL;
CREATE CONSTRAINT deviation_type IF NOT EXISTS FOR (n:Deviation) REQUIRE n.type IS NOT NULL;
CREATE CONSTRAINT deviation_status IF NOT EXISTS FOR (n:Deviation) REQUIRE n.status IS NOT NULL;

// --- INDEXES (query performance) ---

CREATE INDEX business_rule_service IF NOT EXISTS FOR (n:BusinessRule) ON (n.service);
CREATE INDEX business_rule_intent IF NOT EXISTS FOR (n:BusinessRule) ON (n.intent);
CREATE INDEX business_rule_confidence IF NOT EXISTS FOR (n:BusinessRule) ON (n.confidence);
CREATE INDEX business_rule_phase IF NOT EXISTS FOR (n:BusinessRule) ON (n.phase);
CREATE INDEX source_component_type IF NOT EXISTS FOR (n:SourceComponent) ON (n.type);
CREATE INDEX source_component_complexity IF NOT EXISTS FOR (n:SourceComponent) ON (n.complexity);
CREATE INDEX source_component_module IF NOT EXISTS FOR (n:SourceComponent) ON (n.module);
CREATE INDEX service_priority IF NOT EXISTS FOR (n:Service) ON (n.priority);
CREATE INDEX table_service IF NOT EXISTS FOR (n:Table) ON (n.service);
CREATE INDEX endpoint_service IF NOT EXISTS FOR (n:Endpoint) ON (n.service);
CREATE INDEX field_schema IF NOT EXISTS FOR (n:Field) ON (n.schema);
CREATE INDEX test_assertion_service IF NOT EXISTS FOR (n:TestAssertion) ON (n.service);
CREATE INDEX test_assertion_status IF NOT EXISTS FOR (n:TestAssertion) ON (n.status);
CREATE INDEX test_assertion_br_id IF NOT EXISTS FOR (n:TestAssertion) ON (n.brId);
CREATE INDEX implementation_service IF NOT EXISTS FOR (n:Implementation) ON (n.service);
CREATE INDEX deviation_service IF NOT EXISTS FOR (n:Deviation) ON (n.service);
CREATE INDEX deviation_type_idx IF NOT EXISTS FOR (n:Deviation) ON (n.type);
CREATE INDEX deviation_status_idx IF NOT EXISTS FOR (n:Deviation) ON (n.status);
CREATE INDEX decision_classification IF NOT EXISTS FOR (n:Decision) ON (n.classification);
CREATE INDEX decision_weight IF NOT EXISTS FOR (n:Decision) ON (n.weight);
CREATE INDEX entity_state_service IF NOT EXISTS FOR (n:EntityState) ON (n.service);
CREATE INDEX entity_state_entity IF NOT EXISTS FOR (n:EntityState) ON (n.entity);
CREATE INDEX invariant_service IF NOT EXISTS FOR (n:Invariant) ON (n.service);
CREATE INDEX invariant_tier IF NOT EXISTS FOR (n:Invariant) ON (n.tier);
CREATE INDEX extension_point_service IF NOT EXISTS FOR (n:ExtensionPoint) ON (n.service);
CREATE INDEX extension_point_mechanism IF NOT EXISTS FOR (n:ExtensionPoint) ON (n.mechanism);
CREATE INDEX db_object_service IF NOT EXISTS FOR (n:DbObject) ON (n.service);
CREATE INDEX db_object_kind IF NOT EXISTS FOR (n:DbObject) ON (n.kind);
CREATE INDEX implementation_tier IF NOT EXISTS FOR (n:Implementation) ON (n.tier);
CREATE INDEX placement_decision_service IF NOT EXISTS FOR (n:PlacementDecision) ON (n.service);
CREATE INDEX placement_decision_decision IF NOT EXISTS FOR (n:PlacementDecision) ON (n.decision);

// --- FULL-TEXT INDEXES ---

CREATE FULLTEXT INDEX business_rule_text IF NOT EXISTS FOR (n:BusinessRule) ON EACH [n.statement, n.brId];
CREATE FULLTEXT INDEX deviation_text IF NOT EXISTS FOR (n:Deviation) ON EACH [n.description, n.deviationId];

// --- METADATA ---

MERGE (meta:GraphMetadata {id: 'saam-graph'})
SET meta.schemaVersion = '1.0',
    meta.createdAt = datetime(),
    meta.lastUpdated = datetime(),
    meta.engagement = 'unknown',
    meta.phaseCompleted = 0
RETURN meta;
