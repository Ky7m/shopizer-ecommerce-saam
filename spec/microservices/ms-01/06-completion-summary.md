# Customer and Identity — Phase 4 Completion Summary

**Service:** MS-01 Customer and Identity  
**Port:** 8101  
**Schema:** `customer_identity`  
**Status:** 🟡 Complete for Phase 4 extraction; human BA sign-off and Phase 4a classification remain required.

## Decomposition outcome

The package contains **51 rules**: the 30 assigned Phase 3 rules (`BR-CUS-001..028` and `BR-UI-001..002`) re-extracted from source, plus **21 net-new rules** created by distinct behavioral seams found in the deep read. The count is not fitted to a target: the 51 rules decompose into registration/profile (19), reviews/consent (10), credential/token behavior (11), administrator lifecycle/authorization (10), and external identity (1) seams across the 34 CAST-targeted files, with defects retained as explicit corrective rules.

**Net-new findings include:** an unassigned customer token variable before validation; unconditional grace refresh; provider password comparisons against the username rather than the encoded password; reset-token non-consumption and missing password-policy enforcement; inconsistent review aggregate handling on update/delete; merchant scope omitted from newsletter uniqueness; malformed first-name and unparenthesized customer predicates; delivery state/postal mapping risk; review path identifier mismatch; unsupported newsletter mutations; and defective superadmin, store, and user-update authorization comparisons.

## Actual artifact counts

| Artifact | Actual count |
|---|---:|
| Business-rule IDs | 51 |
| PostgreSQL tables | 14 |
| PostgreSQL enum types | 5 |
| API endpoint methods | 39 |
| API path items | 26 |
| CAST-listed source files read | 34 |
| Additional directly referenced source files read | 16 |
| Semantic preservation tables | 51 |

## Endpoint coverage

| Endpoint family | Methods | Status | Driving rules |
|---|---|---|---|
| `/customers` | GET, POST | COVERED | BR-CUS-001, 003..005, 006, 009..010, 015..016 |
| `/customers/{customerId}` | GET, PUT, DELETE | COVERED | BR-CUS-006..008, 017 |
| `/customers/me` | GET, PATCH, DELETE | COVERED | BR-CUS-006..007, 017, 020 |
| `/customers/{customerId}/address` | PATCH | COVERED | BR-CUS-011..014, BR-UI-001 |
| `/customers/me/address` | PATCH | COVERED | BR-CUS-011..014, BR-UI-001 |
| `/customer-auth/registrations` | POST | COVERED | BR-CUS-001..005, 015..016, 019..020 |
| `/customer-auth/login` | POST | COVERED | BR-CUS-019..020, BR-CUS-NN-010 |
| `/customer-auth/refresh` | GET | COVERED | BR-CUS-NN-005..009 |
| `/customers/me/password` | POST | COVERED | BR-CUS-NN-004 |
| `/customer-password-resets` and `/{storeCode}/{token}` | POST, GET, POST | COVERED | BR-CUS-NN-001..003 |
| `/newsletter-subscriptions` and `/{email}` | POST, PUT, DELETE | COVERED | BR-CUS-026..028 |
| `/customers/{customerId}/reviews` and `/{reviewId}` | GET, POST, PUT, DELETE | COVERED | BR-CUS-021..025, BR-UI-002 |
| `/admin-auth/login`, `/admin-auth/refresh` | POST, GET | COVERED | BR-CUS-NN-005..008, 010 |
| `/users` and `/{userId}` | GET, POST, GET, PUT, DELETE | COVERED | BR-CUS-NN-011..015, 019 |
| `/users/{userId}/password`, `/enabled` | PATCH, PATCH | COVERED | BR-CUS-NN-016, 020 |
| `/users/unique`, `/users/me` | POST, GET | COVERED | BR-CUS-NN-011, 019 |
| `/user-password-resets` and `/{storeCode}/{token}` | POST, GET, POST | COVERED | BR-CUS-NN-017..018 |
| `/external-identities` | POST | COVERED | BR-CUS-NN-021 |

