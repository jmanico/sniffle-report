# Architecture — Sniffle Report

## 1. System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                          Public Internet                            │
└──────────┬──────────────────────────────────┬───────────────────────┘
           │                                  │
           ▼                                  ▼
┌─────────────────────┐          ┌─────────────────────────┐
│   React Frontend    │          │   Admin Panel (React)    │
│   (Vite + TS)       │          │   /admin routes          │
│   Port 5173 (dev)   │          │   Auth-gated             │
└────────┬────────────┘          └────────┬────────────────┘
         │ HTTPS                          │ HTTPS + Bearer Token
         ▼                                ▼
┌──────────────────────────────────────────────────────────┐
│              ASP.NET Core Web API (.NET 8)                │
│                                                          │
│  ┌──────────┐  ┌──────────┐  ┌───────────┐  ┌────────┐  │
│  │ Public   │  │ Admin    │  │ Fact-Check│  │ Auth   │  │
│  │Controllers│ │Controllers│ │ Service   │  │Middleware│ │
│  └────┬─────┘  └────┬─────┘  └─────┬─────┘  └────────┘  │
│       │              │              │                     │
│  ┌────▼──────────────▼──────────────▼─────┐              │
│  │         Service Layer                   │              │
│  │  (RegionService, AlertService,          │              │
│  │   TrendService, ResourceService,        │              │
│  │   FactCheckService, PreventionService)  │              │
│  └────────────────┬───────────────────────┘              │
│                   │                                      │
│  ┌────────────────▼───────────────────────┐              │
│  │   Entity Framework Core (DbContext)     │              │
│  └────────────────┬───────────────────────┘              │
└───────────────────┼──────────────────────────────────────┘
                    │
         ┌──────────▼──────────┐
         │   PostgreSQL 16     │
         │   Port 5432         │
         └─────────────────────┘

External integrations (server-side outbound):
  ├── CDC API / Data feeds
  ├── State & county health department APIs
  ├── WHO Disease Outbreak News
  └── Geocoding service (for resource/region mapping)
