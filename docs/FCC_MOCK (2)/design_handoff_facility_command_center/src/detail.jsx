// Contract detail — side-by-side document + AI-extracted fields

function ContractDetail({ state, setState }) {
  const c = window.CONTRACTS.find(x => x.id === state.contractId);
  if (!c) return <div className="page">Contract not found. <a onClick={() => setState(s => ({...s, view: 'contracts'}))}>Back to list</a></div>;

  const [tab, setTab] = useState('universal');
  const [activeAnchor, setActiveAnchor] = useState(null);
  const [localFields, setLocalFields] = useState(() => window.EXTRACTED[c.id] || defaultExtract(c));
  const [justSaved, setJustSaved] = useState(null);

  const d = window.daysBetween(c.end);
  const f = window.FACILITIES.find(f => f.id === c.facility);

  const updateField = (tab, idx, newVal) => {
    setLocalFields(prev => {
      const next = { ...prev };
      next[tab] = next[tab].map((f, i) => i === idx ? { ...f, value: newVal, conf: 'high', edited: true } : f);
      return next;
    });
    setJustSaved(Date.now());
    setTimeout(() => setJustSaved(null), 1500);
  };

  return (
    <div className="page">
      <div className="crumb">
        <a onClick={() => setState(s => ({...s, view: 'dashboard'}))}>Command center</a>
        <span className="sep">›</span>
        <a onClick={() => setState(s => ({...s, view: 'contracts'}))}>Contracts</a>
        <span className="sep">›</span>
        <span>{c.id}</span>
      </div>

      <div className="page-head" style={{alignItems: 'flex-start'}}>
        <div style={{flex: 1, minWidth: 0}}>
          <div className="eyebrow">
            <span className="bullet"/>
            <span>{c.id}</span>
            <span style={{color:'var(--ink-4)'}}>·</span>
            <CategoryLabel id={c.category}/>
            <span style={{color:'var(--ink-4)'}}>·</span>
            <span>{f?.name}</span>
          </div>
          <h1 className="page-title detail-title">
            <span className="counterparty-avatar" style={{width: 24, height: 24, fontSize: 11, verticalAlign: '-2px', marginRight: 10}}>{c.counterparty.initials}</span>
            {c.name}
          </h1>
          <div style={{display:'flex', gap: 16, fontSize: 12, color: 'var(--ink-3)', flexWrap:'wrap', alignItems:'center', marginTop: 14}}>
            {statusChip(c)}
            <span><span className="kbd-hint">END</span> {formatDate(c.end)} <span style={{color: d < 0 ? 'var(--accent)' : d <= 30 ? 'var(--accent)' : 'var(--ink-4)'}}>({d < 0 ? `${Math.abs(d)}d past` : `${d}d`})</span></span>
            <span><span className="kbd-hint">RENEWAL</span> {c.renewal}</span>
            <span><span className="kbd-hint">NOTICE</span> {c.notice}</span>
            <span><span className="kbd-hint">OWNER</span> {c.owner}</span>
            {c.annualValue && <span><span className="kbd-hint">VALUE</span> ${(c.annualValue/1000).toFixed(0)}K/yr</span>}
          </div>
        </div>
        <div style={{display:'flex', gap:8, flexShrink: 0}}>
          <button className="btn"><Icon name="download"/> Original</button>
          <button className="btn"><Icon name="bell"/> Set alert</button>
          <button className="btn primary">Start renegotiation</button>
        </div>
      </div>

      {c.flags && c.flags.length > 0 && (
        <div className="alert danger" style={{border: '1px solid var(--accent)', borderRadius: 'var(--radius-lg)', marginBottom: 16, background: 'var(--accent-soft)'}}>
          <div className="bar"/>
          <div>
            <div className="head">{c.flags[0]}</div>
            <div className="body">Review the notice section in the document — AI highlighted the relevant clause for your review.</div>
          </div>
          <button className="btn sm">Acknowledge</button>
        </div>
      )}

      <div className="split">
        {/* Document preview */}
        <div className="doc-pane">
          <div className="doc-head">
            <span><Icon name="doc" size={12}/> {c.name.toLowerCase().replace(/\s+/g,'_')}.pdf · {c.docPages} pages</span>
            <span>Page 1 / {c.docPages}</span>
          </div>
          <div className="doc-body">
            <div className="doc-page">
              <div className="date">EXECUTED {formatDate(c.effective)}</div>
              <h2>{c.category === 'payer' ? 'PROVIDER PARTICIPATION AGREEMENT' : c.category === 'lease' ? 'COMMERCIAL LEASE AGREEMENT' : c.category === 'employee' ? 'PROFESSIONAL SERVICES AGREEMENT' : 'SERVICES AGREEMENT'}</h2>
              <p style={{color: '#6b6759'}}>BETWEEN {c.counterparty.name.toUpperCase()} AND {f?.name?.toUpperCase()}</p>
              <p>This Agreement (this "Agreement") is entered into as of {formatDate(c.effective)} (the "Effective Date") by and between {c.counterparty.name}, and {f?.name} ("Facility"), and sets forth the terms and conditions pursuant to which the parties will conduct their relationship.</p>

              <h3>1. Scope of Services</h3>
              <p>Facility agrees to render services described in <b>Exhibit A</b> attached hereto and incorporated herein by reference. The services shall be provided in accordance with applicable laws, regulations, and standards of care.</p>

              <h3 id="term">2. Term</h3>
              <p>The initial term of this Agreement shall commence on the Effective Date and continue until <span className={`hl ${activeAnchor==='term'?'active':''}`} onClick={() => setActiveAnchor('term')}>{formatDate(c.end)}</span> (the "Initial Term"), <span className={`hl ${activeAnchor==='renewal'?'active':''}`} onClick={() => setActiveAnchor('renewal')}>unless terminated earlier as provided herein, and shall automatically renew for successive twelve (12) month periods</span> thereafter (each a "Renewal Term").</p>

              <h3>3. Compensation</h3>
              <p>{c.category === 'payer' ? 'Payer shall reimburse Facility in accordance with the fee schedule set forth in Exhibit A, subject to the terms hereof.' : 'Consideration shall be paid per the schedule set forth in the attached exhibits.'}</p>

              {c.category === 'payer' && (
                <>
                  <h3 id="fee">4. Fee Schedule</h3>
                  <p><span className={`hl ${activeAnchor==='fee'?'active':''}`} onClick={() => setActiveAnchor('fee')}>Reimbursement shall be calculated at the percentages of billed charges and fixed per-case rates set forth in Exhibit A</span>, as amended from time to time upon mutual written agreement.</p>
                </>
              )}

              <h3 id="notice">5. Termination & Notice</h3>
              <p>Either party may terminate this Agreement <span className={`hl ${activeAnchor==='notice'?'active':''}`} onClick={() => setActiveAnchor('notice')}>upon ninety (90) days prior written notice to the other party</span>, with or without cause. Termination shall not relieve either party of obligations accrued prior to the effective date of termination.</p>

              <h3>6. Rate Escalation</h3>
              <p>Rates set forth in Exhibit A may be adjusted annually by an amount not to exceed the lesser of (a) the CPI-U for the prior calendar year, or (b) four and two-tenths percent (4.2%), upon written notice from the adjusting party.</p>
            </div>
            <div style={{fontSize: 11, color: 'var(--ink-4)', fontFamily: 'var(--ff-mono)', textTransform: 'uppercase', letterSpacing: '.06em'}}>
              Scroll — showing 1 of {c.docPages}
            </div>
          </div>
        </div>

        {/* Extracted */}
        <div className="ext-pane">
          <div className="ext-tabs">
            <div className={`ext-tab ${tab==='universal'?'active':''}`} onClick={() => setTab('universal')}>
              Universal fields
            </div>
            {c.category === 'payer' && (
              <div className={`ext-tab ${tab==='payer'?'active':''}`} onClick={() => setTab('payer')}>
                Payer-specific
              </div>
            )}
            <div className={`ext-tab ${tab==='dates'?'active':''}`} onClick={() => setTab('dates')}>
              Dates & obligations
            </div>
            <div style={{marginLeft: 'auto', display:'flex', alignItems:'center', padding: '0 12px', fontSize: 11, color: 'var(--ink-3)', gap: 8, fontFamily: 'var(--ff-mono)'}}>
              <Icon name="sparkle" size={11}/>
              <span>AI · {justSaved ? 'Saved' : 'Extracted'}</span>
            </div>
          </div>
          <div className="ext-body">
            {(tab === 'universal' || tab === 'payer') && (localFields[tab] || []).map((f, i) => (
              <div className="field-row" key={`${tab}-${i}`} onClick={() => f.anchor && setActiveAnchor(f.anchor)}>
                <div className="lab">{f.label}</div>
                <div className="val">
                  <input
                    value={f.value}
                    onChange={e => updateField(tab, i, e.target.value)}
                    onClick={e => e.stopPropagation()}
                  />
                  {f.anchor && (
                    <div style={{fontSize: 10.5, color: 'var(--accent)', marginTop: 2, fontFamily: 'var(--ff-mono)', letterSpacing: '.03em', cursor: 'pointer'}}>
                      ↗ jump to source clause
                    </div>
                  )}
                </div>
                <div className={`conf ${f.conf}`}>
                  {f.conf === 'high' ? 'HIGH' : f.conf === 'med' ? 'MED' : 'LOW'}
                  <div className="conf-bar">
                    <span style={{width: f.conf === 'high' ? '92%' : f.conf === 'med' ? '64%' : '38%'}}/>
                  </div>
                </div>
              </div>
            ))}
            {tab === 'dates' && (
              <div>
                <div className="field-row">
                  <div className="lab">Effective</div>
                  <div className="val">{formatDate(c.effective)}</div>
                  <div></div>
                </div>
                <div className="field-row">
                  <div className="lab">Expiration</div>
                  <div className="val">{formatDate(c.end)} · <span style={{color: 'var(--accent)'}}>{window.daysBetween(c.end)}d</span></div>
                  <div></div>
                </div>
                <div className="field-row">
                  <div className="lab">Notice deadline</div>
                  <div className="val">{formatDate(window.daysFromNow(window.daysBetween(c.end) - 90))} <span style={{color: 'var(--ink-3)'}}>(90d before term)</span></div>
                  <div></div>
                </div>
                <div className="field-row">
                  <div className="lab">Auto-renew type</div>
                  <div className="val">{c.renewal}</div>
                  <div></div>
                </div>
                <div style={{padding: 16, borderTop: '1px solid var(--line-2)'}}>
                  <div className="section-title" style={{marginTop: 0}}>Upcoming obligations derived</div>
                  {window.OBLIGATIONS.filter(o => o.contract === c.id).map(o => (
                    <div key={o.id} style={{display:'flex', justifyContent:'space-between', padding:'8px 0', borderBottom:'1px solid var(--line-2)', fontSize: 12.5}}>
                      <span>{o.title}</span>
                      <span style={{fontFamily: 'var(--ff-mono)', color: 'var(--ink-3)'}}>{formatDate(o.due)}</span>
                    </div>
                  ))}
                  {window.OBLIGATIONS.filter(o => o.contract === c.id).length === 0 && (
                    <div style={{fontSize: 12, color: 'var(--ink-3)'}}>No obligations linked yet. <a style={{color:'var(--accent)', cursor:'pointer'}}>+ Add</a></div>
                  )}
                </div>
              </div>
            )}
            <div style={{padding: 14, borderTop: '1px solid var(--line-2)', background: 'var(--surface-2)', fontSize: 11.5, color: 'var(--ink-3)'}}>
              <Icon name="sparkle" size={11}/> Fields extracted by AI from {c.docPages}-page document on {formatDate(window.daysFromNow(-7))}.
              <span style={{color:'var(--ink-2)', marginLeft: 6}}>Edit to correct; changes are saved to the record.</span>
            </div>
          </div>
        </div>
      </div>

      {/* Negotiation Playbook + Activity */}
      <NegotiationPanel contract={c}/>
      <ActivityPanel contract={c}/>
    </div>
  );
}

