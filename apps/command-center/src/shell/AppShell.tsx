import { Bell, Building2, Clock, FileStack, Home, Search, Settings, ShieldAlert, Sparkles, Upload, Users, Zap } from 'lucide-react';
import { NavLink, Outlet } from 'react-router-dom';

const facilities = [
  { code: 'NOR', name: 'Northside' },
  { code: 'LKV', name: 'Lakeview GI' },
  { code: 'EVG', name: 'Evergreen ASC' },
  { code: 'PUG', name: 'Puget' },
];

const workspaceItems = [
  { to: '/dashboard', label: 'Command center', icon: Home },
  { to: '/contracts', label: 'Contracts', icon: FileStack, count: 20 },
  { to: '/renewals', label: 'Renewals', icon: Clock, count: 8 },
  { to: '/alerts', label: 'Alerts', icon: ShieldAlert, count: 6 },
  { to: '/obligations', label: 'Obligations', icon: Sparkles, count: 6 },
  { to: '/review', label: 'Review queue', icon: Sparkles, count: 4 },
  { to: '/sources', label: 'Source discovery', icon: Upload },
];

export function AppShell() {
  return (
    <div className="app" data-theme="operator" data-density="comfortable">
      <header className="topbar">
        <div className="brand">
          <div className="brand-mark" aria-hidden="true" />
          <span className="brand-name">PracticeX Command Center</span>
        </div>
        <button className="facility-switch" type="button">
          <Building2 size={13} />
          <span className="mono-label">Facility</span>
          <strong>All facilities</strong>
        </button>
        <div className="topbar-spacer" />
        <div className="cmdk" role="search">
          <Search size={13} />
          <span>Search contracts, counterparties, facilities...</span>
          <kbd>⌘K</kbd>
        </div>
        <button className="px-icon-button" type="button" aria-label="Notifications">
          <Bell size={15} />
        </button>
        <button className="px-icon-button" type="button" aria-label="Settings">
          <Settings size={15} />
        </button>
        <div className="px-avatar" title="Jordan Okafor">JO</div>
      </header>
      <aside className="sidebar">
        <section className="nav-section">
          <h2 className="section-label">Workspace</h2>
          {workspaceItems.map((item) => (
            <NavLink className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`} key={item.to} to={item.to}>
              <item.icon size={14} />
              <span>{item.label}</span>
              {item.count ? <span className="nav-count">{item.count}</span> : null}
            </NavLink>
          ))}
        </section>
        <section className="nav-section">
          <h2 className="section-label">Facilities</h2>
          <div className="facility-list">
            {facilities.map((facility) => (
              <button className="facility-pill" key={facility.code} type="button">
                <span className="facility-code">{facility.code}</span>
                <span>{facility.name}</span>
              </button>
            ))}
            <button className="facility-pill active" type="button">
              <span className="facility-code">ALL</span>
              <span>Portfolio view</span>
            </button>
          </div>
        </section>
        <section className="nav-section">
          <h2 className="section-label">Intelligence · locked</h2>
          <NavLink className="nav-item" to="/rates">
            <Zap size={14} />
            <span>Rate visibility</span>
            <span className="nav-count">PRO</span>
          </NavLink>
        </section>
        <div style={{ flex: 1 }} />
        <section className="nav-section">
          <h2 className="section-label">Admin</h2>
          <NavLink className="nav-item" to="/admin">
            <Users size={14} />
            <span>Team & roles</span>
          </NavLink>
        </section>
        <div className="plan-card">
          <div className="section-label" style={{ marginLeft: 0 }}>Plan</div>
          <strong>Growth · Basic</strong>
          <div className="muted" style={{ marginTop: 4 }}>100 contracts · 5 facilities</div>
        </div>
      </aside>
      <main className="main">
        <Outlet />
      </main>
    </div>
  );
}

