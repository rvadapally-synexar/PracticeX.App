/**
 * Cloudflare Pages Function — same-origin /api proxy.
 *
 * Routes any request to https://app.practicex.ai/api/* through to the
 * api.practicex.ai tunnel, so the browser sees only same-origin traffic.
 * Eliminates the cross-origin auth dance that broke inline PDF rendering
 * and the cookie scoping issues we hit in Slice 12-13.
 *
 * Auth posture: api.practicex.ai is no longer behind Cloudflare Access
 * (Slice 15). The user-facing surface (app.practicex.ai) IS behind Access,
 * so anyone hitting this proxy must already have authenticated for the
 * UI app. Anyone hitting api.practicex.ai directly bypasses Access but
 * has no UI surface to do anything useful with - the API only exposes
 * tenant-scoped endpoints. Service-token-gated upgrade lined up for
 * post-demo hardening.
 */
export const onRequest: PagesFunction = async ({ request }) => {
  const url = new URL(request.url);

  // Defensive: reject if somehow not /api/*
  if (!url.pathname.startsWith('/api/')) {
    return new Response('Not found', { status: 404 });
  }

  const upstream = `https://api.practicex.ai${url.pathname}${url.search}`;

  const headers = new Headers(request.headers);
  headers.delete('host');
  headers.delete('cf-connecting-ip');
  headers.delete('cf-ipcountry');
  headers.delete('cf-ray');
  headers.delete('cf-visitor');
  headers.delete('x-forwarded-host');
  headers.delete('x-forwarded-proto');

  // Tag the proxied request so downstream logging can distinguish
  // browser-direct vs Pages-proxied calls.
  headers.set('x-practicex-proxy', 'pages-function');

  const init: RequestInit = {
    method: request.method,
    headers,
    redirect: 'manual',
  };
  if (request.method !== 'GET' && request.method !== 'HEAD') {
    init.body = request.body;
    // Ensure stream-body fetches don't choke on missing duplex hint
    (init as RequestInit & { duplex?: string }).duplex = 'half';
  }

  const response = await fetch(upstream, init);

  // Strip cf-* headers we don't want browsers to see.
  const respHeaders = new Headers(response.headers);
  respHeaders.delete('cf-cache-status');
  respHeaders.delete('cf-ray');
  respHeaders.delete('alt-svc');
  respHeaders.delete('nel');
  respHeaders.delete('report-to');

  return new Response(response.body, {
    status: response.status,
    statusText: response.statusText,
    headers: respHeaders,
  });
};
