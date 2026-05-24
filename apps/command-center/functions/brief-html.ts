/**
 * Pages Function: /brief-html
 *
 * Server-renders the Practice Intelligence Brief as a self-contained HTML
 * page suitable for embedding in an <iframe> from the React app. This
 * exists because iPad Chrome (CriOS 148 on iOS 26.3) intermittently fails
 * the SPA's fetch() call to /api/analysis/portfolio-brief with
 * "TypeError: Load failed" — a WebKit bug that affects fetch() but does
 * NOT affect top-level navigations (which iframes use). Routing the
 * brief through an iframe sidesteps the bug entirely.
 *
 * Query params:
 *   ?facility=<uuid>          optional facility filter
 *   ?tenantOverride=<uuid>    optional tenant scope for super-admin
 *
 * Returns:
 *   200 text/html  full self-contained page with the brief rendered
 *   200 text/html  "Generate brief" CTA when API returns 404
 *   200 text/html  error placeholder when API returns 5xx
 */
interface BriefEnv {
  CF_ACCESS_CLIENT_ID?: string;
  CF_ACCESS_CLIENT_SECRET?: string;
}

interface BriefDto {
  briefMd: string;
  model?: string | null;
  sourceDocCount: number;
  tokensIn?: number;
  tokensOut?: number;
  latencyMs?: number;
  generatedAt: string;
}

