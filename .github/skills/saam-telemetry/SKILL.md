---
name: saam-telemetry
description: "Telemetry schema definitions, per-phase metric collection protocols, and performance analytics."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Telemetry Framework

## Purpose

The telemetry framework captures anonymized engagement metrics that enable cross-engagement learning: calibrating confidence weights, validating automatibility scoring, tuning complexity thresholds, and predicting phase durations.

Telemetry is produced **per phase** (not once at engagement end). Each phase completion triggers a telemetry export — so partial engagements still contribute data.

## Design Principles

1. **No client-identifying data** — telemetry contains statistical aggregates and structural characteristics only. No company names, repo URLs, file paths, component names, or business rule content.
2. **Graph is the raw data** — per-component vectors, BR details, and lifecycle events live in the engagement graph. Telemetry YAML is computed from graph queries.
3. **Independently exportable** — each phase's telemetry file is self-contained and can be exported without waiting for later phases.
4. **Retroactively amendable** — later phases may update earlier telemetry (e.g., Phase 4a corrections update Phase 4 flag accuracy).
5. **Timing from task tracker** — phase duration is inferred from task status transitions (first task `in_progress` → last task `completed`), not from manual input.

## Telemetry Directory Structure

```
.saam/telemetry/
├── engagement.yaml                  # engagement-level metadata (anonymized)
├── interventions.yaml               # human corrections/redirections log (append-only)
├── phase0-onboarding.yaml
├── phase1-bottom-up.yaml
├── phase2-top-down.yaml
├── phase3-convergence.yaml
├── phase4-specs.yaml
├── phase4a-ba-review.yaml
├── phase4b-roadmap.yaml
├── phase4c-test-suites.yaml
├── phase5-implementation/
│   ├── summary.yaml                 # aggregate across all services
│   ├── service-001.yaml             # per-service metrics (anonymized ID)
│   ├── service-002.yaml
│   └── ...
└── phase6-evolution/
    ├── cycle-001.yaml
    └── ...
```

## Calibration File Reference

Telemetry analysis produces calibrated parameters stored in `.github/saam-calibration.yaml`. All steering files that use thresholds MUST read from the calibration file (with hardcoded fallbacks for fresh installs). See `.github/saam-calibration.yaml` for the current values and their provenance.

## Telemetry Production Protocol

At the end of each phase (after exit gate approval), the agent MUST:

1. Query the engagement graph for the relevant metrics
2. Compute aggregates and distributions (never export per-component raw data)
3. Write the YAML file to `.saam/telemetry/`
4. If a previous version exists (retroactive amendment), merge new data — do not overwrite timing or earlier metrics

### Timing Inference (All Phases)

Duration is computed from **graph PhaseEvent timestamps** (machine-recorded, not estimated):

```
started_at = PhaseEvent(phase, event="started").timestamp
completed_at = PhaseEvent(phase, event="completed").timestamp
duration_minutes = completed_at - started_at
```

The agent writes PhaseEvent nodes at two points:
- **Phase start:** When the tracking file is created (PRECONDITION step) → `graph_add_node(nodeType="PhaseEvent", id="<phase>-started", properties={phase, event: "started", timestamp: datetime()})`
- **Phase end:** When the exit gate is approved → `graph_add_node(nodeType="PhaseEvent", id="<phase>-completed", properties={phase, event: "completed", timestamp: datetime()})`

At telemetry production time, the agent queries these events and computes `duration_hours` from the difference — NOT from estimation.

**Fallback:** If PhaseEvents are missing (graph unavailable, old engagement), use git commit timestamps as proxy: first commit for phase → last commit for phase.

**For Phase 5 per-service:** Additional PhaseEvent nodes with `service` property:
```
PhaseEvent {phase: "P5", event: "started", service: "order-service", timestamp: ...}
PhaseEvent {phase: "P5", event: "completed", service: "order-service", timestamp: ...}
```

### Step-Level Timing (P1, P4, P5 ONLY — the historically noisy phases)

Phase start/end alone measures **wall clock**, which smears real work together with idle gaps (overnight,
lunch, a 2h serialized CAST fetch, waiting on a human). For the three phases where that noise is worst
(P1 waves + inventory ingest + reclassification; P4 per-service deep extraction; P5 per-service build
loop) the agent ALSO emits **StepEvents** at boundaries it *already* checkpoints — so intra-phase timing
is real, not derived from coarse endpoints.

