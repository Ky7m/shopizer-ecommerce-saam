
# Payments — Domain Model

**Version:** 1.0  
**Service ID:** MS-06  
**Database schema:** `payments`  
**Database:** PostgreSQL 15+

## Ownership

MS-06 owns the tables below. It does not create foreign keys into MS-04, MS-05, MS-10, or MS-11 schemas. External aggregate IDs are stored as opaque references.

## Core Entities

The following DDL is executable PostgreSQL. Column comments provide either legacy mappings, BR-ID justification, or infrastructure justification.

```sql
CREATE SCHEMA IF NOT EXISTS payments;

CREATE TABLE payments.payment_intent (
    payment_intent_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    checkout_session_id VARCHAR(100) NOT NULL,
    order_id VARCHAR(100),
    provider_code VARCHAR(64) NOT NULL,
    provider_config_version BIGINT NOT NULL,
    amount NUMERIC(19,4) NOT NULL CHECK (amount > 0),
    currency_code CHAR(3) NOT NULL,
    status VARCHAR(32) NOT NULL,
    authorized_amount NUMERIC(19,4) NOT NULL DEFAULT 0 CHECK (authorized_amount >= 0),
    captured_amount NUMERIC(19,4) NOT NULL DEFAULT 0 CHECK (captured_amount >= 0),
    client_secret_reference VARCHAR(255),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by VARCHAR(100),
    correlation_id VARCHAR(100),
    CONSTRAINT payment_intent_currency_ck CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT payment_intent_status_ck CHECK (
        status IN (
            'Created','RequiresAction','Authorized','CapturePending',
            'Captured','PartiallyRefunded','Refunded','Failed','Cancelled',
            'Expired','PendingManualSettlement','ReconciliationRequired'
        )
    )
);
COMMENT ON COLUMN payments.payment_intent.amount IS 'Required by BR-PA-020; immutable upstream checkout amount.';
COMMENT ON COLUMN payments.payment_intent.currency_code IS 'Required by BR-PA-020; immutable ISO-4217 currency.';
COMMENT ON COLUMN payments.payment_intent.provider_config_version IS 'Required by BR-EXT-001; pins configuration at intent creation.';
COMMENT ON COLUMN payments.payment_intent.order_id IS 'External MS-05 order reference; no cross-service FK by ownership rule.';
COMMENT ON COLUMN payments.payment_intent.tenant_id IS 'Multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_intent.created_at IS 'Audit/multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_intent.updated_at IS 'Audit/multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_intent.created_by IS 'Audit/multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_intent.correlation_id IS 'Audit/multi-tenancy standard.';

CREATE TABLE payments.payment_operation (
    payment_operation_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_intent_id UUID NOT NULL REFERENCES payments.payment_intent(payment_intent_id),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    operation_type VARCHAR(24) NOT NULL,
    status VARCHAR(32) NOT NULL,
    requested_amount NUMERIC(19,4) NOT NULL CHECK (requested_amount > 0),
    currency_code CHAR(3) NOT NULL,
    idempotency_key VARCHAR(255) NOT NULL,
    request_fingerprint CHAR(64) NOT NULL,
    provider_attempt_id UUID,
    provider_reference VARCHAR(255),
    failure_code VARCHAR(80),
    failure_message VARCHAR(500),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at TIMESTAMPTZ,
    correlation_id VARCHAR(100),
    CONSTRAINT payment_operation_type_ck CHECK (
        operation_type IN ('Initialize','Authorize','Capture','Refund')
    ),
    CONSTRAINT payment_operation_status_ck CHECK (
        status IN ('Requested','InProgress','Succeeded','Failed','ReconciliationRequired')
    ),
    CONSTRAINT payment_operation_currency_ck CHECK (currency_code ~ '^[A-Z]{3}$')
);
COMMENT ON COLUMN payments.payment_operation.idempotency_key IS 'Required by BR-PA-022.';
COMMENT ON COLUMN payments.payment_operation.request_fingerprint IS 'Required by BR-PA-022.';
COMMENT ON COLUMN payments.payment_operation.requested_amount IS 'Required by BR-PA-020 and BR-ORD-017.';
COMMENT ON COLUMN payments.payment_operation.provider_reference IS 'Provider reference required by BR-ORD-015.';
COMMENT ON COLUMN payments.payment_operation.tenant_id IS 'Multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_operation.created_at IS 'Audit/multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_operation.correlation_id IS 'Audit/multi-tenancy standard.';

CREATE TABLE payments.payment_transaction (
    payment_transaction_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_intent_id UUID NOT NULL REFERENCES payments.payment_intent(payment_intent_id),
    payment_operation_id UUID REFERENCES payments.payment_operation(payment_operation_id),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    operation_type VARCHAR(24) NOT NULL,
    status VARCHAR(32) NOT NULL,
    amount NUMERIC(19,4) NOT NULL CHECK (amount > 0),
    currency_code CHAR(3) NOT NULL,
    provider_code VARCHAR(64) NOT NULL,
    provider_reference VARCHAR(255),
    provider_status VARCHAR(100),
    provider_correlation_id VARCHAR(255),
    provider_details JSONB NOT NULL DEFAULT '{}'::jsonb,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    sequence_no BIGINT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    correlation_id VARCHAR(100),
    CONSTRAINT payment_transaction_type_ck CHECK (
        operation_type IN ('Initialize','Authorize','Capture','Refund')
    ),
    CONSTRAINT payment_transaction_status_ck CHECK (
        status IN ('Succeeded','Failed','Pending','ReconciliationRequired')
    ),
    CONSTRAINT payment_transaction_currency_ck CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT payment_transaction_sequence_uq UNIQUE (payment_intent_id, sequence_no)
);
COMMENT ON COLUMN payments.payment_transaction.provider_reference IS 'Maps to provider identifiers previously serialized in legacy transaction details.';
COMMENT ON COLUMN payments.payment_transaction.provider_details IS 'Allowlisted provider response metadata; replaces unrestricted serialized detail maps.';
COMMENT ON COLUMN payments.payment_transaction.sequence_no IS 'Required by BR-PA-021.';
COMMENT ON COLUMN payments.payment_transaction.tenant_id IS 'Multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_transaction.created_at IS 'Audit/multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_transaction.correlation_id IS 'Audit/multi-tenancy standard.';

CREATE TABLE payments.payment_refund (
    payment_refund_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_intent_id UUID NOT NULL REFERENCES payments.payment_intent(payment_intent_id),
    payment_operation_id UUID NOT NULL REFERENCES payments.payment_operation(payment_operation_id),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    amount NUMERIC(19,4) NOT NULL CHECK (amount > 0),
    currency_code CHAR(3) NOT NULL,
    status VARCHAR(24) NOT NULL,
    provider_reference VARCHAR(255),
    requested_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at TIMESTAMPTZ,
    correlation_id VARCHAR(100),
    CONSTRAINT payment_refund_status_ck CHECK (
        status IN ('Reserved','Succeeded','Failed','Released')
    ),
    CONSTRAINT payment_refund_currency_ck CHECK (currency_code ~ '^[A-Z]{3}$')
);
COMMENT ON COLUMN payments.payment_refund.amount IS 'Required by BR-ORD-017 and BR-EXT-003.';
COMMENT ON COLUMN payments.payment_refund.status IS 'Required by BR-EXT-003 reservation lifecycle.';
COMMENT ON COLUMN payments.payment_refund.tenant_id IS 'Multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_refund.requested_at IS 'Audit/multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_refund.correlation_id IS 'Audit/multi-tenancy standard.';

CREATE TABLE payments.payment_provider_reference (
    payment_provider_reference_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_intent_id UUID NOT NULL REFERENCES payments.payment_intent(payment_intent_id),
    payment_transaction_id UUID REFERENCES payments.payment_transaction(payment_transaction_id),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    provider_code VARCHAR(64) NOT NULL,
    reference_type VARCHAR(32) NOT NULL,
    provider_reference VARCHAR(255) NOT NULL,
    is_current BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    correlation_id VARCHAR(100),
    CONSTRAINT provider_reference_type_ck CHECK (
        reference_type IN ('Intent','Authorization','Capture','Refund','Payer','Correlation')
    )
);
COMMENT ON COLUMN payments.payment_provider_reference.provider_reference IS 'Required by BR-ORD-015 and provider-specific rules.';
COMMENT ON COLUMN payments.payment_provider_reference.tenant_id IS 'Multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_provider_reference.created_at IS 'Audit/multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_provider_reference.correlation_id IS 'Audit/multi-tenancy standard.';

CREATE TABLE payments.payment_callback (
    payment_callback_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID,
    store_id UUID,
    provider_code VARCHAR(64) NOT NULL,
    provider_event_id VARCHAR(255),
    provider_reference VARCHAR(255),
    payment_intent_id UUID REFERENCES payments.payment_intent(payment_intent_id),
    verification_status VARCHAR(24) NOT NULL,
    processing_status VARCHAR(24) NOT NULL,
    payload_hash CHAR(64) NOT NULL,
    protected_payload JSONB,
    received_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    processed_at TIMESTAMPTZ,
    correlation_id VARCHAR(100),
    CONSTRAINT callback_verification_ck CHECK (
        verification_status IN ('Unverified','Verified','Rejected','Duplicate')
    ),
    CONSTRAINT callback_processing_ck CHECK (
        processing_status IN ('Received','Applied','Ignored','Failed')
    )
);
COMMENT ON COLUMN payments.payment_callback.verification_status IS 'Required by BR-PA-023.';
COMMENT ON COLUMN payments.payment_callback.protected_payload IS 'Protected provider payload; retention and redaction controlled by MS-12 security standards.';
COMMENT ON COLUMN payments.payment_callback.received_at IS 'Audit/multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_callback.correlation_id IS 'Audit/multi-tenancy standard.';

CREATE TABLE payments.payment_idempotency (
    payment_idempotency_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    payment_intent_id UUID,
    operation_type VARCHAR(24) NOT NULL,
    idempotency_key VARCHAR(255) NOT NULL,
    request_fingerprint CHAR(64) NOT NULL,
    payment_operation_id UUID REFERENCES payments.payment_operation(payment_operation_id),
    replay_status VARCHAR(24) NOT NULL,
    response_snapshot JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    expires_at TIMESTAMPTZ NOT NULL,
    CONSTRAINT idempotency_operation_ck CHECK (
        operation_type IN ('Initialize','Authorize','Capture','Refund','Callback')
    ),
    CONSTRAINT idempotency_replay_ck CHECK (
        replay_status IN ('InProgress','Completed','Conflicted')
    ),
    CONSTRAINT payment_idempotency_scope_uq UNIQUE (
        tenant_id, store_id, payment_intent_id, operation_type, idempotency_key
    )
);
COMMENT ON COLUMN payments.payment_idempotency.idempotency_key IS 'Required by BR-PA-022.';
COMMENT ON COLUMN payments.payment_idempotency.request_fingerprint IS 'Required by BR-PA-022.';
COMMENT ON COLUMN payments.payment_idempotency.response_snapshot IS 'Required by BR-PA-022 for replaying the original result.';
COMMENT ON COLUMN payments.payment_idempotency.tenant_id IS 'Multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_idempotency.created_at IS 'Audit/multi-tenancy standard.';

CREATE TABLE payments.payment_outbox (
    payment_outbox_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    aggregate_type VARCHAR(40) NOT NULL,
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(80) NOT NULL,
    event_version INTEGER NOT NULL DEFAULT 1,
    payload JSONB NOT NULL,
    publish_status VARCHAR(24) NOT NULL DEFAULT 'Pending',
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    published_at TIMESTAMPTZ,
    correlation_id VARCHAR(100),
    CONSTRAINT payment_outbox_status_ck CHECK (
        publish_status IN ('Pending','Published','Failed')
    )
);
COMMENT ON COLUMN payments.payment_outbox.event_type IS 'Required by BR-EXT-002 and BR-PA-023.';
COMMENT ON COLUMN payments.payment_outbox.tenant_id IS 'Multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_outbox.occurred_at IS 'Audit/multi-tenancy standard.';
COMMENT ON COLUMN payments.payment_outbox.correlation_id IS 'Audit/multi-tenancy standard.';

CREATE INDEX payment_intent_store_status_idx
    ON payments.payment_intent (tenant_id, store_id, status, created_at);

CREATE INDEX payment_operation_intent_type_idx
    ON payments.payment_operation (payment_intent_id, operation_type, created_at);

CREATE INDEX payment_transaction_intent_sequence_idx
    ON payments.payment_transaction (payment_intent_id, sequence_no, occurred_at);

CREATE INDEX payment_refund_intent_status_idx
    ON payments.payment_refund (payment_intent_id, status);

CREATE UNIQUE INDEX payment_callback_provider_event_uq
    ON payments.payment_callback (provider_code, provider_event_id)
    WHERE provider_event_id IS NOT NULL;

CREATE INDEX payment_callback_reference_idx
    ON payments.payment_callback (provider_code, provider_reference);

CREATE INDEX payment_outbox_pending_idx
    ON payments.payment_outbox (publish_status, occurred_at);
```

