# Customer and Identity — API Design

**Base path:** `/api/v1`  
**Port:** `8101`  
**JSON:** camelCase; paths: kebab-case  
**Global context:** `Authorization` for protected calls, `x-tenant-id`, `x-store-id`, and `x-correlation-id`. Store scope is validated through MS-10; no foreign database reads are permitted.

## Customer endpoints

### POST `/customers`
- **Purpose:** Create a customer for an administrator.
- **Auth:** administrator group and authorized store.
- **Request:** `CreateCustomerRequest`
- **Response:** `201 Customer`
- **Driving rules:** BR-CUS-001, BR-CUS-003..005, BR-CUS-015..016

### PUT `/customers/{customerId}`
- **Purpose:** Replace editable customer profile fields.
- **Auth:** administrator group and authorized store.
- **Response:** `200 Customer`
- **Driving rules:** BR-CUS-006..007, BR-CUS-011, BR-CUS-015..016

### PATCH `/customers/{customerId}/address`
- **Purpose:** Update billing and/or delivery address.
- **Auth:** administrator group and authorized store.
- **Response:** `204`
- **Driving rules:** BR-CUS-011..014, BR-UI-001

### DELETE `/customers/{customerId}`
- **Purpose:** Delete a customer and dependent attributes.
- **Auth:** administrator group and authorized store.
- **Response:** `204`
- **Driving rules:** BR-CUS-008, BR-CUS-017

### GET `/customers`
- **Purpose:** List customers within the selected store with filters and pagination.
- **Auth:** administrator group and authorized store.
- **CRUD-only:** No — store scoping and grouped search predicates are business behavior.
- **Response:** `200 CustomerListResponse`
- **Driving rules:** BR-CUS-006, BR-CUS-009, BR-CUS-010

### GET `/customers/{customerId}`
- **Purpose:** Read one customer in the selected store.
- **Auth:** administrator group and authorized store.
- **CRUD-only:** Yes — standard store-scoped read.
- **Response:** `200 Customer`
- **Driving rules:** BR-CUS-006

### GET `/customers/me`
- **Purpose:** Read the authenticated customer's profile.
- **Auth:** customer bearer token.
- **Response:** `200 Customer`
- **Driving rules:** BR-CUS-006..007, BR-CUS-020

### PATCH `/customers/me`
- **Purpose:** Update the authenticated customer's profile.
- **Auth:** customer bearer token.
- **Response:** `200 Customer`
- **Driving rules:** BR-CUS-007, BR-CUS-011, BR-CUS-015..016

### PATCH `/customers/me/address`
- **Purpose:** Update the authenticated customer's addresses.
- **Auth:** customer bearer token.
- **Response:** `204`
- **Driving rules:** BR-CUS-011..014, BR-UI-001

### DELETE `/customers/me`
- **Purpose:** Delete the authenticated customer's account.
- **Auth:** customer bearer token.
- **Response:** `204`
- **Driving rules:** BR-CUS-007, BR-CUS-017

### POST `/customer-auth/registrations`
- **Purpose:** Self-service customer registration followed by token issuance.
- **Auth:** anonymous.
- **Response:** `201 AuthenticationResponse`
- **Driving rules:** BR-CUS-001..005, BR-CUS-015..016, BR-CUS-019..020

### POST `/customer-auth/login`
- **Purpose:** Authenticate a customer.
- **Auth:** anonymous.
- **Response:** `200 AuthenticationResponse`
- **Driving rules:** BR-CUS-019..020, BR-CUS-NN-010

### GET `/customer-auth/refresh`
- **Purpose:** Refresh a customer access token.
- **Auth:** customer bearer token.
- **Response:** `200 AuthenticationResponse`
- **Driving rules:** BR-CUS-NN-005..009

### POST `/customers/me/password`
- **Purpose:** Change the authenticated customer's password.
- **Auth:** customer bearer token.
- **Response:** `204`
- **Driving rules:** BR-CUS-NN-004

## Customer reset endpoints

### POST `/customer-password-resets`
- **Purpose:** Issue a customer reset token and email a link.
- **Auth:** anonymous.
- **Response:** `202 ResetRequestResponse`
- **Driving rules:** BR-CUS-NN-001

### GET `/customer-password-resets/{storeCode}/{token}`
- **Purpose:** Verify a customer reset token.
- **Auth:** anonymous.
- **Response:** `200 ResetTokenValidationResponse`
- **Driving rules:** BR-CUS-NN-002

### POST `/customer-password-resets/{storeCode}/{token}`
- **Purpose:** Complete a customer password reset.
- **Auth:** anonymous with token.
- **Response:** `204`
- **Driving rules:** BR-CUS-NN-002..003

## Newsletter endpoints

### POST `/newsletter-subscriptions`
- **Purpose:** Subscribe or update subscriber details for the newsletter.
- **Auth:** anonymous.
- **Response:** `201 NewsletterSubscription`
- **Driving rules:** BR-CUS-026..027

### PUT `/newsletter-subscriptions/{email}`
- **Purpose:** Legacy advertised update operation.
- **CRUD-only:** No — explicitly unsupported in the legacy implementation; target returns `501` until a supported update contract is approved.
- **Response:** `501 ErrorResponse`
- **Driving rules:** BR-CUS-028

