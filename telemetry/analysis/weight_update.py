"""Produce updated saam-calibration.yaml from DuckDB telemetry data.

Reads the current calibration file, runs analysis queries against accumulated
telemetry, and produces a new calibration version with empirically adjusted
weights and thresholds.

Usage:
    python analysis/weight_update.py

Output:
    outputs/calibrated-weights-vN.yaml (N = current version + 1)

Requirements:
    - duckdb, pyyaml
    - At least 10 services in service_metrics for meaningful analysis
    - Run from the telemetry/ directory
"""

import json
import sys
from datetime import datetime
from pathlib import Path

import duckdb
import yaml

DB_PATH = Path(__file__).parent.parent / "data" / "saam_telemetry.duckdb"
CALIBRATION_SOURCE = Path(__file__).parent.parent.parent / "core" / "steering" / "saam-calibration.yaml"
OUTPUT_DIR = Path(__file__).parent.parent / "outputs"

MIN_SAMPLE_SIZE = 10  # minimum services before we adjust weights


def load_current_calibration() -> dict:
    """Load the current calibration file."""
    if CALIBRATION_SOURCE.exists():
        with open(CALIBRATION_SOURCE) as f:
            return yaml.safe_load(f)
    return {}


def compute_automatibility_weights(db: duckdb.DuckDBPyConnection) -> dict | None:
    """Compute optimal automatibility dimension weights from outcome data.

    Uses correlation of each dimension with first_pass_test_rate as a proxy
    for predictive power. Normalizes correlations to sum to 1.0.
    """
    result = db.execute("""
        SELECT
            corr(statement_clarity, first_pass_test_rate) AS clarity_corr,
            corr(algorithm_completeness, first_pass_test_rate) AS algorithm_corr,
            corr(integration_definition, first_pass_test_rate) AS integration_corr,
            corr(data_model_readiness, first_pass_test_rate) AS data_model_corr,
            corr(edge_case_coverage, first_pass_test_rate) AS edge_case_corr,
            count(*) AS n
        FROM service_outcomes
        WHERE first_pass_test_rate IS NOT NULL
          AND statement_clarity IS NOT NULL
    """).fetchone()

    if not result or result[5] < MIN_SAMPLE_SIZE:
        return None

    correlations = {
        "statement_clarity": abs(result[0]) if result[0] is not None else 0,
        "algorithm_completeness": abs(result[1]) if result[1] is not None else 0,
        "integration_definition": abs(result[2]) if result[2] is not None else 0,
        "data_model_readiness": abs(result[3]) if result[3] is not None else 0,
        "edge_case_coverage": abs(result[4]) if result[4] is not None else 0,
    }

    total = sum(correlations.values())
    if total == 0:
        return None

    # Normalize to sum to 1.0, round to 2 decimal places
    weights = {k: round(v / total, 2) for k, v in correlations.items()}

    # Fix rounding to ensure sum == 1.0
    diff = round(1.0 - sum(weights.values()), 2)
    if diff != 0:
        # Add remainder to the largest weight
        max_key = max(weights, key=weights.get)
        weights[max_key] = round(weights[max_key] + diff, 2)

    return {"weights": weights, "sample_size": result[5]}


def compute_automatibility_thresholds(db: duckdb.DuckDBPyConnection) -> dict | None:
    """Find optimal AS thresholds based on outcome boundaries.

    Finds the score below which remediation_cycles > 2 (indicating struggle)
    and above which first_pass_test_rate > 0.8 (indicating smooth execution).
    """
    result = db.execute("""
        SELECT
            -- Type A boundary: score above which avg remediation < 1.5
            (SELECT max(automatibility_score) FROM (
                SELECT automatibility_score,
                       avg(remediation_cycles) OVER (
                           ORDER BY automatibility_score
                           ROWS BETWEEN 2 PRECEDING AND 2 FOLLOWING
                       ) AS rolling_avg
                FROM service_metrics
                WHERE automatibility_score IS NOT NULL AND remediation_cycles IS NOT NULL
            ) sub WHERE rolling_avg > 1.5) AS type_a_boundary,
            -- Type B boundary: score below which avg remediation > 3
            (SELECT min(automatibility_score) FROM (
                SELECT automatibility_score,
                       avg(remediation_cycles) OVER (
                           ORDER BY automatibility_score DESC
                           ROWS BETWEEN 2 PRECEDING AND 2 FOLLOWING
                       ) AS rolling_avg
                FROM service_metrics
                WHERE automatibility_score IS NOT NULL AND remediation_cycles IS NOT NULL
            ) sub WHERE rolling_avg > 3.0) AS type_c_boundary,
            count(*) AS n
        FROM service_metrics
        WHERE automatibility_score IS NOT NULL
    """).fetchone()

    if not result or result[2] < MIN_SAMPLE_SIZE:
        return None

    return {
        "type_a_minimum": round(result[0], 2) if result[0] else None,
        "type_b_minimum": round(result[1], 2) if result[1] else None,
        "sample_size": result[2],
    }