## Database Logic Objects

The refund invariant is cross-row and therefore requires mandatory database enforcement in addition to application locking.

```sql
CREATE OR REPLACE FUNCTION payments.enforce_refund_balance()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    captured_total NUMERIC(19,4);
    refunded_total NUMERIC(19,4);
    reserved_total NUMERIC(19,4);
BEGIN
    SELECT COALESCE(captured_amount, 0)
      INTO captured_total
      FROM payments.payment_intent
     WHERE payment_intent_id = NEW.payment_intent_id
     FOR UPDATE;

    SELECT COALESCE(SUM(amount), 0)
      INTO refunded_total
      FROM payments.payment_refund
     WHERE payment_intent_id = NEW.payment_intent_id
       AND status = 'Succeeded'
       AND payment_refund_id <> COALESCE(NEW.payment_refund_id, gen_random_uuid());

    SELECT COALESCE(SUM(amount), 0)
      INTO reserved_total
      FROM payments.payment_refund
     WHERE payment_intent_id = NEW.payment_intent_id
       AND status = 'Reserved'
       AND payment_refund_id <> COALESCE(NEW.payment_refund_id, gen_random_uuid());

    IF NEW.status IN ('Reserved', 'Succeeded')
       AND refunded_total + reserved_total + NEW.amount > captured_total THEN
        RAISE EXCEPTION USING
            ERRCODE = '23514',
            MESSAGE = 'Refund amount exceeds captured remaining balance';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER payment_refund_balance_guard
BEFORE INSERT OR UPDATE OF amount, status
ON payments.payment_refund
FOR EACH ROW
EXECUTE FUNCTION payments.enforce_refund_balance();
```