```

### User Flows

**Public user**: Selects region → views regional dashboard (active alerts, trend charts, prevention guides) → drills into specific disease or resource → finds local clinics/pharmacies with cost info.

**Admin user**: Logs in → creates/edits health alerts with case data and source attribution → attaches prevention guides with pricing tiers → manages local resources → submits health news items for fact-checking → reviews and publishes fact-check results.

---

## 2. Backend Architecture (ASP.NET Core)

### API Layer

REST API versioned at the URL path level:

```
/api/v1/regions
/api/v1/regions/{regionId}/alerts
/api/v1/regions/{regionId}/trends
/api/v1/regions/{regionId}/resources
/api/v1/regions/{regionId}/prevention
/api/v1/fact-checks
/api/v1/admin/alerts          (auth required)
/api/v1/admin/resources       (auth required)
/api/v1/admin/prevention      (auth required)
/api/v1/admin/fact-checks     (auth required)
/api/v1/admin/news            (auth required)
```

All public health data endpoints are scoped to a region. There is no unscoped `/alerts` endpoint that returns national data.

**Conventions:**
- Controllers are thin — validate input, delegate to services, return DTOs
- Request/response DTOs are separate from EF entities (no leaking DB models to the API surface)
- Pagination via `?page=1&pageSize=25` on all list endpoints, returning `X-Total-Count` header
- Filtering via query parameters: `?disease=chickenpox&dateFrom=2026-01-01`
- API versioning via `Asp.Versioning.Mvc` NuGet package

### Service Layer

Business logic lives in service classes, one per domain aggregate:

| Service | Responsibility |
|---------|---------------|
| `RegionService` | Region lookup, geographic search ("near me" by zip/coordinates) |
| `AlertService` | CRUD for health alerts, regional scoping, severity ranking |
| `TrendService` | Time-series aggregation of case counts, trend calculation |
| `ResourceService` | Local clinic/pharmacy CRUD, geocoding integration, proximity search |
| `PreventionService` | Prevention guides with cost tiers (free/insured/out-of-pocket) |
| `FactCheckService` | Fact-check pipeline orchestration (see Section 4) |
| `NewsService` | Health news item ingestion and management |

Services are registered as scoped dependencies in DI. Services depend on repositories (or DbContext directly), never on controllers or other HTTP concerns.

### Data Access Layer

**ORM:** Entity Framework Core 8 with code-first migrations.

**Pattern:** DbContext injected directly into services (no generic repository wrapper). EF Core's `DbSet<T>` already implements the repository and unit-of-work patterns. Custom query methods live as extension methods on `IQueryable<T>` for reuse.

```
src/backend/
├── Controllers/
│   ├── Public/
│   │   ├── RegionsController.cs
│   │   ├── AlertsController.cs
│   │   ├── TrendsController.cs
│   │   ├── ResourcesController.cs
│   │   └── PreventionController.cs
│   └── Admin/
│       ├── AdminAlertsController.cs
│       ├── AdminResourcesController.cs
│       ├── AdminPreventionController.cs
│       ├── AdminNewsController.cs
│       └── AdminFactChecksController.cs
├── Services/
│   ├── RegionService.cs
│   ├── AlertService.cs
│   ├── TrendService.cs
│   ├── ResourceService.cs
│   ├── PreventionService.cs
│   ├── FactCheckService.cs
│   └── NewsService.cs
├── Models/
│   ├── Entities/          # EF Core entities
│   ├── DTOs/              # Request/response DTOs
│   └── Enums/
├── Data/
│   ├── AppDbContext.cs
│   ├── Migrations/
│   └── QueryExtensions/   # IQueryable extension methods
├── Auth/
│   └── ...
└── Program.cs
```

### Authentication & Authorization

**Provider:** ASP.NET Core Identity with JWT bearer tokens.

- Admin users are stored in the Identity database tables
- Login endpoint issues a short-lived JWT (15 min) + refresh token (7 days)
- Admin endpoints require `[Authorize(Roles = "Admin")]`
- Public endpoints are anonymous — no auth required to read health data
- Phase 2: API keys for mobile clients (issued per-device, rate-limited separately)

**Why JWT over cookies:** The frontend is a separate SPA origin, and JWT avoids CSRF complexity. Tokens are stored in memory (not localStorage) on the frontend and refreshed silently.

### Database

**Choice: PostgreSQL 16**

Rationale:
- Native full-text search (used for searching alerts and resources by keyword) — avoids adding Elasticsearch in Phase 1
- PostGIS extension available for geographic queries in Phase 2 if needed
- `jsonb` columns for semi-structured data (e.g., cost tiers on prevention guides that vary by provider)
- Strong EF Core support via Npgsql provider
- Free, open source, widely deployed

---

## 3. Database Schema

```
┌──────────────┐       ┌──────────────────┐       ┌──────────────────┐
│   Region     │       │   HealthAlert    │       │  DiseaseTrend    │
├──────────────┤       ├──────────────────┤       ├──────────────────┤
│ Id (PK)      │◄──┐   │ Id (PK)          │       │ Id (PK)          │
│ Name         │   ├───│ RegionId (FK)    │   ┌───│ AlertId (FK)     │
│ Type (enum)  │   │   │ Disease          │   │   │ Date             │
│ State        │   │   │ Title            │───┘   │ CaseCount        │
│ Latitude     │   │   │ Summary          │       │ Source           │
│ Longitude    │   │   │ Severity (enum)  │       │ SourceDate       │
│ ParentId(FK) │   │   │ CaseCount        │       │ Notes            │
└──────────────┘   │   │ SourceAttribution│       └──────────────────┘
                   │   │ SourceDate       │
                   │   │ Status (enum)    │
                   │   │ CreatedAt        │
                   │   │ UpdatedAt        │
                   │   └──────────────────┘
                   │
                   │   ┌──────────────────┐       ┌──────────────────┐
                   │   │ PreventionGuide  │       │   CostTier       │
                   │   ├──────────────────┤       ├──────────────────┤
                   ├───│ RegionId (FK)    │       │ Id (PK)          │
                   │   │ Id (PK)          │◄──────│ GuideId (FK)     │
                   │   │ Disease          │       │ Type (enum)      │
                   │   │ Title            │       │ Price            │
                   │   │ Content (text)   │       │ Provider         │
                   │   │ CreatedAt        │       │ Notes            │
                   │   └──────────────────┘       └──────────────────┘
                   │
                   │   ┌──────────────────┐
                   │   │  LocalResource   │
                   │   ├──────────────────┤
                   ├───│ RegionId (FK)    │
                   │   │ Id (PK)          │
                   │   │ Name             │
                   │   │ Type (enum)      │  Clinic | Pharmacy | VaccinationSite
                   │   │ Address          │
                   │   │ Phone            │
                   │   │ Website          │
                   │   │ Latitude         │
                   │   │ Longitude        │
                   │   │ Hours (jsonb)    │
                   │   │ Services (jsonb) │
                   │   └──────────────────┘
                   │
                   │   ┌──────────────────┐       ┌──────────────────┐
                   │   │   NewsItem       │       │   FactCheck      │
                   │   ├──────────────────┤       ├──────────────────┤
                   └───│ RegionId (FK)    │       │ Id (PK)          │
                       │ Id (PK)          │◄──────│ NewsItemId (FK)  │
                       │ Headline         │       │ Status (enum)    │
                       │ Content          │       │ Verdict          │
                       │ SourceUrl        │       │ Sources (jsonb)  │
                       │ PublishedAt      │       │ CheckedAt        │
                       │ CreatedAt        │       │ CheckedBy        │
                       └──────────────────┘       └──────────────────┘
