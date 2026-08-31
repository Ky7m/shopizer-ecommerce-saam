"""SAAM Project Dashboard — Local visibility into engagement progress.

Run: streamlit run graph-mcp/dashboard.py
Connects to the project's Neo4j instance (reads .env for port).

Panels:
1. Engagement Overview
2. Phase Progress
3. Service Health Grid
4. BR-ID Lifecycle Distribution
5. Preservation Quality
6. Signal Status
7. Deviations
8. Telemetry Metrics
"""

import os
import sys
from pathlib import Path

import streamlit as st
from neo4j import GraphDatabase

# ─── Configuration ───────────────────────────────────────────────────────────

def load_env():
    """Load .env from graph-mcp/ directory."""
    env_path = Path(__file__).parent / ".env"
    env = {}
    if env_path.exists():
        for line in env_path.read_text().splitlines():
            line = line.strip()
            if line and not line.startswith("#") and "=" in line:
                key, value = line.split("=", 1)
                env[key] = value
    return env


ENV = load_env()
NEO4J_BOLT_PORT = ENV.get("NEO4J_BOLT_PORT", "7687")
NEO4J_URI = f"bolt://localhost:{NEO4J_BOLT_PORT}"
NEO4J_USER = os.environ.get("NEO4J_USER", "neo4j")
NEO4J_PASSWORD = os.environ.get("NEO4J_PASSWORD", "saamgraph")
PROJECT_NAME = ENV.get("SAAM_PROJECT_NAME", "Unknown Project")
COMPOSE_DIR = str(Path(__file__).parent)


@st.cache_resource
def get_driver():
    """Create Neo4j driver (cached across reruns)."""
    try:
        driver = GraphDatabase.driver(NEO4J_URI, auth=(NEO4J_USER, NEO4J_PASSWORD))
        driver.verify_connectivity()
        return driver
    except Exception as e:
        st.error(f"Cannot connect to Neo4j at {NEO4J_URI}: {e}")
        return None


def query(cypher: str, params: dict = None) -> list[dict]:
    """Execute a Cypher query and return results as list of dicts."""
    driver = get_driver()
    if not driver:
        return []
    with driver.session() as session:
        result = session.run(cypher, params or {})
        return [record.data() for record in result]


# ─── Page Config ─────────────────────────────────────────────────────────────

st.set_page_config(
    page_title=f"SAAM — {PROJECT_NAME}",
    page_icon="🔬",
    layout="wide",
    initial_sidebar_state="collapsed",
)

st.title(f"SAAM Dashboard — {PROJECT_NAME}")

# ─── Panel 1: Engagement Overview ────────────────────────────────────────────

col1, col2, col3, col4 = st.columns(4)

# Get basic counts
node_counts = query("""
    MATCH (n)
    WITH labels(n)[0] AS label, count(n) AS cnt
    RETURN label, cnt ORDER BY cnt DESC
""")
counts = {r["label"]: r["cnt"] for r in node_counts}

services = query("MATCH (s:Service) RETURN s.name AS name, s.serviceId AS id ORDER BY s.name")
br_total = counts.get("BusinessRule", 0)
service_total = counts.get("Service", 0)
phase_events = query("MATCH (pe:PhaseEvent) RETURN pe.phase AS phase, pe.event AS event, pe.timestamp AS ts ORDER BY pe.timestamp")

with col1:
    st.metric("Services", service_total)
with col2:
    st.metric("Business Rules", br_total)
with col3:
    st.metric("Source Components", counts.get("SourceComponent", 0))
with col4:
    st.metric("Test Assertions", counts.get("TestAssertion", 0))

# ─── Panel 2: Phase Progress ─────────────────────────────────────────────────

st.subheader("Phase Progress")

phase_order = ["P0", "P1", "P2", "P3", "P4", "P4A", "P4B", "P4C", "P5", "P6"]
phase_labels = {
    "P0": "Onboard", "P1": "Analyze", "P2": "Design", "P3": "Converge",
    "P4": "Specify", "P4A": "Validate Rules", "P4B": "Score & Plan",
    "P4C": "Gen Tests", "P5": "Implement", "P6": "Evolve"
}

