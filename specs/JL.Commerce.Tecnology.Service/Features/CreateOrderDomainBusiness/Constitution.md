# Constitution — CreateOrderDomainBusiness

> **Status:** Ratified  
> **Scope:** `JL.Commerce.Tecnology.Service` — Order aggregate and all subsystems that produce, consume, or expose Order data.

---

## Preamble

This document defines the **immutable laws** of the Order domain. The rules herein are not implementation suggestions — they are constitutional constraints. Any code, migration, or API change that violates a rule in this document is a **constitution violation**, not a tech-debt item, and must be corrected before the change is merged.

The Spec.md describes *what* the system does. The Plan.md describes *how* it will be built. This Constitution describes the **invariants that neither document may override**.

Conformance language follows RFC 2119: **MUST** / **MUST NOT** / **SHALL** / **SHALL NOT** / **SHOULD** / **MAY**.

---

## Article I — Order State Machine (Immutable Workflow)

### § 1.1 Valid States

| State        | Meaning                                               | Terminal? |
|--------------|-------------------------------------------------------|-----------|
| `Processing` | Order accepted; payment is being processed async      | No        |
| `Processed`  | Payment confirmed; order is fulfilled                 | **Yes**   |
| `Error`      | Payment failed or gateway unreachable                 | **Yes**   |

### § 1.2 Valid Transitions

```
  POST /api/v1/orders
         │
         ▼
   [Processing] ─── payment success ──► [Processed]
         │
         └─────── payment failure / timeout ──► [Error]
```

No other transitions exist. There is no path from `Processed` or `Error` to any other state.

### § 1.3 Workflow Laws

| ID        | Rule                                                                                                                        |
|-----------|-----------------------------------------------------------------------------------------------------------------------------|
| CON-WF-1  | An Order's initial state on creation MUST be `Processing`. No other initial state is valid.                                 |
| CON-WF-2  | `Processing` MUST transition to `Processed` on confirmed payment, or to `Error` on failure or gateway timeout.              |
| CON-WF-3  | `Processed` and `Error` are **terminal states**. Once reached, no further state change is permitted under any circumstance. |
| CON-WF-4  | State transitions MUST be triggered exclusively by domain events (`OrderProcessedEvent`, `OrderErrorEvent`). Direct assignment to the status property outside the aggregate is forbidden. |
| CON-WF-5  | Every state transition MUST record the UTC timestamp of the transition as part of the Order's audit trail.                  |
| CON-WF-6  | The background payment processor MUST check current state before acting. If the order is already in a terminal state, the processor MUST treat it as a no-op and return without re-processing. |

---

## Article II — Domain Invariants

These invariants are enforced inside the Order aggregate. No command handler, infrastructure adapter, or test fixture may bypass them.

| ID        | Source  | Rule                                                                                                                                              |
|-----------|---------|---------------------------------------------------------------------------------------------------------------------------------------------------|
| CON-DI-1  | BR-1    | An Order MUST contain at least one `OrderItem`. A zero-item order MUST be rejected before persistence.                                            |
| CON-DI-2  | BR-2    | Every `OrderItem` MUST reference a CatalogProduct that is **valid and active** at the time of order creation. Inactive or non-existent products MUST be rejected. |
| CON-DI-3  | BR-3    | The unit price on each `OrderItem` is a **price snapshot** captured at creation time. It is immutable; subsequent catalog price changes MUST NOT affect existing orders. |
| CON-DI-4  | BR-4    | `PaymentMethod` MUST be one of the system-recognised values: `CreditCard` or `Pix`. Any other value MUST be rejected with HTTP 400 before the command reaches the aggregate. |
| CON-DI-5  | BR-5    | When `PaymentMethod` is `Pix`, no card data is required or accepted. When `PaymentMethod` is `CreditCard`, a card payment token MUST be present in the command. |
| CON-DI-6  | BR-6    | The total order amount MUST be greater than zero. An order where all items have zero price or zero quantity MUST be rejected.                      |
| CON-DI-7  | BR-7    | Once an Order is in a terminal state (`Processed` or `Error`), no property of the aggregate — including its items, amounts, or payment details — may be mutated. |
| CON-DI-8  | BR-8    | If the payment gateway is unreachable, returns a non-success response, or times out, the order MUST transition to `Error`. There is no "partial success" state. |