```

### Enums

| Enum | Values |
|------|--------|
| `RegionType` | `Zip`, `County`, `Metro`, `State` |
| `AlertSeverity` | `Low`, `Moderate`, `High`, `Critical` |
| `AlertStatus` | `Draft`, `Published`, `Archived` |
| `ResourceType` | `Clinic`, `Pharmacy`, `VaccinationSite`, `Hospital` |
| `CostTierType` | `Free`, `Insured`, `OutOfPocket`, `Promotional` |
| `FactCheckStatus` | `Pending`, `Verified`, `Disputed`, `Unverified` |

### Key Indexes

```sql
-- Core query patterns: "alerts in my region, most recent first"
CREATE INDEX IX_HealthAlert_RegionId_CreatedAt ON HealthAlerts (RegionId, CreatedAt DESC);
CREATE INDEX IX_DiseaseTrend_AlertId_Date ON DiseaseTrends (AlertId, Date DESC);
CREATE INDEX IX_LocalResource_RegionId_Type ON LocalResources (RegionId, Type);
CREATE INDEX IX_NewsItem_RegionId_PublishedAt ON NewsItems (RegionId, PublishedAt DESC);

-- Full-text search on alerts and resources
CREATE INDEX IX_HealthAlert_FTS ON HealthAlerts USING GIN (to_tsvector('english', Title || ' ' || Summary));
CREATE INDEX IX_LocalResource_FTS ON LocalResources USING GIN (to_tsvector('english', Name || ' ' || Address));
```

### Region Hierarchy

Regions form a tree: State → County → Zip/Metro. The `ParentId` self-referential FK enables:
- A user selecting "Travis County" sees all alerts for Travis County AND its child zip codes
- State-level rollup views that aggregate child region data
- Admin can publish an alert at any level in the hierarchy

---

## 4. Fact-Check Pipeline

### Workflow

```
┌─────────────┐     ┌─────────────┐     ┌───────────────┐     ┌─────────────┐
│ News Item   │────▶│  Claim      │────▶│  Source        │────▶│  Verdict    │
│ Submitted   │     │  Extraction │     │  Matching      │     │  Assignment │
│ (Admin)     │     │             │     │                │     │             │
│ Status:     │     │ Identify    │     │ Search trusted │     │ Verified /  │
│ Pending     │     │ checkable   │     │ source registry│     │ Disputed /  │
│             │     │ claims      │     │ for supporting │     │ Unverified  │
│             │     │             │     │ or refuting    │     │             │
└─────────────┘     └─────────────┘     │ evidence       │     └──────┬──────┘
                                        └───────────────┘            │
                                                                     ▼
                                                              ┌─────────────┐
                                                              │  Published  │
                                                              │  with       │
                                                              │  sources &  │
                                                              │  verdict    │
                                                              └─────────────┘
