# Frontend/UI - Extraction Summary

## Segment Profile

- Applications analyzed: `initial-source/shopizer-admin-main` (Angular) and `initial-source/shopizer-shop-reactjs-main` (React).
- Inventory: Angular 135 component TypeScript files, 131 HTML templates, 137 SCSS files; React 92 JavaScript/JSX files and 32 SCSS files.
- Direct database tables accessed: none; both applications call backend APIs.
- Business-significant UI interaction candidates: 15.
- Confidence: high for routes, forms, API calls, and client workflows; medium for backend business meaning.
- Discovery method: direct source read.

## Call Graph

```text
Angular main.ts -> AppModule -> AppComponent -> AppRoutingModule
  -> AuthModule/PagesModule -> feature route -> screen component -> feature service
  -> CrudService -> Angular HttpClient

LoginComponent.onSubmit -> AuthService.login -> CrudService.post('/v1/private/login')
  -> TokenService/UserService persistence -> profile/access check -> Router.navigate(['pages'])

React index.js -> Redux Provider/rootReducer -> App -> BrowserRouter
  -> lazy route component -> page hook or Redux thunk -> WebService -> Axios -> REST API
```

Evidence: `initial-source/shopizer-admin-main/src/app/app-routing.module.ts:7-18`,
`initial-source/shopizer-admin-main/src/app/pages/pages-routing.module.ts:12-70`,
`initial-source/shopizer-admin-main/src/app/pages/auth/login/login.component.ts:58-91`,
`initial-source/shopizer-admin-main/src/app/pages/auth/services/auth.service.ts:22-66`,
`initial-source/shopizer-admin-main/src/app/pages/shared/services/crud.service.ts:18-63`,
`initial-source/shopizer-shop-reactjs-main/src/index.js:17-28`,
`initial-source/shopizer-shop-reactjs-main/src/App.js:52-179`,
`initial-source/shopizer-shop-reactjs-main/src/util/webService.js:4-39`.

## Business Rules

These are UI-derived interaction candidates. Backend validation and persistence semantics require confirmation from the Java services.

### BR-UI-001: Administration requires an authenticated token

`/pages` is protected by `AuthGuard`; unauthenticated users are redirected to `auth`. Login persists token, user ID, roles, and merchant context before navigation.

Source: `initial-source/shopizer-admin-main/src/app/app-routing.module.ts:12-15`,
`initial-source/shopizer-admin-main/src/app/pages/shared/guards/auth.guard.ts:17-25`,
`initial-source/shopizer-admin-main/src/app/pages/auth/login/login.component.ts:58-91`.

### BR-UI-002: Administration navigation is role-filtered

`PagesComponent.checkAccess` hides menu entries when none of their role guards pass.

Source: `initial-source/shopizer-admin-main/src/app/pages/pages-menu.ts:7-94`,
`initial-source/shopizer-admin-main/src/app/pages/pages.component.ts:30-51`,
`initial-source/shopizer-admin-main/src/app/pages/shared/services/user.service.ts:50-85`.

### BR-UI-003: Product SKU must be unique and alphanumeric

The product form validates SKU format and invokes a uniqueness API when the field changes; invalid or non-unique products are not saved.

Source: `initial-source/shopizer-admin-main/src/app/pages/catalogue/products/product-form/product-form.component.ts:145-178`,
`product-form.component.ts:342-351`, `product-form.component.ts:395-450`,
`initial-source/shopizer-admin-main/src/app/pages/catalogue/products/services/product.service.ts:72-76`.

### BR-UI-004: Product visibility and purchasability control storefront availability

The administration form exposes `visible`, `display`, `canBePurchased`, and quantity; the storefront enables add-to-cart only when availability, purchasability, visibility, and quantity conditions pass.

Source: `initial-source/shopizer-admin-main/src/app/pages/catalogue/products/product-form/product-form.component.ts:145-178`,
`initial-source/shopizer-shop-reactjs-main/src/components/product/ProductDescriptionInfo.js:276-297`.

### BR-UI-005: Product descriptions are localized with first-non-empty fallback

The form creates one description per supported language and fills empty language values from the first non-empty description during save.

Source: `initial-source/shopizer-admin-main/src/app/pages/catalogue/products/product-form/product-form.component.ts:181-199`,
`product-form.component.ts:395-450`.

### BR-UI-006: Category code, URL, parent, and hierarchy are managed together

Category creation validates parent, code, sort order, localized name, and friendly URL; code uniqueness and hierarchy movement use category services.

