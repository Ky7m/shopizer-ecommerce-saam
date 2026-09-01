# Dependencies: Tax

**Service ID:** MS-08

## No Dependencies

This service has no outgoing graph `CALLS` edge and publishes or consumes no event in its
approved API design. `POST /tax-calculations` receives customer, product, shipping, and order
snapshots as request data; it does not read another service's tables or call an uncontracted
service. The synchronous caller is MS-04, whose call is recorded in
`spec/microservices/ms-04/05-dependencies.md`.

## Reconciliation note

The MS-08 completion summary lists MS-01, MS-02, MS-04, MS-05, MS-09, and MS-10 as data-context
sources. Those are request-context dependencies, not graph `CALLS` edges. No provider endpoint
has been fabricated for them.