```

### Phase 1 Implementation (Admin-Driven)

In Phase 1, fact-checking is a manual editorial workflow, not an automated AI pipeline:

1. **Submission**: Admin enters a health news item with its source URL and headline
2. **Claim extraction**: Admin identifies the key health claims in the item (e.g., "flu cases up 40% in Dallas County")
3. **Source matching**: Admin searches the trusted source registry and attaches supporting or refuting evidence from official sources
4. **Verdict**: Admin assigns a `FactCheckStatus` (Verified, Disputed, Unverified) and writes a brief verdict summary
5. **Publication**: The news item is published with its fact-check status, verdict, and linked sources visible to public users

### Trusted Source Registry

A curated list of authoritative health data sources, stored in the database:

| Source | Type | URL Pattern |
|--------|------|-------------|
| CDC | Federal | cdc.gov |
| WHO | International | who.int |
| State health departments | State | varies by state |
| County health departments | County | varies by county |
| FDA | Federal | fda.gov |
| NIH / PubMed | Research | pubmed.ncbi.nlm.nih.gov |

Each fact-check record stores its sources as a `jsonb` array with URL, title, access date, and relevance note.

### Phase 2 Enhancement

Automated assistance: integrate an LLM to suggest matching sources for claims, with admin review before publication. The manual workflow remains the authority — automation assists, doesn't replace.

---

## 5. Frontend Architecture (React + TypeScript)

### Build Tooling

**Vite** with TypeScript strict mode. No Create React App.

### Routing

React Router v6 with region-scoped URL structure:

```
/                                   → Landing / region selector
/region/:regionId                   → Regional dashboard
/region/:regionId/alerts            → Alert list
/region/:regionId/alerts/:alertId   → Alert detail + trend chart
/region/:regionId/prevention        → Prevention guides
/region/:regionId/resources         → Local clinics & pharmacies
/region/:regionId/news              → Health news + fact-check status
/admin                              → Admin login
/admin/dashboard                    → Admin dashboard
/admin/alerts                       → Manage alerts
/admin/resources                    → Manage resources
/admin/prevention                   → Manage prevention guides
/admin/news                         → Manage news + fact-checks
```

### State Management

**TanStack Query (React Query)** for server state — API data fetching, caching, and synchronization. No Redux.

- Region context stored in a React context provider (set by URL param and region selector)
- TanStack Query caches are keyed by `[entity, regionId, filters]`
- Stale time: 5 minutes for public data (health alerts don't change second-by-second)
- Admin mutations invalidate related query caches on success

**Why not Redux:** The app is read-heavy with server-driven data. TanStack Query handles caching, loading states, and revalidation out of the box. The only client-side state is the selected region and UI state (modals, form drafts), which React context and local state handle fine.

### API Client

A typed client layer in `src/frontend/src/api/`:

```
api/
├── client.ts          # Axios instance with base URL, auth interceptor, error handling
├── types.ts           # TypeScript interfaces matching backend DTOs
├── regions.ts         # getRegions(), getRegionById(), searchRegions()
├── alerts.ts          # getAlerts(), getAlertById()
├── trends.ts          # getTrends()
├── resources.ts       # getResources(), searchNearby()
├── prevention.ts      # getPreventionGuides()
├── news.ts            # getNews(), getFactCheck()
└── admin.ts           # Admin CRUD operations (auth required)
```

Axios interceptor attaches the JWT from memory on each request. On 401 response, the interceptor attempts a silent token refresh; on failure, redirects to login.

### Component Architecture

```
src/frontend/src/
├── components/
│   ├── layout/            # AppShell, Header, Footer, Sidebar
│   ├── region/            # RegionSelector, RegionContext
│   ├── alerts/            # AlertCard, AlertList, AlertDetail, SeverityBadge
│   ├── trends/            # TrendChart (line chart), CaseCountTable
│   ├── prevention/        # PreventionCard, CostTierBadge
│   ├── resources/         # ResourceCard, ResourceList, ResourceMap
│   ├── news/              # NewsCard, FactCheckBadge, SourceList
│   └── shared/            # Pagination, LoadingSpinner, ErrorBoundary, EmptyState
├── pages/                 # Route-level page components (compose from above)
├── admin/                 # Admin-specific pages and components
├── hooks/                 # Custom hooks (useRegion, useAuth, etc.)
├── api/                   # API client (see above)
└── utils/                 # Date formatting, geographic helpers
```

### Charting

**Recharts** for trend line charts and case count visualizations. Lightweight, React-native, no D3 dependency overhead.

---

## 6. Infrastructure & DevOps

### Local Development

```yaml
# docker-compose.yml
services:
  db:
    image: postgres:16
    ports: ["5432:5432"]
    environment:
      POSTGRES_DB: snifflereport
      POSTGRES_USER: sniffle
      POSTGRES_PASSWORD: localdev
    volumes:
      - pgdata:/var/lib/postgresql/data

  api:
    build: ./src/backend
    ports: ["5000:5000"]
    environment:
      ConnectionStrings__Default: "Host=db;Database=snifflereport;Username=sniffle;Password=localdev"
      ASPNETCORE_ENVIRONMENT: Development
    depends_on: [db]

  frontend:
    build: ./src/frontend
    ports: ["5173:5173"]
    environment:
      VITE_API_URL: http://localhost:5000/api/v1
    depends_on: [api]

