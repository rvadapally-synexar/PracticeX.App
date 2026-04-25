// Contracts list — search, filter, table

function Contracts({ state, setState }) {
  const [query, setQuery] = useState('');
  const [cat, setCat] = useState(state.initialCategory || 'all');
  const [status, setStatus] = useState('all');
  const [sort, setSort] = useState('end-asc');

  const filt = window.CONTRACTS.filter(c => {
    if (state.facility !== 'all' && c.facility !== state.facility && c.facility !== 'all') return false;
    if (cat !== 'all' && c.category !== cat) return false;
    if (status !== 'all') {
      const s = window.statusFor(c.end);
      if (status === 'active' && s !== 'active') return false;
      if (status === 'renewing' && !s.startsWith('renewal')) return false;
      if (status === 'expired' && s !== 'expired') return false;
    }
    if (query) {
      const q = query.toLowerCase();
      const hay = `${c.name} ${c.counterparty.name} ${c.id} ${c.owner}`.toLowerCase();
      if (!hay.includes(q)) return false;
    }
    return true;
  }).sort((a,b) => {
    if (sort === 'end-asc') return window.daysBetween(a.end) - window.daysBetween(b.end);
    if (sort === 'name') return a.name.localeCompare(b.name);
    if (sort === 'value') return (b.annualValue||0) - (a.annualValue||0);
    return 0;
  });

  const totalValue = filt.reduce((s,c) => s + (c.annualValue||0), 0);

  return (
    <div className="page">
      <div className="crumb">
        <a onClick={() => setState(s => ({...s, view: 'dashboard'}))}>Command center</a>
        <span className="sep">›</span>
        <span>Contracts</span>
      </div>
      <div className="page-head">
        <div>
          <div className="eyebrow"><span className="bullet"/><span>Repository</span></div>
          <h1 className="page-title">Contracts</h1>
          <div className="page-subtitle">
            {filt.length} of {window.CONTRACTS.length} contracts
            {totalValue > 0 && <> · <span style={{fontFamily:'var(--ff-mono)'}}>${(totalValue/1000).toFixed(0)}K tracked annual value</span></>}
          </div>
        </div>
        <div style={{display:'flex', gap:8}}>
          <button className="btn"><Icon name="download"/> Export</button>
          <button className="btn primary" onClick={() => setState(s => ({...s, uploadOpen: true}))}>
            <Icon name="upload"/> Upload
          </button>
        </div>
      </div>

      <div className="filter-bar">
        <div className="search-input">
          <Icon name="search"/>
          <input
            placeholder="Search by counterparty, document name, ID, owner…"
            value={query}
            onChange={e => setQuery(e.target.value)}
          />
          {query && <span onClick={() => setQuery('')} style={{cursor:'pointer', color:'var(--ink-4)'}}><Icon name="x" size={12}/></span>}
        </div>
        <div className="filter-pill">
          <span className="lab">Category</span>
          <select value={cat} onChange={e => setCat(e.target.value)} style={{border:'none', background:'none', outline:'none'}}>
            <option value="all">All</option>
            {window.CATEGORIES.map(c => <option key={c.id} value={c.id}>{c.label}</option>)}
          </select>
        </div>
        <div className="filter-pill">
          <span className="lab">Status</span>
          <select value={status} onChange={e => setStatus(e.target.value)} style={{border:'none', background:'none', outline:'none'}}>
            <option value="all">All</option>
            <option value="active">Active</option>
            <option value="renewing">Renewing (0–120d)</option>
            <option value="expired">Expired</option>
          </select>
        </div>
        <div className="filter-pill">
          <span className="lab">Sort</span>
          <select value={sort} onChange={e => setSort(e.target.value)} style={{border:'none', background:'none', outline:'none'}}>
            <option value="end-asc">End date ↑</option>
            <option value="name">Name</option>
            <option value="value">Annual value</option>
          </select>
        </div>
        <div style={{flex: 1}}/>
        <div className="kbd-hint"><kbd style={{padding:'2px 5px', background:'var(--surface)', border:'1px solid var(--line)', borderRadius: 3}}>⌘K</kbd> to search</div>
      </div>

      <div className="card" style={{overflow: 'hidden'}}>
        <div style={{maxHeight: 'calc(100vh - 350px)', overflow: 'auto'}}>
          <table className="tbl">
            <thead>
              <tr>
                <th style={{width: 90}}>ID</th>
                <th>Document</th>
                <th style={{width: 110}}>Category</th>
                <th style={{width: 80}}>Facility</th>
                <th style={{width: 130}}>End date</th>
                <th style={{width: 130}}>Renewal</th>
                <th style={{width: 110}} className="right">Annual value</th>
                <th style={{width: 150}}>Status</th>
                <th style={{width: 90}}>Owner</th>
              </tr>
            </thead>
            <tbody>
              {filt.map(c => {
                const d = window.daysBetween(c.end);
                return (
                  <tr key={c.id} onClick={() => setState(s => ({...s, view: 'detail', contractId: c.id}))}>
                    <td className="mono muted">{c.id}</td>
                    <td className="primary-col">
                      <span className="counterparty-avatar">{c.counterparty.initials}</span>
                      {c.name}
                      {c.flags && c.flags.length > 0 && (
                        <span style={{marginLeft: 8, fontSize: 10.5, fontFamily:'var(--ff-mono)', color:'var(--accent)', letterSpacing:'.04em'}}>
                          · {c.flags[0]}
                        </span>
                      )}
                    </td>
                    <td className="muted"><CategoryLabel id={c.category}/></td>
                    <td><FacilityBadge id={c.facility}/></td>
                    <td className="mono">
                      {formatDate(c.end)}
                      <span style={{color: d < 0 ? 'var(--accent)' : d <= 30 ? 'var(--accent)' : d <= 90 ? 'var(--warn)' : 'var(--ink-4)', marginLeft: 8, fontSize: 10.5}}>
                        {d < 0 ? `-${Math.abs(d)}d` : `${d}d`}
                      </span>
                    </td>
                    <td className="muted" style={{fontSize: 11.5}}>{c.renewal}</td>
                    <td className="mono right">{c.annualValue ? `$${(c.annualValue/1000).toFixed(0)}K` : '—'}</td>
                    <td>{statusChip(c)}</td>
                    <td className="muted" style={{fontSize: 11.5}}>{c.owner}</td>
                  </tr>
                );
              })}
              {filt.length === 0 && (
                <tr><td colSpan="9" style={{padding: 40, textAlign: 'center', color: 'var(--ink-3)'}}>
                  No contracts match these filters.
                </td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

window.Contracts = Contracts;
