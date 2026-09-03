#!/bin/bash
# =============================================================================
# SAAM Validation Runner + Reconciliation Trigger
#
# Runs the xUnit + .NET Aspire integration test suite for a service, parses the
# results into a structured YAML artifact, then calls the reconciliation script
# to update the graph and generate remediation tasks.
#
# The standalone bash suites (validation/<service>/comprehensive-test-suite.sh)
# are DEPRECATED. See .github/skills/saam-dotnet-reference-implementation/SKILL.md
# for the current test standard. Legacy .sh files are retained for reference and
# are no longer executed by this runner.
#
# USAGE:
#   ./validation/run-and-reconcile.sh <service> [trigger]
#
# ARGUMENTS:
#   service         ms-NN (e.g. ms-01), the PascalCase service name
#                   (e.g. CustomerIdentity), or the Aspire resource name
#                   (e.g. customer-identity)
#   trigger         Optional: stage2_smoke | stage4_final | model_b_post_atx |
#                   model_a_inline | ci_pipeline (default: manual)
#
# PREREQUISITES:
#   - .NET SDK 10 on PATH
#   - A running container runtime (the Aspire host provisions PostgreSQL and
#     RabbitMQ). A skipped or non-executed suite is a FAILED gate, never a pass.
#   - python3 with pyyaml available (for the reconciliation script)
#
# OUTPUT:
#   .saam/reconciliation/<service>/validation-run-<timestamp>.yaml
#   (then reconcile_validation.py updates graph + generates remediation tasks)
# =============================================================================

set -uo pipefail

SERVICE_ARG="${1:-}"
TRIGGER="${2:-manual}"

if [ -z "$SERVICE_ARG" ]; then
    echo "Usage: $0 <service> [trigger]"
    echo "Example: $0 ms-01 stage4_final"
    exit 1
fi

WORKSPACE_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
TEST_PROJECT="$WORKSPACE_ROOT/sourcecode/Shopizer.IntegrationTests"
SOLUTION="$WORKSPACE_ROOT/sourcecode/Shopizer.slnx"

# --- Resolve the service identifier to a .NET project / test class name ---
# ms-NN and Aspire resource names both map onto the PascalCase project suffix.
resolve_service() {
    case "$1" in
        ms-01|customer-identity|CustomerIdentity)             echo "CustomerIdentity" ;;
        ms-02|catalog-product|CatalogProduct)                 echo "CatalogProduct" ;;
        ms-03|search|Search)                                  echo "Search" ;;
        ms-04|cart-checkout|CartCheckout)                     echo "CartCheckout" ;;
        ms-05|order-management|OrderManagement)               echo "OrderManagement" ;;
        ms-06|payments|Payments)                              echo "Payments" ;;
        ms-07|pricing-promotions|PricingPromotions)           echo "PricingPromotions" ;;
        ms-08|tax|Tax)                                        echo "Tax" ;;
        ms-09|shipping|Shipping)                              echo "Shipping" ;;
        ms-10|merchant-administration|MerchantAdministration) echo "MerchantAdministration" ;;
        ms-11|content-configuration|ContentConfiguration)     echo "ContentConfiguration" ;;
        ms-12|platform-integrations|PlatformIntegrations)     echo "PlatformIntegrations" ;;
        *)                                                    echo "" ;;
    esac
}

SERVICE_NAME=$(resolve_service "$SERVICE_ARG")
if [ -z "$SERVICE_NAME" ]; then
    echo "ERROR: Unknown service '$SERVICE_ARG'."
    echo "Expected ms-01..ms-12, a PascalCase service name, or an Aspire resource name."
    exit 1
fi

# The graph and the validation/ tree are keyed on ms-NN; keep that as the artifact key.
SERVICE="$SERVICE_ARG"
TEST_CLASS="${SERVICE_NAME}ComprehensiveTests"
TEST_FILE="$TEST_PROJECT/${TEST_CLASS}.cs"
SERVICE_DIR="$WORKSPACE_ROOT/sourcecode/Shopizer.${SERVICE_NAME}"
OUTPUT_DIR="$WORKSPACE_ROOT/.saam/reconciliation/$SERVICE"
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
RUN_ID="val-$(date +%Y%m%d-%H%M%S)"
ARTIFACT="$OUTPUT_DIR/validation-run-${RUN_ID}.yaml"
TRX_DIR="$OUTPUT_DIR/trx"
TRX_FILE="$TRX_DIR/${RUN_ID}.trx"

# --- Validate inputs ---
if [ ! -d "$TEST_PROJECT" ]; then
    echo "ERROR: Integration test project not found: $TEST_PROJECT"
    exit 1
fi