A StepEvent is a `PhaseEvent` with a `step` label (no new node type — additive, pass-through):
```
PhaseEvent {phase: "P1", event: "started", step: "reclassification", timestamp: ...}
PhaseEvent {phase: "P1", event: "completed", step: "reclassification", timestamp: ...}
```
Emit at boundaries that already exist — do NOT invent new mid-step callbacks (that is the bookkeeping the
agent forgets in a long session). Piggyback on:
- **P1:** Step 0 inventory ingest (start/end), each segment (start/end — the tracker already marks these), the reclassification sweep (start/end — it is a known long cycle, so bracket it).
- **P4:** each service's deep extraction (start/end).
- **P5:** each service's build loop already has per-service PhaseEvents — extend with the milestones it already tracks (first-compile, first-test-run, all-tests-passing).

**Do NOT add StepEvents to P2/P3/P4a/P4b/P4c** — they are human-decision phases where wall clock is
dominated by human think-time; step granularity buys little and adds noise. This scoping is intentional.

### Interventions — captured as the ORIGIN of a plan-deviation, NOT a user-input log

An intervention is a human redirect that changes what the agent was about to do. There are two kinds and
both matter — the second is the higher-value one:
- **Solicited:** the human answers a 🔴 PROMPT HUMAN (a defined touchpoint). Reliably hit.
- **Unsolicited:** the human injects an unprompted instruction that changes the plan — e.g. "reconcile
  what's done against CAST" that pulls a step earlier than planned. NOT at any defined touchpoint. These
  are gold: an unsolicited intervention is direct evidence the framework should have done something it
  didn't — it is the framework's own gap register.

**Capture rule (this is the reliable mechanism — NOT a user-input hook, which would firehose every
message and dump raw human text):** an intervention is recorded as a labeled `origin` on the
**plan-deviation event the agent is ALREADY emitting** — a new/changed StepEvent, a tracking-file plan
change, or an unplanned graph write. When the agent takes an action *because the human redirected it off
the current plan*, it stamps that action:
```
PhaseEvent {phase, event, step: "<name>",
            origin: "unsolicited-intervention",              # or "solicited-intervention" | "planned"
            interventionSummary: "reconcile done-work vs CAST -> reclassification needed earlier",
            interventionLedTo: "pulled Step 0b reclassification forward from P4 to P1"}
```
Filtered by construction (only plan-changing actions carry it — no "yes/go" noise, no raw-text capture,
no PII footprint) and content-aware (only the agent knows it is deviating). The agent's tell is simple:
*did I ask for this, or did the human volunteer it, and did it change my plan?* Volunteered + changed the
plan = an unsolicited intervention — stamp the resulting event.

**Reliability floor (honest):** if the agent misses stamping an intervention, the work still happened and
time still passed, so it surfaces in the timing model as an **unattributed wall-clock gap** (below) — an
"unexplained interval, investigate" flag, never silent loss. We under-attribute, flag, and never
fabricate.

### Four derived timing quantities (P1/P4/P5)

Computed at telemetry time from the events above + persisted activity — nature comes from the BOUNDING
events, never from a gap's duration alone:

| Quantity | Definition | Use |
|----------|------------|-----|
| `active_work_minutes` | Σ of StepEvent (start→end) intervals | **Calibration-primary.** A FLOOR on work (unstructured work leaving no step/graph trail is under-counted — the safe error direction). |
| `human_wait_minutes` | Σ of intervals bounded by a solicited-intervention pause and its resume | Attributed human think-time; excluded from active work. |
| `wall_clock_minutes` | Phase start → end | Envelope + human context. Retained BECAUSE it contains the interventions and long cycles that step events might miss. |
| `unattributed_minutes` | `wall_clock − active_work − human_wait` | The residual. **Small = healthy. Large = either uninstrumented work (add a StepEvent next time) or genuine idle (overnight).** Attribute using persisted graph activity (`_lastUpdated`/`_createdAt` inside the interval = work happened) or a logged intervention; if silent, mark `unattributed` and EXCLUDE from work — flag, don't guess. |

**Calibration eligibility:** `active_work_minutes` feeds calibration. `wall_clock` and `unattributed` are
human-context/diagnostic — NOT calibration inputs (they carry idle noise). This resolves the "2h overnight
gap counted as work" corruption at the source.

**Credits tracking (DISABLED):** Credits cannot be read programmatically by the agent. The `credits_used` field is retained in schemas for future use but should NOT be populated — values would be unreliable estimates. If a reliable credits API becomes available, re-enable by uncommenting `credits_per_1k_loc` in `.github/saam-calibration.yaml`.

---

## Timing Data Integrity (MANDATORY — No Backfilling)

**The telemetry system's value depends entirely on timestamp accuracy.** Inaccurate timing data corrupts calibration and makes predictions unreliable for future engagements.

