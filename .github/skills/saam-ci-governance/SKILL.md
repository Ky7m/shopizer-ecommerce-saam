---
name: saam-ci-governance
description: "Continuous Integration governance and automated quality gate enforcement across modernization pipelines."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM CI/CD Governance Integration

## Purpose

This guide explains how to integrate SAAM's invisible governance into a CI/CD pipeline. The pipeline enforces governance automatically on every PR/push — no human selects a tier, the system determines governance level from the code diff.

## What the Pipeline Does

```
PR opened / code pushed
    ↓
1. DETECT: Which services affected? Do changed files contain BR-IDs?
    ↓
2. DRIFT CHECK: Run spec_drift.py on affected services
    ├── No drift → proceed silently
    ├── Non-critical drift → warn (comment on PR)
    └── Critical drift → BLOCK merge
    ↓
3. VALIDATE: Run comprehensive test suites for affected services
    ├── All pass → proceed
    └── Any fail → BLOCK merge
    ↓
4. RECONCILE: Update graph + generate remediation tasks
```

## Prerequisites

| Requirement | Why | Notes |
|-------------|-----|-------|
| Python 3.10+ on runner | Scripts require it | All CI platforms support this |
| `pyyaml` package | YAML parsing | `pip install pyyaml` |
| `neo4j` package | Graph access (optional) | Only needed if graph is network-accessible from CI |
| `spec/` directory | Spec drift needs spec files | Must be in the repo (not gitignored) |
| `validation/` directory | Test suites | Must be in the repo |
| `graph-mcp/scripts/` | Drift detection + reconciliation scripts | Must be in the repo |

### Neo4j Access from CI

**Option A: Graph accessible (recommended)**
- Neo4j runs on a persistent host (EC2, Cloud, self-hosted)
- CI runner connects via bolt:// URI
- Set `NEO4J_URI`, `NEO4J_USER`, `NEO4J_PASSWORD` as CI secrets

**Option B: Graph not accessible (standalone mode)**
- `spec_drift.py` runs in standalone mode (compares spec hashes against stored hashes in files, not graph)
- Reconciliation skipped (graph updates happen when developer runs locally)
- Still enforces: test pass/fail + spec hash comparison from files

---

## Reference Implementation: GitHub Actions

A complete workflow is provided at `.github/workflows/saam-governance.yml.sample` in the SAAM framework repo. It implements all 4 steps as separate jobs with dependency chains.

### Setup

1. Copy the sample workflow to your engagement repo and activate it:
   ```bash
   mkdir -p .github/workflows
   cp .github/workflows/saam-governance.yml.sample .github/workflows/saam-governance.yml
   ```
   Or download from the SAAM repo:
   ```bash
   gh api "repos/mkozinenko/saam/contents/.github/workflows/saam-governance.yml.sample?ref=main" \
     --jq '.content' | base64 -d > .github/workflows/saam-governance.yml
   ```
2. Set repository secrets (Settings → Secrets):
   - `NEO4J_URI` (optional — if graph is accessible from runner)
   - `NEO4J_USER` (optional)
   - `NEO4J_PASSWORD` (optional)
3. Ensure branch protection rules require the `SAAM Governance / spec-drift` check to pass

### Behavior

| Situation | GitHub Actions Behavior |
|-----------|------------------------|
| Change touches only config/CI/docs | Workflow doesn't trigger (path filter) |
| Change touches sourcecode/ with no BR-IDs | Drift check skips, tests run |
| Change touches BR-ID code, no drift | Tests run, reconciliation updates graph |
| Non-critical drift detected | Warning annotation on PR, merge allowed |
| Critical drift detected | `spec-drift` job fails → merge blocked |
| Tests fail | `validation` job fails → merge blocked |

---

## Platform Adaptations

### GitLab CI/CD

