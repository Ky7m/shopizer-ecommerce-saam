# Customer and Identity — Extraction Evidence

**Analysis mode:** Hybrid  
**CAST application:** `Shopizer-Backend`  
**CAST brief:** `assessment/ms-01-cast-brief.md`  
**Deep-read scope:** 34 CAST-targeted files (5,387 LOC) plus 16 directly referenced files.

## CAST-targeted source files processed

| # | File | LOC | Passes / sections read | Rules | Vectors |
|---:|---|---:|---|---|---|
| 1 | `.../sm-shop/.../api/v1/customer/CustomerApi.java` | 206 | 1 full pass; endpoints and authorization | 007-010, 017, UI-001 | ✅ |
| 2 | `.../api/v1/customer/AuthenticateCustomerApi.java` | 246 | 1 full pass; registration, login, refresh, password | 002-005, 019, NN-004 | ✅ |
| 3 | `.../api/v1/customer/ResetCustomerPasswordApi.java` | 128 | 1 full pass; request, verify, complete | NN-001..003 | ✅ |
| 4 | `.../api/v1/customer/CustomerNewsletterApi.java` | 91 | 1 full pass; create/update/delete | 026, 028 | ✅ |
| 5 | `.../api/v1/customer/CustomerReviewApi.java` | 106 | 1 full pass; CRUD mappings | 021-025, UI-002 | ✅ |
| 6 | `.../api/v1/user/UserApi.java` | 284 | 1 full pass; admin CRUD, enablement, profile | NN-011..016, 019-020 | ✅ |
| 7 | `.../api/v1/user/AuthenticateUserApi.java` | 128 | 1 full pass; login and refresh | NN-005..008, 010 | ✅ |
| 8 | `.../api/v1/user/ResetUserPasswordApi.java` | 133 | 1 full pass; request, verify, complete | NN-017..018 | ✅ |
| 9 | `.../services/customer/CustomerServiceImpl.java` | 125 | 1 full pass; lookup, persistence, delete | 006, 017 | ✅ |
| 10 | `.../services/customer/attribute/CustomerAttributeServiceImpl.java` | 79 | 1 full pass; assignment queries/deletion | 015, 017-018 | ✅ |
| 11 | `.../services/customer/attribute/CustomerOptionServiceImpl.java` | 97 | 1 full pass; option save/delete cascade | 015, 018 | ✅ |
| 12 | `.../services/customer/attribute/CustomerOptionValueServiceImpl.java` | 96 | 1 full pass; value save/delete cascade | 015, 018 | ✅ |
| 13 | `.../services/customer/optin/CustomerOptinServiceImpl.java` | 52 | 1 full pass; save/delete/find | 026-027 | ✅ |
| 14 | `.../services/customer/review/CustomerReviewServiceImpl.java` | 109 | 1 full pass; aggregate calculation and lookup | 021-025 | ✅ |
| 15 | `.../model/customer/Customer.java` | 358 | 1 full pass; fields, constraints, relationships | 001-007, 016-017, 023-027 | ✅ |
| 16 | `.../model/customer/review/CustomerReview.java` | 159 | 1 full pass; rating, pair, status, links | 021-025 | ✅ |
| 17 | `.../model/customer/attribute/CustomerAttribute.java` | 105 | 1 full pass; option/value/customer links | 015, 018 | ✅ |
| 18 | `.../model/customer/connection/UserConnection.java` | 14 | 1 full pass; deprecated entity marker | NN-021 | ✅ |
| 19 | `.../model/customer/connection/RemoteUser.java` | 54 | 1 full pass; provider identity fields | NN-021 | ✅ |
| 20 | `.../store/facade/customer/CustomerFacadeImpl.java` | 258 | 1 full pass; reset request/verify/reset | NN-001..004 | ✅ |
| 21 | `.../controller/customer/facade/CustomerFacadeImpl.java` | 1122 | 4 passes: 1-300 orchestration; 301-600 registration/address; 601-900 update/review/opt-in; 901-1122 reset/password | 001-008, 011-018, 021-028, UI-001 | ✅ |
| 22 | `.../repositories/customer/CustomerRepositoryImpl.java` | 147 | 1 full pass; count/list predicates and pagination | 006, 009-010 | ✅ |
| 23 | `.../repositories/user/UserRepositoryImpl.java` | 104 | 1 full pass; store query and pagination | NN-012 | ✅ |
| 24 | `.../services/user/UserServiceImpl.java` | 141 | 1 full pass; store lookup/list/reset lookup | NN-012, 015, 017-018, 020 | ✅ |
| 25 | `.../populator/customer/CustomerPopulator.java` | 268 | 1 full pass; password, addresses, references, attributes | 003-005, 011-016 | ✅ |
| 26 | `.../security/JWTTokenUtil.java` | 193 | 1 full pass; claims, expiry, refresh, validation | NN-005..008 | ✅ |
| 27 | `.../security/customer/JWTCustomerAuthenticationProvider.java` | 74 | 1 full pass; credential verification | 019, NN-010 | ✅ |
| 28 | `.../security/customer/JWTCustomerAuthenticationManager.java` | 92 | 1 full pass; bearer extraction and token validation | NN-009 | ✅ |
| 29 | `.../security/customer/JWTCustomerServicesImpl.java` | 56 | 1 full pass; principal mapping | 020 | ✅ |
| 30 | `.../security/admin/JWTAdminAuthenticationProvider.java` | 71 | 1 full pass; credential verification | NN-010 | ✅ |
| 31 | `.../security/admin/JWTAdminAuthenticationManager.java` | 94 | 1 full pass; bearer parsing and validation | NN-005..008 | ✅ |
| 32 | `.../security/admin/JWTAdminServicesImpl.java` | 114 | 1 full pass; principal/group/permission mapping | NN-010, 019 | ✅ |
| 33 | `.../security/PasswordRequest.java` | 34 | 1 full pass; current/repeat fields | NN-003..004, NN-016 | ✅ |
| 34 | `.../security/ResetPasswordRequest.java` | 49 | 1 full pass; username/return URL fields | NN-001, NN-017 | ✅ |

