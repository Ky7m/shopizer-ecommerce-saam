#!/bin/bash
# Ensures Neo4j container is running before graph operations.
# Called by the SessionStart hook before session_context.py.
# Uses podman (preferred) or docker (fallback).
# Handles per-project isolation: each project gets its own container + ports.
#
# IMPORTANT: Always writes .env with ACTUAL running ports (not just computed ports).
# This fixes the port mismatch issue where .env says one port but container maps another.
#
# Exit 0 = Neo4j is running and .env reflects actual ports
# Exit 1 = Cannot start Neo4j (missing podman/docker, compose failed, or health check failed)

set -euo pipefail

COMPOSE_DIR="$(cd "$(dirname "$0")/.." && pwd)"

# Derive project-specific name from workspace directory name
PROJECT_DIR="$(cd "$COMPOSE_DIR/.." && basename "$(pwd)")"
export SAAM_PROJECT_NAME="${PROJECT_DIR}"
CONTAINER_NAME="saam-graph-${SAAM_PROJECT_NAME}"

# Determine container runtime
if command -v podman &>/dev/null; then
    RUNTIME="podman"
    COMPOSE_CMD="podman compose"
elif command -v docker &>/dev/null; then
    RUNTIME="docker"
    COMPOSE_CMD="docker compose"
else
    echo "ERROR: No container runtime (podman/docker) found" >&2
    exit 1
fi

# ─── Function: read actual mapped ports from running container ───────────────
get_actual_ports() {
    local http_port bolt_port
    # Get the host port mapped to container port 7474 (HTTP)
    http_port=$($RUNTIME port "$CONTAINER_NAME" 7474 2>/dev/null | head -1 | sed 's/.*://')
    # Get the host port mapped to container port 7687 (Bolt)
    bolt_port=$($RUNTIME port "$CONTAINER_NAME" 7687 2>/dev/null | head -1 | sed 's/.*://')
    
    if [ -n "$http_port" ] && [ -n "$bolt_port" ]; then
        export NEO4J_HTTP_PORT="$http_port"
        export NEO4J_BOLT_PORT="$bolt_port"
        return 0
    fi
    return 1
}

# ─── Function: write .env with current port values ───────────────────────────
write_env() {
    cat > "$COMPOSE_DIR/.env" << EOF
SAAM_PROJECT_NAME=${SAAM_PROJECT_NAME}
NEO4J_HTTP_PORT=${NEO4J_HTTP_PORT}
NEO4J_BOLT_PORT=${NEO4J_BOLT_PORT}
COMPOSE_PROJECT_NAME=saam-${SAAM_PROJECT_NAME}
EOF
}

# ─── Function: compute desired ports from project name hash ──────────────────
compute_ports() {
    local port_offset
    port_offset=$(echo -n "$SAAM_PROJECT_NAME" | cksum | awk '{print $1 % 100}')
    export NEO4J_HTTP_PORT=$((7474 + port_offset))
    export NEO4J_BOLT_PORT=$((7687 + port_offset))
}

# ─── Main logic ─────────────────────────────────────────────────────────────

# Case 1: Container is already running
if $RUNTIME ps --format '{{.Names}}' 2>/dev/null | grep -q "^${CONTAINER_NAME}$"; then
    # Read ACTUAL ports from the running container (not computed — actual mapped ports)
    if get_actual_ports; then
        write_env
        # Verify it's healthy
        if curl -sf "http://localhost:${NEO4J_HTTP_PORT}" >/dev/null 2>&1; then
            exit 0
        fi
        # Running but not responding — wait briefly
        for i in $(seq 1 10); do
            if curl -sf "http://localhost:${NEO4J_HTTP_PORT}" >/dev/null 2>&1; then
                exit 0
            fi
            sleep 1
        done
        echo "ERROR: Neo4j container running but not responding on port ${NEO4J_HTTP_PORT}" >&2
        exit 1
    fi
    # Can't read ports — fall through to restart
fi

# Case 2: Container exists but is stopped
if $RUNTIME ps -a --format '{{.Names}}' 2>/dev/null | grep -q "^${CONTAINER_NAME}$"; then
    $RUNTIME start "$CONTAINER_NAME" &>/dev/null
    sleep 3
    # Read actual ports after start
    if get_actual_ports; then
        write_env
        # Wait for health
        for i in $(seq 1 30); do
            if curl -sf "http://localhost:${NEO4J_HTTP_PORT}" >/dev/null 2>&1; then
                exit 0
            fi
            sleep 1
        done
    fi
    echo "ERROR: Neo4j did not respond after starting existing container" >&2
    exit 1
fi

# Case 3: Container doesn't exist — create with compose
compute_ports
write_env

cd "$COMPOSE_DIR"
$COMPOSE_CMD up -d &>/dev/null

# Wait for Neo4j HTTP API to respond (max 30 seconds)
for i in $(seq 1 30); do
    if curl -sf "http://localhost:${NEO4J_HTTP_PORT}" >/dev/null 2>&1; then
        # Verify actual ports match (compose might have picked different ones)
        if get_actual_ports; then
            write_env
        fi
        exit 0
    fi
    sleep 1
done

# Neo4j didn't respond in time — FAIL
echo "ERROR: Neo4j did not respond on port ${NEO4J_HTTP_PORT} within 30 seconds" >&2
exit 1