def compute_complexity_threshold(db: duckdb.DuckDBPyConnection) -> dict | None:
    """Determine optimal complexity ratio threshold from flag precision data."""
    result = db.execute("""
        SELECT
            sum(flagged_count) AS total_flags,
            sum(true_positive_count) AS true_positives,
            count(DISTINCT engagement_id) AS engagements
        FROM complexity_flags
        WHERE dimension != 'control_flow'  -- exclude control-flow (known noisy)
          AND flagged_count > 0
    """).fetchone()

    if not result or result[0] is None or result[0] == 0:
        return None

    precision = result[1] / result[0]

    # If precision is below 50%, the threshold is too aggressive (too many false positives)
    # If precision is above 80%, the threshold could be more aggressive (catching more)
    current_threshold = 3.0  # will read from calibration file
    if precision < 0.5:
        suggested = current_threshold + 0.5  # relax
    elif precision > 0.8:
        suggested = max(2.0, current_threshold - 0.5)  # tighten
    else:
        suggested = current_threshold  # keep

    return {
        "precision": round(precision, 3),
        "suggested_threshold": suggested,
        "total_flags": result[0],
        "true_positives": result[1],
        "engagements": result[2],
    }


def compute_confidence_weights(db: duckdb.DuckDBPyConnection) -> dict | None:
    """Validate/adjust confidence lifecycle weights based on deviation correlation."""
    result = db.execute("""
        SELECT
            -- Services where confidence < 0.7: avg deviations
            avg(deviation_count) FILTER (WHERE confidence_overall < 0.7) AS low_conf_deviations,
            -- Services where confidence >= 0.85: avg deviations
            avg(deviation_count) FILTER (WHERE confidence_overall >= 0.85) AS high_conf_deviations,
            -- Does weakest-link outperform average?
            corr(confidence_overall, deviation_count) AS weakest_link_corr,
            corr(
                (confidence_provenance + confidence_implementation + confidence_test_quality) / 3.0,
                deviation_count
            ) AS average_corr,
            count(*) AS n
        FROM service_metrics
        WHERE confidence_overall IS NOT NULL
          AND confidence_provenance IS NOT NULL
          AND confidence_implementation IS NOT NULL
          AND confidence_test_quality IS NOT NULL
    """).fetchone()

    if not result or result[4] < MIN_SAMPLE_SIZE:
        return None

    return {
        "low_confidence_avg_deviations": round(result[0], 2) if result[0] else None,
        "high_confidence_avg_deviations": round(result[1], 2) if result[1] else None,
        "weakest_link_correlation": round(result[2], 3) if result[2] else None,
        "average_correlation": round(result[3], 3) if result[3] else None,
        "weakest_link_better": (abs(result[2] or 0) > abs(result[3] or 0)),
        "sample_size": result[4],
    }


