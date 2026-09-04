# Shopizer Merchant and Store Administration (MS-10)

ASP.NET Core 10 service for tenant-scoped store identity, hierarchy, language defaults, branding metadata, and signup verification.

- Contract: `spec/microservices/ms-10/04-api-contract.yaml`
- Base path: `/api/v1`
- Persistence: PostgreSQL via Aspire resource `shopizerDb`, raw Npgsql only
- Events: transactional `merchant_store.event_outbox`, RabbitMQ `domain-events` exchange, `StoreCreated`
- Required request header: `x-tenant-id`; `x-store-id` selects a store and defaults to `MerchantAdministration:DefaultStoreCode`; `x-correlation-id` is recommended
- Administrator operations require a bearer token with `administrator` kind and an admin role

Run through the Aspire AppHost from `sourcecode/`. To build the container, use `docker build -f Shopizer.MerchantAdministration/Dockerfile sourcecode/`.
Logo upload/delete requires `MerchantAdministration:FileProviderBaseUrl`; the service returns `503 STORAGE_UNAVAILABLE` when that provider is not configured rather than persisting binary data locally.
