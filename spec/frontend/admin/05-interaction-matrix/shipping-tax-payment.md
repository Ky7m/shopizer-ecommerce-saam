# Interaction Matrix: Shipping, Tax, Payment

| Area/action | Trigger | BFF call/navigation | Feedback | Gating/entity state |
|---|---|---|---|---|
| Shipping configuration load/save | route/Save | GET/POST configuration/expedition | skeleton/save/retry | store context |
| Shipping module list | route | GET modules | empty/error | store context |
| Configure shipping module | select/save | GET module then POST configure | pending/422/409 | module exists |
| Shipping origin | route/save | GET/POST origin | dependent lookup gap as needed | store context |
| Packaging list | route | GET packages | empty/pagination | store context |
| Package add/edit/delete | submit/confirm | POST/PUT/DELETE package | validation/conflict/confirm | package identity |
| Shipping rules | route/submit | no call | deferred panel | no provider contract |
| Payment module list | route | GET `/payment-modules` | skeleton/empty | store/payment scope |
| Payment configure | select/save | GET/PUT payment module | pending/422/409 | module exists |
| Tax class list/form/delete | route/submit/confirm | GET/POST/PUT/DELETE tax classes | empty/validation/confirm | store/tax scope |
| Tax rate list/form/delete | route/submit/confirm | GET/POST/PUT/DELETE tax rates | empty/validation/confirm | class/jurisdiction exact state |
| Country/zone selectors | change | no call until lookup contract | unresolved lookup state | contract gap |
