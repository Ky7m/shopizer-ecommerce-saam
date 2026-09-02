# Customer and Identity — Business Rules

**Service ID:** MS-01  
**Port:** 8101  
**Schema:** `customer_identity`  
**Extraction mode:** Hybrid (CAST transaction bounds + direct source read)  
**Scope:** Customer, administrator identity, credentials, addresses, consent, reviews, attributes, and authorization context.

Statements are target-domain statements. Legacy identifiers appear only in evidence and pseudocode.

## Customer registration and profile

### BR-CUS-001: Store-scoped customer identity uniqueness
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java:314-340,326-339`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer registration` (244070)

**Statement:** A customer login identifier may be used once within a store, while the same identifier may be used in another store.
**Intent:** Validation
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
IF userName is non-blank AND store != null
  existing = customerService.getByNick(userName, store.id)
  RETURN existing != null
ELSE RETURN false
```
**Data Dependencies:** Reads `CUSTOMER.CUSTOMER_NICK`, `CUSTOMER.MERCHANT_ID`; writes none.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `POST /api/v1/customer-auth/registrations {"emailAddress":"maya.chen@example.com","password":"S3cure!Pass","billing":{"firstName":"Maya","lastName":"Chen","countryCode":"US"}}` with store `north-america`
- Success Output: `201 {"customerId":"c-1001","accessToken":"..."}`
- Error Input: same email already belongs to a customer in `north-america`
- Error Output: `409 {"error":"CUSTOMER_IDENTITY_CONFLICT","message":"Login identifier is already registered for this store","statusCode":409}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 2 | 2 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

## Credential recovery and token behavior

### BR-CUS-NN-001: Reset requests create a two-day credential token
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/customer/CustomerFacadeImpl.java:94-130`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer password reset request` (244079)

**Statement:** A valid customer reset request creates a random token that expires two days after issuance, stores it for the selected store, and sends a reset link by email.
**Intent:** State Transition
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
customer = customerService.getByNick(customerName, store.id)
token = UUID.randomUUID()
expiry = DateUtil.addDaysToCurrentDate(2)
customer.credentialsResetRequest = {credentialsRequest: token, credentialsRequestExpiry: expiry}
customerService.saveOrUpdate(customer)
resetLink = buildBaseUrl(returnUrl, store) + "customer/{store.code}/reset/{token}"
sendHtmlEmail(store, customer.emailAddress, resetLink)
```
**Data Dependencies:** Reads `CUSTOMER.CUSTOMER_NICK`, `CUSTOMER_EMAIL_ADDRESS`, `MERCHANT_STORE.STORE_CODE`; writes `CUSTOMER.RESET_CREDENTIALS_REQ`, `RESET_CREDENTIALS_EXP`.  
**Side Effects:** Sends password-reset email asynchronously.
**Concrete Example:**
- API Input: `POST /api/v1/customer-password-resets {"username":"ava@example.com","returnUrl":"https://shop.example/reset"}`
- Success Output: `202 {"status":"ResetLinkSent"}`
- Error Input: username not found in store
- Error Output: `404 {"error":"CUSTOMER_NOT_FOUND","message":"Customer was not found for this store","statusCode":404}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 2 | OK |

### BR-CUS-NN-002: Reset tokens are store-bound and time-limited
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/customer/CustomerFacadeImpl.java:200-247`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer password reset` (244080)

**Statement:** A reset token is valid only for the store that issued it and until its expiry instant; missing or expired tokens are rejected.
**Intent:** Compliance
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
customer = customerService.getByPasswordResetToken(store, token)
IF customer == null reject
expiry = customer.credentialsResetRequest.credentialsRequestExpiry
IF expiry == null reject configuration
IF now > expiry reject expired
RETURN customer
```
**Data Dependencies:** Reads `CUSTOMER.RESET_CREDENTIALS_REQ`, `RESET_CREDENTIALS_EXP`, `MERCHANT_ID`.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `GET /api/v1/customer-password-resets/default/9b2...`
- Success Output: `200 {"valid":true}`
- Error Input: same token under another store or after expiry
- Error Output: `410 {"error":"RESET_TOKEN_INVALID","message":"Reset token is invalid or expired","statusCode":410}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | N/A |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 0 | 0 | N/A |
| Error paths | 2 | 2 | OK |

### BR-CUS-NN-003: Reset completion must consume the token and enforce password policy
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java:209-220; initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/ResetCustomerPasswordApi.java:105-124`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer password reset` (244080)

**Statement:** Completing a reset requires matching non-blank passwords, a valid token, and the configured password policy; successful completion stores the encoded password and invalidates the token.
**Intent:** Compliance
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
REQUIRE password and repeatPassword are non-blank and equal
customer = verifyCustomerLink(token, store)
REQUIRE passwordPolicy.accepts(password)
customer.password = passwordEncoder.encode(password)
customer.credentialsResetRequest = null
customerService.save(customer)
```
**Data Dependencies:** Reads/writes `CUSTOMER.CUSTOMER_PASSWORD`, `RESET_CREDENTIALS_REQ`, `RESET_CREDENTIALS_EXP`.  
**Side Effects:** Existing reset links cannot be replayed.
**Concrete Example:**
- API Input: `POST /api/v1/customer-password-resets/default/9b2... {"password":"New!Pass2026","repeatPassword":"New!Pass2026"}`
- Success Output: `204`
- Error Input: `"password":"short","repeatPassword":"short"`
- Error Output: `422 {"error":"PASSWORD_POLICY_FAILED","message":"Password does not meet policy","statusCode":422}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 0 | 1 | GAP |
| State transitions | 1 | 2 | GAP |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 2 | GAP |
| Integrations | 0 | 0 | N/A |
| Error paths | 2 | 3 | GAP |

**Preservation:** FLAGGED — source does not visibly clear token or apply policy; both are required target corrections.

### BR-CUS-NN-004: Authenticated password changes require current-password proof
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/AuthenticateCustomerApi.java:215-244; initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java:1106-1119`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** A signed-in customer may change a password only after proving the current password and submitting matching new-password fields.
**Intent:** Authorization
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
customer = getCustomerByUserName(passwordRequest.username, store)
IF customer == null return 404
IF NOT passwordMatch(passwordRequest.current, customer) reject
IF password != repeatPassword reject
changePassword(customer, password)
```
**Data Dependencies:** Reads `CUSTOMER.CUSTOMER_NICK`, `CUSTOMER.CUSTOMER_PASSWORD`; writes `CUSTOMER.CUSTOMER_PASSWORD`.  
**Side Effects:** Password encoding.
**Concrete Example:**
- API Input: `POST /api/v1/customers/me/password {"username":"ivy@example.com","current":"Old!Pass1","password":"New!Pass2","repeatPassword":"New!Pass2"}`
- Success Output: `204`
- Error Input: current password `"wrong"`
- Error Output: `401 {"error":"CURRENT_PASSWORD_INVALID","message":"Current password does not match","statusCode":401}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 2 | 2 | OK |

### BR-CUS-NN-005: JWT claims carry identity, audience, issue time, and expiry
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/JWTTokenUtil.java:110-137`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transactions `Customer login` (244071) and `User create/update/delete/list` (244248-244256)

**Statement:** Every access token identifies its subject, is marked for the API audience, records issuance time, and expires after the configured lifetime.
**Intent:** Compliance
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
createdDate = DateUtil.getDate()
expirationDate = createdDate + expiration * 1000 milliseconds
Jwts.builder().setSubject(userDetails.username).setAudience("api")
  .setIssuedAt(createdDate).setExpiration(expirationDate).signWith(HS512, secret)
