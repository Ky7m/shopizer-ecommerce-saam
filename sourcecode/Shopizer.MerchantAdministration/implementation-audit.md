# Implementation Audit: Merchant and Store Administration

## Decisions Made During Implementation

| Timestamp | Area | Decision | Rationale |
|---|---|---|---|
| 2026-09-04 | Persistence | Kept the repository on raw Npgsql and the existing shared Aspire `shopizerDb` resource. | The repository's AppHost already uses the shared database resource; no ORM is permitted. |
| 2026-09-04 | Code identity | Normalized store codes by trimming and lower-casing before lookup and persistence. | BR-MSA-VAL-001 requires equivalent identifiers to share one uniqueness representation. |
| 2026-09-04 | Deletion | Implemented guarded soft deletion with restrictive child policy by default. | The domain state model defines Deleted as terminal and inferred clarification rejects active children. |
| 2026-09-04 | Branding | Delegated logo bytes to an HTTP file-provider boundary and persisted only its returned URI. | BR-MSA-BRD-001 explicitly excludes binary bytes from the MS-10 record. |

## Specification Boundaries Requiring Review

| Area | Boundary | Resolution |
|---|---|---|
| Reference services | Country, language, unit, and currency services are not registered in the current AppHost. | Enforced the documented reference-code allowlists and recorded provider calls as a future integration boundary. |
| Signup delivery | The contract returns no verification token. | Persisted a hashed, single-use token and returned only the contract response; delivery remains an external provider concern. |
| Authentication | The existing development AppHost does not share MS-01's generated development JWT secret. | MS-10 validates a configured secret in non-development and remains non-rejecting when no development secret is configured; production must set `MerchantAdministration:JwtSecret`. |

## Validation Record

| Check | Status | Result |
|---|---|---|
| DTO copy integrity | PASS | All 20 files copied verbatim from `spec/microservices/ms-10/08-dtos/`. |
| Targeted project build | NOT_RUN | Pending compile fixes. |
| Solution build | NOT_RUN | Pending targeted build. |
| Runtime Aspire validation | NOT_RUN | Requires container/MTP environment. |
