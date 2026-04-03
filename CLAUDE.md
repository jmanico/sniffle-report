# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Sniffle Report is a community health news site that surfaces regional health trends — communicable disease outbreaks, case breakdowns, prevention guidance, cost/access info for vaccines, and local clinic/pharmacy resources. Content is admin-driven and fact-checked.

## Architecture

**Stack:** React frontend + C#/.NET backend API (clean separation)

```
sniffle-report/
├── src/
│   ├── frontend/          # React app (Vite + TypeScript)
│   └── backend/           # ASP.NET Core Web API (C#)
├── tests/
│   ├── frontend/          # Jest/Vitest tests
│   └── backend/           # xUnit tests
└── docker-compose.yml     # Local dev orchestration
```

### Backend (ASP.NET Core)

- REST API with controllers for: regions, health alerts, disease trends, resources (clinics/pharmacies), admin content management, fact-checking
- Entity Framework Core for data access
- Regional data model: alerts and trends are scoped to geographic regions
- Admin endpoints behind authentication/authorization
- Fact-check pipeline: health news claims verified against trusted sources (CDC, WHO, state health departments)

### Frontend (React + TypeScript)

- Region selector drives all views — user picks their area, sees local data
- Key views: regional dashboard, disease trend detail, prevention/cost info, local resources map, admin panel
- API client layer talks to the .NET backend

### Key Domain Concepts

- **Region**: Geographic area (zip, county, or metro area) that scopes all health data
- **Health Alert**: Admin-published alert about a disease trend in a region (e.g., "Chickenpox: 47 cases in Travis County")
- **Prevention Guide**: What-to-do content with cost info (free, $20, grocery store clinic deals)
- **Local Resource**: Clinics, pharmacies, vaccination sites — geocoded to regions
- **Fact Check**: Verification status and source links attached to health news items

## Build & Run Commands

```bash
# Backend
dotnet restore src/backend/
dotnet build src/backend/
dotnet run --project src/backend/
dotnet test tests/backend/
dotnet test tests/backend/ --filter "FullyQualifiedName~ClassName.TestName"  # single test

# Frontend
cd src/frontend && npm install
cd src/frontend && npm run dev        # dev server
cd src/frontend && npm run build      # production build
cd src/frontend && npm test           # all tests
cd src/frontend && npm test -- --run TestFile.test.ts  # single test file

# Full stack (once docker-compose exists)
docker-compose up
```

## Secure Coding Standards

- **React frontend**: Zod validation on API responses, `validateAndSanitizeUrl` on all URLs, no `dangerouslySetInnerHTML`, CSP-compatible, explicit prop destructuring
- **ASP.NET Core backend**: Fallback deny-all authorization, dedicated DTOs (never bind/return entities), FluentValidation, rate limiting, ProblemDetails for errors, structured logging with PII redaction

See ARCHITECTURE.md Section 7 for the full security architecture and threat model (issue #4).

### Security Rules (Non-Negotiable)

- **Audit logging**: every admin write operation (create, update, delete, status change) must produce an `AuditLogEntry` in the same database transaction. No exceptions.
- **Soft deletes**: health content entities (alerts, news, fact-checks, prevention guides) use `IsDeleted`/`DeletedAt`/`DeletedBy` — never hard delete without SuperAdmin role
- **Refresh tokens in HttpOnly cookies** — never in JS memory, localStorage, or sessionStorage
- **MFA required** for all admin accounts (TOTP-based)
- **Two admin roles**: `Admin` (content CRUD) and `SuperAdmin` (account management, audit access, hard deletes)
- **ReDoS prevention**: `RegexOptions.NonBacktracking` on any regex evaluated against user input
- **Query limits**: trend endpoints enforce max date range (1 year); search endpoints enforce max query length (200 chars)
- **PII redaction**: admin emails, passwords, tokens, MFA codes never appear in logs. See ARCHITECTURE.md Section 3 for full PII classification.

## Development Guidelines

- All health data endpoints must be region-scoped — never return unfiltered national data as a default
- Admin/content-management endpoints require `[Authorize(Roles = "Admin")]`; public read endpoints are `[AllowAnonymous]`
- Admin routes return 404 (not 401/403) to unauthenticated users
- Fact-check status is a first-class field on health news items, not an afterthought
- Fact-check verdict changes require justification and produce a `FactCheckHistory` entry
- Disease case counts and trend data must include source attribution (which health department, date of data)
- Cost/access info for vaccines and preventive care must distinguish between free, insured, and out-of-pocket pricing
- Local resource data (clinics, pharmacies) needs geocoding for regional filtering
- Input validation on all API boundaries; output encoding on all rendered content (XSS prevention)
- Use parameterized queries / EF Core — no raw SQL string concatenation
- CORS configured explicitly for the frontend origin only
- CI uses `npm ci` (not `npm install`) to enforce lockfile integrity

## Phase 2 Considerations

- Mobile-responsive design from the start (use responsive patterns in Phase 1)
- API pagination and caching headers for scalability
- Database indexing on region + date for trend queries