function pageShell(bodyHtml: string, opts: { briefJsonForRender?: string } = {}): string {
  const briefJson = opts.briefJsonForRender ?? 'null';
  return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Practice Intelligence Brief</title>
<style>
  :root {
    --px-bg: #f7f3ea;
    --px-ink: #2b2a25;
    --px-muted: #6c6a60;
    --px-accent: #1d6f42;
    --px-orange: #d4631e;
  }
  * { box-sizing: border-box; }
  html, body { margin: 0; padding: 0; background: var(--px-bg); color: var(--px-ink); }
  body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", system-ui, sans-serif; line-height: 1.6; padding: 24px; }
  .meta { color: var(--px-muted); font-size: 12px; margin-bottom: 18px; letter-spacing: .5px; text-transform: uppercase; }
  .empty { padding: 40px; text-align: center; color: var(--px-muted); border: 1px solid #e2dccd; border-radius: 10px; background: #fff; }
  .empty h2 { font-family: "Times New Roman", Georgia, serif; font-weight: 600; margin: 0 0 8px; color: var(--px-ink); }
  article { background: #fff; padding: 28px 32px; border: 1px solid #e2dccd; border-radius: 10px; max-width: 900px; margin: 0 auto; }
  article h1 { font-family: "Times New Roman", Georgia, serif; font-size: 28px; margin: 0 0 8px; }
  article h2 { font-family: "Times New Roman", Georgia, serif; font-size: 22px; margin: 28px 0 8px; padding-top: 12px; border-top: 1px solid #f0ebdb; color: var(--px-accent); }
  article h3 { font-size: 16px; margin: 18px 0 6px; color: var(--px-orange); }
  article p { margin: 10px 0; }
  article ul, article ol { padding-left: 22px; }
  article li { margin: 4px 0; }
  article code { background: #f0ebdb; padding: 1px 6px; border-radius: 3px; font-size: 90%; }
  article strong { color: var(--px-ink); }
  article hr { border: 0; border-top: 1px solid #e2dccd; margin: 20px 0; }
  article blockquote { border-left: 3px solid var(--px-accent); margin: 12px 0; padding: 4px 12px; color: var(--px-muted); }
  article table { width: 100%; border-collapse: collapse; margin: 12px 0; font-size: 13px; }
  article th, article td { border: 1px solid #e2dccd; padding: 6px 10px; text-align: left; }
  article th { background: #f0ebdb; }
</style>
</head>
<body>
${bodyHtml}
<script>
  // marked.js — minimal markdown→HTML inline. Self-contained to avoid
  // any external CDN dependency from inside the iframe.
  // (~3KB compact subset: headings, bold/italic, lists, code, links, hr, blockquote, tables)
  function renderMarkdown(md) {
    if (!md) return '';
    var lines = md.split(/\\r?\\n/);
    var out = [];
    var inCode = false, codeLang = '';
    var inList = false, listType = '';
    var inTable = false, tableRows = [];
    function flushList() { if (inList) { out.push('</' + listType + '>'); inList = false; listType = ''; } }
    function flushTable() {
      if (!inTable) return;
      var html = '<table>';
      if (tableRows.length) {
        html += '<thead><tr>' + tableRows[0].map(function(c){return '<th>'+escapeHtml(c)+'</th>';}).join('') + '</tr></thead>';
        html += '<tbody>';
        for (var i = 2; i < tableRows.length; i++) {
          html += '<tr>' + tableRows[i].map(function(c){return '<td>'+inline(c)+'</td>';}).join('') + '</tr>';
        }
        html += '</tbody>';
      }
      html += '</table>';
      out.push(html);
      inTable = false; tableRows = [];
    }
    function escapeHtml(s) { return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }
    function inline(s) {
      s = escapeHtml(s);
      s = s.replace(/\\*\\*([^*]+)\\*\\*/g, '<strong>$1</strong>');
      s = s.replace(/\\*([^*]+)\\*/g, '<em>$1</em>');
      s = s.replace(/\`([^\`]+)\`/g, '<code>$1</code>');
      s = s.replace(/\\[([^\\]]+)\\]\\(([^)]+)\\)/g, '<a href="$2" target="_blank" rel="noopener">$1</a>');
      return s;
    }
    for (var i = 0; i < lines.length; i++) {
      var line = lines[i];
      if (/^\`\`\`/.test(line)) {
        if (inCode) { out.push('</code></pre>'); inCode = false; }
        else { flushList(); flushTable(); codeLang = line.slice(3).trim(); out.push('<pre><code>'); inCode = true; }
        continue;
      }
      if (inCode) { out.push(escapeHtml(line)); continue; }
      if (/^\\|.*\\|$/.test(line)) {
        if (!inTable) { flushList(); inTable = true; tableRows = []; }
        var cells = line.replace(/^\\||\\|$/g, '').split('|').map(function(c){return c.trim();});
        tableRows.push(cells);
        continue;
      } else if (inTable) { flushTable(); }
      if (/^---+$/.test(line) || /^\\*\\*\\*+$/.test(line)) { flushList(); out.push('<hr>'); continue; }
      var h = line.match(/^(#{1,6})\\s+(.+)$/);
      if (h) { flushList(); out.push('<h' + h[1].length + '>' + inline(h[2]) + '</h' + h[1].length + '>'); continue; }
      if (/^>\\s+/.test(line)) { flushList(); out.push('<blockquote>' + inline(line.replace(/^>\\s+/,'')) + '</blockquote>'); continue; }
      var ul = line.match(/^[-*]\\s+(.+)$/);
      var ol = line.match(/^(\\d+)\\.\\s+(.+)$/);
      if (ul) {
        if (!inList || listType !== 'ul') { flushList(); out.push('<ul>'); inList = true; listType = 'ul'; }
        out.push('<li>' + inline(ul[1]) + '</li>'); continue;
      }
      if (ol) {
        if (!inList || listType !== 'ol') { flushList(); out.push('<ol>'); inList = true; listType = 'ol'; }
        out.push('<li>' + inline(ol[2]) + '</li>'); continue;
      }
      flushList();
      if (line.trim() === '') { out.push(''); continue; }
      out.push('<p>' + inline(line) + '</p>');
    }
    flushList(); flushTable();
    if (inCode) out.push('</code></pre>');
    return out.join('\\n');
  }

  var brief = ${briefJson};
  if (brief && brief.briefMd) {
    var art = document.getElementById('brief-article');
    if (art) art.innerHTML = renderMarkdown(brief.briefMd);
  }
</script>
</body>
</html>`;
}

export const onRequest: PagesFunction<BriefEnv> = async ({ request, env }) => {
  const url = new URL(request.url);
  const facility = url.searchParams.get('facility');
  const tenantOverride = url.searchParams.get('tenantOverride');

  // Build upstream URL
  const upstream = new URL('https://api.practicex.ai/api/analysis/portfolio-brief');
  if (facility) upstream.searchParams.set('facility', facility);
  if (tenantOverride) upstream.searchParams.set('tenantOverride', tenantOverride);
  upstream.searchParams.set('_t', String(Date.now()));

  const apiHeaders: Record<string, string> = { Accept: 'application/json' };
  if (env.CF_ACCESS_CLIENT_ID && env.CF_ACCESS_CLIENT_SECRET) {
    apiHeaders['CF-Access-Client-Id'] = env.CF_ACCESS_CLIENT_ID;
    apiHeaders['CF-Access-Client-Secret'] = env.CF_ACCESS_CLIENT_SECRET;
  }

  let apiResp: Response;
  try {
    apiResp = await fetch(upstream.toString(), { headers: apiHeaders, redirect: 'manual' });
  } catch (e) {
    return htmlResponse(pageShell(`<div class="empty"><h2>Couldn't reach the workspace</h2><p>${(e as Error).message}</p></div>`));
  }

  if (apiResp.status === 404) {
    return htmlResponse(pageShell(`
      <div class="empty">
        <div class="meta">PREMIUM · CROSS-DOCUMENT SYNTHESIS</div>
        <h2>Practice Intelligence Brief</h2>
        <p>No brief has been generated for this scope yet.</p>
      </div>
    `));
  }

  if (!apiResp.ok) {
    return htmlResponse(pageShell(`
      <div class="empty">
        <h2>Brief temporarily unavailable</h2>
        <p>API returned status ${apiResp.status}. Refresh in a moment.</p>
      </div>
    `));
  }

  let dto: BriefDto;
  try {
    dto = await apiResp.json();
  } catch (e) {
    return htmlResponse(pageShell(`<div class="empty"><h2>Brief response malformed</h2><p>${(e as Error).message}</p></div>`));
  }

  const generatedDate = new Date(dto.generatedAt).toLocaleString();
  const meta = `${dto.sourceDocCount} docs synthesised · ${dto.model ?? 'authored'} · ${generatedDate}`;
  return htmlResponse(pageShell(`
    <article>
      <div class="meta">PREMIUM · CROSS-DOCUMENT SYNTHESIS — ${escapeHtmlServer(meta)}</div>
      <div id="brief-article"></div>
    </article>
  `, { briefJsonForRender: JSON.stringify(dto) }));
};

function htmlResponse(html: string): Response {
  return new Response(html, {
    status: 200,
    headers: {
      'Content-Type': 'text/html; charset=utf-8',
      'Cache-Control': 'no-store, no-cache, must-revalidate',
      'X-Frame-Options': 'SAMEORIGIN',
    },
  });
}

function escapeHtmlServer(s: string): string {
  return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}
