"""Graph reconciliation tools: compare SAAM graph against CAST graph."""

import json
from typing import Any

from mcp.server.fastmcp import FastMCP

from saam_graph.db import GraphDB


def register_reconciliation_tools(server: FastMCP) -> None:
    """Register graph reconciliation tools with the MCP server."""

    @server.tool()
    def graph_extraction_coverage(minComplexity: int = 0) -> str:
        """Find CAST business-layer components with no extracted BR-IDs.

        DENOMINATOR = the FULL CAST business-layer inventory (every SourceComponent with
        businessLayer <> false), NOT only the components SAAM chose to ingest a rule from.
        This is the fix for the wrong-denominator bug: previously the query counted only
        components that already had a SourceComponent node created during extraction, so a
        never-walked component was invisible and coverage falsely reported ~100%. With the
        full inventory ingested at Phase 1 (see saam-graph-validation.md), a component with
        NO EXTRACTED_FROM edge is a real gap and is counted here.

        minComplexity defaults to 0 (count the whole business layer). Raise it only to focus
        the printed gap list on heavier components — it does NOT change what 'business layer'
        means (that is businessLayer, not a complexity threshold).
        """
        gaps = GraphDB.execute_read("""
            MATCH (sc:SourceComponent)
            WHERE coalesce(sc.businessLayer, true) = true
            AND (sc.isDeadCode IS NULL OR sc.isDeadCode = false)
            AND coalesce(sc.complexity, 0) >= $min
            AND NOT EXISTS { MATCH (:BusinessRule)-[:EXTRACTED_FROM]->(sc) }
            RETURN sc.name AS name, sc.complexity AS complexity, sc.module AS module,
                   coalesce(sc.intentCategory, 'unknown') AS intent
            ORDER BY coalesce(sc.complexity, 0) DESC
        """, {"min": minComplexity})

        total = GraphDB.execute_read("""
            MATCH (sc:SourceComponent)
            WHERE coalesce(sc.businessLayer, true) = true
            AND (sc.isDeadCode IS NULL OR sc.isDeadCode = false)
            AND coalesce(sc.complexity, 0) >= $min
            RETURN count(sc) AS total
        """, {"min": minComplexity})

        t = total[0]["total"] if total else 0
        covered = t - len(gaps)
        pct = (covered / t * 100) if t > 0 else 0

        output = f"EXTRACTION COVERAGE (business layer, complexity >= {minComplexity}):\n"
        output += f"  Denominator = full CAST business inventory: {t}\n"
        output += f"  Covered: {covered}, Gaps (no BR extracted): {len(gaps)}, Coverage: {pct:.1f}%\n"
        if t == 0:
            output += ("  WARNING: denominator is 0 — the full CAST inventory was NOT ingested. "
                       "This coverage number is MEANINGLESS until Phase 1 ingests every business "
                       "component as a SourceComponent (see saam-graph-validation.md). Do NOT read "
                       "this as 'nothing to cover'.\n")
        for g in gaps[:15]:
            output += f"    - {g['name']} (complexity={g['complexity']}, intent={g['intent']}, module={g.get('module', '?')})\n"
        return output

    @server.tool()
    def graph_assignment_coverage() -> str:
        """Find BR-IDs not assigned to any service (excluding Obsolete/Deferred)."""
        orphans = GraphDB.execute_read("""
            MATCH (br:BusinessRule)
            WHERE NOT EXISTS { MATCH (br)-[:ASSIGNED_TO]->(:Service) }
            AND NOT EXISTS { MATCH (d:Decision)-[:DECIDED_AS]->(br) WHERE d.classification IN ['Obsolete', 'Deferred'] }
            RETURN br.brId AS brId, br.statement AS statement
            ORDER BY br.brId
        """)

        total = GraphDB.execute_read("MATCH (br:BusinessRule) RETURN count(br) AS total")
        t = total[0]["total"] if total else 0
        pct = ((t - len(orphans)) / t * 100) if t > 0 else 0

        output = f"ASSIGNMENT COVERAGE: {t - len(orphans)}/{t} ({pct:.1f}%), Orphaned: {len(orphans)}\n"
        for o in orphans[:10]:
            output += f"    - {o['brId']}: {(o.get('statement', '') or '')[:60]}\n"
        return output

    @server.tool()
    def graph_implementation_coverage(service: str | None = None) -> str:
        """Find Active/Core BR-IDs without passing tests."""
        svc_filter = "AND s.serviceId = $service" if service else ""
        params = {"service": service} if service else {}

        gaps = GraphDB.execute_read(f"""
            MATCH (d:Decision)-[:DECIDED_AS]->(br:BusinessRule)-[:ASSIGNED_TO]->(s:Service)
            WHERE d.classification IN ['Core', 'Active'] {svc_filter}
            AND NOT EXISTS {{ MATCH (br)-[:TESTED_BY]->(ta:TestAssertion) WHERE ta.status = 'PASS' }}
            RETURN br.brId AS brId, s.name AS service, d.classification AS classification
            ORDER BY br.brId
        """, params)

        output = f"IMPLEMENTATION COVERAGE: {len(gaps)} Active/Core rules without passing tests\n"
        for g in gaps[:20]:
            output += f"    - {g['brId']} [{g['classification']}] in {g['service']}\n"
        return output

    @server.tool()
    def graph_unaccounted_loss(minComplexity: int = 0) -> str:
        """Master query: business logic neither extracted+implemented NOR explicitly excluded.

        DENOMINATOR = the FULL CAST business-layer inventory (businessLayer <> false), not the
        ingested subset. A component is UNACCOUNTED when it is business-layer, not dead code, and:
          - has NO BusinessRule extracted from it (never walked — the dominant miss class), OR
          - its rules exist but none is implemented AND none is explicitly Obsolete/Deferred.

        This is the corrected zero-unaccounted-loss guarantee. It is only meaningful when the full
        CAST inventory was ingested at Phase 1 — otherwise the denominator is the ingested subset
        and a 100%/ZERO reading is FALSE (the historical bug). minComplexity defaults to 0 so the
        whole business layer is measured; raise it only to focus the printed list.
        """
        gaps = GraphDB.execute_read("""
            MATCH (sc:SourceComponent)
            WHERE coalesce(sc.businessLayer, true) = true
            AND (sc.isDeadCode IS NULL OR sc.isDeadCode = false)
            AND coalesce(sc.complexity, 0) >= $min
            WITH sc
            OPTIONAL MATCH (br:BusinessRule)-[:EXTRACTED_FROM]->(sc)
            WITH sc, collect(br) AS rules
            WHERE size(rules) = 0
            OR ANY(r IN rules WHERE
                NOT EXISTS { MATCH (r)-[:CLAIMS_IMPLEMENTATION]->() }
                AND NOT EXISTS { MATCH (d:Decision)-[:DECIDED_AS]->(r) WHERE d.classification IN ['Obsolete', 'Deferred'] }
            )
            RETURN sc.name AS name, coalesce(sc.complexity, 0) AS complexity,
                   coalesce(sc.intentCategory, 'unknown') AS intent, size(rules) AS extractedRules
            ORDER BY complexity DESC
        """, {"min": minComplexity})

        total = GraphDB.execute_read("""
            MATCH (sc:SourceComponent)
            WHERE coalesce(sc.businessLayer, true) = true
            AND (sc.isDeadCode IS NULL OR sc.isDeadCode = false)
            AND coalesce(sc.complexity, 0) >= $min
            RETURN count(sc) AS total
        """, {"min": minComplexity})

        t = total[0]["total"] if total else 0
        pct = ((t - len(gaps)) / t * 100) if t > 0 else 0

        output = f"UNACCOUNTED LOSS: {len(gaps)}/{t} business components unaccounted ({pct:.1f}% accountability)\n"
        if t == 0:
            output += ("  WARNING: denominator is 0 — the full CAST business inventory was NOT ingested. "
                       "A 'ZERO UNACCOUNTED LOSS' here would be FALSE: the query is comparing against an "
                       "empty set, not the legacy. Ingest the full inventory at Phase 1 first "
                       "(saam-graph-validation.md). Do NOT sign off on this.\n")
        elif len(gaps) == 0:
            output += "  ZERO UNACCOUNTED LOSS (against the full CAST business inventory)\n"

        # Coverage SHAPE — where the gap concentrates by intent (posting captured vs entry/derive/distribute missed).
        shape = GraphDB.execute_read("""
            MATCH (sc:SourceComponent)
            WHERE coalesce(sc.businessLayer, true) = true
            AND (sc.isDeadCode IS NULL OR sc.isDeadCode = false)
            AND coalesce(sc.complexity, 0) >= $min
            WITH coalesce(sc.intentCategory, 'unknown') AS intent, sc,
                 EXISTS { MATCH (:BusinessRule)-[:EXTRACTED_FROM]->(sc) } AS hasRule
            RETURN intent,
                   count(sc) AS total,
                   sum(CASE WHEN hasRule THEN 1 ELSE 0 END) AS covered,
                   sum(coalesce(sc.complexity, 0)) AS complexityTotal,
                   sum(CASE WHEN hasRule THEN coalesce(sc.complexity, 0) ELSE 0 END) AS complexityCovered
            ORDER BY total DESC
        """, {"min": minComplexity})
        if shape:
            output += "  Coverage shape by intent (count | complexity-weighted):\n"
            for s in shape:
                c_pct = (s["covered"] / s["total"] * 100) if s["total"] else 0
                cw_pct = (s["complexityCovered"] / s["complexityTotal"] * 100) if s["complexityTotal"] else 0
                output += (f"    - {s['intent']}: {s['covered']}/{s['total']} ({c_pct:.0f}%) "
                           f"| cx {cw_pct:.0f}%\n")

        for g in gaps[:20]:
            output += f"    - {g['name']} (complexity={g['complexity']}, intent={g['intent']}, rules={g['extractedRules']})\n"
        return output

    @server.tool()
    def graph_call_pattern_preservation() -> str:
        """Compare CAST call graph against modernized service dependencies."""
        results = GraphDB.execute_read("""
            MATCH (sc1:SourceComponent)-[:SOURCE_CALLS]->(sc2:SourceComponent)
            MATCH (br1:BusinessRule)-[:EXTRACTED_FROM]->(sc1)
            MATCH (br2:BusinessRule)-[:EXTRACTED_FROM]->(sc2)
            MATCH (br1)-[:ASSIGNED_TO]->(s1:Service)
            MATCH (br2)-[:ASSIGNED_TO]->(s2:Service)
            WHERE s1 <> s2
            WITH DISTINCT s1, s2
            OPTIONAL MATCH (s1)-[r:CALLS]->(s2)
            RETURN s1.name AS fromService, s2.name AS toService, r IS NOT NULL AS preserved
            ORDER BY preserved ASC
        """)

        if not results:
            return "No cross-service call patterns found in CAST data."

        lost = [r for r in results if not r["preserved"]]
        output = f"CALL PATTERN PRESERVATION: {len(results) - len(lost)}/{len(results)} preserved, {len(lost)} lost\n"
        for l in lost:
            output += f"    LOST: {l['fromService']} -> {l['toService']}\n"
        return output

    @server.tool()
    def graph_reconciliation_report(phase: str) -> str:
        """Run all relevant reconciliation queries for the current phase."""
        output = f"=== RECONCILIATION REPORT ({phase}) ===\n\n"
        output += graph_extraction_coverage() + "\n"
        if phase in ("phase-3", "phase-4a", "phase-5", "phase-6"):
            output += graph_assignment_coverage() + "\n"
        if phase in ("phase-5", "phase-6"):
            output += graph_implementation_coverage() + "\n"
            output += graph_call_pattern_preservation() + "\n"
        output += graph_unaccounted_loss() + "\n"
        return output
