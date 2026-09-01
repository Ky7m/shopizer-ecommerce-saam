-- SAAM Telemetry — DuckDB Schema
-- Run: duckdb data/saam_telemetry.duckdb < ingest/schema.sql
-- Or: called automatically by import_telemetry.py on first run

-- =============================================================================
-- ENGAGEMENTS (one row per engagement)
-- =============================================================================

CREATE TABLE IF NOT EXISTS engagements (
    engagement_id       VARCHAR PRIMARY KEY,
    industry            VARCHAR,
    legacy_stack        VARCHAR[],
    target_stack        VARCHAR[],
    total_services      INTEGER,
    total_br_count      INTEGER,
    analysis_mode       VARCHAR,        -- cast | direct | hybrid
    start_date          DATE,
    team_size           INTEGER,
    imported_at         TIMESTAMP DEFAULT current_timestamp
);

-- =============================================================================
-- PHASE METRICS (one row per phase per engagement)
-- =============================================================================

CREATE TABLE IF NOT EXISTS phase_metrics (
    engagement_id       VARCHAR NOT NULL,
    phase               VARCHAR NOT NULL,       -- P0, P1, P2, P3, P4, P4A, P4B, P4C, P5, P6
    started_at          TIMESTAMP,
    completed_at        TIMESTAMP,
    duration_hours      DECIMAL,
    credits_used        DECIMAL,               -- coding agent credits / API tokens consumed for this phase
    actor               VARCHAR,               -- agent | human | mixed
    -- Phase-specific metrics stored as JSON for flexibility
    metrics             JSON,
    PRIMARY KEY (engagement_id, phase)
);

-- =============================================================================
-- SOURCE VECTORS (Phase 1 — aggregate source complexity data)
-- =============================================================================

CREATE TABLE IF NOT EXISTS source_vectors (
    engagement_id       VARCHAR NOT NULL,
    total_components    INTEGER,
    -- Aggregates
    agg_control_flow    INTEGER,
    agg_data_flow       INTEGER,
    agg_constants       INTEGER,
    agg_state_transitions INTEGER,
    agg_outcomes        INTEGER,
    agg_data_writes     INTEGER,
    agg_integrations    INTEGER,
    agg_error_paths     INTEGER,
    -- Per-component distribution stats (JSON: {min, max, median, p90, mean})
    stats_control_flow  JSON,
    stats_data_flow     JSON,
    stats_constants     JSON,
    stats_state_transitions JSON,
    stats_outcomes      JSON,
    stats_data_writes   JSON,
    stats_integrations  JSON,
    stats_error_paths   JSON,
    -- Complexity buckets
    simple_lt_10        INTEGER,
    medium_10_to_30     INTEGER,
    complex_30_to_60    INTEGER,
    very_complex_gt_60  INTEGER,
    PRIMARY KEY (engagement_id)
);

-- =============================================================================
-- COMPLEXITY FLAGS (Phase 4 — per-dimension flag rates)
-- =============================================================================

CREATE TABLE IF NOT EXISTS complexity_flags (
    engagement_id       VARCHAR NOT NULL,
    dimension           VARCHAR NOT NULL,       -- control_flow, data_flow, constants, etc.
    flagged_count       INTEGER DEFAULT 0,
    flagged_alone_count INTEGER DEFAULT 0,      -- only for control_flow
    resolved_count      INTEGER DEFAULT 0,
    true_positive_count INTEGER DEFAULT 0,
    PRIMARY KEY (engagement_id, dimension)
);

-- =============================================================================
-- BA REVIEW METRICS (Phase 4a)
-- =============================================================================

CREATE TABLE IF NOT EXISTS ba_review_metrics (
    engagement_id       VARCHAR NOT NULL,
    mode                VARCHAR,               -- full_workshop | agent_defaults
    total_br_reviewed   INTEGER,
    br_approved         INTEGER,
    br_modified         INTEGER,
    br_dropped          INTEGER,
    br_added            INTEGER,
    br_reclassified     INTEGER,
    br_deferred         INTEGER,
    critical_br_count   INTEGER,
    avg_review_minutes  DECIMAL,
    disputes            INTEGER,
    -- Retroactive corrections to Phase 4 flags
    false_flags_dismissed  INTEGER DEFAULT 0,
    true_gaps_confirmed    INTEGER DEFAULT 0,
    new_rules_from_flags   INTEGER DEFAULT 0,
    PRIMARY KEY (engagement_id)
);

-- =============================================================================
-- AUTOMATIBILITY SCORES (Phase 4b — per-service)
-- =============================================================================

CREATE TABLE IF NOT EXISTS automatibility_scores (
    engagement_id       VARCHAR NOT NULL,
    service_id          VARCHAR NOT NULL,       -- anonymized: SVC-001, SVC-002
    service_domain      VARCHAR,
    composite_score     DECIMAL,
    statement_clarity   DECIMAL,
    algorithm_completeness DECIMAL,
    integration_definition DECIMAL,
    data_model_readiness   DECIMAL,
    edge_case_coverage     DECIMAL,
    implementation_type    VARCHAR,             -- A | B | C
    PRIMARY KEY (engagement_id, service_id)
);

-- =============================================================================
-- SERVICE METRICS (Phase 5 — per-service implementation outcomes)
-- =============================================================================

CREATE TABLE IF NOT EXISTS service_metrics (
    engagement_id       VARCHAR NOT NULL,
    service_id          VARCHAR NOT NULL,
    service_domain      VARCHAR,
    implementation_type VARCHAR,               -- A | B | C
    automatibility_score DECIMAL,
    -- Timing
    started_at          TIMESTAMP,
    first_compile_at    TIMESTAMP,
    first_test_run_at   TIMESTAMP,
    all_tests_passing_at TIMESTAMP,
    completed_at        TIMESTAMP,
    total_duration_hours DECIMAL,
    -- Execution
    first_pass_compile  BOOLEAN,
    first_pass_test_rate DECIMAL,
    remediation_cycles  INTEGER,
    human_interventions INTEGER,
    total_generated_loc INTEGER,
    test_count          INTEGER,
    -- BR coverage
    br_in_scope         INTEGER,
    br_validated        INTEGER,
    br_stuck_claims     INTEGER,
    br_required_remediation INTEGER,
    -- Complexity
    source_complexity_avg DECIMAL,
    spec_complexity_avg   DECIMAL,
    complexity_ratio      DECIMAL,
    condensation_flags    INTEGER,
    condensation_true_pos INTEGER,
    -- Confidence at completion
    confidence_overall    DECIMAL,
    confidence_provenance DECIMAL,
    confidence_implementation DECIMAL,
    confidence_test_quality   DECIMAL,
    -- Deviations
    deviation_count       INTEGER,
    deviations_auto       INTEGER,
    deviations_human      INTEGER,
    dev_code_count        INTEGER,
    dev_test_count        INTEGER,
    spec_drift_count      INTEGER,
    -- Prediction tracking
    duration_exceeded_prediction BOOLEAN DEFAULT false,
    credits_used        DECIMAL,               -- Coding agent credits / API tokens for this service
    PRIMARY KEY (engagement_id, service_id)
);

-- =============================================================================
-- EVOLUTION CYCLES (Phase 6 — per-cycle metrics)
-- =============================================================================

CREATE TABLE IF NOT EXISTS evolution_cycles (
    engagement_id       VARCHAR NOT NULL,
    cycle_id            VARCHAR NOT NULL,
    started_at          TIMESTAMP,
    completed_at        TIMESTAMP,
    duration_hours      DECIMAL,
    trigger_type        VARCHAR,               -- deviation | bug | feature
    items_processed     INTEGER,
    br_affected         INTEGER,
    specs_updated       INTEGER,
    tests_updated       INTEGER,
    code_changes        INTEGER,
    new_deviations      INTEGER,
    confidence_before   DECIMAL,
    confidence_after    DECIMAL,
    PRIMARY KEY (engagement_id, cycle_id)
);

-- =============================================================================
-- HUMAN INTERVENTIONS (cross-phase corrections and redirections)
-- =============================================================================

CREATE TABLE IF NOT EXISTS interventions (
    engagement_id       VARCHAR NOT NULL,
    phase               VARCHAR NOT NULL,
    service             VARCHAR,
    type                VARCHAR NOT NULL,       -- correction | redirect | redo | clarification | bug_report
    category            VARCHAR NOT NULL,       -- naming | format | extraction_depth | missed_files | wrong_output | steering_violation | context_pressure | other
    overhead_minutes    INTEGER,
    timestamp           TIMESTAMP
);

-- =============================================================================
-- VIEWS (pre-built analytical queries)
-- =============================================================================

-- Cross-engagement service outcomes joined with automatibility
CREATE OR REPLACE VIEW service_outcomes AS
SELECT
    sm.*,
    ascore.statement_clarity,
    ascore.algorithm_completeness,
    ascore.integration_definition,
    ascore.data_model_readiness,
    ascore.edge_case_coverage,
    e.industry,
    e.legacy_stack,
    e.analysis_mode
FROM service_metrics sm
LEFT JOIN automatibility_scores ascore
    ON sm.engagement_id = ascore.engagement_id
    AND sm.service_id = ascore.service_id
LEFT JOIN engagements e
    ON sm.engagement_id = e.engagement_id;

-- Phase duration summary across engagements
CREATE OR REPLACE VIEW phase_durations AS
SELECT
    phase,
    count(*) AS sample_count,
    avg(duration_hours) AS avg_hours,
    median(duration_hours) AS median_hours,
    min(duration_hours) AS min_hours,
    max(duration_hours) AS max_hours,
    stddev(duration_hours) AS stddev_hours
FROM phase_metrics
WHERE duration_hours IS NOT NULL
GROUP BY phase
ORDER BY phase;

-- Intervention summary by category (steering improvement signal)
CREATE OR REPLACE VIEW intervention_summary AS
SELECT
    category,
    count(*) AS total_occurrences,
    count(DISTINCT engagement_id) AS engagements_affected,
    avg(overhead_minutes) AS avg_overhead_minutes,
    sum(overhead_minutes) AS total_overhead_minutes
FROM interventions
GROUP BY category
ORDER BY total_occurrences DESC;

-- Stack-based cohort analysis (calibration by source/target stack)
-- BR count pulled from latest phase telemetry (not engagement.yaml initial count)
CREATE OR REPLACE VIEW stack_cohorts AS
SELECT
    e.legacy_stack[1] AS primary_source_stack,
    e.target_stack[1] AS primary_target_stack,
    count(DISTINCT e.engagement_id) AS engagements,
    avg(pm_p1.duration_hours) AS avg_phase1_hours,
    avg(pm_p4.duration_hours) AS avg_phase4_hours,
    -- Use latest BR count: prefer P4b enriched > P4a > P4 > engagement
    avg(COALESCE(
        CAST(json_extract_string(pm_p4b.metrics, '$.total_rules_final') AS INTEGER),
        CAST(json_extract_string(pm_p4a.metrics, '$.post_phase_rules') AS INTEGER),
        CAST(json_extract_string(pm_p4.metrics, '$.total_rules_extracted') AS INTEGER),
        CAST(json_extract_string(pm_p4.metrics, '$.total_br_extracted') AS INTEGER),
        e.total_br_count
    )) AS avg_br_count_latest,
    -- Greenfield dimension
    avg(COALESCE(CAST(json_extract_string(pm_p4b.metrics, '$.greenfield_services_added') AS INTEGER), 0)) AS avg_greenfield_services,
    -- Phase 4a mode
    mode(json_extract_string(pm_p4b.metrics, '$.phase4a_mode')) AS common_phase4a_mode
FROM engagements e
LEFT JOIN phase_metrics pm_p1 ON e.engagement_id = pm_p1.engagement_id AND pm_p1.phase = 'P1'
LEFT JOIN phase_metrics pm_p4 ON e.engagement_id = pm_p4.engagement_id AND pm_p4.phase = 'P4'
LEFT JOIN phase_metrics pm_p4a ON e.engagement_id = pm_p4a.engagement_id AND pm_p4a.phase IN ('P4a', 'P4A')
LEFT JOIN phase_metrics pm_p4b ON e.engagement_id = pm_p4b.engagement_id AND pm_p4b.phase IN ('4b', 'P4B', 'P4b')
GROUP BY 1, 2;

-- Rule progression analysis (P1 → P4 → P4a → P4b growth tracking)
CREATE OR REPLACE VIEW rule_progression AS
SELECT
    e.engagement_id,
    e.legacy_stack[1] AS source_stack,
    e.target_stack[1] AS target_stack,
    COALESCE(
        CAST(json_extract_string(pm_p1.metrics, '$.br_candidates_extracted') AS INTEGER),
        e.total_br_count
    ) AS p1_rules,
    CAST(json_extract_string(pm_p4.metrics, '$.total_rules_extracted') AS INTEGER) AS p4_rules,
    CAST(json_extract_string(pm_p4a.metrics, '$.post_phase_rules') AS INTEGER) AS p4a_rules,
    CAST(json_extract_string(pm_p4b.metrics, '$.total_rules_final') AS INTEGER) AS p4b_rules,
    -- Growth ratios (P1 anchor from Phase 1 metrics, not engagement.total_br_count)
    CASE WHEN COALESCE(CAST(json_extract_string(pm_p1.metrics, '$.br_candidates_extracted') AS INTEGER), e.total_br_count) > 0
        THEN round(CAST(json_extract_string(pm_p4.metrics, '$.total_rules_extracted') AS DECIMAL) /
             COALESCE(CAST(json_extract_string(pm_p1.metrics, '$.br_candidates_extracted') AS INTEGER), e.total_br_count), 2)
        ELSE NULL END AS growth_p1_to_p4,
    CASE WHEN CAST(json_extract_string(pm_p4.metrics, '$.total_rules_extracted') AS INTEGER) > 0
        THEN round(CAST(json_extract_string(pm_p4a.metrics, '$.post_phase_rules') AS DECIMAL) / CAST(json_extract_string(pm_p4.metrics, '$.total_rules_extracted') AS DECIMAL), 2)
        ELSE NULL END AS growth_p4_to_p4a,
    -- Greenfield contribution
    COALESCE(CAST(json_extract_string(pm_p4b.metrics, '$.greenfield_services_added') AS INTEGER), 0) AS greenfield_services,
    -- Phase 4a resolution mode
    json_extract_string(pm_p4b.metrics, '$.phase4a_mode') AS phase4a_mode,
    -- Automatibility outcome
    CAST(json_extract_string(pm_p4b.metrics, '$.avg_automatibility_overall') AS DECIMAL) AS automatibility_final
