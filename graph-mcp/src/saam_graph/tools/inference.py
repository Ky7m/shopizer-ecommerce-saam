"""Graph inference tools: lifecycle states, confidence, unused tables."""

from typing import Any

from mcp.server.fastmcp import FastMCP

from saam_graph.db import GraphDB


def register_inference_tools(server: FastMCP) -> None:
    """Register graph inference tools with the MCP server."""

    @server.tool()
    def graph_run_inferences(rules: list[str] | None = None) -> str:
        """Execute inference rules: lifecycle states, effective confidence, transitive deps, completeness, risk, unused tables, condensed rules, signal status."""
        rules = rules or ["lifecycle_states", "effective_confidence", "transitive_dependencies",
                          "implementation_completeness", "test_coverage", "extraction_risk",
                          "unused_tables", "condensed_rules", "signal_status"]
        results = []

        if "lifecycle_states" in rules:
            results.append(_infer_lifecycle_states())
        if "effective_confidence" in rules:
            results.append(_infer_effective_confidence())
        if "transitive_dependencies" in rules:
            results.append(_infer_transitive_deps())
        if "implementation_completeness" in rules:
            results.append(_infer_impl_completeness())
        if "test_coverage" in rules:
            results.append(_infer_test_coverage())
        if "extraction_risk" in rules:
            results.append(_infer_extraction_risk())
        if "unused_tables" in rules:
            results.append(_infer_unused_tables())
        if "condensed_rules" in rules:
            results.append(_infer_condensed_rules())
        if "signal_status" in rules:
            results.append(_infer_signal_status())

        return "INFERENCE RESULTS:\n\n" + "\n".join(results)

    @server.tool()
    def graph_propagate_confidence(service: str | None = None) -> str:
        """Recalculate confidence scores through the graph. Returns per-service summary."""
        svc_filter = "WHERE s.serviceId = $service" if service else ""
        params = {"service": service} if service else {}

        GraphDB.execute_write(f"""
            MATCH (s:Service) {svc_filter}
            OPTIONAL MATCH (s)<-[:ASSIGNED_TO]-(br:BusinessRule)
            WHERE EXISTS {{ MATCH (d:Decision)-[:DECIDED_AS]->(br) WHERE d.classification IN ['Core', 'Active'] }}
            WITH s, collect(coalesce(br.effectiveConfidence, br._confidence, 0.5)) AS confidences
            SET s._confidence = CASE WHEN size(confidences) > 0
                THEN reduce(sum = 0.0, c IN confidences | sum + c) / size(confidences) ELSE 0.0 END
        """, params)

        results = GraphDB.execute_read(f"""
            MATCH (s:Service) {svc_filter}
            RETURN s.name AS name, s.serviceId AS id, s._confidence AS confidence
            ORDER BY s._confidence ASC
        """, params)

        output = "CONFIDENCE PROPAGATION:\n"
        for r in results:
            conf = r["confidence"] or 0
            output += f"  {r['name']} ({r['id']}): {conf:.2f}\n"
        return output

    @server.tool()
    def graph_detect_unused_tables() -> str:
        """Find tables where ALL BR-IDs are Obsolete or Deferred."""
        result = GraphDB.execute_read("""
            MATCH (t:Table)<-[:OWNS]-(s:Service)<-[:ASSIGNED_TO]-(br:BusinessRule)
            WITH t, s, collect(br) AS rules
            WHERE size(rules) > 0
            AND ALL(r IN rules WHERE EXISTS {
                MATCH (d:Decision)-[:DECIDED_AS]->(r) WHERE d.classification IN ['Obsolete', 'Deferred']
            })
            RETURN t.name AS tableName, s.name AS service, size(rules) AS rulesCount
        """)

        if not result:
            return "No unused tables detected."

        output = f"UNUSED TABLES ({len(result)} candidates):\n"
        for r in result:
            output += f"  - {r['tableName']} (service: {r['service']}, {r['rulesCount']} excluded rules)\n"
        return output

    @server.tool()
    def graph_detect_condensed_rules(service: str | None = None, threshold: float = 3.0) -> str:
        """Detect rules with semantic preservation gaps using 8-dimension vector comparison. Finds components where business-relevant dimensions (not just control-flow) have significant gaps between source and spec."""
        svc_filter = "AND s.serviceId = $service" if service else ""
        params: dict[str, Any] = {"threshold": threshold}
        if service:
            params["service"] = service

        # Compare each dimension per component (aggregate spec across rules from same component)
        result = GraphDB.execute_read(f"""
            MATCH (br:BusinessRule)-[:EXTRACTED_FROM]->(sc:SourceComponent)
            MATCH (br)-[:ASSIGNED_TO]->(s:Service)
            WHERE sc.srcControlFlow IS NOT NULL {svc_filter}
            WITH sc, s, collect(br) AS rules,
                 sc.srcControlFlow AS sCtrl, sc.srcDataFlow AS sData,
                 sc.srcConstants AS sConst, sc.srcStateTransitions AS sStates,
                 sc.srcOutcomes AS sOut, sc.srcDataWrites AS sWrites,
                 sc.srcIntegrations AS sInteg, sc.srcErrorPaths AS sErr
            WITH sc, s, rules, sCtrl, sData, sConst, sStates, sOut, sWrites, sInteg, sErr,
                 reduce(t=0, r IN rules | t + coalesce(r.specControlFlow, 0)) AS spCtrl,
                 reduce(t=0, r IN rules | t + coalesce(r.specDataFlow, 0)) AS spData,
                 reduce(t=0, r IN rules | t + coalesce(r.specConstants, 0)) AS spConst,
                 reduce(t=0, r IN rules | t + coalesce(r.specStateTransitions, 0)) AS spStates,
                 reduce(t=0, r IN rules | t + coalesce(r.specOutcomes, 0)) AS spOut,
                 reduce(t=0, r IN rules | t + coalesce(r.specDataWrites, 0)) AS spWrites,
                 reduce(t=0, r IN rules | t + coalesce(r.specIntegrations, 0)) AS spInteg,
                 reduce(t=0, r IN rules | t + coalesce(r.specErrorPaths, 0)) AS spErr
            WITH sc, s, rules,
                 CASE WHEN coalesce(sData,0) > 0 AND spData = 0 THEN 'CRITICAL' WHEN coalesce(sData,0) > 0 AND toFloat(sData)/spData > $threshold THEN 'FLAGGED' ELSE 'OK' END AS dataStatus,
                 CASE WHEN coalesce(sConst,0) > 0 AND spConst = 0 THEN 'CRITICAL' WHEN coalesce(sConst,0) > 0 AND toFloat(sConst)/spConst > $threshold THEN 'FLAGGED' ELSE 'OK' END AS constStatus,
                 CASE WHEN coalesce(sStates,0) > 0 AND spStates = 0 THEN 'CRITICAL' WHEN coalesce(sStates,0) > 0 AND toFloat(sStates)/spStates > $threshold THEN 'FLAGGED' ELSE 'OK' END AS statesStatus,
                 CASE WHEN coalesce(sOut,0) > 0 AND spOut = 0 THEN 'CRITICAL' WHEN coalesce(sOut,0) > 0 AND toFloat(sOut)/spOut > $threshold THEN 'FLAGGED' ELSE 'OK' END AS outStatus,
                 CASE WHEN coalesce(sWrites,0) > 0 AND spWrites = 0 THEN 'CRITICAL' WHEN coalesce(sWrites,0) > 0 AND toFloat(sWrites)/spWrites > $threshold THEN 'FLAGGED' ELSE 'OK' END AS writesStatus,
                 CASE WHEN coalesce(sInteg,0) > 0 AND spInteg = 0 THEN 'CRITICAL' WHEN coalesce(sInteg,0) > 0 AND toFloat(sInteg)/spInteg > $threshold THEN 'FLAGGED' ELSE 'OK' END AS integStatus,
                 CASE WHEN coalesce(sErr,0) > 0 AND spErr = 0 THEN 'CRITICAL' WHEN coalesce(sErr,0) > 0 AND toFloat(sErr)/spErr > $threshold THEN 'FLAGGED' ELSE 'OK' END AS errStatus,
                 CASE WHEN coalesce(sCtrl,0) > 0 AND spCtrl = 0 THEN 'CRITICAL' WHEN coalesce(sCtrl,0) > 0 AND toFloat(sCtrl)/spCtrl > $threshold THEN 'FLAGGED' ELSE 'OK' END AS ctrlStatus
            WHERE dataStatus <> 'OK' OR constStatus <> 'OK' OR statesStatus <> 'OK' OR outStatus <> 'OK' OR writesStatus <> 'OK' OR integStatus <> 'OK' OR errStatus <> 'OK'
                  OR (ctrlStatus <> 'OK' AND (dataStatus <> 'OK' OR constStatus <> 'OK' OR statesStatus <> 'OK'))
            RETURN sc.name AS component, s.name AS service, s.serviceId AS serviceId,
                   size(rules) AS ruleCount, [r IN rules | r.brId] AS ruleIds,
                   ctrlStatus, dataStatus, constStatus, statesStatus, outStatus, writesStatus, integStatus, errStatus
            ORDER BY integStatus DESC, constStatus DESC, errStatus DESC
        """, params)

        if not result:
            scope = f" for service {service}" if service else ""
            return f"No semantic preservation gaps detected{scope}. All business dimensions preserved."

        output = f"SEMANTIC PRESERVATION GAPS ({len(result)} components):\n\n"
        for r in result:
            flagged_dims = []
            for dim, status in [("control-flow", r["ctrlStatus"]), ("data-flow", r["dataStatus"]),
                                ("constants", r["constStatus"]), ("states", r["statesStatus"]),
                                ("outcomes", r["outStatus"]), ("writes", r["writesStatus"]),
                                ("integrations", r["integStatus"]), ("errors", r["errStatus"])]:
                if status != "OK":
                    flagged_dims.append(f"{dim}={status}")

            # Control-flow alone is NOT a signal (infrastructure noise)
            non_ctrl_flags = [d for d in flagged_dims if not d.startswith("control-flow")]
            if not non_ctrl_flags:
                continue  # Only control-flow flagged → skip (infra noise)

            output += f"  {r['component']} ({r['service']}):\n"
            output += f"    Gaps: {', '.join(flagged_dims)}\n"
            output += f"    Rules ({r['ruleCount']}): {', '.join(r['ruleIds'][:5])}\n"
            if non_ctrl_flags:
                output += f"    ACTION: Re-extract focusing on: {', '.join(non_ctrl_flags)}\n"
            output += "\n"

        return output