### Timing Source Hierarchy (Preference Order)

| Source | `timing_source` value | Reliability | When Used |
|--------|----------------------|-------------|-----------|
| Graph PhaseEvent nodes | `machine_recorded` | HIGH | Default — agent writes PhaseEvent at phase start/end |
| Git commit timestamps | `git_derived` | HIGH | Fallback when graph unavailable — `git log --format=%aI` |
| Session observation (agent saw it happen) | `session_observed` | MEDIUM | When both graph and git miss the event |
| Human reported (user states duration) | `human_reported` | MEDIUM | Human corrects or provides external timing |
| Retroactive estimate (reconstructed later) | `retroactive_estimate` | LOW | **AVOID** — only when no other source exists |

### Rules

1. **Every telemetry YAML MUST include `timing_source` field.** If missing, the data is untrusted for calibration.

2. **`machine_recorded` is the ONLY source that feeds calibration automatically.** All other sources require `calibration_eligible: false` flag.

3. **PhaseEvent nodes MUST be written at EXACTLY two moments:**
   - When the tracking file is CREATED (phase start) — not when extraction/generation begins later
   - When the exit gate is PRESENTED to the human (phase end) — not after human approves

4. **NEVER retroactively create PhaseEvent nodes.** If a phase ran without events (graph was down), use git timestamps. If git wasn't committed during the phase, mark as `timing_source: retroactive_estimate` with a `timing_caveat` note explaining why.

5. **NEVER overwrite `started_at` or `completed_at` timestamps after initial recording.** If a phase was paused and resumed (multiple sessions), record the FIRST start and LAST completion. The gap is real elapsed time.

6. **For Phase 5 per-service timing:** The git commit of the service code (`feat(phase5): <service>...`) IS the service completion timestamp. The commit of tracking/telemetry update is the overhead timestamp. Both are machine-recorded (git supplies the time).

### What This Prevents

| Bad Pattern | How It Corrupts | Prevention |
|-------------|----------------|------------|
| Agent estimates "~30 min" without evidence | Calibration learns wrong durations | PhaseEvent nodes provide real timestamps |
| Human backfills timing from memory | Memory is unreliable (±50% error typical) | `timing_source: human_reported` excluded from auto-calibration |
| Agent writes timestamps after the fact | "Created" != "happened at" | PhaseEvent MUST be written IN THE MOMENT |
| Phase runs across sessions without events | Gap becomes invisible | Git commit timestamps as fallback |
| Retroactive telemetry import | Timing looks precise but isn't | `retroactive_estimate` flag + `calibration_eligible: false` |

### Calibration Eligibility

Only telemetry with `timing_source: machine_recorded` OR `timing_source: git_derived` is eligible for automatic calibration updates. All other sources are recorded for human analysis but do NOT feed into `.github/saam-calibration.yaml` weight updates.

This means the FIRST engagement with proper PhaseEvent discipline will produce the FIRST reliable calibration data. Prior engagements have `calibration_eligible: false` for most timing dimensions because they were retroactively reconstructed.

## Per-Phase Schemas

### engagement.yaml

```yaml
schema_version: "1.0"
engagement_id: "ENG-2026-003"          # internal tracking ID
industry: "manufacturing"               # sector classification
legacy_stack: ["dotnet-framework", "sql-server", "wcf"]
target_stack: ["java-spring", "postgresql", "kafka"]
total_services_in_scope: 14
total_br_count: 342                     # final count after Phase 4a
analysis_mode: "cast"                   # cast | direct | hybrid
start_date: "2026-08-01"
team_size: 3                            # humans involved
```

**Stack labeling is critical for calibration accuracy.** Different source stacks produce dramatically different extraction throughput (Ruby 18.5K LOC/h vs COBOL TBD). Calibration queries group by `legacy_stack[0]` (primary source) and `target_stack[0]` (primary target) to produce stack-specific predictions rather than misleading cross-stack averages.

### phase1-bottom-up.yaml

