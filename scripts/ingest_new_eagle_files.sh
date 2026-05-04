#!/bin/bash
# Upload the new files added to the Eagle GI data folder via the running API.
# Existing 18 docs will dedupe by SHA256 (the DocumentAsset unique index per
# tenant). Only the genuinely new ones land.
#
# After ingestion, pin the candidate's facility (handled separately) and run
# /api/analysis/llm-extract-batch to refresh canonical headlines.

set -e

CONN_ID="4517ad99-e88e-47de-aea5-ce60860c77e6"
DATA_DIR="C:/HareKrishna/Raghu/PracticeX/PracticeX.App/data/Eagle GI/Eagle GI"
API="https://localhost:7100"

cd "$DATA_DIR"

NEW_FILES=(
  "2023 Due Diligence Questions _ Eagle Gastroenterology.xlsx"
  "Addendum to Eagle GI Shareholder Employment Agreement FINAL 2-21-06 (1).doc"
  "CRNA vacation 2024.xlsx"
  "Eagle Physicians Rent Roll 11 01 25.xlsx"
  "Endoscopy Net Book Value as of 4-30-2023.xlsx"
  "FieldsResearch_Signed_HIPAA_Agreement_34C0001117.pdf"
  "License tracker 2026.xlsx"
  "Schedule 2026.xlsx"
  "signed contract 7.3.24.pdf"
  "Steris 7.2025 signed.pdf"
)

CURL_FILES=()
for f in "${NEW_FILES[@]}"; do
  if [[ -f "$f" ]]; then
    CURL_FILES+=(-F "files=@${f}")
    echo "  + $f"
  else
    echo "  ! missing: $f"
  fi
done

echo
echo "Uploading ${#CURL_FILES[@]} file parts to /folder/scan..."
curl -s -k -X POST "$API/api/sources/connections/$CONN_ID/folder/scan" \
  "${CURL_FILES[@]}" \
  -F "notes=eagle-gi-new-files-2026-05-04" \
  | python -m json.tool