```yaml
# .gitlab-ci.yml

stages:
  - detect
  - governance
  - validate

variables:
  NEO4J_URI: ${NEO4J_URI}
  NEO4J_USER: ${NEO4J_USER}
  NEO4J_PASSWORD: ${NEO4J_PASSWORD}

detect-changes:
  stage: detect
  script:
    - |
      CHANGED=$(git diff --name-only origin/$CI_MERGE_REQUEST_TARGET_BRANCH_NAME...HEAD)
      SERVICES=$(echo "$CHANGED" | grep '^sourcecode/' | cut -d/ -f2 | sort -u | tr '\n' ',')
      HAS_BR_IDS="false"
      for file in $(echo "$CHANGED" | grep '^sourcecode/'); do
        if [ -f "$file" ] && grep -qP 'BR-[A-Z]{2}-[A-Z]{2,4}-\d{2,3}' "$file"; then
          HAS_BR_IDS="true"; break
        fi
      done
      echo "SERVICES=$SERVICES" >> detect.env
      echo "HAS_BR_IDS=$HAS_BR_IDS" >> detect.env
  artifacts:
    reports:
      dotenv: detect.env
  rules:
    - if: $CI_PIPELINE_SOURCE == "merge_request_event"
      changes:
        - sourcecode/**
        - spec/**
        - validation/**

spec-drift:
  stage: governance
  needs: [detect-changes]
  script:
    - pip install pyyaml neo4j
    - |
      for service in $(echo "$SERVICES" | tr ',' '\n'); do
        python3 graph-mcp/scripts/spec_drift.py --service "$service" || {
          # Check if critical
          OUTPUT=$(python3 graph-mcp/scripts/spec_drift.py --service "$service" --format yaml)
          if echo "$OUTPUT" | grep -q "tier3_human_review"; then
            echo "CRITICAL DRIFT — blocking merge"
            exit 1
          fi
        }
      done
  rules:
    - if: $HAS_BR_IDS == "true"

validation:
  stage: validate
  needs: [detect-changes]
  script:
    - |
      for service in $(echo "$SERVICES" | tr ',' '\n'); do
        SUITE="validation/$service/comprehensive-test-suite.sh"
        [ -f "$SUITE" ] && bash "$SUITE" || exit 1
      done
  rules:
    - if: $SERVICES != ""
```

**Key differences from GitHub Actions:**
- Uses `dotenv` artifacts to pass variables between jobs
- `rules:` instead of `if:` conditions
- `$CI_MERGE_REQUEST_TARGET_BRANCH_NAME` instead of `github.base_ref`

---

### Azure DevOps Pipelines

```yaml
# azure-pipelines.yml

trigger:
  branches:
    include: [main, develop]
  paths:
    include:
      - sourcecode/*
      - spec/*
      - validation/*

pool:
  vmImage: 'ubuntu-latest'

stages:
  - stage: Governance
    jobs:
      - job: DetectAndDrift
        steps:
          - checkout: self
            fetchDepth: 0

          - task: UsePythonVersion@0
            inputs:
              versionSpec: '3.11'

          - script: pip install pyyaml neo4j
            displayName: Install dependencies

          - script: |
              CHANGED=$(git diff --name-only origin/$(System.PullRequest.TargetBranch)...HEAD)
              SERVICES=$(echo "$CHANGED" | grep '^sourcecode/' | cut -d/ -f2 | sort -u | tr '\n' ',')
              echo "##vso[task.setvariable variable=SERVICES]$SERVICES"

              for service in $(echo "$SERVICES" | tr ',' '\n'); do
                python3 graph-mcp/scripts/spec_drift.py --service "$service" || {
                  OUTPUT=$(python3 graph-mcp/scripts/spec_drift.py --service "$service" --format yaml)
                  if echo "$OUTPUT" | grep -q "tier3_human_review"; then
                    echo "##vso[task.logissue type=error]Critical spec drift detected"
                    exit 1
                  fi
                }
              done
            displayName: Spec drift detection
            env:
              NEO4J_URI: $(NEO4J_URI)
              NEO4J_USER: $(NEO4J_USER)
              NEO4J_PASSWORD: $(NEO4J_PASSWORD)

      - job: Validation
        dependsOn: DetectAndDrift
        steps:
          - script: |
              for service in $(echo "$(SERVICES)" | tr ',' '\n'); do
                SUITE="validation/$service/comprehensive-test-suite.sh"
                [ -f "$SUITE" ] && bash "$SUITE" || exit 1
              done
            displayName: Run test suites
```

