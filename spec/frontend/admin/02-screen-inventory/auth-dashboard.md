# Screen Inventory: Authentication, Home, Gallery, Errors

## Authentication

| Screen | Data on load | URL state | Actions | State visuals |
|---|---|---|---|---|
| Login `/auth` | none | remember-username preference is local UI state | submit `username`/`password`, show/hide password, remember username, forgot, register | initial form, inline required/email errors, pending submit, invalid credentials, 401/session error |
| Register `/auth/register` | store uniqueness check as fields change | form state only | enter store signup fields, submit | field validation, uniqueness conflict, success handoff |
| Forgot `/auth/forgot-password` | none | email/return URL | request reset | sent confirmation, unknown-user/422 error |
| Reset `/user/{id}/reset/{token}` | token validation | route token | enter/confirm password, submit | token invalid/expired, mismatch, success |

Legacy evidence: `pages/auth/auth-routing.module.ts`, login/forgot/register/reset components,
`auth/services/auth.service.ts`, `token.service.ts`, and `shared/interceptors/*.ts`. Keep auth
outside the admin shell and preserve the logo-led card structure.

## Home

Preserve the “Store information” card with merchant/store name, address, city/state/postal
code, country, phone, current user name, and last access. Load profile and current store in
parallel; bind exact provider fields only. The disabled “Delete cache” control remains visible
as a deferred system-management capability until a contract exists. Dashboard chart/metrics
regions are a visible contract-gap panel, not mock data.

## Gallery and errors

`/gallery` is a modal-like image picker used by content editors. Load image manager entries,
render provider-returned image values, support keyboard selection and cancel, and return to the
calling editor. `/errorPage`, `/pages/error-500`, and not-found states preserve the Shopizer
logo and “Take me home” terminology while adding a retry/support correlation action.