# Build phase status from PhaseEvents + tracking files (fallback)
phase_status = {}
for pe in phase_events:
    phase = pe["phase"]
    event = pe["event"]
    if phase not in phase_status:
        phase_status[phase] = {}
    phase_status[phase][event] = pe["ts"]

# Fallback: check tracking files for phases without PhaseEvents
WORKSPACE_ROOT = Path(COMPOSE_DIR).parent
TRACKING_DIR = WORKSPACE_ROOT / "tracking"

tracking_file_map = {
    "P0": "phase0-onboarding.md",
    "P1": "phase1-bottom-up.md",
    "P2": "phase2-top-down.md",
    "P3": "phase3-convergence.md",
    "P4": "phase4-spec-generation.md",
    "P4A": "phase4a-ba-review.md",
    "P4B": "phase4b-automatibility.md",
    "P4C": "phase4c-test-suites.md",
    "P5": "phase5-setup.md",
    "P6": "phase6-evolution.md",
}

for phase, filename in tracking_file_map.items():
    if phase not in phase_status:
        tracking_path = TRACKING_DIR / filename
        if tracking_path.exists():
            content = tracking_path.read_text()
            if "COMPLETE" in content.upper():
                phase_status[phase] = {"completed": "from_tracking"}
            elif "IN_PROGRESS" in content.upper() or "IN PROGRESS" in content.upper():
                phase_status[phase] = {"started": "from_tracking"}
            else:
                # File exists = at least started
                phase_status[phase] = {"started": "from_tracking"}

# Determine current phase (last one that's started but not completed)
current_phase = None
for phase in reversed(phase_order):
    ps = phase_status.get(phase, {})
    if "started" in ps and "completed" not in ps:
        current_phase = phase
        break

# Display as columns
phase_cols = st.columns(len(phase_order))
for i, phase in enumerate(phase_order):
    with phase_cols[i]:
        status = phase_status.get(phase, {})
        if "completed" in status:
            st.success(f"**{phase}**\n{phase_labels.get(phase, '')}")
        elif "started" in status:
            if phase == current_phase:
                st.warning(f"**>> {phase} <<**\n{phase_labels.get(phase, '')}")
            else:
                st.warning(f"**{phase}**\n{phase_labels.get(phase, '')}")
        else:
            st.empty()
            st.caption(f"{phase}\n{phase_labels.get(phase, '')}")

# ─── Panel 3: Service Health Grid ────────────────────────────────────────────

st.subheader("Service Health")

service_data = query("""
    MATCH (s:Service)
    OPTIONAL MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(s)
    WITH s, count(br) AS rules,
         avg(CASE WHEN br.provenanceConfidence IS NOT NULL THEN br.provenanceConfidence ELSE null END) AS avgProv,
         avg(CASE WHEN br.implementationConfidence IS NOT NULL THEN br.implementationConfidence ELSE null END) AS avgImpl
    RETURN s.name AS name, s.serviceId AS id,
           rules,
           CASE WHEN avgProv IS NOT NULL THEN round(avgProv * 100) ELSE null END AS provenance,
           CASE WHEN avgImpl IS NOT NULL THEN round(avgImpl * 100) ELSE null END AS implementation,
           coalesce(s.integration_pass_rate, null) AS integration,
           coalesce(s.signalStatus, 'UNKNOWN') AS signal
    ORDER BY rules DESC
""")

