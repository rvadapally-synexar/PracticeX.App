#!/bin/bash
# Build + deploy the command-center to Cloudflare Pages.
#
# CRITICAL: must run with cwd = apps/command-center so wrangler picks up
# functions/api/[[path]].ts and bundles it as a Pages Function. Running
# from the project root (or any other cwd) silently deploys the static
# dist/ without the function — every /api/* request then falls through
# to index.html and the React app dies with
# "Unexpected token '<', '<!doctype'" on every fetch().
# This script enforces the correct cwd so you can't reproduce that bug.

set -e
cd "$(dirname "$0")/../apps/command-center"

echo "=== Building frontend bundle ==="
npm run build

echo
echo "=== Verifying functions/api/ exists ==="
if [ ! -f "functions/api/[[path]].ts" ]; then
  echo "FATAL: functions/api/[[path]].ts not found. Wrangler will deploy"
  echo "without the /api/* proxy and the app will break. Aborting."
  exit 1
fi
ls -la "functions/api/"

echo
echo "=== Deploying to Cloudflare Pages ==="
npx wrangler pages deploy dist \
  --project-name practicex-app \
  --branch main \
  --commit-dirty=true

echo
echo "=== Smoke-testing the Function ==="
sleep 3
PROD_HOST="app.practicex.ai"
RESP=$(curl -s -o /dev/null -w "%{http_code}" "https://${PROD_HOST}/api/system/info")
if [ "$RESP" = "302" ]; then
  echo "OK: /api/system/info → 302 (Cloudflare Access redirect for unauth caller — expected)"
elif [ "$RESP" = "200" ]; then
  echo "OK: /api/system/info → 200"
else
  echo "WARN: /api/system/info → ${RESP} (unexpected — investigate)"
fi
