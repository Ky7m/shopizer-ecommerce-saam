"""Reconcile validation results with the SAAM Knowledge Graph.

Reads a validation artifact YAML (produced by run-and-reconcile.sh),
updates graph lifecycle states, creates/resolves Deviation nodes,
and generates remediation tasks.

Usage:
  python3 graph-mcp/scripts/reconcile_validation.py <artifact-path>

  Example:
  python3 graph-mcp/scripts/reconcile_validation.py .saam/reconciliation/order-service/validation-run-20260813-143000.yaml

What it does:
  1. Reads the YAML artifact (test results per BR-ID)
  2. Promotes passing BR-IDs: lifecycleState -> "Passing", confidence -> 0.85
  3. Creates Deviation nodes for failing BR-IDs (if not already existing)
  4. Regresses previously-passing BR-IDs that now fail
  5. Updates service completeness (pass_rate, last_validated)
  6. Generates/updates remediation tasks in spec/<service>/tasks.md

Exit codes:
  0 = success (graph updated)
  1 = error (YAML parse failure, Neo4j unavailable)
"""

import json
import os
import sys
import yaml
from datetime import datetime, timezone
from pathlib import Path

from neo4j import GraphDatabase


def _load_env():
    """Load .env from graph-mcp/ if available (has dynamic port)."""
    env_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env")
    if os.path.exists(env_path):
        with open(env_path) as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith("#") and "=" in line:
                    key, value = line.split("=", 1)
                    # .env is AUTHORITATIVE — override stale shell values (not setdefault)
                    os.environ[key.strip()] = value.strip()


_load_env()

NEO4J_URI = os.environ.get("NEO4J_URI") or f"bolt://localhost:{os.environ.get('NEO4J_BOLT_PORT', '7687')}"
NEO4J_USER = os.environ.get("NEO4J_USER", "neo4j")
NEO4J_PASSWORD = os.environ.get("NEO4J_PASSWORD", "saamgraph")


def connect():
    """Connect to Neo4j, return driver."""
    try:
        driver = GraphDatabase.driver(NEO4J_URI, auth=(NEO4J_USER, NEO4J_PASSWORD))
        driver.verify_connectivity()
        return driver
    except Exception as e:
        print(f"ERROR: Cannot connect to Neo4j at {NEO4J_URI}: {e}", file=sys.stderr)
        sys.exit(1)


def parse_artifact(path: str) -> dict:
    """Parse the validation artifact YAML."""
    try:
        with open(path) as f:
            return yaml.safe_load(f)
    except Exception as e:
        print(f"ERROR: Cannot parse artifact {path}: {e}", file=sys.stderr)
        sys.exit(1)


def promote_passing_br_ids(driver, service: str, passing_br_ids: list[str], timestamp: str):
    """Promote passing BR-IDs to 'Passing' lifecycle state with confidence 0.85."""
    if not passing_br_ids:
        return 0

    with driver.session() as session:
        result = session.run("""
            UNWIND $brIds AS brId
            MATCH (br:BusinessRule {brId: brId})
            WHERE br.lifecycleState IN ['Declared', 'Tested', 'Verified']
            SET br.lifecycleState = 'Passing',
                br.implementationConfidence = 0.85,
                br.lastValidatedAt = $timestamp
            RETURN count(br) AS promoted
        """, brIds=passing_br_ids, timestamp=timestamp)
        record = result.single()
        return record["promoted"] if record else 0


def set_behavioral_status(driver, service: str, artifact: dict, timestamp: str):
    """Set BusinessRule.behavioralStatus from behavioral-assertion results (anti-skeleton signal).

    The artifact may carry per-BR behavioral outcomes under test_execution.behavioral:
      { br_id: "real" | "partial" | "stub" | "unimplemented" }
    'stub' = endpoint responded (shape OK) but behavioral assertions failed (state/amount/event).
    This is the distinction TEST_RESULTS shape-checks cannot express. Skip silently if absent.
    """
    behavioral = artifact.get("test_execution", {}).get("behavioral", {})
    if not behavioral:
        return 0
    updated = 0
    with driver.session() as session:
        for br_id, status in behavioral.items():
            if status not in ("unimplemented", "stub", "partial", "real"):
                continue
            r = session.run("""
                MATCH (br:BusinessRule {brId: $brId})
                SET br.behavioralStatus = $status, br.behavioralCheckedAt = $ts
                RETURN br.brId AS id
            """, brId=br_id, status=status, ts=timestamp).single()
            if r:
                updated += 1
    return updated


