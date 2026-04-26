import { Button } from '@practicex/design-system';
import { FolderUp, Upload, X } from 'lucide-react';
import { useCallback, useRef, useState } from 'react';
import { readFileList, walkDataTransfer, type QueuedFile } from '../../lib/folder-traverse';

export function FolderUploadModal({
  onClose,
  onUpload,
}: {
  onClose: () => void;
  onUpload: (files: QueuedFile[], notes?: string) => Promise<void>;
}) {
  const [files, setFiles] = useState<QueuedFile[]>([]);
  const [notes, setNotes] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const folderInputRef = useRef<HTMLInputElement>(null);

  const addFiles = useCallback((fileList: FileList | null) => {
    const next = readFileList(fileList);
    if (next.length === 0) return;
    setFiles((prev) => [...prev, ...next]);
  }, []);

  const handleDrop = useCallback(
    (event: React.DragEvent<HTMLDivElement>) => {
      event.preventDefault();
      void walkDataTransfer(event.dataTransfer).then((collected) => {
        if (collected.length > 0) {
          setFiles((prev) => [...prev, ...collected]);
        }
      });
    },
    [],
  );

  const handleSubmit = useCallback(async () => {
    if (files.length === 0) return;
    setBusy(true);
    setError(null);
    try {
      await onUpload(files, notes.trim() || undefined);
    } catch (err) {
      const detail =
        (err as { detail?: string; title?: string }).detail ??
        (err as { title?: string }).title ??
        'Upload failed.';
      setError(detail);
    } finally {
      setBusy(false);
    }
  }, [files, notes, onUpload]);

  return (
    <div
      role="dialog"
      aria-modal="true"
      style={{
        alignItems: 'center',
        background: 'rgba(30, 42, 26, 0.42)',
        bottom: 0,
        display: 'flex',
        justifyContent: 'center',
        left: 0,
        position: 'fixed',
        right: 0,
        top: 0,
        zIndex: 100,
      }}
    >
      <div
        style={{
          background: 'var(--px-surface)',
          border: '1px solid var(--px-line)',
          borderRadius: 'var(--px-radius-lg)',
          maxHeight: '78vh',
          overflowY: 'auto',
          padding: 22,
          width: 640,
        }}
      >
        <header style={{ alignItems: 'center', display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
          <div>
            <div className="mono-label">Source discovery</div>
            <h2 style={{ fontFamily: 'var(--px-serif)', fontSize: 24, margin: '4px 0 0' }}>Upload folder or files</h2>
            <div className="muted" style={{ marginTop: 4 }}>
              Folder structure is preserved as folder hints. Hashes are computed for de-duplication.
            </div>
          </div>
          <button className="px-icon-button" type="button" onClick={onClose} aria-label="Close">
            <X size={14} />
          </button>
        </header>

        <div
          onDragOver={(event) => event.preventDefault()}
          onDrop={handleDrop}
          style={{
            alignItems: 'center',
            background: 'var(--px-surface-2)',
            border: '1px dashed var(--px-line)',
            borderRadius: 'var(--px-radius)',
            display: 'flex',
            flexDirection: 'column',
            gap: 6,
            padding: 22,
            textAlign: 'center',
          }}
        >
          <FolderUp size={22} />
          <strong>Drop a folder here</strong>
          <div className="muted">or pick from your machine</div>
          <div style={{ display: 'flex', gap: 8, marginTop: 10 }}>
            <Button variant="secondary" onClick={() => folderInputRef.current?.click()}>
              <FolderUp size={14} /> Pick folder
            </Button>
            <Button variant="secondary" onClick={() => fileInputRef.current?.click()}>
              <Upload size={14} /> Pick files
            </Button>
          </div>
          <input
            ref={folderInputRef}
            type="file"
            // The folder-picker attributes are not in standard React types yet —
            // both Chrome and Safari accept them via the lower-case spelling.
            {...({ webkitdirectory: '', directory: '' } as Record<string, string>)}
            multiple
            hidden
            onChange={(event) => addFiles(event.currentTarget.files)}
          />
          <input
            ref={fileInputRef}
            type="file"
            multiple
            hidden
            onChange={(event) => addFiles(event.currentTarget.files)}
          />
        </div>

        {files.length > 0 ? (
          <div style={{ marginTop: 18 }}>
            <div className="mono-label">{files.length} files queued</div>
            <div style={{ maxHeight: 200, overflowY: 'auto', marginTop: 8 }}>
              {files.map((entry, idx) => (
                <div
                  key={`${entry.relativePath}-${idx}`}
                  style={{
                    alignItems: 'center',
                    borderTop: '1px solid var(--px-line-2)',
                    display: 'flex',
                    fontSize: 12.5,
                    gap: 10,
                    padding: '8px 4px',
                  }}
                >
                  <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                    {entry.relativePath}
                  </span>
                  <span className="mono-label">{(entry.file.size / 1024).toFixed(1)} KB</span>
                  <button
                    className="px-button ghost"
                    type="button"
                    onClick={() => setFiles((prev) => prev.filter((_, i) => i !== idx))}
                  >
                    <X size={12} />
                  </button>
                </div>
              ))}
            </div>
          </div>
        ) : null}

        <div style={{ marginTop: 18 }}>
          <label className="mono-label" htmlFor="upload-notes">
            Notes (optional)
          </label>
          <textarea
            id="upload-notes"
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
            placeholder="Internal note for the audit log…"
            rows={2}
            style={{
              background: 'var(--px-surface-2)',
              border: '1px solid var(--px-line)',
              borderRadius: 'var(--px-radius)',
              fontFamily: 'var(--px-sans)',
              marginTop: 4,
              padding: 10,
              resize: 'vertical',
              width: '100%',
            }}
          />
        </div>

        {error ? (
          <div
            style={{
              background: 'var(--px-orange-soft)',
              borderRadius: 'var(--px-radius)',
              color: 'var(--px-orange)',
              fontSize: 12.5,
              marginTop: 14,
              padding: 10,
            }}
          >
            {error}
          </div>
        ) : null}

        <footer style={{ alignItems: 'center', display: 'flex', gap: 10, justifyContent: 'flex-end', marginTop: 18 }}>
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={files.length === 0 || busy}>
            {busy ? 'Uploading…' : `Upload & scan${files.length ? ` (${files.length})` : ''}`}
          </Button>
        </footer>
      </div>
    </div>
  );
}