```yaml
schema_version: "1.0"
phase: "P1"
started_at: "2026-08-01T09:00:00Z"
completed_at: "2026-08-05T17:30:00Z"
# --- Timing (P1/P4/P5 carry the step-level split; other phases carry duration_hours only) ---
wall_clock_minutes: 6750                 # phase start -> end (envelope; NOT calibration input)
active_work_minutes: 1840                # Σ StepEvent intervals (CALIBRATION-PRIMARY; a floor on work)
human_wait_minutes: 210                  # Σ solicited-intervention pause->resume
unattributed_minutes: 4700               # wall - active - wait; large here = overnight gaps across a 4-day phase (expected, excluded from work)
step_timings:                            # per-step active intervals (from StepEvents)
  - step: "step0-inventory-ingest"   minutes: 55
  - step: "reclassification"         minutes: 130   # known long cycle (serialized CAST fetch) — bracketed, so it's WORK not mystery gap
  - step: "segment-financials-gl"    minutes: 240
  - step: "segment-payroll"          minutes: 310
  # ... one per segment
interventions:                           # plan-deviations stamped with origin (NOT a user-input log)
  - origin: "unsolicited-intervention"
    at: "2026-08-31T14:10:00Z"
    summary: "reconcile done-work vs CAST"
    led_to: "pulled reclassification forward from P4 to P1"   # framework-gap signal
duration_hours: 30.7                     # = active_work_minutes/60 (work time; NOT wall clock)
# credits_used: null                       # DISABLED — not programmatically accessible
actor: "agent"                          # agent | human | mixed
analysis_mode: "cast"

metrics:
  total_source_files_analyzed: 847
  total_loc: 312000
  languages: ["csharp", "sql", "xml"]
  modules_identified: 23
  components_catalogued: 156
  br_candidates_extracted: 489
  integration_points_found: 34
  data_stores_identified: 8

# CAST intent-classification fidelity (CAST/Hybrid only) — empirical instrumentation of two framework
# assumptions: "names are a weak prior" and "CAST classification is incomplete". Computed from the
# castIntent -> firstPassIntent snapshot deltas on businessLayer SourceComponents (no live instrumentation).
# Keep cast_unknown (CAST silent) SEPARATE from firstpass_flips (CAST classified, SAAM overrode) — they
# imply different fixes.
cast_intent_fidelity:
  business_components: 10112              # denominator (businessLayer=true)
  cast_unknown: 7329                      # castIntent='unknown' — CAST had no classification/accessors
  cast_unknown_pct: 72.5                  # the empirical size of "CAST fails classification"
  name_confirmed: 1980                    # castIntent == firstPassIntent (SAAM agreed with CAST)
  firstpass_flips_from_cast: 803          # castIntent != firstPassIntent AND castIntent != 'unknown' (SAAM overrode CAST)
  firstpass_flip_matrix:                  # from -> to counts for the overrides (where CAST had an opinion)
    validate_to_entry: 210
    validate_to_distribute: 95
    post_to_distribute: 40
    # ...
  method: "behavior+name"                 # behavior+name | name_only(fallback) — flag if fallback was used

source_vectors:
  total_components_with_vectors: 156

  # Sum across all components
  aggregate:
    control_flow: 1847
    data_flow: 423
    constants: 189
    state_transitions: 67
    outcomes: 312
    data_writes: 156
    integrations: 34
    error_paths: 278

  # Statistical distribution per component
  per_component_stats:
    control_flow:
      min: 0
      max: 87
      median: 8
      p90: 24
      mean: 11.8
    data_flow:
      min: 0
      max: 34
      median: 2
      p90: 6
      mean: 2.7
    constants:
      min: 0
      max: 42
      median: 0
      p90: 3
      mean: 1.2
    state_transitions:
      min: 0
      max: 12
      median: 0
      p90: 1
      mean: 0.4
    outcomes:
      min: 0
      max: 15
      median: 2
      p90: 4
      mean: 2.0
    data_writes:
      min: 0
      max: 8
      median: 1
      p90: 2
      mean: 1.0
    integrations:
      min: 0
      max: 5
      median: 0
      p90: 1
      mean: 0.2
    error_paths:
      min: 0
      max: 23
      median: 1
      p90: 4
      mean: 1.8

  # How many components fall in each total-vector-sum bucket
  complexity_distribution:
    simple_lt_10: 89
    medium_10_to_30: 42
    complex_30_to_60: 18
    very_complex_gt_60: 7
```

### phase2-top-down.yaml

```yaml
schema_version: "1.0"
phase: "P2"
started_at: "2026-08-03T09:00:00Z"
completed_at: "2026-08-08T16:00:00Z"
duration_hours: 127.0
actor: "mixed"

metrics:
  bounded_contexts_identified: 5
  services_designed: 14
  shared_kernel_entities: 3
  integration_patterns_count: 8         # event-driven, REST, etc.
  data_stores_planned: 6
  adrs_produced: 4                      # architecture decision records
```

### phase3-convergence.yaml

```yaml
schema_version: "1.0"
phase: "P3"
started_at: "2026-08-09T09:00:00Z"
completed_at: "2026-08-11T17:00:00Z"
duration_hours: 56.0
actor: "agent"

metrics:
  br_assigned_to_services: 489
  br_unassigned_requiring_decision: 12
  boundary_conflicts_found: 3
  boundary_conflicts_resolved: 3
  feature_validations_run: 14
  feature_validations_passed: 12
  gap_analysis_items: 7
  gap_items_resolved: 5
```

