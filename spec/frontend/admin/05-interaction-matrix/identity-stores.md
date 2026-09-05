# Interaction Matrix: Identity, Context, Users, Stores

| Area/action | Trigger | BFF call/navigation | Success/loading/error | Role/entity gating |
|---|---|---|---|---|
| Login submit | valid form submit | POST `/api/admin/v1/auth/login` | pending button; navigate Home; invalid credentials inline | anonymous |
| Register submit | valid store signup form | POST `/api/admin/v1/stores/signup` | pending; 409/422; success handoff | anonymous |
| Forgot password | submit email | POST `/api/admin/v1/auth/password-resets` | sent state; 422 inline | anonymous |
| Reset password | valid token/password form | GET then POST reset path | token pending/invalid; success login | token valid |
| Logout | user menu | clear session, navigate `/auth` | immediate disabled menu | authenticated |
| Store selector | selection change | update context; reload current route | pending selector; 403 rollback prior context | permitted context only |
| User list search/page | input/submit/page | GET `/api/admin/v1/users` | skeleton/empty/paginator/error | `IsAdmin` target binding |
| Create user | click Create | navigate create route | form | `IsAdmin` target binding |
| Save user | valid form | POST/PUT users | pending/save; 409 username; 422 fields | `IsAdmin`; unique username |
| Enable/disable | toggle | PATCH `/api/admin/v1/users/{userId}/enabled` | pending row; conflict/error | admin scope; entity exists |
| Delete user | confirm | DELETE `/api/admin/v1/users/{userId}` | confirm/pending/remove; 409 retain | admin scope; cannot delete protected self if backend says so |
| Profile | navigation/load | GET `/api/admin/v1/users/me` | skeleton/profile/error | authenticated |
| Home/store information | authenticated route load | GET `/api/admin/v1/users/me` plus current-store GET | parallel skeletons; empty/lookup-gap message | authenticated context |
| Delete cache | click disabled legacy button | no call | remains visibly unavailable with contract-gap explanation | authenticated; deferred |
| Server error home action | click Take me home | navigate `/pages/home` | focus heading | public/static error |
| Change password | submit | PATCH `/api/admin/v1/users/{userId}/password` | success and dirty reset | authenticated administrator |
| Store list/search | load/search/page | GET `/stores` | skeleton/empty/paginator | store admin scope |
| Store create | submit | POST `/stores` + uniqueness if approved | pending; conflict; success list | superadmin/adminretail binding |
| Store update/delete | submit/confirm | PUT/DELETE `/stores/{storeCode}` | preserve draft; 409; reload | store scope |
| Branding upload/delete | file/confirm | POST/DELETE branding logo | progress; preview from `Branding`; error | store scope |
| Retailer mutation | any submit | no call; deferred panel | gap state | no contract |
