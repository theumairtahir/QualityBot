import { test, expect } from '@playwright/test';

// Critical flow: issue #2 "Add a new student" plus the team's name rules (PR #15 thread).
// Each test is independent and uses a unique, letters-only surname: names may contain only
// letters, spaces, hyphens and apostrophes (digits are rejected since commit c446da6).
const unique = () => {
  const n = Date.now() * 1000 + Math.floor(Math.random() * 1000);
  let s = '';
  for (let v = n; v > 0; v = Math.floor(v / 26)) s = String.fromCharCode(97 + (v % 26)) + s;
  return `Qa${s[0].toUpperCase()}${s.slice(1)}`;
};
const apiUrl = process.env.QA_API_URL ?? 'http://localhost:8081';
const isoDate = (d: Date) => d.toISOString().slice(0, 10);

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
    await page.locator('input[name="email"]').fill(`${last.toLowerCase()}@qa.example`);
    await page.locator('select[name="class-name"]').selectOption('Grade 4');
    await page.getByTestId('save-student').click();
    await expect(page.getByText('Student added', { exact: true })).toBeVisible();
    // The toast text also exists in a screen-reader live region; exact match keeps the locator unique.
    await expect(page.getByText(`Grace ${last} is now on record.`, { exact: true })).toBeVisible();
    await expect(page).toHaveURL(/\/students$/);
    const row = page.getByRole('row', { name: new RegExp(`Grace ${last}`) });
    await expect(row).toBeVisible();
    await expect(row).toContainText('Grade 4');
    await expect(row).toContainText(`${last.toLowerCase()}@qa.example`);
  });

  test('cancel returns to the list without saving', async ({ page }) => {
    const last = unique();
    await page.goto('/students/new');
    await page.locator('input[name="first-name"]').fill('Nobody');
    await page.locator('input[name="last-name"]').fill(last);
    await page.locator('select[name="class-name"]').selectOption('Grade 1');
    await page.getByRole('link', { name: 'Cancel' }).or(page.getByRole('button', { name: 'Cancel' })).first().click();
    await expect(page).toHaveURL(/\/students$/);
    await expect(page.getByRole('row', { name: new RegExp(`Nobody ${last}`) })).toHaveCount(0);
  });
});

test.describe('students: name rules', { tag: '@critical' }, () => {
  test('UI rejects digits and special characters in first and last name', async ({ page }) => {
    await page.goto('/students/new');
    await page.locator('input[name="first-name"]').fill('J0hn@#');
    await page.locator('input[name="last-name"]').fill('QAui123!');
    await page.locator('select[name="class-name"]').selectOption('Grade 3');
    await page.getByTestId('save-student').click();
    await expect(page.getByText('First name can only contain letters, spaces, hyphens and apostrophes.')).toBeVisible();
    await expect(page.getByText('Last name can only contain letters, spaces, hyphens and apostrophes.')).toBeVisible();
    await expect(page).toHaveURL(/\/students\/new$/);
  });

  test('UI trims surrounding spaces silently and saves the trimmed name', async ({ page }) => {
    const last = unique();
    await page.goto('/students/new');
    await page.locator('input[name="first-name"]').fill('  Trim  ');
    await page.locator('input[name="last-name"]').fill(`  ${last}  `);
    await page.locator('select[name="class-name"]').selectOption('Grade 5');
    await page.getByTestId('save-student').click();
    await expect(page.getByText(`Trim ${last} is now on record.`, { exact: true })).toBeVisible();
    // Since PR #16 the row starts with the roll number, so anchor on the name cell, not on the row start.
    await expect(page.getByRole('cell', { name: `Trim ${last}`, exact: true })).toBeVisible();
  });

  for (const bad of ['John3', 'Ann@!#', '-Ann', 'Smith_Two']) {
    test(`API rejects name "${bad}"`, async ({ request }) => {
      const res = await request.post(`${apiUrl}/api/students`, {
        data: { firstName: bad, lastName: 'Valid', className: 'Grade 1' },
      });
      expect(res.status()).toBe(400);
      expect((await res.json()).errors.FirstName[0]).toBe(
        'First name can only contain letters, spaces, hyphens and apostrophes.',
      );
    });
  }

  for (const good of ['Anne-Marie', "O'Brien", 'O’Neil', 'Zoë', 'José María', 'محمد']) {
    test(`API accepts real-world name "${good}"`, async ({ request }) => {
      const res = await request.post(`${apiUrl}/api/students`, {
        data: { firstName: good, lastName: unique(), className: 'Grade 1' },
      });
      expect(res.status()).toBe(201);
      expect((await res.json()).firstName).toBe(good);
    });
  }

  test('API trims surrounding spaces from names without a validation error', async ({ request }) => {
    const last = unique();
    const res = await request.post(`${apiUrl}/api/students`, {
      data: { firstName: '  Trim  ', lastName: `  ${last}  `, className: 'Grade 1' },
    });
    expect(res.status()).toBe(201);
    const body = await res.json();
    expect(body.firstName).toBe('Trim');
    expect(body.lastName).toBe(last);
  });

  test('API rejects a name longer than 100 characters and accepts exactly 100', async ({ request }) => {
    const post = (firstName: string) =>
      request.post(`${apiUrl}/api/students`, { data: { firstName, lastName: unique(), className: 'Grade 1' } });
    expect((await post('A'.repeat(100))).status()).toBe(201);
    const tooLong = await post('A'.repeat(101));
    expect(tooLong.status()).toBe(400);
    expect((await tooLong.json()).errors.FirstName[0]).toBe('First name must be at most 100 characters.');
  });
});

