import sys

from playwright.sync_api import sync_playwright


EXPECTED_URL = (
    "https://downloads.jsoncool.com/windows/36.2-preview/"
    "0.1.0-alpha__p2-20260807-r1__build-4615b881f7/"
    "LearnHeartstone-Windows-x64-0.1.0-alpha__p2-20260807-r1__build-4615b881f7.zip"
)
BASE_URL = sys.argv[1].rstrip("/") if len(sys.argv) > 1 else "http://127.0.0.1:4173"


with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True)
    for width, height in ((375, 812), (1440, 900)):
        page = browser.new_page(viewport={"width": width, "height": height})
        console_errors = []
        page.on(
            "console",
            lambda message: console_errors.append(message.text)
            if message.type == "error"
            else None,
        )
        page.goto(f"{BASE_URL}/download")
        page.wait_for_load_state("networkidle")

        link = page.get_by_role("link", name="下载 Windows 版")
        assert link.count() == 1
        assert link.get_attribute("href") == EXPECTED_URL
        assert link.is_visible()
        link.focus()
        assert page.evaluate(
            "(element) => document.activeElement === element", link.element_handle()
        )

        bounds = link.bounding_box()
        assert bounds is not None and bounds["height"] >= 44
        assert page.locator("text=暂无可下载候选").count() == 0
        assert page.get_by_text("压缩包约 180 MB，下载后解压即可运行。").count() == 1
        for hidden_detail in ("SHA-256", "内容快照", "发布前必须同时具备"):
            assert page.get_by_text(hidden_detail, exact=True).count() == 0
        assert page.evaluate("document.documentElement.scrollWidth <= document.documentElement.clientWidth")
        assert not console_errors, console_errors
        page.close()

    browser.close()

print(f"DOWNLOAD_PAGE_SMOKE_OK base={BASE_URL} mobile=375x812 desktop=1440x900")
