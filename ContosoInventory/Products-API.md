# Products API

The Products API manages inventory products. Routes use the `/api/products` base path and JSON request/response bodies.

## Authentication and access

All product routes require an authenticated user. Authenticate with `POST /api/auth/login` and send the returned HTTP-only authentication cookie with subsequent requests. Any authenticated user can read products. Creating, updating, deleting, and restocking products require the `Admin` role.

Unauthenticated requests receive `401 Unauthorized`. Authenticated users without the required role receive `403 Forbidden`.

## Product fields and validation

| Field | Type | Constraints |
|---|---|---|
| `name` | string | Required; 2–200 characters |
| `sku` | string | Required; 1–64 characters; unique, case-insensitive |
| `description` | string | Required; 1–1000 characters |
| `price` | decimal | 0.01–999999.99 |
| `stockQuantity` | integer | 0 or greater |
| `categoryId` | integer | Must reference an existing category |
| `createdDate` | date/time | Assigned by the server on creation |
| `lastUpdatedDate` | date/time | Assigned by the server on create and update |

Create and update requests contain the first six fields. IDs and dates are managed by the server. Restock requests contain a positive `quantity` to add to the existing stock. Product SKUs are trimmed and compared without regard to case.

## Endpoints

### List products

**`GET /api/products?categoryId={id}`**

Returns all products, optionally filtered by category ID.

- `200 OK` — Products returned.
- `400 Bad Request` — Category ID is not positive.
- `401 Unauthorized` — Authentication cookie is missing or invalid.

### Get a product

**`GET /api/products/{id}`**

- `200 OK` — Product returned.
- `401 Unauthorized` — Authentication cookie is missing or invalid.
- `404 Not Found` — No product has that ID.

### Create a product

**`POST /api/products`** (Admin only)

- `201 Created` — Product created; the `Location` header identifies it.
- `400 Bad Request` — Field validation failed or the category does not exist.
- `401 Unauthorized` / `403 Forbidden` — Authentication or role requirements were not met.
- `409 Conflict` — SKU is already in use, ignoring case.

```json
{
  "name": "Wireless Mouse",
  "sku": "MOUSE-100",
  "description": "Wireless ergonomic mouse",
  "price": 29.99,
  "stockQuantity": 25,
  "categoryId": 4
}
```

### Update a product

**`PUT /api/products/{id}`** (Admin only)

Uses the same request fields and validation as create.

- `200 OK` — Product updated.
- `400 Bad Request` — Field validation failed or the category does not exist.
- `401 Unauthorized` / `403 Forbidden` — Authentication or role requirements were not met.
- `404 Not Found` — No product has that ID.
- `409 Conflict` — Another product already uses the SKU, ignoring case.

### Delete a product

**`DELETE /api/products/{id}`** (Admin only)

- `204 No Content` — Product deleted.
- `401 Unauthorized` / `403 Forbidden` — Authentication or role requirements were not met.
- `404 Not Found` — No product has that ID.

### Restock a product

**`POST /api/products/{id}/restock`** (Admin only)

Adds a positive quantity to the current stock. Returns `400 Bad Request` if the addition would exceed the integer stock limit.

```json
{
  "quantity": 10
}
```

- `200 OK` — Product and updated stock quantity returned.
- `400 Bad Request` — Quantity is not positive or the resulting stock would overflow.
- `401 Unauthorized` / `403 Forbidden` — Authentication or role requirements were not met.
- `404 Not Found` — No product has that ID.

## Category deletion

Categories referenced by products cannot be deleted. `DELETE /api/categories/{id}` returns `409 Conflict` while products reference that category; delete or reassign those products first.

## Manual requests

See [Products-API.http](ContosoInventory.Server/Products-API.http) for examples covering each endpoint.
