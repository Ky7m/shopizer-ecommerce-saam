# Customer and Identity — Domain Model

**Schema:** `customer_identity`  
**Database:** PostgreSQL 16  
**Ownership:** MS-01 owns all tables below. `store_id` is an opaque reference to MS-10; no cross-service foreign key is created.

## Core Entities

```sql
CREATE SCHEMA IF NOT EXISTS customer_identity;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TYPE customer_identity.customer_status AS ENUM ('Active', 'Suspended', 'Deleted');
CREATE TYPE customer_identity.address_type AS ENUM ('Billing', 'Delivery');
CREATE TYPE customer_identity.review_status AS ENUM ('Pending', 'Published', 'Rejected', 'Deleted');
CREATE TYPE customer_identity.reset_subject_type AS ENUM ('Customer', 'Administrator');
CREATE TYPE customer_identity.subscription_status AS ENUM ('Subscribed', 'Unsubscribed');

CREATE TABLE customer_identity.customer_accounts (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  store_id VARCHAR(100) NOT NULL,
  login_name VARCHAR(96) NOT NULL,
  email_address VARCHAR(96) NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  gender VARCHAR(16) NOT NULL DEFAULT 'M',
  date_of_birth DATE,
  company_name VARCHAR(100),
  provider VARCHAR(80),
  status customer_identity.customer_status NOT NULL DEFAULT 'Active',
  default_language_code VARCHAR(10) NOT NULL,
  review_average NUMERIC(4,2) NOT NULL DEFAULT 0 CHECK (review_average >= 0 AND review_average <= 5),
  review_count INTEGER NOT NULL DEFAULT 0 CHECK (review_count >= 0),
  anonymous BOOLEAN NOT NULL DEFAULT FALSE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  created_by VARCHAR(100),
  correlation_id UUID,
  CONSTRAINT uq_customer_store_login UNIQUE (store_id, login_name),
  CONSTRAINT uq_customer_store_email UNIQUE (store_id, email_address)
);

CREATE INDEX ix_customer_store_email ON customer_identity.customer_accounts (store_id, email_address);
CREATE INDEX ix_customer_status ON customer_identity.customer_accounts (store_id, status);

CREATE TABLE customer_identity.customer_addresses (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  customer_id UUID NOT NULL REFERENCES customer_identity.customer_accounts(id) ON DELETE CASCADE,
  address_type customer_identity.address_type NOT NULL,
  first_name VARCHAR(64) NOT NULL,
  last_name VARCHAR(64) NOT NULL,
  company_name VARCHAR(100),
  street_address VARCHAR(256) NOT NULL,
  city VARCHAR(100) NOT NULL,
  postal_code VARCHAR(20) NOT NULL,
  state_province VARCHAR(100),
  telephone VARCHAR(32),
  country_code VARCHAR(10) NOT NULL,
  zone_code VARCHAR(20),
  latitude VARCHAR(100),
  longitude VARCHAR(100),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT uq_customer_address_type UNIQUE (customer_id, address_type)
);

CREATE INDEX ix_customer_address_country ON customer_identity.customer_addresses (country_code, zone_code);

CREATE TABLE customer_identity.customer_options (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  store_id VARCHAR(100) NOT NULL,
  code VARCHAR(100) NOT NULL,
  option_type VARCHAR(10) NOT NULL,
  sort_order INTEGER NOT NULL DEFAULT 0,
  is_active BOOLEAN NOT NULL DEFAULT TRUE,
  is_public BOOLEAN NOT NULL DEFAULT FALSE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT uq_customer_option_store_code UNIQUE (store_id, code),
  CONSTRAINT ck_customer_option_code CHECK (code ~ '^[A-Za-z0-9_]+$')
);

CREATE INDEX ix_customer_option_store ON customer_identity.customer_options (store_id, sort_order);

CREATE TABLE customer_identity.customer_option_values (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  option_id UUID NOT NULL REFERENCES customer_identity.customer_options(id) ON DELETE CASCADE,
  store_id VARCHAR(100) NOT NULL,
  code VARCHAR(100) NOT NULL,
  image_url VARCHAR(512),
  sort_order INTEGER NOT NULL DEFAULT 0,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT uq_customer_option_value_store_code UNIQUE (store_id, code),
  CONSTRAINT uq_customer_option_value_option_code UNIQUE (option_id, code),
  CONSTRAINT ck_customer_option_value_code CHECK (code ~ '^[A-Za-z0-9_]+$')
);

CREATE TABLE customer_identity.customer_attributes (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  customer_id UUID NOT NULL REFERENCES customer_identity.customer_accounts(id) ON DELETE CASCADE,
  option_id UUID NOT NULL REFERENCES customer_identity.customer_options(id) ON DELETE RESTRICT,
  option_value_id UUID NOT NULL REFERENCES customer_identity.customer_option_values(id) ON DELETE RESTRICT,
  text_value TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT uq_customer_attribute_option UNIQUE (customer_id, option_id)
);

CREATE INDEX ix_customer_attributes_customer ON customer_identity.customer_attributes (customer_id);

CREATE TABLE customer_identity.customer_reviews (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  reviewer_customer_id UUID NOT NULL REFERENCES customer_identity.customer_accounts(id) ON DELETE CASCADE,
  reviewed_customer_id UUID NOT NULL REFERENCES customer_identity.customer_accounts(id) ON DELETE CASCADE,
  rating NUMERIC(3,1) NOT NULL CHECK (rating >= 1 AND rating <= 5),
  review_text TEXT,
  review_date TIMESTAMPTZ NOT NULL DEFAULT now(),
  status customer_identity.review_status NOT NULL DEFAULT 'Pending',
  read_count BIGINT NOT NULL DEFAULT 0 CHECK (read_count >= 0),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT uq_customer_review_pair UNIQUE (reviewer_customer_id, reviewed_customer_id),
  CONSTRAINT ck_customer_review_not_self CHECK (reviewer_customer_id <> reviewed_customer_id)
);

CREATE INDEX ix_customer_reviews_target ON customer_identity.customer_reviews (reviewed_customer_id, status);

CREATE TABLE customer_identity.newsletter_subscriptions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  store_id VARCHAR(100) NOT NULL,
  campaign_code VARCHAR(50) NOT NULL,
  email_address VARCHAR(320) NOT NULL,
  first_name VARCHAR(64),
  last_name VARCHAR(64),
  status customer_identity.subscription_status NOT NULL DEFAULT 'Subscribed',
  subscribed_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  unsubscribed_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT uq_newsletter_store_campaign_email UNIQUE (store_id, campaign_code, email_address)
);

CREATE INDEX ix_newsletter_email ON customer_identity.newsletter_subscriptions (store_id, email_address);

CREATE TABLE customer_identity.permission_groups (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name VARCHAR(100) NOT NULL UNIQUE,
  group_type VARCHAR(30) NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE customer_identity.permissions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name VARCHAR(150) NOT NULL UNIQUE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE customer_identity.group_permissions (
  group_id UUID NOT NULL REFERENCES customer_identity.permission_groups(id) ON DELETE CASCADE,
  permission_id UUID NOT NULL REFERENCES customer_identity.permissions(id) ON DELETE CASCADE,
  PRIMARY KEY (group_id, permission_id)
);

CREATE TABLE customer_identity.administrator_accounts (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  store_id VARCHAR(100) NOT NULL,
  user_name VARCHAR(100) NOT NULL,
  email_address VARCHAR(320) NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  first_name VARCHAR(100),
  last_name VARCHAR(100),
  is_active BOOLEAN NOT NULL DEFAULT TRUE,
  default_language_code VARCHAR(10),
  question_one TEXT,
  question_two TEXT,
  question_three TEXT,
  answer_one TEXT,
  answer_two TEXT,
  answer_three TEXT,
  last_access_at TIMESTAMPTZ,
  login_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  created_by VARCHAR(100),
  correlation_id UUID,
  CONSTRAINT uq_admin_store_username UNIQUE (store_id, user_name)
);

CREATE INDEX ix_admin_store_email ON customer_identity.administrator_accounts (store_id, email_address);
CREATE INDEX ix_admin_active ON customer_identity.administrator_accounts (store_id, is_active);

CREATE TABLE customer_identity.administrator_group_memberships (
  administrator_id UUID NOT NULL REFERENCES customer_identity.administrator_accounts(id) ON DELETE CASCADE,
  group_id UUID NOT NULL REFERENCES customer_identity.permission_groups(id) ON DELETE RESTRICT,
  PRIMARY KEY (administrator_id, group_id)
);

CREATE TABLE customer_identity.credential_reset_tokens (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  subject_type customer_identity.reset_subject_type NOT NULL,
  customer_id UUID REFERENCES customer_identity.customer_accounts(id) ON DELETE CASCADE,
  administrator_id UUID REFERENCES customer_identity.administrator_accounts(id) ON DELETE CASCADE,
  token_hash VARCHAR(255) NOT NULL UNIQUE,
  expires_at TIMESTAMPTZ NOT NULL,
  consumed_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT ck_reset_one_subject CHECK (
    (subject_type = 'Customer' AND customer_id IS NOT NULL AND administrator_id IS NULL)
    OR (subject_type = 'Administrator' AND administrator_id IS NOT NULL AND customer_id IS NULL)
  )
);

CREATE INDEX ix_reset_expiry ON customer_identity.credential_reset_tokens (expires_at, consumed_at);

CREATE TABLE customer_identity.external_identity_connections (
  user_id VARCHAR(100) NOT NULL,
  provider_id VARCHAR(100) NOT NULL,
  provider_user_id VARCHAR(255) NOT NULL,
  access_token TEXT,
  refresh_token TEXT,
  secret TEXT,
  display_name VARCHAR(255),
  profile_url VARCHAR(512),
  image_url VARCHAR(512),
  expires_at TIMESTAMPTZ,
  rank INTEGER NOT NULL DEFAULT 0,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, provider_id, provider_user_id)
);

COMMENT ON COLUMN customer_identity.customer_accounts.store_id IS 'Opaque MS-10 store reference; maps from CUSTOMER.MERCHANT_ID and BR-CUS-001.';
COMMENT ON COLUMN customer_identity.customer_accounts.login_name IS 'Maps from CUSTOMER.CUSTOMER_NICK; BR-CUS-001/002.';
COMMENT ON COLUMN customer_identity.customer_accounts.email_address IS 'Maps from CUSTOMER.CUSTOMER_EMAIL_ADDRESS; BR-CUS-003.';
COMMENT ON COLUMN customer_identity.customer_accounts.password_hash IS 'Maps from CUSTOMER.CUSTOMER_PASSWORD; encoded per BR-CUS-005.';
COMMENT ON COLUMN customer_identity.customer_accounts.review_average IS 'Maps from CUSTOMER.REVIEW_AVG; maintained by BR-CUS-023..025.';
COMMENT ON COLUMN customer_identity.customer_accounts.review_count IS 'Maps from CUSTOMER.REVIEW_COUNT; maintained by BR-CUS-023..025.';
COMMENT ON COLUMN customer_identity.customer_addresses.state_province IS 'Maps from BILLING_STATE/DELIVERY_STATE; kept distinct from postal_code per BR-CUS-014.';
COMMENT ON COLUMN customer_identity.customer_attributes.option_id IS 'Maps from CUSTOMER_ATTRIBUTE.OPTION_ID; scoped by BR-CUS-015.';
COMMENT ON COLUMN customer_identity.customer_attributes.option_value_id IS 'Maps from CUSTOMER_ATTRIBUTE.OPTION_VALUE_ID; scoped by BR-CUS-015.';
COMMENT ON COLUMN customer_identity.newsletter_subscriptions.store_id IS 'Maps from CUSTOMER_OPTIN.MERCHANT_ID; included in uniqueness per BR-CUS-027.';
COMMENT ON COLUMN customer_identity.credential_reset_tokens.token_hash IS 'Maps from RESET_CREDENTIALS_REQ as a one-way token reference; BR-CUS-NN-001..003.';
COMMENT ON COLUMN customer_identity.external_identity_connections.user_id IS 'Maps from USERCONNECTION.userId; composite identity key per BR-CUS-NN-021.';
```