```
**Data Dependencies:** Reads configured `jwt.secret`, `jwt.expiration`; writes no database row.  
**Side Effects:** Token signing.
**Concrete Example:**
- API Input: `POST /api/v1/customer-auth/login {"username":"kai@example.com","password":"K!Pass2026"}`
- Success Output: `200 {"accessToken":"eyJ...","claims":{"sub":"kai@example.com","aud":"api","expiresAt":"2026-09-01T14:10:18Z"}}`
- Error Input: token has audience `"unknown"` for API access
- Error Output: `401 {"error":"TOKEN_AUDIENCE_INVALID","message":"Token is not valid for this API","statusCode":401}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 2 | 2 | OK |
| Constants | 5 | 5 | OK |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

### BR-CUS-NN-006: JWT validation rejects expired or pre-reset tokens
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/JWTTokenUtil.java:84-106,173-187`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transactions `Customer refresh` (244072) and `User create/update/delete/list` (244248-244256)

**Statement:** An access token is accepted only when its subject matches the loaded identity, it is not expired, and it was issued after the identity's last password reset.
**Intent:** Authorization
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
usernameEquals = token.subject == user.username
expired = token.expiration.before(now)
createdBeforeReset = lastPasswordReset != null AND token.issuedAt.before(lastPasswordReset)
RETURN usernameEquals AND NOT expired AND NOT createdBeforeReset
```
**Data Dependencies:** Reads token claims and `CUSTOMER.CUSTOMER_NICK` or `USERS.ADMIN_NAME`, audit reset timestamp.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `GET /api/v1/customers/me` with token subject `kai@example.com`
- Success Output: `200 {"id":"c-1017","emailAddress":"kai@example.com"}`
- Error Input: token expired yesterday
- Error Output: `401 {"error":"TOKEN_EXPIRED","message":"Access token has expired","statusCode":401}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | N/A |
| Error paths | 2 | 2 | OK |
| Integrations | 0 | 0 | N/A |

### BR-CUS-NN-007: Refresh is allowed only under the normal refresh predicate
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/JWTTokenUtil.java:140-157`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer refresh` (244072)

**Statement:** A refresh token request may issue a replacement only when the token is not invalidated by a password reset and is unexpired, except for explicitly supported mobile/tablet audiences.
**Intent:** Authorization
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
created = issuedAt(token)
RETURN NOT createdBeforeLastPasswordReset(created, lastPasswordReset)
  AND (NOT isTokenExpired(token) OR ignoreTokenExpiration(token))
```
**Data Dependencies:** Reads token claims and identity reset timestamp.  
**Side Effects:** Replacement token is signed.
**Concrete Example:**
- API Input: `GET /api/v1/customer-auth/refresh` with valid bearer token
- Success Output: `200 {"customerId":"c-1018","accessToken":"new..."}`
- Error Input: token was issued before a password reset
- Error Output: `400 {"error":"REFRESH_NOT_ALLOWED","message":"Token cannot be refreshed","statusCode":400}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-NN-008: Refresh must not be unconditional
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/JWTTokenUtil.java:140-151`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer refresh` (244072)

**Statement:** The refresh decision must evaluate token validity; it must never return success for every input.
**Intent:** Compliance
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
legacy canTokenBeRefreshedWithGrace computes predicates t/u/v and then returns true
target returns the predicate result and records a security event for rejected refreshes
```
**Data Dependencies:** Reads token claims and reset timestamp; writes security audit event in target.  
**Side Effects:** Rejected attempts are auditable.
**Concrete Example:**
- API Input: `GET /api/v1/customer-auth/refresh` with malformed token
- Success Output: `400 {"error":"REFRESH_NOT_ALLOWED","message":"Token cannot be refreshed","statusCode":400}`
- Error Input: arbitrary unsigned token
- Error Output: `401 {"error":"TOKEN_INVALID","message":"Token signature is invalid","statusCode":401}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 6 | GAP |
| Data-flow | 3 | 3 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 0 | 1 | GAP |
| Outcomes | 1 | 2 | GAP |
| Data writes | 0 | 1 | GAP |
| Integrations | 0 | 0 | N/A |
| Error paths | 0 | 2 | GAP |

**Preservation:** FLAGGED — corrective security behavior is net-new.

### BR-CUS-NN-009: Customer token middleware must parse the bearer token before validation
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/customer/JWTCustomerAuthenticationManager.java:36-71`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transactions `Customer login` (244071) and `Customer refresh` (244072)

**Statement:** Customer requests with a bearer header must extract the token, resolve its subject, load the customer, and validate the token before establishing authentication.
**Intent:** Authorization
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
requestHeader = request.getHeader(tokenHeader)
REQUIRE requestHeader startsWith "Bearer "
authToken = requestHeader.substring(7)
username = jwtTokenUtil.getUsernameFromToken(authToken)
userDetails = loadUserByUsername(username)
IF userDetails != null AND validateToken(authToken, userDetails) authenticate
```
**Data Dependencies:** Reads request header, `CUSTOMER`, group and permission joins.  
**Side Effects:** Security context authentication.
**Concrete Example:**
- API Input: `GET /api/v1/customers/me` with `Authorization: Bearer eyJ...`
- Success Output: `200 {"id":"c-1019","emailAddress":"nora@example.com"}`
- Error Input: header `Authorization: Bearer ...` but token extraction yields no token
- Error Output: `401 {"error":"TOKEN_INVALID","message":"Authentication token is invalid","statusCode":401}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** FLAGGED in source implementation because `authToken` is not assigned before validation; target explicitly fixes the net-new finding.

## Administrator identity and authorization

### BR-CUS-NN-010: Administrator authentication uses the encoded stored password
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/admin/JWTAdminAuthenticationProvider.java:36-58`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `User create/update/delete/list` (244248-244256)

**Statement:** An administrator authenticates only when the submitted password matches the stored encoded administrator password.
**Intent:** Authorization
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
user = jwtAdminDetailsService.loadUserByUsername(auth.name)
REQUIRE user != null
REQUIRE passwordEncoder.matches(auth.credentials, user.password)
return authenticated token
legacy code passes auth.name as the encoded-password argument; target uses user.password
```
**Data Dependencies:** Reads `USERS.ADMIN_NAME`, `USERS.ADMIN_PASSWORD`; writes none.  
**Side Effects:** Security context.
**Concrete Example:**
- API Input: `POST /api/v1/admin-auth/login {"username":"admin","password":"Adm!nPass9"}`
- Success Output: `200 {"userId":"u-1","accessToken":"..."}`
- Error Input: wrong password
- Error Output: `401 {"error":"BAD_CREDENTIALS","message":"Username or password is incorrect","statusCode":401}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

**Preservation:** FLAGGED — encoded-password argument defect corrected.

### BR-CUS-NN-011: Administrator creation is store-scoped and policy-validated
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/user/UserFacadeImpl.java:303-361`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `User create/update/delete/list` (244248-244256)

**Statement:** An administrator can be created only if its username is unused in the store, the two submitted passwords match and satisfy policy, and at least one valid group is assigned.
**Intent:** Validation
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
REQUIRE user and store and username
IF userService.getByUserName(username, store.code) != null reject conflict
REQUIRE securityFacade.matchRawPasswords(password, repeatPassword)
REQUIRE securityFacade.validateUserPassword(password)
populate userModel; REQUIRE userModel.groups not empty
userModel.adminPassword = encode(password); save
```
**Data Dependencies:** Reads `USERS.ADMIN_NAME`, `MERCHANT_STORE.STORE_CODE`, groups; writes `USERS`, `USER_GROUP`.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `POST /api/v1/users {"userName":"ops-anna","email":"anna@merchant.example","password":"Adm!nPass9","repeatPassword":"Adm!nPass9","groups":["ADMIN"]}`
- Success Output: `201 {"id":"u-2","userName":"ops-anna","active":true}`
- Error Input: password and repeat password differ
- Error Output: `422 {"error":"PASSWORD_MISMATCH","message":"Passwords must match","statusCode":422}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 3 | 3 | OK |