def _infer_lifecycle_states() -> str:
    """Advance lifecycle states based on graph relationships."""
    r1 = GraphDB.execute_write("""
        MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(:Service)
        WHERE br.lifecycleState = 'Extracted' OR br.lifecycleState IS NULL
        SET br.lifecycleState = 'Assigned'
        RETURN count(br) AS n
    """)
    r2 = GraphDB.execute_write("""
        MATCH (br:BusinessRule)-[:CLAIMS_IMPLEMENTATION]->()
        WHERE br.lifecycleState IN ['Extracted', 'Assigned']
        SET br.lifecycleState = 'Declared', br.implementationConfidence = coalesce(br.implementationConfidence, 0.5)
        RETURN count(br) AS n
    """)
    r3 = GraphDB.execute_write("""
        MATCH (br:BusinessRule)-[:TESTED_BY]->(:TestAssertion)
        WHERE br.lifecycleState = 'Declared'
        SET br.lifecycleState = 'Tested', br.testQualityConfidence = coalesce(br.testQualityConfidence, 0.6)
        RETURN count(br) AS n
    """)
    r4 = GraphDB.execute_write("""
        MATCH (br:BusinessRule)-[:TESTED_BY]->(ta:TestAssertion)
        WHERE br.lifecycleState IN ['Declared', 'Tested']
        WITH br, collect(ta.status) AS statuses
        WHERE ALL(s IN statuses WHERE s = 'PASS') AND size(statuses) > 0
        SET br.lifecycleState = 'Passing', br.implementationConfidence = 0.9, br.testQualityConfidence = 0.9
        RETURN count(br) AS n
    """)

    counts = [r[0]["n"] if r else 0 for r in [r1, r2, r3, r4]]
    return f"  [lifecycle] Assigned:{counts[0]}, Declared:{counts[1]}, Tested:{counts[2]}, Passing:{counts[3]}"