## Entity State Model

#### Customer account lifecycle
| State | Type |
|---|---|
| Active | initial |
| Suspended | — |
| Deleted | terminal |

| From | To | Trigger (BR-ID) | Guard |
|---|---|---|---|
| Active | Suspended | BR-CUS-NN-006 | Password reset or security administration invalidates access |
| Suspended | Active | BR-CUS-007 | A valid administrator re-enables the account |
| Active | Deleted | BR-CUS-017 | Authorized deletion request |
| Suspended | Deleted | BR-CUS-017 | Authorized deletion request |

#### Administrator account lifecycle
| State | Type |
|---|---|
| Active | initial |
| Suspended | — |
| Deleted | terminal |

| From | To | Trigger (BR-ID) | Guard |
|---|---|---|---|
| Active | Suspended | BR-CUS-NN-020 | Authorized enablement request sets active=false |
| Suspended | Active | BR-CUS-NN-020 | Authorized enablement request sets active=true |
| Active | Deleted | BR-CUS-NN-015 | Target is not a protected super administrator |
| Suspended | Deleted | BR-CUS-NN-015 | Target is not a protected super administrator |

#### Customer review lifecycle
| State | Type |
|---|---|
| Pending | initial |
| Published | — |
| Rejected | — |
| Deleted | terminal |