if service_data:
    # Display as a grid of cards
    cols_per_row = 4
    for row_start in range(0, len(service_data), cols_per_row):
        row = service_data[row_start:row_start + cols_per_row]
        cols = st.columns(cols_per_row)
        for i, svc in enumerate(row):
            with cols[i]:
                signal_emoji = {"CLEAR": "🟢", "BLOCKED": "🔴", "FLAGGED": "🟡"}.get(svc["signal"], "⚪")
                prov = svc.get("provenance")
                impl = svc.get("implementation")
                integ = svc.get("integration")
                prov_display = f"{prov:.0f}%" if prov is not None else "—"
                impl_display = f"{impl:.0f}%" if impl is not None else "—"
                integration_display = f"{integ:.0%}" if integ is not None and integ > 0 else "—"
                st.markdown(f"""
                **{svc['name']}** {signal_emoji}
                - Rules: {svc['rules']}
                - Extraction: {prov_display}
                - Implementation: {impl_display}
                - Integration: {integration_display}
                """)
else:
    st.info("No service data available.")

# ─── Panel 4: BR-ID Lifecycle Distribution ───────────────────────────────────

st.subheader("Business Rule Lifecycle")

lifecycle_data = query("""
    MATCH (br:BusinessRule)
    RETURN coalesce(br.lifecycleState, 'Unknown') AS state, count(br) AS cnt
    ORDER BY cnt DESC
""")

if lifecycle_data:
    import pandas as pd
    df = pd.DataFrame(lifecycle_data)
    # Order by lifecycle progression
    state_order = ["Extracted", "Assigned", "Declared", "Tested", "Passing", "Verified", "Obsolete", "Deferred", "Unknown"]
    df["order"] = df["state"].apply(lambda x: state_order.index(x) if x in state_order else 99)
    df = df.sort_values("order")
    st.bar_chart(df.set_index("state")["cnt"])
else:
    st.info("No business rules in graph.")

# ─── Panel 5: Preservation Quality ──────────────────────────────────────────

st.subheader("Semantic Preservation")

preservation_data = query("""
    MATCH (br:BusinessRule)
    WHERE br.specControlFlow IS NOT NULL
    WITH count(br) AS withVectors,
         sum(CASE WHEN toLower(coalesce(br.preservationStatus, br.preservationFlag, '')) = 'ok' THEN 1 ELSE 0 END) AS ok,
         sum(CASE WHEN toLower(coalesce(br.preservationStatus, br.preservationFlag, '')) = 'flagged' THEN 1 ELSE 0 END) AS flagged,
         sum(CASE WHEN toLower(coalesce(br.preservationStatus, br.preservationFlag, '')) IN ['unresolved', 'critical'] THEN 1 ELSE 0 END) AS unresolved
    RETURN withVectors, ok, flagged, unresolved
""")

rules_without_vectors = query("""
    MATCH (br:BusinessRule)
    WHERE br.specControlFlow IS NULL
    RETURN count(br) AS cnt
""")

col_p1, col_p2, col_p3, col_p4 = st.columns(4)
if preservation_data and preservation_data[0]["withVectors"] > 0:
    p = preservation_data[0]
    with col_p1:
        st.metric("With Vectors", p["withVectors"])
    with col_p2:
        st.metric("OK", p["ok"], delta=None)
    with col_p3:
        st.metric("Flagged", p["flagged"])
    with col_p4:
        st.metric("Unresolved", p["unresolved"])
else:
    no_vec = rules_without_vectors[0]["cnt"] if rules_without_vectors else 0
    st.info(f"No preservation vectors computed yet. {no_vec} rules without vectors.")

# ─── Panel 6: Automatibility Scores ──────────────────────────────────────────

st.subheader("Automatibility Scores")