### BR-CUS-NN-012: Administrator listing follows store hierarchy and pagination
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/user/UserFacadeImpl.java:578-658; initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/user/UserServiceImpl.java:102-120`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `User create/update/delete/list` (244248-244256)

**Statement:** Administrator listings use the requested store, expand retailer stores to their permitted store collection, and return a paginated result.
**Intent:** Authorization
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
store = merchantStoreService.getByCode(criteria.storeCode)
IF store.retailer == true
  criteria.storeIds = merchantStoreService.findAllStoreNames(store.code).ids
  criteria.storeCode = null
PageRequest.of(page,count)
select listByStoreIds OR listAll OR listByStore
```
**Data Dependencies:** Reads `MERCHANT_STORE.STORE_CODE`, retailer/parent lineage, `USERS.MERCHANT_ID`; writes none.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `GET /api/v1/users?storeCode=retailer&page=0&pageSize=20`
- Success Output: `200 {"items":[{"id":"u-3","userName":"manager"}],"pagination":{"page":0,"pageSize":20,"totalItems":8,"totalPages":1}}`
- Error Input: non-authorized admin requests another store
- Error Output: `403 {"error":"STORE_ACCESS_DENIED","message":"Administrator is not authorized for this store","statusCode":403}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 12 | 12 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 2 | OK |

### BR-CUS-NN-013: Only super administrators may assign the super-admin group
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/user/UserFacadeImpl.java:580-610`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `User create/update/delete/list` (244248-244256)

**Statement:** A non-super administrator cannot grant the super-administrator group to another user.
**Intent:** Authorization
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
currentUser = userService.getByUserName(authenticatedUser)
isSuperAdmin = currentUser.groups contains groupName "SUPERADMIN"
FOR requestedGroup IN user.groups
  IF requestedGroup.name == "SUPERADMIN" AND NOT isSuperAdmin reject
```
**Data Dependencies:** Reads `USERS`, `USER_GROUP`, `SM_GROUP.GROUP_NAME`; writes `USER_GROUP` only after authorization.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `PUT /api/v1/users/u-4 {"userName":"bob","groups":["SUPERADMIN"]}`
- Success Output: `200` only for a super administrator
- Error Input: request made by `admin` group
- Error Output: `403 {"error":"SUPERADMIN_ASSIGNMENT_DENIED","message":"Only a super administrator may grant this group","statusCode":403}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-NN-014: User updates preserve protected identity and require correct target authorization
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/user/UserFacadeImpl.java:394-477`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `User create/update/delete/list` (244248-244256)

**Statement:** An administrator may modify only the intended user in an authorized store; self-edits cannot move ownership to another store, and protected super-administrator membership and fields are preserved unless the actor is a super administrator.
**Intent:** Authorization
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
userModel = userService.getById(id); REQUIRE userModel.id == id
auth = userService.getByUserName(authenticatedUser)
requestedExisting = getByUserName(user.userName)
IF requestedExisting != null AND requestedExisting.id != userModel.id reject identity mismatch
IF editing self AND userModel.store.code != store.code reject store change
IF editing another AND actor lacks admin/superadmin/store-admin group AND store changes reject
IF target originally superadmin preserve original groups
IF actor not superadmin preserve original groups and active flag
save updated model
```
**Data Dependencies:** Reads/writes `USERS.USER_ID`, `ADMIN_NAME`, `ACTIVE`, `MERCHANT_ID`, `USER_GROUP`.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `PUT /api/v1/users/u-5 {"userName":"alice","storeCode":"west","firstName":"Alice"}`
- Success Output: `200 {"id":"u-5","userName":"alice","storeCode":"west"}`
- Error Input: non-super admin tries to move own account from `east` to `west`
- Error Output: `403 {"error":"STORE_CHANGE_DENIED","message":"Self-service edits cannot change store ownership","statusCode":403}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 14 | 14 | OK |
| Data-flow | 10 | 10 | OK |
| Constants | 5 | 5 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 4 | 4 | OK |

**Preservation:** FLAGGED — source compares the authenticated string with a user object and duplicates the super-admin predicate; target uses explicit IDs and role names.

### BR-CUS-NN-015: Administrator deletion protects super administrators
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/user/UserFacadeImpl.java:372-392`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `User create/update/delete/list` (244248-244256)

**Statement:** A super-administrator account cannot be deleted; other accounts may be deleted only after store scoping and authorization checks.
**Intent:** Authorization
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
user = userService.findByStore(id, merchant)
REQUIRE user != null
IF user.groups contains group with groupName == SUPERADMIN reject
userService.delete(user)
```
**Data Dependencies:** Reads `USERS.MERCHANT_ID`, `USER_GROUP`, `SM_GROUP.GROUP_NAME`; deletes `USERS` and memberships.  
**Side Effects:** Account removal.
**Concrete Example:**
- API Input: `DELETE /api/v1/users/u-6` where target has group `SUPERADMIN`
- Success Output: `409 {"error":"PROTECTED_ACCOUNT","message":"Super-administrator accounts cannot be deleted","statusCode":409}`
- Error Input: target user is not in the requested store
- Error Output: `404 {"error":"USER_NOT_FOUND","message":"User was not found in this store","statusCode":404}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 2 | 2 | OK |

**Preservation:** FLAGGED — source checks a group collection against a string; target performs a group-name predicate.

### BR-CUS-NN-016: Administrator password changes require current proof and policy validation
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/user/UserFacadeImpl.java:478-523`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `User create/update/delete/list` (244248-244256)

**Statement:** Changing an administrator password requires the current password, a policy-compliant new password, and persistence of the encoded replacement.
**Intent:** Compliance
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
auth = userService.getByUserName(authenticatedUser)
userModel = userService.getById(userId)
REQUIRE securityFacade.matchPassword(userModel.adminPassword, changePassword.password)
REQUIRE securityFacade.validateUserPassword(changePassword.changePassword)
userModel.adminPassword = securityFacade.encodePassword(changePassword.changePassword)
userService.update(userModel)
```
**Data Dependencies:** Reads/writes `USERS.ADMIN_PASSWORD`; reads `USERS.USER_ID`, `ADMIN_NAME`.  
**Side Effects:** Token invalidation should follow password change.
**Concrete Example:**
- API Input: `PATCH /api/v1/users/u-7/password {"password":"Old!Pass1","changePassword":"New!Pass2"}`
- Success Output: `204`
- Error Input: new password `"abc"`
- Error Output: `422 {"error":"PASSWORD_POLICY_FAILED","message":"Password does not meet policy","statusCode":422}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 2 | GAP |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 1 | GAP |
| Error paths | 2 | 2 | OK |

**Preservation:** FLAGGED — token invalidation is a target integration requirement.

### BR-CUS-NN-017: Administrator reset requests use the same two-day token lifecycle
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/user/UserFacadeImpl.java:827-878`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `User password reset` (244245-244247)

**Statement:** A valid administrator reset request stores a random token for two days, scoped to the store, and emails a reset link to the administrator.
**Intent:** State Transition
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
user = userService.getByUserName(userName, store.code)
token = UUID.randomUUID()
expiry = DateUtil.addDaysToCurrentDate(2)
user.credentialsResetRequest = {credentialsRequest: token, credentialsRequestExpiry: expiry}
userService.saveOrUpdate(user)
REQUIRE filePathUtils.isValidURL(userContextPath)
send reset link to user.adminEmail
```
**Data Dependencies:** Reads/writes `USERS.ADMIN_NAME`, `ADMIN_EMAIL`, `MERCHANT_ID`, reset fields.  
**Side Effects:** Sends email.
**Concrete Example:**
- API Input: `POST /api/v1/user-password-resets {"username":"admin@merchant.example","returnUrl":"https://admin.example/reset"}`
- Success Output: `202 {"status":"ResetLinkSent"}`
- Error Input: return URL `"not-a-url"`
- Error Output: `422 {"error":"INVALID_RETURN_URL","message":"Return URL is invalid","statusCode":422}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 2 | OK |

