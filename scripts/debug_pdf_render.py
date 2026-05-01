"""
Reproduces the PDF inline rendering failure for a small PDF.
Loads the document detail page, watches network + console, screenshots
what the <object> renders.
"""
from playwright.sync_api import sync_playwright
import os

SVC_FILE = os.path.expanduser("~/.cf-practicex-svc.txt")
SVC_HEADERS: dict[str, str] = {}
with open(SVC_FILE) as fh:
    for line in fh:
        k, _, v = line.strip().partition("=")
        if k == "CLIENT_ID":
            SVC_HEADERS["CF-Access-Client-Id"] = v
        elif k == "CLIENT_SECRET":
            SVC_HEADERS["CF-Access-Client-Secret"] = v

# 02_brahmbhatt = small (260KB) — should render inline easily
SMALL_DOC_ID = "cc9022ac-e677-4167-835a-1a6cd0b75797"
PAGE_URL = f"https://practicex-app.pages.dev/portfolio/{SMALL_DOC_ID}"
PDF_URL = f"https://practicex-app.pages.dev/api/analysis/documents/{SMALL_DOC_ID}/content"

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    context = browser.new_context(extra_http_headers=SVC_HEADERS)
    page = context.new_page()

    network_log = []
    page.on("request", lambda r: network_log.append(("REQ", r.method, r.url[:120])))
    page.on("response", lambda r: network_log.append(("RES", r.status, r.url[:120], dict(r.headers).get("content-type", "-"))))
    page.on("console", lambda m: print(f"  console[{m.type}] {m.text[:200]}"))
    page.on("pageerror", lambda e: print(f"  PAGE_ERROR: {e}"))

    print(f"Navigating to {PAGE_URL}")
    page.goto(PAGE_URL, wait_until="networkidle", timeout=30000)
    print(f"Loaded. Title: {page.title()}")

    # Find the <object> element
    obj = page.query_selector("object")
    if obj:
        print(f"\n<object> found:")
        print(f"  data: {obj.get_attribute('data')}")
        print(f"  type: {obj.get_attribute('type')}")
        # Check if children are visible (means <object> failed)
        fallback_visible = page.query_selector("object .document-source-fallback")
        if fallback_visible and fallback_visible.is_visible():
            print(f"  FALLBACK IS VISIBLE — <object> refused to render PDF")
            print(f"  Fallback text: {fallback_visible.inner_text()[:200]}")
        else:
            print(f"  PDF appears to be rendering (fallback not visible)")
    else:
        print(f"\nNo <object> element found")
        page.screenshot(path="C:/HareKrishna/Raghu/PracticeX/PracticeX.App/scripts/debug_no_object.png")

    print(f"\nNetwork log for /content URL:")
    for entry in network_log:
        if "/content" in entry[2]:
            print(f"  {entry}")

    page.screenshot(path="C:/HareKrishna/Raghu/PracticeX/PracticeX.App/scripts/debug_pdf_render.png", full_page=True)
    print(f"\nScreenshot saved.")
    browser.close()
