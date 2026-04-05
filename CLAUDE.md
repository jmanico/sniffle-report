# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Sniffle Report is a **static community health site** that surfaces regional health trends — communicable disease outbreaks, case breakdowns, prevention guidance, cost/access info for vaccines, and local clinic/pharmacy resources. Data is sourced from 12 public feeds (CDC, FDA, CMS) and published as flat HTML/JSON with no live server.

## Architecture

**Model:** Local data pipeline + static site export. No live server, no database, no auth in production.

```
sniffle-report/
├── src/
│   ├── frontend/          # React app (Vite + TypeScript) — builds to static HTML/JS
│   └── backend/           # ASP.NET Core API (local only — data pipeline + export)
├── tests/
│   ├── frontend/          # Vitest tests
│   └── backend/           # xUnit tests (92 tests)
├── static-site/           # Generated output (git-ignored) — deploy this folder
├── static-export/         # Generated JSON data (git-ignored)
├── export.sh              # Build script: sync → export → build ��� assemble
├── docker-compose.yml     # Local dev orchestration
├── ARCHITECTURE.md        # Full architecture documentation
└── CLAUDE.md              # This file
```

### Local Backend (Never Published)

- .NET 8 API running in Docker with PostgreSQL — local only
- 12 feed connectors (CDC Socrata, CDC/FDA RSS, CMS NPI Registry, openFDA)
- Background services: feed polling (60s check) + snapshot builder (post-sync rebuild)
- `RegionSnapshotBuilder`: precomputes dashboards for all 3,205 regions
- `StaticSiteExporter`: dumps snapshots to ~3,265 JSON files
- Admin endpoints for content management (local use only — no auth needed)

### Published Frontend (Static Site)

- React SPA reading from `/data/*.json` files — no API calls
- Browse-based navigation: Home (state grid) → State (county table) → County (dashboard)
- No search, no authentication, no dynamic behavior
- Total output: ~3,282 files, ~14 MB

### Key Domain Concepts

- **Region**: Geographic area (state or county) that scopes all health data (3,205 regions)
- **RegionSnapshot**: Precomputed dashboard per region with JSONB columns for alerts, trends, resources, news
- **Health Alert**: Disease surveillance data from CDC/FDA feeds
- **Local Resource**: Clinics, pharmacies, hospitals from NPI Registry (24K+ providers)
- **News Item**: Health news, food/drug recalls from RSS feeds with fact-check status

## Build & Run Commands

```bash
# Full static site export (the main workflow)
./export.sh                          # sync feeds → export JSON → build frontend → assemble

# Preview static site
npx serve static-site

# Backend (local data pipeline)
docker-compose up                    # start PostgreSQL + API + frontend
dotnet build src/backend/
dotnet test tests/backend/           # 92 tests

# Frontend
cd src/frontend && npm install
cd src/frontend && npm run build     # production build
cd src/frontend && npm test          # 21 tests

# Trigger manual export
curl -X POST http://localhost:5001/api/v1/export/static
```

## Data Sources (12 Active Feeds)

| Source | Type | Data |
|--------|------|------|
| CDC NNDSS Weekly Tables | Socrata | 50+ diseases, state-level |
| CDC Wastewater Surveillance | Socrata | COVID wastewater, county-level |
| CDC PLACES County Health | Socrata | 29 chronic disease measures |
| CDC Drug Overdose Deaths | Socrata | Provisional overdose counts |
| CDC Food Safety / Outbreak Alerts | RSS | Food safety + outbreak news |
| FDA Drug / Food Recalls | RSS | Recall alerts |
| NPI Registry (CMS) | REST | 24K+ pharmacies, clinics, hospitals |
| openFDA Drug Enforcement | REST | Drug recall actions |

## Security Model

**Published site has zero attack surface** — no server, no database, no API, no auth, no user input processing. All files are pre-generated and immutable.

**Local pipeline** runs only on operator's machine with standard dev security (local-only PostgreSQL, no inbound connections).

## Development Guidelines

- All health data is region-scoped — organized by state → county hierarchy
- The published site reads ONLY from static JSON files at `/data/*.json`
- No live API calls in shipped frontend code (verified by tree-shaking audit)
- PLACES community health data prefixed with `[Community Health]` and excluded from top alerts
- NPI resources mapped to counties via ZIP-to-county crosswalk (33K entries)
- Feed connectors handle deduplication, multi-county mapping, fuzzy name matching
- Source attribution required on all health data
- Fact-check status carried through from feed sources