### BR-CUS-NN-018: Administrator reset completion invalidates the token
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/user/UserFacadeImpl.java:854-900`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `User password reset` (244245-244247)

**Statement:** An administrator reset token must be valid and unexpired, and successful completion stores an encoded policy-compliant password and consumes the token.
**Intent:** Compliance
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
user = verifyUserLink(token, store)
REQUIRE passwordPolicy.accepts(password)
user.adminPassword = passwordEncoder.encode(password)
user.credentialsResetRequest = null
userService.save(user)
```
**Data Dependencies:** Reads/writes `USERS.ADMIN_PASSWORD`, reset fields, `MERCHANT_ID`.  
**Side Effects:** Prior reset links are invalidated.
**Concrete Example:**
- API Input: `POST /api/v1/user-password-resets/default/tok-7 {"password":"N3w!Admin9","repeatPassword":"N3w!Admin9"}`
- Success Output: `204`
- Error Input: expired token
- Error Output: `410 {"error":"RESET_TOKEN_INVALID","message":"Reset token is invalid or expired","statusCode":410}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 0 | 1 | GAP |
| State transitions | 1 | 2 | GAP |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 2 | GAP |
| Integrations | 0 | 0 | N/A |
| Error paths | 2 | 3 | GAP |

**Preservation:** FLAGGED — source lacks visible policy enforcement and token clearing.

### BR-CUS-NN-019: Disabled administrators cannot authenticate
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/user/UserApi.java:270-281; initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/user/User.java:106-114`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `User create/update/delete/list` (244248-244256)

**Statement:** An administrator account marked inactive cannot access its authenticated profile or protected operations until re-enabled.
**Intent:** Authorization
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
user = userFacade.findByUserName(principal.name, null, language)
IF NOT user.active reject UnauthorizedException("User ... not active")
```
**Data Dependencies:** Reads `USERS.ADMIN_NAME`, `USERS.ACTIVE`; writes none.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `GET /api/v1/users/me` with inactive user token
- Success Output: `401 {"error":"USER_INACTIVE","message":"User account is inactive","statusCode":401}`
- Error Input: inactive user attempts update
- Error Output: same `401` response
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 2 | 2 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-NN-020: User enablement is store-scoped
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/user/UserFacadeImpl.java:624-649`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `User create/update/delete/list` (244248-244256)

**Statement:** Only an administrator authorized for a store may change the active flag of a user belonging to that store.
**Intent:** Authorization
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
modelUser = userService.findByStore(user.id, store.code)
IF modelUser == null reject
modelUser.active = user.active
userService.saveOrUpdate(modelUser)
```
**Data Dependencies:** Reads `USERS.USER_ID`, `MERCHANT_ID`; writes `USERS.ACTIVE`.  
**Side Effects:** Future authentication reflects the new state.
**Concrete Example:**
- API Input: `PATCH /api/v1/users/u-8/enabled {"active":false}`
- Success Output: `204`
- Error Input: user `u-8` belongs to another store
- Error Output: `404 {"error":"USER_NOT_FOUND","message":"User was not found in this store","statusCode":404}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-NN-021: External identity connections use a composite provider key
**Source Reference:** `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/customer/connection/UserConnectionPK.java:11-57; initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/customer/connection/UserConnection.java:6-14`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer login` (244071)

**Statement:** A remote identity connection is uniquely identified by local user identity, provider, and provider-user identity.
**Intent:** Validation
**Classification:** Active
**Weight:** Medium
**Logic:**
```pseudocode
primaryKey = (userId, providerId, providerUserId)
reject a second connection with the same composite key
```
**Data Dependencies:** Reads/writes `USERCONNECTION.userId`, `providerId`, `providerUserId`, access and refresh tokens.  
**Side Effects:** External identity exchange may populate profile metadata.
**Concrete Example:**
- API Input: `POST /api/v1/external-identities {"userId":"c-1020","provider":"google","providerUserId":"g-77","accessToken":"..."}`
- Success Output: `201 {"provider":"google","providerUserId":"g-77"}`
- Error Input: same composite identity already linked
- Error Output: `409 {"error":"IDENTITY_CONNECTION_EXISTS","message":"External identity is already linked","statusCode":409}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |


### BR-CUS-021: Customer review creation is unique per reviewer and target
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java:978-1002; initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/customer/review/CustomerReview.java:33-80`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** A customer may submit at most one review for a given reviewed customer.
**Intent:** Validation
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
existing = customerReviewService.getByReviewerAndReviewed(review.customerId, customerId)
IF existing != null reject duplicate
```
**Data Dependencies:** Reads `CUSTOMER_REVIEW.CUSTOMERS_ID`, `CUSTOMER_REVIEW.REVIEWED_CUSTOMER_ID`; writes `CUSTOMER_REVIEW`.  
**Side Effects:** Review aggregate recalculation.
**Concrete Example:**
- API Input: `POST /api/v1/customers/c-2001/reviews {"customerId":"c-2002","rating":5,"description":"Helpful seller"}`
- Success Output: `201 {"reviewId":"r-1","rating":5}`
- Error Input: same reviewer and target already have review `r-1`
- Error Output: `409 {"error":"DUPLICATE_REVIEW","message":"A review already exists for this customer","statusCode":409}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-022: Review ratings cannot exceed five
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java:994-1000`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** A customer review rating must be within the inclusive range from one through five.
**Intent:** Validation
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
IF review.rating > Constants.MAX_REVIEW_RATING_SCORE (5) reject
target.reviewedCustomer = customerId
```
**Data Dependencies:** Reads/writes `CUSTOMER_REVIEW.REVIEWS_RATING`.  
**Side Effects:** Aggregate rating update.
**Concrete Example:**
- API Input: `POST /api/v1/customers/c-2003/reviews {"customerId":"c-2004","rating":4,"description":"Good communication"}`
- Success Output: `201 {"reviewId":"r-2","rating":4}`
- Error Input: `{"customerId":"c-2004","rating":6,"description":"Too generous"}`
- Error Output: `422 {"error":"RATING_OUT_OF_RANGE","message":"Rating must be between 1 and 5","statusCode":422}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 2 | 2 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-023: Review creation updates the target average and count
**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/customer/review/CustomerReviewServiceImpl.java:27-67`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** After a new review is accepted, the reviewed customer's average rating is recalculated from the prior average and count, and the count increases by one.
**Intent:** Calculation
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
count = reviewedCustomer.customerReviewCount or 0
average = reviewedCustomer.customerReviewAvg or 0
total = average * count + review.reviewRating
count = count + 1
reviewedCustomer.customerReviewAvg = total / count
reviewedCustomer.customerReviewCount = count
save(review)
update(reviewedCustomer)
```
**Data Dependencies:** Reads/writes `CUSTOMER.REVIEW_AVG`, `CUSTOMER.REVIEW_COUNT`; writes `CUSTOMER_REVIEW.REVIEWS_RATING`.  
**Side Effects:** Two customer-domain writes in one service operation.
**Concrete Example:**
- API Input: `POST /api/v1/customers/c-2005/reviews {"customerId":"c-2006","rating":5,"description":"Excellent"}`
- Success Output: `201 {"reviewId":"r-3","targetRatingAverage":4.5,"targetReviewCount":2}`
- Error Input: existing count is `0` but stored average is corrupt
- Error Output: `409 {"error":"REVIEW_AGGREGATE_INVALID","message":"Existing rating aggregate is inconsistent","statusCode":409}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-024: Review updates must persist the changed review and recompute aggregates
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java:1006-1023; initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/CustomerReviewApi.java:88-96`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** Updating a review requires ownership by the target customer, validates the rating range, persists the new content, and recalculates the target aggregate by replacing the prior rating rather than adding another review.
**Intent:** State Transition
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
customerReview = getCustomerReviewById(reviewId)
IF customerReview.reviewedCustomer.id != id reject not found
IF review.rating > 5 reject
legacy method only sets review.reviewedCustomer and returns without save; target must load old rating,
replace it, persist review, and recompute average/count
```
**Data Dependencies:** Reads/writes `CUSTOMER_REVIEW.CUSTOMER_REVIEW_ID`, `REVIEWED_CUSTOMER_ID`, `REVIEWS_RATING`; writes `CUSTOMER.REVIEW_AVG`, `CUSTOMER.REVIEW_COUNT`.  
**Side Effects:** Aggregate update.
**Concrete Example:**
- API Input: `PUT /api/v1/customers/c-2007/reviews/r-4 {"rating":3,"description":"Updated after follow-up"}`
- Success Output: `200 {"reviewId":"r-4","rating":3,"targetRatingAverage":3.5}`
- Error Input: `reviewId:"r-4"` belongs to target `c-2008`
- Error Output: `404 {"error":"REVIEW_NOT_FOUND","message":"Review does not belong to this customer","statusCode":404}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 2 | GAP |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 3 | GAP |
| Integrations | 0 | 0 | N/A |
| Error paths | 2 | 2 | OK |