volumes:
  pgdata:
```

### Environment Configuration

| Layer | Mechanism | Secrets |
|-------|-----------|---------|
| Backend | `appsettings.json` + `appsettings.{Environment}.json` | Connection strings, JWT signing key via environment variables or user-secrets in dev |
| Frontend | `.env` files (VITE_-prefixed) | No secrets — only the API base URL |
| Docker | `docker-compose.override.yml` for local overrides | Local-only passwords, never committed |

### CI/CD Pipeline

GitHub Actions with two workflows:

**ci.yml** (on push/PR to main):
1. Backend: `dotnet restore` → `dotnet build` → `dotnet test`
2. Frontend: `npm ci` → `npm run lint` → `npm run build` → `npm test`
3. Both must pass for PR merge

**deploy.yml** (on merge to main, Phase 2):
1. Build Docker images
2. Push to container registry
3. Deploy to hosting environment

### Logging & Monitoring

- **Structured logging**: Serilog with JSON output, writing to console (Docker captures stdout)
- **Log levels**: `Information` for request/response summaries, `Warning` for validation failures, `Error` for exceptions
- **Health check endpoint**: `/health` returns API and database connectivity status
- **Phase 2**: Add Application Insights or Seq for log aggregation, plus uptime monitoring

---

## 7. Security Architecture

All code in this project follows mandatory secure coding standards:

- **React frontend**: Follows the [Secure-by-Default React 19 Expert](../Manicode.ai/prompts/code%20security/Client%20Side%20Frameworks/ReactJS/00%20React19%20Secure%20Generator%20(JS).md) prompt
- **ASP.NET Core backend**: Follows the [Secure C# ASP.NET Core API Developer](../Manicode.ai/prompts/code%20security/Backend%20Frameworks/DotNet/01%20Secure%20C%23%20ASP.NET%20Core%20API%20Developer.md) prompt

### OWASP Top 10 Mapping

| OWASP Category | Mitigation |
|----------------|-----------|
| A01 Broken Access Control | Fallback authorization policy: default-deny all endpoints. `[Authorize(Policy = "...")]` on all admin endpoints. Public endpoints explicitly marked `[AllowAnonymous]`. Prevent IDOR: filter at query level (`Where(o => o.Id == id && o.UserId == userId)`). Return 404 for inaccessible resources. |
| A02 Cryptographic Failures | HTTPS enforced via HSTS; OAuth2/OIDC token validation with pinned algorithms (`ValidAlgorithms` set to expected signing algorithm), `ClockSkew = TimeSpan.Zero`; passwords hashed with ASP.NET Identity (PBKDF2 default). Refresh token rotation with reuse detection. |
| A03 Injection | EF Core parameterized queries exclusively; no raw SQL. React JSX auto-escapes output. Never use `dangerouslySetInnerHTML` — use safe rendering libraries (e.g., react-markdown). Zod schema validation on all API responses before rendering in the frontend. |
| A04 Insecure Design | Region-scoped data model prevents accidental data leakage across regions; admin and public APIs are separate controller groups. Never bind to entity/domain models — always use dedicated DTOs. Never return entity objects — project to DTOs with `Select()`. |
| A05 Security Misconfiguration | CORS with `WithOrigins()` specific origins only — never `SetIsOriginAllowed(_ => true)`. Development secrets via user-secrets (not appsettings). Security headers: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy`. Remove server headers: `AddServerHeader = false`. |
| A06 Vulnerable Components | Dependabot enabled; `dotnet list package --vulnerable --include-transitive` in CI; `npm audit` in CI. Minimize frontend dependencies — prefer native platform or React features first. |
| A07 Auth Failures | Short-lived JWTs (15 min); refresh token rotation with reuse detection; account lockout after 5 failed attempts. Stricter rate limits on auth endpoints. |
| A08 Data Integrity Failures | Fact-check sources include URL, access date, and admin attribution — audit trail for editorial decisions. `System.Text.Json` exclusively — never `BinaryFormatter`, `NetDataContractSerializer`, or `Newtonsoft.Json` with `TypeNameHandling`. |
| A09 Logging Failures | Serilog structured logging with PII redaction; never string-interpolate log templates. Auth events (login, failed login, token refresh) logged at Information level. |
| A10 SSRF | No user-supplied URLs fetched server-side in Phase 1; Phase 2 RSS ingestion will use an allowlist of domains. |

