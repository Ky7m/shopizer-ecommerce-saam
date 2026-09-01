-- SAAM Telemetry Analysis: Confidence Calibration
-- Question: Do confidence scores predict actual deviations and failures?
-- Run: duckdb data/saam_telemetry.duckdb < analysis/confidence_calibration.sql

-- =============================================================================
-- 1. Confidence vs deviation count
-- =============================================================================

SELECT
    '--- Confidence vs Deviations ---' AS section;

SELECT
    CASE
        WHEN confidence_overall < 0.7 THEN 'low (<0.7)'
        WHEN confidence_overall < 0.85 THEN 'medium (0.7-0.84)'
        ELSE 'high (>=0.85)'
    END AS confidence_bucket,
    count(*) AS n,
    avg(deviation_count) AS avg_deviations,
    avg(deviations_human) AS avg_human_deviations,
    avg(spec_drift_count) AS avg_spec_drift
FROM service_metrics
WHERE confidence_overall IS NOT NULL
GROUP BY 1
ORDER BY 1;

-- =============================================================================
-- 2. Confidence vs first-pass test rate
-- =============================================================================

SELECT
    '--- Confidence vs First-Pass Test Rate ---' AS section;

SELECT
    corr(confidence_overall, first_pass_test_rate) AS correlation,
    count(*) AS sample_size
FROM service_metrics
WHERE confidence_overall IS NOT NULL
  AND first_pass_test_rate IS NOT NULL;

-- =============================================================================
-- 3. Per-dimension analysis: which dimension best predicts problems?
-- =============================================================================

SELECT
    '--- Per-Dimension Correlation with Deviations ---' AS section;

SELECT
    corr(confidence_provenance, deviation_count) AS provenance_vs_dev,
    corr(confidence_implementation, deviation_count) AS implementation_vs_dev,
    corr(confidence_test_quality, deviation_count) AS test_quality_vs_dev,
    count(*) AS sample_size
FROM service_metrics
WHERE confidence_overall IS NOT NULL
  AND deviation_count IS NOT NULL;

-- =============================================================================
-- 4. Do low-confidence services need more remediation?
-- =============================================================================

SELECT
    '--- Confidence vs Remediation Effort ---' AS section;

SELECT
    CASE
        WHEN confidence_overall < 0.7 THEN 'low (<0.7)'
        WHEN confidence_overall < 0.85 THEN 'medium (0.7-0.84)'
        ELSE 'high (>=0.85)'
    END AS confidence_bucket,
    count(*) AS n,
    avg(remediation_cycles) AS avg_remediation,
    avg(human_interventions) AS avg_human_interventions,
    avg(total_duration_hours) AS avg_duration
FROM service_metrics
WHERE confidence_overall IS NOT NULL
GROUP BY 1
ORDER BY 1;

-- =============================================================================
-- 5. Phase 6 evolution: does pre-evolution confidence predict improvement?
-- =============================================================================

SELECT
    '--- Evolution: Confidence Before vs After ---' AS section;

SELECT
    trigger_type,
    count(*) AS cycles,
    avg(confidence_before) AS avg_before,
    avg(confidence_after) AS avg_after,
    avg(confidence_after - confidence_before) AS avg_improvement,
    avg(items_processed) AS avg_items
FROM evolution_cycles
WHERE confidence_before IS NOT NULL
GROUP BY trigger_type;

-- =============================================================================
-- 6. Validate the weakest-link model: is min() the right aggregation?
-- =============================================================================

SELECT
    '--- Weakest-Link vs Average: Which Predicts Deviations Better? ---' AS section;

SELECT
    corr(confidence_overall, deviation_count) AS weakest_link_corr,
    corr(
        (confidence_provenance + confidence_implementation + confidence_test_quality) / 3.0,
        deviation_count
    ) AS average_corr,
    count(*) AS sample_size
FROM service_metrics
WHERE confidence_overall IS NOT NULL
  AND confidence_provenance IS NOT NULL
  AND confidence_implementation IS NOT NULL
  AND confidence_test_quality IS NOT NULL
  AND deviation_count IS NOT NULL;