**Key differences:**
- `##vso[task.setvariable]` for passing variables
- `##vso[task.logissue type=error]` for blocking annotations
- `$(System.PullRequest.TargetBranch)` for base branch reference

---

### Bitbucket Pipelines

```yaml
# bitbucket-pipelines.yml

pipelines:
  pull-requests:
    '**':
      - step:
          name: SAAM Governance
          image: python:3.11-slim
          script:
            - pip install pyyaml neo4j
            - |
              CHANGED=$(git diff --name-only origin/$BITBUCKET_PR_DESTINATION_BRANCH...HEAD)
              SERVICES=$(echo "$CHANGED" | grep '^sourcecode/' | cut -d/ -f2 | sort -u)

              for service in $SERVICES; do
                python3 graph-mcp/scripts/spec_drift.py --service "$service" || {
                  OUTPUT=$(python3 graph-mcp/scripts/spec_drift.py --service "$service" --format yaml)
                  if echo "$OUTPUT" | grep -q "tier3_human_review"; then
                    echo "CRITICAL: Spec drift on Critical BR-ID — blocking"
                    exit 1
                  fi
                }
              done

              for service in $SERVICES; do
                SUITE="validation/$service/comprehensive-test-suite.sh"
                [ -f "$SUITE" ] && bash "$SUITE"
              done
          condition:
            changesets:
              includePaths:
                - "sourcecode/**"
                - "spec/**"
                - "validation/**"
```

**Key differences:**
- Single step (simpler — no multi-job orchestration)
- `$BITBUCKET_PR_DESTINATION_BRANCH` for target
- `condition.changesets.includePaths` for path filtering

---

## Configuration Guide

### When to Enable

Enable CI governance when:
- The engagement has a code repository with PR-based workflow
- Phase 5 implementation is underway (services exist in `sourcecode/`)
- The team wants automated enforcement (not just agent-driven)

### What to Customize

| Setting | What to Adjust |
|---------|---------------|
| **Path triggers** | Add frontend paths if frontend is in scope (`spec/frontend/**`) |
| **Branch protection** | Set the governance check as "required" in branch settings |
| **Service startup** | The validation job needs actual build+start commands per your stack |
| **Neo4j access** | Provide connection details or remove graph-dependent steps |
| **Notification** | Add Slack/Teams notification on drift detection (optional) |

### Minimal vs Full Setup

**Minimal (no graph access):**
- Drift detection: compares spec hashes from files (doesn't need Neo4j)
- Validation: runs test suites (doesn't need Neo4j)
- Reconciliation: skipped (done locally by developer)
- Still provides: merge blocking on test failures + spec drift warnings

**Full (with graph access):**
- All of the above plus:
- Signal status computed after validation
- Graph updated with test results
- Remediation tasks regenerated in PR
- Service `signalStatus` accurate for the next agent session

---

## Integration with SAAM Workflow

### During Phase 5

CI governance supplements the agent-driven workflow:
- Agent validates locally → CI validates on push
- Both use the same scripts (`spec_drift.py`, `run-and-reconcile.sh`)
- If agent missed something, CI catches it before merge

### During Phase 6

CI governance IS the primary enforcement mechanism:
- Developer makes a change → pushes
- CI determines governance level automatically
- No "what tier is this?" question — pipeline decides
- Agent picks up the reconciliation result in the next session

### Telemetry Integration

Each CI validation run produces a reconciliation artifact (`.saam/reconciliation/<service>/`). These artifacts:
- Feed into per-service telemetry at exit gate time
- Track `trigger: ci_pipeline` for calibration (CI vs agent-driven outcomes)
- Contribute to duration and remediation cycle counts
