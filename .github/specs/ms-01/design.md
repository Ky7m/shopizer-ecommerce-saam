# Design: Customer and Identity (MS-01)

## Architecture

- **Language/framework:** C# with ASP.NET Core on .NET 10.
- **Persistence:** Npgsql ADO.NET repository; PostgreSQL is selected from
  `ConnectionStrings__customeridentitydb`, `DATABASE_URL`, or Aspire's named
  connection. The explicit no-connection development fallback is isolated in the
  repository and is never selected when a database is configured.
- **Messaging:** RabbitMQ `domain-events` topic exchange plus a durable PostgreSQL
  event outbox row written with registration.
- **Security:** locally validated HMAC-SHA512 JWT, PBKDF2-SHA256 password hashes,
  tenant/store claims, active-account and password-reset cutoff checks.

## Project structure

```
DTOs/                  # copied verbatim from spec/microservices/ms-01/08-dtos
Models/Domain.cs       # domain records, request context and contract mappers
Data/IdentityRepository.cs
Data/SchemaInitializer.cs
Services/IdentityServices.cs
Services/EventPublisher.cs
Middleware/HttpPipeline.cs
Controllers/AdministratorController.cs
```

## Persistence

`SchemaInitializer` creates the 14 specified tables, five PostgreSQL enums,
constraints and indexes in `customer_identity`; it also creates the event and
email outbox boundaries needed by the dependencies specification. Repository
queries apply both tenant and store predicates for all tenant-owned aggregates.
Address, attribute, review and reset operations use explicit state transitions.

## API mapping

Controllers map every operation in `04-api-contract.yaml` and use copied DTOs for
all request/response contract types. `ApiController` validation returns 422;
`ErrorMiddleware` produces JSON rule codes for domain errors, malformed route
identifiers and unexpected failures. The literal legacy newsletter update remains
an explicit 501 and unsubscribe performs a real update.

## Business services

| Component | Rules |
|---|---|
| `IdentityService` registration/profile/address | BR-CUS-001..020, BR-UI-001 |
| `TokenService` and reset operations | BR-CUS-NN-001..009, BR-CUS-NN-017..018 |
| Administrator operations | BR-CUS-NN-010..020 |
| Review operations | BR-CUS-021..025, BR-UI-002 |
| Newsletter/external operations | BR-CUS-NN-021, BR-CUS-026..028 |
| `EventPublisher` | BR-CUS-002 event side effect |

## Known specification boundaries

The dependencies specification supplies no OIDC provider contract and no email
provider contract. The implementation therefore validates locally issued access
tokens and persists reset email work at the outbox boundary without inventing an
external provider protocol. Store hierarchy expansion cannot query MS-10 because
the approved dependency graph has no outgoing service call; requests remain scoped
to the supplied opaque store reference.