| Name | Kind | Implements | Enforces Invariant | Migration Order | Binding | Placement |
|---|---|---|---|---:|---|---|
| `enforce_refund_balance` | function | BR-EXT-003 | INV-PA-003 | 10 | Repository refund reservation invokes transaction; trigger calls function | mandatory-db-integrity |
| `payment_refund_balance_guard` | trigger |  | INV-PA-003 | 30 | Trigger — no app call | mandatory-db-integrity |

## Entity State Model

### `payment_intent` lifecycle

- **States:** `Created (initial)`, `RequiresAction`, `Authorized`, `CapturePending`, `Captured`, `PartiallyRefunded`, `Refunded (terminal)`, `Failed (terminal)`, `Cancelled (terminal)`, `Expired (terminal)`, `PendingManualSettlement`, `ReconciliationRequired`
- **Transitions:**

| From | To | Trigger | Guard |
|---|---|---|---|
| Created | RequiresAction | BR-EXT-005 / BR-EXT-007 | Provider initialization requires customer action |
| Created | Authorized | BR-EXT-009 | Local manual-payment policy approves immediate recognition |
| Created | Failed | BR-EXT-001 / BR-EXT-004 | Provider configuration or initialization fails |
| Created | Cancelled | BR-PA-022 | Client cancels before authorization |
| RequiresAction | Authorized | BR-EXT-005 / BR-EXT-007 | Verified provider authorization matches amount and currency |
| RequiresAction | Failed | BR-EXT-004 / BR-EXT-007 | Provider rejects or token expires |
| RequiresAction | Expired | BR-PA-023 | Action window expires according to configured provider policy |
| Authorized | CapturePending | BR-ORD-016 | Authorized balance exists and idempotency reservation succeeds |
| Authorized | Captured | BR-EXT-009 | Manual settlement policy records local capture |
| Authorized | Failed | BR-ORD-016 | Provider capture cannot be completed |
| Authorized | Cancelled | BR-ORD-016 | Authorization is cancelled before capture |
| CapturePending | Captured | BR-EXT-002 / BR-EXT-005 | Provider confirms capture |
| CapturePending | Failed | BR-ORD-016 | Provider rejects capture |
| CapturePending | ReconciliationRequired | BR-ORD-015 | Provider result and local commit cannot be reconciled |
| Captured | PartiallyRefunded | BR-EXT-003 | Successful refund is less than remaining captured balance |
| Captured | Refunded | BR-ORD-017 / BR-EXT-003 | Successful refund exhausts captured balance |
| Captured | ReconciliationRequired | BR-PA-023 | Callback contradicts recorded provider state |
| PartiallyRefunded | PartiallyRefunded | BR-EXT-003 | Another successful refund leaves positive balance |
| PartiallyRefunded | Refunded | BR-EXT-003 | Successful refund exhausts remaining balance |
| PartiallyRefunded | ReconciliationRequired | BR-PA-023 | Provider callback contradicts local refund history |
| PendingManualSettlement | Captured | BR-EXT-009 | Approved manual-settlement confirmation received |
| PendingManualSettlement | Failed | BR-EXT-009 | Manual settlement rejected |
| ReconciliationRequired | Captured | BR-ORD-015 | Operator/provider reconciliation confirms capture |
| ReconciliationRequired | Failed | BR-ORD-015 | Reconciliation confirms failure |