Source: `initial-source/shopizer-admin-main/src/app/pages/catalogue/categories/category-form/category-form.component.ts:150-181`,
`category-form.component.ts:248-380`,
`initial-source/shopizer-admin-main/src/app/pages/catalogue/categories/services/category.service.ts:20-67`.

### BR-UI-007: Store creation carries merchant configuration

Store forms capture identity, address, languages, default language, currency, measurements, retailer status, and parent retailer; store-code uniqueness is checked before creation.

Source: `initial-source/shopizer-admin-main/src/app/pages/store-management/store-form/store-form.component.ts:200-229`,
`store-form.component.ts:364-430`,
`initial-source/shopizer-admin-main/src/app/pages/store-management/services/store.service.ts:20-54`.

### BR-UI-008: Shipping rules are configurable query/action definitions

The rule editor loads criteria and actions, serializes values, dates, enabled state, store, actions, and rule sets, then creates or updates rules.

Source: `initial-source/shopizer-admin-main/src/app/pages/shipping/rules/rules.component.ts:74-207`,
`initial-source/shopizer-admin-main/src/app/pages/shipping/services/shared.service.ts:66-90`.

### BR-UI-009: Administration order details support lifecycle and payment operations

The order screen exposes status history, customer/address edits, transaction details, refund, and capture. Observed statuses include `ORDERED`, `PROCESSED`, `DELIVERED`, `REFUNDED`, and `CANCELED`.

Source: `initial-source/shopizer-admin-main/src/app/pages/orders/order-details/order-details.component.ts:27-59`,
`order-details.component.ts:66-238`, `order-details.component.ts:266-294`,
`initial-source/shopizer-admin-main/src/app/pages/orders/services/orders.service.ts:18-57`.

### BR-UI-010: Storefront cart identity is merchant-specific

The cart cookie name includes the merchant code; cart ID is persisted in a cookie and Redux-local-storage state and reloaded through anonymous or authenticated endpoints.

Source: `initial-source/shopizer-shop-reactjs-main/src/App.js:52-59`,
`initial-source/shopizer-shop-reactjs-main/src/redux/actions/cartActions.js:67-110`,
`initial-source/shopizer-shop-reactjs-main/src/redux/reducers/cartReducer.js:23-37`.

### BR-UI-011: Product option selections can change price

Option-value IDs are sent to the product-price endpoint whenever radio, select, or checkbox options change; displayed original and discounted prices are updated.

Source: `initial-source/shopizer-shop-reactjs-main/src/components/product/ProductDescriptionInfo.js:45-107`,
`ProductDescriptionInfo.js:174-236`, `ProductDescriptionInfo.js:286-300`.

### BR-UI-012: Add-to-cart creates or updates a backend cart

An empty cart uses `POST /cart`; an existing cart uses `PUT /cart/{cartId}` with SKU, quantity, and selected option IDs.

Source: `initial-source/shopizer-shop-reactjs-main/src/redux/actions/cartActions.js:16-56`,
`initial-source/shopizer-shop-reactjs-main/src/components/product/ProductDescriptionInfo.js:286-305`.

### BR-UI-013: Coupon codes are syntactically restricted and applied server-side

Coupon input accepts an alphanumeric/space/underscore/hyphen pattern, then invokes the cart promotion endpoint and replaces displayed cart state with the response.

Source: `initial-source/shopizer-shop-reactjs-main/src/pages/other/Cart.js:18-35`,
`Cart.js:190-207`, `initial-source/shopizer-shop-reactjs-main/src/util/constant.js:38-43`.

### BR-UI-014: Checkout obtains shipping options and recalculates totals

Checkout submits postal/country data for shipping quotes, selects a quote, and retrieves totals using the selected quote.

Source: `initial-source/shopizer-shop-reactjs-main/src/pages/other/Checkout.js:280-358`,
`Checkout.js:461-506`, `Checkout.js:724-790`.

### BR-UI-015: Checkout tokenizes Stripe payment and submits capture data

Checkout creates a Stripe token and submits currency, shipping quote, payment module, token, and amount for capture.

Source: `initial-source/shopizer-shop-reactjs-main/src/pages/other/Checkout.js:198-225`,
`Checkout.js:508-625`.

Potential defect for human confirmation: `onSubmitOrder` passes `result.token.id` to `onPayment`, while `onPayment` later reads `result.token` (`Checkout.js:508-556`).

## Data Access Patterns

Frontend database access: none. API resource families include authentication/users, products/inventory, categories, brands, options, stores/content, orders, shipping, payment modules, tax, storefront catalogue, cart/promotions, checkout, customers, CMS/contact/newsletter.

