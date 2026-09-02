# SAAM Knowledge Graph MCP Server

Neo4j-backed knowledge graph for SAAM modernization engagements. Provides traceability from legacy source → extracted rules → target services → API contracts → tests → implementations → deviations, with formal inference, confidence propagation, and agent context construction.

## Quick Start

```bash
# Start Neo4j and write the project-specific ports to .env
cd graph-mcp
bash scripts/ensure_neo4j.sh

# Neo4j Browser is available at http://localhost:<NEO4J_HTTP_PORT from .env>
# Default credentials: neo4j / saamgraph

# Initialize schema (constraints + indexes)
uv run python scripts/init_schema.py

# Run the MCP server
uv run saam-graph
```

## Architecture

```
┌─────────────────────────────────────────────┐
│  AI Coding Agent / Harness (via MCP)        │
│  • Adds nodes/edges as phases execute       │
│  • Queries for context construction         │
│  • Runs reconciliation at phase gates       │
└─────────────────┬───────────────────────────┘
                  │ MCP (stdio)
┌─────────────────▼───────────────────────────┐
│  saam-graph MCP Server (Python)             │
│  • Tool definitions (mutations, queries,    │
│    reconciliation, inference, context)      │
│  • Schema validation                        │
│  • Confidence propagation logic             │
└─────────────────┬───────────────────────────┘
                  │ Bolt (dynamic port from graph-mcp/.env)
┌─────────────────▼───────────────────────────┐
│  Neo4j 5 Community (Podman or Docker container) │
│  • Graph storage                            │
│  • Cypher query execution                   │
│  • APOC library (graph algorithms)          │
│  • Constraint enforcement                   │
│  • Browser UI (7474) for visualization      │
└─────────────────────────────────────────────┘
```

## MCP Tools

### Mutation Tools
- `graph_add_node` — Create a node with type, properties, provenance
- `graph_add_edge` — Create a relationship between nodes
- `graph_update_node` — Update node properties
- `graph_bulk_import` — Import multiple nodes/edges from a phase output

### Query Tools
- `graph_query_nodes` — Find nodes by type and properties
- `graph_traverse` — Traverse relationships from a starting node (N hops)
- `graph_impact_analysis` — Find all nodes affected by a change to a given node
- `graph_cypher` — Execute raw Cypher for advanced queries

### Reconciliation Tools
- `graph_extraction_coverage` — Compare CAST components vs extracted BR-IDs
- `graph_assignment_coverage` — Find BR-IDs not assigned to services
- `graph_implementation_coverage` — Find Active/Core BR-IDs without implementations
- `graph_unaccounted_loss` — The master query: what's missing and not explicitly excluded?
- `graph_call_pattern_preservation` — Compare CAST call graph vs service dependencies

### Inference Tools
- `graph_run_inferences` — Execute all inference rules, create derived edges
- `graph_propagate_confidence` — Recalculate confidence scores through the graph
- `graph_detect_unused_tables` — Find tables with no Active BR-IDs writing to them

### Context Construction Tools
- `graph_implementation_context` — Get everything needed to implement a service
- `graph_fix_context` — Get everything needed to fix a deviation/bug
- `graph_phase_status` — Get completion metrics for a phase

## Configuration

### Kiro IDE
Add to `.kiro/settings/mcp.json`:

```json
{
  "mcpServers": {
    "saam-graph": {
      "type": "stdio",
      "command": "uv",
      "args": ["--directory", "<path-to-graph-mcp>", "run", "saam-graph"],
      "env": {
        "NEO4J_USER": "neo4j",
        "NEO4J_PASSWORD": "saamgraph"
      },
      "disabled": false
    }
  }
}
```

### GitHub Copilot / Other MCP Clients
Add to `.mcp.json` or your MCP client configuration:

```json
{
  "mcpServers": {
    "saam-graph": {
      "type": "stdio",
      "command": "uv",
      "args": ["--directory", "graph-mcp", "run", "saam-graph"],
      "env": {
        "NEO4J_USER": "neo4j",
        "NEO4J_PASSWORD": "saamgraph"
      }
    }
  }
}
```

`NEO4J_URI` is intentionally omitted. `scripts/ensure_neo4j.sh` discovers the
actual per-project Bolt port and writes `NEO4J_URI` to `graph-mcp/.env`; the
server loads that file on startup. Do not hardcode `bolt://localhost:7687` in
the MCP configuration.

## Troubleshooting

### `Failed to connect to Neo4j` or `Connection refused`

The project uses isolated, dynamic host ports derived from the workspace name.
The container's internal Bolt port is `7687`, but the host port is recorded in
`graph-mcp/.env` as `NEO4J_BOLT_PORT` and `NEO4J_URI`.

From the repository root, refresh the container and generated connection
settings:

```bash
bash graph-mcp/scripts/ensure_neo4j.sh
grep -E '^(NEO4J_BOLT_PORT|NEO4J_URI)=' graph-mcp/.env
```

Keep `NEO4J_URI` out of `.mcp.json`. A stale `NEO4J_URI` inherited by the MCP
client can override the project's dynamic port. The effective configuration
precedence is:

1. `NEO4J_URI` loaded from `graph-mcp/.env`
2. `NEO4J_URI` inherited from the environment
3. `NEO4J_BOLT_PORT`
4. `bolt://localhost:7687` fallback

Verify Copilot's parsed workspace configuration and the server directly:

```bash
copilot mcp list
copilot mcp get saam-graph
uv --directory graph-mcp run saam-graph
```

The direct startup should log `Connected to Neo4j`. Reload or restart the MCP
client after changing `.mcp.json`.

## Schema

See `saam-graph-schema.yaml` for the formal definition of:
- Node types and their required/optional properties
- Edge types with source/target constraints
- Cardinality rules
- Inference rules
- Confidence propagation model

## Automatic Context Hooks

The graph also powers context hooks (or harness adapters) that inject context automatically:

### Kiro IDE Hooks (`.kiro/hooks/`, when using the Kiro harness)
| Hook | Trigger | Script | What It Provides |
|------|---------|--------|-----------------|
| `graph-session-context.json` | SessionStart | `scripts/session_context.py` | Engagement overview, service progress, open deviations, pending work |
| `graph-file-context.json` | PreToolUse (fs_write/str_replace/fs_append) | `scripts/file_context.py` | Service endpoints, contract field names, pending BR-IDs (only for `sourcecode/` paths) |

### GitHub Copilot Hook Adapter (`.github/hooks/`)
- `saam-hooks.json` registers the Copilot lifecycle commands.
- `saam-copilot-adapter.ts` executes SessionStart, PreToolUse, PostToolUse, and AgentStop context handling.

Both scripts connect to Neo4j directly (not through the MCP server) for minimal latency. They exit 1 silently if Neo4j is unavailable — no disruption to normal workflow.