Terminal states have no outgoing transitions: `Refunded`, `Failed`, `Cancelled`, and `Expired`.

### `payment_operation` lifecycle

- **States:** `Requested (initial)`, `InProgress`, `Succeeded (terminal)`, `Failed (terminal)`, `ReconciliationRequired`
- **Transitions:** `Requested → InProgress` after idempotency reservation; `InProgress → Succeeded` on normalized provider success; `InProgress → Failed` on definitive provider failure; `InProgress → ReconciliationRequired` when provider/local outcomes diverge; `ReconciliationRequired → Succeeded` or `Failed` after reconciliation.

### `payment_callback` lifecycle

- **States:** `Received (initial)`, `Applied (terminal)`, `Ignored (terminal)`, `Failed (terminal)`
- **Transitions:** `Received → Applied` after verification and valid state application; `Received → Ignored` for duplicate or irrelevant callbacks; `Received → Failed` for processing failure.

## Data Invariants

| Invariant ID | Statement | Entity | Kind | Tier |
|---|---|---|---|---|
| INV-PA-001 | Payment intent amount and currency are immutable after creation | `payment_intent` | constraint | both |
| INV-PA-002 | A payment operation currency must equal its payment intent currency | `payment_operation` | cross-entity | both |
| INV-PA-003 | Successful plus reserved refunds must not exceed captured amount | `payment_refund` | cross-entity | both |
| INV-PA-004 | A provider reference must belong to one tenant, store, provider, and payment intent scope | `payment_provider_reference` | referential | both |
| INV-PA-005 | A payment operation idempotency scope and request fingerprint are unique | `payment_idempotency` | constraint | db |
| INV-PA-006 | Transaction sequence numbers are unique within a payment intent | `payment_transaction` | constraint | db |
| INV-PA-007 | Computed refundable balance equals captured amount minus successful and reserved refunds | `payment_intent` | computed | both |

