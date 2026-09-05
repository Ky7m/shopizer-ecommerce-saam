# BFF Contract: Identity, Users, and Store Context

All paths below are browser-facing BFF paths. The provider path is included for binding only.

| Frontend BFF method/path | Provider | Exact provider method/path | Request -> response schemas | Screens |
|---|---|---|---|---|
| POST `/api/admin/v1/auth/login` | MS-01 | POST `/admin-auth/login` | `AuthenticationRequest` -> `AuthenticationResponse` | Login |
| GET `/api/admin/v1/auth/refresh` | MS-01 | GET `/admin-auth/refresh` | bearer/context -> `AuthenticationResponse` | Session renewal |
| POST `/api/admin/v1/auth/password-resets` | MS-01 | POST `/user-password-resets` | `ResetRequest` -> `ResetRequestResponse` | Forgot password |
| GET `/api/admin/v1/auth/password-resets/{storeCode}/{token}` | MS-01 | GET `/user-password-resets/{storeCode}/{token}` | path values -> `ResetTokenValidationResponse` | Reset password |
| POST `/api/admin/v1/auth/password-resets/{storeCode}/{token}` | MS-01 | POST `/user-password-resets/{storeCode}/{token}` | `ResetPasswordRequest` -> `204` (no body) | Reset password |
| POST `/api/admin/v1/users/unique` | MS-01 | POST `/users/unique` | `UniqueUsernameRequest` -> `EntityExistsResponse` | Create/edit validation |
| GET `/api/admin/v1/users/me` | MS-01 | GET `/users/me` | context -> `Administrator` | Profile, dashboard |
| GET `/api/admin/v1/users` | MS-01 | GET `/users` | provider query -> `AdministratorListResponse` | User list |
| POST `/api/admin/v1/users` | MS-01 | POST `/users` | `CreateAdministratorRequest` -> `Administrator` | Create user |
| GET `/api/admin/v1/users/{userId}` | MS-01 | GET `/users/{userId}` | path -> `Administrator` | User details |
| PUT `/api/admin/v1/users/{userId}` | MS-01 | PUT `/users/{userId}` | `UpdateAdministratorRequest` -> `Administrator` | User details |
| DELETE `/api/admin/v1/users/{userId}` | MS-01 | DELETE `/users/{userId}` | path -> `204` | User list/details |
| PATCH `/api/admin/v1/users/{userId}/password` | MS-01 | PATCH `/users/{userId}/password` | `AdministratorPasswordChangeRequest` -> `204` | Change password |
| PATCH `/api/admin/v1/users/{userId}/enabled` | MS-01 | PATCH `/users/{userId}/enabled` | `EnabledRequest` -> `Administrator` | User list/details |
| POST `/api/admin/v1/stores/signup` | MS-10 | POST `/stores/signup` | `CreateStoreRequest` -> `SignupResponse` | Register |
| GET `/api/admin/v1/stores` | MS-10 | GET `/stores` | provider query -> `StoreListResponse` | Stores list |
| POST `/api/admin/v1/stores` | MS-10 | POST `/stores` | `CreateStoreRequest` -> `Store` | Create store |
| GET `/api/admin/v1/stores/{storeCode}` | MS-10 | GET `/stores/{storeCode}` | path -> `Store` | Store detail/home |
| PUT `/api/admin/v1/stores/{storeCode}` | MS-10 | PUT `/stores/{storeCode}` | `UpdateStoreRequest` -> `Store` | Store detail |
| DELETE `/api/admin/v1/stores/{storeCode}` | MS-10 | DELETE `/stores/{storeCode}` | path -> `204` (no body) | Stores list |
| GET `/api/admin/v1/stores/uniqueness` | MS-10 | GET `/stores/uniqueness` | provider query -> `EntityExistsResponse` | Create store validation |
| GET `/api/admin/v1/stores/names` | MS-10 | GET `/stores/names` | provider query -> `StoreNameListResponse` | Store selectors |
| GET `/api/admin/v1/merchants/{merchantCode}/stores` | MS-10 | GET `/merchants/{merchantCode}/stores` | path/query -> `StoreListResponse` | Retailer stores (read only) |
| GET `/api/admin/v1/merchants/{merchantCode}/children` | MS-10 | GET `/merchants/{merchantCode}/children` | path/query -> `StoreListResponse` | Retailer stores (read only) |
| GET `/api/admin/v1/stores/{storeCode}/languages` | MS-10 | GET `/stores/{storeCode}/languages` | path -> `LanguageListResponse` | Store form |
| PUT `/api/admin/v1/stores/{storeCode}/languages` | MS-10 | PUT `/stores/{storeCode}/languages` | `ReplaceLanguagesRequest` -> `Store` | Store form |
| GET `/api/admin/v1/stores/{storeCode}/branding` | MS-10 | GET `/stores/{storeCode}/branding` | path -> `Branding` | Store branding |
| PUT `/api/admin/v1/stores/{storeCode}/branding` | MS-10 | PUT `/stores/{storeCode}/branding` | `BrandingRequest` -> `Branding` | Store branding |
| POST `/api/admin/v1/stores/{storeCode}/branding/logo` | MS-10 | POST `/stores/{storeCode}/branding/logo` | `LogoUploadRequest` -> `Branding` | Store branding |
| DELETE `/api/admin/v1/stores/{storeCode}/branding/logo` | MS-10 | DELETE `/stores/{storeCode}/branding/logo` | path -> `204` (no body) | Store branding |

## UI field bindings and state behavior

`Login` sends `username` and `password` from the legacy email/password form. Persist only the
approved session representation; do not persist provider roles in localStorage. Profile and
dashboard bind `Administrator.userName`, `Administrator.lastAccess`, and the store fields
returned by `Store`; any legacy display field absent from the exact schema stays as a code or
an explicit unavailable value.

Store forms retain `name`, `code`, phone/email, address, supported language selection, default
language, currency, weight and size units, and operating-since layout only where those exact
fields occur in `CreateStoreRequest`, `UpdateStoreRequest`, or `Store`. Store branding binds
the exact `Branding`/`BrandingRequest` fields and upload schema; no social-network or marketing
endpoint is invented.

Lists show skeleton rows while loading, HTTP 200 empty envelopes as “No users”/“No stores”,
422 errors inline, 401 through session recovery, 403 with disabled mutation actions, 409 with
the provider conflict message and a reload action, and 500/503 with retry. Query parameters
for provider pagination are retained in the URL; the BFF must not silently change page size.

## Open decisions / gaps

- The legacy `users/unique` call is POST in the provider contract, so the BFF may expose GET
  only if it translates the documented request without changing semantics; otherwise use POST
  at the frontend too. This must be settled in BFF implementation.
- Retailer create/edit and legacy marketing/landing-page mutations have no complete provider
  operation and are deferred.
- Provider contracts declare bearer security but do not publish the mapping from
  `isAdminRetail`, `isAdminStore`, and other legacy flags to scopes/roles.
