#!/usr/bin/env node
/**
 * SAAM GitHub Copilot Lifecycle Hook Adapter Shim
 *
 * Maps Copilot lifecycle events (sessionStart, preToolUse, postToolUse, agentStop)
 * to SAAM Python tooling and Neo4j Knowledge Graph context scripts.
 */

import { spawnSync } from 'node:child_process';
import * as path from 'node:path';
import * as fs from 'node:fs';
import { fileURLToPath } from 'node:url';

export interface CopilotHookInput {
  sessionId?: string;
  timestamp?: number;
  cwd?: string;
  toolName?: string;
  toolArgs?: Record<string, any>;
  toolResult?: {
    resultType?: string;
    textResultForLlm?: string;
  };
  filePath?: string;
  eventName?: 'sessionStart' | 'preToolUse' | 'postToolUse' | 'agentStop' | string;
  arguments?: Record<string, any>;
  path?: string;
}

export interface CopilotHookResponse {
  decision?: 'allow' | 'deny' | 'block';
  reason?: string;
  additionalContext?: string;
  modifiedResult?: {
    resultType?: string;
    textResultForLlm?: string;
  };
}

export interface AdapterResult {
  success: boolean;
  output?: string;
  error?: string;
}

export function findWorkspaceRoot(startDir: string = process.cwd()): string {
  let current = path.resolve(startDir);
  while (current !== path.dirname(current)) {
    if (
      fs.existsSync(path.join(current, 'graph-mcp')) ||
      fs.existsSync(path.join(current, '.github')) ||
      fs.existsSync(path.join(current, 'package.json'))
    ) {
      return current;
    }
    current = path.dirname(current);
  }
  return process.cwd();
}

export function extractTargetFile(payload: CopilotHookInput): string | undefined {
  if (payload.filePath && typeof payload.filePath === 'string') {
    return payload.filePath;
  }
  if (payload.path && typeof payload.path === 'string') {
    return payload.path;
  }
  const args = payload.toolArgs || payload.arguments;
  if (args && typeof args === 'object') {
    const candidate = args.path || args.filePath || args.targetFile || args.file_path || args.file;
    if (typeof candidate === 'string') {
      return candidate;
    }
  }
  return undefined;
}

export function runPythonScript(scriptPath: string, args: string[] = [], stdinData?: string): AdapterResult {
  const root = findWorkspaceRoot();
  const fullScriptPath = path.isAbsolute(scriptPath) ? scriptPath : path.join(root, scriptPath);

  if (!fs.existsSync(fullScriptPath)) {
    return {
      success: false,
      error: `Script not found: ${fullScriptPath}`
    };
  }

  // Attempt using uv run first, then fallback to python3
  const uvAvailable = spawnSync('command -v uv', { shell: true }).status === 0;

  let cmd: string;
  let cmdArgs: string[];

  if (uvAvailable) {
    cmd = 'uv';
    cmdArgs = ['run', '--directory', path.join(root, 'graph-mcp'), 'python', fullScriptPath, ...args];
  } else {
    cmd = 'python3';
    cmdArgs = [fullScriptPath, ...args];
  }

  try {
    const res = spawnSync(cmd, cmdArgs, {
      cwd: root,
      input: stdinData,
      encoding: 'utf-8',
      env: {
        ...process.env,
        PATH: `${process.env.HOME || ''}/.local/bin:${process.env.PATH || ''}`
      }
    });

    if (res.error) {
      return { success: false, error: res.error.message };
    }

    return {
      success: res.status === 0,
      output: res.stdout,
      error: res.stderr
    };
  } catch (err: any) {
    return {
      success: false,
      error: err.message || String(err)
    };
  }
}

export async function handleCopilotEvent(payload: CopilotHookInput): Promise<CopilotHookResponse> {
  const root = findWorkspaceRoot();
  const event = payload.eventName || 'sessionStart';

  switch (event) {
    case 'sessionStart': {
      // 1. Ensure Neo4j is running
      const ensureScript = path.join(root, 'graph-mcp', 'scripts', 'ensure_neo4j.sh');
      if (fs.existsSync(ensureScript)) {
        try {
          spawnSync('bash', [ensureScript], { cwd: root, stdio: 'ignore' });
        } catch {
          // ignore script startup error
        }
      }

      // 2. Reconcile BR-ID annotations
      runPythonScript('graph-mcp/scripts/detect_br_ids.py', ['--all']);

      // 3. Query session context
      const sessionCtx = runPythonScript('graph-mcp/scripts/session_context.py');
      const response: CopilotHookResponse = {};
      if (sessionCtx.success && sessionCtx.output && sessionCtx.output.trim()) {
        response.additionalContext = sessionCtx.output.trim();
      }
      return response;
    }

    case 'preToolUse':
    case 'toolUse': {
      const targetFile = extractTargetFile(payload);
      if (targetFile && typeof targetFile === 'string' && targetFile.includes('sourcecode/')) {
        const fileCtx = runPythonScript(
          'graph-mcp/scripts/file_context.py',
          [],
          JSON.stringify({ arguments: { path: targetFile } })
        );
        if (fileCtx.success && fileCtx.output && fileCtx.output.trim()) {
          return {
            decision: 'allow',
            additionalContext: fileCtx.output.trim()
          };
        }
      }
      return { decision: 'allow' };
    }

    case 'postToolUse':
    case 'postFileSave':
    case 'fileChange': {
      const targetFile = extractTargetFile(payload);
      if (targetFile && typeof targetFile === 'string' && targetFile.includes('sourcecode/')) {
        const res = runPythonScript(
          'graph-mcp/scripts/detect_br_ids.py',
          ['--stdin'],
          JSON.stringify({ path: targetFile })
        );
        if (res.success && res.output && res.output.trim()) {
          return { additionalContext: res.output.trim() };
        }
      }
      return {};
    }

    case 'agentStop': {
      return { decision: 'allow' };
    }

    default:
      return { decision: 'allow' };
  }
}

export function readStdinSync(): string {
  try {
    if (process.stdin.isTTY) {
      return '';
    }
    return fs.readFileSync(0, 'utf-8');
  } catch {
    return '';
  }
}

export async function main(): Promise<void> {
  const stdinRaw = readStdinSync();
  let payload: CopilotHookInput = {};

  if (stdinRaw && stdinRaw.trim()) {
    try {
      payload = JSON.parse(stdinRaw.trim());
    } catch {
      payload = {};
    }
  }

  if (!payload.eventName && process.argv[2]) {
    payload.eventName = process.argv[2] as CopilotHookInput['eventName'];
  }
  if (!payload.filePath && process.argv[3]) {
    payload.filePath = process.argv[3];
  }
  if (!payload.eventName) {
    payload.eventName = 'sessionStart';
  }

  try {
    const result = await handleCopilotEvent(payload);
    console.log(JSON.stringify(result));
    process.exit(0);
  } catch (err: any) {
    // Fault tolerant: exit 0 with allow decision
    console.log(JSON.stringify({ decision: 'allow' }));
    process.exit(0);
  }
}

// CLI Execution check
if (process.argv[1]) {
  try {
    const currentFile = fileURLToPath(import.meta.url);
    if (path.resolve(process.argv[1]) === path.resolve(currentFile)) {
      main();
    }
  } catch {
    if (process.argv[1].endsWith('saam-copilot-adapter.ts')) {
      main();
    }
  }
}
