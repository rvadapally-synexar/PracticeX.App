// Rate Benchmarking page — Premium "killer screen"
// PayerPrice-style: CPT × payer × region benchmark vs contracted rates

function RateBenchmark({ state, setState }) {
  const [cpt, setCpt] = useState('45380');
  const [payer, setPayer] = useState('all');
  const rows = window.computeVariance();

  const selectedCpt = window.CPT_CATALOG.find(c => c.code === cpt);
  const b = window.BENCHMARKS[cpt];

  const rowsForCpt = rows.filter(r => r.cpt === cpt);

  const totalOpportunity = rows.filter(r => r.annualImpact > 0).reduce((s,r) => s + r.annualImpact, 0);
  const underpaidCount = rows.filter(r => r.delta < -20).length;

  if (state.tier !== 'premium') {
    return (
      <div className="page">
        <div className="crumb">
          <a onClick={() => setState(s => ({...s, view: 'dashboard'}))}>Command center</a>
          <span className="sep">›</span>
          <span>Rate visibility</span>
        </div>
        <div className="page-head">
          <div>
            <div className="eyebrow"><span className="bullet"/><span>Premium intelligence</span></div>
            <h1 className="page-title">Payer rate benchmarking</h1>
            <div className="page-subtitle">See how your contracted rates compare to the regional market — per CPT, per payer.</div>
          </div>
          <button className="btn primary" onClick={() => setState(s => ({...s, tier: 'premium'}))}>
            <Icon name="sparkle"/> Preview Premium
          </button>
        </div>
        <div className="card" style={{overflow: 'hidden', position: 'relative', minHeight: 400}}>
          <div className="card-body locked" style={{minHeight: 400}}>
            <div style={{display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 16}}>
              {[1,2,3].map(i => (
                <div key={i} style={{padding: 20, border: '1px solid var(--line-2)', borderRadius: 'var(--radius)'}}>
                  <div style={{height: 12, background: 'var(--line-2)', borderRadius: 2, width: '50%', marginBottom: 10}}/>
                  <div style={{height: 30, background: 'var(--line)', borderRadius: 2, width: '70%', marginBottom: 14}}/>
                  <div style={{height: 8, background: 'var(--line-2)', borderRadius: 2, width: '90%', marginBottom: 6}}/>
                  <div style={{height: 8, background: 'var(--line-2)', borderRadius: 2, width: '60%'}}/>
                </div>
              ))}
            </div>
            <div className="lock-overlay">
              <div className="inner" style={{maxWidth: 480}}>
                <div className="premium-badge" style={{marginBottom: 14}}>Premium</div>
                <div style={{fontFamily:'var(--ff-serif)', fontSize: 28, marginBottom: 8, lineHeight: 1.1}}>
                  Am I getting paid fairly?
                </div>
                <div style={{fontSize: 13, color: 'var(--ink-2)', marginBottom: 18, lineHeight: 1.5}}>
                  Compare your contracted rates against regional percentiles from public Transparency-in-Coverage data.
                  See per-CPT, per-payer variance, Medicare multiples, and annualized revenue opportunity — powered by
                  our benchmark data partner.
                </div>
                <button className="btn primary" onClick={() => setState(s => ({...s, tier: 'premium'}))}>
                  Unlock Premium
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="page">
      <div className="crumb">
        <a onClick={() => setState(s => ({...s, view: 'dashboard'}))}>Command center</a>
        <span className="sep">›</span>
        <span>Rate visibility</span>
        <span className="premium-badge" style={{marginLeft: 6}}>Premium</span>
      </div>

      <div className="page-head">
        <div>
          <div className="eyebrow"><span className="bullet"/><span>PayerPrice benchmark · Puget Sound ASC market · Q2 2026</span></div>
          <h1 className="page-title">Am I getting paid fairly?</h1>
          <div className="page-subtitle">
            Your contracted rates compared to regional percentiles from public Transparency-in-Coverage files, normalized to CMS ASC 2026.
          </div>
        </div>
        <div style={{display:'flex', gap:8, flexShrink: 0}}>
          <button className="btn"><Icon name="download"/> Export brief</button>
          <button className="btn primary"><Icon name="zap"/> Generate negotiation pack</button>
        </div>
      </div>

      {/* Top KPIs */}
      <div className="kpi-row">
        <div className="kpi">
          <div className="label"><Icon name="alert" size={11}/> Annualized opportunity</div>
          <div className="value" style={{color: 'var(--accent)'}}>${(totalOpportunity/1000).toFixed(0)}K</div>
          <div className="delta">where you're under regional median · across {underpaidCount} payer/CPT combos</div>
        </div>
        <div className="kpi">
          <div className="label"><Icon name="stack" size={11}/> CPT codes tracked</div>
          <div className="value">{window.CPT_CATALOG.length}</div>
          <div className="delta">covering {window.CPT_CATALOG.reduce((s,c)=>s+c.volume,0).toLocaleString()} procedures/yr</div>
        </div>
        <div className="kpi">
          <div className="label"><Icon name="building" size={11}/> Peer ASCs in benchmark</div>
          <div className="value">42</div>
          <div className="delta">same market · site-of-service matched · facility fee only</div>
        </div>
        <div className="kpi">
          <div className="label"><Icon name="book" size={11}/> Source</div>
          <div className="value" style={{fontSize: 18, lineHeight: 1.2, marginTop: 10, fontFamily: 'var(--ff-sans)', fontWeight: 500}}>PayerPrice<br/><span style={{fontSize: 11, color: 'var(--ink-3)', fontFamily:'var(--ff-mono)'}}>MRF · TiC · CMS</span></div>
          <div className="delta" style={{marginTop: 2}}>Refreshed April 11, 2026</div>
        </div>
      </div>

      {/* Selector */}
      <div className="filter-bar" style={{marginTop: 20}}>
        <div className="filter-pill">
          <span className="lab">CPT</span>
          <select value={cpt} onChange={e => setCpt(e.target.value)} style={{border:'none', background:'none', outline:'none', minWidth: 320}}>
            {window.CPT_CATALOG.map(c => (
              <option key={c.code} value={c.code}>{c.code} — {c.desc}</option>
            ))}
          </select>
        </div>
        <div className="filter-pill">
          <span className="lab">Site of service</span>
          <select style={{border:'none', background:'none', outline:'none'}}>
            <option>ASC (22)</option>
            <option>HOPD (24)</option>
          </select>
        </div>
        <div className="filter-pill">
          <span className="lab">Geography</span>
          <select style={{border:'none', background:'none', outline:'none'}}>
            <option>Puget Sound CBSA</option>
            <option>Washington state</option>
            <option>West region</option>
          </select>
        </div>
        <div style={{flex: 1}}/>
        <div className="kbd-hint">CMS ASC 2026: <span style={{fontFamily:'var(--ff-mono)', color:'var(--ink-2)'}}>${selectedCpt.cms2026.toFixed(2)}</span></div>
      </div>

      {/* Killer screen: benchmark for selected CPT */}
      <div className="card">
        <div className="card-head">
          <h3>{cpt} — {selectedCpt.desc}</h3>
          <span className="sub">{selectedCpt.volume} procedures/yr · {b.regional.sampleSize} peer ASCs · Medicare-normalized</span>
        </div>
        <div className="card-body" style={{padding: 20}}>

          {/* The comparison bar chart — contracted vs regional band */}
          <div style={{marginBottom: 18, fontSize: 11, color: 'var(--ink-3)', fontFamily: 'var(--ff-mono)', textTransform: 'uppercase', letterSpacing: '.06em', display: 'flex', gap: 20, alignItems: 'center'}}>
            <span style={{display:'inline-flex', alignItems:'center', gap: 6}}>
              <span style={{width: 16, height: 10, background: 'var(--line)', border: '1px solid var(--line)'}}/> p10–p90 range
            </span>
            <span style={{display:'inline-flex', alignItems:'center', gap: 6}}>
              <span style={{width: 2, height: 14, background: 'var(--ink)'}}/> Regional median
            </span>
            <span style={{display:'inline-flex', alignItems:'center', gap: 6}}>
              <span style={{width: 10, height: 10, background: 'var(--accent)', borderRadius: '50%'}}/> Your contracted rate
            </span>
          </div>

          <BenchmarkBars cpt={cpt} selectedCpt={selectedCpt} rows={rowsForCpt} />

          <div style={{marginTop: 20, padding: 14, background: 'var(--surface-2)', border: '1px solid var(--line-2)', borderRadius: 'var(--radius)', fontSize: 12}}>
            <div style={{display: 'flex', gap: 20, flexWrap: 'wrap'}}>
              <Stat label="Regional median" value={`$${b.regional.median}`} sub={`p10 $${b.regional.p10} · p90 $${b.regional.p90}`}/>
              <Stat label="Market CMS multiple" value={`${b.regional.cmsMultiple.toFixed(2)}×`} sub="Regional median / CMS"/>
              <Stat label="Your avg multiple" value={`${(Object.values(b.contracted).reduce((s,v)=>s+v,0)/5/selectedCpt.cms2026).toFixed(2)}×`} sub="vs CMS ASC 2026"/>
              <Stat label="Underpaid vs median" value={rowsForCpt.filter(r => r.delta < 0).length + '/5'} sub="of your payers"/>
            </div>
          </div>
        </div>
      </div>

      {/* Payer-by-payer detail */}
      <div className="card" style={{marginTop: 20}}>
        <div className="card-head">
          <h3>Variance by payer</h3>
          <span className="sub">Sortable · click a row to open the underlying contract</span>
        </div>
        <div className="card-body flush">
          <table className="tbl">
            <thead>
              <tr>
                <th>Payer</th>
                <th className="right">Contracted</th>
                <th className="right">Median</th>
                <th className="right">Δ vs median</th>
                <th className="right">% diff</th>
                <th className="right">CMS multiple</th>
                <th style={{width: 180}}>Position</th>
                <th className="right">Annual impact</th>
              </tr>
            </thead>
            <tbody>
              {rowsForCpt.map(r => {
                const kind = r.delta < -20 ? 'danger' : r.delta > 20 ? 'ok' : 'info';
                return (
                  <tr key={r.payer} onClick={() => {
                    const c = window.CONTRACTS.find(c => c.counterparty.name.includes(r.payer.split(' ')[0]));
                    if (c) setState(s => ({...s, view: 'detail', contractId: c.id}));
                  }}>
                    <td className="primary-col">{r.payer}</td>
                    <td className="mono right">${r.rate}</td>
                    <td className="mono right muted">${r.median}</td>
                    <td className="mono right" style={{color: r.delta < 0 ? 'var(--accent)' : r.delta > 0 ? 'var(--ok)' : 'var(--ink-3)'}}>
                      {r.delta > 0 ? '+' : ''}${r.delta.toFixed(0)}
                    </td>
                    <td className="mono right" style={{color: r.pct < 0 ? 'var(--accent)' : 'var(--ok)'}}>
                      {r.pct > 0 ? '+' : ''}{r.pct.toFixed(1)}%
                    </td>
                    <td className="mono right">{r.cmsMx.toFixed(2)}×</td>
                    <td>
                      <PositionBar rate={r.rate} p10={r.p10} p90={r.p90} median={r.median}/>
                    </td>
                    <td className="mono right" style={{color: r.annualImpact > 5000 ? 'var(--accent)' : 'var(--ink-3)', fontWeight: r.annualImpact > 5000 ? 500 : 400}}>
                      {r.annualImpact > 0 ? '+$' + Math.round(r.annualImpact/1000) + 'K' : r.annualImpact < -500 ? '-$' + Math.abs(Math.round(r.annualImpact/1000)) + 'K' : '—'}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {/* Portfolio opportunity */}
      <div className="card" style={{marginTop: 20}}>
        <div className="card-head">
          <h3>Top revenue opportunities across your portfolio</h3>
          <span className="sub">Where contracted rates fall below regional median</span>
        </div>
        <div className="card-body flush">
          <table className="tbl">
            <thead>
              <tr>
                <th>Payer</th>
                <th>CPT</th>
                <th>Description</th>
                <th className="right">Δ/case</th>
                <th className="right">Est. volume</th>
                <th className="right">Annual gap</th>
              </tr>
            </thead>
            <tbody>
              {rows.filter(r => r.annualImpact > 3000).sort((a,b) => b.annualImpact - a.annualImpact).slice(0, 8).map((r, i) => (
                <tr key={i}>
                  <td className="primary-col">{r.payer}</td>
                  <td className="mono">{r.cpt}</td>
                  <td className="muted" style={{fontSize: 11.5, maxWidth: 320, overflow: 'hidden', textOverflow: 'ellipsis'}}>{r.desc}</td>
                  <td className="mono right" style={{color: 'var(--accent)'}}>-${Math.abs(r.delta).toFixed(0)}</td>
                  <td className="mono right muted">{r.volume}</td>
                  <td className="mono right" style={{color: 'var(--accent)', fontWeight: 500}}>${Math.round(r.annualImpact/1000)}K</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Methodology */}
      <div className="card" style={{marginTop: 20, background: 'var(--surface-2)'}}>
        <div className="card-body" style={{fontSize: 11.5, color: 'var(--ink-3)', lineHeight: 1.6}}>
          <div style={{fontFamily: 'var(--ff-mono)', fontSize: 10.5, letterSpacing: '.08em', textTransform: 'uppercase', color: 'var(--ink-4)', marginBottom: 6}}>Methodology</div>
          Benchmarks derived from public Transparency-in-Coverage machine-readable files and hospital MRF disclosures,
          aggregated and normalized by PayerPrice. Comparisons restrict to the same CPT/HCPCS, same payer, same market
          (Puget Sound CBSA), and same site-of-service (ASC). Percentiles reflect allowed amounts only. CMS ASC 2026
          serves as the fixed denominator for multiples. Sample sizes under 20 are marked low-confidence.
        </div>
      </div>
    </div>
  );
}

function Stat({ label, value, sub }) {
  return (
    <div>
      <div style={{fontFamily: 'var(--ff-mono)', fontSize: 10, color: 'var(--ink-4)', letterSpacing: '.08em', textTransform: 'uppercase', marginBottom: 4}}>{label}</div>
      <div style={{fontFamily: 'var(--ff-serif)', fontSize: 22, lineHeight: 1, marginBottom: 2}}>{value}</div>
      <div style={{fontSize: 10.5, color: 'var(--ink-3)', fontFamily: 'var(--ff-mono)'}}>{sub}</div>
    </div>
  );
}

function BenchmarkBars({ cpt, selectedCpt, rows }) {
  // Scale: min across p10 & contracted, max across p90 & contracted
  const allVals = rows.flatMap(r => [r.rate, r.p10, r.p90, r.median]);
  const min = Math.floor(Math.min(...allVals, selectedCpt.cms2026) * 0.95);
  const max = Math.ceil(Math.max(...allVals) * 1.05);
  const range = max - min;
  const pos = v => ((v - min) / range) * 100;

  return (
    <div>
      {rows.map(r => (
        <div key={r.payer} style={{display:'grid', gridTemplateColumns: '160px 1fr 100px', alignItems:'center', gap: 14, padding: '10px 0', borderBottom: '1px solid var(--line-2)'}}>
          <div style={{fontSize: 12.5, fontWeight: 500}}>{r.payer}</div>
          <div style={{position: 'relative', height: 26}}>
            {/* p10-p90 band */}
            <div style={{position: 'absolute', top: 8, height: 10, left: `${pos(r.p10)}%`, width: `${pos(r.p90) - pos(r.p10)}%`, background: 'var(--line-2)', border: '1px solid var(--line)', borderRadius: 2}}/>
            {/* median tick */}
            <div style={{position: 'absolute', top: 4, height: 18, left: `${pos(r.median)}%`, width: 2, background: 'var(--ink)'}}/>
            {/* CMS tick */}
            <div style={{position: 'absolute', top: 4, height: 18, left: `${pos(selectedCpt.cms2026)}%`, width: 1, background: 'var(--ink-3)', borderLeft: '1px dashed var(--ink-3)'}}/>
            {/* contracted dot */}
            <div style={{
              position: 'absolute', top: 7, left: `calc(${pos(r.rate)}% - 6px)`,
              width: 12, height: 12, borderRadius: '50%',
              background: r.delta < -20 ? 'var(--accent)' : r.delta > 20 ? 'var(--ok)' : 'var(--ink)',
              border: '2px solid var(--surface)',
              boxShadow: '0 0 0 1px ' + (r.delta < -20 ? 'var(--accent)' : r.delta > 20 ? 'var(--ok)' : 'var(--ink)'),
            }}/>
            {/* labels */}
            <div style={{position:'absolute', top: 0, left: `${pos(r.rate)}%`, transform: 'translateX(-50%)', fontSize: 10, fontFamily: 'var(--ff-mono)', color: r.delta < -20 ? 'var(--accent)' : r.delta > 20 ? 'var(--ok)' : 'var(--ink)', fontWeight: 600}}>
              ${r.rate}
            </div>
          </div>
          <div style={{textAlign: 'right', fontSize: 11, fontFamily: 'var(--ff-mono)', color: r.delta < 0 ? 'var(--accent)' : 'var(--ok)'}}>
            {r.pct > 0 ? '+' : ''}{r.pct.toFixed(1)}%<br/>
            <span style={{color: 'var(--ink-4)', fontSize: 10}}>{r.cmsMx.toFixed(2)}× CMS</span>
          </div>
        </div>
      ))}
      {/* Axis */}
      <div style={{display:'grid', gridTemplateColumns: '160px 1fr 100px', gap: 14, paddingTop: 6, fontSize: 10, fontFamily: 'var(--ff-mono)', color: 'var(--ink-4)'}}>
        <div></div>
        <div style={{display:'flex', justifyContent:'space-between'}}>
          <span>${min}</span>
          <span>${Math.round((min+max)/2)}</span>
          <span>${max}</span>
        </div>
        <div></div>
      </div>
    </div>
  );
}

function PositionBar({ rate, p10, p90, median }) {
  const pos = ((rate - p10) / (p90 - p10)) * 100;
  const medPos = ((median - p10) / (p90 - p10)) * 100;
  return (
    <div style={{position: 'relative', height: 16, background: 'var(--line-2)', borderRadius: 2, border: '1px solid var(--line-2)'}}>
      <div style={{position:'absolute', top: 0, bottom: 0, left: `${medPos}%`, width: 1, background: 'var(--ink-3)'}}/>
      <div style={{
        position: 'absolute', top: 2, bottom: 2,
        left: `calc(${Math.max(0, Math.min(100, pos))}% - 3px)`,
        width: 6, background: rate < median ? 'var(--accent)' : 'var(--ok)', borderRadius: 2,
      }}/>
    </div>
  );
}

window.RateBenchmark = RateBenchmark;
