# Hacker News Best Stories API

Small .NET 9 Web API that fetches **best stories** from the Hacker News API, retrieves each item’s details in **parallel**, and returns the **top N stories sorted by score**, with **in-memory caching** to avoid overloading the Hacker News API.

## Tech Stack

- **.NET 9** (ASP.NET Core)
- **Refit** (typed HTTP client)
- **Swagger UI** for interactive docs

---

## How to run the application

### Option A — Visual Studio (HTTP profile)

1. Open the solution in Visual Studio 2022+.
2. In the run dropdown, select the **HTTP** launch profile.
3. Press **F5**.
4. Test in a browser/Postman:
   - **Best stories (v1)**:  
     `GET http://localhost:5012/api/v1/stories?limit=50`  

### Option B — Docker

1. Install Docker Desktop: https://www.docker.com/products/docker-desktop
2. From the repository root (where the `Dockerfile` is), build:
   ```bash
   docker build -t beststories-api:latest .
   ```
3. Run:
   ```bash
   docker run --rm -p 8080:8080 beststories-api:latest
   ```
4. Test:
   - `GET http://localhost:8080/api/v1/stories?limit=50`

### OpenAPI / Swagger UI

If in Development environment:

- **OpenAPI JSON**: `GET /openapi/v1.json`  
- **Swagger UI**: `GET /swagger`

---

## Endpoints (v1)

- **GET** `/api/v1/stories?limit={n}`  
  Returns the best `n` stories sorted by **score** (desc). 
  Response DTO:
  ```json
  [
    {
      "title": "Some title",
      "uri": "https://…",
      "postedBy": "author",
      "time": "2025-10-31T16:20:15+00:00",
      "score": 123,
      "commentCount": 45
    }
  ]
  ```

---

## Configuration

`appsettings.json` (example):
```json
{
  "HackerNews": {
    "BaseUrl": ""
  },
  "HackerNewsService": {
    "ItemTTLInMinutes": 20,
    "MaxDegreeOfParallelism": 10
  }
}
```

- **HackerNews.BaseUrl**: Refit client base address.  
- **HackerNewsService.ItemTTLInMinutes**: cache TTL for `/v0/item/{id}.json`.  
- **HackerNewsService.MaxDegreeOfParallelism**: parallel item fetches from Hacker News.

Environment variable overrides (examples):  
`HackerNews__BaseUrl`, `HackerNewsService__ItemTTLInMinutes`, `HackerNewsService__MaxDegreeOfParallelism`.

---

## Assumptions & design decisions

- **Do not rely on upstream ordering**  
  Although `https://hacker-news.firebaseio.com/v0/beststories.json` often appears ordered, this API **explicitly sorts by the fetched items’ `score`** to guarantee “best *n* by score (desc)” regardless of upstream order or score changes during fetching.

- **Parallel fan-out with bounded concurrency**  
  Uses `parallel requests` with a configurable maximum degree of parallelism to fetch item details quickly **without overloading** the Hacker News API.

- **Per-item cache**  
  Item details are cached for `ItemTTLInMinutes` to reduce repeated calls for frequently requested items.

- **API versioning**  
  We start at **v1** (`/api/v1/...`) to allow evolving the API without breaking clients.

- **Error handling for item fetches**  
  Transient errors during item retrieval are logged and the failed items are omitted from the response.

---

## Enhancements / next steps

**Architecture & structure**
- Split solution into projects:
  - `*.Contracts` (DTOs / request/response models)
  - `*.Application` (services, orchestration, mappers)
  - `*.Infrastructure` (Refit clients, repositories, external integrations)
  - `*.Api` (controllers, DI, configs)
- Add a **default/global error handler** (ProblemDetails middleware) to produce consistent error responses and hide internal details.
- Add a **`sort` query parameter** in the future (e.g., `sort=best`, defaulting to score desc) to enable more flexible result ordering while keeping current behavior as the default.
- Add **integration tests** that spin up the API with a test host (e.g., `WebApplicationFactory`) and verify end-to-end behavior against a mocked Hacker News backend.
- Switch the **in-memory cache** to a distributed **Redis cache**:
   - Shared across multiple API instances for horizontal scaling
   - Persists across restarts/deploys, reducing cold starts
   - Centralized TTL/eviction and easy cross-node invalidation
   - Improved observability (metrics, keyspace notifications)

Enables extras like distributed locks to prevent cache stampedes and simple rate limiting patterns
---

## Testing

### Unit tests

- Frameworks: **xUnit**, **Moq**, **Bogus**.
Run:
```bash
dotnet test
```

### Manual testing (Postman/curl)

**Local (Visual Studio HTTP profile):**
```bash
curl "http://localhost:{PORT}/api/v1/stories?limit=20"
```

**Docker:**
```bash
curl "http://localhost:8080/api/v1/stories?limit=20"
```
