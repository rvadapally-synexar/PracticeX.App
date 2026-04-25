// Post-upload Extraction Review — triage queue for QA of AI-extracted fields.
// Shows a batch of recently-uploaded contracts with low-confidence fields flagged.

function ExtractionReview({ state, setState }) {
  const batch = [
    { id: 'C-00212', name: 'Regence BlueShield — Amendment #3', category: 'payer', conf: 0.72, low: 3, med: 2, high: 12, uploaded: '2h ago', owner: null, facility: 'nor' },
    { id: 'V-00451', name: 'Olympus Scope Service — 2026 Renewal', category: 'vendor', conf: 0.86, low: 1, med: 3, high: 11, uploaded: '5h ago', owner: 'R. Chen', facility: 'nor' },
    { id: 'L-00018', name: 'Northside Suite 310 — Lease Amendment', category: 'lease', conf: 0.68, low: 4, med: 3, high: 9, uploaded: '5h ago', owner: null, facility: 'nor' },
    { id: 'E-00231', name: 'Dr. Marcus Hale — Comp Addendum', category: 'employee', conf: 0.91, low: 0, med: 2, high: 14, uploaded: 'Yesterday', owner: 'K. Sato', facility: 'evg' },
  ];

  const [sel, setSel] = useState(batch[0].id);
  const cur = batch.find(b => b.id === sel);

  const sampleFields = [
    { label: 'Counterparty', value: cur.name.split(' — ')[0], conf: 'high', source: 'Page 1, header' },
    { label: 'Effective date', value: cur.id === 'L-00018' ? 'June 1, 2026' : 'April 1, 2026', conf: 'high', source: 'Section 1.1' },
    { label: 'End date', value: cur.id === 'L-00018' ? '?' : 'March 31, 2029', conf: cur.id === 'L-00018' ? 'low' : 'high', source: cur.id === 'L-00018' ? 'Not found — check handwritten addendum' : 'Section 2' },
    { label: 'Rent escalator', value: cur.id === 'L-00018' ? '3.0% annual or CPI-U' : '—', conf: cur.id === 'L-00018' ? 'med' : 'high', source: cur.id === 'L-00018' ? 'Section 4.2' : '—' },
    { label: 'Notice period', value: '120 days', conf: cur.id === 'L-00018' ? 'med' : 'high', source: 'Section 14' },
    { label: 'Renewal type', value: cur.id === 'L-00018' ? 'Option to extend 5yr (exercisable)' : 'Auto-renew 12mo', conf: cur.id === 'C-00212' ? 'low' : 'high', source: cur.id === 'C-00212' ? 'Conflicting language — see §3 vs §7.4' : 'Section 3' },
    { label: 'Annual rent', value: cur.id === 'L-00018' ? '$292,400' : '—', conf: cur.id === 'L-00018' ? 'low' : 'high', source: 'Exhibit A (scanned, handwritten)' },
    { label: 'Rate schedule attached', value: cur.id === 'C-00212' ? 'Yes — Exhibit A-2' : '—', conf: 'high', source: cur.category === 'payer' ? 'Exhibit A-2' : '—' },
  ];

  const nextCard = () => {
    const idx = batch.findIndex(b => b.id === sel);
    if (idx < batch.length - 1) setSel(batch[idx + 1].id);
  };

  return (
    <div className="page">
      <div className="crumb">
        <a onClick={() => setState(s => ({...s, view: 'dashboard'}))}>Command center</a>
        <span className="sep">›</span>
        <span>Extraction review</span>
      </div>

      <div className="page-head">
        <div>
          <div className="eyebrow"><span className="bullet"/><span>Human QA · {batch.length} contracts awaiting review</span></div>
          <h1 className="page-title">Review extracted fields</h1>
          <div className="page-subtitle">
            Confirm or correct AI-extracted fields before contracts go live. Low-confidence fields are flagged for attention.
          </div>
        </div>
        <div style={{display:'flex', gap:8, flexShrink: 0}}>
          <button className="btn">Skip batch</button>
          <button className="btn primary">Approve all high-confidence</button>
        </div>
      </div>

      <div style={{display:'grid', gridTemplateColumns:'280px 1fr', gap: 20}}>
        {/* Queue */}
        <div className="card" style={{overflow:'hidden'}}>
          <div className="card-head">
            <h3>Queue</h3>
            <span className="sub">{batch.length} pending</span>
          </div>
          <div>
            {batch.map(b => {
              const isSel = b.id === sel;
              return (
                <div key={b.id} onClick={() => setSel(b.id)}
                  style={{
                    padding: '12px 14px',
                    borderBottom: '1px solid var(--line-2)',
                    cursor: 'pointer',
                    background: isSel ? 'var(--accent-soft)' : 'transparent',
                    borderLeft: isSel ? '3px solid var(--accent)' : '3px solid transparent'
                  }}>
                  <div style={{fontSize: 12.5, fontWeight: 500, marginBottom: 3}}>{b.name}</div>
                  <div style={{fontFamily:'var(--ff-mono)', fontSize: 10.5, color: 'var(--ink-3)', letterSpacing:'.04em'}}>
                    {b.id} · <CategoryLabel id={b.category}/> · {b.uploaded}
                  </div>
                  <div style={{display:'flex', gap: 6, marginTop: 6, alignItems:'center'}}>
                    <div style={{width: 60, height: 4, background: 'var(--line)', borderRadius: 2, overflow: 'hidden'}}>
                      <div style={{width: `${b.conf * 100}%`, height: '100%', background: b.conf > 0.85 ? 'var(--ok)' : b.conf > 0.75 ? 'var(--warn)' : 'var(--accent)'}}/>
                    </div>
                    <span style={{fontFamily:'var(--ff-mono)', fontSize: 10, color: 'var(--ink-3)'}}>{Math.round(b.conf*100)}%</span>
                    {b.low > 0 && (
                      <span style={{marginLeft: 'auto', fontFamily:'var(--ff-mono)', fontSize: 10, color:'var(--accent)'}}>{b.low} flagged</span>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Review panel */}
        <div className="card">
          <div className="card-head">
            <h3>{cur.name}</h3>
            <span className="sub">{cur.id} · AI extracted {cur.uploaded} · {sampleFields.length} fields</span>
          </div>
          <div className="card-body">
            <div style={{display:'flex', gap: 20, marginBottom: 14, padding: 12, background: 'var(--surface-2)', borderRadius: 'var(--radius)', border: '1px solid var(--line-2)'}}>
              <Stat label="Overall confidence" value={`${Math.round(cur.conf*100)}%`} sub={cur.conf > 0.85 ? 'Ready for approval' : 'Review needed'}/>
              <Stat label="High confidence" value={cur.high} sub="auto-approved"/>
              <Stat label="Medium" value={cur.med} sub="review recommended"/>
              <Stat label="Low — flagged" value={cur.low} sub="must confirm"/>
            </div>

            {sampleFields.map((f, i) => (
              <div key={i} style={{
                display:'grid', gridTemplateColumns:'160px 1fr 180px 120px', gap: 16,
                padding: '14px 0', borderBottom: '1px solid var(--line-2)', alignItems: 'start'
              }}>
                <div>
                  <div style={{fontFamily:'var(--ff-mono)', fontSize: 10.5, color:'var(--ink-3)', textTransform:'uppercase', letterSpacing:'.05em'}}>{f.label}</div>
                  <div style={{fontSize: 10.5, color:'var(--ink-4)', fontFamily:'var(--ff-mono)', marginTop: 3}}>{f.source}</div>
                </div>
                <div>
                  <input defaultValue={f.value} style={{
                    width: '100%', border: 'none', borderBottom: '1px solid var(--line)',
                    padding: '4px 0', fontSize: 13, background: 'transparent', outline: 'none',
                    fontWeight: 500
                  }}/>
                  {f.conf === 'low' && (
                    <div style={{fontSize: 11, color: 'var(--accent)', marginTop: 4, fontFamily: 'var(--ff-mono)'}}>
                      ⚠ Field not confidently extracted — manual review required
                    </div>
                  )}
                </div>
                <div>
                  <div className={`conf ${f.conf}`} style={{justifyContent:'flex-start', marginBottom: 6}}>
                    <div className="conf-bar"><span style={{width: f.conf === 'high' ? '92%' : f.conf === 'med' ? '64%' : '32%'}}/></div>
                    <span style={{fontFamily:'var(--ff-mono)', fontSize: 10}}>{f.conf.toUpperCase()}</span>
                  </div>
                </div>
                <div style={{display:'flex', gap: 4}}>
                  <button className="btn sm" style={{background: 'var(--ok-soft)', borderColor: 'var(--ok-soft)', color: 'var(--ok)'}}>
                    ✓ Confirm
                  </button>
                  <button className="btn sm ghost" title="Flag for follow-up">⚑</button>
                </div>
              </div>
            ))}

            {/* Assign owner */}
            <div style={{display:'flex', gap: 20, padding: '18px 0', borderBottom: '1px solid var(--line-2)', alignItems:'center'}}>
              <div style={{fontFamily:'var(--ff-mono)', fontSize: 10.5, color:'var(--ink-3)', textTransform:'uppercase', letterSpacing:'.05em', width: 160}}>Assign owner</div>
              <div style={{flex:1, display:'flex', gap: 8, alignItems: 'center'}}>
                {['J. Okafor', 'M. Paredes', 'R. Chen', 'K. Sato', 'A. Linh', 'S. Whitfield'].map(o => (
                  <button key={o} className={`btn sm ${cur.owner === o ? 'primary' : ''}`}>{o}</button>
                ))}
                <button className="btn sm ghost">+ Invite</button>
              </div>
            </div>

            <div style={{display:'flex', gap: 10, justifyContent:'flex-end', marginTop: 20}}>
              <button className="btn">Save draft</button>
              <button className="btn" onClick={nextCard}>Skip →</button>
              <button className="btn primary" onClick={nextCard}>Approve & continue</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function Stat({ label, value, sub }) {
  return (
    <div style={{minWidth: 120}}>
      <div style={{fontFamily: 'var(--ff-mono)', fontSize: 10, color: 'var(--ink-4)', letterSpacing: '.08em', textTransform: 'uppercase', marginBottom: 4}}>{label}</div>
      <div style={{fontFamily: 'var(--ff-serif)', fontSize: 22, lineHeight: 1, marginBottom: 2}}>{value}</div>
      <div style={{fontSize: 10.5, color: 'var(--ink-3)', fontFamily: 'var(--ff-mono)'}}>{sub}</div>
    </div>
  );
}

window.ExtractionReview = ExtractionReview;