### DELETE `/newsletter-subscriptions/{email}`
- **Purpose:** Unsubscribe a newsletter address.
- **CRUD-only:** No — target capability is required to close the legacy advertised operation.
- **Response:** `204` when implemented; `501` is the compatibility response while capability is disabled.
- **Driving rules:** BR-CUS-028

## Review endpoints

### POST `/customers/{customerId}/reviews`
- **Purpose:** Create a review for a customer.
- **Auth:** authenticated customer.
- **Response:** `201 CustomerReview`
- **Driving rules:** BR-CUS-021..023

### GET `/customers/{customerId}/reviews`
- **Purpose:** List reviews for a customer.
- **Auth:** public read.
- **CRUD-only:** Yes — standard read projection.
- **Response:** `200 CustomerReviewListResponse`
- **Driving rules:** BR-CUS-021, BR-CUS-023

### PUT `/customers/{customerId}/reviews/{reviewId}`
- **Purpose:** Update a review and recompute the target aggregate.
- **Auth:** review owner or authorized moderator.
- **Response:** `200 CustomerReview`
- **Driving rules:** BR-CUS-024, BR-UI-002

### DELETE `/customers/{customerId}/reviews/{reviewId}`
- **Purpose:** Delete a review and recompute the target aggregate.
- **Auth:** review owner or authorized moderator.
- **Response:** `204`
- **Driving rules:** BR-CUS-025, BR-UI-002

## Administrator endpoints

### POST `/admin-auth/login`
- **Purpose:** Authenticate an administrator.
- **Auth:** anonymous.
- **Response:** `200 AuthenticationResponse`
- **Driving rules:** BR-CUS-NN-010

### GET `/admin-auth/refresh`
- **Purpose:** Refresh an administrator access token.
- **Auth:** administrator bearer token.
- **Response:** `200 AuthenticationResponse`
- **Driving rules:** BR-CUS-NN-005..008

### GET `/users/{userId}`
- **Purpose:** Read one administrator in the selected store.
- **Auth:** administrator group and authorized store.
- **CRUD-only:** Yes — standard store-scoped read after authorization.
- **Response:** `200 Administrator`
- **Driving rules:** BR-CUS-NN-012, BR-CUS-NN-019

### POST `/users`
- **Purpose:** Create an administrator.
- **Auth:** administrator group and authorized store.
- **Response:** `201 Administrator`
- **Driving rules:** BR-CUS-NN-011, BR-CUS-NN-013

### PUT `/users/{userId}`
- **Purpose:** Update an administrator.
- **Auth:** authorized administrator.
- **Response:** `200 Administrator`
- **Driving rules:** BR-CUS-NN-013..014

### PATCH `/users/{userId}/password`
- **Purpose:** Change an administrator password.
- **Auth:** authenticated administrator.
- **Response:** `204`
- **Driving rules:** BR-CUS-NN-016

### GET `/users`
- **Purpose:** List administrators with store hierarchy and pagination.
- **Auth:** administrator group and authorized store.
- **CRUD-only:** No — retailer hierarchy expansion and authorization are business behavior.
- **Response:** `200 AdministratorListResponse`
- **Driving rules:** BR-CUS-NN-012, BR-CUS-NN-019

### PATCH `/users/{userId}/enabled`
- **Purpose:** Enable or suspend an administrator in a store.
- **Auth:** administrator group and authorized store.
- **Response:** `204`
- **Driving rules:** BR-CUS-NN-019..020

### DELETE `/users/{userId}`
- **Purpose:** Delete a non-protected administrator.
- **Auth:** administrator group and authorized store.
- **CRUD-only:** No — protected-account rule applies.
- **Response:** `204`
- **Driving rules:** BR-CUS-NN-015

### POST `/users/unique`
- **Purpose:** Check whether an administrator username exists in a store.
- **Auth:** administrator group.
- **Response:** `200 EntityExistsResponse`
- **Driving rules:** BR-CUS-NN-011

### GET `/users/me`
- **Purpose:** Read the authenticated administrator profile.
- **Auth:** administrator bearer token.
- **Response:** `200 Administrator`
- **Driving rules:** BR-CUS-NN-019

## Administrator reset endpoints

### POST `/user-password-resets`
- **Purpose:** Issue an administrator reset token and email a link.
- **Auth:** anonymous.
- **Response:** `202 ResetRequestResponse`
- **Driving rules:** BR-CUS-NN-017

### GET `/user-password-resets/{storeCode}/{token}`
- **Purpose:** Verify an administrator reset token.
- **Auth:** anonymous.
- **Response:** `200 ResetTokenValidationResponse`
- **Driving rules:** BR-CUS-NN-018

### POST `/user-password-resets/{storeCode}/{token}`
- **Purpose:** Complete an administrator password reset.
- **Auth:** anonymous with token.
- **Response:** `204`
- **Driving rules:** BR-CUS-NN-018

## External identity endpoints

### POST `/external-identities`
- **Purpose:** Link a customer or administrator to a provider identity.
- **Auth:** authenticated account.
- **Response:** `201 ExternalIdentityConnection`
- **Driving rules:** BR-CUS-NN-021

## API-to-rule coverage

Every non-CRUD endpoint above has at least one driving rule. The explicitly CRUD-only operations are `GET /customers/{customerId}`, `GET /customers/{customerId}/reviews`, `GET /users/{userId}`, and standard read projections; all other endpoints have validation, authorization, lifecycle, aggregation, or integration behavior.
