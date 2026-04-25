// Tweaks panel

function TweaksPanel({ state, setState, visible, onClose }) {
  if (!visible) return null;

  const set = (k, v) => {
    setState(s => ({ ...s, [k]: v }));
    try {
      window.parent.postMessage({ type: '__edit_mode_set_keys', edits: { [k]: v } }, '*');
    } catch(e){}
  };

  return (
    <div className="tweaks-panel">
      <h4>
        <span>Tweaks</span>
        <span className="x" onClick={onClose}>×</span>
      </h4>

      <div className="tweak">
        <div className="lab">Theme</div>
        <div className="seg">
          <button className={state.theme==='operator'?'active':''} onClick={() => set('theme','operator')}>Paper</button>
          <button className={state.theme==='clinical'?'active':''} onClick={() => set('theme','clinical')}>Clinical</button>
          <button className={state.theme==='dark'?'active':''} onClick={() => set('theme','dark')}>Dark</button>
        </div>
      </div>

      <div className="tweak">
        <div className="lab">Density</div>
        <div className="seg">
          <button className={state.density==='comfortable'?'active':''} onClick={() => set('density','comfortable')}>Comfortable</button>
          <button className={state.density==='compact'?'active':''} onClick={() => set('density','compact')}>Compact</button>
        </div>
      </div>

      <div className="tweak">
        <div className="lab">Product tier</div>
        <div className="seg">
          <button className={state.tier==='basic'?'active':''} onClick={() => set('tier','basic')}>Basic</button>
          <button className={state.tier==='premium'?'active':''} onClick={() => set('tier','premium')}>Premium</button>
        </div>
      </div>

      <div style={{fontSize: 10.5, color: 'var(--ink-4)', fontFamily: 'var(--ff-mono)', letterSpacing: '.05em', marginTop: 10}}>
        Basic = upload, extract, search, monitor.<br/>
        Premium adds rate visibility, benchmarks, optimization.
      </div>
    </div>
  );
}

window.TweaksPanel = TweaksPanel;

// Theme swatch overrides applied via data-theme
(function setupThemes() {
  const styles = document.createElement('style');
  styles.textContent = `
    [data-theme="clinical"] {
      --bg: #F5F7F9;
      --surface: #FFFFFF;
      --surface-2: #FAFBFC;
      --ink: #0E1820;
      --ink-2: #2D3A46;
      --ink-3: #5E6D7A;
      --ink-4: #9AA5AF;
      --line: #E1E6EB;
      --line-2: #ECEFF3;
      --accent: #0E6DB0;
      --accent-soft: #DCEAF5;
      --ok: #2C7A4B;
      --ok-soft: #DDEEE2;
      --warn: #A65D00;
      --warn-soft: #F7E5CC;
      --danger: #B32B1F;
      --danger-soft: #F6D9D4;
    }
    [data-theme="clinical"] .doc-body { background: #E6EBF0; }
    [data-theme="clinical"] .hl { background: #DCEAF5; }
    [data-theme="clinical"] .hl.active { background: #0E6DB0; }
    [data-theme="clinical"] .brand-mark { background: #0E1820; }
  `;
  document.head.appendChild(styles);
})();
