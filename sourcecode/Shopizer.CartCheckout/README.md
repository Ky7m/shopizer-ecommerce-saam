# Shopizer Cart and Checkout (MS-04)

This service owns tenant/store-scoped shopping carts, checkout snapshots, idempotency
records, and the transactional `OrderSubmitted.v1` outbox. It does not write order,
payment, inventory, pricing, tax, or shipping state.

Run from the repository root with `dotnet run --project sourcecode/Shopizer.CartCheckout`.
Under Aspire, the service uses its configured PostgreSQL resource and RabbitMQ
resource `rabbitmq`, while isolating its data in the `cart_checkout_schema` schema.
It uses service-discovered references to MS-01, MS-02, MS-06,
MS-07, MS-08, and MS-09.

Every request requires `x-tenant-id`, `x-store-id`, and `x-correlation-id`; authenticated
operations also require the MS-01 bearer token. Checkout and payment initialization
require `idempotency-key`. The HTTP contract is
`spec/microservices/ms-04/04-api-contract.yaml`, rooted at `/api/v1`.

The Docker build context is `sourcecode/`; the included multi-stage Dockerfile exposes
port 8080.
