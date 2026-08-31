# Customer, User, and Security - Extraction Summary

## Segment Profile

- Scope: customer registration/profile/address/password flows, administrative users/groups, JWT and Spring Security.
- Modules: `sm-shop`, `sm-core`, `sm-core-model`.
- Persistence: customer, billing/delivery, customer attributes, users, groups, permissions, credentials reset.
- Business rules extracted: 28.
- Discovery: direct Java source read; confidence high for persistence/security intent, medium where defects or runtime provider selection require confirmation.

## Call Graph

```text
POST /api/v1/auth/register
  -> AuthenticateCustomerApi.register -> CustomerFacadeImpl.registerCustomer
  -> CustomerPopulator -> CustomerServiceImpl.saveOrUpdate
  -> authenticate -> JWTCustomerAuthenticationProvider -> JWTTokenUtil

Bearer request -> security manager/filter -> JWTTokenUtil -> customer/user lookup
  -> facade authorization -> profile/address/password/customer mutation

Password reset -> request UUID/expiry -> email -> verify store/token/expiry
  -> encode new password -> persist customer
```

Evidence: `AuthenticateCustomerApi.java:94-203`, `CustomerFacadeImpl.java:179-250`,
`CustomerPopulator.java:49-239`, `JWTTokenUtil.java:119-187`,
`MultipleEntryPointsSecurityConfig.java:324-415`.

## Business Rules

| ID | Rule | Source reference | Confidence |
|---|---|---|---|
| BR-CUS-001 | Customer authentication and ownership use `Customer.nick` as username; principal name must match it. | `Customer.java:52-55,82-95`; `CustomerFacadeImpl.authorize:80-90`; `CustomerRepository.findByUserName:10-45` | High |
| BR-CUS-002 | Customer nickname uniqueness is intended to be merchant-scoped, though some lookups are unscoped. | `Customer.java:52-55`; `CustomerServiceImpl.getByNick:45-52` | High/Medium |
| BR-CUS-003 | Storefront registration overwrites username with email address. | `AuthenticateCustomerApi.register:94-150`; `CustomerFacadeImpl.registerCustomer:179-250`; `CustomerPopulator:66-70` | High |
| BR-CUS-004 | Billing first/last name and billing country are required for normal persistence. | `Billing.java:17-23,53-55`; `Customer.java:124-129`; `AuthenticateCustomerApi.register:112-139` | High |
| BR-CUS-005 | Address country and zone codes resolve through reference services and reject unsupported references. | `CustomerPopulator:101-185`; `CustomerServiceImpl.update:70-112` | High |
| BR-CUS-006 | Customers with no groups receive default `CUSTOMER` group. | `CustomerFacadeImpl.setCustomerModelDefaultProperties:397-430`; `GroupRepository.findByName:12-33` | High |
| BR-CUS-007 | Authorities derive from groups and permissions plus customer authentication authority. | `AbstractCustomerServices.loadUserByUsername:27-91`; `Group.java:62-71`; `Permission.java:48-53` | High |
| BR-CUS-008 | Customer profile/address/password/delete operations require principal name equal to customer nickname. | `CustomerFacadeImpl.authorize:80-90`; `CustomerApi.java:62-199` | High |
| BR-CUS-009 | Successful registration persists, authenticates, and returns a JWT. | `AuthenticateCustomerApi.register:94-150`; `JWTTokenUtil.generateToken:119-137` | High |
| BR-CUS-010 | Customer passwords are BCrypt encoded before persistence. | `CustomerPopulator:49-70`; `CustomerFacadeImpl.resetPassword:209-219`; security config `45-57` | High |
| BR-CUS-011 | Billing and delivery address updates can be performed independently. | `CustomerFacadeImpl.updateCustomer:582-716`; `CustomerPopulator:106-185` | Medium/High |
| BR-CUS-012 | Customer option/value attributes must exist and belong to the current store. | `CustomerPopulator:210-239`; `CustomerAttribute.java:20-54` | High |
| BR-CUS-013 | Customer attributes are removed before customer deletion. | `CustomerServiceImpl.delete:45-68`; `CustomerApi.deleteCustomer:131-199` | High |
| BR-CUS-014 | Password reset requests create UUID tokens with two-day expiry and send email. | `CustomerFacadeImpl.requestPasswordReset:93-127`; `CredentialsReset.java:10-18` | High |
| BR-CUS-015 | Reset token lookup is store-scoped and expiry checked; token is not visibly cleared after reset. | `CustomerFacadeImpl.verifyCustomerLink:224-248`; `ResetCustomerPasswordApi:78-124` | High |
| BR-CUS-016 | Admin user DTO group names resolve to persistent groups. | `PersistableUserPopulator:44-102`; `GroupRepository.listGroupByNames:12-33` | High |
| BR-CUS-017 | Admin facade password policy requires upper/lowercase, digit, and length 6-12. | `SecurityFacadeImpl.USER_PASSWORD_PATTERN:25-27`; `validateUserPassword:63-68` | High |
| BR-CUS-018 | Customer/admin password policies are inconsistent across facade, credentials service, and React validation. | `SecurityFacadeImpl:25-68`; `CredentialsServiceImpl:22-56`; React `LoginRegister.js:156-270` | High |
| BR-CUS-019 | Custom password providers appear to compare the raw password with username instead of stored encoded password. | `JWTAdminAuthenticationProvider:36-64`; `JWTCustomerAuthenticationProvider:29-57` | High source concern |
| BR-CUS-020 | Admin mutations require permitted administrative groups. | `UserApi.java:78-278`; `UserFacadeImpl.authorizedGroups:127-190` | Medium/High |
| BR-CUS-021 | Non-superadmins are intended not to assign `SUPERADMIN`; update filtering contains a probable comparison typo. | `UserFacadeImpl.authorizedGroups:127-190`; `UserFacadeImpl.update:303-525` | Medium |
| BR-CUS-022 | Store authorization intends same-store and parent/child access; `findByStore` may fail to verify user membership. | `UserFacadeImpl.authorizedStore:303-390`; `UserServiceImpl.findByStore:77-89` | Medium |
| BR-CUS-023 | Superadmin deletion protection is intended but compares a group collection to a string constant. | `UserFacadeImpl.delete:527-731` | Medium/High |
| BR-CUS-024 | JWTs use HS512, subject, audience, issued-at, configured expiry, and secret. | `JWTTokenUtil.java:39-54,119-187`; `authentication.properties:2-7` | High |
| BR-CUS-025 | `canTokenBeRefreshedWithGrace` calculates checks but returns true unconditionally. | `JWTTokenUtil.canTokenBeRefreshedWithGrace:140-151` | High |
| BR-CUS-026 | Customer bearer-token extraction and security-context assignment appear commented/incomplete. | `JWTCustomerAuthenticationManager.attemptAuthentication:36-75` | High |
| BR-CUS-027 | Admin private and customer authentication routes use separate security chains and authorities; CSRF is disabled. | `MultipleEntryPointsSecurityConfig:324-415` | High |
| BR-CUS-028 | Password reset paths encode new passwords but do not visibly apply the normal policy validator. | `CustomerFacadeImpl.resetPassword:209-219`; `UserFacadeImpl:793-927`; `SecurityFacadeImpl:63-68` | Medium |

