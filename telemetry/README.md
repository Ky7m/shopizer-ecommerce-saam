# SAAM Telemetry — Central Analytics Store

## Purpose

This directory is the centralized analytics store for SAAM engagement telemetry. It receives anonymized metrics from completed engagement phases, stores them in DuckDB for statistical analysis, and produces calibrated weights that feed back into SAAM steering files.

## Architecture

```
Engagements produce .saam/telemetry/*.yaml (per-phase, anonymized)
    ↓
Copy to telemetry/data/raw/<engagement-id>/
    ↓
python ingest/import_telemetry.py data/raw/<engagement-id>/
    ↓
DuckDB (telemetry/data/saam_telemetry.duckdb) populated
    ↓
Run analysis queries (telemetry/analysis/*.sql)
    ↓
python analysis/weight_update.py → outputs/calibrated-weights-vN.yaml
    ↓
Copy to core/steering/saam-calibration.yaml → recompile (npm run package) → commit → next engagement uses it
```

## Directory Structure

```
telemetry/
├── README.md                          # this file
├── ingest/
│   ├── schema.sql                     # DuckDB table definitions
│   └── import_telemetry.py            # import script (YAML → DuckDB)
├── analysis/
│   ├── automatibility_validation.sql  # does AS predict implementation success?
│   ├── complexity_calibration.sql     # are condensation flags useful?
│   ├── confidence_calibration.sql     # does confidence predict deviations?
│   ├── duration_prediction.sql        # can we predict phase durations?
│   └── weight_update.py              # produces calibrated weights YAML
├── data/
│   ├── raw/                           # imported engagement telemetry folders
│   │   ├── ENG-2026-001/             # (gitignored — real engagement data)
│   │   └── ...
│   └── saam_telemetry.duckdb          # the analytics database (gitignored)
└── outputs/
    └── calibrated-weights-vN.yaml     # produced by weight_update.py
```

## Usage

### Import an engagement

```bash
# Copy telemetry from engagement workspace
cp -r /path/to/engagement/.saam/telemetry/ telemetry/data/raw/ENG-2026-003/

# Import into DuckDB
cd telemetry
python ingest/import_telemetry.py data/raw/ENG-2026-003/
```

### Run analysis

```bash
# Interactive exploration
python -c "import duckdb; db = duckdb.connect('data/saam_telemetry.duckdb'); print(db.sql('SELECT * FROM engagements').df())"

# Run specific analysis query
duckdb data/saam_telemetry.duckdb < analysis/automatibility_validation.sql

# Produce updated calibration weights
python analysis/weight_update.py
```

### Apply calibrated weights

```bash
# Review the output
cat outputs/calibrated-weights-v2.yaml

# Apply to canonical SAAM steering and recompile distributions
cp outputs/calibrated-weights-v2.yaml ../core/steering/saam-calibration.yaml
npm run package
```

## What's Committed vs Gitignored

| Path | Committed? | Why |
|------|-----------|-----|
| `ingest/` scripts | Yes | Tooling is part of the framework |
| `analysis/` queries and scripts | Yes | Analysis logic is part of the framework |
| `data/raw/` | No (gitignored) | Contains engagement-specific data |
| `data/saam_telemetry.duckdb` | No (gitignored) | Generated database file |
| `outputs/` | Yes | Calibration artifacts are framework deliverables |

## Prerequisites

- Python 3.10+
- `duckdb` package: `pip install duckdb` or `uv pip install duckdb`
- `pyyaml` package: `pip install pyyaml` or `uv pip install pyyaml`