Computed-field provenance: `refundable_balance = captured_amount - SUM(payment_refund.amount WHERE status IN ('Reserved','Succeeded'))`.

## Target-to-Legacy Mapping

| Target Entity | Legacy Evidence |
|---|---|
| `payment_transaction` | `SM_TRANSACTION` / `Transaction` lines 35-187 |
| `payment_intent` | New target aggregate required by BR-PA-020 and BR-PA-022 |
| `payment_operation` | New target operation record required by BR-PA-022 |
| `payment_refund` | New target cumulative refund record required by BR-ORD-017 and BR-EXT-003 |
| `payment_provider_reference` | Legacy provider IDs serialized in transaction details |
| `payment_callback` | No confirmed legacy equivalent; required by BR-PA-023 |
| `payment_idempotency` | No legacy equivalent; required by BR-PA-022 |
| `payment_outbox` | No legacy equivalent; required for authenticated cross-service events |

## Phase 4b inferred data clarifications

- `[Inferred in Phase 4b — Mode A]` Callback records retain provider event ID, received time,
  signature-verification result, payment intent ID, provider amount/currency/status, and the
  reconciliation outcome.
- `[Inferred in Phase 4b — Mode A]` Refund balance enforcement is an application invariant
  backed by a serialized payment-intent update or equivalent lock so concurrent refunds cannot
  over-reserve the captured amount.

CREATE INDEX IF NOT EXISTS payment_callback_provider_event_idx
    ON payments.payment_callback (provider_code, provider_event_id);
CREATE INDEX IF NOT EXISTS payment_refund_intent_status_idx
    ON payments.payment_refund (payment_intent_id, status);
