"""Detect BR-ID references in source files and update the SAAM Knowledge Graph.

Scans source code files for BR-ID patterns (e.g., BR-OR-VAL-001) in comments,
annotations, or method names, then creates/updates CLAIMS_IMPLEMENTATION edges in Neo4j.

Usage:
  # Single file (called by PostFileSave hook):
  python3 graph-mcp/scripts/detect_br_ids.py --file sourcecode/order-service/src/service/OrderService.java

  # Batch (called after git checkout of ATX branch):
  python3 graph-mcp/scripts/detect_br_ids.py --service order-service

  # From stdin (hook mode — receives JSON with file path):
  echo '{"path": "sourcecode/order-service/src/..."}' | python3 graph-mcp/scripts/detect_br_ids.py --stdin

Exit codes:
  0 = success (BR-IDs detected and graph updated, or no BR-IDs found — both are fine)
  1 = error (Neo4j unavailable, invalid arguments)
"""

import argparse
import hashlib
import json
import os
import re
import sys
from pathlib import Path

from neo4j import GraphDatabase

NEO4J_URI = os.environ.get("NEO4J_URI", "bolt://localhost:7687")
NEO4J_USER = os.environ.get("NEO4J_USER", "neo4j")
NEO4J_PASSWORD = os.environ.get("NEO4J_PASSWORD", "saamgraph")


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

# Workspace root = parent of graph-mcp/ (this file lives in graph-mcp/scripts/).
# Resolve sourcecode/ and spec/ against this, NOT the current working directory —
# hooks invoke this script with varying CWDs (e.g. `uv run --directory graph-mcp`
# changes CWD into graph-mcp/, which used to break sourcecode/ and spec/ lookups).
WORKSPACE_ROOT = Path(__file__).resolve().parent.parent.parent


def _ws(rel: str) -> Path:
    """Resolve a workspace-relative path (sourcecode/..., spec/...) to an absolute path.
    Falls back to CWD-relative if it doesn't exist under the workspace root (belt & suspenders)."""
    p = WORKSPACE_ROOT / rel
    if p.exists():
        return p
    cwd_p = Path(rel)
    return cwd_p if cwd_p.exists() else p

def _br_id_regex() -> str:
    """Read the BR-ID pattern from the single source of truth (saam-calibration.yaml → br_id_pattern).
    NEVER hardcode a divergent pattern — three separate hardcoded patterns previously disagreed and
    silently missed flat BR-AP-001 style IDs at Phase 5. Falls back to the widened union pattern
    (group segment optional, admits both BR-AP-001 and BR-GL-PST-001) only if calibration is unreadable."""
    fallback = r"BR-[A-Z]{2,6}(?:-[A-Z]{2,6})?-[0-9]{2,3}"
    try:
        for candidate in (
            WORKSPACE_ROOT / "core/steering/saam-calibration.yaml",
            WORKSPACE_ROOT / ".kiro/steering/saam-calibration.yaml",
            WORKSPACE_ROOT / "dist/kiro-ide/.kiro/steering/saam-calibration.yaml",
            Path("core/steering/saam-calibration.yaml"),
            Path(".kiro/steering/saam-calibration.yaml"),
            Path("dist/kiro-ide/.kiro/steering/saam-calibration.yaml"),
        ):
            if candidate.exists():
                import re as _re
                text = candidate.read_text(encoding="utf-8", errors="ignore")
                # find the br_id_pattern block, then its regex_tolerant (preferred for detection) or regex
                block = text.split("br_id_pattern:", 1)
                if len(block) == 2:
                    body = block[1]
                    m = _re.search(r'regex_tolerant:\s*"([^"]+)"', body) or _re.search(r'regex:\s*"([^"]+)"', body)
                    if m:
                        return m.group(1)
                break
    except Exception:
        pass
    return fallback


# BR-ID pattern — sourced from saam-calibration.yaml (single source of truth), NOT hardcoded.
BR_ID_PATTERN = re.compile(_br_id_regex())

# File extensions to scan
SOURCE_EXTENSIONS = {
    ".java", ".kt", ".ts", ".js", ".py", ".cs", ".go",
    ".rs", ".rb", ".php", ".scala", ".groovy",
}

# BR-ID heading pattern in spec files
BR_HEADING_PATTERN = re.compile(rf"^###\s+({_br_id_regex()})\s*[:\-]", re.MULTILINE)