**Preservation:** FLAGGED (state transitions, data writes) — source defect is explicitly carried as a target correction.

### BR-CUS-025: Review deletion must recalculate the target aggregate
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java:784-806; initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/customer/review/CustomerReviewServiceImpl.java:69-79`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** Deleting a review requires ownership by the target customer and must remove the review while recomputing the target average and count from the remaining reviews.
**Intent:** Calculation
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
review = getCustomerReviewById(reviewId)
IF review.reviewedCustomer.id != customerId reject
delete(review)
targetReviews = all reviews for target
target.count = size(targetReviews)
target.average = sum(r.rating) / count, or 0 when empty
save target
```
**Data Dependencies:** Reads/deletes `CUSTOMER_REVIEW`; writes `CUSTOMER.REVIEW_AVG`, `CUSTOMER.REVIEW_COUNT`.  
**Side Effects:** None observed in source; target operation is transactional.
**Concrete Example:**
- API Input: `DELETE /api/v1/customers/c-2009/reviews/r-5`
- Success Output: `204`; target aggregate becomes `average:4,count:1`
- Error Input: review belongs to another target
- Error Output: `404 {"error":"REVIEW_NOT_FOUND","message":"Review does not belong to this customer","statusCode":404}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 5 | GAP |
| Data-flow | 3 | 5 | GAP |
| Constants | 0 | 1 | GAP |
| State transitions | 1 | 2 | GAP |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 3 | GAP |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

**Preservation:** FLAGGED (source has no aggregate correction on delete; target requirement prevents stale ratings).

### BR-CUS-026: Newsletter opt-in is idempotent within a store and campaign
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java:809-859; initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/customer/optin/CustomerOptinServiceImpl.java:24-50`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** Repeated newsletter enrollment for the same email, store, and campaign updates the subscriber profile instead of creating a duplicate; a first enrollment creates a dated subscription.
**Intent:** State Transition
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
optinDef = optinService.getOptinByCode(store, NEWSLETTER)
subscription = customerOptinService.findByEmailAddress(store, email, NEWSLETTER)
IF subscription != null update firstName,lastName
ELSE create with email, names, optinDate=now, optin=optinDef, merchantStore=store
save(subscription)
```
**Data Dependencies:** Reads `OPTIN.OPTIN_CODE`, `CUSTOMER_OPTIN.EMAIL`, `OPTIN_ID`, `MERCHANT_ID`; writes `CUSTOMER_OPTIN`.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `POST /api/v1/newsletter-subscriptions {"email":"tara@example.com","firstName":"Tara","lastName":"Nguyen"}`
- Success Output: `201 {"email":"tara@example.com","status":"Subscribed","storeId":"default"}`
- Error Input: newsletter campaign is not configured for the store
- Error Output: `422 {"error":"NEWSLETTER_NOT_CONFIGURED","message":"Newsletter opt-in is unavailable","statusCode":422}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-027: Newsletter uniqueness includes merchant scope
**Source Reference:** `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/optin/CustomerOptin.java:35-72; initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/customer/optin/CustomerOptinRepository.java:9-17`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** The same email may subscribe independently in different stores; uniqueness is scoped by store and newsletter campaign.
**Intent:** Compliance
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
legacy entity unique constraint is (EMAIL, OPTIN_ID) and omits MERCHANT_ID
repository lookup receives merchantId, optinCode, email
target database unique key = (merchant_id, optin_id, normalized_email)
```
**Data Dependencies:** Reads/writes `CUSTOMER_OPTIN.EMAIL`, `OPTIN_ID`, `MERCHANT_ID`.  
**Side Effects:** None.
**Concrete Example:**
- API Input: two `POST /api/v1/newsletter-subscriptions` requests for `alex@example.com`, stores `east` and `west`
- Success Output: `201` for each store with separate subscription IDs
- Error Input: second enrollment for same email and same store
- Error Output: `200 {"email":"alex@example.com","status":"Subscribed"}` (idempotent update, not duplicate)
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-028: Unsupported newsletter mutations are explicit capability gaps
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/CustomerNewsletterApi.java:66-91`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** Newsletter modification and unsubscribe endpoints are advertised by the legacy surface but do not execute; the target must expose a deliberate unsubscribe capability rather than silently returning success.
**Intent:** Validation
**Classification:** Active
**Weight:** Medium
**Logic:**
```pseudocode
PUT /newsletter/{email} -> throw UnsupportedOperationException
DELETE /newsletter/{email} -> throw UnsupportedOperationException
```
**Data Dependencies:** None read or written by the legacy methods.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `DELETE /api/v1/newsletter-subscriptions/lee@example.com`
- Success Output: `204 {"status":"Unsubscribed"}` in target implementation
- Error Input: unsupported legacy path without a target unsubscribe implementation
- Error Output: `501 {"error":"NEWSLETTER_UNSUBSCRIBE_UNAVAILABLE","message":"Unsubscribe capability is not implemented","statusCode":501}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 1 | 1 | OK |
| Data-flow | 0 | 0 | N/A |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 1 | 2 | GAP |
| Data writes | 0 | 1 | GAP |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

**Preservation:** FLAGGED — net-new target capability required to close an advertised but unsupported operation.

### BR-UI-001: Profile forms preserve separate billing and delivery addresses
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/CustomerApi.java:159-182; initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/customer/CustomerPopulator.java:106-188`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer address` (243978)

**Statement:** Customer profile screens and APIs must allow billing and delivery addresses to be edited independently while preserving state, postal code, country, and zone values.
**Intent:** Validation
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
authenticated address request selects billing or delivery
CustomerPopulator maps sourceBilling and sourceShipping into separate embedded objects
```
**Data Dependencies:** Reads/writes billing and delivery address columns on `CUSTOMER`.  
**Side Effects:** Customer profile update.
**Concrete Example:**
- API Input: `PATCH /api/v1/customers/me/address {"billing":{"address":"1 Main","city":"Boston","postalCode":"02108","countryCode":"US"},"delivery":{"address":"9 Harbor","city":"Salem","postalCode":"01970","countryCode":"US"}}`
- Success Output: `204`
- Error Input: delivery state is returned as postal code
- Error Output: `500 {"error":"ADDRESS_FIELD_LOSS","message":"Billing and delivery fields must remain distinct","statusCode":500}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-UI-002: Customer review URLs use one review identifier
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/CustomerReviewApi.java:88-104`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** Review modification and deletion links use the same canonical review identifier and must bind it to the target customer.
**Intent:** Routing
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
legacy update mapping declares {reviewid} but method expects reviewId
delete mapping declares {reviewId}
target contract uses /reviews/{reviewId} for both operations
```
**Data Dependencies:** Reads `CUSTOMER_REVIEW.CUSTOMER_REVIEW_ID`, `REVIEWED_CUSTOMER_ID`.  
**Side Effects:** Review mutation.
**Concrete Example:**
- API Input: `PUT /api/v1/customers/c-2010/reviews/r-6 {"rating":4,"description":"Edited"}`
- Success Output: `200 {"reviewId":"r-6","rating":4}`
- Error Input: path uses an unbound identifier
- Error Output: `400 {"error":"INVALID_REVIEW_PATH","message":"reviewId is required","statusCode":400}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 2 | 2 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-002: Registration derives the login identifier from email
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/AuthenticateCustomerApi.java:94-123`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer registration` (244070)

