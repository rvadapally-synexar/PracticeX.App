// Upload modal — drag-drop, simulated AI extraction

function UploadModal({ onClose, onDone }) {
  const [stage, setStage] = useState('drop'); // drop | uploading | extracting | done
  const [files, setFiles] = useState([]);
  const [drag, setDrag] = useState(false);

  const addFiles = (list) => {
    const items = list.map((f, i) => ({
      id: `U${Date.now()}-${i}`,
      name: f.name || f,
      size: f.size ? `${Math.round(f.size/1024)} KB` : `${(Math.random()*2 + 0.5).toFixed(1)} MB`,
      progress: 0,
      phase: 'upload',
      category: guessCategory(f.name || f),
      facility: 'nor',
    }));
    setFiles(items);
    setStage('uploading');
  };

  const simFakeDrop = () => {
    addFiles([
      'Regence_BCBS_Amendment_2.pdf',
      'Olympus_Scope_Service_Renewal_2026.pdf',
      'Northside_Suite_310_Lease_Amendment.pdf',
      'Dr_Hale_Employment_Addendum.pdf',
    ]);
  };

  useEffect(() => {
    if (stage !== 'uploading') return;
    let t;
    const tick = () => {
      setFiles(prev => {
        let allDone = true;
        const next = prev.map(f => {
          if (f.phase === 'upload' && f.progress < 100) {
            allDone = false;
            return { ...f, progress: Math.min(100, f.progress + 14 + Math.random()*10) };
          }
          if (f.phase === 'upload' && f.progress >= 100) {
            allDone = false;
            return { ...f, phase: 'extracting', progress: 0 };
          }
          if (f.phase === 'extracting' && f.progress < 100) {
            allDone = false;
            return { ...f, progress: Math.min(100, f.progress + 9 + Math.random()*8) };
          }
          if (f.phase === 'extracting' && f.progress >= 100) {
            return { ...f, phase: 'done' };
          }
          return f;
        });
        if (allDone && next.every(f => f.phase === 'done')) {
          clearInterval(t);
          setTimeout(() => setStage('done'), 400);
        }
        return next;
      });
    };
    t = setInterval(tick, 280);
    return () => clearInterval(t);
  }, [stage]);

  return (
    <div className="modal-scrim" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <div className="modal-head">
          <div>
            <h2>Upload contracts</h2>
            <div style={{fontSize: 12, color: 'var(--ink-3)', marginTop: 2}}>
              {stage === 'drop' && 'PDFs, Word docs, images. We\'ll extract the key terms.'}
              {stage === 'uploading' && `${files.filter(f=>f.phase!=='done').length} processing · ${files.filter(f=>f.phase==='done').length} complete`}
              {stage === 'done' && `${files.length} contracts extracted and ready to review.`}
            </div>
          </div>
          <button className="icon-btn" onClick={onClose}><Icon name="x"/></button>
        </div>
        <div className="modal-body">
          {stage === 'drop' && (
            <div>
              <div
                className={`dropzone ${drag ? 'dragging' : ''}`}
                onDragOver={e => { e.preventDefault(); setDrag(true); }}
                onDragLeave={() => setDrag(false)}
                onDrop={e => { e.preventDefault(); setDrag(false); addFiles([...e.dataTransfer.files]); }}
              >
                <div style={{fontSize: 28, marginBottom: 8, color: 'var(--ink-3)'}}>
                  <Icon name="upload" size={28}/>
                </div>
                <div className="big">Drop contracts here</div>
                <div className="sub">or <a style={{color: 'var(--accent)', cursor: 'pointer'}} onClick={simFakeDrop}>browse files</a> · up to 50 at a time · max 100 MB each</div>
                <div className="types">
                  <Chip kind="ink">PDF</Chip>
                  <Chip kind="ink">DOCX</Chip>
                  <Chip kind="ink">PNG</Chip>
                  <Chip kind="ink">JPG scans</Chip>
                </div>
              </div>
              <div style={{marginTop: 20, display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12}}>
                {[
                  ['Auto-categorize', 'By payer, vendor, lease, employee, or processor.'],
                  ['Extract key fields', 'Dates, notice windows, counterparties, renewal terms.'],
                  ['Flag for review', 'Low-confidence fields are highlighted for human QA.'],
                ].map(([t,b]) => (
                  <div key={t} style={{padding: 12, border: '1px solid var(--line-2)', borderRadius: 'var(--radius)', background: 'var(--surface-2)'}}>
                    <div style={{fontWeight: 500, fontSize: 12.5, marginBottom: 4, display: 'flex', alignItems: 'center', gap: 6}}>
                      <Icon name="sparkle" size={12}/> {t}
                    </div>
                    <div style={{fontSize: 11.5, color: 'var(--ink-3)'}}>{b}</div>
                  </div>
                ))}
              </div>
              <div style={{display:'flex', justifyContent:'flex-end', marginTop: 18, gap: 8}}>
                <button className="btn" onClick={onClose}>Cancel</button>
                <button className="btn primary" onClick={simFakeDrop}>Demo with sample files</button>
              </div>
            </div>
          )}

          {(stage === 'uploading' || stage === 'done') && (
            <div>
              {files.map(f => (
                <div className="upload-item" key={f.id}>
                  <div style={{color: 'var(--ink-3)'}}><Icon name="doc"/></div>
                  <div>
                    <div className="name">{f.name}</div>
                    <div className="meta">
                      {f.size} ·{' '}
                      {f.phase === 'upload' && 'Uploading…'}
                      {f.phase === 'extracting' && <><Icon name="sparkle" size={10}/> Extracting fields…</>}
                      {f.phase === 'done' && <span style={{color: 'var(--ok)'}}>✓ Extracted · auto-categorized as {f.category}</span>}
                    </div>
                    {f.phase !== 'done' && (
                      <div className="bar">
                        <span style={{
                          width: `${f.progress}%`,
                          background: f.phase === 'extracting' ? 'var(--ink)' : 'var(--accent)'
                        }}/>
                      </div>
                    )}
                  </div>
                  <div style={{textAlign: 'right'}}>
                    {f.phase === 'done' ? (
                      <Chip kind="ok" dot>Ready</Chip>
                    ) : (
                      <div style={{fontFamily: 'var(--ff-mono)', fontSize: 11, color: 'var(--ink-3)'}}>
                        {Math.round(f.progress)}%
                      </div>
                    )}
                  </div>
                </div>
              ))}

              {stage === 'done' && (
                <div style={{marginTop: 16, padding: 14, background: 'var(--ok-soft)', borderRadius: 'var(--radius)', fontSize: 12.5}}>
                  <div style={{fontWeight: 500, marginBottom: 4}}>✓ All files processed</div>
                  <div style={{color: 'var(--ink-2)'}}>
                    Review the extracted fields for each contract. Fields with lower confidence are flagged.
                  </div>
                </div>
              )}

              <div style={{display:'flex', justifyContent:'flex-end', marginTop: 18, gap: 8}}>
                <button className="btn" onClick={onClose}>Close</button>
                {stage === 'done' && (
                  <button className="btn primary" onClick={onDone}>Review extractions</button>
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function guessCategory(name) {
  const n = (name || '').toLowerCase();
  if (n.includes('bcbs') || n.includes('aetna') || n.includes('cigna') || n.includes('uhc') || n.includes('payer') || n.includes('regence') || n.includes('united')) return 'payer';
  if (n.includes('lease') || n.includes('suite')) return 'lease';
  if (n.includes('employ') || n.includes('physician') || n.includes('dr_') || n.includes('hale') || n.includes('vasquez')) return 'employee';
  if (n.includes('waystar') || n.includes('stripe') || n.includes('clearinghouse') || n.includes('payment')) return 'processor';
  return 'vendor';
}

window.UploadModal = UploadModal;