def compute_spec_hash(service_name: str, br_id: str) -> str | None:
    """Compute the spec hash for a specific BR-ID from the service's spec file.

    Returns first 16 chars of SHA256, or None if spec not found.
    """
    spec_file = _ws(f"spec/microservices/{service_name}/01-business-rules.md")
    if not spec_file.exists():
        specs_dir = _ws("spec/microservices")
        # Try without hyphen normalization
        for candidate in specs_dir.iterdir() if specs_dir.exists() else []:
            if candidate.is_dir() and (candidate / "01-business-rules.md").exists():
                if service_name.replace("-", "") in candidate.name.replace("-", ""):
                    spec_file = candidate / "01-business-rules.md"
                    break
        if not spec_file.exists():
            return None

    content = spec_file.read_text(encoding="utf-8")
    matches = list(BR_HEADING_PATTERN.finditer(content))
    if not matches:
        return None

    for i, match in enumerate(matches):
        if match.group(1) == br_id:
            start = match.start()
            end = matches[i + 1].start() if i + 1 < len(matches) else len(content)
            section = content[start:end]
            normalized = "\n".join(line.rstrip() for line in section.splitlines())
            normalized = re.sub(r"\n{3,}", "\n\n", normalized).strip()
            return hashlib.sha256(normalized.encode("utf-8")).hexdigest()[:16]

    return None


def extract_service_from_path(file_path: str) -> str | None:
    """Extract service name from sourcecode/<service>/..."""
    match = re.search(r"sourcecode/([^/]+)/", file_path)
    return match.group(1) if match else None


def find_br_ids_in_file(file_path: str) -> set[str]:
    """Scan a file for BR-ID references."""
    try:
        with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
            content = f.read()
    except (OSError, IOError):
        return set()

    return set(BR_ID_PATTERN.findall(content))


def find_br_ids_in_service(service_dir: str) -> dict[str, set[str]]:
    """Scan all source files in a service directory for BR-ID references.
    Returns: {file_path: {br_ids}}
    """
    results = {}
    service_path = Path(service_dir)

    if not service_path.exists():
        return results

    for file_path in service_path.rglob("*"):
        if file_path.is_file() and file_path.suffix in SOURCE_EXTENSIONS:
            br_ids = find_br_ids_in_file(str(file_path))
            if br_ids:
                results[str(file_path)] = br_ids

    return results


def update_graph(service_name: str, file_br_ids: dict[str, set[str]]) -> dict:
    """Update Neo4j graph with CLAIMS_IMPLEMENTATION edges for detected BR-IDs."""
    try:
        driver = GraphDatabase.driver(NEO4J_URI, auth=(NEO4J_USER, NEO4J_PASSWORD))
        driver.verify_connectivity()
    except Exception:
        return {"error": "Neo4j unavailable", "detected": 0, "updated": 0}

    all_br_ids = set()
    for br_ids in file_br_ids.values():
        all_br_ids.update(br_ids)

    created = 0
    not_found = []

    with driver.session() as session:
        for file_path, br_ids in file_br_ids.items():
            # Extract class/method name from file path for the Implementation node
            rel_path = file_path.split("sourcecode/", 1)[-1] if "sourcecode/" in file_path else file_path
            file_name = Path(file_path).stem

            for br_id in br_ids:
                # Check if the BR-ID exists in the graph
                result = session.run("""
                    MATCH (br:BusinessRule {brId: $brId})
                    RETURN br.brId AS brId
                """, {"brId": br_id}).single()

                if not result:
                    not_found.append(br_id)
                    continue

                # Create/update Implementation node + CLAIMS_IMPLEMENTATION edge
                # Also advance lifecycle state to 'Declared' and set implementationConfidence
                spec_hash = compute_spec_hash(service_name, br_id)
                session.run("""
                    MATCH (br:BusinessRule {brId: $brId})
                    MERGE (impl:Implementation {methodName: $methodName, service: $service})
                    SET impl.filePath = $filePath,
                        impl.className = $className,
                        impl._confidence = 0.75,
                        impl._createdBy = 'detect_br_ids',
                        impl._lastUpdated = datetime()
                    MERGE (br)-[ci:CLAIMS_IMPLEMENTATION]->(impl)
                    ON CREATE SET ci.detectedAt = datetime(), ci.detectedBy = 'detect_br_ids'
                    SET ci.specHash = $specHash,
                        impl.brIds = coalesce(impl.brIds, []) + CASE
                        WHEN NOT $brId IN coalesce(impl.brIds, []) THEN [$brId]
                        ELSE [] END
                    // Also stamp specHash on the BusinessRule node
                    WITH br, impl
                    SET br.specHash = $specHash, br._specHashUpdatedAt = datetime()
                    // Advance lifecycle state to Declared (only if not already further along)
                    WITH br, impl
                    WHERE br.lifecycleState IN ['Extracted', 'Assigned'] OR br.lifecycleState IS NULL
                    SET br.lifecycleState = 'Declared',
                        br.implementationConfidence = 0.5,
                        br.effectiveConfidence = CASE
                            WHEN br.provenanceConfidence IS NOT NULL
                            THEN CASE WHEN br.provenanceConfidence < 0.5 THEN br.provenanceConfidence ELSE 0.5 END
                            ELSE 0.5
                            END
                """, {
                    "brId": br_id,
                    "methodName": file_name,
                    "service": service_name,
                    "filePath": rel_path,
                    "className": file_name,
                    "specHash": spec_hash,
                })
                created += 1

        # Update service implementation completeness
        session.run("""
            MATCH (s:Service)
            WHERE toLower(replace(s.name, ' ', '-')) = toLower($service)
               OR s.serviceId = $service
            OPTIONAL MATCH (s)<-[:ASSIGNED_TO]-(br:BusinessRule)
            WITH s, count(br) AS total
            OPTIONAL MATCH (s)<-[:ASSIGNED_TO]-(br2:BusinessRule)-[:CLAIMS_IMPLEMENTATION]->()
            With s, total, count(DISTINCT br2) AS implemented
            SET s.implementationCompleteness = CASE WHEN total > 0 THEN toFloat(implemented) / total ELSE 0.0 END
        """, {"service": service_name})

    driver.close()

    return {
        "service": service_name,
        "filesScanned": len(file_br_ids),
        "brIdsDetected": len(all_br_ids),
        "edgesCreated": created,
        "brIdsNotInGraph": list(set(not_found)),
    }


