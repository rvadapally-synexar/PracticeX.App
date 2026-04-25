// Dashboard — KPIs, renewals timeline, alerts, obligations

function Dashboard({ state, setState }) {
  const { facility, tier } = state;
  const filt = (c) => c.facility === facility || facility === 'all' || c.facility === 'all';
  const contracts = window.CONTRACTS.filter(filt);

  const renewing60 = contracts.filter(c => {
    const d = window.daysBetween(c.end);
    return d > 0 && d <= 60;
  }).sort((a,b) => window.daysBetween(a.end) - window.daysBetween(b.end));

  const expired = contracts.filter(c => window.daysBetween(c.end) < 0).length;
  const activeTotal = contracts.length;
  const totalAnnual = contracts.reduce((s,c) => s + (c.annualValue || 0), 0);

  const alertsFiltered = window.ALERTS;

  const byCategory = window.CATEGORIES.map(cat => ({
    ...cat,
    count: contracts.filter(c => c.category === cat.id).length,
  }));
  const maxCount = Math.max(...byCategory.map(c=>c.count), 1);

  const go = (view, extra) => setState(s => ({ ...s, view, ...extra }));

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <div className="eyebrow">
            <span className="bullet"/>
            <span>Operations overview</span>
            <span style={{color:'var(--ink-4)'}}>·</span>
            <span>{formatDate(window.TODAY.toISOString().slice(0,10))}</span>
          </div>
          <h1 className="page-title">Command center</h1>
          <div className="page-subtitle">
            Everything you signed. What renews. What needs action this month.
          </div>
        </div>
        <div style={{display:'flex', gap:8}}>
          <button className="btn" onClick={() => go('contracts')}>
            <Icon name="stack"/> View all contracts
          </button>
          <button className="btn primary" onClick={() => setState(s => ({...s, uploadOpen: true}))}>
            <Icon name="upload"/> Upload documents
          </button>
        </div>
      </div>

      {/* KPIs */}
      <div className="kpi-row">
        <div className="kpi">
          <div className="label"><Icon name="stack" size={11}/> Active contracts</div>
          <div className="value">{activeTotal}</div>
          <div className="delta">across {window.FACILITIES.length - 1} facilities · <span className="up">+4</span> this quarter</div>
        </div>
        <div className="kpi">
          <div className="label"><Icon name="clock" size={11}/> Renew in 60 days</div>
          <div className="value">{renewing60.length}</div>
          <div className="delta"><span className="down">{renewing60.filter(c=>window.daysBetween(c.end)<=30).length} urgent</span> · under 30 days</div>
        </div>
        <div className="kpi">
          <div className="label"><Icon name="alert" size={11}/> Needs attention</div>
          <div className="value">{alertsFiltered.filter(a => a.severity !== 'info').length}</div>
          <div className="delta">{expired} expired · {alertsFiltered.filter(a=>a.severity==='warn').length} in notice window</div>
        </div>
        <div className="kpi">
          <div className="label"><Icon name="book" size={11}/> Tracked annual value</div>
          <div className="value">${(totalAnnual/1000).toFixed(0)}K</div>
          <div className="delta">extracted from {contracts.filter(c=>c.annualValue).length} contracts</div>
        </div>
      </div>

      {/* Main grid */}
      <div className="break-col" style={{marginTop: 20}}>
        <div className="stack-20">
          {/* Renewals timeline */}
          <div className="card">
            <div className="card-head">
              <h3>Upcoming renewals & notice windows</h3>
              <span className="sub">Next 120 days · {renewing60.length + contracts.filter(c=>{const d=window.daysBetween(c.end);return d>60&&d<=120;}).length} items</span>
            </div>
            <div className="card-body flush">
              <div className="timeline">
                {contracts
                  .filter(c => { const d = window.daysBetween(c.end); return d >= -14 && d <= 120; })
                  .sort((a,b) => window.daysBetween(a.end) - window.daysBetween(b.end))
                  .slice(0, 7)
                  .map(c => {
                    const d = window.daysBetween(c.end);
                    const urgency = d < 0 ? 'urgent' : d <= 30 ? 'urgent' : d <= 60 ? 'warn' : '';
                    return (
                      <div key={c.id} className={`tl-row ${urgency}`} onClick={() => go('detail', { contractId: c.id })}>
                        <div className="when">
                          <span className="big">{formatDate(c.end)}</span>
                          {c.renewal}
                        </div>
                        <div>
                          <div className="title">
                            <span className="counterparty-avatar">{c.counterparty.initials}</span>
                            {c.name}
                          </div>
                          <div className="meta">
                            {c.id} · <CategoryLabel id={c.category}/> · {window.FACILITIES.find(f=>f.id===c.facility)?.code} · notice {c.notice}
                          </div>
                        </div>
                        <div className="days">
                          <span className="n">{d < 0 ? `${Math.abs(d)}d` : `${d}d`}</span>
                          {d < 0 ? 'past term' : 'to term'}
                        </div>
                      </div>
                    );
                  })}
              </div>
            </div>
          </div>

          {/* By category */}
          <div className="card">
            <div className="card-head">
              <h3>Contracts by category</h3>
              <span className="sub">{facility === 'all' ? 'All facilities' : window.FACILITIES.find(f=>f.id===facility)?.name}</span>
            </div>
            <div className="card-body">
              {byCategory.map(cat => (
                <div className="bar-chart" key={cat.id}>
                  <div className="lab">{cat.label}</div>
                  <div className="track">
                    <div className="fill" style={{width: `${(cat.count/maxCount)*100}%`}}/>
                  </div>
                  <div className="val">{cat.count}</div>
                </div>
              ))}
            </div>
          </div>

          {/* Rate visibility — premium */}
          {tier === 'premium' ? (
            <div className="card">
              <div className="card-head">
                <h3>Payer rate visibility <span className="premium-badge">Premium</span></h3>
                <span className="sub">vs. CMS 2026 · ASC CPT 45380</span>
              </div>
              <div className="card-body">
                {[
                  { p: 'Aetna Commercial', rate: 1.42, bench: 1.35 },
                  { p: 'Regence BCBS', rate: 1.28, bench: 1.35 },
                  { p: 'UnitedHealthcare', rate: 1.18, bench: 1.35 },
                  { p: 'Cigna PPO', rate: 1.51, bench: 1.35 },
                  { p: 'Premera BC', rate: 1.33, bench: 1.35 },
                ].map(row => (
                  <div className="bar-chart accent" key={row.p}>
                    <div className="lab">{row.p}</div>
                    <div className="track" style={{height: 14}}>
                      <div className="fill" style={{width: `${(row.rate/2)*100}%`}}/>
                      <div style={{position:'absolute', top:0, bottom:0, left: `${(row.bench/2)*100}%`, width:1, background:'var(--ink-2)'}}/>
                    </div>
                    <div className="val">{row.rate.toFixed(2)}×</div>
                  </div>
                ))}
                <div style={{fontSize: 11, color: 'var(--ink-3)', marginTop: 10, fontFamily: 'var(--ff-mono)'}}>
                  Black line: CMS 2026 multiple · benchmark derived from 38 peer ASCs
                </div>
              </div>
            </div>
          ) : (
            <div className="card" style={{position: 'relative', overflow: 'hidden'}}>
              <div className="card-head">
                <h3>Payer rate visibility</h3>
                <span className="sub">Premium</span>
              </div>
              <div className="card-body locked" style={{minHeight: 180}}>
                {[1,2,3,4,5].map(i => (
                  <div className="bar-chart" key={i}>
                    <div className="lab" style={{background: 'var(--line-2)', height: 10, borderRadius: 2, width: '60%'}}/>
                    <div className="track"><div className="fill" style={{width: `${40 + i*8}%`, background:'var(--line)'}}/></div>
                    <div className="val" style={{color: 'var(--ink-4)'}}>—</div>
                  </div>
                ))}
                <div className="lock-overlay">
                  <div className="inner">
                    <div style={{fontFamily:'var(--ff-serif)', fontSize: 20, marginBottom: 6}}>See rate deltas vs. CMS multiple</div>
                    <div style={{fontSize: 12.5, color: 'var(--ink-3)', marginBottom: 14}}>
                      Per-CPT rate visibility and peer benchmarks — included in Premium.
                    </div>
                    <button className="btn" onClick={() => setState(s => ({...s, tier: 'premium'}))}>
                      <Icon name="sparkle" size={12}/> Preview Premium
                    </button>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* right column */}
        <div className="stack-20">
          {/* Alerts */}
          <div className="card">
            <div className="card-head">
              <h3>Alerts</h3>
              <span className="sub">{window.ALERTS.length} open</span>
            </div>
            <div className="card-body flush">
              {window.ALERTS.map(a => (
                <div key={a.id} className={`alert ${a.severity}`} onClick={() => a.contract && go('detail', { contractId: a.contract })}
                  style={{cursor: a.contract ? 'pointer' : 'default'}}>
                  <div className="bar"/>
                  <div>
                    <div className="head">{a.title}</div>
                    <div className="body">{a.body}</div>
                    <div className="meta">{a.meta}</div>
                  </div>
                  <button className="btn ghost sm" onClick={(e) => { e.stopPropagation(); }}>Snooze</button>
                </div>
              ))}
            </div>
          </div>

          {/* Obligations */}
          <div className="card">
            <div className="card-head">
              <h3>Obligations this month</h3>
              <span className="sub">{window.OBLIGATIONS.filter(o=>!o.done).length} open</span>
            </div>
            <div className="card-body flush">
              {window.OBLIGATIONS.map(o => {
                const d = window.daysBetween(o.due);
                const due = d < 0 ? `${Math.abs(d)}d overdue` : d === 0 ? 'Due today' : `In ${d}d`;
                return (
                  <div className={`oblig-row ${o.done ? 'done' : ''}`} key={o.id}>
                    <div className={`check ${o.done ? 'done' : ''}`}
                      onClick={() => {
                        o.done = !o.done; setState(s => ({...s, _t: Date.now()}));
                      }}>
                      {o.done && <Icon name="check" size={10}/>}
                    </div>
                    <div>
                      <div className="title">{o.title}</div>
                      <div className="due">
                        {o.contract && <span>{o.contract} · </span>}
                        {window.FACILITIES.find(f=>f.id===o.facility)?.code}
                      </div>
                    </div>
                    <div className="due" style={{color: d < 0 ? 'var(--accent)' : d <= 7 ? 'var(--warn)' : 'var(--ink-3)'}}>
                      {due}
                    </div>
                    <div className="due">{formatDate(o.due)}</div>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Recent activity */}
          <div className="card">
            <div className="card-head">
              <h3>Recent activity</h3>
              <span className="sub">Last 7 days</span>
            </div>
            <div className="card-body" style={{fontSize: 12.5}}>
              {[
                ['J. Okafor', 'uploaded', 'Regence Amendment #2', '2h ago'],
                ['AI extraction', 'completed on', 'V-00444 Epic Community Connect', '5h ago'],
                ['K. Sato', 'corrected end date on', 'E-00222 Northwest Anesthesia', 'Yesterday'],
                ['System', 'flagged', 'Waystar notice window closed', '2d ago'],
                ['R. Chen', 'acknowledged', 'Stericycle auto-renew', '3d ago'],
              ].map(([who, verb, what, when], i) => (
                <div key={i} style={{display:'flex', gap: 10, padding: '6px 0', borderBottom: i<4?'1px solid var(--line-2)':'none'}}>
                  <div style={{color:'var(--ink-3)', fontFamily:'var(--ff-mono)', fontSize: 10.5, width: 60, paddingTop: 2}}>{when}</div>
                  <div style={{flex:1}}>
                    <span style={{fontWeight: 500}}>{who}</span>{' '}
                    <span style={{color: 'var(--ink-3)'}}>{verb}</span>{' '}
                    <span>{what}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

window.Dashboard = Dashboard;