# Check if automatibility-scores.md exists in the workspace
auto_scores_path = WORKSPACE_ROOT / "modernization" / "automatibility-scores.md"
if auto_scores_path.exists():
    # Parse the scores from the file (look for table rows with service names and percentages)
    import re
    auto_content = auto_scores_path.read_text()
    # Try to extract table rows — handle multiple format patterns
    # Pattern 1: | N | service-name | NN% | NN% | delta | Type X | rules |
    score_rows = re.findall(
        r'\|\s*\d+\s*\|\s*([\w-]+)\s*\|\s*(\d+)%\s*\|\s*(\d+)%\s*\|\s*[+\-]?\d+\s*\|\s*(Type [ABC])\s*\|\s*(\d+)\s*\|',
        auto_content
    )
    # Pattern 2: | MS-XX service-name | N% | N% | N% | N% | N% | **N%** | Type X |
    if not score_rows:
        score_rows_v2 = re.findall(
            r'\|\s*MS-\d+\s+([\w-]+)\s*\|\s*\d+%\s*\|\s*\d+%\s*\|\s*\d+%\s*\|\s*\d+%\s*\|\s*\d+%\s*\|\s*\*?\*?(\d+)%\*?\*?\s*\|\s*(Type [ABC])\s*\|',
            auto_content
        )
        if score_rows_v2:
            # Convert to unified format: (service, before=same, after, type, rules=0)
            score_rows = [(svc, score, score, typ, "0") for svc, score, typ in score_rows_v2]
    if score_rows:
        import pandas as pd
        df_auto = pd.DataFrame(score_rows, columns=["Service", "Before", "After", "Type", "Rules"])
        df_auto["Before"] = df_auto["Before"].astype(int)
        df_auto["After"] = df_auto["After"].astype(int)
        df_auto["Rules"] = df_auto["Rules"].astype(int)

        # Summary metrics
        col_a1, col_a2, col_a3, col_a4 = st.columns(4)
        with col_a1:
            st.metric("Avg Score", f"{df_auto['After'].mean():.0f}%")
        with col_a2:
            type_a = len(df_auto[df_auto["Type"] == "Type A"])
            st.metric("Type A (>=85%)", type_a)
        with col_a3:
            type_b = len(df_auto[df_auto["Type"] == "Type B"])
            st.metric("Type B (70-84%)", type_b)
        with col_a4:
            type_c = len(df_auto[df_auto["Type"] == "Type C"])
            st.metric("Type C (<70%)", type_c)

        # Colored table sorted by type then score descending
        type_sort = {"Type A": 0, "Type B": 1, "Type C": 2}
        df_auto["_sort"] = df_auto["Type"].map(type_sort)
        df_auto = df_auto.sort_values(["_sort", "After"], ascending=[True, False])

        display_df = df_auto[["Service", "After", "Type"]].copy()
        display_df.columns = ["Service", "Score", "Classification"]

        # Use emoji indicators instead of background colors (works in dark + light themes)
        def classify_emoji(typ):
            if typ == "Type A":
                return "🟢 Type A"
            elif typ == "Type B":
                return "🟡 Type B"
            elif typ == "Type C":
                return "🔴 Type C"
            return typ

        display_df["Classification"] = display_df["Classification"].apply(classify_emoji)
        display_df["Score"] = display_df["Score"].apply(lambda x: f"{x}%")

        # Use st.table (no scroll, shows all rows) instead of st.dataframe (scrollable)
        st.table(display_df.reset_index(drop=True))
    else:
        st.info("Automatibility scores file exists but couldn't parse score table. Check `modernization/automatibility-scores.md` format.")
else:
    st.info("Will be populated once Phase 4b is completed.")

# ─── Panel 7: Signal Status ─────────────────────────────────────────────────

st.subheader("Signal Status")

signal_data = query("""
    MATCH (br:BusinessRule)
    RETURN coalesce(br.signalStatus, 'NOT_COMPUTED') AS status, count(br) AS cnt
    ORDER BY cnt DESC
""")

if signal_data:
    col_s1, col_s2, col_s3 = st.columns(3)
    signals = {r["status"]: r["cnt"] for r in signal_data}
    with col_s1:
        st.metric("CLEAR", signals.get("CLEAR", 0))
    with col_s2:
        st.metric("BLOCKED", signals.get("BLOCKED", 0))
    with col_s3:
        st.metric("FLAGGED", signals.get("FLAGGED", 0))

    # Show blocked items if any
    blocked = query("""
        MATCH (br:BusinessRule {signalStatus: 'BLOCKED'})
        RETURN br.brId AS id, br.signalBlockers AS blockers
        LIMIT 10
    """)
    if blocked:
        st.warning("Blocked rules (action needed):")
        for b in blocked:
            st.markdown(f"- **{b['id']}**: {b['blockers']}")