**Statement:** Self-service registration uses the submitted email address as the customer's login identifier.
**Intent:** Routing
**Classification:** Active
**Weight:** Medium
**Logic:**
```pseudocode
customer.userName = customer.emailAddress
IF customerFacade.checkIfUserExists(customer.userName, merchantStore) THEN reject 409
```
**Data Dependencies:** Reads `CUSTOMER.CUSTOMER_NICK`, `CUSTOMER.CUSTOMER_EMAIL_ADDRESS`, `MERCHANT_STORE.MERCHANT_ID`; writes `CUSTOMER.CUSTOMER_NICK`.  
**Side Effects:** Registration persists a customer and starts authentication.
**Concrete Example:**
- API Input: `POST /api/v1/customer-auth/registrations {"emailAddress":"liam.ortiz@example.com","userName":"ignored","password":"Kite!2026","billing":{"firstName":"Liam","lastName":"Ortiz","countryCode":"CA"}}`
- Success Output: `201 {"customerId":"c-1002","accessToken":"..."}`
- Error Input: email already registered in the selected store
- Error Output: `409 {"error":"CUSTOMER_IDENTITY_CONFLICT","message":"Email is already registered","statusCode":409}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-003: Registration requires a billing country
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/AuthenticateCustomerApi.java:118-123`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer registration` (244070)

**Statement:** A new customer cannot be registered unless a billing country is supplied and resolves to a supported country.
**Intent:** Validation
**Classification:** Active
**Weight:** Medium
**Logic:**
```pseudocode
REQUIRE customer.billing != null
REQUIRE customer.billing.country != null
CustomerPopulator resolves billing country code through countries map
IF no match THEN ConversionException("Unsuported country code ...")
```
**Data Dependencies:** Reads `COUNTRY.ISO_CODE`; writes `CUSTOMER.BILLING_COUNTRY_ID`.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `POST /api/v1/customer-auth/registrations {"emailAddress":"ana@shop.example","password":"A1pha!Pass","billing":{"firstName":"Ana","lastName":"Silva"}}`
- Success Output: `422 {"error":"BILLING_COUNTRY_REQUIRED","message":"Billing country is required","statusCode":422}`
- Error Input: `{"emailAddress":"ana@shop.example","password":"A1pha!Pass","billing":{"firstName":"Ana","lastName":"Silva","countryCode":"ZZ"}}`
- Error Output: `422 {"error":"UNSUPPORTED_COUNTRY","message":"Country code ZZ is not supported","statusCode":422}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 2 | 2 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 2 | 2 | OK |

### BR-CUS-004: New customers receive the customer group
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java:398-423,633-645`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer registration` (244070)

**Statement:** Every newly persisted customer is assigned the store's customer group and receives authenticated-customer permissions.
**Intent:** Authorization
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
IF customer has no groups
  groups = groupService.listGroup(CUSTOMER)
  add group whose groupName == Constants.GROUP_CUSTOMER
```
**Data Dependencies:** Reads `SM_GROUP.GROUP_TYPE`, `SM_GROUP.GROUP_NAME`; writes `CUSTOMER_GROUP.CUSTOMER_ID`, `CUSTOMER_GROUP.GROUP_ID`.  
**Side Effects:** Later authentication resolves group permissions.
**Concrete Example:**
- API Input: `POST /api/v1/customer-auth/registrations {"emailAddress":"noah@shop.example","password":"N0ah!Pass","billing":{"firstName":"Noah","lastName":"King","countryCode":"GB"}}`
- Success Output: `201 {"customerId":"c-1003","accessToken":"...","roles":["customer"]}`
- Error Input: store has no configured customer group
- Error Output: `500 {"error":"CUSTOMER_GROUP_CONFIGURATION","message":"Customer access group is not configured","statusCode":500}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

### BR-CUS-005: Customer passwords are stored encoded
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/customer/CustomerPopulator.java:63-70; initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java:377-395`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer registration` (244070)

**Statement:** A customer password is never stored in clear text; registration and password changes persist only a one-way encoded value.
**Intent:** Compliance
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
IF source.password is not blank
  target.password = passwordEncoder.encode(source.password)
During registration, getCustomerModel may encode customer.password again before save
```
**Data Dependencies:** Writes `CUSTOMER.CUSTOMER_PASSWORD`; reads request password.  
**Side Effects:** Password encoder invocation.
**Concrete Example:**
- API Input: `POST /api/v1/customer-auth/registrations {"emailAddress":"zoe@shop.example","password":"Zoe!Strong9","billing":{"firstName":"Zoe","lastName":"Park","countryCode":"US"}}`
- Success Output: `201 {"customerId":"c-1004","accessToken":"..."}`
- Error Input: persistence inspection attempts to find `Zoe!Strong9` as stored password
- Error Output: `500 {"error":"CREDENTIAL_STORAGE_VIOLATION","message":"Clear-text password storage is prohibited","statusCode":500}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 2 | 2 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

