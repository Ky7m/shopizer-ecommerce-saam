# Admin User Flows

## Workflow authority

The sequence below is derived from Angular route/component/service behavior:
`app-routing.module.ts`, `pages-routing.module.ts`, feature routing modules, component
handlers, and feature services. No backend `07-workflows.md` exists in this repository and
`spec/07-cross-service-workflows.md` is absent. Therefore every flow marked with a
`GAP-WF-ADMIN-*` ID is a workflow-spec gap, not a completed backend workflow. Before
implementation, the listed provider owners must publish the missing workflow authority,
including state transitions, authorization, retries, and idempotency.

## Flow catalog

| ID | Flow | Legacy sequence | Authority status |
|---|---|---|---|
| GAP-WF-ADMIN-001 | Authenticate and establish context | Login -> token -> profile -> role calculation -> merchant/store context -> Pages/Home | UI-derived; MS-01 workflow missing |
| GAP-WF-ADMIN-002 | Switch tenant/store | select context -> update headers -> reload store-scoped data -> retain safe route | UI target requirement; no backend workflow |
| GAP-WF-ADMIN-003 | Maintain order | list -> detail -> load history/transactions -> edit snapshot/history -> capture/refund/cancel | UI-derived; MS-05 workflow missing |
| GAP-WF-ADMIN-004 | Maintain product/category | list -> create/detail -> validate uniqueness -> save -> media/category/availability/price child flow | UI-derived; MS-02/MS-07 workflow missing |
| GAP-WF-ADMIN-005 | Publish content | list -> add/edit -> rich editor/image picker -> save/delete | UI-derived; MS-11 workflow missing |
| GAP-WF-ADMIN-006 | Configure shipping/payment/tax | select store -> list/configure -> validate -> save -> reload | UI-derived; cross-service workflow missing |
| GAP-WF-ADMIN-007 | Manage users/stores | list -> create/detail -> uniqueness -> save -> enable/delete | UI-derived; MS-01/MS-10 workflow missing |
| GAP-WF-ADMIN-008 | File/image lifecycle | open manager -> folder/list -> upload -> rename/delete/download -> select image | UI-derived; MS-11 workflow missing |

## GAP-WF-ADMIN-001: authentication and context

1. Anonymous navigation resolves to `/auth`; protected `/pages/**` navigation is intercepted.
2. Login validates required email/password locally and calls the BFF login path.
3. On success, the session provider stores the approved token/session and obtains the current
   administrator. It does not trust or reconstruct roles from browser storage.
4. The tenant/store context provider resolves the permitted default context. Subsequent BFF
   calls include `x-tenant-id` and store-scoped calls include `x-store-id`.
5. Navigate to `/pages/home`; show the Store information card.
6. On 401, clear session/context, preserve a safe return path, and navigate to login.

**Must be produced before implementation:** MS-01 admin login/refresh/session workflow,
claim-to-role mapping, context discovery rules, refresh concurrency behavior, and logout
invalidation rules.

## GAP-WF-ADMIN-002: tenant/store switching

The selector is available in the authenticated shell only for users with more than one
permitted context. A change is confirmed if dirty state exists, updates the scoped context,
invalidates all store-scoped data, and reloads the current route. A 403 leaves the prior
context active and shows an authorization message. A missing/invalid context is a blocking
state, never a silent fallback.

**Must be produced before implementation:** context ownership/selection endpoint or session
claim rule, cross-store cache invalidation rule, and BFF anti-confused-deputy behavior.

## GAP-WF-ADMIN-003: order maintenance

List -> select order -> load `Order`, history, next payment action, and transactions ->
edit customer snapshot or append status history -> save -> reload detail. Capture, refund, and
cancel are explicit confirmation commands, each disabled while pending and idempotent per the
provider contract. Invoice is a read/download action. Do not infer allowed transitions from
the button list; use the published provider state model and workflow once supplied.

## GAP-WF-ADMIN-004: product/category maintenance

Products and categories use list -> create/detail -> client validation -> provider save ->
return/reload. Product child routes preserve the sequence images, category association,
availability/inventory, then price. Category hierarchy uses a selected parent and a non-drag
move action. Provider 409 errors retain entered values and identify the conflicting identity.
Options, properties, groups, types and discount controls stop at a contract-gap panel.

## GAP-WF-ADMIN-005: content and files

Content list -> add/edit -> code uniqueness check -> editor/image picker -> provider save.
File/image manager actions are independently pending and update the current folder/list only
after success. Delete and rename are confirmed. Rich text is sanitized at the approved
boundary. The image picker returns a provider-approved asset reference to the calling editor.

## GAP-WF-ADMIN-006: configuration domains

Select store -> load one domain's configuration/modules -> edit exact schema fields -> save ->
show success and reload. Do not combine payment, shipping, and tax writes in one client
transaction. If a lookup is unavailable, keep the field unresolved and expose the lookup
contract gap rather than submitting an invented code.

## Deferred flow handling

Every deferred route remains navigable for traceability. It renders route title, legacy source
route, gap ID, and the provider contract needed; it has no mutation controls and does not
pretend that a 200 empty response means “no data”.
