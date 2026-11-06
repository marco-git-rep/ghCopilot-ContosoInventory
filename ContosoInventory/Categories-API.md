# Categories API

The Categories API manages inventory categories. All routes use the `/api/categories` base path and JSON request/response bodies.

## Authentication and access

All category routes require an authenticated user. Authenticate with `POST /api/auth/login`; the server sets an HTTP-only authentication cookie. Send that cookie with subsequent requests. Reading categories is available to any authenticated user. Creating, updating, deleting, and toggling a category require the `Admin` role.

Unauthenticated requests receive `401 Unauthorized`. Authenticated users without the required role receive `403 Forbidden`.

The API applies a global per-IP rate limit of 100 requests per minute, with up to 10 queued requests. Excess requests are rejected with `429 Too Many Requests`.

## Data models

### Category response

```json
{
  "id": 12,
  "name": "Networking",
  "description": "Network equipment and accessories",
  "displayOrder": 3,
  "isActive": true,
  "createdDate": "2026-01-15T10:30:00Z",
  "lastModifiedDate": "2026-01-15T10:30:00Z"
}
```

### Create category request

| Field | Type | Constraints |
|---|---|---|
| `name` | string | Required; 2–100 characters |
| `description` | string | Required; up to 500 characters |
| `displayOrder` | integer | 1–1000 |

New categories are active by default. Names must be unique, ignoring case.

### Update category request

| Field | Type | Constraints |
|---|---|---|
| `name` | string | Required; 2–100 characters |
| `description` | string | Required; up to 500 characters |
| `displayOrder` | integer | 1–1000 |
| `isActive` | boolean | Defaults to `false` if omitted |

Names must be unique, ignoring case, excluding the category being updated.

## Endpoints

### List categories

**`GET /api/categories`**

Returns all categories ordered by `displayOrder`.

- **Parameters:** None
- **Request body:** None
- **Query string:** None
- **Responses:**
  - `200 OK` — Categories returned.
  - `401 Unauthorized` — Authentication cookie is missing or invalid.
  - `429 Too Many Requests` — Rate limit exceeded.

**Example request**

```http
GET /api/categories
Accept: application/json
Cookie: .ContosoInventory.Auth=<auth-cookie>
```

**Example response — `200 OK`**

```json
[
  {
    "id": 12,
    "name": "Networking",
    "description": "Network equipment and accessories",
    "displayOrder": 3,
    "isActive": true,
    "createdDate": "2026-01-15T10:30:00Z",
    "lastModifiedDate": "2026-01-15T10:30:00Z"
  }
]
```

### Get a category

**`GET /api/categories/{id}`**

Returns the category with the specified identifier.

- **Path parameters:** `id` (integer, required) — Category identifier.
- **Request body:** None
- **Query string:** None
- **Responses:**
  - `200 OK` — Category returned.
  - `401 Unauthorized` — Authentication cookie is missing or invalid.
  - `404 Not Found` — No category has that identifier; the response body is empty.
  - `429 Too Many Requests` — Rate limit exceeded.

**Example request**

```http
GET /api/categories/12
Accept: application/json
Cookie: .ContosoInventory.Auth=<auth-cookie>
```

**Example response — `200 OK`**

```json
{
  "id": 12,
  "name": "Networking",
  "description": "Network equipment and accessories",
  "displayOrder": 3,
  "isActive": true,
  "createdDate": "2026-01-15T10:30:00Z",
  "lastModifiedDate": "2026-01-15T10:30:00Z"
}
```

### Create a category

**`POST /api/categories`**

Creates an active category. Requires the `Admin` role.

- **Path parameters:** None
- **Request body:** `Create category request`
- **Query string:** None
- **Responses:**
  - `201 Created` — Category created; the `Location` header identifies it.
  - `400 Bad Request` — Request validation failed or the name is already in use.
  - `401 Unauthorized` — Authentication cookie is missing or invalid.
  - `403 Forbidden` — User does not have the `Admin` role.
  - `429 Too Many Requests` — Rate limit exceeded.

**Example request**

```http
POST /api/categories
Content-Type: application/json
Cookie: .ContosoInventory.Auth=<admin-auth-cookie>
```