def _infer_effective_confidence() -> str:
    """Calculate effectiveConfidence as min of active dimensions."""
    GraphDB.execute_write("""
        MATCH (br:BusinessRule) WHERE br.provenanceConfidence IS NULL AND br._confidence IS NOT NULL
        SET br.provenanceConfidence = br._confidence
    """)
    result = GraphDB.execute_write("""
        MATCH (br:BusinessRule) WHERE br.provenanceConfidence IS NOT NULL
        WITH br, br.provenanceConfidence AS prov, br.implementationConfidence AS impl, br.testQualityConfidence AS tq
        SET br.effectiveConfidence = CASE
            WHEN impl IS NOT NULL AND tq IS NOT NULL
                THEN reduce(m = 1.0, v IN [prov, impl, tq] | CASE WHEN v < m THEN v ELSE m END)
            WHEN impl IS NOT NULL THEN CASE WHEN prov < impl THEN prov ELSE impl END
            ELSE prov END
        RETURN count(br) AS updated, avg(br.effectiveConfidence) AS avg
    """)
    if result and result[0]["updated"] > 0:
        return f"  [confidence] Updated {result[0]['updated']} rules, avg={result[0]['avg']:.2f}"
    return "  [confidence] No rules with provenance set"


def _infer_transitive_deps() -> str:
    """Compute transitive service dependencies."""
    GraphDB.execute_write("MATCH ()-[r:TRANSITIVELY_DEPENDS_ON]->() DELETE r")
    result = GraphDB.execute_write("""
        MATCH path = (a:Service)-[:CALLS*2..5]->(c:Service) WHERE a <> c
        WITH DISTINCT a, c, min(length(path)) AS hops
        MERGE (a)-[r:TRANSITIVELY_DEPENDS_ON]->(c) SET r.hops = hops
        RETURN count(r) AS created
    """)
    count = result[0]["created"] if result else 0
    return f"  [transitive_deps] Created {count} edges"