---

## Article III — Idempotency & Concurrency Laws

| ID        | Rule                                                                                                                                                     |
|-----------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| CON-IC-1  | Every `POST /api/v1/orders` request MUST include a client-generated `TransactionId` (UUID v4). This field is the **idempotency key** for the operation.  |
| CON-IC-2  | If an Order with the same `TransactionId` already exists, the server MUST return the current order status without creating a duplicate or re-triggering payment processing. |
| CON-IC-3  | Concurrent `POST` requests carrying the same `TransactionId` MUST be serialized at the persistence layer. The database MUST enforce a unique constraint on `TransactionId`. Optimistic concurrency (EF Core row version) MUST be used as a secondary guard. |
| CON-IC-4  | The async payment processor MUST be idempotent end-to-end. Re-delivery of the same `OrderCreatedEvent` message MUST NOT produce duplicate payment charges or duplicate state transitions. |

---

## Article IV — Data Security Mandates

### § 4.1 Request and Response Sanitization

| ID        | Rule                                                                                                                                                         |
|-----------|--------------------------------------------------------------------------------------------------------------------------------------------------------------|
| CON-SEC-1 | All incoming request payloads MUST be sanitized at the Application layer (via FluentValidation) before the command handler executes. Sanitization MUST strip HTML tags, null bytes (`\0`), and control characters from all string fields. |
| CON-SEC-2 | API responses MUST NEVER include: raw card numbers, CVV codes, full CPF/CNPJ digits, full account numbers, or any other raw financial credential. These fields MUST be masked or excluded from every DTO and serialized output. |
| CON-SEC-3 | Error responses MUST NOT expose internal stack traces, raw database error messages, or gateway response bodies to API consumers. The caller receives only a correlation ID and a generic error message. Full diagnostic details are written to structured logs only. |
| CON-SEC-4 | All HTTP responses from Order endpoints MUST include the following security headers: `X-Content-Type-Options: nosniff`, `Cache-Control: no-store, no-cache` (payment-related endpoints), and `Strict-Transport-Security: max-age=31536000; includeSubDomains` (HSTS). |

### § 4.2 Sensitive Data at Rest — Encryption and Masking

**PII/PCI Classification Table:**

| Field                            | Classification | Storage Rule                                 | API Display Rule              |
|----------------------------------|----------------|----------------------------------------------|-------------------------------|
| `ShippingAddress.RecipientName`  | PII            | Encrypted at rest (AES-256-GCM)              | Omitted from response         |
| `ShippingAddress.Street`         | PII            | Encrypted at rest (AES-256-GCM)              | City + State + ZipCode only   |
| `ShippingAddress.Complement`     | PII            | Encrypted at rest (AES-256-GCM)              | Omitted from response         |
| `PaymentDetails.CardHolderName`  | PCI            | Encrypted at rest (AES-256-GCM)              | Omitted from response         |
| `PaymentDetails.CardLastFour`    | PCI            | Stored plain (4 digits only, never full PAN) | `**** **** **** {last4}`      |
| `CustomerDocument` (CPF/CNPJ)    | PII            | Encrypted at rest (AES-256-GCM)              | `***.***.***-{last2}` (CPF)   |

| ID         | Rule                                                                                                                                                          |
|------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------|
| CON-SEC-5  | All fields in the PII/PCI table MUST be stored encrypted at rest using AES-256-GCM or an equivalent authenticated encryption scheme.                         |
| CON-SEC-6  | Encrypted ciphertext stored in the database MUST be decryptable by the Application/Domain layer for internal processing (fraud checks, gateway calls, business rule evaluation). Decryption MUST NOT occur in Presentation layer code. Decrypted values MUST NEVER appear in API responses. |
| CON-SEC-7  | Read projections (DTOs) returned to API consumers MUST apply the display masking rules in the table above, independently of any stored ciphertext. Masking is applied by the AutoMapper profile or a dedicated masking service, never inside the aggregate. |
| CON-SEC-8  | Encryption keys MUST be managed outside application source code — via environment variables, Azure Key Vault, AWS Secrets Manager, or equivalent. Hardcoded keys in source or config files are a constitution violation. |
| CON-SEC-9  | Full PAN (Primary Account Number) MUST NEVER be stored in any layer — database, cache, log, or message queue. Only tokenized card references (from the payment gateway) or the last four digits are permitted to persist. |
| CON-SEC-10 | Sensitive PII fields MUST NEVER appear in application logs. Log statements referencing an order MUST use only `OrderId`, `TransactionId`, and `OrderStatus`. |