```json
{
  "name": "Networking",
  "description": "Network equipment and accessories",
  "displayOrder": 3
}
```

**Example response — `201 Created`**

```json
{
  "id": 12,
  "name": "Networking",
  "description": "Network equipment and accessories",
  "displayOrder": 3,
  "isActive": true,
  "createdDate": "2026-01-15T10:30:00Z",
  "lastModifiedDate": "2026-01-15T10:30:00Z"
}
```

**Example response — `400 Bad Request`**

```json
{
  "message": "A category with the name 'Networking' already exists."
}
```

### Update a category

**`PUT /api/categories/{id}`**

Replaces the editable fields of the specified category. Requires the `Admin` role.

- **Path parameters:** `id` (integer, required) — Category identifier.
- **Request body:** `Update category request`
- **Query string:** None
- **Responses:**
  - `200 OK` — Category updated.
  - `400 Bad Request` — Request validation failed or the name is already in use.
  - `401 Unauthorized` — Authentication cookie is missing or invalid.
  - `403 Forbidden` — User does not have the `Admin` role.
  - `404 Not Found` — No category has that identifier; the response body is empty.
  - `429 Too Many Requests` — Rate limit exceeded.

**Example request**

```http
PUT /api/categories/12
Content-Type: application/json
Cookie: .ContosoInventory.Auth=<admin-auth-cookie>
```

```json
{
  "name": "Networking",
  "description": "Network equipment, cables, and accessories",
  "displayOrder": 3,
  "isActive": true
}
```

**Example response — `200 OK`**

```json
{
  "id": 12,
  "name": "Networking",
  "description": "Network equipment, cables, and accessories",
  "displayOrder": 3,
  "isActive": true,
  "createdDate": "2026-01-15T10:30:00Z",
  "lastModifiedDate": "2026-01-16T08:15:00Z"
}
```

**Example response — `400 Bad Request`**

```json
{
  "message": "A category with the name 'Networking' already exists."
}
```

### Delete a category

**`DELETE /api/categories/{id}`**

Permanently deletes the specified category. Requires the `Admin` role.

- **Path parameters:** `id` (integer, required) — Category identifier.
- **Request body:** None
- **Query string:** None
- **Responses:**
  - `204 No Content` — Category deleted; response body is empty.
  - `401 Unauthorized` — Authentication cookie is missing or invalid.
  - `403 Forbidden` — User does not have the `Admin` role.
  - `404 Not Found` — No category has that identifier; the response body is empty.
  - `409 Conflict` — Products still reference the category.
  - `429 Too Many Requests` — Rate limit exceeded.

Categories referenced by products cannot be deleted. Delete or reassign those products first.

**Example request**

```http
DELETE /api/categories/12
Cookie: .ContosoInventory.Auth=<admin-auth-cookie>
```

**Example response — `204 No Content`**

No response body.

### Toggle a category's active status

**`POST /api/categories/{id}/toggle-active`**

Switches the category's `isActive` value and returns the updated category. Requires the `Admin` role.

- **Path parameters:** `id` (integer, required) — Category identifier.
- **Request body:** None
- **Query string:** None
- **Responses:**
  - `200 OK` — Active status toggled; updated category returned.
  - `401 Unauthorized` — Authentication cookie is missing or invalid.
  - `403 Forbidden` — User does not have the `Admin` role.
  - `404 Not Found` — No category has that identifier; the response body is empty.
  - `429 Too Many Requests` — Rate limit exceeded.

**Example request**

```http
POST /api/categories/12/toggle-active
Cookie: .ContosoInventory.Auth=<admin-auth-cookie>
```

**Example response — `200 OK`**

```json
{
  "id": 12,
  "name": "Networking",
  "description": "Network equipment and accessories",
  "displayOrder": 3,
  "isActive": false,
  "createdDate": "2026-01-15T10:30:00Z",
  "lastModifiedDate": "2026-01-16T08:15:00Z"
}
```

## Related documentation

For the login request and authentication response, see [AuthController](ContosoInventory.Server/Controllers/AuthController.cs) and the Swagger UI at `/swagger` while the application is running in Development.