if [ ! -f "$TEST_FILE" ]; then
    echo "ERROR: Test class not found: $TEST_FILE"
    echo "Phase 4c must generate ${TEST_CLASS}.cs before this service can be validated."
    exit 1
fi

if [ ! -d "$SERVICE_DIR" ]; then
    echo "ERROR: Service directory not found: $SERVICE_DIR"
    exit 1
fi

mkdir -p "$OUTPUT_DIR" "$TRX_DIR"

echo "=== SAAM Validation: $SERVICE ($SERVICE_NAME) ==="
echo "  Trigger: $TRIGGER"
echo "  Suite: $TEST_FILE"
echo "  Filter: FullyQualifiedName~$TEST_CLASS"
echo "  Artifact: $ARTIFACT"
echo ""

# --- Build ---
echo "[run-and-reconcile] Building solution..."
BUILD_START=$(date +%s)
BUILD_OUTPUT=$(dotnet build "$SOLUTION" --nologo -v quiet 2>&1)
BUILD_EXIT_CODE=$?
BUILD_END=$(date +%s)
BUILD_DURATION=$((BUILD_END - BUILD_START))

if [ $BUILD_EXIT_CODE -ne 0 ]; then
    echo "$BUILD_OUTPUT"
    echo ""
    echo "BUILD FAILED for $SERVICE — not running tests."
    cat > "$ARTIFACT" << EOF
schema_version: "1.0"
service: "$SERVICE"
run_id: "$RUN_ID"
timestamp: "$TIMESTAMP"
trigger: "$TRIGGER"
implementation_type: "${IMPLEMENTATION_TYPE:-unknown}"

build:
  status: "fail"
  duration_seconds: $BUILD_DURATION

test_execution:
  suite: "${TEST_CLASS}.cs"
  total: 0
  passed: 0
  failed: 0
  skipped: 0
  pass_rate: 0
  duration_seconds: 0
  exit_code: $BUILD_EXIT_CODE

  failures:
    []

  br_ids_passing: 0
  br_ids_failing: 0

br_id_detection:
  code_br_ids: ""
EOF
    exit 1
fi

# --- Run the integration suite ---
# The Aspire host provisions PostgreSQL and RabbitMQ; this requires a container runtime.
echo "[run-and-reconcile] Running $TEST_CLASS..."
TEST_START=$(date +%s)

if grep -q '"runner"[[:space:]]*:[[:space:]]*"Microsoft.Testing.Platform"' "$WORKSPACE_ROOT/sourcecode/global.json" 2>/dev/null; then
    (
        cd "$WORKSPACE_ROOT/sourcecode" &&
        dotnet test --project "Shopizer.IntegrationTests/Shopizer.IntegrationTests.csproj" \
            --no-build \
            --filter-class "*$TEST_CLASS*"
    ) > "$TRX_DIR/${RUN_ID}.log" 2>&1
else
    dotnet test "$TEST_PROJECT" \
        --nologo \
        --no-build \
        --filter "FullyQualifiedName~$TEST_CLASS" \
        --logger "trx;LogFileName=$TRX_FILE" \
        > "$TRX_DIR/${RUN_ID}.log" 2>&1
fi
TEST_EXIT_CODE=$?

TEST_END=$(date +%s)
DURATION=$((TEST_END - TEST_START))

tail -20 "$TRX_DIR/${RUN_ID}.log"

# --- Parse the TRX into the reconciliation artifact ---
# BR-IDs are resolved by joining the TRX outcomes against the [Trait("BR", "...")]
# attributes in the test source, which is deterministic and independent of how the
# TRX logger chooses to serialize traits.
export WORKSPACE_ROOT SERVICE RUN_ID TIMESTAMP TRIGGER TEST_CLASS SERVICE_DIR
export BUILD_DURATION DURATION TEST_EXIT_CODE
export IMPLEMENTATION_TYPE="${IMPLEMENTATION_TYPE:-unknown}"

python3 - "$TRX_FILE" "$TEST_FILE" "$ARTIFACT" "$TRX_DIR/${RUN_ID}.log" << 'PYEOF'
import os, re, sys, xml.etree.ElementTree as ET

trx_path, test_file, artifact_path, log_path = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]

service = os.environ.get("SERVICE", "")
run_id = os.environ.get("RUN_ID", "")
timestamp = os.environ.get("TIMESTAMP", "")
trigger = os.environ.get("TRIGGER", "manual")
test_class = os.environ.get("TEST_CLASS", "")
service_dir = os.environ.get("SERVICE_DIR", "")
implementation_type = os.environ.get("IMPLEMENTATION_TYPE", "unknown")
build_duration = os.environ.get("BUILD_DURATION", "0")
duration = os.environ.get("DURATION", "0")
exit_code = os.environ.get("TEST_EXIT_CODE", "1")

