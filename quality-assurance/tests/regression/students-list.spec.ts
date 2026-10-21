import { test, expect } from '@playwright/test';

// Critical flow: issue #3 "View and search the student list" (PR #16).
// Relies on seed/002-students-list-edit.sql: "Qa Searchable" (Grade 3), "Zoë Qaumlaut", "Percent Qawildcard".
// Search is debounced (250 ms) and sent as GET /api/students?search=<term>; the server matches
// "<first> <last>" case-insensitively with %, _ and \ treated literally.
const apiUrl = process.env.QA_API_URL ?? 'http://localhost:8081';

const searchBox = (page: import('@playwright/test').Page) =>
  page.getByRole('searchbox', { name: 'Search students' });

test.describe('students: list and search', { tag: '@critical' }, () => {
  test('the list shows roll number, name and class for each student', async ({ page }) => {
    await page.goto('/students');
    // The Table component's AriaLabel="Students" is not exposed on the <table>; scope by test id instead.
    const table = page.getByTestId('student-list').getByRole('table');
    await expect(table.getByRole('columnheader', { name: 'Roll no.' })).toBeVisible();
    await expect(table.getByRole('columnheader', { name: 'Name' })).toBeVisible();
    await expect(table.getByRole('columnheader', { name: 'Class' })).toBeVisible();

    const row = page.getByRole('row', { name: /Qa Searchable/ });
    await expect(row).toBeVisible();
    // Roll numbers are assigned by the database, so only the shape is stable.
    await expect(row.getByRole('cell').first()).toHaveText(/^\d+$/);
    await expect(row).toContainText('Grade 3');
  });

  test('typing in the search box filters the list by name', async ({ page }) => {
    await page.goto('/students');
    await expect(page.getByRole('row', { name: /Zoë Qaumlaut/ })).toBeVisible();

    await searchBox(page).fill('searchable');

    await expect(page.getByRole('row', { name: /Qa Searchable/ })).toBeVisible();
    await expect(page.getByRole('row', { name: /Zoë Qaumlaut/ })).toHaveCount(0);
  });

  test('search is case-insensitive and spans first and last name', async ({ page }) => {
    await page.goto('/students');
    await searchBox(page).fill('QA SEARCH');
    await expect(page.getByRole('row', { name: /Qa Searchable/ })).toBeVisible();
    await expect(page.getByRole('row', { name: /Percent Qawildcard/ })).toHaveCount(0);
  });

  test('shows "No students found" when nothing matches and recovers when cleared', async ({ page }) => {
    await page.goto('/students');
    await searchBox(page).fill('zzzzqa');
    await expect(page.getByText('No students found')).toBeVisible();
    await expect(page.getByText('No student\'s name matches "zzzzqa".')).toBeVisible();
    await expect(page.getByTestId('student-list').getByRole('table')).toHaveCount(0);
    await expect(page.getByText('No students yet')).toHaveCount(0);

    await searchBox(page).fill('');
    await expect(page.getByRole('row', { name: /Zoë Qaumlaut/ })).toBeVisible();
    await expect(page.getByText('No students found')).toHaveCount(0);
  });

  test('a percent sign is searched literally, not as a wildcard', async ({ page }) => {
    await page.goto('/students');
    await expect(page.getByRole('row', { name: /Percent Qawildcard/ })).toBeVisible();
    await searchBox(page).fill('%');
    await expect(page.getByText('No students found')).toBeVisible();
    await expect(page.getByRole('row', { name: /Percent Qawildcard/ })).toHaveCount(0);
  });

  test('typing is debounced into fewer requests than keystrokes', async ({ page }) => {
    await page.goto('/students');
    await expect(page.getByRole('row', { name: /Zoë Qaumlaut/ })).toBeVisible();
    const searches: string[] = [];
    page.on('request', (r) => {
      if (r.url().includes('/api/students?search=')) searches.push(r.url());
    });

    await searchBox(page).pressSequentially('searchable');
    await expect(page.getByRole('row', { name: /Zoë Qaumlaut/ })).toHaveCount(0);
    await expect(page.getByRole('row', { name: /Qa Searchable/ })).toBeVisible();

    expect(searches.length).toBeLessThan('searchable'.length);
    expect(searches.at(-1)).toContain('search=searchable');
  });
});

test.describe('students: search API', () => {
  test('search matches case-insensitively across first and last name', async ({ request }) => {
    const res = await request.get(`${apiUrl}/api/students?search=${encodeURIComponent('QA SEARCH')}`);
    expect(res.status()).toBe(200);
    const names = (await res.json()).map((s: { firstName: string; lastName: string }) => `${s.firstName} ${s.lastName}`);
    expect(names).toContain('Qa Searchable');
    expect(names).not.toContain('Zoë Qaumlaut');
  });

  test('search is Unicode case-insensitive', async ({ request }) => {
    const res = await request.get(`${apiUrl}/api/students?search=${encodeURIComponent('ZOË QAUML')}`);
    const names = (await res.json()).map((s: { firstName: string; lastName: string }) => `${s.firstName} ${s.lastName}`);
    expect(names).toContain('Zoë Qaumlaut');
  });

  for (const term of ['%', '_', '\\', "'; drop table students;--"]) {
    test(`search for ${JSON.stringify(term)} is literal and harmless`, async ({ request }) => {
      // Names may only contain letters, spaces, hyphens and apostrophes, so none of these can match.
      const res = await request.get(`${apiUrl}/api/students?search=${encodeURIComponent(term)}`);
      expect(res.status()).toBe(200);
      expect(await res.json()).toEqual([]);
      const all = await request.get(`${apiUrl}/api/students`);
      expect(all.status()).toBe(200);
    });
  }

  test('a whitespace-only search returns the full list', async ({ request }) => {
    const all = (await (await request.get(`${apiUrl}/api/students`)).json()).length;
    const blank = (await (await request.get(`${apiUrl}/api/students?search=%20%20`)).json()).length;
    expect(blank).toBe(all);
  });

  test('every student carries a positive roll number, unique and assigned on creation', async ({ request }) => {
    const before = (await (await request.get(`${apiUrl}/api/students`)).json()) as { rollNumber: number }[];
    const rolls = before.map((s) => s.rollNumber);
    expect(rolls.every((r) => Number.isInteger(r) && r >= 1)).toBeTruthy();
    expect(new Set(rolls).size).toBe(rolls.length);

    const created = await request.post(`${apiUrl}/api/students`, {
      data: { firstName: 'Roll', lastName: 'Qarollcheck', className: 'Grade 1' },
    });
    expect(created.status()).toBe(201);
    const body = await created.json();
    expect(body.rollNumber).toBeGreaterThan(Math.max(0, ...rolls));
    await request.delete(`${apiUrl}/api/students/${body.id}`);
  });
});