test.describe('students: email, date of birth and class rules', () => {
  test('API rejects an email longer than 254 characters with 400 and accepts exactly 254', async ({ request }) => {
    const post = (email: string) =>
      request.post(`${apiUrl}/api/students`, { data: { firstName: 'Long', lastName: unique(), className: 'Grade 1', email } });
    expect((await post(`${'a'.repeat(242)}@example.com`)).status()).toBe(201);
    const tooLong = await post(`${'a'.repeat(243)}@example.com`);
    expect(tooLong.status()).toBe(400);
    expect((await tooLong.json()).errors.Email[0]).toBe('Email must be at most 254 characters.');
  });

  test('API rejects an email with a trailing dot or no dot in the host', async ({ request }) => {
    // Open bug (PR #15, email-format-loopholes): remove test.fail once fixed.
    test.fail(true, 'known bug: MailAddress-based check accepts ada@example.com. and a.b@localhost');
    for (const email of ['ada@example.com.', 'a.b@localhost']) {
      const res = await request.post(`${apiUrl}/api/students`, {
        data: { firstName: 'Loop', lastName: unique(), className: 'Grade 1', email },
      });
      expect(res.status(), email).toBe(400);
    }
  });

  test('API rejects a date of birth in the future and accepts today', async ({ request }) => {
    const post = (dateOfBirth: string) =>
      request.post(`${apiUrl}/api/students`, { data: { firstName: 'Dob', lastName: unique(), className: 'Grade 1', dateOfBirth } });
    const tomorrow = await post(isoDate(new Date(Date.now() + 86_400_000)));
    expect(tomorrow.status()).toBe(400);
    expect((await tomorrow.json()).errors.DateOfBirth[0]).toBe("Date of birth can't be in the future.");
    expect((await post(isoDate(new Date()))).status()).toBe(201);
  });

  test('API rejects a class that is not in the list', async ({ request }) => {
    const res = await request.post(`${apiUrl}/api/students`, {
      data: { firstName: 'Class', lastName: unique(), className: 'Grade 99' },
    });
    expect(res.status()).toBe(400);
    expect((await res.json()).errors.ClassName[0]).toBe('Pick a class from the list.');
  });

  test('UI does not save a student while the date of birth is invalid', async ({ page }) => {
    // Open bug (PR #15, future-dob-silently-dropped): remove test.fail once fixed.
    test.fail(true, 'known bug: the picker shows an error but the form submits with dateOfBirth = null');
    const last = unique();
    await page.goto('/students/new');
    await page.locator('input[name="first-name"]').fill('Future');
    await page.locator('input[name="last-name"]').fill(last);
    await page.locator('select[name="class-name"]').selectOption('Grade 2');
    await page.locator('#date-of-birth').fill('1/1/2030');
    await page.keyboard.press('Tab');
    await expect(page.getByText(/Choose a date on or before/)).toBeVisible();
    await page.getByTestId('save-student').click();
    await expect(page).toHaveURL(/\/students\/new$/);
    await expect(page.getByText(`Future ${last} is now on record.`)).toHaveCount(0);
  });
});

test.describe('students: accessibility', () => {
  test('form inputs are associated with their labels', async ({ page }) => {
    // Open bug (PR #15, form-labels-not-associated): remove test.fail once fixed.
    test.fail(true, 'known bug: wrapper and input share the same id, so labels resolve to the wrapper');
    await page.goto('/students/new');
    await expect(page.getByLabel('First name')).toHaveAttribute('name', 'first-name');
    await expect(page.getByLabel('Last name')).toHaveAttribute('name', 'last-name');
    await expect(page.getByLabel('Email')).toHaveAttribute('name', 'email');
    await expect(page.getByLabel('Class')).toHaveAttribute('name', 'class-name');
  });
});