def _infer_impl_completeness() -> str:
    """Calculate implementation completeness per service."""
    result = GraphDB.execute_write("""
        MATCH (s:Service)
        OPTIONAL MATCH (s)<-[:ASSIGNED_TO]-(br:BusinessRule)
        WITH s, count(br) AS total
        OPTIONAL MATCH (s)<-[:ASSIGNED_TO]-(br2:BusinessRule)-[:CLAIMS_IMPLEMENTATION]->()
        WITH s, total, count(br2) AS impl
        SET s.implementationCompleteness = CASE WHEN total > 0 THEN toFloat(impl) / total ELSE 0.0 END
        RETURN s.name AS name, total, impl
    """)
    output = "  [impl_completeness]"
    for r in (result or []):
        output += f" {r['name']}:{r['impl']}/{r['total']}"
    return output


def _infer_test_coverage() -> str:
    """Calculate test coverage per service."""
    result = GraphDB.execute_write("""
        MATCH (s:Service)
        OPTIONAL MATCH (s)<-[:ASSIGNED_TO]-(br:BusinessRule)-[:TESTED_BY]->(ta:TestAssertion)
        WHERE ta.status = 'PASS'
        WITH s, count(DISTINCT br) AS tested
        OPTIONAL MATCH (s)<-[:ASSIGNED_TO]-(br2:BusinessRule)
        WITH s, tested, count(br2) AS total
        SET s.testCoverage = CASE WHEN total > 0 THEN toFloat(tested) / total ELSE 0.0 END
        RETURN s.name AS name, tested, total
    """)
    output = "  [test_coverage]"
    for r in (result or []):
        output += f" {r['name']}:{r['tested']}/{r['total']}"
    return output