**CAST-targeted LOC total:** 5,387 (the prior audit rounded this to 5,388).

## Directly referenced source files processed

| File | LOC | Sections read | Rules / vectors |
|---|---:|---|---|
| `.../security/AbstractCustomerServices.java` | 96 | full; customer principal and permissions | BR-CUS-020; ✅ |
| `.../store/facade/user/UserFacadeImpl.java` | 930 | 3 passes: 1-260 lookup/authz; 261-520 create/update/password; 521-930 listing/reset/enablement | BR-CUS-NN-011..020; ✅ |
| `.../model/user/User.java` | 336 | 2 passes: identity/relationships; status/reset/audit | NN-011..020; ✅ |
| `.../model/user/Group.java` | 120 | full; group type/name and permissions | NN-011, NN-013, NN-015; ✅ |
| `.../model/user/Permission.java` | 97 | full; permission identity | BR-CUS-020, NN-013; ✅ |
| `.../model/common/Billing.java` | 161 | full; billing address fields and country/zone | 011-014, UI-001; ✅ |
| `.../model/common/Delivery.java` | 152 | full; delivery address fields and country/zone | 011-014, UI-001; ✅ |
| `.../model/common/CredentialsReset.java` | 36 | full; token and expiry fields | NN-001..003, NN-017..018; ✅ |
| `.../model/system/optin/CustomerOptin.java` | 145 | full; opt-in fields and unique constraint | 026-027; ✅ |
| `.../model/customer/attribute/CustomerOption.java` | 182 | full; option scope, code, active/public flags | 015, 018; ✅ |
| `.../model/customer/attribute/CustomerOptionValue.java` | 152 | full; value scope, code, image | 015, 018; ✅ |
| `.../model/customer/connection/UserConnectionPK.java` | 64 | full; composite identity key | NN-021; ✅ |
| `.../model/customer/connection/AbstractUserConnectionWithCompositeKey.java` | 60 | full; composite key persistence | NN-021; ✅ |
| `.../model/customer/connection/AbstractUserConnection.java` | 105 | full; provider token metadata | NN-021; ✅ |
| `.../model/merchant/MerchantStore.java` | 427 | 1 full pass; store code, parent, retailer, language/email | 001, 006, 011, NN-011..012, 017; ✅ |
| `.../repositories/customer/optin/CustomerOptinRepository.java` | 19 | full; store/campaign/email lookup predicates | BR-CUS-026..027; ✅ |

**Additional LOC total:** 3,082.  
**Total source LOC read:** 8,469 across 50 files.

## CAST and discovery evidence

The brief bounded the read using CAST transactions `244070` (registration), `243977` (profile), `243978` (address), `244071` (login), `244079-244080` (reset), `244072` (refresh), and `244245-244256` (administrator operations). Direct reads then verified source behavior and exposed defects not represented in the Phase 3 surface grouping. Every rule in `01-business-rules.md` records the relevant transaction reference and exact source line range.

## Files searched but not read

The following were searched or explicitly classified as context-only and were not read because they contain transport-only shapes or generic infrastructure rather than independent business decisions:

- `sm-shop-model/.../Readable*`, `Persistable*`, and generic customer/user DTO variants (except `PasswordRequest` and `ResetPasswordRequest`).
- `sm-core-model/.../CustomerCriteria.java` and `CustomerList.java` (queried only through repository and API behavior).
- Generic mappers, audit/logging helpers, framework configuration, and unrelated merchant/store service implementations.
- CAST application-wide component lists outside the MS-01 brief; no dead-code claim is made for those components.

## Extraction status

- CAST-listed files processed: **34/34**
- Directly referenced files processed: **16**
- Rules extracted: **51**
- Preservation tables: **51/51**, eight dimensions each
- Source vectors: **complete for every processed component**
- Unsupported or defective legacy behavior is recorded as a gap/correction, not silently reproduced.