### BR-CUS-006: Customer lookup is store-scoped
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java:287-308; initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/customer/CustomerRepositoryImpl.java:35-44`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** Customer identity and profile reads must resolve the customer within the requested store boundary.
**Intent:** Authorization
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
customerService.getByNick(userName, merchantStore.id)
repository predicate includes c.merchantStore.id = :mId
```
**Data Dependencies:** Reads `CUSTOMER.CUSTOMER_NICK`, `CUSTOMER.MERCHANT_ID`; writes none.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `GET /api/v1/customers/me` with `x-store-id: west`
- Success Output: `200 {"id":"c-1005","emailAddress":"sam@shop.example","storeId":"west"}`
- Error Input: valid customer identifier exists only in store `east`
- Error Output: `404 {"error":"CUSTOMER_NOT_FOUND","message":"Customer is not registered in this store","statusCode":404}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 2 | 2 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-007: Customer self-service operations use the authenticated principal
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/CustomerApi.java:149-204`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** A customer may read, modify, change the address of, or delete only the profile identified by the authenticated principal.
**Intent:** Authorization
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
principal = request.userPrincipal.name
customerFacade.getCustomerByNick(principal, merchantStore, language)
customerFacade.update(principal, customer, merchantStore)
customerFacade.updateAddress(principal, customer, merchantStore)
customerFacade.getCustomerByUserName(principal, merchantStore)
```
**Data Dependencies:** Reads `CUSTOMER.CUSTOMER_NICK`, `CUSTOMER.CUSTOMER_ID`; writes customer profile/address rows or deletes a customer.  
**Side Effects:** Customer deletion cascades customer attributes.
**Concrete Example:**
- API Input: `PATCH /api/v1/customers/me {"firstName":"Rina","lastName":"Lopez"}`
- Success Output: `200 {"id":"c-1006","firstName":"Rina","lastName":"Lopez"}`
- Error Input: token for `rina@example.com` is used with a body targeting `customerId:"c-1007"`
- Error Output: `403 {"error":"CUSTOMER_SCOPE_VIOLATION","message":"Authenticated customer cannot modify another profile","statusCode":403}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-008: Administrative customer deletion requires an allowed group
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/CustomerApi.java:91-109`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** Only super administrators, administrators, or retail administrators may delete a customer through the administrative API.
**Intent:** Authorization
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
authenticatedUser = userFacade.authenticatedUser()
REQUIRE authenticatedUser != null
userFacade.authorizedGroup(authenticatedUser, [SUPERADMIN, ADMIN, ADMIN_RETAIL])
customerFacade.deleteById(id)
```
**Data Dependencies:** Reads `USERS.ADMIN_NAME`, `USER_GROUP`, `SM_GROUP.GROUP_NAME`; deletes `CUSTOMER` and dependent `CUSTOMER_ATTRIBUTE`.  
**Side Effects:** Customer data and attributes are deleted.
**Concrete Example:**
- API Input: `DELETE /api/v1/customers/c-1008` as `admin@example.com`
- Success Output: `204`
- Error Input: same request as a customer-role principal
- Error Output: `403 {"error":"ADMIN_ROLE_REQUIRED","message":"Administrative group is required","statusCode":403}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 3 | 3 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-009: Customer listings are paginated and store-filtered
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/CustomerApi.java:116-130; initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/customer/CustomerRepositoryImpl.java:25-133`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** Administrative customer listings are limited to the selected store and return a bounded page with a total count.
**Intent:** Validation
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
criteria.startIndex = page when provided
criteria.maxCount = count when provided
repository count predicate: c.merchantStore.id = :mId
object query applies firstResult and maxResults
```
**Data Dependencies:** Reads `CUSTOMER.MERCHANT_ID`, `CUSTOMER.CUSTOMER_EMAIL_ADDRESS`, billing name/country and attributes.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `GET /api/v1/customers?page=2&pageSize=20&email=lee` with store `default`
- Success Output: `200 {"items":[{"id":"c-1010","emailAddress":"lee@example.com"}],"pagination":{"page":2,"pageSize":20,"totalItems":41,"totalPages":3}}`
- Error Input: `pageSize=0`
- Error Output: `400 {"error":"INVALID_PAGE_SIZE","message":"pageSize must be greater than zero","statusCode":400}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 7 | GAP |
| Data-flow | 12 | 12 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-010: Customer search predicates must remain conjunctively store-scoped
**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/customer/CustomerRepositoryImpl.java:46-72`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** Name, email, and country filters must never return a customer from outside the requested store; combined filters apply to the same customer.
**Intent:** Compliance
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
legacy name predicate appends "and firstName like :nm or lastName like :nm" without parentheses
legacy first-name predicate contains "c..billing.firstName"
target query must group OR terms under store predicate and use billing.firstName
```
**Data Dependencies:** Reads `CUSTOMER.MERCHANT_ID`, `BILLING_FIRST_NAME`, `BILLING_LAST_NAME`, `CUSTOMER_EMAIL_ADDRESS`, `BILLING_COUNTRY_ID`.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `GET /api/v1/customers?firstName=Lee&countryCode=US` in store `north`
- Success Output: `200 {"items":[{"id":"c-1011","firstName":"Lee","countryCode":"US"}],"pagination":{"totalItems":1}}`
- Error Input: a last-name match exists in store `south` but not `north`
- Error Output: `200 {"items":[],"pagination":{"totalItems":0}}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-011: Address country and zone codes resolve to reference data
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/customer/CustomerPopulator.java:106-151,155-188; initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java:496-551`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer address` (243978)

**Statement:** A billing or delivery address may reference only a supported country and, when supplied, a supported zone; an unknown code is rejected.
**Intent:** Validation
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
country = countries.get(address.country)
IF country == null reject "Unsuported country code"
IF address.zone is not blank
  zone = zoneService.getByCode(address.zone)
  IF zone == null reject "Unsuported zone code"
```
**Data Dependencies:** Reads `COUNTRY.ISO_CODE`, `ZONE.CODE`; writes embedded billing/delivery country and zone identifiers.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `PATCH /api/v1/customers/me/address {"billing":{"address":"1 Pine St","city":"Austin","postalCode":"78701","countryCode":"US","zoneCode":"TX"}}`
- Success Output: `204`
- Error Input: identical request with `zoneCode:"XX"`
- Error Output: `422 {"error":"UNSUPPORTED_ZONE","message":"Zone code XX is not supported","statusCode":422}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 2 | 2 | OK |

### BR-CUS-012: Billing address fields are required for a complete billing update
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java:1034-1046`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer address` (243978)

**Statement:** A billing address change requires street address, city, postal code, and country.
**Intent:** Validation
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
IF billing != null
  REQUIRE billing.address
  REQUIRE billing.city
  REQUIRE billing.postalCode
  REQUIRE billing.country
```
**Data Dependencies:** Reads request fields; writes `CUSTOMER.BILLING_STREET_ADDRESS`, `BILLING_CITY`, `BILLING_POSTCODE`, `BILLING_COUNTRY_ID`.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `PATCH /api/v1/customers/me/address {"billing":{"address":"44 Oak Ave","city":"Denver","postalCode":"80202","countryCode":"US"}}`
- Success Output: `204`
- Error Input: billing object omits `postalCode`
- Error Output: `422 {"error":"BILLING_POSTAL_CODE_REQUIRED","message":"Billing postal code is required","statusCode":422}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-013: Delivery address may inherit billing values when omitted
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/customer/facade/CustomerFacadeImpl.java:1047-1062`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer address` (243978)

**Statement:** When no delivery address is supplied, the service creates one from billing; partial delivery input is completed from billing values.
**Intent:** Calculation
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
IF delivery == null delivery = billing
ELSE IF delivery.address blank delivery.address = billing.address
ELSE IF delivery.city blank legacy code assigns delivery.address = billing.city
ELSE IF delivery.postalCode blank legacy code assigns delivery.address = billing.postalCode
ELSE IF delivery.countryCode blank legacy code assigns delivery.address = delivery.countryCode
```
**Data Dependencies:** Reads billing and delivery fields; writes `CUSTOMER.DELIVERY_*`.  
**Side Effects:** Address update persists both embedded address values.
**Concrete Example:**
- API Input: `PATCH /api/v1/customers/me/address {"billing":{"address":"5 King Rd","city":"Dublin","postalCode":"D02","countryCode":"IE"}}`
- Success Output: `204 {"delivery":{"address":"5 King Rd","city":"Dublin","postalCode":"D02","countryCode":"IE"}}`
- Error Input: delivery has `city:"Cork"` but omits country
- Error Output: `422 {"error":"DELIVERY_COUNTRY_REQUIRED","message":"Delivery country could not be derived","statusCode":422}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 4 | 3 | GAP |
| Data writes | 4 | 4 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-014: Delivery state and postal fields must remain distinct
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/customer/CustomerPopulator.java:155-166`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer address` (243978)

**Statement:** Delivery state/province and postal code are separate address attributes and must be persisted to their corresponding fields.
**Intent:** Compliance
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
delivery.postalCode = sourceShipping.postalCode
delivery.state = sourceShipping.stateProvince
```
**Data Dependencies:** Reads `DELIVERY_POSTCODE`, request `postalCode`, request `stateProvince`; writes `DELIVERY_POSTCODE`, `DELIVERY_STATE`.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `PATCH /api/v1/customers/me/address {"delivery":{"postalCode":"10001","stateProvince":"NY","countryCode":"US"}}`
- Success Output: `204`
- Error Input: response maps `"postalCode":"NY"` and loses `"stateProvince":"NY"` distinction
- Error Output: `500 {"error":"ADDRESS_MAPPING_ERROR","message":"Postal code and state must be preserved separately","statusCode":500}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 1 | 1 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-015: Customer attributes require same-store option definitions
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/customer/CustomerPopulator.java:207-236`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer registration` (244070)

**Statement:** A customer attribute can be saved only when both its option and option value exist and belong to the selected store.
**Intent:** Validation
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
option = customerOptionService.getById(attr.customerOption.id)
value = customerOptionValueService.getById(attr.customerOptionValue.id)
REQUIRE option != null AND value != null
REQUIRE option.merchantStore.id == store.id
REQUIRE value.merchantStore.id == store.id
```
**Data Dependencies:** Reads `CUSTOMER_OPTION.CUSTOMER_OPTION_ID`, `CUSTOMER_OPTION.MERCHANT_ID`, `CUSTOMER_OPTION_VALUE.CUSTOMER_OPTION_VALUE_ID`, `CUSTOMER_OPTION_VALUE.MERCHANT_ID`; writes `CUSTOMER_ATTRIBUTE`.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `POST /api/v1/customer-auth/registrations {"emailAddress":"eli@example.com","password":"Eli!2026","billing":{"firstName":"Eli","lastName":"Brown","countryCode":"US"},"attributes":[{"optionId":"opt-1","optionValueId":"val-2","textValue":"Large"}]}`
- Success Output: `201 {"customerId":"c-1012","attributes":[{"optionId":"opt-1","valueId":"val-2"}]}`
- Error Input: option `opt-1` belongs to another store
- Error Output: `422 {"error":"ATTRIBUTE_SCOPE_VIOLATION","message":"Customer option is not valid for this store","statusCode":422}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 4 | 4 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 3 | 3 | OK |

### BR-CUS-016: Customer defaults include gender and language
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/customer/CustomerPopulator.java:91-104,241-258`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer registration` (244070)

**Statement:** A customer receives a default gender value when absent and a default language from the request or store context when absent.
**Intent:** Calculation
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
IF source.gender != null AND target.gender == null target.gender = CustomerGender.valueOf(source.gender)
IF target.gender == null target.gender = M
IF target.defaultLanguage == null
  lang = source.language == null ? requestLanguage : languageService.getByCode(source.language)
  target.defaultLanguage = lang
```
**Data Dependencies:** Reads `LANGUAGE.CODE`, request gender/language; writes `CUSTOMER.CUSTOMER_GENDER`, `CUSTOMER.LANGUAGE_ID`.  
**Side Effects:** None.
**Concrete Example:**
- API Input: `POST /api/v1/customer-auth/registrations {"emailAddress":"mia@example.com","password":"Mia!2026","billing":{"firstName":"Mia","lastName":"Stone","countryCode":"US"},"language":"fr"}`
- Success Output: `201 {"customerId":"c-1013","gender":"M","language":"fr"}`
- Error Input: `language:"xx"` is not configured
- Error Output: `422 {"error":"UNSUPPORTED_LANGUAGE","message":"Language xx is not supported","statusCode":422}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | N/A |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

### BR-CUS-017: Customer deletion removes customer attributes
**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/customer/CustomerServiceImpl.java:91-111`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** Deleting a customer also removes all customer-specific attribute assignments.
**Intent:** State Transition
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
customer = getById(customer.id)
attributes = customerAttributeService.getByCustomer(customer.merchantStore, customer)
FOR attribute IN attributes customerAttributeService.delete(attribute)
customerRepository.delete(customer)
```
**Data Dependencies:** Reads `CUSTOMER_ATTRIBUTE.CUSTOMER_ID`, `CUSTOMER_ATTRIBUTE.OPTION_ID`; deletes `CUSTOMER_ATTRIBUTE` and `CUSTOMER`.  
**Side Effects:** Cascading delete; no event was observed.
**Concrete Example:**
- API Input: `DELETE /api/v1/customers/c-1014`
- Success Output: `204`
- Error Input: customer has an attribute row that cannot be loaded
- Error Output: `500 {"error":"CUSTOMER_DELETE_FAILED","message":"Customer and dependent attributes were not removed","statusCode":500}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 2 | 2 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-CUS-018: Customer attribute and option removals clean dependent assignments
**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/customer/attribute/CustomerOptionServiceImpl.java:52-78; initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/customer/attribute/CustomerOptionValueServiceImpl.java:58-86`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer profile` (243977)

