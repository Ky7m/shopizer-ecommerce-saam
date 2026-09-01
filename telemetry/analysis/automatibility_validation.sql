-- SAAM Telemetry Analysis: Automatibility Validation
-- Question: Does the automatibility score predict implementation success?
-- Run: duckdb data/saam_telemetry.duckdb < analysis/automatibility_validation.sql

-- =============================================================================
-- 1. Overall correlation: AS vs first-pass test rate
-- =============================================================================

SELECT
    '--- AS vs First-Pass Test Rate ---' AS section;

SELECT
    corr(automatibility_score, first_pass_test_rate) AS correlation,
    count(*) AS sample_size
FROM service_metrics
WHERE automatibility_score IS NOT NULL
  AND first_pass_test_rate IS NOT NULL;

-- =============================================================================
-- 2. AS vs remediation cycles
-- =============================================================================

SELECT
    '--- AS vs Remediation Cycles ---' AS section;

SELECT
    corr(automatibility_score, remediation_cycles) AS correlation,
    count(*) AS sample_size
FROM service_metrics
WHERE automatibility_score IS NOT NULL
  AND remediation_cycles IS NOT NULL;

-- =============================================================================
-- 3. AS vs human interventions
-- =============================================================================

SELECT
    '--- AS vs Human Interventions ---' AS section;

SELECT
    corr(automatibility_score, human_interventions) AS correlation,
    count(*) AS sample_size
FROM service_metrics
WHERE automatibility_score IS NOT NULL
  AND human_interventions IS NOT NULL;

-- =============================================================================
-- 4. AS vs total duration
-- =============================================================================

SELECT
    '--- AS vs Duration (hours) ---' AS section;

SELECT
    corr(automatibility_score, total_duration_hours) AS correlation,
    count(*) AS sample_size
FROM service_metrics
WHERE automatibility_score IS NOT NULL
  AND total_duration_hours IS NOT NULL;

-- =============================================================================
-- 5. Bucketed analysis: outcomes by AS range
-- =============================================================================

SELECT
    '--- Outcomes by AS Bucket ---' AS section;

SELECT
    CASE
        WHEN automatibility_score >= 0.85 THEN 'high (>=85%)'
        WHEN automatibility_score >= 0.70 THEN 'medium (70-84%)'
        ELSE 'low (<70%)'
    END AS as_bucket,
    count(*) AS n,
    avg(first_pass_test_rate) AS avg_first_pass,
    avg(remediation_cycles) AS avg_remediation,
    avg(human_interventions) AS avg_human,
    avg(total_duration_hours) AS avg_duration_hours,
    avg(deviation_count) AS avg_deviations
FROM service_metrics
WHERE automatibility_score IS NOT NULL
GROUP BY 1
ORDER BY 1;

-- =============================================================================
-- 6. Implementation type accuracy: did AS threshold predict the right type?
-- =============================================================================

SELECT
    '--- Type Classification Accuracy ---' AS section;

SELECT
    implementation_type,
    count(*) AS n,
    avg(automatibility_score) AS avg_score,
    min(automatibility_score) AS min_score,
    max(automatibility_score) AS max_score,
    avg(first_pass_test_rate) AS avg_first_pass,
    avg(remediation_cycles) AS avg_remediation
FROM service_metrics
WHERE automatibility_score IS NOT NULL
GROUP BY implementation_type
ORDER BY implementation_type;

-- =============================================================================
-- 7. Per-dimension predictive power (requires service_outcomes view)
-- =============================================================================

SELECT
    '--- Per-Dimension Correlations with First-Pass Rate ---' AS section;

SELECT
    corr(statement_clarity, first_pass_test_rate) AS clarity_corr,
    corr(algorithm_completeness, first_pass_test_rate) AS algorithm_corr,
    corr(integration_definition, first_pass_test_rate) AS integration_corr,
    corr(data_model_readiness, first_pass_test_rate) AS data_model_corr,
    corr(edge_case_coverage, first_pass_test_rate) AS edge_case_corr,
    count(*) AS sample_size
FROM service_outcomes
WHERE first_pass_test_rate IS NOT NULL
  AND statement_clarity IS NOT NULL;


-- =============================================================================
-- 8. Context-specific AS thresholds (source_stack × target_stack × br_tier)
-- Determines: at what AS does implementation success drop below 80%?
-- Run after Phase 5 data is available
-- =============================================================================

SELECT
    '--- Context-Specific AS Thresholds ---' AS section;

SELECT
    e.legacy_stack[1] AS source_stack,
    e.target_stack[1] AS target_stack,
    CASE WHEN sm.br_in_scope <= 20 THEN 'br_lt_20'
         WHEN sm.br_in_scope <= 40 THEN 'br_20_40'
         ELSE 'br_gt_40' END AS br_tier,
    -- Empirical threshold: 25th percentile of AS among services that struggled
    percentile_cont(0.25) WITHIN GROUP (ORDER BY sm.automatibility_score)
        FILTER (WHERE sm.first_pass_test_rate < 0.8) AS threshold_below_which_struggles,
    -- For comparison: average AS among successful services
    avg(sm.automatibility_score) FILTER (WHERE sm.first_pass_test_rate >= 0.8) AS avg_as_successful,
    count(*) AS sample_size
FROM service_metrics sm
JOIN engagements e ON sm.engagement_id = e.engagement_id
WHERE sm.automatibility_score IS NOT NULL
  AND sm.first_pass_test_rate IS NOT NULL
GROUP BY 1, 2, 3
HAVING count(*) >= 3;  -- minimum 3 services per cohort for any signal