## Data Access and Integrations

| Area | Create/read/update/delete and integrations |
|---|---|
| Customer | Customer and embedded billing/delivery are persisted through `CustomerServiceImpl`; repository supports username, store, and reset-token queries. |
| Attributes | Customer attributes and option/value references are validated against store-owned option data. |
| Users/groups | User CRUD resolves groups by name; permissions are read through group relationships. |
| Security | Spring Security chains, custom admin/customer providers, JWT utility, password encoder, and bearer filters. |
| Reset | Customer repository, merchant URL builder, email templates, locale/language services. |

## Layer A/B/C Flags

- Lifecycle: registered/authenticated/updated/deleted customer; reset requested/verified/consumed; active admin user; group membership.
- Invariants: principal/customer identity alignment, merchant-scoped uniqueness, valid address references, protected superadmin, single-use reset token, encoded passwords.
- Extensibility: group/permission matrix, merchant-store hierarchy, configurable JWT expiry and password encoder.
- Placement candidates: identity/authorization in domain boundary; JWT validation at edge; repository-level tenant predicates; no database-tier decision yet.

## Source Semantic Vectors

| Component | Control | Data | Constants | States | Outcomes | Writes | Integrations | Errors |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `CustomerFacadeImpl` | 88 | 62 | 16 | 12 | 26 | 11 | 13 | 29 |
| `CustomerServiceImpl` | 42 | 34 | 8 | 8 | 14 | 9 | 8 | 17 |
| `CustomerPopulator` | 51 | 47 | 10 | 3 | 18 | 2 | 9 | 19 |
| `AuthenticateCustomerApi` | 34 | 21 | 6 | 4 | 17 | 0 | 7 | 12 |
| `UserFacadeImpl` | 78 | 55 | 15 | 14 | 24 | 10 | 11 | 31 |
| `JWTTokenUtil` | 28 | 21 | 10 | 5 | 11 | 0 | 2 | 15 |
| `JWTAdminAuthenticationProvider` | 18 | 13 | 3 | 3 | 7 | 0 | 3 | 10 |
| `JWTCustomerAuthenticationProvider` | 18 | 13 | 3 | 3 | 7 | 0 | 3 | 10 |

## Clarification Items

Confirm global versus merchant-scoped identity, password policy authority, custom provider activation, JWT refresh/token extraction defects, store authorization, superadmin deletion, reset-token single use, address setter defects, hard-delete versus anonymization, and customer/admin route security before migration.
