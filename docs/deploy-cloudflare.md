# Cloudflare deployment runbook (mode A: real data behind Access)

End state:
- `https://app.practicex.net` -> Cloudflare Pages -> built from `apps/command-center` against `https://api.practicex.net`
- `https://api.practicex.net` -> Cloudflare Tunnel -> `https://localhost:7100` on the dev laptop
- Cloudflare Access in front of `app.practicex.net` -> email-OTP gate, whitelist-only
- Cloudflare Access in front of `api.practicex.net` -> service-token (so the browser can hit the API while logged in via Access)

Time budget: ~30 minutes.

> **PII reminder:** This deploys real Eagle GI customer data behind email-gated auth. Your whitelist is the only thing standing between this URL and a HIPAA exposure. Tighten the whitelist before clicking "Save policy."

---

## Prereqs

- Cloudflare account with `practicex.net` in your zone list (already done -- you confirmed).
- GitHub repo `rvadapally-synexar/PracticeX.App` accessible from your Cloudflare account.
- `cloudflared` installed locally. On Windows:
  ```powershell
  winget install --id Cloudflare.cloudflared
  ```
  Restart the terminal after install.

---

## Step 1 -- Connect the repo to Cloudflare Pages (~5 min)

1. Cloudflare dashboard -> **Workers & Pages** -> **Create application** -> **Pages** -> **Connect to Git**.
2. Select GitHub, authorize Cloudflare, pick `rvadapally-synexar/PracticeX.App`.
3. Project name: `practicex-app` (this becomes the `*.pages.dev` URL).
4. Production branch: `main`.
5. Build settings:
   - **Framework preset**: Vite
   - **Root directory**: `apps/command-center`
   - **Build command**: `npm install && npm run build`
   - **Build output directory**: `dist`
6. Environment variables (Production):
   - `VITE_API_BASE` = `https://api.practicex.net/api`
7. **Save and Deploy.** First build will fail to fetch the API (it doesn't exist yet) but the static assets will deploy at `https://practicex-app.pages.dev`.

---

## Step 2 -- Stand up the Cloudflare Tunnel for the API (~10 min)

The tunnel publishes `https://localhost:7100` (your laptop's API) at `https://api.practicex.net` -- without opening any port on your network.

```powershell
# 1. Authenticate cloudflared against your account (browser pops).
cloudflared tunnel login

# 2. Create a named tunnel. Cloudflare returns a tunnel UUID + writes
#    a credentials JSON to %USERPROFILE%\.cloudflared\<UUID>.json.
cloudflared tunnel create practicex-api

# 3. Add a DNS route so api.practicex.net resolves to this tunnel.
cloudflared tunnel route dns practicex-api api.practicex.net

# 4. Write the tunnel config. The hostname must match the DNS route above.
#    Replace <UUID> with the actual UUID from step 2.
@"
tunnel: <UUID>
credentials-file: $env:USERPROFILE\.cloudflared\<UUID>.json
ingress:
  - hostname: api.practicex.net
    service: https://localhost:7100
    originRequest:
      noTLSVerify: true
  - service: http_status:404
"@ | Out-File -Encoding utf8 $env:USERPROFILE\.cloudflared\config.yml

# 5. Run it (keep this terminal open while you want the demo accessible).
cloudflared tunnel run practicex-api
```

`noTLSVerify: true` is fine here because the tunnel terminates TLS at the edge -- the localhost-side cert is the .NET dev cert which Cloudflare doesn't trust by default.

To run as a background service later:
```powershell
cloudflared --config $env:USERPROFILE\.cloudflared\config.yml service install
```

---

## Step 3 -- Lock both URLs behind Cloudflare Access (~10 min, MANDATORY)

This is the security gate. Without it, anyone hitting the URLs sees Eagle GI's data.

### Set up the team

1. Cloudflare dashboard -> **Zero Trust** -> first-time setup creates a free team. Pick a team name (e.g. `practicex`).

### Create the application policies

1. **Zero Trust** -> **Access** -> **Applications** -> **Add an application** -> **Self-hosted**.
2. **Application name**: `PracticeX Command Center`.
3. **Subdomain**: `app` -- domain: `practicex.net`.
4. **Identity providers**: One-time PIN (default). Add Google / Microsoft later for SSO.
5. Click **Next**, then create a policy:
   - Policy name: `Whitelist - Eagle GI demo`.
   - Action: **Allow**.
   - Include rule: **Emails** -> add each whitelisted address one per line.
6. Save. Repeat the same for `api.practicex.net` with the **same** policy (or share via Access groups).

### Whitelist (only these emails can see the URL)

- `rvadapally@practicex.ai` (you)
- `<parag's email>`
- `<board members and others as needed>`

Test it: open the URL in an incognito window, you should get an email-OTP prompt.

---

## Step 4 -- Verify end-to-end (~5 min)

1. Make sure your laptop has the API running: `dotnet run --launch-profile https --project src/PracticeX.Api/PracticeX.Api.csproj`
2. Make sure cloudflared tunnel is running: `cloudflared tunnel run practicex-api` in a separate terminal.
3. Open `https://app.practicex.net/portfolio` in incognito.
4. Email-OTP gate -> enter whitelisted email -> get code -> proceed.
5. Portfolio loads with the 18 Eagle GI documents.

If it doesn't work, check in this order:
- `https://api.practicex.net/api/system/info` returns `{"product":"PracticeX Command Center"...}`. If 502, the tunnel is down or the API isn't running.
- Browser devtools network tab: any CORS error means I need to add the actual origin to `Cors.AllowedOrigins` in `appsettings.json`.
- Cloudflare Access logs (Zero Trust -> Logs -> Authentication) show every login attempt.

---

## When you want to take the demo down

```powershell
# Stop the tunnel (URL still resolves but 502s).
# In the cloudflared terminal: Ctrl+C.

# Or kill access entirely by removing the Allow policy:
# Zero Trust -> Access -> Applications -> Edit -> remove the policy -> Save.
# Now everyone (including you) gets blocked.
```

To take the public URL fully offline: Cloudflare Pages -> project -> **Pause** the deployment.

---

## Future hardening (don't bother for tonight)

- Service token on the API tunnel so the browser passes a bearer token instead of relying on Access cookies.
- mTLS on the tunnel for stronger origin auth.
- Move the API off the laptop into Azure App Service / Container Apps so the demo works without you online.
- Move the Cloudflare zone from the personal Gmail account onto a `practicex.ai` account so PracticeX Inc owns the domain rather than Raghu personally.
