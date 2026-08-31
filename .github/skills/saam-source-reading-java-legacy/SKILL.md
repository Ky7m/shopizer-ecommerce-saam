---
name: saam-source-reading-java-legacy
description: "Source-reading patterns for legacy Java, Spring, Hibernate/JPA, and Drools applications."
---

# SAAM Java Legacy Source Reading Guide

## Scope

Use this guide for Java applications built with Spring MVC or Spring Boot, Hibernate/JPA,
repositories or DAOs, and rule engines such as Drools. Read source for business meaning; treat
framework wiring and generated persistence behavior as supporting evidence.

## Component Classification

- **Interactive/API:** `@Controller`, `@RestController`, request mappings, command handlers.
- **Service/library:** `@Service`, service interfaces and implementations, domain orchestration.
- **Persistence:** `@Repository`, Spring Data repositories, DAOs, entity managers, JPQL/native SQL.
- **Batch/integration:** scheduled listeners, message consumers, provider adapters, import/export jobs.
- **Rule engine:** Drools `.drl` rules, rule services, agenda/event handlers.
- **Utility/infrastructure:** mappers, formatters, logging, generic helpers, framework configuration.

Do not exclude an `*Init`, `*Setup`, mapper, or adapter solely because its name sounds technical.
Check whether it bridges domain entities, consumes user-selected identifiers, or changes durable
state.

## Business-Rule Signals

Look for:

- guards in controllers and services (`if`, `switch`, validation annotations, authorization checks);
- calculations of totals, prices, tax, shipping, discounts, eligibility, or balances;
- status/state reads followed by writes;
- entity creation, updates, deletes, and cascading relationships;
- repository query predicates that encode eligibility or visibility;
- transaction boundaries (`@Transactional`) and rollback/error handling;
- emitted events, provider calls, email, search indexing, and file/object storage;
- Drools conditions, salience, agenda groups, rule consequences, and fact updates.

Behavior in annotations and configuration is part of the rule when it changes validation, routing,
transactionality, security, or persistence semantics.

## Traceability Method

For every extracted rule record the exact:

`initial-source/<path>:<class-or-method>:<start-end lines>`

Also record whether the evidence came from a direct source read, a CAST transaction/call graph, or
both. Keep API DTO validation separate from domain/service validation when their behavior differs.

## Data and Integration Extraction

Map each rule to:

- JPA entity/table and distinct fields read;
- repository/DAO operation and write target;
- transaction boundary and state transition;
- external provider, HTTP client, queue/event, email, search, storage, or module call;
- returned response, exception, error code, or rollback outcome.

When no static DDL exists, use `@Entity`, `@Table`, `@Column`, relationship annotations, migrations,
and repository queries as schema evidence. Mark inferred table names and unresolved mappings.

## Source Semantic Vector

Count raw occurrences for each component, including infrastructure behavior:

1. control-flow branches and boolean conditions;
2. distinct entity/table and field references read or written;
3. hard-coded constants and thresholds;
4. status/state assignments and lifecycle changes;
5. distinct returns, responses, and exception outcomes;
6. distinct insert/update/delete targets;
7. external calls, events, and provider/module invocations;
8. distinct error and exception paths.

Record zero only when the source was read and the dimension is genuinely absent. In Hybrid mode,
retain CAST cyclomatic complexity as `srcControlFlow` until the direct source count supersedes it.

## Implicit-System Flags

Flag, without finalizing, the following:

- lifecycle candidates from status/state fields or guarded transitions;
- invariant candidates from repeated guards, entity relationships, checks, and balance equations;
- extensibility signals from configuration, metadata, user-defined fields, or parameter tables;
- placement-review candidates for bulk/set-based operations, scheduled work, database procedures, and
  large aggregations.

Phase 1 records the evidence and concern. Later phases define the closed state machine, invariant
tiers, extensibility engine, and placement decision.