### phase4-specs.yaml

```yaml
schema_version: "1.0"
phase: "P4"
started_at: "2026-08-12T09:00:00Z"
completed_at: "2026-08-19T17:00:00Z"
duration_hours: 176.0
actor: "agent"

metrics:
  services_specified: 14
  total_br_extracted: 342
  avg_br_per_service: 24.4
  api_contracts_generated: 14
  ddl_schemas_generated: 14
  independent_validation_passed: true   # 5-rule random test
  # Implicit-system layers (A/B/C) — counts of extracted structural elements:
  entity_state_machines: 9              # entities with an Entity State Model (Layer A)
  entity_states_total: 34               # sum of states across all machines
  state_transitions_total: 41           # sum of transitions
  data_invariants_total: 22             # Layer A invariants extracted
  data_invariants_mandatory_db: 7       # of those, tier db/both (integrity)
  extension_points_total: 5             # Layer B extension points
  db_objects_total: 6                   # Layer C db-tier objects (views/functions/procs/triggers)
  placement_candidates_flagged: 11      # PLACEMENT_REVIEW candidates surfaced (default app-tier)
  # Intent fidelity — second-order signal (CAST/Hybrid): how often a human source read in P4 corrected
  # an intent that behavior+name (Step 0b) got wrong. Computed from firstPassIntent -> p4Intent deltas
  # on components P4 actually read (the extracted set). Complements the P1 cast_intent_fidelity block.
  p4_intent:
    components_read: 355                 # components P4 touched (p4Intent stamped)
    p4_confirmed_firstpass: 331          # p4Intent == firstPassIntent (source read confirmed the first pass)
    p4_flips_from_firstpass: 24          # p4Intent != firstPassIntent (source read corrected behavior+name)
    p4_flip_matrix:                      # from -> to counts
      validate_to_derive: 9
      entry_to_distribute: 7
      # ...
  # New metrics (calibration signals):
  total_source_files_read: 173          # actual files read during deep extraction
  avg_extraction_minutes_per_service: 20 # wall-clock per service
  rule_growth_from_phase1_pct: 105      # (phase4_rules / phase1_rules - 1) × 100
  overhead_minutes: 78                   # rework, steering updates, format fixes
  overhead_pct_of_extraction: 45.6      # overhead / extraction × 100

complexity_metrics:
  components_analyzed: 156
  components_flagged: 23
  components_critical: 4
  components_resolved_pass2: 15
  components_unresolved_to_ba: 4
  passes_needed_avg: 1.6

  dimension_flags:
    control_flow:
      flagged_count: 34
      flagged_alone_count: 18
      resolved_count: 12
      true_positive_count: 4
    data_flow:
      flagged_count: 8
      resolved_count: 7
      true_positive_count: 6
    constants:
      flagged_count: 12
      resolved_count: 10
      true_positive_count: 9
    state_transitions:
      flagged_count: 5
      resolved_count: 4
      true_positive_count: 4
    outcomes:
      flagged_count: 9
      resolved_count: 7
      true_positive_count: 6
    data_writes:
      flagged_count: 3
      resolved_count: 3
      true_positive_count: 3
    integrations:
      flagged_count: 6
      resolved_count: 5
      true_positive_count: 5
    error_paths:
      flagged_count: 11
      resolved_count: 9
      true_positive_count: 8

  control_flow_alone_was_correct: 18
  control_flow_alone_was_wrong: 0

  # Threshold sensitivity (what WOULD have been flagged at other ratios)
  current_ratio_threshold: 3.0
  flags_at_threshold_2: 45
  flags_at_threshold_4: 12
  flags_at_threshold_5: 7
```

### phase4a-ba-review.yaml