def compute_planning_estimates(db: duckdb.DuckDBPyConnection) -> dict | None:
    """Compute empirical planning estimates from historical data."""
    # BA review velocity
    ba_result = db.execute("""
        SELECT
            avg(ba.total_br_reviewed::DECIMAL / pm.duration_hours) AS avg_br_per_hour,
            count(*) AS n
        FROM ba_review_metrics ba
        JOIN phase_metrics pm ON ba.engagement_id = pm.engagement_id AND pm.phase = 'P4A'
        WHERE pm.duration_hours > 0
    """).fetchone()

    # Duration by implementation type
    duration_result = db.execute("""
        SELECT
            implementation_type,
            avg(total_duration_hours) AS avg_hours,
            avg(remediation_cycles) AS avg_remediation,
            count(*) AS n
        FROM service_metrics
        WHERE total_duration_hours IS NOT NULL
        GROUP BY implementation_type
    """).fetchall()

    if not ba_result or ba_result[1] < 2:
        return None

    planning = {
        "ba_review_velocity_br_per_hour": round(ba_result[0], 1) if ba_result[0] else None,
        "ba_sample_size": ba_result[1],
    }

    if duration_result:
        remediation = {}
        duration = {}
        for row in duration_result:
            impl_type = row[0]
            if impl_type and row[2] is not None:
                remediation[f"type_{impl_type.lower()}"] = round(row[2], 1)
            if impl_type and row[1] is not None:
                duration[f"type_{impl_type.lower()}"] = round(row[1], 0)
        planning["avg_remediation_cycles"] = remediation
        planning["avg_duration_hours_by_type"] = duration

    return planning


def produce_calibration(current: dict, analysis: dict) -> dict:
    """Merge analysis results into a new calibration document."""
    new_version = current.get("calibration_version", 1) + 1
    sample_size = analysis.get("total_services", 0)

    calibration = {
        "schema_version": "1.0",
        "calibration_version": new_version,
        "calibration_date": datetime.now().strftime("%Y-%m-%d"),
        "calibration_basis": "empirical" if sample_size >= 30 else "empirical_preliminary",
        "sample_size": sample_size,
        "previous_version": current.get("calibration_version", 1),
    }

    # Confidence section — keep current values unless analysis suggests change
    confidence = current.get("confidence", {})
    conf_analysis = analysis.get("confidence")
    if conf_analysis:
        calibration["confidence"] = confidence.copy() if confidence else {}
        calibration["confidence"]["_analysis"] = {
            "weakest_link_better_than_average": conf_analysis["weakest_link_better"],
            "low_conf_avg_deviations": conf_analysis["low_confidence_avg_deviations"],
            "high_conf_avg_deviations": conf_analysis["high_confidence_avg_deviations"],
            "sample_size": conf_analysis["sample_size"],
        }
        # Keep attention_threshold, adjust if data shows different boundary
        calibration["confidence"]["attention_threshold"] = confidence.get("attention_threshold", 0.7)
    else:
        calibration["confidence"] = confidence

    # Automatibility section
    automatibility = current.get("automatibility", {})
    calibration["automatibility"] = {}

    as_weights = analysis.get("automatibility_weights")
    if as_weights:
        calibration["automatibility"]["weights"] = as_weights["weights"]
        calibration["automatibility"]["_weights_analysis"] = {
            "sample_size": as_weights["sample_size"],
            "method": "correlation with first_pass_test_rate, normalized",
        }
    else:
        calibration["automatibility"]["weights"] = automatibility.get("weights", {})

    as_thresholds = analysis.get("automatibility_thresholds")
    if as_thresholds and as_thresholds.get("type_a_minimum"):
        calibration["automatibility"]["thresholds"] = {
            "type_a_minimum": as_thresholds["type_a_minimum"],
            "type_b_minimum": as_thresholds["type_b_minimum"] or automatibility.get("thresholds", {}).get("type_b_minimum", 0.70),
        }
    else:
        calibration["automatibility"]["thresholds"] = automatibility.get("thresholds", {})

    calibration["automatibility"]["minimum_for_implementation"] = automatibility.get("minimum_for_implementation", 0.75)
    calibration["automatibility"]["mandatory_improvement_below"] = automatibility.get("mandatory_improvement_below", 0.60)

    # Complexity section
    complexity = current.get("complexity", {})
    calibration["complexity"] = complexity.copy() if complexity else {}

    cx_analysis = analysis.get("complexity")
    if cx_analysis:
        calibration["complexity"]["ratio_threshold"] = cx_analysis["suggested_threshold"]
        calibration["complexity"]["_analysis"] = {
            "precision": cx_analysis["precision"],
            "total_flags": cx_analysis["total_flags"],
            "true_positives": cx_analysis["true_positives"],
            "engagements": cx_analysis["engagements"],
        }

    # Planning section
    planning = current.get("planning", {})
    calibration["planning"] = planning.copy() if planning else {}

    plan_analysis = analysis.get("planning")
    if plan_analysis:
        if plan_analysis.get("ba_review_velocity_br_per_hour"):
            calibration["planning"]["ba_review_velocity_br_per_hour"] = plan_analysis["ba_review_velocity_br_per_hour"]
        if plan_analysis.get("avg_remediation_cycles"):
            calibration["planning"]["avg_remediation_cycles"] = plan_analysis["avg_remediation_cycles"]
        if plan_analysis.get("avg_duration_hours_by_type"):
            calibration["planning"]["_empirical_duration_by_type"] = plan_analysis["avg_duration_hours_by_type"]

    # Governance section — carry forward unchanged (no telemetry data for this yet)
    calibration["governance"] = current.get("governance", {})

    return calibration


