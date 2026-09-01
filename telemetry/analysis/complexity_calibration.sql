-- SAAM Telemetry Analysis: Complexity / Semantic Preservation Calibration
-- Question: Are condensation flags useful? What's the optimal ratio threshold?
-- Run: duckdb data/saam_telemetry.duckdb < analysis/complexity_calibration.sql

-- =============================================================================
-- 1. Overall flag precision (true positives / total flags)
-- =============================================================================

SELECT
    '--- Overall Flag Precision ---' AS section;

SELECT
    sum(flagged_count) AS total_flags,
    sum(true_positive_count) AS true_positives,
    CASE WHEN sum(flagged_count) > 0
        THEN round(sum(true_positive_count)::DECIMAL / sum(flagged_count), 3)
        ELSE NULL END AS precision,
    count(DISTINCT engagement_id) AS engagements
FROM complexity_flags
WHERE flagged_count > 0;

-- =============================================================================
-- 2. Per-dimension precision (which dimensions are most predictive?)
-- =============================================================================

SELECT
    '--- Per-Dimension Precision ---' AS section;

SELECT
    dimension,
    sum(flagged_count) AS total_flags,
    sum(true_positive_count) AS true_positives,
    CASE WHEN sum(flagged_count) > 0
        THEN round(sum(true_positive_count)::DECIMAL / sum(flagged_count), 3)
        ELSE NULL END AS precision,
    sum(resolved_count) AS resolved
FROM complexity_flags
GROUP BY dimension
ORDER BY precision DESC NULLS LAST;

-- =============================================================================
-- 3. Control-flow alone heuristic validation
-- =============================================================================

SELECT
    '--- Control-Flow Alone Heuristic ---' AS section;

SELECT
    sum(flagged_alone_count) AS flagged_alone_total,
    sum(CASE WHEN dimension = 'control_flow' THEN true_positive_count ELSE 0 END) AS cf_true_positives,
    'Should be near 0 — control-flow alone should NOT be a true positive' AS interpretation
FROM complexity_flags
WHERE dimension = 'control_flow';

-- =============================================================================
-- 4. Threshold sensitivity from Phase 4 telemetry
-- =============================================================================

SELECT
    '--- Threshold Sensitivity (from phase_metrics) ---' AS section;

SELECT
    engagement_id,
    metrics->>'$.current_ratio_threshold' AS current_threshold,
    metrics->>'$.flags_at_threshold_2' AS flags_at_2,
    metrics->>'$.flags_at_threshold_4' AS flags_at_4,
    metrics->>'$.flags_at_threshold_5' AS flags_at_5
FROM phase_metrics
WHERE phase = 'P4'
  AND metrics->>'$.current_ratio_threshold' IS NOT NULL;

-- =============================================================================
-- 5. BA review corrections (from Phase 4a)
-- =============================================================================

SELECT
    '--- BA Review Corrections to Flags ---' AS section;

SELECT
    sum(false_flags_dismissed) AS total_false_flags,
    sum(true_gaps_confirmed) AS total_true_gaps,
    sum(new_rules_from_flags) AS total_new_rules,
    CASE WHEN (sum(false_flags_dismissed) + sum(true_gaps_confirmed)) > 0
        THEN round(sum(true_gaps_confirmed)::DECIMAL / (sum(false_flags_dismissed) + sum(true_gaps_confirmed)), 3)
        ELSE NULL END AS ba_confirmed_precision
FROM ba_review_metrics;

-- =============================================================================
-- 6. Do flagged services produce more deviations in Phase 5?
-- =============================================================================

SELECT
    '--- Condensation Flags vs Phase 5 Deviations ---' AS section;

SELECT
    CASE WHEN condensation_flags > 0 THEN 'flagged' ELSE 'clean' END AS flag_status,
    count(*) AS n,
    avg(deviation_count) AS avg_deviations,
    avg(remediation_cycles) AS avg_remediation,
    avg(first_pass_test_rate) AS avg_first_pass
FROM service_metrics
GROUP BY 1;