```yaml
schema_version: "1.0"
phase: "P4A"
started_at: "2026-08-20T09:00:00Z"
completed_at: "2026-08-28T16:00:00Z"
duration_hours: 55.0
actor: "human"
mode: "full_workshop"                   # full_workshop | agent_defaults

metrics:
  total_br_reviewed: 342
  br_approved_unchanged: 278
  br_modified: 41
  br_dropped_obsolete: 18
  br_added_new: 5
  br_reclassified: 12
  br_deferred: 8
  critical_br_count: 67
  avg_review_time_per_br_minutes: 9.6
  disputes_requiring_escalation: 3
  # Implicit-system layer decisions (A/B) reviewed at 4a:
  invariants_reviewed: 22
  invariants_load_bearing: 15           # kept as essential business truth
  invariants_legacy_artifact: 4         # dropped/relaxed as legacy artifacts
  state_machine_closure_fixes: 3        # non-closed machines the BA resolved
  extension_points_reviewed: 5
  extension_points_reproduce: 3         # kept, engine resolves
  extension_points_unify: 1             # collapsed to one behavior
  extension_points_drop: 1              # obsolete customization dropped

# Retroactive amendment (added after Phase 4a completes):
# Updates Phase 4 complexity flag accuracy
complexity_corrections:
  false_flags_dismissed: 2             # BA said "this flag was noise"
  true_gaps_confirmed: 3              # BA confirmed real missing logic
  new_rules_added_from_flags: 4       # unresolved flags led to new rules
```

### phase4b-roadmap.yaml

```yaml
schema_version: "1.0"
phase: "P4B"
started_at: "2026-08-29T09:00:00Z"
completed_at: "2026-08-30T14:00:00Z"
duration_hours: 13.0
actor: "agent"

metrics:
  services_scored: 14
  iterations_needed: 2
  mode_used: "mixed"                    # agent_defaults | real_workshops | mixed

  automatibility_distribution:
    high_gt_85: 4
    medium_70_85: 7
    low_lt_70: 3
  avg_automatibility: 0.78
  lowest_automatibility: 0.61
  highest_automatibility: 0.94

  per_dimension_averages:
    statement_clarity: 0.82
    algorithm_completeness: 0.71
    integration_definition: 0.76
    data_model_readiness: 0.84
    edge_case_coverage: 0.69

  primary_blockers:
    - "unclear_algorithm"
    - "undocumented_integration"
    - "missing_edge_cases"

  implementation_type_distribution:
    type_a_full_auto: 4
    type_b_assisted: 7
    type_c_manual: 3

  improvement_items:
    working_sessions_planned: 5
    working_sessions_completed: 4
    info_requests_planned: 12
    info_requests_completed: 11
    score_improvement_avg_points: 12.3

  # Placement review (Layer C) — tier decisions made at 4b:
  placement_candidates_reviewed: 11
  placement_decisions:
    app_tier: 5                          # kept app-tier (default confirmed)
    app_with_strategy: 2                 # app-tier with batch/stream/read-model
    db_view: 1
    db_function: 2
    db_proc: 1
    db_trigger: 0
  placement_mode: "mixed"                # agent_defaults | real_workshop | mixed
```

### phase4c-test-suites.yaml

```yaml
schema_version: "1.0"
phase: "P4C"
started_at: "2026-09-01T09:00:00Z"
completed_at: "2026-09-03T17:00:00Z"
duration_hours: 56.0
actor: "agent"

metrics:
  services_with_test_suites: 14
  total_test_cases: 487
  avg_tests_per_service: 34.8
  avg_tests_per_br: 1.4
  br_with_no_tests: 0
  br_with_multiple_tests: 145
  # Implicit-system layer test coverage (A/B/C mandatory case-classes):
  state_machine_test_cases: 28          # illegal-transition / guard / terminal
  invariant_test_cases: 19              # invariant-holds / computed-non-placeholder
  extension_point_test_cases: 10        # resolves-with-config / default-when-unconfigured / udf-roundtrip
  db_tier_object_test_cases: 12         # function/view/trigger behavior + placement-honored
```

### phase5-implementation/service-NNN.yaml

```yaml
schema_version: "1.0"
service_id: "SVC-001"                   # anonymized sequential ID
service_domain: "order-management"      # domain category
implementation_type: "B"                # A | B | C
automatibility_score: 0.82
# credits_used: null                     # DISABLED — not programmatically accessible

timing:
  started_at: "2026-09-05T09:00:00Z"
  first_compile_at: "2026-09-05T11:23:00Z"
  first_test_run_at: "2026-09-05T12:45:00Z"
  all_tests_passing_at: "2026-09-06T16:30:00Z"
  completed_at: "2026-09-07T10:00:00Z"
  total_duration_hours: 49.0

execution_metrics:
  first_pass_compile: true
  first_pass_test_rate: 0.73
  remediation_cycles: 2
  human_interventions: 1
  total_generated_loc: 2340
  test_count: 47

br_metrics:
  br_in_scope: 28
  br_validated: 26
  br_stuck_claims_only: 2
  br_required_remediation: 4

complexity_metrics:
  source_complexity_avg: 18.3
  spec_complexity_avg: 6.1
  ratio: 3.0
  condensation_flags_raised: 1
  condensation_flags_true_positive: 0

confidence_at_completion:
  overall: 0.87
  provenance: 0.91
  implementation: 0.87
  test_quality: 0.88

deviations:
  count: 1
  auto_remediated: 1
  human_resolved: 0
  severity_distribution:
    low: 1
    medium: 0
    high: 0
  types:
    dev_code: 3                         # bugs caught and fixed
    dev_test: 1                         # test adapted (tech debt)
    spec_drift: 0                       # needs BA decision
```