def create_deviations(driver, service: str, failures: list[dict], run_id: str, timestamp: str):
    """Create Deviation nodes for failing BR-IDs."""
    if not failures:
        return 0

    created = 0
    with driver.session() as session:
        for failure in failures:
            br_id = failure.get("br_id", "UNKNOWN")
            if br_id == "UNKNOWN":
                continue

            reason = failure.get("reason", "assertion failed")
            test_num = failure.get("test_num", "?")

            # Check for an existing OPEN deviation for this BR-ID
            existing = session.run("""
                MATCH (d:Deviation {brId: $brId, status: 'OPEN'})
                RETURN d.id AS id
            """, brId=br_id).single()

            # Check for a previously-RESOLVED deviation that is now failing again = REGRESSION
            regressed = None
            if not existing:
                regressed = session.run("""
                    MATCH (d:Deviation {brId: $brId, status: 'RESOLVED'})
                    RETURN d.id AS id LIMIT 1
                """, brId=br_id).single()

            if regressed:
                # Reopen the resolved deviation and increment regressedCount (loop-stop signal:
                # this fix was applied, passed, then broke again — root cause likely elsewhere).
                row = session.run("""
                    MATCH (d:Deviation {brId: $brId}) WHERE d.status = 'RESOLVED'
                    RETURN d.attemptLog AS attemptLog LIMIT 1
                """, brId=br_id).single()
                try:
                    log = json.loads(row["attemptLog"]) if row and row.get("attemptLog") else []
                except (ValueError, TypeError):
                    log = []
                log.append({"at": timestamp, "reason": reason, "event": "regression"})
                session.run("""
                    MATCH (d:Deviation {brId: $brId}) WHERE d.status = 'RESOLVED'
                    SET d.status = 'OPEN',
                        d.lastSeenAt = $timestamp,
                        d.lastReason = $reason,
                        d.occurrences = coalesce(d.occurrences, 1) + 1,
                        d.regressedCount = coalesce(d.regressedCount, 0) + 1,
                        d.attemptLog = $attemptLog
                """, brId=br_id, timestamp=timestamp, reason=reason, attemptLog=json.dumps(log[-20:]))
                created += 0  # reopened, not created
            elif existing:
                # Update existing deviation: bump occurrences + append to attemptLog (loop-stop history).
                # attemptLog is a JSON array of {at, reason} the fix agent reads to avoid re-trying failed
                # approaches. We read-modify-write in Python (no APOC dependency).
                row = session.run("""
                    MATCH (d:Deviation {brId: $brId, status: 'OPEN'})
                    RETURN d.attemptLog AS attemptLog
                """, brId=br_id).single()
                try:
                    log = json.loads(row["attemptLog"]) if row and row.get("attemptLog") else []
                except (ValueError, TypeError):
                    log = []
                log.append({"at": timestamp, "reason": reason})
                session.run("""
                    MATCH (d:Deviation {brId: $brId, status: 'OPEN'})
                    SET d.lastSeenAt = $timestamp,
                        d.lastReason = $reason,
                        d.occurrences = coalesce(d.occurrences, 1) + 1,
                        d.attemptLog = $attemptLog
                """, brId=br_id, timestamp=timestamp, reason=reason, attemptLog=json.dumps(log[-20:]))
            else:
                # Create new deviation (initialize history fields for loop-stop tracking)
                dev_id = f"DEV-{service}-{br_id}-{run_id}"
                session.run("""
                    CREATE (d:Deviation {
                        id: $devId,
                        brId: $brId,
                        service: $service,
                        status: 'OPEN',
                        type: 'DEV-TEST',
                        reason: $reason,
                        testNum: $testNum,
                        createdAt: $timestamp,
                        lastSeenAt: $timestamp,
                        occurrences: 1,
                        regressedCount: 0,
                        attemptLog: $attemptLog
                    })
                    WITH d
                    OPTIONAL MATCH (br:BusinessRule {brId: $brId})
                    FOREACH (_ IN CASE WHEN br IS NOT NULL THEN [1] ELSE [] END |
                        CREATE (br)-[:HAS_DEVIATION]->(d)
                    )
                """, devId=dev_id, brId=br_id, service=service,
                    reason=reason, testNum=str(test_num), timestamp=timestamp,
                    attemptLog=json.dumps([{"at": timestamp, "reason": reason}]))
                created += 1

    return created


def regress_failing_br_ids(driver, service: str, failing_br_ids: list[str], timestamp: str):
    """Regress previously-passing BR-IDs back to 'Declared' if they now fail."""
    if not failing_br_ids:
        return 0

    with driver.session() as session:
        result = session.run("""
            UNWIND $brIds AS brId
            MATCH (br:BusinessRule {brId: brId})
            WHERE br.lifecycleState = 'Passing'
            SET br.lifecycleState = 'Declared',
                br.implementationConfidence = 0.5,
                br.regressedAt = $timestamp
            RETURN count(br) AS regressed
        """, brIds=failing_br_ids, timestamp=timestamp)
        record = result.single()
        return record["regressed"] if record else 0


