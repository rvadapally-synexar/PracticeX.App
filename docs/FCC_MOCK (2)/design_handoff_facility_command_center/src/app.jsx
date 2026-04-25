// App shell — sidebar, topbar, view switcher

function App() {
  const [state, setState] = useState(() => {
    const saved = localStorage.getItem('fcc-state');
    const base = {
      view: 'dashboard',
      facility: 'nor',
      contractId: null,
      uploadOpen: false,
      tweaksOpen: false,
      theme: window.TWEAKS.theme || 'operator',
      density: window.TWEAKS.density || 'comfortable',
      tier: window.TWEAKS.tier || 'basic',
    };
    if (saved) {
      try { return { ...base, ...JSON.parse(saved), uploadOpen: false }; } catch(e) {}
    }
    return base;
  });

  useEffect(() => {
    localStorage.setItem('fcc-state', JSON.stringify({
      view: state.view, facility: state.facility, contractId: state.contractId,
      theme: state.theme, density: state.density, tier: state.tier,
    }));
  }, [state.view, state.facility, state.contractId, state.theme, state.density, state.tier]);

  useEffect(() => {
    document.body.setAttribute('data-theme', state.theme);
    document.body.setAttribute('data-density', state.density);
  }, [state.theme, state.density]);

  // Edit-mode listener (Tweaks)
  useEffect(() => {
    const onMsg = (e) => {
      if (e.data?.type === '__activate_edit_mode') setState(s => ({...s, tweaksOpen: true}));
      if (e.data?.type === '__deactivate_edit_mode') setState(s => ({...s, tweaksOpen: false}));
    };
    window.addEventListener('message', onMsg);
    try { window.parent.postMessage({type: '__edit_mode_available'}, '*'); } catch(e) {}
    return () => window.removeEventListener('message', onMsg);
  }, []);

  const facility = window.FACILITIES.find(f => f.id === state.facility);
  const [facMenu, setFacMenu] = useState(false);

  const navItems = [
    { id: 'dashboard', label: 'Command center', icon: 'home' },
    { id: 'contracts', label: 'Contracts', icon: 'stack', count: window.CONTRACTS.length },
    { id: 'renewals',  label: 'Renewals', icon: 'clock', count: window.CONTRACTS.filter(c => { const d = window.daysBetween(c.end); return d > 0 && d <= 120; }).length },
    { id: 'alerts',    label: 'Alerts', icon: 'alert', count: window.ALERTS.length },
    { id: 'obligations', label: 'Obligations', icon: 'check', count: window.OBLIGATIONS.filter(o=>!o.done).length },
    { id: 'review',    label: 'Review queue', icon: 'sparkle', count: 4 },
  ];
  const premiumItems = [
    { id: 'rates', label: 'Rate visibility', icon: 'zap', premium: true },
    { id: 'benchmarks', label: 'Benchmarks', icon: 'sparkle', premium: true },
  ];
  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          <div className="brand-mark"/>
          <span className="brand-name">Facility Command Center</span>
        </div>

        <div style={{position: 'relative'}}>
          <div className="facility-switch" onClick={() => setFacMenu(v => !v)}>
            <Icon name="building" size={12}/>
            <span style={{color: 'var(--ink-3)', fontFamily: 'var(--ff-mono)', fontSize: 10.5, textTransform: 'uppercase', letterSpacing: '.06em'}}>Facility</span>
            <span style={{fontWeight: 500}}>{facility?.name}</span>
            <span className="chev"><Icon name="chev" size={12}/></span>
          </div>
          {facMenu && (
            <div style={{position: 'absolute', top: 38, left: 0, background: 'var(--surface)', border: '1px solid var(--line)', borderRadius: 'var(--radius)', padding: 4, minWidth: 280, zIndex: 20, boxShadow: '0 10px 30px rgba(0,0,0,.1)'}}>
              {window.FACILITIES.map(f => (
                <div key={f.id}
                  onClick={() => { setState(s => ({...s, facility: f.id})); setFacMenu(false); }}
                  style={{padding: '8px 10px', fontSize: 12.5, cursor: 'pointer', borderRadius: 'var(--radius)', display: 'flex', justifyContent: 'space-between',
                    background: f.id === state.facility ? 'var(--surface-2)' : 'transparent'}}>
                  <span>
                    <span className="counterparty-avatar">{f.code}</span>
                    {f.name}
                  </span>
                  <span style={{color: 'var(--ink-4)', fontSize: 11}}>{f.city}</span>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="topbar-spacer"/>

        <div className="cmdk" onClick={() => setState(s => ({...s, view: 'contracts'}))}>
          <Icon name="search" size={12}/>
          <span>Search contracts, counterparties, facilities…</span>
          <kbd>⌘K</kbd>
        </div>
        <button className="icon-btn" title="Notifications"><Icon name="bell"/></button>
        <button className="icon-btn" title="Settings"><Icon name="settings"/></button>
        <div className="avatar" title="Jordan Okafor, ASC Administrator">JO</div>
      </header>

      <aside className="sidebar">
        <div className="nav-section">
          <h5>Workspace</h5>
          {navItems.map(n => (
            <div key={n.id}
              className={`nav-item ${state.view === n.id ? 'active' : ''}`}
              onClick={() => setState(s => ({...s, view: n.id, contractId: null}))}>
              <Icon name={n.icon}/> <span>{n.label}</span>
              {n.count !== undefined && <span className="nav-count">{n.count}</span>}
            </div>
          ))}
        </div>

        <div className="nav-section">
          <h5>Facilities</h5>
          {window.FACILITIES.filter(f => f.id !== 'all').map(f => (
            <div key={f.id}
              className={`fac-pill ${state.facility === f.id ? 'active' : ''}`}
              onClick={() => setState(s => ({...s, facility: f.id}))}>
              <span className="fac-code">{f.code}</span>
              <span>{f.name.replace(' Associates','').replace(' Endoscopy Center','').replace(' Digestive Clinic','')}</span>
            </div>
          ))}
          <div className={`fac-pill ${state.facility === 'all' ? 'active' : ''}`} onClick={() => setState(s => ({...s, facility: 'all'}))}>
            <span className="fac-code">ALL</span><span>Portfolio view</span>
          </div>
        </div>

        <div className="nav-section">
          <h5>Intelligence {state.tier === 'premium' ? '' : '· locked'}</h5>
          {premiumItems.map(n => (
            <div key={n.id}
              className="nav-item"
              onClick={() => setState(s => ({...s, view: n.id}))}
              style={{opacity: state.tier === 'premium' ? 1 : 0.6, cursor: 'pointer'}}>
              <Icon name={n.icon}/> <span>{n.label}</span>
              <span className="nav-count"><span className="premium-badge">PRO</span></span>
            </div>
          ))}
        </div>

        <div style={{flex: 1}}/>

        <div className="nav-section" style={{marginBottom: 0}}>
          <h5>Admin</h5>
          <div className="nav-item"><Icon name="user"/> <span>Team & roles</span></div>
          <div className="nav-item"><Icon name="settings"/> <span>Settings</span></div>
        </div>

        <div style={{marginTop: 12, padding: 10, border: '1px solid var(--line)', borderRadius: 'var(--radius)', fontSize: 11, color: 'var(--ink-3)'}}>
          <div style={{fontFamily: 'var(--ff-mono)', fontSize: 10, letterSpacing: '.08em', textTransform: 'uppercase', marginBottom: 4, color: 'var(--ink-4)'}}>Plan</div>
          <div style={{fontWeight: 500, color: 'var(--ink)', fontSize: 12.5, marginBottom: 4}}>
            {state.tier === 'premium' ? 'Scale · Premium' : 'Growth · Basic'}
          </div>
          <div>{state.tier === 'premium' ? '250 contracts · 10 facilities' : '100 contracts · 5 facilities'}</div>
        </div>
      </aside>

      <main className="main">
        {state.view === 'dashboard'   && <Dashboard state={state} setState={setState}/>}
        {state.view === 'contracts'   && <Contracts state={state} setState={setState}/>}
        {state.view === 'renewals'    && <Contracts state={{...state, initialCategory: 'all'}} setState={(s)=>setState(s)}/>}
        {state.view === 'alerts'      && <AlertsView state={state} setState={setState}/>}
        {state.view === 'obligations' && <ObligationsView state={state} setState={setState}/>}
        {state.view === 'detail'      && <ContractDetail state={state} setState={setState}/>}
        {state.view === 'review'      && <ExtractionReview state={state} setState={setState}/>}
        {(state.view === 'rates' || state.view === 'benchmarks') && <RateBenchmark state={state} setState={setState}/>}
      </main>

      {state.uploadOpen && (
        <UploadModal
          onClose={() => setState(s => ({...s, uploadOpen: false}))}
          onDone={() => setState(s => ({...s, uploadOpen: false, view: 'review'}))}
        />
      )}

      <TweaksPanel state={state} setState={setState} visible={state.tweaksOpen} onClose={() => setState(s => ({...s, tweaksOpen: false}))}/>

      {!state.tweaksOpen && (
        <button className="tweaks-fab" onClick={() => setState(s => ({...s, tweaksOpen: true}))}>
          <Icon name="settings" size={14}/> Tweaks
        </button>
      )}
    </div>
  );
}

// --- Alerts & Obligations full pages ---
function AlertsView({ state, setState }) {
  return (
    <div className="page">
      <div className="crumb">
        <a onClick={() => setState(s => ({...s, view: 'dashboard'}))}>Command center</a>
        <span className="sep">›</span>
        <span>Alerts</span>
      </div>
      <div className="page-head">
        <div>
          <div className="eyebrow"><span className="bullet"/><span>Monitoring</span></div>
          <h1 className="page-title">Alerts</h1>
          <div className="page-subtitle">{window.ALERTS.length} open · {window.ALERTS.filter(a=>a.severity==='danger').length} urgent</div>
        </div>
      </div>
      <div className="card">
        <div className="card-body flush">
          {window.ALERTS.map(a => (
            <div key={a.id} className={`alert ${a.severity}`} onClick={() => a.contract && setState(s => ({...s, view: 'detail', contractId: a.contract}))} style={{cursor: a.contract ? 'pointer' : 'default'}}>
              <div className="bar"/>
              <div>
                <div className="head">{a.title}</div>
                <div className="body">{a.body}</div>
                <div className="meta">{a.meta}</div>
              </div>
              <div style={{display:'flex', gap:6}}>
                <button className="btn sm ghost" onClick={e => e.stopPropagation()}>Snooze</button>
                <button className="btn sm" onClick={e => e.stopPropagation()}>Acknowledge</button>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function ObligationsView({ state, setState }) {
  const [_, force] = useState(0);
  return (
    <div className="page">
      <div className="crumb">
        <a onClick={() => setState(s => ({...s, view: 'dashboard'}))}>Command center</a>
        <span className="sep">›</span>
        <span>Obligations</span>
      </div>
      <div className="page-head">
        <div>
          <div className="eyebrow"><span className="bullet"/><span>What needs action</span></div>
          <h1 className="page-title">Obligations</h1>
          <div className="page-subtitle">Derived from your contracts + compliance calendar</div>
        </div>
      </div>
      <div className="card">
        <div className="card-body flush">
          {window.OBLIGATIONS.map(o => {
            const d = window.daysBetween(o.due);
            const due = d < 0 ? `${Math.abs(d)}d overdue` : d === 0 ? 'Due today' : `Due in ${d}d`;
            return (
              <div className={`oblig-row ${o.done ? 'done' : ''}`} key={o.id} style={{gridTemplateColumns: '16px 1fr 110px 140px 120px'}}>
                <div className={`check ${o.done ? 'done' : ''}`}
                  onClick={() => { o.done = !o.done; force(x => x+1); }}>
                  {o.done && <Icon name="check" size={10}/>}
                </div>
                <div>
                  <div className="title">{o.title}</div>
                  <div className="due">
                    {o.contract && <a style={{color: 'var(--accent)', cursor:'pointer'}} onClick={() => setState(s => ({...s, view: 'detail', contractId: o.contract}))}>{o.contract}</a>}
                  </div>
                </div>
                <div className="due"><FacilityBadge id={o.facility}/></div>
                <div className="due" style={{color: d < 0 ? 'var(--accent)' : d <= 7 ? 'var(--warn)' : 'var(--ink-3)'}}>
                  {due}
                </div>
                <div className="due">{formatDate(o.due)}</div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}

window.App = App;
ReactDOM.createRoot(document.getElementById('root')).render(<App/>);