def _infer_extraction_risk() -> str:
    """Flag high-risk extractions."""
    result = GraphDB.execute_write("""
        MATCH (br:BusinessRule)-[:EXTRACTED_FROM]->(sc:SourceComponent)
        WHERE br._confidence < 0.7 AND sc.complexity > 20
        SET br.extractionRisk = 'High'
        RETURN count(br) AS n
    """)
    return f"  [extraction_risk] Flagged {result[0]['n'] if result else 0} high-risk rules"


def _infer_unused_tables() -> str:
    """Mark removal candidate tables."""
    result = GraphDB.execute_write("""
        MATCH (t:Table)<-[:OWNS]-(s:Service)<-[:ASSIGNED_TO]-(br:BusinessRule)
        WITH t, collect(br) AS rules
        WHERE size(rules) > 0 AND ALL(r IN rules WHERE EXISTS {
            MATCH (d:Decision)-[:DECIDED_AS]->(r) WHERE d.classification IN ['Obsolete', 'Deferred']
        })
        SET t.candidateForRemoval = true
        RETURN count(t) AS n
    """)
    return f"  [unused_tables] {result[0]['n'] if result else 0} candidates"


def _infer_condensed_rules() -> str:
    """Detect semantic preservation gaps across all services using 8-dimension vector."""
    # Check each business-relevant dimension (skip control-flow-only flags)
    dims = [
        ("srcDataFlow", "specDataFlow", "data-flow"),
        ("srcConstants", "specConstants", "constants"),
        ("srcStateTransitions", "specStateTransitions", "states"),
        ("srcOutcomes", "specOutcomes", "outcomes"),
        ("srcDataWrites", "specDataWrites", "writes"),
        ("srcIntegrations", "specIntegrations", "integrations"),
        ("srcErrorPaths", "specErrorPaths", "errors"),
    ]

    total_flagged = 0
    for src_field, spec_field, dim_name in dims:
        result = GraphDB.execute_write(f"""
            MATCH (br:BusinessRule)-[:EXTRACTED_FROM]->(sc:SourceComponent)
            WHERE sc.{src_field} IS NOT NULL AND sc.{src_field} > 0
            AND (br.{spec_field} IS NULL OR br.{spec_field} = 0)
            AND (br.preservationFlag IS NULL OR br.preservationFlag <> 'unresolved')
            SET br.preservationFlag = 'flagged',
                br.flaggedDimensions = CASE
                    WHEN br.flaggedDimensions IS NULL THEN '{dim_name}'
                    WHEN NOT '{dim_name}' IN br.flaggedDimensions THEN br.flaggedDimensions + ',{dim_name}'
                    ELSE br.flaggedDimensions END
            RETURN count(br) AS n
        """)
        flagged = result[0]["n"] if result else 0
        total_flagged += flagged

    if total_flagged == 0:
        return "  [semantic_preservation] No gaps detected — all business dimensions preserved"
    return f"  [semantic_preservation] {total_flagged} rules flagged across business dimensions (CRITICAL: spec=0 where source>0)"


