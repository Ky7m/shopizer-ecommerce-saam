# Admin Frontend Test Plan

Tests are planned only; no test code is created in Phase 4 Stage 2. Playwright runs against the
live .NET Aspire AppHost with the Admin host and a contract-faithful BFF/provider test setup.
Test data must include at least two tenants/stores and administrators with each legacy role
binding once the backend scope matrix is published.

## Authentication and context

| Area | Playwright coverage |
|---|---|
| Login | valid login reaches Home; required/email/password validation; invalid credentials; loading/duplicate-submit prevention |
| Refresh/logout | approved refresh path preserves session; 401 returns to login; logout clears access and safe return route |
| Guards | anonymous direct navigation to every `/pages/**` route is blocked; authorized route renders; 403 renders permission state |
| Tenant/store | selector lists only permitted contexts; switching sends `x-tenant-id`/`x-store-id`; store-scoped data reloads; unauthorized context is rejected and rolled back |

## Domain workflow coverage

Cover each contract-backed major workflow: user list/create/edit/enable/delete; store list/
create/edit/branding/logo; product list/create/edit/media/category/availability/price; category
list/create/edit/hierarchy move; customer list/add/edit/address/delete; order list/detail/
history/snapshot/capture/refund/cancel/invoice; pages and boxes CRUD; files/images/folders
upload/rename/delete/download/gallery selection; shipping configuration/module/origin/
packaging/expedition; payment module configure; tax class/rate CRUD.

For each workflow assert route, visible legacy terminology, exact field casing in request
payloads, context headers, success reload, back navigation, dirty-form protection, and
provider response field binding. Assert that mutations do not call a provider URL directly.

## State and error matrix

Every major screen has Playwright scenarios for:

- loading skeleton/disabled controls;
- valid HTTP 200 empty envelope and explicit empty-state text;
- field validation/HTTP 400 or 422;
- HTTP 401 session expiry;
- HTTP 403 authorization;
- HTTP 404 missing entity;
- HTTP 409 uniqueness or entity-state conflict;
- HTTP 500 and 503 retry;
- contract-gap screen with no network call and no fake empty list.

Use BFF route interception or a contract-faithful test provider only at the BFF boundary.
Assertions must verify that the UI path is `/api/admin/v1/...` and that BFF/provider binding
tests verify the exact provider contract path/method separately.

## Accessibility and responsive coverage

Run Playwright accessibility assertions for keyboard-only navigation, focus order/restoration,
landmarks/headings, labels and error association, dialog focus trap, table headers, status
announcements, 44px targets, color-independent status, reduced motion, and image alt text.
Exercise desktop, tablet, and narrow mobile widths for navigation drawer, forms, tables,
product/category tabs, hierarchy, file manager, order detail, and error states.

## Traceability and release gates

Each routed inventory row must have at least one route/guard test or an explicit deferred-gap
test. Each BFF mapping must have a contract-binding test asserting the provider path/method and
schema reference. Before implementation, resolve `GAP-WF-ADMIN-001` through
`GAP-WF-ADMIN-008` and publish the missing role/scope matrix; tests must not encode guessed
workflow transitions or authorization.