# ─── Panel 7: Deviations ────────────────────────────────────────────────────

st.subheader("Deviations")

deviation_data = query("""
    MATCH (d:Deviation)
    RETURN d.status AS status, count(d) AS cnt
    ORDER BY cnt DESC
""")

if deviation_data:
    col_d1, col_d2 = st.columns(2)
    devs = {r["status"]: r["cnt"] for r in deviation_data}
    with col_d1:
        st.metric("Open", devs.get("OPEN", 0))
    with col_d2:
        st.metric("Resolved", devs.get("RESOLVED", 0))

    # Show open deviations
    open_devs = query("""
        MATCH (d:Deviation {status: 'OPEN'})
        RETURN d.brId AS brId, d.service AS service, d.reason AS reason, d.type AS type
        ORDER BY d.createdAt DESC LIMIT 10
    """)
    if open_devs:
        st.dataframe(open_devs, use_container_width=True)
else:
    st.success("No deviations recorded.")

# ─── Panel 8: Telemetry ─────────────────────────────────────────────────────

st.subheader("Telemetry")

# Phase durations from PhaseEvents + telemetry YAML fallback
duration_data = []

# Source 1: PhaseEvents in graph
if phase_status:
    for phase in phase_order:
        ps = phase_status.get(phase, {})
        if "started" in ps and "completed" in ps and ps["started"] != "from_tracking" and ps["completed"] != "from_tracking":
            try:
                from datetime import datetime
                start = datetime.fromisoformat(ps["started"].replace("Z", "+00:00"))
                end = datetime.fromisoformat(ps["completed"].replace("Z", "+00:00"))
                minutes = (end - start).total_seconds() / 60
                duration_data.append({"Phase": phase, "Duration (min)": round(minutes, 1), "Source": "graph"})
            except (ValueError, TypeError):
                pass

# Source 2: Telemetry YAML files (fallback for phases without graph events)
import yaml
TELEMETRY_DIR = WORKSPACE_ROOT / ".saam" / "telemetry"
telemetry_phase_map = {
    "P0": "phase0-onboarding.yaml",
    "P1": "phase1-bottom-up.yaml",
    "P2": "phase2-top-down.yaml",
    "P3": "phase3-convergence.yaml",
    "P4": "phase4-specs.yaml",
    "P4A": "phase4a-ba-review.yaml",
    "P4B": "phase4b-roadmap.yaml",
    "P4C": "phase4c-test-suites.yaml",
}

phases_with_data = {d["Phase"] for d in duration_data}
for phase, filename in telemetry_phase_map.items():
    if phase not in phases_with_data:
        telem_path = TELEMETRY_DIR / filename
        if telem_path.exists():
            try:
                telem = yaml.safe_load(telem_path.read_text())
                dur_hours = telem.get("duration_hours")
                dur_mins = telem.get("duration_minutes")
                if dur_mins:
                    duration_data.append({"Phase": phase, "Duration (min)": round(float(dur_mins), 1), "Source": "yaml"})
                elif dur_hours:
                    duration_data.append({"Phase": phase, "Duration (min)": round(float(dur_hours) * 60, 1), "Source": "yaml"})
            except Exception:
                pass

if duration_data:
    import pandas as pd
    df_dur = pd.DataFrame(duration_data)
    # Sort by phase order
    df_dur["order"] = df_dur["Phase"].apply(lambda x: phase_order.index(x) if x in phase_order else 99)
    df_dur = df_dur.sort_values("order")
    st.bar_chart(df_dur.set_index("Phase")["Duration (min)"])
else:
    st.info("No timing data available (no PhaseEvents or telemetry YAML files).")

# ─── Footer ─────────────────────────────────────────────────────────────────

st.divider()
st.caption(f"Connected to Neo4j at {NEO4J_URI} | Project: {PROJECT_NAME} | Refresh: click 'R' or Ctrl+R")