def _infer_signal_status() -> str:
    """Compute signalStatus for each BusinessRule based on the precedence model.

    Gates (block progress):
      - TEST_FAILING: any TESTED_BY assertion has status=FAIL
      - MUTATION_SURVIVED: Critical rule with mutationKillRate < 1.0
      - SPEC_DRIFT_CRITICAL: Critical rule with spec hash mismatch on CLAIMS_IMPLEMENTATION edge
      - OPEN_DEVIATION: open deviation exists against this rule

    Flags (inform, don't block):
      - SPEC_DRIFT: non-Critical rule with spec hash mismatch
      - WEAK_TEST: non-Critical rule with mutationKillRate < 1.0
      - LOW_CONFIDENCE: effectiveConfidence < 0.7
    """
    # Step 1: Evaluate signal status per BusinessRule
    result = GraphDB.execute_write("""
        MATCH (br:BusinessRule)
        WHERE br.lifecycleState IS NOT NULL

        // Get classification
        OPTIONAL MATCH (d:Decision)-[:DECIDED_AS]->(br)
        WITH br, d,
             (d.classification = 'Core' AND d.weight = 'Critical') AS isCritical

        // Check for failing tests
        OPTIONAL MATCH (br)-[:TESTED_BY]->(failTa:TestAssertion)
        WHERE failTa.status = 'FAIL'
        WITH br, d, isCritical, count(failTa) > 0 AS hasFailingTest

        // Check for spec drift (hash mismatch on CLAIMS_IMPLEMENTATION edge)
        OPTIONAL MATCH (br)-[ci:CLAIMS_IMPLEMENTATION]->()
        WITH br, d, isCritical, hasFailingTest,
             (ci IS NOT NULL AND ci.specHash IS NOT NULL AND br.specHash IS NOT NULL AND ci.specHash <> br.specHash) AS hasSpecDrift

        // Check for open deviations
        OPTIONAL MATCH (dev:Deviation)-[:DEVIATES_FROM]->(br)
        WHERE dev.status = 'OPEN'
        WITH br, isCritical, hasFailingTest, hasSpecDrift, count(dev) > 0 AS hasOpenDeviation

        // Compute gates
        WITH br, isCritical, hasFailingTest, hasSpecDrift, hasOpenDeviation,
             br.mutationKillRate AS mutationRate,
             br.effectiveConfidence AS confidence

        WITH br, isCritical, hasFailingTest, hasSpecDrift, hasOpenDeviation, mutationRate, confidence,
             CASE WHEN hasFailingTest THEN 'TEST_FAILING' ELSE null END AS g_test,
             CASE WHEN isCritical AND mutationRate IS NOT NULL AND mutationRate < 1.0 THEN 'MUTATION_SURVIVED' ELSE null END AS g_mutation,
             CASE WHEN isCritical AND hasSpecDrift THEN 'SPEC_DRIFT_CRITICAL' ELSE null END AS g_drift,
             CASE WHEN hasOpenDeviation THEN 'OPEN_DEVIATION' ELSE null END AS g_deviation

        // Compute flags
        WITH br, isCritical, hasSpecDrift, mutationRate, confidence,
             [x IN [g_test, g_mutation, g_drift, g_deviation] WHERE x IS NOT NULL] AS blockers,
             CASE WHEN NOT isCritical AND hasSpecDrift THEN 'SPEC_DRIFT' ELSE null END AS f_drift,
             CASE WHEN NOT isCritical AND mutationRate IS NOT NULL AND mutationRate < 1.0 THEN 'WEAK_TEST' ELSE null END AS f_mutation,
             CASE WHEN confidence IS NOT NULL AND confidence < 0.7 THEN 'LOW_CONFIDENCE' ELSE null END AS f_confidence

        WITH br, blockers,
             [x IN [f_drift, f_mutation, f_confidence] WHERE x IS NOT NULL] AS flags

        SET br.signalStatus = CASE
                WHEN size(blockers) > 0 THEN 'BLOCKED'
                WHEN size(flags) > 0 THEN 'FLAGGED'
                ELSE 'CLEAR'
            END,
            br.signalBlockers = blockers,
            br.signalFlags = flags,
            br._signalUpdatedAt = datetime()

        RETURN
            count(CASE WHEN br.signalStatus = 'BLOCKED' THEN 1 END) AS blocked,
            count(CASE WHEN br.signalStatus = 'FLAGGED' THEN 1 END) AS flagged,
            count(CASE WHEN br.signalStatus = 'CLEAR' THEN 1 END) AS clear
    """)

    blocked = result[0]["blocked"] if result else 0
    flagged = result[0]["flagged"] if result else 0
    clear = result[0]["clear"] if result else 0

    # Step 1b: Implicit-system layer structural gates (A/C) — service-level, not per-BR.
    # STATE_MACHINE_NOT_CLOSED: an owned entity has a dangling transition (target state not declared)
    #   or a non-terminal state with no outgoing transition (dead end).
    # MANDATORY_DB_OBJECT_MISSING: an integrity invariant (tier db/both) with no enforcing DbObject.
    GraphDB.execute_write("""
        MATCH (s:Service)
        // dead-end states: owned, non-terminal, no outgoing transition
        OPTIONAL MATCH (s)-[:OWNS]->(:Table)-[:HAS_STATE]->(es:EntityState)
        WHERE coalesce(es.isTerminal, false) = false AND NOT (es)-[:TRANSITIONS_TO]->()
        WITH s, count(DISTINCT es) AS deadEnds
        // dangling transitions: a transition from an owned entity's state to a state NOT owned by the
        // same entity set (target not declared). Compare within the same entity.
        OPTIONAL MATCH (s)-[:OWNS]->(:Table)-[:HAS_STATE]->(a:EntityState)-[:TRANSITIONS_TO]->(b:EntityState)
        WHERE NOT EXISTS { MATCH (:Table)-[:HAS_STATE]->(b) WHERE b.entity = a.entity }
        WITH s, deadEnds, count(b) AS dangling
        // integrity invariants (db/both) with no enforcing DbObject
        OPTIONAL MATCH (s)-[:OWNS]->(:Table)<-[:CONSTRAINS]-(inv:Invariant)
        WHERE inv.tier IN ['db','both']
          AND NOT EXISTS { MATCH (o:DbObject {enforcesInvariantId: inv.invariantId}) }
        WITH s, deadEnds, dangling, count(inv) AS missingDb
        SET s.implicitBlockers =
            [x IN [
                CASE WHEN deadEnds > 0 OR dangling > 0 THEN 'STATE_MACHINE_NOT_CLOSED' ELSE null END,
                CASE WHEN missingDb > 0 THEN 'MANDATORY_DB_OBJECT_MISSING' ELSE null END
            ] WHERE x IS NOT NULL]
    """)

    # Step 2: Service-level rollup — includes implicit-layer structural blockers
    GraphDB.execute_write("""
        MATCH (s:Service)
        OPTIONAL MATCH (s)<-[:ASSIGNED_TO]-(br:BusinessRule)
        WITH s,
             count(CASE WHEN br.signalStatus = 'BLOCKED' THEN 1 END) AS blockedCount,
             count(CASE WHEN br.signalStatus = 'FLAGGED' THEN 1 END) AS flaggedCount,
             size(coalesce(s.implicitBlockers, [])) AS implicitBlocked
        SET s.signalStatus = CASE
                WHEN blockedCount > 0 OR implicitBlocked > 0 THEN 'BLOCKED'
                WHEN flaggedCount > 0 THEN 'FLAGGED'
                ELSE 'CLEAR'
            END,
            s.signalBlockedCount = blockedCount + implicitBlocked,
            s.signalFlaggedCount = flaggedCount,
            s._signalUpdatedAt = datetime()
    """)

    return f"  [signal_status] BLOCKED:{blocked} FLAGGED:{flagged} CLEAR:{clear}"