function NegotiationPanel({ contract: c }) {
  const [expanded, setExpanded] = useState(true);
  const d = window.daysBetween(c.end);
  const isPayer = c.category === 'payer';
  const isLease = c.category === 'lease';
  const isVendor = c.category === 'vendor';

  const plays = isPayer ? [
    { priority: 'high', lever: 'CPT 43239 rate — underpaid', detail: 'Your rate is 14% below regional ASC median and 82% of CMS. Annual opportunity at current volume: $46K.', ask: 'Request +12% on 43239; concede ±2% on low-volume 43270.', conf: 88 },
    { priority: 'high', lever: 'Auto-renewal w/ CPI cap', detail: 'Current language: "lesser of CPI-U or 4.2%." Over last 3 yrs this capped you below medical inflation.', ask: 'Negotiate floor at 3.5% or use MEI instead of CPI-U.', conf: 81 },
    { priority: 'med', lever: 'Timely filing window', detail: '90-day filing is below industry norm of 180 days. Costs you ~$18K/yr in write-offs.', ask: 'Request 180-day timely filing; precedent from Premera contract.', conf: 72 },
    { priority: 'med', lever: 'Prior-auth carve-outs', detail: 'No carve-outs for routine screening colonoscopy (CPT 45378). Adds 2.3 days DSO on average.', ask: 'Add USPSTF A/B preventive carve-out.', conf: 68 },
  ] : isLease ? [
    { priority: 'high', lever: 'Holdover rent', detail: 'Current holdover = 150% of base rent. Market standard is 125–135% for medical.', ask: 'Reduce to 125%; extend cure period to 15 days.', conf: 76 },
    { priority: 'high', lever: 'Exclusive use clause', detail: 'Current lease has no exclusive-use protection. Landlord could lease adjacent suite to competing ASC.', ask: 'Add exclusivity for endoscopy/GI within the medical building.', conf: 84 },
    { priority: 'med', lever: 'CAM audit rights', detail: 'No right to audit CAM reconciliation — 3yr cap missed by $11K in 2024.', ask: 'Add 3-yr look-back audit right with 60-day cure.', conf: 70 },
  ] : isVendor ? [
    { priority: 'high', lever: 'Volume tier not captured', detail: 'You hit Tier 2 volume (400+ cases/yr) but billed at Tier 1. ~$14K under-discount.', ask: 'Request retroactive Tier 2 pricing + auto-tier triggers.', conf: 82 },
    { priority: 'med', lever: 'Loaner scope reconciliation', detail: 'No SLA for loaner turnaround during repair. Last event cost 2 days of room utilization.', ask: 'Add 48-hr loaner SLA with credit for breach.', conf: 65 },
    { priority: 'med', lever: 'Auto-renewal notice', detail: 'Notice period 90d is tight; competing vendor quoted 9% lower last quarter.', ask: 'Extend to 120d notice; add benchmark-match clause.', conf: 60 },
  ] : [
    { priority: 'med', lever: 'Termination flexibility', detail: 'Mutual 90-day termination is standard; your payer portfolio skews toward 60-day.', ask: 'Align to 60-day mutual; may concede on indemnification cap.', conf: 70 },
  ];

  const timing = d < 60 ? 'Too late — renegotiate at next renewal window.' : d < 150 ? `Act now — notice deadline in ${d - 90}d.` : `Best window opens ${formatDate(window.daysFromNow(d - 150))}.`;

  return (
    <div className="card" style={{marginTop: 16}}>
      <div className="card-head" style={{cursor: 'pointer'}} onClick={() => setExpanded(v => !v)}>
        <h3 style={{display:'flex', alignItems:'center', gap: 8}}>
          <span style={{display:'inline-block', width: 20, height: 20, borderRadius: 4, background: 'var(--accent-soft)', color: 'var(--accent)', fontSize: 11, lineHeight: '20px', textAlign: 'center', fontFamily: 'var(--ff-mono)'}}>AI</span>
          Negotiation playbook
          <span className="premium-badge" style={{marginLeft: 4}}>PRO</span>
        </h3>
        <span className="sub">{plays.length} levers · {timing}</span>
        <span style={{marginLeft: 'auto', fontSize: 14, color: 'var(--ink-3)'}}>{expanded ? '▾' : '▸'}</span>
      </div>

      {expanded && (
        <div className="card-body">
          <div style={{display:'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12, marginBottom: 18, padding: 12, background: 'var(--surface-2)', border: '1px solid var(--line-2)', borderRadius: 'var(--radius)'}}>
            <div>
              <div style={{fontFamily:'var(--ff-mono)', fontSize: 10, color: 'var(--ink-4)', textTransform: 'uppercase', letterSpacing: '.08em', marginBottom: 4}}>Posture</div>
              <div style={{fontSize: 13, fontWeight: 500}}>{isPayer ? 'Assertive — you are below market' : isLease ? 'Measured — long-term relationship' : 'Balanced'}</div>
            </div>
            <div>
              <div style={{fontFamily:'var(--ff-mono)', fontSize: 10, color: 'var(--ink-4)', textTransform: 'uppercase', letterSpacing: '.08em', marginBottom: 4}}>Window</div>
              <div style={{fontSize: 13, fontWeight: 500}}>{timing}</div>
            </div>
            <div>
              <div style={{fontFamily:'var(--ff-mono)', fontSize: 10, color: 'var(--ink-4)', textTransform: 'uppercase', letterSpacing: '.08em', marginBottom: 4}}>Total opportunity</div>
              <div style={{fontSize: 13, fontWeight: 500, color: 'var(--accent)'}}>{isPayer ? '+$64K / yr est.' : isLease ? '$11K recoverable' : isVendor ? '$14K retro' : '—'}</div>
            </div>
          </div>

          {plays.map((p, i) => (
            <div key={i} style={{
              display:'grid', gridTemplateColumns: '90px 1fr 140px',
              gap: 16, padding: '14px 0', borderTop: i === 0 ? 'none' : '1px solid var(--line-2)', alignItems: 'start'
            }}>
              <div>
                <span className={`chip ${p.priority === 'high' ? 'danger' : p.priority === 'med' ? 'warn' : ''}`} style={{fontFamily: 'var(--ff-mono)'}}>
                  {p.priority.toUpperCase()}
                </span>
              </div>
              <div>
                <div style={{fontWeight: 500, fontSize: 13.5, marginBottom: 4}}>{p.lever}</div>
                <div style={{fontSize: 12, color: 'var(--ink-2)', marginBottom: 6, lineHeight: 1.5}}>{p.detail}</div>
                <div style={{fontSize: 12, color: 'var(--accent)', fontStyle: 'italic'}}>
                  <span style={{fontFamily: 'var(--ff-mono)', fontStyle: 'normal', fontSize: 10, letterSpacing: '.08em', textTransform: 'uppercase', marginRight: 8}}>Ask →</span>
                  {p.ask}
                </div>
              </div>
              <div style={{textAlign: 'right'}}>
                <div style={{fontFamily: 'var(--ff-mono)', fontSize: 10, color: 'var(--ink-4)', textTransform: 'uppercase', letterSpacing: '.06em', marginBottom: 4}}>Confidence</div>
                <div style={{fontFamily: 'var(--ff-serif)', fontSize: 20, lineHeight: 1, marginBottom: 6}}>{p.conf}<span style={{fontSize: 11, color: 'var(--ink-3)'}}>%</span></div>
                <div style={{width: '100%', height: 3, background: 'var(--line)', borderRadius: 2}}>
                  <div style={{width: `${p.conf}%`, height: '100%', background: p.conf > 80 ? 'var(--ok)' : p.conf > 65 ? 'var(--warn)' : 'var(--accent)', borderRadius: 2}}/>
                </div>
              </div>
            </div>
          ))}

          <div style={{display:'flex', gap: 10, justifyContent:'flex-end', marginTop: 16, paddingTop: 14, borderTop: '1px solid var(--line-2)'}}>
            <button className="btn">Export brief (.pdf)</button>
            <button className="btn">Draft email to counterparty</button>
            <button className="btn primary">Start renegotiation</button>
          </div>
        </div>
      )}
    </div>
  );
}

function ActivityPanel({ contract: c }) {
  const [expanded, setExpanded] = useState(true);
  const [newNote, setNewNote] = useState('');
  const owner = c.owner;

  const collaborators = [
    { name: 'J. Okafor', role: 'ASC Admin', primary: true },
    { name: 'M. Paredes', role: 'Billing Lead' },
    { name: 'Dr. K. Sato', role: 'Medical Director' },
  ];

  const activity = [
    { when: '2h ago', who: 'J. Okafor', kind: 'comment', what: 'Legal wants us to push on the holdover clause — let\'s include in renegotiation brief.' },
    { when: 'Yesterday', who: 'AI', kind: 'ai', what: `Flagged ${c.flags?.[0] || 'upcoming renewal obligation'}.` },
    { when: '3d ago', who: 'M. Paredes', kind: 'ack', what: 'Acknowledged notice deadline alert.' },
    { when: '1w ago', who: 'J. Okafor', kind: 'assign', what: 'Assigned ownership to J. Okafor.' },
    { when: '2w ago', who: 'System', kind: 'version', what: 'Document uploaded and extracted (v1).' },
    { when: `${Math.round((new Date() - new Date(c.effective)) / (1000*60*60*24*30))} mo ago`, who: 'System', kind: 'created', what: 'Contract record created.' },
  ];

  const kindIcon = { comment: '💬', ai: '◆', ack: '✓', assign: '→', version: '⎙', created: '•' };
  const kindColor = { ai: 'var(--accent)', ack: 'var(--ok)', assign: 'var(--ink-2)', version: 'var(--ink-3)', comment: 'var(--ink-2)', created: 'var(--ink-4)' };

  return (
    <div className="card" style={{marginTop: 16, marginBottom: 24}}>
      <div className="card-head" style={{cursor: 'pointer'}} onClick={() => setExpanded(v => !v)}>
        <h3>Ownership & activity</h3>
        <span className="sub">Owner · {owner} · {activity.length} events</span>
        <span style={{marginLeft: 'auto', fontSize: 14, color: 'var(--ink-3)'}}>{expanded ? '▾' : '▸'}</span>
      </div>

      {expanded && (
        <div style={{display:'grid', gridTemplateColumns:'240px 1fr', gap: 0}}>
          {/* Owners */}
          <div style={{padding: 18, borderRight: '1px solid var(--line-2)'}}>
            <div style={{fontFamily:'var(--ff-mono)', fontSize: 10, color: 'var(--ink-4)', textTransform:'uppercase', letterSpacing:'.08em', marginBottom: 10}}>Accountable</div>
            {collaborators.map((p, i) => (
              <div key={i} style={{display:'flex', alignItems:'center', gap: 10, padding: '8px 0', borderBottom: i < collaborators.length - 1 ? '1px solid var(--line-2)' : 'none'}}>
                <div style={{width: 28, height: 28, borderRadius: '50%', background: p.primary ? 'var(--accent)' : 'var(--surface-2)', color: p.primary ? '#fff' : 'var(--ink-2)', fontSize: 10.5, fontWeight: 500, display:'grid', placeItems:'center', border: '1px solid var(--line)', fontFamily: 'var(--ff-mono)'}}>
                  {p.name.replace('Dr. ','').split(' ').map(s => s[0]).join('')}
                </div>
                <div style={{flex:1, minWidth: 0}}>
                  <div style={{fontSize: 12.5, fontWeight: 500}}>{p.name}</div>
                  <div style={{fontSize: 10.5, color: 'var(--ink-3)', fontFamily:'var(--ff-mono)', letterSpacing:'.03em'}}>{p.role}{p.primary ? ' · OWNER' : ''}</div>
                </div>
              </div>
            ))}
            <button className="btn sm ghost" style={{marginTop: 10, width: '100%'}}>+ Assign collaborator</button>

            <div style={{marginTop: 20, paddingTop: 16, borderTop: '1px solid var(--line-2)'}}>
              <div style={{fontFamily:'var(--ff-mono)', fontSize: 10, color: 'var(--ink-4)', textTransform:'uppercase', letterSpacing:'.08em', marginBottom: 10}}>Acknowledgments</div>
              <div style={{fontSize: 12, color: 'var(--ink-2)', lineHeight: 1.6}}>
                <div style={{display:'flex', justifyContent:'space-between', padding: '4px 0'}}>
                  <span>J. Okafor</span><span style={{color: 'var(--ok)'}}>✓ Acknowledged</span>
                </div>
                <div style={{display:'flex', justifyContent:'space-between', padding: '4px 0'}}>
                  <span>M. Paredes</span><span style={{color: 'var(--ok)'}}>✓ Acknowledged</span>
                </div>
                <div style={{display:'flex', justifyContent:'space-between', padding: '4px 0'}}>
                  <span>Dr. K. Sato</span><span style={{color: 'var(--ink-4)'}}>— Pending</span>
                </div>
              </div>
              <button className="btn sm" style={{marginTop: 10, width: '100%'}}>Request acknowledgment</button>
            </div>
          </div>

          {/* Timeline */}
          <div style={{padding: 18}}>
            <div style={{fontFamily:'var(--ff-mono)', fontSize: 10, color: 'var(--ink-4)', textTransform:'uppercase', letterSpacing:'.08em', marginBottom: 12}}>Activity timeline</div>

            <div style={{display:'flex', gap: 8, marginBottom: 16, alignItems:'center'}}>
              <input
                value={newNote}
                onChange={e => setNewNote(e.target.value)}
                placeholder="Add a note, tag someone with @, link to contract with #…"
                style={{flex: 1, padding: '8px 10px', border: '1px solid var(--line)', borderRadius: 'var(--radius)', fontSize: 12.5, background: 'var(--surface-2)', fontFamily: 'var(--ff-body)'}}/>
              <button className="btn sm primary">Post</button>
            </div>

            <div style={{position: 'relative', paddingLeft: 20}}>
              <div style={{position: 'absolute', left: 5, top: 8, bottom: 8, width: 1, background: 'var(--line-2)'}}/>
              {activity.map((a, i) => (
                <div key={i} style={{position: 'relative', paddingBottom: 14}}>
                  <div style={{position: 'absolute', left: -20, top: 2, width: 11, height: 11, borderRadius: '50%', background: 'var(--surface)', border: `2px solid ${kindColor[a.kind]}`, display: 'grid', placeItems: 'center', fontSize: 7, color: kindColor[a.kind], fontWeight: 700}}>
                    {a.kind === 'ai' ? '◆' : a.kind === 'ack' ? '✓' : ''}
                  </div>
                  <div style={{display:'flex', gap: 8, alignItems: 'baseline', marginBottom: 2}}>
                    <span style={{fontSize: 12.5, fontWeight: 500, color: a.kind === 'ai' ? 'var(--accent)' : 'var(--ink)'}}>{a.who}</span>
                    <span style={{fontFamily: 'var(--ff-mono)', fontSize: 10, color: 'var(--ink-4)', letterSpacing: '.03em'}}>{a.when}</span>
                  </div>
                  <div style={{fontSize: 12.5, color: 'var(--ink-2)', lineHeight: 1.5}}>{a.what}</div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function defaultExtract(c) {
  const f = window.FACILITIES.find(f => f.id === c.facility);
  return {
    universal: [
      { label: 'Document name', value: c.name, conf: 'high' },
      { label: 'Contract type', value: {payer:'Payer agreement',vendor:'Vendor contract',lease:'Lease',employee:'Employee agreement',processor:'Processor agreement'}[c.category], conf: 'high' },
      { label: 'Facility', value: f?.name || '—', conf: 'high' },
      { label: 'Counterparty', value: c.counterparty.name, conf: 'high' },
      { label: 'Effective date', value: formatDate(c.effective), conf: 'high' },
      { label: 'End date', value: formatDate(c.end), conf: 'high', anchor: 'term' },
      { label: 'Renewal type', value: c.renewal, conf: 'high', anchor: 'renewal' },
      { label: 'Notice period', value: c.notice, conf: 'high', anchor: 'notice' },
      { label: 'Owner', value: c.owner, conf: 'high' },
      { label: 'Annual value', value: c.annualValue ? `$${c.annualValue.toLocaleString()}` : 'Not specified', conf: c.annualValue ? 'med' : 'low' },
    ],
  };
}

window.ContractDetail = ContractDetail;