Explicit CRUD-only endpoints are marked in `03-api-design.md`; non-CRUD operations have a driving rule.

## Semantic preservation

| Source component group | Status | Evidence |
|---|---|---|
| Registration/profile/address | FLAGGED → corrective target rules | All data-flow, writes, and error paths captured; address fallback and field separation expose source defects. |
| Reviews | FLAGGED → BA review | Create formula preserved; update persistence and delete aggregate behavior are inconsistent in source and corrected in target rules. |
| Customer/admin authentication | FLAGGED → security correction | Token parsing, provider password argument, and unconditional grace refresh are explicit net-new findings. |
| Password reset | FLAGGED → security correction | Two-day token and email flow preserved; token consumption and password policy are target invariants absent from source. |
| Administrator authorization | FLAGGED → authorization correction | Store lineage and group checks preserved; superadmin membership and self-update comparisons require target predicates. |
| Attributes/opt-in/external identity | OK / FLAGGED for scope | Same-store options and composite provider key preserved; newsletter uniqueness is corrected to include store. |

Across 51 tables, each rule has all eight dimensions. Source/Spec values are intentionally not uniform: source defects and target corrections are shown as `GAP` rather than rubber-stamped `OK`.

## Placement candidate evidence

| Candidate | Legacy tier | Data-volume signal | Set-vs-row | Call frequency | App-tier risk | Default |
|---|---|---|---|---|---|---|
| Customer listing/search | Repository query | All customers in a store; joins addresses/attributes/groups | Set-based | Interactive | App-side filtering would load large store populations and break pagination | app-tier |
| Review aggregate maintenance | Service transaction | One target plus all reviews on delete/rebuild | Row plus aggregate | Interactive | Recomputing in application can create stale projections under concurrent edits | app-tier |
| Group/permission resolution | Service + joins | Groups and permissions per login | Set-based join | Every authenticated request/login | Repeated round trips increase authentication latency | app-tier |
| Reset-token verification | Service query | Single token lookup | Row-at-a-time | Interactive | Inconsistent consumption allows replay | app-tier |
| Newsletter uniqueness | Database unique constraint | All subscriptions for store/campaign | Set-based index | Every opt-in | App-only check races under concurrent signups | app-tier starting point; DB constraint mandatory |

No Phase 4b database placement decision is made here. Internal uniqueness, range, referential, and consumed-token invariants are database-enforced in the DDL.

## Dependencies and events

- **Upstream:** MS-10 store scope validation; OIDC/external identity provider where configured.
- **Downstream:** authenticated customer and administrator context consumed by MS-02, MS-04, MS-05, MS-10; `CustomerRegistered` event is an approved architecture capability but its publisher contract is deferred to Stage 1.5.
- **Outbound integrations:** email delivery for reset and registration messages.
- **No direct cross-service table reads:** `store_id` is an opaque MS-10 reference.

## Unresolved gaps

1. Password policy expression is delegated to Phase 4a/4b; source only invokes `validateUserPassword` and does not expose its policy.
2. JWT secret/expiration configuration values are externalized and must be pinned in deployment configuration.
3. OIDC provider contract, email delivery contract, and `CustomerRegistered` event schema must be reconciled in Stage 1.5.
4. Review moderation ownership and exact status transition triggers need BA confirmation.
5. Newsletter update/unsubscribe target behavior is specified, but implementation choice (`204` capability versus compatibility `501`) needs human approval.
6. The requested six-file package intentionally omits `05-dependencies.md`, DTOs, workflows, tests, and graph import; these belong to later SAAM stages.

## Readiness

The six requested files exist and are implementation-oriented: semantic rules with source evidence, executable PostgreSQL DDL, a 39-operation OpenAPI 3.1 contract, endpoint coverage, placement evidence, and explicit gaps. Human review is still required before Phase 4a sign-off.
