"""Import SAAM engagement telemetry from YAML files into DuckDB.

Usage:
    python import_telemetry.py <path-to-engagement-telemetry-dir>

Example:
    python import_telemetry.py data/raw/ENG-2026-003/

The script:
1. Reads engagement.yaml for metadata
2. Reads each phase*.yaml for phase-level metrics
3. Reads phase5-implementation/service-*.yaml for per-service metrics
4. Reads phase6-evolution/cycle-*.yaml for evolution metrics
5. Inserts/upserts all data into DuckDB

Idempotent: re-running with the same engagement replaces existing rows.
"""

import json
import sys
from pathlib import Path

import duckdb
import yaml

DB_PATH = Path(__file__).parent.parent / "data" / "saam_telemetry.duckdb"
SCHEMA_PATH = Path(__file__).parent / "schema.sql"


def ensure_schema(db: duckdb.DuckDBPyConnection) -> None:
    """Create tables if they don't exist."""
    schema_sql = SCHEMA_PATH.read_text()
    db.execute(schema_sql)


def load_yaml(path: Path) -> dict | None:
    """Load a YAML file, return None if missing."""
    if not path.exists():
        return None
    with open(path) as f:
        return yaml.safe_load(f)


def import_engagement(db: duckdb.DuckDBPyConnection, data: dict, engagement_id: str) -> None:
    """Import engagement.yaml metadata."""
    db.execute("""
        INSERT OR REPLACE INTO engagements
        (engagement_id, industry, legacy_stack, target_stack, total_services,
         total_br_count, analysis_mode, start_date, team_size)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, [
        engagement_id,
        data.get("industry"),
        data.get("legacy_stack", []),
        data.get("target_stack", []),
        data.get("total_services_in_scope"),
        data.get("total_br_count"),
        data.get("analysis_mode"),
        data.get("start_date"),
        data.get("team_size"),
    ])


def import_phase_metrics(db: duckdb.DuckDBPyConnection, data: dict, engagement_id: str) -> None:
    """Import a generic phase metrics file."""
    phase = data.get("phase", "UNKNOWN")
    metrics = data.get("metrics", {})

    db.execute("""
        INSERT OR REPLACE INTO phase_metrics
        (engagement_id, phase, started_at, completed_at, duration_hours, credits_used, actor, metrics)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?)
    """, [
        engagement_id,
        phase,
        data.get("started_at"),
        data.get("completed_at"),
        data.get("duration_hours"),
        data.get("credits_used"),
        data.get("actor"),
        json.dumps(metrics),
    ])


def import_source_vectors(db: duckdb.DuckDBPyConnection, data: dict, engagement_id: str) -> None:
    """Import Phase 1 source vector aggregates."""
    sv = data.get("source_vectors", {})
    if not sv:
        return

    agg = sv.get("aggregate", {})
    stats = sv.get("per_component_stats", {})
    dist = sv.get("complexity_distribution", {})

    db.execute("""
        INSERT OR REPLACE INTO source_vectors
        (engagement_id, total_components,
         agg_control_flow, agg_data_flow, agg_constants, agg_state_transitions,
         agg_outcomes, agg_data_writes, agg_integrations, agg_error_paths,
         stats_control_flow, stats_data_flow, stats_constants, stats_state_transitions,
         stats_outcomes, stats_data_writes, stats_integrations, stats_error_paths,
         simple_lt_10, medium_10_to_30, complex_30_to_60, very_complex_gt_60)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, [
        engagement_id,
        sv.get("total_components_with_vectors"),
        agg.get("control_flow"),
        agg.get("data_flow"),
        agg.get("constants"),
        agg.get("state_transitions"),
        agg.get("outcomes"),
        agg.get("data_writes"),
        agg.get("integrations"),
        agg.get("error_paths"),
        json.dumps(stats.get("control_flow")) if stats.get("control_flow") else None,
        json.dumps(stats.get("data_flow")) if stats.get("data_flow") else None,
        json.dumps(stats.get("constants")) if stats.get("constants") else None,
        json.dumps(stats.get("state_transitions")) if stats.get("state_transitions") else None,
        json.dumps(stats.get("outcomes")) if stats.get("outcomes") else None,
        json.dumps(stats.get("data_writes")) if stats.get("data_writes") else None,
        json.dumps(stats.get("integrations")) if stats.get("integrations") else None,
        json.dumps(stats.get("error_paths")) if stats.get("error_paths") else None,
        dist.get("simple_lt_10"),
        dist.get("medium_10_to_30"),
        dist.get("complex_30_to_60"),
        dist.get("very_complex_gt_60"),
    ])