| From | To | Trigger (BR-ID) | Guard |
|---|---|---|---|
| Pending | Published | BR-CUS-021 | Unique reviewer-target pair and rating 1..5 |
| Pending | Rejected | BR-CUS-022 | Rating or moderation policy rejects the review |
| Rejected | Pending | BR-CUS-024 | Authorized correction supplies valid review content |
| Published | Pending | BR-CUS-024 | Authorized edit requires re-evaluation |
| Pending | Deleted | BR-CUS-025 | Authorized owner deletion |
| Published | Deleted | BR-CUS-025 | Authorized owner deletion |

## Data Invariants

| Invariant ID | Statement | Entity | Kind | Tier |
|---|---|---|---|---|
| INV-CUS-001 | A customer login name is unique within a store. | customer_accounts | uniqueness | db |
| INV-CUS-002 | A newsletter email is unique within a store and campaign. | newsletter_subscriptions | uniqueness | db |
| INV-CUS-003 | A review rating is between 1 and 5 inclusive. | customer_reviews | range | db |
| INV-CUS-004 | A customer review pair contains distinct reviewer and reviewed customers. | customer_reviews | referential | db |
| INV-CUS-005 | Customer review average equals the sum of current review ratings divided by current review count, or zero when no reviews exist. | customer_accounts | computed: SUM(customer_reviews.rating) / COUNT(customer_reviews.id) | both |
| INV-CUS-006 | A consumed credential reset token cannot be used again. | credential_reset_tokens | monotonic-status | both |
| INV-CUS-007 | A reset token belongs to exactly one customer or administrator subject. | credential_reset_tokens | referential | db |

