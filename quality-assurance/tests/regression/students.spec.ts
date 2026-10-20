import { test, expect } from '@playwright/test';

// Critical flow: issue #2 "Add a new student". Each test is independent and uses a unique name.
const unique = () => `QA${Date.now()}${Math.floor(Math.random() * 1000)}`;

test.describe('students: add', { tag: '@critical' }, () => {
  test('required fields are validated', async ({ page }) => {
    await page.goto('/students/new');
    await page.getByTestId('save-student').click();
    await expect(page.getByText('First name is required.')).toBeVisible();
    await expect(page.getByText('Last name is required.')).toBeVisible();
    await expect(page.getByText('Class is required.')).toBeVisible();
  });

  test('an invalid email is rejected', async ({ page }) => {
    await page.goto('/students/new');
    await page.locator('input[name="first-name"]').fill('Ada');
    await page.locator('input[name="last-name"]').fill('Lovelace');
    await page.locator('input[name="email"]').fill('not-an-email');
    await page.locator('select[name="class-name"]').selectOption('Grade 5');
    await page.getByTestId('save-student').click();
    await expect(page.getByText('Enter a valid email address.')).toBeVisible();
  });

  test('saving a student shows a success message and lists the student', async ({ page }) => {
    const last = unique();
    await page.goto('/students/new');
    await page.locator('input[name="first-name"]').fill('Grace');
    await page.locator('input[name="last-name"]').fill(last);
    await page.locator('select[name="class-name"]').selectOption('Grade 4');
    await page.getByTestId('save-student').click();
    await expect(page.getByText('Student added', { exact: true })).toBeVisible();
    await expect(page).toHaveURL(/\/students$/);
    await expect(page.getByRole('row', { name: new RegExp(`Grace ${last}`) })).toBeVisible();
  });

  test('API rejects an email longer than 254 characters with 400, not 500', async ({ request }) => {
    // Open bug (PR #15, email-too-long-500): expected to fail until fixed; remove this line then.
    test.fail(true, 'known bug: API returns 500');
    const apiUrl = process.env.QA_API_URL ?? 'http://localhost:8081';
    const res = await request.post(`${apiUrl}/api/students`, {
      data: { firstName: 'Long', lastName: unique(), className: 'Grade 1', email: `${'a'.repeat(250)}@example.com` },
    });
    expect(res.status()).toBe(400);
  });
});
