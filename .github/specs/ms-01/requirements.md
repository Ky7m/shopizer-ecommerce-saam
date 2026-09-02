# Requirements: Customer and Identity (MS-01)

Source of truth: `spec/microservices/ms-01/01-business-rules.md`,
`02-domain-model.md`, `03-api-design.md`, and the frozen `04-api-contract.yaml`.

## Functional requirements

### FR-1: Customer registration, profile, address, attributes and lifecycle
**Source:** BR-CUS-001..010, BR-CUS-011..020, BR-UI-001.

The service shall maintain customer identities within tenant/store scope; derive the
login name from the email at self-registration; require a valid billing country;
encode credentials; apply customer group, gender and language defaults; validate
country/zone and same-store option references; preserve separate billing and delivery
addresses; expose authenticated-principal self service; page grouped customer
searches; and remove attributes when an account is deleted.

### FR-2: Customer credentials and token security
**Source:** BR-CUS-NN-001..009.

The service shall issue random two-day store-bound reset tokens, reject missing,
expired, consumed, cross-store and wrong-subject tokens, enforce password policy,
consume reset tokens after successful use, require current-password proof for
changes, and sign/validate audience-bound JWTs with subject, issued-at, expiry,
tenant/store and password-reset invalidation claims. Refresh is predicate-based and
never unconditional.

### FR-3: Administrator identity and authorization
**Source:** BR-CUS-NN-010..020.

The service shall authenticate administrators against encoded passwords, enforce
store-scoped uniqueness and group policy, preserve protected administrator identity
and super-admin membership, page store-scoped administrator listings, prevent
protected-account deletion, enforce current-password proof and policy on changes,
apply the same reset lifecycle, reject inactive accounts, and scope enablement to
the selected store.

### FR-4: Reviews, newsletter and external identities
**Source:** BR-CUS-NN-021, BR-CUS-021..028, BR-UI-002.

The service shall enforce the external identity composite key, one review per
reviewer/target pair, rating range 1..5, transactional aggregate recomputation on
create/update/delete, canonical `reviewId` routing, store/campaign/email newsletter
upsert and independent store subscriptions. The legacy newsletter PUT returns an
explicit 501; unsubscribe is a deliberate persisted state transition.

## API requirements

All operations use `/api/v1`, camelCase JSON, kebab-case paths, and the required
`x-tenant-id`, `x-store-id`, and `x-correlation-id` context from the contract.

| Endpoint group | Operations | Contract success |
|---|---:|---|
| Customers | 10 | 200/201/204 |
| Customer authentication | 3 | 200/201 |
| Customer reset | 3 | 200/202/204 |
| Newsletter | 3 | 201/204/501 |
| Reviews | 4 | 200/201/204 |
| Administrator authentication | 2 | 200 |
| Administrators | 8 | 200/201/204 |
| Administrator reset | 3 | 200/202/204 |
| External identities | 1 | 201 |

Error responses are the frozen `ErrorResponse` shape with stable rule-specific
codes and the exact contract status (400, 401, 403, 404, 409, 410, 422, 501, 500).

## Data requirements

MS-01 owns all 14 tables in `02-domain-model.md` under PostgreSQL schema
`customer_identity`: customer accounts, addresses, options, option values,
attributes, reviews, newsletter subscriptions, permission groups, permissions,
group permissions, administrator accounts, administrator memberships, credential
reset tokens, and external identity connections. `store_id` is opaque MS-10 data;
no cross-service foreign key or database access is permitted.

## Non-functional requirements

- PostgreSQL is primary whenever an Aspire/DATABASE connection is configured.
- Schema creation is explicit and includes enums, indexes, constraints and safe
  additive startup migration columns.
- Registration writes a durable event outbox row and attempts the
  `CustomerRegistered` RabbitMQ event with shared metadata.
- Credentials, reset tokens and access tokens are not logged.
- `/health`, `/alive` and Aspire ServiceDefaults remain available.