### Frontend Security Controls (React 19)

- **All data is untrusted by default** — validate and sanitize every boundary (inputs, props, API responses)
- **Zod schema validation** on all API responses before rendering
- **URL protocol allowlist** via `validateAndSanitizeUrl` on all `href`, `src`, `formAction`, `action` attributes — only `https:`, `mailto:`, `tel:` allowed
- **No `dangerouslySetInnerHTML`** — use react-markdown or equivalent safe rendering libraries
- **CSP-compatible code**: no inline event handlers, no `data:` URIs in sinks, style via CSS Modules or atomic key-value pairs
- **Explicit prop destructuring** — no `{...userProps}` on DOM elements; manually map validated attributes only
- **Secure React keys** — never use attacker-controlled data as `key` props; use `crypto.randomUUID()` or server-provided stable IDs
- **Race condition protection** — every async `useEffect` implements cleanup/ignore flag for stale data
- **postMessage security** — verify `event.origin` and schema-validate `event.data` before acting
- **React Router v7+** — gate all navigations through `validateAndSanitizeUrl`; map IDs to routes (no free-form paths)
- **Dynamic CSS** — never inject raw CSS from users; use `CSS.escape()` for selectors

### Backend Security Controls (ASP.NET Core)

- **Fallback deny-all authorization policy**: `FallbackPolicy = RequireAuthenticatedUser().Build()`
- **Explicit binding sources**: `[FromBody]`, `[FromQuery]`, `[FromRoute]`, `[FromHeader]` on all parameters
- **FluentValidation** on all request DTOs; validate route, query, and header parameters
- **Rate limiting** on all endpoints via `[EnableRateLimiting]` with `Retry-After` on 429 responses
- **`ProblemDetails` for all errors** — never expose `ex.Message` or `ex.StackTrace` to clients
- **`Cache-Control: no-store`** on all authenticated responses; never cache authenticated endpoint responses
- **API keys**: store only SHA-256 hashes; present raw key once at creation; require in `Authorization` header or `X-API-Key`, never in query strings
- **Request size limits**: `KestrelServerOptions.Limits.MaxRequestBodySize` with per-action `[RequestSizeLimit]`
- **Pagination**: enforce max page size with `Math.Clamp()`; allow-list sort/filter fields
- **Secrets**: load from vault services via `IOptions<T>`, never from source code

### Additional Security Controls

- **Rate limiting**: ASP.NET Core rate limiting middleware — 100 req/min per IP on public endpoints, stricter on auth endpoints
- **Input validation**: FluentValidation on all request DTOs; max lengths, allowed characters, enum range checks
- **Content Security Policy**: CSP header restricting script sources to self; all frontend code CSP-compatible
- **HTTPS**: HSTS with 1-year max-age; HTTP→HTTPS redirect
- **Health checks**: separate liveness (unauthenticated, no details) from readiness (authenticated, dependency status) probes

---

## 8. Phase 2 Readiness

### Mobile Strategy

Phase 1 builds with responsive-first CSS (Tailwind CSS utility classes). Phase 2 options:

| Option | Pros | Cons |
|--------|------|------|
| **PWA** (recommended) | Single codebase; installable; offline capable; push notifications | No app store presence; limited native API access |
| React Native | Native performance; app store presence | Second codebase; shared logic needs extraction |

Recommendation: **PWA** — the app is read-heavy informational content, not a native-feature-dependent experience. A service worker can cache the regional dashboard for offline access.

### Scalability

| Concern | Phase 1 (sufficient) | Phase 2 (scale) |
|---------|---------------------|-----------------|
| API caching | HTTP cache headers (Cache-Control, ETag) | Redis distributed cache for hot queries |
| Database reads | Single PostgreSQL instance with proper indexing | Read replica for public endpoints |
| Static assets | Vite production build | CDN (CloudFront / Cloudflare) |
| API rate limiting | In-process rate limiter | Distributed rate limiter (Redis-backed) |
| Search | PostgreSQL full-text search | Elasticsearch if FTS performance degrades |
| Background jobs | N/A | Hangfire or .NET BackgroundService for RSS ingestion, automated fact-check suggestions |

### Data Ingestion (Phase 2)

- RSS feed ingestion from health department news feeds
- Scheduled jobs to pull case count data from CDC/state APIs
- Admin review queue for ingested items before publication