BR_PATTERN = r"BR-[A-Z0-9]{2,6}(?:-[A-Z0-9]{2,6})?-[0-9]{2,3}"


def br_id_regex():
    """Read the BR-ID pattern from saam-calibration.yaml — the single source of truth."""
    root = os.environ.get("WORKSPACE_ROOT", "")
    candidate = os.path.join(root, ".github", "saam-calibration.yaml")
    try:
        with open(candidate, encoding="utf-8", errors="ignore") as handle:
            text = handle.read()
        block = text.split("br_id_pattern:", 1)
        if len(block) == 2:
            match = re.search(r'regex_tolerant:\s*"([^"]+)"', block[1]) or \
                    re.search(r'regex:\s*"([^"]+)"', block[1])
            if match:
                return match.group(1)
    except OSError:
        pass
    return BR_PATTERN


pattern = br_id_regex()

# method name -> [BR-IDs] from [Trait("BR", "...")] attributes in the test source
traits: dict[str, list[str]] = {}
pending: list[str] = []
try:
    with open(test_file, encoding="utf-8", errors="ignore") as handle:
        for line in handle:
            trait = re.search(r'\[\s*Trait\s*\(\s*"BR"\s*,\s*"([^"]+)"\s*\)\s*\]', line)
            if trait:
                pending.append(trait.group(1))
                continue
            method = re.search(r'\b(?:public|private|internal)\s+(?:async\s+)?[\w<>,\s\[\]?]+\s+(\w+)\s*\(', line)
            if method:
                if pending:
                    traits.setdefault(method.group(1), []).extend(pending)
                pending = []
except OSError:
    pass

total = passed = failed = skipped = 0
failures = []
passing_br_ids: set[str] = set()
failing_br_ids: set[str] = set()

if trx_path and os.path.isfile(trx_path):
    try:
        tree = ET.parse(trx_path)
        ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
        for result in tree.getroot().findall(".//t:UnitTestResult", ns):
            name = result.get("testName") or ""
            outcome = (result.get("outcome") or "").lower()
            method = name.split("(")[0].split(".")[-1]
            br_ids = traits.get(method) or re.findall(pattern, name) or ["UNKNOWN"]
            total += 1
            if outcome == "passed":
                passed += 1
                passing_br_ids.update(br_ids)
            elif outcome in ("notexecuted", "skipped"):
                # A skipped test is NOT a pass. It counts as a failure of the gate.
                skipped += 1
                failing_br_ids.update(br_ids)
                failures.append((method, br_ids[0], "test was skipped or not executed"))
            else:
                failed += 1
                failing_br_ids.update(br_ids)
                message = result.find(".//t:Message", ns)
                reason = (message.text or "").strip().replace("\n", " ") if message is not None else "assertion failed"
                failures.append((method, br_ids[0], reason[:400]))
    except (OSError, ET.ParseError) as error:
        failures.append(("<suite>", "UNKNOWN", f"could not parse TRX results: {error}"))
else:
    # Native MTP only exposes a console summary unless the optional TRX extension is installed.
    try:
        with open(log_path, encoding="utf-8", errors="ignore") as handle:
            output = handle.read()
        summary = re.search(
            r"total:\s*(\d+)\s+failed:\s*(\d+)\s+succeeded:\s*(\d+)\s+skipped:\s*(\d+)",
            output,
            re.IGNORECASE,
        )
        if summary:
            total, failed, passed, skipped = map(int, summary.groups())
            if failed or skipped:
                failures.append(("<suite>", "UNKNOWN", "Native MTP console summary reported failures."))
                failing_br_ids.update(br_id for values in traits.values() for br_id in values)
            else:
                passing_br_ids.update(br_id for values in traits.values() for br_id in values)
        else:
            failures.append(("<suite>", "UNKNOWN", "could not parse native MTP console summary"))
    except OSError as error:
        failures.append(("<suite>", "UNKNOWN", f"could not read native MTP output: {error}"))

pass_rate = round(passed / total, 3) if total else 0

# BR-IDs claimed by annotations in the service source
code_br_ids: set[str] = set()
for current, _dirs, files in os.walk(service_dir):
    if f"{os.sep}bin{os.sep}" in current + os.sep or f"{os.sep}obj{os.sep}" in current + os.sep:
        continue
    for filename in files:
        if not filename.endswith(".cs"):
            continue
        try:
            with open(os.path.join(current, filename), encoding="utf-8", errors="ignore") as handle:
                code_br_ids.update(re.findall(pattern, handle.read()))
        except OSError:
            pass


