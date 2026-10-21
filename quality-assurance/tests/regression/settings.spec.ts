import { test, expect, type Page } from '@playwright/test';

// Critical flow: issue #14 "Set the school profile" (PR #17). One school_profile row is shared by
// the whole app, so the tests in this file run in order in one worker ('default' mode, unlike
// 'serial', does not skip the rest after a failure); each one saves the state it asserts on and none
// depends on another's leftovers.
test.describe.configure({ mode: 'default' });

const apiUrl = process.env.QA_API_URL ?? 'http://localhost:8081';
const unique = () => {
  const n = Date.now() * 1000 + Math.floor(Math.random() * 1000);
  let s = '';
  for (let v = n; v > 0; v = Math.floor(v / 26)) s = String.fromCharCode(97 + (v % 26)) + s;
  return `Qa ${s[0].toUpperCase()}${s.slice(1)} Academy`;
};
// The sidebar is rendered twice (mobile drawer + desktop); only one copy is visible.
const header = (page: Page) => page.locator('[data-testid="school-name"]:visible');
const subtitle = (page: Page) => page.locator('[data-testid="school-subtitle"]:visible');
const nameInput = (page: Page) => page.locator('input[name="school-name"]');
const yearInput = (page: Page) => page.locator('input[name="academic-year"]');

async function openSettings(page: Page) {
  await page.goto('/settings');
  await expect(page.getByTestId('school-form')).toBeVisible();
}

test.describe('settings: school profile', { tag: '@critical' }, () => {
  test('sidebar Settings link opens the school profile form', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('navigation', { name: 'Main' }).getByRole('link', { name: 'Settings' }).click();
    await expect(page).toHaveURL(/\/settings$/);
    await expect(page.getByRole('heading', { name: 'Settings', level: 1 })).toBeVisible();
    await expect(page.getByTestId('school-form')).toBeVisible();
  });

  test('saving name and year updates the sidebar header without a reload and persists', async ({ page }) => {
    const name = unique();
    await openSettings(page);
    await nameInput(page).fill(`  ${name}  `);
    await yearInput(page).fill(' 2026-2027 ');
    await page.getByTestId('save-settings').click();
    await expect(page.getByText('Settings saved', { exact: true })).toBeVisible();
    await expect(page.getByText(`${name} is now the school name.`, { exact: true })).toBeVisible();
    await expect(header(page)).toHaveText(name);
    await expect(subtitle(page)).toHaveText('Student Information System · 2026-2027');

    await page.goto('/students');
    await expect(header(page)).toHaveText(name);
    await expect(subtitle(page)).toHaveText('Student Information System · 2026-2027');
    const res = await page.request.get(`${apiUrl}/api/school`);
    expect(await res.json()).toEqual({ name, academicYear: '2026-2027' });
  });

  test('clearing the academic year removes it from the sidebar subtitle', async ({ page }) => {
    const name = unique();
    await openSettings(page);
    await nameInput(page).fill(name);
    await yearInput(page).fill('');
    await page.getByTestId('save-settings').click();
    await expect(header(page)).toHaveText(name);
    await expect(subtitle(page)).toHaveText('Student Information System');
    const res = await page.request.get(`${apiUrl}/api/school`);
    expect(await res.json()).toEqual({ name, academicYear: null });
  });

  test('a blank school name and a malformed year are rejected without calling the API', async ({ page }) => {
    await openSettings(page);
    const puts: string[] = [];
    page.on('request', (r) => { if (r.method() === 'PUT') puts.push(r.url()); });
    await nameInput(page).fill('   ');
    await yearInput(page).fill('2026-2028');
    await page.getByTestId('save-settings').click();
    await expect(page.getByText('School name is required.')).toBeVisible();
    await expect(page.getByText('Use two consecutive years, like 2026-2027.')).toBeVisible();
    expect(puts).toHaveLength(0);
  });
});

test.describe('settings: API rules', { tag: '@critical' }, () => {
  test('API requires a name and limits it to 100 characters', async ({ request }) => {
    for (const name of [null, '', '   ']) {
      const res = await request.put(`${apiUrl}/api/school`, { data: { name, academicYear: null } });
      expect(res.status()).toBe(400);
      expect((await res.json()).errors.Name).toEqual(['School name is required.']);
    }
    const tooLong = await request.put(`${apiUrl}/api/school`, { data: { name: 'a'.repeat(101) } });
    expect(tooLong.status()).toBe(400);
    expect((await tooLong.json()).errors.Name).toEqual(['School name must be at most 100 characters.']);
    const exact = await request.put(`${apiUrl}/api/school`, { data: { name: `  ${'a'.repeat(100)}  ` } });
    expect(exact.status()).toBe(200);
    expect((await exact.json()).name).toBe('a'.repeat(100));
  });

  for (const year of ['2026', '2026-27', '2026/2027', '2026-2028', '2027-2026', '2026–2027', 'abcd-efgh']) {
    test(`API rejects academic year "${year}"`, async ({ request }) => {
      const res = await request.put(`${apiUrl}/api/school`, { data: { name: 'Year Rule Academy', academicYear: year } });
      expect(res.status()).toBe(400);
      expect((await res.json()).errors.AcademicYear).toEqual(['Use two consecutive years, like 2026-2027.']);
    });
  }

  test('API renders an HTML name as text in the header', async ({ page, request }) => {
    const name = '<img src=x onerror=alert(1)>';
    expect((await request.put(`${apiUrl}/api/school`, { data: { name, academicYear: null } })).status()).toBe(200);
    await page.goto('/');
    await expect(header(page)).toHaveText(name);
    await expect(header(page).locator('img')).toHaveCount(0);
  });
});

test.describe('settings: open bugs', () => {
  // Bug academic-year-unicode-digits-crash (PR #17): regex \d matches non-ASCII digits, int.Parse throws.
  test('API rejects an academic year written in non-ASCII digits with 400', async ({ request }) => {
    test.fail(true, 'academic-year-unicode-digits-crash: API returns 500');
    const res = await request.put(`${apiUrl}/api/school`, { data: { name: 'Digits Academy', academicYear: '٢٠٢٦-٢٠٢٧' } });
    expect(res.status()).toBe(400);
  });

  test('UI shows a validation message for an academic year in non-ASCII digits', async ({ page }) => {
    test.fail(true, 'academic-year-unicode-digits-crash: the page crashes into the error boundary');
    await openSettings(page);
    await nameInput(page).fill('Digits Academy');
    await yearInput(page).fill('２０２６-２０２７');
    await page.getByTestId('save-settings').click();
    await expect(page.getByText('Use two consecutive years, like 2026-2027.')).toBeVisible({ timeout: 5000 });
  });

  // Known issue form-labels-not-associated, which also affects /settings (PR #17).
  test('settings inputs are associated with their labels', async ({ page }) => {
    test.fail(true, 'form-labels-not-associated: Id is applied to the wrapper and the input');
    await openSettings(page);
    // getByLabel would resolve to the wrapper div that carries the duplicate id; ask for the textbox.
    await expect(page.getByRole('textbox', { name: 'School name' })).toBeVisible({ timeout: 3000 });
    await expect(page.getByRole('textbox', { name: 'Academic year' })).toBeVisible({ timeout: 3000 });
  });
});
