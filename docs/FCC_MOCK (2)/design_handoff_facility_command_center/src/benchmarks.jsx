// Payer benchmark data — Premium "killer screen"
// CPT-level commercial payer rates for the practice's top procedures,
// compared to regional percentiles (from public Transparency-in-Coverage
// machine-readable files) and CMS 2026 ASC rates.

// Our facility's top ASC GI procedures
const CPT_CATALOG = [
  { code: '43235', desc: 'EGD diagnostic, with or without collection', cms2026: 385.42, volume: 412 },
  { code: '43239', desc: 'EGD with biopsy, single or multiple',           cms2026: 419.88, volume: 624 },
  { code: '45378', desc: 'Colonoscopy, diagnostic',                        cms2026: 461.15, volume: 287 },
  { code: '45380', desc: 'Colonoscopy with biopsy, single or multiple',   cms2026: 495.73, volume: 918 },
  { code: '45385', desc: 'Colonoscopy with lesion removal by snare',      cms2026: 721.36, volume: 402 },
  { code: '45391', desc: 'Colonoscopy with endoscopic US examination',    cms2026: 812.04, volume: 68 },
];

// Payers we hold contracts with
const BENCH_PAYERS = ['Aetna Commercial', 'Regence BlueShield', 'UnitedHealthcare', 'Cigna PPO', 'Premera Blue Cross'];

// Contracted rates (your ASC's actual negotiated rates) per payer × CPT
// Market data = regional Puget Sound ASC allowed-amounts (median/p10/p90)
// from public MRF files — PayerPrice-style benchmark.
const BENCHMARKS = {
  '43235': {
    contracted: { 'Aetna Commercial': 542, 'Regence BlueShield': 488, 'UnitedHealthcare': 454, 'Cigna PPO': 578, 'Premera Blue Cross': 511 },
    regional:   { median: 520, p10: 438, p90: 612, cmsMultiple: 1.35, sampleSize: 38 },
  },
  '43239': {
    contracted: { 'Aetna Commercial': 596, 'Regence BlueShield': 538, 'UnitedHealthcare': 494, 'Cigna PPO': 628, 'Premera Blue Cross': 559 },
    regional:   { median: 567, p10: 478, p90: 668, cmsMultiple: 1.35, sampleSize: 38 },
  },
  '45378': {
    contracted: { 'Aetna Commercial': 655, 'Regence BlueShield': 591, 'UnitedHealthcare': 544, 'Cigna PPO': 691, 'Premera Blue Cross': 614 },
    regional:   { median: 623, p10: 525, p90: 734, cmsMultiple: 1.35, sampleSize: 42 },
  },
  '45380': {
    contracted: { 'Aetna Commercial': 704, 'Regence BlueShield': 635, 'UnitedHealthcare': 585, 'Cigna PPO': 745, 'Premera Blue Cross': 660 },
    regional:   { median: 669, p10: 562, p90: 788, cmsMultiple: 1.35, sampleSize: 44 },
  },
  '45385': {
    contracted: { 'Aetna Commercial': 1024, 'Regence BlueShield': 924, 'UnitedHealthcare': 850, 'Cigna PPO': 1083, 'Premera Blue Cross': 961 },
    regional:   { median: 974, p10: 820, p90: 1148, cmsMultiple: 1.35, sampleSize: 39 },
  },
  '45391': {
    contracted: { 'Aetna Commercial': 1154, 'Regence BlueShield': 1041, 'UnitedHealthcare': 958, 'Cigna PPO': 1222, 'Premera Blue Cross': 1082 },
    regional:   { median: 1096, p10: 923, p90: 1292, cmsMultiple: 1.35, sampleSize: 28 },
  },
};

// Computed: for each (payer, cpt) — variance vs regional median, Medicare multiple, annualized $ impact
function computeVariance() {
  const rows = [];
  for (const cpt of CPT_CATALOG) {
    const b = BENCHMARKS[cpt.code];
    if (!b) continue;
    for (const payer of BENCH_PAYERS) {
      const rate = b.contracted[payer];
      const median = b.regional.median;
      const delta = rate - median;
      const pct = (rate / median - 1) * 100;
      const cmsMx = rate / cpt.cms2026;
      // Annualized opportunity: assume payer holds ~22% of volume on average
      const payerShare = { 'Aetna Commercial': 0.26, 'Regence BlueShield': 0.22, 'UnitedHealthcare': 0.18, 'Cigna PPO': 0.14, 'Premera Blue Cross': 0.20 }[payer];
      const annualImpact = -delta * cpt.volume * payerShare;  // positive = gap / opportunity
      rows.push({
        cpt: cpt.code, desc: cpt.desc, payer, rate, median,
        p10: b.regional.p10, p90: b.regional.p90,
        delta, pct, cmsMx, benchCmsMx: b.regional.cmsMultiple,
        volume: Math.round(cpt.volume * payerShare), annualImpact,
        sampleSize: b.regional.sampleSize,
      });
    }
  }
  return rows;
}

window.CPT_CATALOG = CPT_CATALOG;
window.BENCH_PAYERS = BENCH_PAYERS;
window.BENCHMARKS = BENCHMARKS;
window.computeVariance = computeVariance;