---

## Article V — Governance and Compliance

| ID        | Rule                                                                                                                                                                      |
|-----------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| CON-GOV-1 | Every Order state transition MUST produce an immutable audit log entry containing: `OrderId`, `TransactionId`, `FromState`, `ToState`, `OccurredAtUtc`, `TriggeredByEvent`. |
| CON-GOV-2 | Order records MUST be retained for a minimum of **5 years** from creation date to satisfy financial audit requirements. Physical deletion of Order rows is forbidden. Logical (soft) deletion via a `DeletedAt` flag is the only permitted removal mechanism, and it MUST NOT be applied within the retention window. |
| CON-GOV-3 | PII fields on Orders (name, address, CPF) are subject to LGPD (Brazil) / GDPR right-to-erasure requests. Erasure is fulfilled by replacing encrypted PII values with a pseudonymised tombstone token (e.g., `[ERASED-{timestamp}]`). The Order record itself MUST be retained to satisfy CON-GOV-2. Financial data takes precedence over erasure when in conflict. |
| CON-GOV-4 | Payment processing MUST comply with PCI-DSS scope reduction. No raw card data (PAN, CVV) may traverse application memory beyond the initial tokenisation call to the payment gateway. The service operates as a PCI SAQ-A-EP (or equivalent) reduced-scope participant. |
| CON-GOV-5 | All communication with the payment gateway MUST use TLS 1.2 or higher with server certificate validation enabled. Disabling certificate validation or using self-signed certificates in production environments is a constitution violation. |
| CON-GOV-6 | The audit log (CON-GOV-1) MUST be append-only. No audit entry may be updated or deleted by any application code path. Audit records are outside the normal soft-delete lifecycle. |

---

## Article VI — API Contract Invariants

These constraints apply to the external API surface. They MUST NOT be changed without a version increment.

| ID        | Rule                                                                                                                                                    |
|-----------|---------------------------------------------------------------------------------------------------------------------------------------------------------|
| CON-API-1 | `POST /api/v1/orders` MUST return **HTTP 202 Accepted** when the order is accepted for async processing. Using 200 or 201 is a contract violation.      |
| CON-API-2 | The 202 response body MUST include the `transactionId` and the initial `status` (`Processing`). Clients MUST NOT be required to parse the `Location` header to discover the ID. |
| CON-API-3 | `GET /api/v1/orders/{transactionId}` MUST return **HTTP 404** when no order exists for the given `transactionId`. Using 200 with an empty body or 400 is a contract violation. |
| CON-API-4 | The GET endpoint MUST return the current status without blocking or long-polling. This endpoint is a polling target; clients are responsible for polling cadence. |
| CON-API-5 | Breaking changes to response schema (field removal, type change, renamed field) MUST trigger a version increment (e.g., `/api/v2/orders`). Additive changes (new optional fields) are non-breaking and do not require a version bump. |
| CON-API-6 | The `transactionId` path parameter on the GET endpoint MUST be treated as an opaque UUID. Invalid UUIDs MUST return HTTP 400 before any domain logic executes. |

---

## Appendix — Constitution Rule Index

| Article   | ID Range             | Domain                              |
|-----------|----------------------|-------------------------------------|
| I         | CON-WF-1 – CON-WF-6  | Order State Machine                 |
| II        | CON-DI-1 – CON-DI-8  | Domain Invariants                   |
| III       | CON-IC-1 – CON-IC-4  | Idempotency & Concurrency           |
| IV § 4.1  | CON-SEC-1 – CON-SEC-4 | Request/Response Sanitization      |
| IV § 4.2  | CON-SEC-5 – CON-SEC-10 | Sensitive Data Encryption & Masking |
| V         | CON-GOV-1 – CON-GOV-6 | Governance & Compliance            |
| VI        | CON-API-1 – CON-API-6 | API Contract Invariants             |
