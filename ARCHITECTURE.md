# Architecture — Sniffle Report

## 1. System Overview

Sniffle Report uses a **local data pipeline + static site export** architecture. All data processing, feed ingestion, and database operations run locally on the operator's machine. The only thing published to the internet is flat HTML/CSS/JS + precomputed JSON files — no live server, no database, no API, no authentication surface.

```
┌──────────────────────────────────────────────────────────────┐
���                    Local Machine (Operator)                    │
│                                                              │
│  ┌─────────────────┐  ┌──────────────┐  ┌────────────────┐  │
│  │  PostgreSQL 16  ���  │  .NET 8 API  │  │  Background    │  │
│  │  (local only)   │◄─│  (local only) │  │  Services      │  │
│  └─────────────────┘  └──────┬───────┘  │  - Feed Poller │  │
│                              │          │  - Snapshot     │  │
│                              │          │    Builder      │  │
│                              │          └────────────────┘  │
│                              │                               │
│  ┌───────────────────────────▼───────────────────────────┐  │
│  │              Static Site Exporter                      │  │
│  │  Reads snapshots → writes 3,265 JSON files (14 MB)    │  │
│  └───────────────────────────┬───────────────────────────┘  │
│                              │                               │
│  ┌───────────────────────────▼───────────────────────────┐  │
│  │              Vite Frontend Build                       │  │
│  │  React SPA reads from /data/*.json (no API calls)     │  │
│  └───────────────────────────┬───────────────────────────┘  │
│                              │                               │
│                    ┌─────────▼──────────┐                   │
│                    │  static-site/      │                   │
│                    │  3,282 files, 14MB │                   │
│                    └─────────┬──────────┘                   │
└──────────────────────────────┼───────────────────────────────┘
                               │ git push / rsync / deploy
                    ┌──────────▼──────────┐
                    │  Static Host        │
                    │  (GitHub Pages,     │
                    │   Netlify, S3, etc) │
                    │                     │
                    │  No server runtime  │
                    │  No database        │
                    │  No authentication  │
                    │  No API endpoints   │
                    └─────────────────────┘

External Data Sources (fetched locally):
  ├── CDC NNDSS Weekly Tables (Socrata)
  ├── CDC Wastewater Surveillance (Socrata)
  ├── CDC PLACES County Health (Socrata)
  ├── CDC Provisional Drug Overdose Deaths (Socrata)
  ├── CDC Food Safety Alerts (RSS)
  ├── CDC Outbreak Alerts (RSS)
  ├── FDA Drug Recalls (RSS)
  ├── FDA Food and Safety Recalls (RSS)
  ├── NPI Registry — Pharmacies (CMS REST API)
  ├── NPI Registry — Clinics (CMS REST API)
  ├── NPI Registry — Hospitals (CMS REST API)
  └── openFDA Drug Enforcement (REST API)
```

### User Flow (Published Static Site)

**Public user**: Browses state grid on homepage → selects state → browses county table → selects county → views regional dashboard (alerts, trends, prevention guides, resources, news) — all from precomputed JSON, no server interaction.

### Operator Flow (Local Machine)

**Operator**: Runs `docker-compose up` → feeds sync automatically → snapshots rebuild → runs `./export.sh` → static site assembled → pushes to hosting.

---

## 2. Local Data Pipeline

### Background Services

Two `BackgroundService` instances run inside the .NET API container:

| Service | Cadence | Purpose |
|---------|---------|---------|
| `FeedPollingBackgroundService` | 60s check interval | Polls 12 feed sources on their individual schedules. After all syncs complete, triggers snapshot rebuild if any data changed. |
| `RegionSnapshotBuilderBackgroundService` | 5 min (safety net) | Rebuilds all 3,205 region snapshots. Skips if no new `FeedSyncLog` entries since last build. |

### Feed Connectors

| Connector | Feed Sources | Output |
|-----------|-------------|--------|
| `CdcSocrataConnector` | NNDSS, Wastewater, PLACES, Overdose Deaths | HealthAlerts + DiseaseTrends |
| `CdcRssConnector` | CDC RSS, FDA RSS | NewsItems + FactChecks |
| `NpiRegistryConnector` | NPI Pharmacies, Clinics, Hospitals | LocalResources (24K+ providers) |
| `OpenFdaConnector` | Drug Enforcement | NewsItems |