FROM engagements e
LEFT JOIN phase_metrics pm_p1 ON e.engagement_id = pm_p1.engagement_id AND pm_p1.phase = 'P1'
LEFT JOIN phase_metrics pm_p4 ON e.engagement_id = pm_p4.engagement_id AND pm_p4.phase = 'P4'
LEFT JOIN phase_metrics pm_p4a ON e.engagement_id = pm_p4a.engagement_id AND pm_p4a.phase IN ('P4a', 'P4A')
LEFT JOIN phase_metrics pm_p4b ON e.engagement_id = pm_p4b.engagement_id AND pm_p4b.phase IN ('4b', 'P4B', 'P4b');


-- Credits cost prediction view (LOC-anchored, stack-specific, mode-aware)
-- Enables: "Given LOC + source_stack + mode + target_stack + impl_type → predicted credits"
CREATE OR REPLACE VIEW credits_by_stack AS
SELECT
    e.legacy_stack[1] AS source_stack,
    e.target_stack[1] AS target_stack,
    e.analysis_mode AS extraction_mode,        -- direct | cast | hybrid
    e.total_br_count AS initial_br_count,
    -- Total extraction credits (P1 + P4 + P4a + P4b)
    (COALESCE(pm_p1.credits_used, 0) +
     COALESCE(pm_p4.credits_used, 0) +
     COALESCE(pm_p4a.credits_used, 0) +
     COALESCE(pm_p4b.credits_used, 0)) AS extraction_credits_total,
    -- Credits per 1K source LOC (extraction)
    CASE WHEN CAST(json_extract_string(pm_p1.metrics, '$.total_loc') AS INTEGER) > 0
        THEN (COALESCE(pm_p1.credits_used, 0) + COALESCE(pm_p4.credits_used, 0)) * 1000.0 /
             CAST(json_extract_string(pm_p1.metrics, '$.total_loc') AS INTEGER)
        ELSE NULL END AS extraction_credits_per_1k_loc,
    -- Phase 5 generation credits (Type A only — Type B/C are ATX compute)
    pm_p5.credits_used AS generation_credits_total,
    -- LOC from Phase 1
    CAST(json_extract_string(pm_p1.metrics, '$.total_loc') AS INTEGER) AS total_source_loc
FROM engagements e
LEFT JOIN phase_metrics pm_p1 ON e.engagement_id = pm_p1.engagement_id AND pm_p1.phase = 'P1'
LEFT JOIN phase_metrics pm_p4 ON e.engagement_id = pm_p4.engagement_id AND pm_p4.phase = 'P4'
LEFT JOIN phase_metrics pm_p4a ON e.engagement_id = pm_p4a.engagement_id AND pm_p4a.phase IN ('P4a', 'P4A')
LEFT JOIN phase_metrics pm_p4b ON e.engagement_id = pm_p4b.engagement_id AND pm_p4b.phase IN ('4b', 'P4B', 'P4b')
LEFT JOIN phase_metrics pm_p5 ON e.engagement_id = pm_p5.engagement_id AND pm_p5.phase = 'P5';

-- Per-service generation credits by implementation type and target stack
CREATE OR REPLACE VIEW generation_credits_by_type AS
SELECT
    e.target_stack[1] AS target_stack,
    sm.implementation_type,
    count(*) AS services,
    avg(sm.credits_used) AS avg_credits_per_service,
    sum(sm.credits_used) AS total_credits,
    avg(sm.br_in_scope) AS avg_rules_per_service
FROM service_metrics sm
JOIN engagements e ON sm.engagement_id = e.engagement_id
WHERE sm.credits_used IS NOT NULL
GROUP BY 1, 2;