def update_service_state(driver, service: str, artifact: dict, timestamp: str):
    """Update service node with latest validation results."""
    test_exec = artifact.get("test_execution", {})
    pass_rate = test_exec.get("pass_rate", 0)
    total = test_exec.get("total", 0)
    passed = test_exec.get("passed", 0)

    with driver.session() as session:
        session.run("""
            MATCH (s:Service)
            WHERE s.name = $service OR s.serviceId = $service
            SET s.integration_pass_rate = $passRate,
                s.integration_tests_total = $total,
                s.integration_tests_passed = $passed,
                s.last_validated_at = $timestamp
        """, service=service, passRate=float(pass_rate),
            total=int(total), passed=int(passed), timestamp=timestamp)


def resolve_passing_deviations(driver, service: str, passing_br_ids: list[str], timestamp: str):
    """Resolve open deviations for BR-IDs that now pass."""
    if not passing_br_ids:
        return 0

    with driver.session() as session:
        result = session.run("""
            UNWIND $brIds AS brId
            MATCH (d:Deviation {brId: brId, status: 'OPEN'})
            SET d.status = 'RESOLVED',
                d.resolvedAt = $timestamp,
                d.resolvedBy = 'validation_pass'
            RETURN count(d) AS resolved
        """, brIds=passing_br_ids, timestamp=timestamp)
        record = result.single()
        return record["resolved"] if record else 0


def check_implicit_layer_gates(driver, service: str) -> list[str]:
    """Recompute the service-level implicit-system structural gates (Layer A/C) and return the
    active blocker codes. A service can pass 100% of its test suite and still be NOT done because
    of a non-closed state machine or an unenforced mandatory-DB integrity invariant — these are
    service-level structural properties, not per-BR test outcomes. Mirrors inference.py
    implicit_layer_gates so reconcile surfaces them even when tests are green.
    """
    with driver.session() as session:
        rec = session.run("""
            MATCH (s:Service)
            WHERE s.name = $service OR s.serviceId = $service
            WITH s LIMIT 1
            OPTIONAL MATCH (s)-[:OWNS]->(:Table)-[:HAS_STATE]->(es:EntityState)
            WHERE coalesce(es.isTerminal, false) = false AND NOT (es)-[:TRANSITIONS_TO]->()
            WITH s, count(DISTINCT es) AS deadEnds
            OPTIONAL MATCH (s)-[:OWNS]->(:Table)-[:HAS_STATE]->(a:EntityState)-[:TRANSITIONS_TO]->(b:EntityState)
            WHERE NOT EXISTS { MATCH (:Table)-[:HAS_STATE]->(b) WHERE b.entity = a.entity }
            WITH s, deadEnds, count(b) AS dangling
            OPTIONAL MATCH (s)-[:OWNS]->(:Table)<-[:CONSTRAINS]-(inv:Invariant)
            WHERE inv.tier IN ['db','both']
              AND NOT EXISTS { MATCH (o:DbObject {enforcesInvariantId: inv.invariantId}) }
            WITH deadEnds, dangling, count(inv) AS missingDb
            RETURN deadEnds, dangling, missingDb
        """, service=service).single()
    if not rec:
        return []
    blockers = []
    if (rec["deadEnds"] or 0) > 0 or (rec["dangling"] or 0) > 0:
        blockers.append("STATE_MACHINE_NOT_CLOSED")
    if (rec["missingDb"] or 0) > 0:
        blockers.append("MANDATORY_DB_OBJECT_MISSING")
    return blockers