### phase5-implementation/summary.yaml

```yaml
schema_version: "1.0"
phase: "P5"
started_at: "2026-09-05T09:00:00Z"
completed_at: "2026-10-15T17:00:00Z"
duration_hours: 968.0
actor: "mixed"

aggregate:
  services_implemented: 14
  total_generated_loc: 28400
  total_test_count: 487
  overall_first_pass_compile_rate: 0.86
  overall_first_pass_test_rate: 0.68
  avg_remediation_cycles: 2.1
  total_human_interventions: 8
  total_deviations: 12
  deviations_auto_remediated: 9
  deviations_human_resolved: 3

by_implementation_type:
  type_a:
    count: 4
    avg_duration_hours: 24.5
    avg_first_pass_test_rate: 0.82
    avg_remediation_cycles: 1.2
  type_b:
    count: 7
    avg_duration_hours: 52.3
    avg_first_pass_test_rate: 0.67
    avg_remediation_cycles: 2.4
  type_c:
    count: 3
    avg_duration_hours: 120.0
    avg_first_pass_test_rate: null       # not applicable
    avg_remediation_cycles: 0.3

confidence_at_completion:
  avg_overall: 0.85
  avg_provenance: 0.89
  avg_implementation: 0.86
  avg_test_quality: 0.84
  services_below_0_7: 1
```

### phase6-evolution/cycle-NNN.yaml

```yaml
schema_version: "1.0"
cycle_id: "CYC-001"
started_at: "2026-10-20T09:00:00Z"
completed_at: "2026-10-22T14:00:00Z"
duration_hours: 53.0
trigger: "deviation"                    # deviation | bug | feature

metrics:
  items_processed: 3
  br_affected: 5
  specs_updated: 2
  tests_updated: 3
  code_changes: 4
  new_deviations_found: 0
  confidence_before_avg: 0.72
  confidence_after_avg: 0.89
```

---

## Human Interventions Log (Cross-Phase)

### interventions.yaml

This file is **append-only** throughout the engagement. Whenever the human corrects, redirects, or re-requests work from the agent outside the normal SAAM workflow, the agent MUST record it here.

**When to record an intervention:**
- Human says "that's wrong, redo it" (correction)
- Human says "you're not following the steering" (steering violation)
- Human says "you missed these files" (missed files)
- Human says "wrong filename/format" (naming/format)
- Human redirects the agent to a different approach (redirect)
- Human requests a full redo of a service/phase (redo)
- Agent asks a clarification question not defined in the steering protocol (clarification)

**When NOT to record:**
- Normal human-in-the-loop checkpoints (exit gates, BA review, mode selection) — these are defined workflow
- Human providing information the steering says to ask for (RFI responses, tech stack choice)

```yaml
schema_version: "1.0"

interventions:
  - timestamp: "2026-08-11T14:30:00Z"
    phase: "P4"                         # P0 | P1 | P2 | P3 | P4 | P4A | P4B | P4C | P5 | P6
    service: "compliance-service"       # or null if not service-specific
    type: "correction"                  # correction | redirect | redo | clarification | bug_report
    category: "extraction_depth"        # naming | format | extraction_depth | missed_files | wrong_output | steering_violation | context_pressure | other
    description: "Agent summarized Phase 1 instead of deep-reading source"
    resolution: "Re-delegated with explicit instruction to read source files"
    estimated_overhead_minutes: 15

  - timestamp: "2026-08-11T15:00:00Z"
    phase: "P4"
    service: "compliance-service"
    type: "correction"
    category: "missed_files"
    description: "4 source files reported as not found but actually exist"
    resolution: "Searched by concept name, found files, re-extracted"
    estimated_overhead_minutes: 20
```

### Intervention categories

| Category | Meaning | Steering Improvement Signal |
|----------|---------|---------------------------|
| `naming` | Wrong filename, wrong ID format, wrong directory | Naming constraints not emphatic enough |
| `format` | Wrong document structure, missing sections | Template not followed — needs inline in delegation |
| `extraction_depth` | Shallow extraction, summarized instead of deep-reading | Context pressure mitigations insufficient |
| `missed_files` | Files exist but agent reported as missing | Source resolution protocol not followed |
| `wrong_output` | Produced wrong artifact type or content | Delegation prompt unclear |
| `steering_violation` | Agent didn't follow explicit steering instruction | Instruction not prominent enough or context-compacted |
| `context_pressure` | Agent degraded work quality due to context limits | Need subagent delegation or session break |
| `other` | Doesn't fit above categories | Document for pattern analysis |

