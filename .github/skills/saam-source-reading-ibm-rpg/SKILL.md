---
name: saam-source-reading-ibm-rpg
description: "Source code analysis guide for IBM i, RPG IV, CL, DDS, and DB2 legacy architectures."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# Source Reading Guide: IBM i (RPG IV, CL, DDS)

## When to Activate
Activate this guide when the legacy system is IBM i / AS/400 based (RPG, CL, DDS, SQL/400).

## RPG Program Structure

### Free-Format RPG IV (.RPGLE)
```rpg
**free
ctl-opt main(Main);
dcl-f CUSTMAS disk usage(*update);
dcl-proc Main;
end-proc;
dcl-proc ValidateCustomer;
  dcl-pi *n ind;
    custId char(10) const;
  end-pi;
end-proc;
```

### Key Operations
| Op Code | Meaning | Business Relevance |
|---------|---------|-------------------|
| CHAIN | Random read by key | Data retrieval |
| READ/READE | Sequential read | List processing |
| WRITE | Insert record | Data creation |
| UPDATE | Modify record | Data mutation |
| DELETE | Remove record | Data removal |
| EXSR | Execute subroutine | Internal call |
| CALLP | Call procedure | External/internal call |
| IF/SELECT | Conditional | Business rule |
| MONITOR | Error handling | Exception handling |
| DOW/FOR | Loop | Iteration |

### Indicators
- *INLR: Program end signal
- *IN01-*IN99: Context-dependent (check display file for meaning)
- *IN03: Usually F3=Exit, *IN12: Usually F12=Cancel

## CL Programs (.CLP)
Orchestrate jobs, overrides, library lists, message handling.
Key commands: SBMJOB, OVRDBF, CALL, SNDPGMMSG, CHGDTAARA

## DDS Files
- Physical File (.PF) = Database table
- Logical File (.LF) = View/index with selection criteria
- Display File (.DSPF) = Screen layout with indicators

## Data Queue / Message Queue
Asynchronous messaging via QSNDDTAQ/QRCVDTAQ API calls.

## What to Extract
1. IF/SELECT statements → business conditions
2. CHAIN/READ before UPDATE → validation patterns
3. Subroutine structure → modular business logic
4. Error indicators → error handling rules
5. Data Queue sends → integration points
6. CALL/CALLP → program dependencies
7. Display file indicators → UI-driven rules
