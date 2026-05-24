/**
 * Cloudflare Pages Function — same-origin /api proxy.
 *
 * Routes any request to https://app.practicex.ai/api/* through to the
 * api.practicex.ai tunnel, so the browser sees only same-origin traffic.
 *
 * Auth posture (Slice 16, finalized after #5):
 *   - app.practicex.ai is gated by Cloudflare Access (email-OTP).
 *   - api.practicex.ai is gated by Cloudflare Access with a single allowed
 *     identity: a service token. The proxy below carries the token's
 *     CF-Access-Client-Id + CF-Access-Client-Secret headers, so users who
 *     reach this function via the authenticated UI shell get through.
 *     Direct hits to api.practicex.ai (anyone who knows the URL) get 403.
 *   - The two secrets live as encrypted Pages env vars and are never
 *     present in source.
 */
interface ProxyEnv {
  CF_ACCESS_CLIENT_ID?: string;
  CF_ACCESS_CLIENT_SECRET?: string;
}

export const onRequest: PagesFunction<ProxyEnv> = async ({ request, env }) => {
  const url = new URL(request.url);

  // Defensive: reject if somehow not /api/*
  if (!url.pathname.startsWith('/api/')) {
    return new Response('Not found', { status: 404 });
  }

  // Short-circuit CORS preflight (OPTIONS) requests. The React app sends
  // X-Tenant-Override on every API call when the super-admin org-switcher
  // is set; that custom header makes the browser issue a preflight before
  // the actual GET. Forwarding the OPTIONS upstream is pointless — the API
  // doesn't have an OPTIONS handler, and Cloudflare Access intercepts
  // anyway and 302s to the OTP login (preflights have no credentials).
  // The browser then sees a 3xx on the preflight and blocks the real
  // fetch with "TypeError: Failed to fetch", which the React app catches
  // as a generic network error (no status) and renders as the "Stay tuned"
  // maintenance card. Respond to the preflight ourselves with a 204 + the
  // headers the browser is asking permission to send.
  if (request.method === 'OPTIONS') {
    const reqOrigin = request.headers.get('origin') ?? '';
    const reqHeaders = request.headers.get('access-control-request-headers') ?? '';
    return new Response(null, {
      status: 204,
      headers: {
        'Access-Control-Allow-Origin': reqOrigin,
        'Access-Control-Allow-Credentials': 'true',
        'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, PATCH, OPTIONS',
        'Access-Control-Allow-Headers':
          reqHeaders || 'content-type, accept, x-tenant-override, x-impersonate-email',
        'Access-Control-Max-Age': '86400',
        'Vary': 'origin, access-control-request-headers',
      },
    });
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

  // Cloudflare Access service-token credentials. Presence of both is what
  // lets us pass through the api.practicex.ai Access app's Allow policy.
  // Missing values surface as 403 from upstream — caller will see the
  // failure rather than us silently pretending things are fine.
  if (env.CF_ACCESS_CLIENT_ID && env.CF_ACCESS_CLIENT_SECRET) {
    headers.set('CF-Access-Client-Id', env.CF_ACCESS_CLIENT_ID);
    headers.set('CF-Access-Client-Secret', env.CF_ACCESS_CLIENT_SECRET);
  }

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

  // Stamp explicit CORS headers. This is technically a same-origin proxy
  // (browser at app.practicex.ai fetching app.practicex.ai/api/*) but the
  // OTP-authenticated Cloudflare Access path appears to mark the response
  // in a way that browsers treat as cross-origin for fetch() — empirically,
  // direct address-bar navigation succeeds while the React fetch fails
  // with "Load failed" (Safari) / "Failed to fetch" (Chrome/Firefox), all
  // of which are silent CORS-block error patterns. Adding the explicit
  // headers makes the check pass unconditionally. Service-token-auth path
  // doesn't have this problem, which is why our Playwright probes worked.
  const reqOrigin = request.headers.get('origin');
  if (reqOrigin) {
    respHeaders.set('access-control-allow-origin', reqOrigin);
    respHeaders.set('access-control-allow-credentials', 'true');
    respHeaders.set('access-control-allow-headers',
      'content-type, accept, x-tenant-override, x-impersonate-email');
    respHeaders.set('vary', 'origin');
  }

  // For text/JSON responses, buffer fully and re-emit as ArrayBuffer with an
  // explicit Content-Length. iPad Safari was emitting "Load failed" on the
  // 30KB+ portfolio-brief response when we passed `response.body` through as
  // a stream — buffering eliminates whatever Content-Length / chunked-encoding
  // quirk Cloudflare's Pages runtime introduces between the upstream and the
  // browser. PDFs and other binary still stream.
  const ct = (response.headers.get('content-type') || '').toLowerCase();
  const shouldBuffer =
    ct.includes('application/json') ||
    ct.includes('text/') ||
    ct.includes('application/problem+json');

  if (shouldBuffer) {
    const buf = await response.arrayBuffer();
    respHeaders.set('content-length', String(buf.byteLength));
    // CRITICAL: response.arrayBuffer() returns ALREADY-DECOMPRESSED bytes
    // because the Fetch API transparently handles content-encoding. If we
    // re-emit the buffer while keeping the original Content-Encoding: gzip
    // header, the browser tries to decompress decompressed bytes → fails.
    // On iPad Chrome 148 this surfaces as TypeError: Load failed; desktop
    // browsers happen to be more forgiving but it's still wrong everywhere.
    // Strip the encoding-related headers so the buffered body is delivered
    // as the plain bytes it actually is.
    respHeaders.delete('content-encoding');
    respHeaders.delete('transfer-encoding');
    return new Response(buf, {
      status: response.status,
      statusText: response.statusText,
      headers: respHeaders,
    });
  }

  return new Response(response.body, {
    status: response.status,
    statusText: response.statusText,
    headers: respHeaders,
  });
};
