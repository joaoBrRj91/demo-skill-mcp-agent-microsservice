# Spec: Create Order — Domain Business

## Overview

Customers can place orders against catalog products. Submitting an order is non-blocking: the system immediately acknowledges receipt and begins processing asynchronously. The customer tracks progress through a polling endpoint that returns richer data once processing completes.

---

## Domain Concepts

### Order

An order records a customer's intent to purchase one or more catalog products. It belongs to a single user and carries the chosen payment method and a shipping destination. An order progresses through a defined set of states and cannot regress once it reaches a terminal state.

### Order Status

| Status     | Meaning                                                             |
|------------|---------------------------------------------------------------------|
| Processing | The order has been received; payment is being processed.            |
| Processed  | Payment succeeded; the order is confirmed.                          |
| Error      | Processing failed. An error message explains the cause.             |

### Order Item

Each order line links one catalog product to a quantity and the unit price captured at the moment of order placement. Prices are not recalculated after the order is created.

### Payment Method

Two payment methods are accepted:

- **Credit Card** — requires card number, cardholder name, expiry (MM/YY), and CVV.
- **Pix** — requires a Pix key.

### Shipping Address

The destination for the order: street, city, state, postal code, and country.

### Transaction ID

The identifier assigned to an order at creation. It is the primary reference for polling the order's status.

---

## Business Rules

| ID   | Rule                                                                                         |
|------|----------------------------------------------------------------------------------------------|
| BR-1 | An order must contain at least one item.                                                     |
| BR-2 | Each item must reference a valid catalog product and have a quantity of at least 1.          |
| BR-3 | Unit price is fixed at order creation and never recalculated.                                |
| BR-4 | The payment method must be Credit Card or Pix; no other method is accepted.                  |
| BR-5 | A Credit Card payment requires card number, cardholder name, expiry (MM/YY), and CVV.       |
| BR-6 | A Pix payment requires a non-empty Pix key.                                                  |
| BR-7 | An order in Processed or Error state cannot change status again.                             |
| BR-8 | A gateway error or anti-fraud rejection sets the order to Error with an explanatory message. |

---

## API Contract

### POST /api/v1/orders — Place an Order

**Request body**

| Field                      | Type    | Required | Description                                    |
|----------------------------|---------|----------|------------------------------------------------|
| userId                     | UUID    | Yes      | The customer placing the order                 |
| items                      | array   | Yes      | One or more order lines                        |
| items[].catalogProductId   | UUID    | Yes      | The product being ordered                      |
| items[].quantity           | integer | Yes      | Must be ≥ 1                                    |
| items[].unitPrice          | decimal | Yes      | Price snapshot at the moment of ordering       |
| payment.method             | string  | Yes      | `"CreditCard"` or `"Pix"`                      |
| payment.cardNumber         | string  | Cond.    | Required when method = CreditCard              |
| payment.holderName         | string  | Cond.    | Required when method = CreditCard              |
| payment.expiry             | string  | Cond.    | MM/YY — required when method = CreditCard      |
| payment.cvv                | string  | Cond.    | 3–4 digits — required when method = CreditCard |
| payment.pixKey             | string  | Cond.    | Required when method = Pix                     |
| address.street             | string  | Yes      |                                                |
| address.city               | string  | Yes      |                                                |
| address.state              | string  | Yes      |                                                |
| address.zipCode            | string  | Yes      |                                                |
| address.country            | string  | Yes      |                                                |

**Response — 202 Accepted**

| Field         | Type   | Description                          |
|---------------|--------|--------------------------------------|
| transactionId | UUID   | Identifier used to poll order status |
| status        | string | Always `"Processing"` at this point  |

**Response — 422 Unprocessable Entity**

Returned when validation rules are violated (empty items, missing payment fields, etc.).

---

### GET /api/v1/orders/{transactionId} — Poll Order Status

**Path parameter:** `transactionId` — UUID returned by the POST endpoint.

**Response — 200 OK (while processing)**

| Field         | Type   | Value          |
|---------------|--------|----------------|
| transactionId | UUID   |                |
| status        | string | `"Processing"` |

**Response — 200 OK (processed or error)**

| Field                          | Type     | Notes                                   |
|--------------------------------|----------|-----------------------------------------|
| transactionId                  | UUID     |                                         |
| status                         | string   | `"Processed"` or `"Error"`              |
| errorMessage                   | string?  | Populated only when status = `"Error"`  |
| order.id                       | UUID     |                                         |
| order.userId                   | UUID     |                                         |
| order.items                    | array    |                                         |
| order.items[].catalogProductId | UUID     |                                         |
| order.items[].quantity         | integer  |                                         |
| order.items[].unitPrice        | decimal  |                                         |
| order.paymentMethod            | string   | `"CreditCard"` or `"Pix"`               |
| order.address.street           | string   |                                         |
| order.address.city             | string   |                                         |
| order.address.state            | string   |                                         |
| order.address.zipCode          | string   |                                         |
| order.address.country          | string   |                                         |
| order.createdAt                | datetime | UTC                                     |

**Response — 404 Not Found**

Returned when the `transactionId` does not correspond to any order.

---

## Async Processing Flow

1. Client sends POST with order details.
2. System persists the order with status **Processing** and returns the `transactionId` immediately (202 Accepted).
3. In the background, the system calls the payment gateway with the order's payment details and total amount.
4. **On success:** the order transitions to **Processed**.
5. **On failure:** the order transitions to **Error** with a message describing the reason (e.g., gateway timeout, anti-fraud rejection, insufficient funds).
6. Client polls GET until status is no longer `"Processing"`.

The payment gateway integration is initially backed by a mock that simulates success. The mock is a placeholder for a real provider to be selected later; substituting it must not require changes to order creation or processing logic.

---

## Test Scenarios

| #  | Scenario                             | Given                                               | Expected                                              |
|----|--------------------------------------|-----------------------------------------------------|-------------------------------------------------------|
| 1  | Successful Pix order                 | Valid payload, Pix method, gateway succeeds         | 202 → eventually GET returns Processed + full order   |
| 2  | Successful CreditCard order          | Valid payload, CreditCard method, gateway succeeds  | Same as above                                         |
| 3  | Gateway failure                      | Gateway returns failure with error message          | Order status = Error; errorMessage matches            |
| 4  | Poll while still processing          | GET called before background job completes          | 200 with status=Processing, no order details          |
| 5  | Poll after processed                 | GET called after background job succeeds            | 200 with full order details                           |
| 6  | Poll after error                     | GET called after gateway failure                    | 200 with status=Error and errorMessage                |
| 7  | Unknown transaction ID               | GET with non-existent UUID                          | 404 Not Found                                         |
| 8  | Empty items list                     | POST with items=[]                                  | 422 Unprocessable Entity                              |
| 9  | Missing CreditCard fields            | POST with method=CreditCard, no cardNumber          | 422 Unprocessable Entity                              |
| 10 | Missing PixKey                       | POST with method=Pix, no pixKey                     | 422 Unprocessable Entity                              |
| 11 | Item quantity below minimum          | POST with quantity=0                                | 422 Unprocessable Entity                              |
| 12 | Attempt to re-process resolved order | ProcessOrder called on Processed/Error order        | Domain exception; status unchanged                    |
| 13 | Price snapshot is preserved          | Valid POST with items[].unitPrice=9.99; GET after processing completes | Stored unitPrice equals 9.99 — no recalculation |
| 14 | Unsupported payment method           | POST with payment.method="BankTransfer"             | 422 Unprocessable Entity                              |
