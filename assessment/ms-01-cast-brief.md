# CAST Scout Brief: Customer and Identity (MS-01)

**Analysis mode:** Hybrid (CAST structure + direct source read)  
**CAST application:** `Shopizer-Backend`  
**Local root mapping:** `§{main_sources}§` → `initial-source/shopizer-3.2.7/`

## Entry Points

CAST identified the following customer and identity transactions:

| Transaction | CAST ID | Reduced / full objects | Complexity signal |
|---|---:|---:|---|
| Customer registration | 244070 | 144 / 3072 | Highest-risk flow |
| Customer profile | 243977 | 18 / 298 | Profile read |
| Customer address | 243978 | 31 / 435 | Address lifecycle |
| Customer login | 244071 | 5 / 63 | Authentication |
| Customer password reset request | 244079 | 33 / 326 | Credential recovery |
| Customer password reset | 244080 | 21 / 58 | Credential recovery |
| Customer refresh | 244072 | 1 / 63 | Token refresh |
| User create/update/delete/list | 244248–244256 | 13–48 reduced | Administration |
| User password reset | 244245–244247 | 14–29 reduced | Credential recovery |

## Source Files to Read

These files are the primary business-logic candidates. Read complete files, using the Java legacy
multi-pass protocol for files over 500 lines.

| Local path | CAST evidence / inclusion reason |
|---|---|
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/CustomerApi.java` | Customer profile, registration, address, and account operations |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/AuthenticateCustomerApi.java` | Customer login, refresh, and authentication flow |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/ResetCustomerPasswordApi.java` | Customer reset-token and password-reset flows |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/CustomerNewsletterApi.java` | Customer opt-in/opt-out behavior |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/CustomerReviewApi.java` | Customer review ownership and moderation-facing behavior |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/user/UserApi.java` | Administrative user lifecycle and enablement |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/user/AuthenticateUserApi.java` | Administrative user authentication |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/user/ResetUserPasswordApi.java` | Administrative credential recovery |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/customer/CustomerServiceImpl.java` | Customer persistence, uniqueness, lifecycle, and lookup rules |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/customer/attribute/CustomerAttributeServiceImpl.java` | Customer attribute behavior |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/customer/attribute/CustomerOptionServiceImpl.java` | Customer option definitions |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/customer/attribute/CustomerOptionValueServiceImpl.java` | Customer option value behavior |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/customer/optin/CustomerOptinServiceImpl.java` | Newsletter/opt-in persistence |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/customer/review/CustomerReviewServiceImpl.java` | Review persistence and ownership checks |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/customer/Customer.java` | Customer domain fields and lifecycle evidence |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/customer/review/CustomerReview.java` | Review domain fields |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/customer/attribute/CustomerAttribute.java` | Attribute domain fields |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/customer/connection/UserConnection.java` | External identity connection data |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/customer/connection/RemoteUser.java` | Remote identity mapping |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/customer/CustomerFacadeImpl.java` | Registration, profile, address, review, and customer account orchestration |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java` | Alternate customer facade implementation referenced by API wiring |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/customer/CustomerRepositoryImpl.java` | Customer query predicates, uniqueness, and scope behavior |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/user/UserRepositoryImpl.java` | User query predicates, uniqueness, and enablement lookup |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/user/UserServiceImpl.java` | Administrative user lifecycle and authorization data |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/customer/CustomerPopulator.java` | Customer response mapping and nested address/attribute semantics |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/JWTTokenUtil.java` | JWT claims, expiration, and token validation |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/customer/JWTCustomerAuthenticationProvider.java` | Customer authentication and credential verification |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/customer/JWTCustomerAuthenticationManager.java` | Customer authentication manager behavior |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/customer/JWTCustomerServicesImpl.java` | Customer security principal and token service behavior |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/admin/JWTAdminAuthenticationProvider.java` | Administrative credential verification |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/admin/JWTAdminAuthenticationManager.java` | Administrative authentication manager behavior |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/admin/JWTAdminServicesImpl.java` | Administrative security principal and token service behavior |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/PasswordRequest.java` | Password-change request fields and validation |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/ResetPasswordRequest.java` | Password-reset request fields and validation |

## Context-Only / Skip Unless Referenced

| Path family | Reason |
|---|---|
| `sm-shop-model/.../Readable*`, `Persistable*` DTOs | Include only when field/validation semantics affect a rule |
| `sm-core-model/.../CustomerCriteria.java`, `CustomerList.java` | Read only to resolve query predicates and pagination semantics |
| Generic mappers, logging, audit, and framework configuration | Infrastructure unless they alter authorization, persistence, or transaction behavior |

## Owned Data Candidates

Customer, address, customer attributes/options, customer reviews, opt-ins, users, roles/groups,
permissions, sessions, credential-reset tokens, and external identity connections. Exact table
names and columns must be derived from JPA annotations and repository queries during extraction.

## Cross-Service Dependencies

| Dependency | Evidence / purpose |
|---|---|
| OIDC or external identity provider | Authentication/token exchange if present in source |
| MS-10 Merchant and Store Administration | Store/tenant scope validation if customer/user operations require it |

## Existing P1 Rules Requiring P4 Upgrade

Re-extract all assigned rules rather than copying summaries:

- `BR-CUS-001..028`
- `BR-UI-001..002` where the rule is an identity/customer interaction

## Hidden-Engine Check

The CAST surface is substantially larger than a small CRUD baseline for customer and user entities.
The residual is expected to contain an identity engine: registration uniqueness and activation,
authentication/token refresh, password-reset token validity, authorization/enablement, external
identity mapping, and opt-in/review ownership behavior. Treat these as deep business logic, not
generic CRUD, and check for configuration-driven or provider-driven behavior.

## Dead Code

No dead-code determination is made in this brief. The extractor must not include components that
CAST or source evidence identifies as unreachable, audit-only, or generic infrastructure.
