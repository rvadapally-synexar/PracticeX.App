-- Slice 12: store the extracted fullText alongside layout_json so the UI can
-- show a "what we read" snippet for digital docs (DOCX, digital PDFs) that
-- never went through Doc Intelligence. Idempotent.

ALTER TABLE doc.document_assets
  ADD COLUMN IF NOT EXISTS extracted_full_text text;
