-- SAAM Telemetry Analysis: Duration Prediction
-- Question: Can we predict phase/service durations from input characteristics?
-- Run: duckdb data/saam_telemetry.duckdb < analysis/duration_prediction.sql

-- =============================================================================
-- 1. Phase duration summary across all engagements
-- =============================================================================

SELECT
    '--- Phase Duration Summary ---' AS section;

SELECT * FROM phase_durations;

-- =============================================================================
-- 2. BA review velocity: BRs per hour
-- =============================================================================

SELECT
    '--- BA Review Velocity ---' AS section;

SELECT
    engagement_id,
    total_br_reviewed,
    (SELECT duration_hours FROM phase_metrics pm
     WHERE pm.engagement_id = ba.engagement_id AND pm.phase = 'P4A') AS duration_hours,
    CASE WHEN (SELECT duration_hours FROM phase_metrics pm
               WHERE pm.engagement_id = ba.engagement_id AND pm.phase = 'P4A') > 0
        THEN round(total_br_reviewed::DECIMAL /
             (SELECT duration_hours FROM phase_metrics pm
              WHERE pm.engagement_id = ba.engagement_id AND pm.phase = 'P4A'), 2)
        ELSE NULL END AS br_per_hour
FROM ba_review_metrics ba;

SELECT
    '--- BA Review Velocity (aggregate) ---' AS section;

SELECT
    avg(total_br_reviewed::DECIMAL / pm.duration_hours) AS avg_br_per_hour,
    median(total_br_reviewed::DECIMAL / pm.duration_hours) AS median_br_per_hour,
    count(*) AS sample_size
FROM ba_review_metrics ba
JOIN phase_metrics pm ON ba.engagement_id = pm.engagement_id AND pm.phase = 'P4A'
WHERE pm.duration_hours > 0;

-- =============================================================================
-- 3. Phase 1 throughput: LOC per hour by analysis mode
-- =============================================================================

SELECT
    '--- Phase 1 Throughput by Analysis Mode ---' AS section;

SELECT
    e.analysis_mode,
    avg(pm.metrics->>'$.total_loc')::INTEGER AS avg_loc,
    avg(pm.duration_hours) AS avg_hours,
    avg((pm.metrics->>'$.total_loc')::DECIMAL / pm.duration_hours) AS avg_loc_per_hour,
    count(*) AS n
FROM phase_metrics pm
JOIN engagements e ON pm.engagement_id = e.engagement_id
WHERE pm.phase = 'P1' AND pm.duration_hours > 0
GROUP BY e.analysis_mode;

-- =============================================================================
-- 4. Service implementation duration by type and score
-- =============================================================================

SELECT
    '--- Service Duration by Implementation Type ---' AS section;

SELECT
    implementation_type,
    count(*) AS n,
    avg(total_duration_hours) AS avg_hours,
    median(total_duration_hours) AS median_hours,
    min(total_duration_hours) AS min_hours,
    max(total_duration_hours) AS max_hours,
    avg(automatibility_score) AS avg_as
FROM service_metrics
WHERE total_duration_hours IS NOT NULL
GROUP BY implementation_type
ORDER BY implementation_type;

-- =============================================================================
-- 5. Duration prediction factors: what correlates with duration?
-- =============================================================================

SELECT
    '--- Duration Correlation Factors ---' AS section;

SELECT
    corr(automatibility_score, total_duration_hours) AS as_corr,
    corr(br_in_scope, total_duration_hours) AS br_count_corr,
    corr(test_count, total_duration_hours) AS test_count_corr,
    corr(source_complexity_avg, total_duration_hours) AS complexity_corr,
    count(*) AS sample_size
FROM service_metrics
WHERE total_duration_hours IS NOT NULL
  AND automatibility_score IS NOT NULL;

-- =============================================================================
-- 6. Duration by AS bucket and BR count
-- =============================================================================

SELECT
    '--- Duration by AS Bucket and BR Count ---' AS section;

SELECT
    CASE
        WHEN automatibility_score >= 0.85 THEN 'high (>=85%)'
        WHEN automatibility_score >= 0.70 THEN 'medium (70-84%)'
        ELSE 'low (<70%)'
    END AS as_bucket,
    CASE
        WHEN br_in_scope <= 15 THEN 'small (<=15 BRs)'
        WHEN br_in_scope <= 30 THEN 'medium (16-30 BRs)'
        ELSE 'large (>30 BRs)'
    END AS size_bucket,
    count(*) AS n,
    avg(total_duration_hours) AS avg_hours,
    avg(remediation_cycles) AS avg_remediation
FROM service_metrics
WHERE total_duration_hours IS NOT NULL
  AND automatibility_score IS NOT NULL
GROUP BY 1, 2
ORDER BY 1, 2;

-- =============================================================================
-- 7. Prediction accuracy: did calibration estimates match reality?
-- =============================================================================

SELECT
    '--- Prediction Accuracy (services that exceeded prediction) ---' AS section;

SELECT
    count(*) FILTER (WHERE duration_exceeded_prediction = true) AS exceeded_count,
    count(*) AS total_count,
    CASE WHEN count(*) > 0
        THEN round(count(*) FILTER (WHERE duration_exceeded_prediction = true)::DECIMAL / count(*), 3)
        ELSE NULL END AS exceeded_rate
FROM service_metrics;
