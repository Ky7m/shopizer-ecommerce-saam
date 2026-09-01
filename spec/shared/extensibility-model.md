# Shared Extensibility Model

## Purpose

This document defines the common resolution mechanism for deployment-configured
behavior. Service specifications reference these extension points rather than
hardcoding provider, metadata, cache, or secret-policy choices.

## Resolution Contract

`extensionResolver.resolve(extensionPoint, context)` returns the configured
value or strategy for the current tenant, store, module family, and operation.
The resolver rejects unknown extension-point identifiers, preserves tenant and
store isolation, and never exposes secret values in public responses. When no
override exists, the documented default for the extension point applies.

## Extension Points

| ID | Name | Mechanism | Resolution | MS-11 rules |
|---|---|---|---|---|
| EXT-CMS-021 | CMS storage provider and object-key strategy | config/provider | Resolve the selected content provider and canonical key strategy; no automatic fallback | BR-EXT-021, BR-EXT-022, BR-EXT-023, BR-EXT-029 |
| EXT-PAY-024 | Runtime payment implementation discovery | metadata/provider | Resolve available runtime payment implementations and project their metadata | BR-EXT-024 |
| EXT-PROVIDER-025 | Provider validation and discovery boundary | provider | Resolve the provider validation/discovery adapter without executing provider-owned operations in MS-11 | BR-EXT-025 |
| EXT-MODULE-CACHE-026 | Module discovery cache policy | config/cache | Resolve cache invalidation or versioning for the affected module family | BR-EXT-026 |
| EXT-CONFIG-027 | Secret classification and redaction | config/policy | Resolve sensitive-field classification and redact or omit secret values from public projections | BR-EXT-027 |
| EXT-MODULE-METADATA-028 | Environment-aware module metadata | metadata | Resolve supported regions and environment metadata for a module definition | BR-EXT-028 |
| EXT-CMS-DELETE-030 | Provider-neutral idempotent deletion | provider/policy | Resolve the scoped deletion adapter and return idempotent success for an absent object | BR-EXT-030 |

## MS-11 Binding Rule

Every MS-11 rule annotated with an `Extension Point:` entry calls
`extensionResolver.resolve(...)` before applying the resolved provider,
metadata, cache, or redaction behavior. A rule may define a default, but it
must not replace the resolver with a hardcoded deployment-specific value.
