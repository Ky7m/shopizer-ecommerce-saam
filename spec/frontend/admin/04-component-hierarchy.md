# Admin Component Hierarchy

## Host/client tree

```text
Shopizer.Admin host
  App.razor
    Routes.razor
      Router / FocusOnNavigate / NotFound
      MainLayout
        AdminSessionProvider
        TenantStoreContextProvider
        AdminShell
          SkipLink + Header
            BrandMark
            TenantStoreSelector
            UserMenu
          NavigationDrawer
            NavigationTree
          MainContent
            RouteView
              DomainLayout
                Screen components
  Error / ReconnectModal / status-code pages

Shopizer.Admin.Client (Interactive Auto)
  BffHttpClient
  SessionState
  TenantStoreState
  RouteQueryState
  Reusable UI components
  Domain screen components and view models
```

The host remains responsible for app composition, BFF edge configuration, server-side
session/token handling, antiforgery, and error boundaries. The client assembly owns interactive
screen behavior, local validation, view state, and accessible component rendering. The
implementation must extend the established `Program.cs` composition rather than introduce a
second hosting model.

## Providers and state ownership

| State | Owner | Lifetime | Rules |
|---|---|---|---|
| Session/authentication | `AdminSessionProvider` | scoped/session | Holds authenticated state and safe return route; never exposes raw token to arbitrary components. |
| Tenant/store | `TenantStoreContextProvider` | scoped plus persisted approved selection | Validates permitted contexts, emits context change, invalidates scoped caches. |
| Navigation visibility | `NavigationPolicy` | derived | Uses server-provided claims/scopes and legacy role labels; does not authorize calls. |
| Route/query | `RouteQueryState` | URL | Search, page, selected ID, tab/folder, and return route remain bookmarkable. |
| Feature list/detail | domain coordinator | route scope | Owns loading/empty/error/mutation states and exact DTO models. |
| Form draft | form component/edit coordinator | component scope | Owns dirty state, validation, submit cancellation, and confirmation on navigation. |
| Toast/notifications | `FeedbackHost` | app scope | Announces success/error without being the only error channel. |

## Reusable components

- `AdminShell`, `ResponsiveNavigation`, `Breadcrumbs`, `PageHeader`, `RightSideMenu`.
- `DataTable`, `DataTableToolbar`, `Pagination`, `EmptyState`, `LoadingTable`,
  `ErrorState`, `ContractGapState`.
- `Field`, `ValidationSummary`, `SearchField`, `SelectField`, `DateField`, `MoneyField`,
  `ConfirmDialog`, `DirtyNavigationGuard`.
- `EntityListPage`, `EntityDetailPage`, `EntityForm`, `StatusBadge`, `ActionMenu`.
- `RichContentEditor` with an explicit sanitized HTML boundary, `ImagePickerDialog`,
  `FileManager`, `UploadDropZone`, `FolderTree`.
- `ProductTabs`, `CategoryTree`, `OrderLineTable`, `HistoryTimeline`,
  `PaymentModuleForm`, `ShippingPackageForm`, `TaxRateForm`.

Reusable components accept typed provider DTOs or small view models whose fields are traceable
to exact contract fields. They do not embed service URLs or domain-specific guesses.

## Layout and routing boundaries

Route components are thin coordinators. Domain layouts preserve the legacy parent/child
navigation and route parameter names. Detail tabs are child routes so refresh/back behavior
matches the Angular application. Deferred child routes use `ContractGapState`.

## BFF and JavaScript interop

`BffHttpClient` is the only browser data boundary. It accepts a relative frontend BFF path,
serializes exact request DTOs, adds approved auth/context headers through the session/context
pipeline, and returns typed status/error results. It must reject provider URL strings.

JavaScript interop is permitted only for behavior that cannot be implemented in Blazor:
focus restoration for dialogs, file picker/drop integration, clipboard/download handoff, and
responsive navigation focus trapping. Interop modules must be small, keyboard-safe, and have
an accessible Blazor fallback. No interop is used for authorization, API calls, or state.