def main() -> None:
    if not DB_PATH.exists():
        print(f"ERROR: Database not found: {DB_PATH}", file=sys.stderr)
        print("Run import_telemetry.py first to create the database.", file=sys.stderr)
        sys.exit(1)

    db = duckdb.connect(str(DB_PATH), read_only=True)

    # Check sample size
    total_services = db.execute("SELECT count(*) FROM service_metrics").fetchone()[0]
    total_engagements = db.execute("SELECT count(*) FROM engagements").fetchone()[0]

    print(f"Telemetry database: {total_engagements} engagements, {total_services} services")

    if total_services < MIN_SAMPLE_SIZE:
        print(f"\nWARNING: Only {total_services} services in database (minimum {MIN_SAMPLE_SIZE} for calibration).")
        print("Producing calibration with available data — confidence in adjustments is LOW.")

    # Load current calibration
    current = load_current_calibration()
    current_version = current.get("calibration_version", 1)
    print(f"Current calibration: v{current_version} ({current.get('calibration_basis', 'unknown')})")

    # Run analysis
    print("\nRunning analysis...")
    analysis = {"total_services": total_services}

    print("  - Automatibility weights...")
    analysis["automatibility_weights"] = compute_automatibility_weights(db)

    print("  - Automatibility thresholds...")
    analysis["automatibility_thresholds"] = compute_automatibility_thresholds(db)

    print("  - Complexity threshold...")
    analysis["complexity"] = compute_complexity_threshold(db)

    print("  - Confidence calibration...")
    analysis["confidence"] = compute_confidence_weights(db)

    print("  - Planning estimates...")
    analysis["planning"] = compute_planning_estimates(db)

    db.close()

    # Produce new calibration
    new_calibration = produce_calibration(current, analysis)

    # Write output
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    new_version = new_calibration["calibration_version"]
    output_path = OUTPUT_DIR / f"calibrated-weights-v{new_version}.yaml"

    with open(output_path, "w") as f:
        f.write(f"# SAAM Calibration Parameters — v{new_version}\n")
        f.write(f"# Generated: {new_calibration['calibration_date']}\n")
        f.write(f"# Basis: {new_calibration['calibration_basis']}\n")
        f.write(f"# Sample: {total_services} services across {total_engagements} engagements\n")
        f.write(f"# Previous: v{current_version}\n\n")
        yaml.dump(new_calibration, f, default_flow_style=False, sort_keys=False, allow_unicode=True)

    print(f"\nProduced: {output_path}")
    print(f"  Version: {new_version}")
    print(f"  Basis: {new_calibration['calibration_basis']}")

    # Summary of changes
    print("\nChanges from current calibration:")
    if analysis.get("automatibility_weights"):
        print(f"  Automatibility weights: UPDATED (from {analysis['automatibility_weights']['sample_size']} services)")
    else:
        print("  Automatibility weights: unchanged (insufficient data)")

    if analysis.get("complexity") and analysis["complexity"].get("suggested_threshold") != current.get("complexity", {}).get("ratio_threshold"):
        print(f"  Complexity threshold: {current.get('complexity', {}).get('ratio_threshold')} → {analysis['complexity']['suggested_threshold']} (precision: {analysis['complexity']['precision']})")
    else:
        print("  Complexity threshold: unchanged")

    if analysis.get("planning"):
        print(f"  BA velocity: {analysis['planning'].get('ba_review_velocity_br_per_hour')} BR/hour (empirical)")
    else:
        print("  Planning estimates: unchanged (insufficient data)")

    print(f"\nTo apply: cp {output_path} ../core/steering/saam-calibration.yaml && npm run package")


if __name__ == "__main__":
    main()