Angular uses `environment.apiUrl` and `environment.shippingApi` through `CrudService` (`initial-source/shopizer-admin-main/src/environments/environment.ts:12-24`; `src/app/pages/shared/services/crud.service.ts:12-63`). React uses Axios with a runtime base URL and bearer token (`initial-source/shopizer-shop-reactjs-main/src/util/webService.js:4-39`).

## Entity Lifecycles and Invariants (Layer A flags)

| Entity | Lifecycle / states | Candidate invariant |
|---|---|---|
| Product | New, visible/hidden, purchasable/not purchasable, available/out of stock | SKU uniqueness; availability and quantity coherence |
| Category | Created, visible/hidden, nested | Code/URL identity; parent exists |
| Order | ORDERED, PROCESSED, DELIVERED, REFUNDED, CANCELED | Capture/refund/status operation valid for payment state |
| Cart | Anonymous, authenticated, empty/cleared, checked-out | Cart ID remains associated with merchant/customer |
| Shipping rule | Created, enabled/disabled, date-bounded | Criteria/actions/store serialize consistently |
| Customer | Registered, authenticated, updated, deleted | Password confirmation and valid address shape |
| Store | Created, configured, branded, updated/deleted | Store-code uniqueness and language consistency |

## Extensibility Signals (Layer B flags)

| Component | Mechanism | What varies |
|---|---|---|
| Product descriptions | Language-specific description array | Names, URLs, metadata, descriptions |
| Product options | Backend-defined radio/select/checkbox types | Variant selection and option pricing |
| Shipping rules | Remote criteria/actions and query builder | Eligibility and action behavior |
| Store configuration | Languages, currency, measurements, retailer flags | Store operation and presentation |
| CMS content | API-provided localized pages/boxes/messages | Customer-visible content |

## Placement Candidates (Layer C flags)

No database placement decision is supported by frontend evidence alone. Candidates for deeper review are checkout (multi-step total/payment consistency), cart updates (repeated recalculation), shipping-rule evaluation, storefront category reads, and administration product listings.

## Frontend/UI Components

### Angular administration

- Angular 11 TypeScript client-side SPA with lazy-loaded feature modules.
- Nebular, Bootstrap 4, PrimeNG, ng2-smart-table, Angular tree/CDK, reactive forms, `@ngx-translate`, Summernote/TinyMCE, ECharts/Chart.js, Leaflet, uploaders, and toastr.
- Routes/screens include authentication, dashboard, users, stores, categories, products, brands, catalogues, product groups, options, product types, content, orders, shipping, payment, tax, and customers.
- Role-filtered navigation is rendered by `PagesComponent` and `pages-menu.ts`.

### React storefront

- React 16.6 JavaScript/JSX client-side SPA with `BrowserRouter`, lazy routes, Redux/Thunk/local-storage persistence, Bootstrap/React Bootstrap, custom SCSS, `react-hook-form`, Stripe Elements, and Axios.
- Routes/screens include home, category, product detail, content, search, login/register, password reset, cart, checkout, order confirmation/history/details, account, and contact.
- Shared assets include Angular translations (`en`, `es`, `fr`, `ru`), React translations (`english`, `french`), administration images/logo/theme assets, storefront SCSS/fonts, shared headers, menus, product wrappers, and cart/account components.

Evidence: `initial-source/shopizer-admin-main/src/app/pages/pages-routing.module.ts:12-70`,
`initial-source/shopizer-admin-main/src/app/catalogue/products/routing/products-routing.module.ts:36-79`,
`initial-source/shopizer-admin-main/package.json:21-95`,
`initial-source/shopizer-shop-reactjs-main/package.json:5-52`,
`initial-source/shopizer-shop-reactjs-main/src/redux/reducers/rootReducer.js:11-20`.

## Items Requiring Human Clarification

- Confirm the React checkout payment-token mismatch.
- Confirm direct navigation behavior for category/product route parameters.
- Confirm anonymous-to-authenticated cart merge semantics.
- Confirm quantity-limit enforcement and reducer behavior.
- Confirm localized-field fallback behavior and the meaning of `visible`, `display`, `available`, and `canBePurchased`.
- Confirm account-deletion and cart-cleanup expectations.
- Review frontend handling of failed API calls and CMS/product `dangerouslySetInnerHTML`.
- Confirm Angular invalid-login loading behavior.
- Confirm whether development API URLs are deployment defaults or placeholders.