### Telemetry export (anonymized summary)

When producing per-phase telemetry, aggregate interventions for that phase:

```yaml
# Added to each phase's telemetry YAML:
interventions:
  total: 4
  by_type:
    correction: 3
    redo: 1
  by_category:
    extraction_depth: 2
    missed_files: 1
    naming: 1
  total_overhead_minutes: 65
  overhead_pct_of_phase_duration: 8.2
```

This measures the gap between "autonomous operation" and "actual operation with human correction."

### Agent protocol for recording

After ANY human correction or redirection that wasn't a defined SAAM workflow checkpoint:

1. Acknowledge the correction
2. Fix the issue
3. Append an entry to `.saam/telemetry/interventions.yaml`

The description and resolution fields use generic terms (no client data, no source filenames, no BR-ID text). Categories and counts are safe for telemetry export.

---

## Validation Artifacts as Telemetry Source

The reconciliation pipeline (`validation/run-and-reconcile.sh` + `graph-mcp/scripts/reconcile_validation.py`) produces structured YAML artifacts in `.saam/reconciliation/<service>/` after every test suite run. These artifacts are the **raw input** for Phase 5 per-service telemetry.

### How validation artifacts feed telemetry

```
validation/run-and-reconcile.sh <service>
    ↓ produces
.saam/reconciliation/<service>/validation-run-<id>.yaml  (raw per-run data)
    ↓ consumed by
reconcile_validation.py (updates graph + generates Kiro tasks)
    ↓ at service exit gate, agent aggregates all runs into:
.saam/telemetry/phase5-implementation/service-NNN.yaml  (telemetry export)
```

### Mapping: artifact fields → telemetry fields

| Validation artifact field | Telemetry field | How |
|--------------------------|-----------------|-----|
| First run's `pass_rate` | `first_pass_test_rate` | Value from the earliest validation-run artifact |
| Count of runs | `remediation_cycles` | Number of validation-run artifacts minus 1 |
| First run's `timestamp` | `first_test_run_at` | Timestamp of earliest artifact |
| Run where `pass_rate` = 1.0 | `all_tests_passing_at` | Timestamp of first 100% artifact |
| Last run's `timestamp` | `completed_at` | Timestamp of exit gate artifact |
| Failures in first run | `condensation_flags_true_positive` | If failure maps to a preservation-flagged BR, count it |

### Telemetry production protocol (at service exit gate)

When the agent produces the Phase 5 per-service telemetry YAML, it reads ALL validation artifacts in `.saam/reconciliation/<service>/` (chronologically sorted) and computes:

1. **Timing** — from first artifact's timestamp to last (or from task tracker, whichever is more accurate)
2. **First-pass metrics** — from the FIRST validation artifact (before any fixes)
3. **Remediation count** — total artifacts minus 1
4. **BR coverage** — from the LAST artifact's br_ids_passing / br_ids_failing
5. **Deviation history** — how many deviations were created vs resolved across runs

This means: **the telemetry is derived, not manually entered.** The validation pipeline produces it automatically.

## Export Protocol

Telemetry export is simply: **copy `.saam/telemetry/` from the engagement workspace.**

Since no client-identifying data is present:
- Safe to commit to a private `saam-telemetry` repository
- Safe to upload to shared S3 bucket
- Safe to email as archive

No special export tooling required — the directory structure IS the export format.

## Import Protocol (Central Analytics)

The central analytics repo (`saam-analytics/`) imports telemetry:

```bash
# Copy telemetry folder from engagement
cp -r /path/to/engagement/.saam/telemetry/ saam-analytics/data/raw/ENG-2026-003/

# Run import (loads into DuckDB)
python ingest/import_telemetry.py data/raw/ENG-2026-003/
```

Import is idempotent — re-running with the same engagement ID performs an upsert.

## Calibration Feedback Loop

```
Engagements produce telemetry → imported to central DuckDB →
  statistical analysis (correlations, regressions) →
  produces updated .github/saam-calibration.yaml →
  committed to SAAM repo →
  next engagement uses calibrated values
```

The calibration file is updated when:
- Sample size reaches meaningful thresholds (10+, 30+, 50+ services)
- A specific metric shows statistically significant deviation from current weights
- False positive/negative rates indicate threshold adjustment needed

See `.github/saam-calibration.yaml` for current calibrated values and their provenance.