def import_complexity_flags(db: duckdb.DuckDBPyConnection, data: dict, engagement_id: str) -> None:
    """Import Phase 4 per-dimension complexity flag rates."""
    cm = data.get("complexity_metrics", {})
    flags = cm.get("dimension_flags", {})
    if not flags:
        return

    # Clear existing flags for this engagement (upsert per dimension)
    db.execute("DELETE FROM complexity_flags WHERE engagement_id = ?", [engagement_id])

    for dim, values in flags.items():
        db.execute("""
            INSERT INTO complexity_flags
            (engagement_id, dimension, flagged_count, flagged_alone_count, resolved_count, true_positive_count)
            VALUES (?, ?, ?, ?, ?, ?)
        """, [
            engagement_id,
            dim,
            values.get("flagged_count", 0),
            values.get("flagged_alone_count", 0),
            values.get("resolved_count", 0),
            values.get("true_positive_count", 0),
        ])


def import_ba_review(db: duckdb.DuckDBPyConnection, data: dict, engagement_id: str) -> None:
    """Import Phase 4a BA review metrics."""
    metrics = data.get("metrics", {})
    corrections = data.get("complexity_corrections", {})

    db.execute("""
        INSERT OR REPLACE INTO ba_review_metrics
        (engagement_id, mode, total_br_reviewed, br_approved, br_modified,
         br_dropped, br_added, br_reclassified, br_deferred, critical_br_count,
         avg_review_minutes, disputes, false_flags_dismissed, true_gaps_confirmed,
         new_rules_from_flags)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, [
        engagement_id,
        data.get("mode"),
        metrics.get("total_br_reviewed"),
        metrics.get("br_approved_unchanged"),
        metrics.get("br_modified"),
        metrics.get("br_dropped_obsolete"),
        metrics.get("br_added_new"),
        metrics.get("br_reclassified"),
        metrics.get("br_deferred"),
        metrics.get("critical_br_count"),
        metrics.get("avg_review_time_per_br_minutes"),
        metrics.get("disputes_requiring_escalation"),
        corrections.get("false_flags_dismissed", 0),
        corrections.get("true_gaps_confirmed", 0),
        corrections.get("new_rules_added_from_flags", 0),
    ])


def import_automatibility(db: duckdb.DuckDBPyConnection, data: dict, engagement_id: str) -> None:
    """Import Phase 4b automatibility scores and rule progression data."""
    # Phase 4b has a non-standard structure — data is at top level, not under 'metrics:'
    # Extract rule_counts.total if available
    rule_counts = data.get("rule_counts", {})
    total_rules = rule_counts.get("total")

    # Extract automatibility averages
    autom = data.get("automatibility", {})
    averages = autom.get("averages", {})
    score_dist = autom.get("score_distribution", {})

    # Extract planning data
    planning = data.get("planning", {})

    # Extract greenfield info from Phase 4a data (new_services_added field)
    # This is stored in Phase 4a metrics, but we add it to 4b enriched metrics for the view
    p4a_data = None
    p4a_file = None
    import os
    raw_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # won't work — use db
    # Query Phase 4a metrics from the already-imported data
    p4a_metrics_row = db.execute(
        "SELECT metrics FROM phase_metrics WHERE engagement_id = ? AND phase IN ('P4a', 'P4A')",
        [engagement_id]
    ).fetchone()
    greenfield_count = 0
    if p4a_metrics_row and p4a_metrics_row[0]:
        import json as _json
        p4a_m = _json.loads(p4a_metrics_row[0])
        greenfield_count = p4a_m.get("new_services_added", 0)

    # Extract Phase 4a mode from the data if referenced
    phase4a_mode = autom.get("mode_used") or data.get("mode_used")
    # Also try to get mode from ba_review_metrics table
    if not phase4a_mode:
        mode_row = db.execute(
            "SELECT mode FROM ba_review_metrics WHERE engagement_id = ?",
            [engagement_id]
        ).fetchone()
        if mode_row:
            phase4a_mode = mode_row[0]

    # Store enriched metrics in phase_metrics (overwrite the generic import)
    enriched_metrics = {
        "total_rules_final": total_rules,
        "avg_automatibility_backend": averages.get("backend_after"),
        "avg_automatibility_overall": averages.get("overall_after"),
        "type_a_count": score_dist.get("type_a_count"),
        "type_b_count": score_dist.get("type_b_count"),
        "type_c_count": score_dist.get("type_c_count"),
        "recommended_model": planning.get("recommended_model"),
        "timeline_model_c_weeks": planning.get("timeline_model_c_weeks"),
        "greenfield_services_added": greenfield_count,
        "phase4a_mode": phase4a_mode,
        "tech_stack_changes": planning.get("tech_stack_changes_from_preliminary", 0),
    }

    import json
    db.execute("""
        UPDATE phase_metrics
        SET metrics = ?
        WHERE engagement_id = ? AND phase IN ('4b', 'P4B', 'P4b')
    """, [json.dumps(enriched_metrics), engagement_id])

    # If no row was updated (phase key mismatch), insert won't help — the generic import
    # already handled it. The enriched metrics just add more data to the existing row.


def import_service_metrics(db: duckdb.DuckDBPyConnection, data: dict, engagement_id: str) -> None:
    """Import Phase 5 per-service implementation metrics."""
    timing = data.get("timing", {})
    execution = data.get("execution_metrics", {})
    br = data.get("br_metrics", {})
    complexity = data.get("complexity_metrics", {})
    confidence = data.get("confidence_at_completion", {})
    deviations = data.get("deviations", {})
    types = deviations.get("types", {})

    service_id = data.get("service_id", "UNKNOWN")

    db.execute("""
        INSERT OR REPLACE INTO service_metrics
        (engagement_id, service_id, service_domain, implementation_type, automatibility_score,
         started_at, first_compile_at, first_test_run_at, all_tests_passing_at, completed_at,
         total_duration_hours, first_pass_compile, first_pass_test_rate, remediation_cycles,
         human_interventions, total_generated_loc, test_count,
         br_in_scope, br_validated, br_stuck_claims, br_required_remediation,
         source_complexity_avg, spec_complexity_avg, complexity_ratio,
         condensation_flags, condensation_true_pos,
         confidence_overall, confidence_provenance, confidence_implementation, confidence_test_quality,
         deviation_count, deviations_auto, deviations_human,
         dev_code_count, dev_test_count, spec_drift_count,
         duration_exceeded_prediction)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, [
        engagement_id,
        service_id,
        data.get("service_domain"),
        data.get("implementation_type"),
        data.get("automatibility_score"),
        timing.get("started_at"),
        timing.get("first_compile_at"),
        timing.get("first_test_run_at"),
        timing.get("all_tests_passing_at"),
        timing.get("completed_at"),
        timing.get("total_duration_hours"),
        execution.get("first_pass_compile"),
        execution.get("first_pass_test_rate"),
        execution.get("remediation_cycles"),
        execution.get("human_interventions"),
        execution.get("total_generated_loc"),
        execution.get("test_count"),
        br.get("br_in_scope"),
        br.get("br_validated"),
        br.get("br_stuck_claims_only"),
        br.get("br_required_remediation"),
        complexity.get("source_complexity_avg"),
        complexity.get("spec_complexity_avg"),
        complexity.get("ratio"),
        complexity.get("condensation_flags_raised"),
        complexity.get("condensation_flags_true_positive"),
        confidence.get("overall"),
        confidence.get("provenance"),
        confidence.get("implementation"),
        confidence.get("test_quality"),
        deviations.get("count"),
        deviations.get("auto_remediated"),
        deviations.get("human_resolved"),
        types.get("dev_code", 0),
        types.get("dev_test", 0),
        types.get("spec_drift", 0),
        data.get("duration_exceeded_prediction", False),
    ])

    # Also insert into automatibility_scores if we have dimension data
    if data.get("automatibility_score") is not None:
        db.execute("""
            INSERT OR REPLACE INTO automatibility_scores
            (engagement_id, service_id, service_domain, composite_score, implementation_type)
            VALUES (?, ?, ?, ?, ?)
        """, [
            engagement_id,
            service_id,
            data.get("service_domain"),
            data.get("automatibility_score"),
            data.get("implementation_type"),
        ])