## Column Provenance Notes

- Embedded billing and delivery fields in `Customer` become rows in `customer_addresses` to allow explicit address type and independent updates.
- `customer_accounts.store_id` and `administrator_accounts.store_id` intentionally have no FK: MS-10 owns store lifecycle and architecture prohibits cross-service database writes.
- Reset credentials are split into `credential_reset_tokens` so consumption is explicit and replay prevention is enforceable.
- `review_average` and `review_count` are retained as read-optimized projections of `customer_reviews`; BR-CUS-NN-003/004/005 and the invariant require transactional recomputation.
- Group and permission tables preserve the source many-to-many relationships while using domain names and internal foreign keys.

| Target table | Column provenance / justification |
|---|---|
| `customer_accounts` | `id` replaces `CUSTOMER_ID`; `store_id` replaces `MERCHANT_ID`; `login_name` replaces `CUSTOMER_NICK`; `email_address` replaces `CUSTOMER_EMAIL_ADDRESS`; `password_hash` replaces `CUSTOMER_PASSWORD`; `gender`, `date_of_birth`, `company_name`, `provider`, `anonymous` replace corresponding `Customer` fields; `status` is the target lifecycle; language and audit fields are standard identity context. |
| `customer_addresses` | `customer_id` is the internal FK; address, name, company, phone, country, zone, state, postal, latitude, and longitude map from embedded `Billing`/`Delivery` fields; `address_type` is required to separate the two embedded value objects. |
| `customer_options` | `store_id`, `code`, `option_type`, `sort_order`, `is_active`, and `is_public` map from `CustomerOption`; audit fields are standard. |
| `customer_option_values` | `option_id` is the target parent relationship needed by option-set behavior; `store_id`, `code`, `image_url`, and `sort_order` map from `CustomerOptionValue`; audit fields are standard. |
| `customer_attributes` | `customer_id`, `option_id`, `option_value_id`, and `text_value` map from `CustomerAttribute`; unique customer/option assignment implements the source uniqueness constraint. |
| `customer_reviews` | Reviewer/target IDs map from `CUSTOMERS_ID` and `REVIEWED_CUSTOMER_ID`; `rating`, `read_count`, `review_date`, and `status` map from the review entity; `review_text` carries localized description content referenced by the API model; audit fields are standard. |
| `newsletter_subscriptions` | `store_id`, `campaign_code`, email, names, and subscription timestamps map from `CustomerOptin` plus `Optin`; `status` and `unsubscribed_at` are required to close the advertised unsubscribe operation; scoped uniqueness corrects the source omission. |
| `permission_groups` | `name` and `group_type` map from `SM_GROUP.GROUP_NAME` and `GROUP_TYPE`; audit fields are standard. |
| `permissions` | `name` maps from `PERMISSION.PERMISSION_NAME`; audit fields are standard. |
| `group_permissions` | Composite keys map the source `PERMISSION_GROUP` relationship; both FKs are internal. |
| `administrator_accounts` | `store_id`, username, email, password, names, active flag, language, security questions/answers, and access/login timestamps map from `USERS`; audit fields are standard. |
| `administrator_group_memberships` | Composite relationship maps from `USER_GROUP`; internal FKs enforce membership integrity. |
| `credential_reset_tokens` | Subject type, subject FK, token hash, expiry, and consumption timestamp derive from `CredentialsReset` and net-new replay prevention requirements. |
| `external_identity_connections` | Composite identity fields and provider token/profile metadata map from `UserConnection`, `RemoteUser`, and `UserConnectionPK`; timestamps are standard. |