def generate_tasks(service: str, failures: list[dict], workspace_root: str):
    """Generate remediation tasks from failures."""
    if not failures:
        return

    specs_base = os.environ.get("SAAM_SPECS_DIR")
    if not specs_base:
        if os.path.isdir(os.path.join(workspace_root, ".github", "specs")):
            specs_base = os.path.join(workspace_root, ".github", "specs")
        elif os.path.isdir(os.path.join(workspace_root, "spec", "microservices")):
            specs_base = os.path.join(workspace_root, "spec", "microservices")
        else:
            specs_base = os.path.join(workspace_root, ".kiro", "specs")

    tasks_dir = os.path.join(specs_base, service)
    os.makedirs(tasks_dir, exist_ok=True)
    tasks_path = os.path.join(tasks_dir, "tasks.md")

    lines = [
        f"# Remediation Tasks: {service}\n",
        f"\nGenerated from validation run. {len(failures)} test(s) failing.\n",
        "\n## Tasks\n",
    ]

    for i, failure in enumerate(failures, 1):
        br_id = failure.get("br_id", "UNKNOWN")
        reason = failure.get("reason", "assertion failed")
        test_num = failure.get("test_num", "?")
        lines.append(f"\n### Task {i}: Fix {br_id} (Test #{test_num})\n")
        lines.append(f"- **BR-ID:** {br_id}\n")
        lines.append(f"- **Failure:** {reason}\n")
        lines.append(f"- **Action:** Read spec for {br_id}, compare with implementation, fix code\n")
        lines.append(f"- **Verify:** Re-run test #{test_num}\n")

    with open(tasks_path, "w") as f:
        f.writelines(lines)

    print(f"  Tasks written: {tasks_path} ({len(failures)} tasks)")


def main():
    if len(sys.argv) < 2:
        print("Usage: reconcile_validation.py <artifact-path>", file=sys.stderr)
        sys.exit(1)

    artifact_path = sys.argv[1]
    artifact = parse_artifact(artifact_path)

    service = artifact.get("service", "unknown")
    run_id = artifact.get("run_id", "unknown")
    timestamp = artifact.get("timestamp", datetime.now(timezone.utc).isoformat())
    test_exec = artifact.get("test_execution", {})
    failures = test_exec.get("failures", [])

    # Extract BR-IDs from test output (stored in artifact by run-and-reconcile.sh)
    # For passing: we need to know which BR-IDs passed
    # The artifact has br_ids_passing count but not the list — we'll use graph inference
    passing_br_ids = []
    failing_br_ids = []

    for failure in failures:
        br_id = failure.get("br_id")
        if br_id and br_id != "UNKNOWN":
            failing_br_ids.append(br_id)

    # Connect to graph
    driver = connect()

    try:
        # If pass_rate is 1.0 (100%), promote ALL service BR-IDs
        pass_rate = float(test_exec.get("pass_rate", 0))
        if pass_rate >= 0.99:
            # All tests pass — promote all BR-IDs for this service
            with driver.session() as session:
                result = session.run("""
                    MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(s:Service)
                    WHERE s.name = $service OR s.serviceId = $service
                    RETURN br.brId AS brId
                """, service=service)
                passing_br_ids = [r["brId"] for r in result if r["brId"]]
        else:
            # Partial pass — get passing BR-IDs by exclusion (all assigned minus failing)
            with driver.session() as session:
                result = session.run("""
                    MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(s:Service)
                    WHERE (s.name = $service OR s.serviceId = $service)
                    AND NOT br.brId IN $failingIds
                    RETURN br.brId AS brId
                """, service=service, failingIds=failing_br_ids)
                passing_br_ids = [r["brId"] for r in result if r["brId"]]

        # Execute reconciliation steps
        promoted = promote_passing_br_ids(driver, service, passing_br_ids, timestamp)
        deviations_created = create_deviations(driver, service, failures, run_id, timestamp)
        regressed = regress_failing_br_ids(driver, service, failing_br_ids, timestamp)
        resolved = resolve_passing_deviations(driver, service, passing_br_ids, timestamp)
        behavioral_set = set_behavioral_status(driver, service, artifact, timestamp)
        update_service_state(driver, service, artifact, timestamp)
        implicit_blockers = check_implicit_layer_gates(driver, service)

        # Print summary
        print(f"\n  [reconcile] Service: {service}")
        print(f"  [reconcile] Pass rate: {pass_rate:.1%} ({test_exec.get('passed', 0)}/{test_exec.get('total', 0)})")
        print(f"  [reconcile] BR-IDs promoted to Passing: {promoted}")
        print(f"  [reconcile] Deviations created: {deviations_created}")
        print(f"  [reconcile] Deviations resolved: {resolved}")
        print(f"  [reconcile] BR-IDs regressed: {regressed}")
        print(f"  [reconcile] Behavioral status set: {behavioral_set}")
        if implicit_blockers:
            print(f"  [reconcile] ⚠ STRUCTURAL BLOCKERS (Layer A/C) — service NOT done even at 100% test pass:")
            for b in implicit_blockers:
                print(f"                {b}")
            print(f"                Fix the state model / add the missing DB object; a passing test suite")
            print(f"                does not clear a structural gate. See saam-signal-precedence.md.")

        # Generate remediation tasks if there are failures
        if failures:
            workspace_root = str(Path(artifact_path).resolve().parents[2])
            generate_tasks(service, failures, workspace_root)

    finally:
        driver.close()


if __name__ == "__main__":
    main()