### Ingestion Pipeline

```
Feed Source → Connector.FetchAsync() → NormalizedFeedRecord[]
  → IngestionService.ProcessRecordAsync()
    → Deduplicate (SHA-256 hash + external ID)
    → Map jurisdiction to Region (RegionMappingService)
    → Create/update HealthAlert, DiseaseTrend, NewsItem, or LocalResource
    → Evaluate alert thresholds (AlertThresholdService)
  → FeedPollingBackgroundService triggers RegionSnapshotBuilder.RebuildAllAsync()
```

### Region Snapshot System

The `RegionSnapshotBuilder` precomputes a denormalized dashboard for every region:

- Batch-loads all alerts, trends, news, prevention guides, and resources
- Builds full region hierarchy map (BFS traversal for parent→child rollup)
- For each of 3,205 regions: collects data from all descendant regions, computes top alerts, WoW trend changes, resource counts, prevention highlights, news highlights
- Upserts one `RegionSnapshot` row per region with JSONB columns
- Typical rebuild: ~400ms for all 3,205 regions

### Static Export

`StaticSiteExporter` reads from the database and writes JSON files:

| Output File | Content |
|-------------|---------|
| `data/states.json` | Index of all states with county counts and alert summaries |
| `data/states/{CODE}.json` | Per-state county list with snapshot summaries |
| `data/regions/{ID}.json` | Full dashboard snapshot per region (3,205 files) |
| `data/status.json` | Feed sync status and coverage summary |
| `data/news.json` | National-level news items |

Total: ~3,265 JSON files, ~14 MB.

---

## 3. Database Schema (Local Only)

The PostgreSQL database runs only on the operator's machine. It is never exposed to the internet.

### Core Entities

| Entity | Purpose | Count |
|--------|---------|-------|
| `Region` | Geographic hierarchy (State → County) | 3,205 |
| `RegionSnapshot` | Precomputed dashboard per region (JSONB) | 3,205 |
| `HealthAlert` | Disease surveillance alerts | ~16,000 |
| `DiseaseTrend` | Time-series case count data points | ~16,000 |
| `LocalResource` | Clinics, pharmacies, hospitals (from NPI) | ~24,000 |
| `NewsItem` | Health news, food/drug recalls | ~190 |
| `FactCheck` | Verification status on news items | ~190 |
| `PreventionGuide` | Prevention guidance with cost tiers | ~4 (seed) |
| `FeedSource` | Feed configuration (URL, interval, status) | 12 |
| `FeedSyncLog` | Historical sync results per feed | Growing |
| `IngestedRecord` | Deduplication tracking (SHA-256 hash) | ~35,000 |
| `AuditLogEntry` | All admin/system write operations | Growing |

### Embedded Data Files

| File | Content | Size |
|------|---------|------|
| `Data/SeedData/us-counties.json` | 3,143 US counties with FIPS codes and centroids | 301 KB |
| `Data/SeedData/zip-to-county.json` | 33,048 ZIP-to-county mappings for NPI geocoding | 945 KB |

---

## 4. Frontend Architecture (Static Site)

### Published Pages

The frontend is a React SPA that reads exclusively from static JSON files at `/data/*.json`. No live API calls, no authentication, no search backend.

| Route | Page | Data Source |
|-------|------|-------------|
| `/` | Homepage — state grid | `data/states.json` |
| `/states/:code` | State page — county table | `data/states/{code}.json` |
| `/region/:id` | County dashboard | `data/regions/{id}.json` |
| `/status` | System status | `data/status.json` + `data/states.json` |

### Data Layer

```
src/frontend/src/
├── api/
│   └── staticClient.ts     # fetchStaticJson() — reads from /data/*.json
├── hooks/
│   └── useStaticData.ts     # useStates(), useStateDetail(), useStaticDashboard(), etc.
├── pages/
│   ├── HomePage.tsx          # State grid (51 states)
│   ├── StateBrowsePage.tsx   # County table for a state
│   ├── RegionalDashboardPage.tsx  # Dashboard with alerts, trends, resources, news
│   ├── StatusPage.tsx        # Feed sync status + coverage
│   └── NotFoundPage.tsx
└── components/
    ├── dashboard/SeverityBadge.tsx
    └── news/FactCheckBadge.tsx
```

### What's NOT in the Published Bundle