def main():
    parser = argparse.ArgumentParser(description="Detect BR-ID references in source code")
    parser.add_argument("--file", help="Single file to scan")
    parser.add_argument("--service", help="Service directory name (scans all files in sourcecode/<service>/)")
    parser.add_argument("--all", action="store_true", help="Scan ALL services under sourcecode/ (post-bulk-generation / post-git-pull)")
    parser.add_argument("--stdin", action="store_true", help="Read file path from stdin JSON (hook mode)")
    args = parser.parse_args()

    if args.all:
        # Batch mode across all services — for bulk-landed code (ATX output, git pull, fix loops)
        # that never triggers PostFileSave.
        src_root = _ws("sourcecode")
        if not src_root.is_dir():
            print(f"ERROR: sourcecode/ not found under {WORKSPACE_ROOT}", file=sys.stderr)
            sys.exit(1)
        grand_total = 0
        for svc_dir in sorted(p for p in src_root.iterdir() if p.is_dir()):
            file_br_ids = find_br_ids_in_service(str(svc_dir))
            if not file_br_ids:
                continue
            result = update_graph(svc_dir.name, file_br_ids)
            grand_total += result.get("edgesCreated", 0)
            print(f"  {svc_dir.name}: {result['brIdsDetected']} BR-IDs, {result['edgesCreated']} edges")
        print(f"\n[SAAM Graph] Batch scan complete: {grand_total} CLAIMS_IMPLEMENTATION edges across all services")
        sys.exit(0)

    if args.stdin:
        # Hook mode: read JSON from stdin
        try:
            data = json.loads(sys.stdin.read())
            file_path = data.get("path", "")
        except (json.JSONDecodeError, EOFError):
            sys.exit(1)

        if not file_path or "sourcecode/" not in file_path:
            sys.exit(0)  # Not a sourcecode file — nothing to do

        service_name = extract_service_from_path(file_path)
        if not service_name:
            sys.exit(0)

        # Check extension
        if Path(file_path).suffix not in SOURCE_EXTENSIONS:
            sys.exit(0)

        br_ids = find_br_ids_in_file(file_path)
        if not br_ids:
            sys.exit(0)  # No BR-IDs in this file — fine

        result = update_graph(service_name, {file_path: br_ids})
        if result.get("edgesCreated", 0) > 0:
            print(f"[SAAM Graph] Detected {len(br_ids)} BR-ID(s) in {Path(file_path).name}: {', '.join(sorted(br_ids))}")

    elif args.file:
        # Single file mode
        service_name = extract_service_from_path(args.file)
        if not service_name:
            print(f"ERROR: Cannot determine service from path: {args.file}", file=sys.stderr)
            sys.exit(1)

        br_ids = find_br_ids_in_file(args.file)
        if not br_ids:
            print(f"No BR-ID references found in {args.file}")
            sys.exit(0)

        result = update_graph(service_name, {args.file: br_ids})
        print(json.dumps(result, indent=2))

    elif args.service:
        # Batch mode: scan entire service (resolve against workspace root, not CWD)
        service_dir = str(_ws(f"sourcecode/{args.service}"))
        if not os.path.isdir(service_dir):
            print(f"ERROR: Directory not found: {service_dir}", file=sys.stderr)
            sys.exit(1)

        file_br_ids = find_br_ids_in_service(service_dir)
        if not file_br_ids:
            print(f"No BR-ID references found in {service_dir}")
            sys.exit(0)

        result = update_graph(args.service, file_br_ids)
        print(json.dumps(result, indent=2))

        # Summary
        print(f"\nSummary: {result['brIdsDetected']} BR-IDs detected across {result['filesScanned']} files, {result['edgesCreated']} graph edges created")
        if result.get("brIdsNotInGraph"):
            print(f"WARNING: {len(result['brIdsNotInGraph'])} BR-IDs found in code but NOT in graph: {result['brIdsNotInGraph'][:10]}")

    else:
        parser.print_help()
        sys.exit(1)


if __name__ == "__main__":
    main()