def import_evolution_cycle(db: duckdb.DuckDBPyConnection, data: dict, engagement_id: str) -> None:
    """Import Phase 6 evolution cycle metrics."""
    metrics = data.get("metrics", {})

    db.execute("""
        INSERT OR REPLACE INTO evolution_cycles
        (engagement_id, cycle_id, started_at, completed_at, duration_hours,
         trigger_type, items_processed, br_affected, specs_updated,
         tests_updated, code_changes, new_deviations,
         confidence_before, confidence_after)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, [
        engagement_id,
        data.get("cycle_id"),
        data.get("started_at"),
        data.get("completed_at"),
        data.get("duration_hours"),
        data.get("trigger"),
        metrics.get("items_processed"),
        metrics.get("br_affected"),
        metrics.get("specs_updated"),
        metrics.get("tests_updated"),
        metrics.get("code_changes"),
        metrics.get("new_deviations_found"),
        metrics.get("confidence_before_avg"),
        metrics.get("confidence_after_avg"),
    ])


def import_engagement_dir(telemetry_dir: Path) -> None:
    """Import all telemetry files from an engagement directory."""
    # Determine engagement ID from engagement.yaml or directory name
    engagement_data = load_yaml(telemetry_dir / "engagement.yaml")
    if engagement_data:
        engagement_id = engagement_data.get("engagement_id", telemetry_dir.name)
    else:
        engagement_id = telemetry_dir.name
        print(f"WARNING: No engagement.yaml found, using directory name as ID: {engagement_id}")

    # Connect to DuckDB
    DB_PATH.parent.mkdir(parents=True, exist_ok=True)
    db = duckdb.connect(str(DB_PATH))
    ensure_schema(db)

    print(f"Importing engagement: {engagement_id}")
    imported = []

    # 1. Engagement metadata
    if engagement_data:
        import_engagement(db, engagement_data, engagement_id)
        imported.append("engagement.yaml")

    # 2. Phase files (generic phase metrics for all phases)
    phase_files = {
        "phase0-onboarding.yaml": None,
        "phase1-bottom-up.yaml": "source_vectors",
        "phase2-top-down.yaml": None,
        "phase3-convergence.yaml": None,
        "phase4-specs.yaml": "complexity_flags",
        "phase4-spec-generation.yaml": "complexity_flags",  # alternate name
        "phase4a-ba-review.yaml": "ba_review",
        "phase4b-roadmap.yaml": "automatibility",
        "phase4c-test-suites.yaml": None,
    }

    for filename, special_handler in phase_files.items():
        data = load_yaml(telemetry_dir / filename)
        if data:
            import_phase_metrics(db, data, engagement_id)
            imported.append(filename)

            # Special handlers for structured data within phase files
            if special_handler == "source_vectors":
                import_source_vectors(db, data, engagement_id)
            elif special_handler == "complexity_flags":
                import_complexity_flags(db, data, engagement_id)
            elif special_handler == "ba_review":
                import_ba_review(db, data, engagement_id)
            elif special_handler == "automatibility":
                import_automatibility(db, data, engagement_id)

    # 3. Phase 5 per-service files
    p5_dir = telemetry_dir / "phase5-implementation"
    if p5_dir.exists() and p5_dir.is_dir():
        # Summary
        summary = load_yaml(p5_dir / "summary.yaml")
        if summary:
            import_phase_metrics(db, summary, engagement_id)
            imported.append("phase5-implementation/summary.yaml")

        # Per-service files
        service_count = 0
        for svc_file in sorted(p5_dir.glob("service-*.yaml")):
            data = load_yaml(svc_file)
            if data:
                import_service_metrics(db, data, engagement_id)
                service_count += 1
        if service_count:
            imported.append(f"phase5-implementation/service-*.yaml ({service_count} files)")

    # 3b. Flat Phase 5 file (Model C / ATX Batch — single file with aggregate + per_service array)
    p5_flat = telemetry_dir / "phase5-implementation.yaml"
    if p5_flat.exists():
        data = load_yaml(p5_flat)
        if data:
            # Import as phase metrics (aggregate)
            generation = data.get("generation", {})
            results = generation.get("results", {})
            metrics = {
                "execution_model": data.get("execution_model"),
                "total_jobs": results.get("total_jobs"),
                "succeeded": results.get("succeeded"),
                "failed": results.get("failed"),
                "total_files_generated": results.get("total_files_generated"),
                "total_lines_generated": results.get("total_lines_generated"),
                "avg_duration_seconds": results.get("avg_duration_seconds"),
                "avg_files_per_service": results.get("avg_files_per_service"),
                "wall_clock_minutes": generation.get("wall_clock_minutes"),
                "total_compute_minutes": generation.get("total_compute_minutes"),
            }
            # Compute duration in hours from wall_clock_minutes
            wall_minutes = generation.get("wall_clock_minutes")
            duration_hours = wall_minutes / 60.0 if wall_minutes else None

            db.execute("""
                INSERT OR REPLACE INTO phase_metrics
                (engagement_id, phase, started_at, completed_at, duration_hours, actor, metrics)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            """, [
                engagement_id,
                "P5",
                generation.get("started_at"),
                generation.get("completed_at"),
                duration_hours,
                "agent",
                json.dumps(metrics),
            ])
            imported.append("phase5-implementation.yaml (flat/batch format)")

    # 4. Phase 6 evolution cycles
    p6_dir = telemetry_dir / "phase6-evolution"
    if p6_dir.exists():
        cycle_count = 0
        for cycle_file in sorted(p6_dir.glob("cycle-*.yaml")):
            data = load_yaml(cycle_file)
            if data:
                import_evolution_cycle(db, data, engagement_id)
                cycle_count += 1
        if cycle_count:
            imported.append(f"phase6-evolution/cycle-*.yaml ({cycle_count} files)")

    # 4b. Phase 6 session files at telemetry root (alternate naming: phase6-session-*.yaml)
    p6_session_count = 0
    for p6_file in sorted(telemetry_dir.glob("phase6-session-*.yaml")):
        data = load_yaml(p6_file)
        if data:
            cycle_id = data.get("cycle_id") or data.get("session_id") or p6_file.stem
            metrics = data.get("metrics", {})
            db.execute("""
                INSERT OR REPLACE INTO evolution_cycles
                (engagement_id, cycle_id, started_at, completed_at, duration_hours,
                 trigger_type, items_processed, br_affected, specs_updated,
                 tests_updated, code_changes, new_deviations,
                 confidence_before, confidence_after)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, [
                engagement_id,
                cycle_id,
                data.get("started_at"),
                data.get("completed_at"),
                data.get("duration_hours"),
                data.get("trigger") or data.get("trigger_type"),
                metrics.get("items_processed"),
                metrics.get("br_affected"),
                metrics.get("specs_updated"),
                metrics.get("tests_updated"),
                metrics.get("code_changes"),
                metrics.get("new_deviations_found") or metrics.get("new_deviations"),
                metrics.get("confidence_before_avg") or metrics.get("confidence_before"),
                metrics.get("confidence_after_avg") or metrics.get("confidence_after"),
            ])
            p6_session_count += 1
    if p6_session_count:
        imported.append(f"phase6-session-*.yaml ({p6_session_count} files)")

    # 5. Human interventions log
    interventions_file = telemetry_dir / "interventions.yaml"
    if interventions_file.exists():
        data = load_yaml(interventions_file)
        # Support both 'interventions' and 'entries' keys
        entries = data.get("interventions") or data.get("entries") if data else None
        if entries:
            # Clear existing interventions for this engagement (upsert)
            db.execute("DELETE FROM interventions WHERE engagement_id = ?", [engagement_id])
            for entry in entries:
                db.execute("""
                    INSERT INTO interventions
                    (engagement_id, phase, service, type, category, overhead_minutes, timestamp)
                    VALUES (?, ?, ?, ?, ?, ?, ?)
                """, [
                    engagement_id,
                    entry.get("phase"),
                    entry.get("service"),
                    entry.get("type"),
                    entry.get("category") or entry.get("impact", "other"),
                    entry.get("estimated_overhead_minutes") or entry.get("overhead_minutes"),
                    entry.get("timestamp"),
                ])
            imported.append(f"interventions.yaml ({len(entries)} entries)")

    db.close()

    # Report
    print(f"  Imported: {len(imported)} file(s)")
    for f in imported:
        print(f"    - {f}")
    print(f"  Database: {DB_PATH}")


def main() -> None:
    if len(sys.argv) != 2:
        print("Usage: python import_telemetry.py <path-to-engagement-telemetry-dir>")
        print("Example: python import_telemetry.py data/raw/ENG-2026-003/")
        sys.exit(1)

    telemetry_dir = Path(sys.argv[1])
    if not telemetry_dir.is_dir():
        print(f"ERROR: Not a directory: {telemetry_dir}", file=sys.stderr)
        sys.exit(1)

    import_engagement_dir(telemetry_dir)
    print("\nDone.")


if __name__ == "__main__":
    main()