The following exist in the source tree for local development but are **not shipped** in the static build (verified by tree-shaking audit):

- Axios HTTP client (`api/client.ts`)
- Live API hooks (`useAlerts`, `useTrends`, `useResources`, etc.)
- Authentication code (JWT tokens, refresh logic)
- Search components (`RegionSearchPanel`, `RegionSelector`)
- Admin panel pages
- AppShell / Header with region selector

---

## 5. Build & Deploy Pipeline

### Export Script (`export.sh`)

```bash
#!/bin/bash
# 1. Start local docker services (PostgreSQL + .NET API)
docker-compose up -d

# 2. Wait for API readiness
# 3. Trigger static export (POST /api/v1/export/static)
# 4. Copy JSON from container to host
# 5. Build frontend (Vite production build)
# 6. Assemble static-site/ directory (HTML + CSS + JS + JSON)
```

Output: `static-site/` directory ready for deployment.

### Deployment Options

| Host | Method | Cost |
|------|--------|------|
| GitHub Pages | `git push` to gh-pages branch | Free |
| Netlify | Drag-and-drop or git deploy | Free tier |
| AWS S3 + CloudFront | `aws s3 sync` | ~$1/month |
| Any static host | Upload `static-site/` folder | Varies |

### Update Cadence

Run `./export.sh` whenever you want fresh data. Typical workflow:
1. Leave `docker-compose up` running (feeds sync automatically)
2. Run `./export.sh` daily/weekly
3. Push `static-site/` to hosting

---

## 6. Security Model

### Published Site (Zero Attack Surface)

The published static site has **no server-side code, no database, no API, no authentication**:

- All files are pre-generated HTML/CSS/JS/JSON
- No user input is processed server-side
- No dynamic content generation
- No credentials stored or transmitted
- No CORS, no CSRF, no SSRF, no SQL injection — none of these apply
- The only "security" concern is ensuring the published data is accurate (handled by the trusted source pipeline)

### Local Pipeline (Operator's Machine Only)

The local data pipeline has a standard security posture appropriate for a development environment:

- PostgreSQL with local-only credentials (never exposed to internet)
- .NET API runs in Docker with no port forwarding to public networks
- Admin operations are local-only (no auth needed — it's your machine)
- Feed connectors make outbound HTTPS requests to trusted sources (CDC, FDA, CMS)
- No inbound connections from the internet

### Data Integrity

- Feed data sourced exclusively from official government APIs (CDC, FDA, CMS/NPI)
- Deduplication via SHA-256 hash prevents duplicate ingestion
- Audit log tracks all data mutations
- Snapshot rebuild is deterministic — same database state produces same JSON output
- Fact-check status carried through from feed source (CDC/FDA auto-verified)

---

## 7. Data Sources

| Source | Organization | Type | Data | Update Interval |
|--------|-------------|------|------|-----------------|
| NNDSS Weekly Tables | CDC | Socrata | State-level disease case counts (50+ diseases) | 24h |
| Wastewater Surveillance | CDC | Socrata | County-level COVID wastewater percentiles | 6h |
| PLACES County Health | CDC | Socrata | 29 chronic disease measures per county | Weekly |
| Drug Overdose Deaths | CDC | Socrata | State-level provisional overdose counts | Weekly |
| Food Safety Alerts | CDC | RSS | Food safety outbreak news | 12h |
| Outbreak Alerts | CDC | RSS | Disease outbreak news | 12h |
| Drug Recalls | FDA | RSS | Drug recall alerts | 12h |
| Food and Safety Recalls | FDA | RSS | Food/product recall alerts | 12h |
| NPI Pharmacies | CMS | REST | 9,300+ pharmacies nationwide | 30d |
| NPI Clinics | CMS | REST | 6,500+ urgent care clinics | 30d |
| NPI Hospitals | CMS | REST | 8,400+ hospitals | 30d |
| Drug Enforcement | openFDA | REST | Drug recall enforcement actions | 24h |

---

## 8. Region Coverage

- **51 states** (50 states + DC) with county data
- **3,143 counties** (all US counties from Census Bureau FIPS data)
- **5 metro areas** (Austin, Chicago, LA, Seattle, Atlanta)
- **3,205 total regions** with precomputed dashboard snapshots
- County-to-state hierarchy enables automatic rollup
- NPI resources mapped to counties via 33K-entry ZIP-to-county crosswalk
