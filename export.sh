#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUTPUT_DIR="$SCRIPT_DIR/static-site"

echo "=== Sniffle Report Static Site Export ==="
echo ""

# 1. Ensure docker services are running
echo "[1/5] Starting docker services..."
cd "$SCRIPT_DIR"
docker-compose up -d
echo "  Waiting for API to be ready..."
for i in $(seq 1 30); do
  if curl -sf http://localhost:5001/ > /dev/null 2>&1; then
    echo "  API is ready."
    break
  fi
  sleep 2
done

# 2. Trigger static export from the running API
echo "[2/5] Exporting static data from database..."
EXPORT_RESULT=$(curl -sf -X POST http://localhost:5001/api/v1/export/static)
echo "  $EXPORT_RESULT"

# 3. Copy exported data from container to host
echo "[3/5] Copying exported data from container..."
rm -rf "$SCRIPT_DIR/static-export"
mkdir -p "$SCRIPT_DIR/static-export"
docker cp sniffle-report-api-1:/static-export/data "$SCRIPT_DIR/static-export/data"
FILE_COUNT=$(find "$SCRIPT_DIR/static-export/data" -name "*.json" | wc -l | tr -d ' ')
echo "  Copied $FILE_COUNT JSON files."

# 4. Build frontend
echo "[4/5] Building frontend..."
cd "$SCRIPT_DIR/src/frontend"
npm run build --silent

# 5. Assemble static site
echo "[5/5] Assembling static site..."
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"
cp -r dist/* "$OUTPUT_DIR/"
cp -r "$SCRIPT_DIR/static-export/data" "$OUTPUT_DIR/data"

TOTAL_SIZE=$(du -sh "$OUTPUT_DIR" | cut -f1)
echo ""
echo "=== Static site ready ==="
echo "  Output: $OUTPUT_DIR"
echo "  Size: $TOTAL_SIZE"
echo "  Files: $(find "$OUTPUT_DIR" -type f | wc -l | tr -d ' ')"
echo ""
echo "To preview: npx serve $OUTPUT_DIR"
echo "To deploy:  push $OUTPUT_DIR to GitHub Pages, Netlify, or S3"