def quote(value: str) -> str:
    return value.replace("\\", "\\\\").replace('"', '\\"')


lines = [
    'schema_version: "1.0"',
    f'service: "{service}"',
    f'run_id: "{run_id}"',
    f'timestamp: "{timestamp}"',
    f'trigger: "{trigger}"',
    f'implementation_type: "{implementation_type}"',
    "",
    "build:",
    '  status: "pass"',
    f"  duration_seconds: {build_duration}",
    "",
    "test_execution:",
    f'  suite: "{test_class}.cs"',
    f"  total: {total}",
    f"  passed: {passed}",
    f"  failed: {failed}",
    f"  skipped: {skipped}",
    f"  pass_rate: {pass_rate}",
    f"  duration_seconds: {duration}",
    f"  exit_code: {exit_code}",
    "",
    "  failures:",
]

if failures:
    for index, (name, br_id, reason) in enumerate(failures, start=1):
        lines += [
            f"    - test_num: {index}",
            f'      name: "{quote(name)}"',
            f'      br_id: "{quote(br_id)}"',
            f'      reason: "{quote(reason)}"',
        ]
else:
    lines.append("    []")

lines += [
    "",
    f"  br_ids_passing: {len(passing_br_ids)}",
    f"  br_ids_failing: {len(failing_br_ids)}",
    "",
    "br_id_detection:",
    f'  code_br_ids: "{",".join(sorted(code_br_ids))}"',
]

with open(artifact_path, "w", encoding="utf-8") as handle:
    handle.write("\n".join(lines) + "\n")
PYEOF
PARSE_EXIT_CODE=$?

if [ $PARSE_EXIT_CODE -ne 0 ]; then
    echo "ERROR: Failed to parse test results into $ARTIFACT"
    exit 1
fi

# --- Report ---
TOTAL=$(sed -n 's/^  total: \([0-9]*\)$/\1/p' "$ARTIFACT")
PASSED=$(sed -n 's/^  passed: \([0-9]*\)$/\1/p' "$ARTIFACT")
FAILED=$(sed -n 's/^  failed: \([0-9]*\)$/\1/p' "$ARTIFACT")
SKIPPED=$(sed -n 's/^  skipped: \([0-9]*\)$/\1/p' "$ARTIFACT")
PASS_RATE=$(sed -n 's/^  pass_rate: \(.*\)$/\1/p' "$ARTIFACT")
BR_IDS_PASSING_COUNT=$(sed -n 's/^  br_ids_passing: \([0-9]*\)$/\1/p' "$ARTIFACT")
BR_IDS_FAILING_COUNT=$(sed -n 's/^  br_ids_failing: \([0-9]*\)$/\1/p' "$ARTIFACT")

echo ""
echo "=== Results ==="
echo "  Total: $TOTAL | Passed: $PASSED | Failed: $FAILED | Skipped: $SKIPPED"
echo "  Pass rate: $PASS_RATE"
echo "  BR-IDs passing: $BR_IDS_PASSING_COUNT | failing: $BR_IDS_FAILING_COUNT"
echo "  Artifact: $ARTIFACT"
if [ -n "$TRX_FILE" ] && [ -f "$TRX_FILE" ]; then
    echo "  TRX: $TRX_FILE"
else
    echo "  Native MTP log: $TRX_DIR/${RUN_ID}.log"
fi

# --- Call reconciliation script ---
RECONCILE_SCRIPT="$WORKSPACE_ROOT/graph-mcp/scripts/reconcile_validation.py"
if [ -f "$RECONCILE_SCRIPT" ]; then
    echo ""
    echo "[run-and-reconcile] Running graph reconciliation..."
    uv run --project "$WORKSPACE_ROOT/graph-mcp" python "$RECONCILE_SCRIPT" "$ARTIFACT" ||
        echo "WARNING: Reconciliation failed (graph may be unavailable)"
else
    echo ""
    echo "[run-and-reconcile] Reconciliation script not found — artifact produced but graph not updated"
fi

# --- Exit with the gate result ---
# A suite that ran zero tests, or skipped any, is a FAILED gate — never a pass.
if [ "${TOTAL:-0}" = "0" ]; then
    echo ""
    echo "NO TESTS EXECUTED for $SERVICE — the Aspire host requires a container runtime with PostgreSQL and RabbitMQ."
    echo "A non-executed suite is a FAILED gate."
    exit 1
elif [ "${FAILED:-0}" = "0" ] && [ "${SKIPPED:-0}" = "0" ]; then
    echo ""
    echo "ALL TESTS PASSED for $SERVICE"
    exit 0
else
    echo ""
    echo "$FAILED failed / $SKIPPED skipped for $SERVICE — see artifact for details"
    exit 1
fi