**Statement:** Removing an attribute option or value first removes customer assignments and option-collection links that depend on it.
**Intent:** State Transition
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
FOR attribute in getByOptionId(option.store, option.id) delete(attribute)
FOR optionSet in optionSetService.listByOption(option, option.store) delete(optionSet)
delete(option)
For a value, use getByCustomerOptionValueId and listByOptionValue before delete(value)
```
**Data Dependencies:** Reads/deletes `CUSTOMER_ATTRIBUTE`, option-set tables, `CUSTOMER_OPTION`, `CUSTOMER_OPTION_VALUE`.  
**Side Effects:** Cascading cleanup.
**Concrete Example:**
- API Input: `DELETE /api/v1/customer-options/opt-5`
- Success Output: `204`
- Error Input: option has an assignment that cannot be deleted
- Error Output: `409 {"error":"OPTION_IN_USE","message":"Option could not be removed while dependent assignments remain","statusCode":409}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 3 | 3 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 1 | 1 | OK |

### BR-CUS-019: Customer login returns a token only after authentication succeeds
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/customer/AuthenticateCustomerApi.java:157-196`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer login` (244071)

**Statement:** A customer receives an access token only after the submitted credentials authenticate successfully; invalid credentials return unauthorized.
**Intent:** Authorization
**Classification:** Core
**Weight:** Critical
**Logic:**
```pseudocode
authentication = jwtCustomerAuthenticationManager.authenticate(username, password)
IF BadCredentialsException return 401 {"message":"Bad credentials"}
IF authentication == null return 500
load JWTUser by username
token = jwtTokenUtil.generateToken(userDetails)
return AuthenticationResponse(user.id, token)
```
**Data Dependencies:** Reads `CUSTOMER.CUSTOMER_NICK`, `CUSTOMER.CUSTOMER_PASSWORD`, group/permission joins; writes no identity row.  
**Side Effects:** Security context is populated.
**Concrete Example:**
- API Input: `POST /api/v1/customer-auth/login {"username":"jules@example.com","password":"N0tTheHash"}`
- Success Output: `200 {"customerId":"c-1015","accessToken":"eyJ..."}`
- Error Input: password `"wrong-password"`
- Error Output: `401 {"error":"BAD_CREDENTIALS","message":"Username or password is incorrect","statusCode":401}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 2 | OK |

### BR-CUS-020: Customer permissions derive from groups
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/AbstractCustomerServices.java:50-92; initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/security/customer/JWTCustomerServicesImpl.java:34-53`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `Customer login` (244071)

**Statement:** Authenticated customer authorities are the authenticated-customer role plus permissions associated with every assigned group.
**Intent:** Authorization
**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
user = customerService.getByNick(userName)
groupsId = user.groups.map(group.id)
authorities.add("ROLE_" + PERMISSION_CUSTOMER_AUTHENTICATED)
FOR permission in permissionService.getPermissions(groupsId)
  authorities.add(permission.permissionName)
return JWTUser(... authorities ...)
```
**Data Dependencies:** Reads `CUSTOMER_GROUP`, `SM_GROUP.GROUP_ID`, `PERMISSION_GROUP`, `PERMISSION.PERMISSION_NAME`; writes none.  
**Side Effects:** Authorities are embedded in the authenticated security object.
**Concrete Example:**
- API Input: `POST /api/v1/customer-auth/login {"username":"vip@example.com","password":"V!pPass2026"}`
- Success Output: `200 {"customerId":"c-1016","accessToken":"...","authorities":["ROLE_CUSTOMER_AUTHENTICATED","customer.read"]}`
- Error Input: group permission lookup fails
- Error Output: `503 {"error":"AUTHORITY_LOOKUP_UNAVAILABLE","message":"Customer permissions could not be loaded","statusCode":503}`
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |
