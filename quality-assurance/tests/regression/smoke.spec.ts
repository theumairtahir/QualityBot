import { test, expect } from '@playwright/test';

// Critical flow #1: the shell comes up and talks to the API. Every other regression test assumes this.
// Locators follow the app shell spec (docs/superpowers/specs/2026-10-19-sis-app-shell-design.md):
// sidebar navigation, dashboard page, system-info card reading "Connected · v<version> · <environment>".
const apiUrl = process.env.QA_API_URL ?? 'http://localhost:8081';

test.describe('app shell', { tag: '@critical' }, () => {
  test('API reports healthy', async ({ request }) => {
    const res = await request.get(`${apiUrl}/health`);
    expect(res.ok()).toBeTruthy();
    expect(await res.text()).toBe('Healthy');
  });

  test('dashboard loads and shows the API as connected', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('navigation')).toBeVisible();
    await expect(page.getByText(/connected/i)).toBeVisible();
  });

  test('unknown route shows the not-found page instead of a blank screen', async ({ page }) => {
    await page.goto('/this-route-does-not-exist');
    // The title appears in the breadcrumb and header too; the <h1> is the unambiguous one.
    await expect(page.getByRole('heading', { name: /not found/i })).toBeVisible();
  });
});
