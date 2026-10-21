import { test, expect, type APIRequestContext, type Page } from '@playwright/test';

// Critical flow: issue #4 "Edit or remove a student" (PR #16).
// Each test creates its own student through the API (unique, letters-only surname) so tests are
// independent and can run in any order; nothing here touches the seed rows.
const apiUrl = process.env.QA_API_URL ?? 'http://localhost:8081';

const unique = () => {
  const n = Date.now() * 1000 + Math.floor(Math.random() * 1000);
  let s = '';
  for (let v = n; v > 0; v = Math.floor(v / 26)) s = String.fromCharCode(97 + (v % 26)) + s;
  return `Qa${s[0].toUpperCase()}${s.slice(1)}`;
};

type Student = {
  id: string;
  rollNumber: number;
  firstName: string;
  lastName: string;
  dateOfBirth: string | null;
  email: string | null;
  className: string;
};

const createStudent = async (request: APIRequestContext, overrides: Partial<Record<string, unknown>> = {}) => {
  const res = await request.post(`${apiUrl}/api/students`, {
    data: { firstName: 'Edit', lastName: unique(), className: 'Grade 2', ...overrides },
  });
  expect(res.status()).toBe(201);
  return (await res.json()) as Student;
};

const rowFor = (page: Page, student: Student) =>
  page.getByRole('row', { name: new RegExp(`${student.firstName} ${student.lastName}`) });

test.describe('students: edit', { tag: '@critical' }, () => {
  test('the edit page is prefilled from the list and saves the changes', async ({ page, request }) => {
    const student = await createStudent(request, { email: 'edit@qa.example', dateOfBirth: '2012-05-01' });

    await page.goto('/students');
    await rowFor(page, student).getByTestId('edit-student').click();

    await expect(page).toHaveURL(new RegExp(`/students/${student.id}/edit$`));
    await expect(page.getByRole('heading', { name: 'Edit student' })).toBeVisible();
    await expect(page.locator('input[name="first-name"]')).toHaveValue('Edit');
    await expect(page.locator('input[name="last-name"]')).toHaveValue(student.lastName);
    await expect(page.locator('input[name="email"]')).toHaveValue('edit@qa.example');
    await expect(page.locator('select[name="class-name"]')).toHaveValue('Grade 2');
    await expect(page.locator('#date-of-birth')).toHaveValue('5/1/2012');

    const newLast = unique();
    await page.locator('input[name="last-name"]').fill(newLast);
    await page.locator('select[name="class-name"]').selectOption('Grade 7');
    await page.locator('input[name="email"]').fill('changed@qa.example');
    await page.getByTestId('save-student').click();

    await expect(page.getByText('Student updated', { exact: true })).toBeVisible();
    await expect(page.getByText(`Edit ${newLast}'s details were saved.`, { exact: true })).toBeVisible();
    await expect(page).toHaveURL(/\/students$/);
    const row = page.getByRole('row', { name: new RegExp(`Edit ${newLast}`) });
    await expect(row).toBeVisible();
    await expect(row).toContainText('Grade 7');
    await expect(row).toContainText('changed@qa.example');

    const saved = (await (await request.get(`${apiUrl}/api/students/${student.id}`)).json()) as Student;
    expect(saved.lastName).toBe(newLast);
    expect(saved.rollNumber).toBe(student.rollNumber);
  });

  test('clearing email and date of birth on edit stores them as empty', async ({ page, request }) => {
    const student = await createStudent(request, { email: 'clear@qa.example', dateOfBirth: '2012-05-01' });
    await page.goto(`/students/${student.id}/edit`);
    await expect(page.locator('input[name="email"]')).toHaveValue('clear@qa.example');

    await page.locator('input[name="email"]').fill('');
    await page.getByRole('button', { name: 'Clear Date of birth' }).click();
    await page.getByTestId('save-student').click();

    await expect(page.getByText('Student updated', { exact: true })).toBeVisible();
    const saved = (await (await request.get(`${apiUrl}/api/students/${student.id}`)).json()) as Student;
    expect(saved.email).toBeNull();
    expect(saved.dateOfBirth).toBeNull();
  });

  test('editing applies the same validation as adding', async ({ page, request }) => {
    const student = await createStudent(request);
    await page.goto(`/students/${student.id}/edit`);
    await expect(page.locator('input[name="last-name"]')).toHaveValue(student.lastName);

    await page.locator('input[name="first-name"]').fill('Ed1t');
    await page.locator('input[name="email"]').fill('not-an-email');
    await page.getByTestId('save-student').click();

    await expect(page.getByText('First name can only contain letters, spaces, hyphens and apostrophes.')).toBeVisible();
    await expect(page.getByText('Enter a valid email address.')).toBeVisible();
    await expect(page).toHaveURL(new RegExp(`/students/${student.id}/edit$`));
    const unchanged = (await (await request.get(`${apiUrl}/api/students/${student.id}`)).json()) as Student;
    expect(unchanged.firstName).toBe('Edit');
  });

  test('cancel leaves the student unchanged', async ({ page, request }) => {
    const student = await createStudent(request);
    await page.goto(`/students/${student.id}/edit`);
    await expect(page.locator('input[name="last-name"]')).toHaveValue(student.lastName);
    await page.locator('input[name="last-name"]').fill('Qanotsaved');
    await page.getByRole('link', { name: 'Cancel' }).or(page.getByRole('button', { name: 'Cancel' })).first().click();
    await expect(page).toHaveURL(/\/students$/);
    await expect(rowFor(page, student)).toBeVisible();
    await expect(page.getByRole('row', { name: /Qanotsaved/ })).toHaveCount(0);
  });

  test('an unknown student id shows "Student not found" with a way back', async ({ page }) => {
    await page.goto('/students/00000000-0000-0000-0000-000000000000/edit');
    await expect(page.getByTestId('student-not-found')).toContainText('Student not found');
    await page.getByTestId('student-not-found').getByRole('link', { name: 'Back to students' }).click();
    await expect(page).toHaveURL(/\/students$/);
  });

  test('saving a student that was removed meanwhile tells the user it no longer exists', async ({ page, request }) => {
    // Open bug (PR #16, edit-removed-student-wrong-error): remove test.fail once fixed.
    test.fail(true, 'known bug: PUT 404 is shown as "The API couldn\'t be reached"');
    const student = await createStudent(request);
    await page.goto(`/students/${student.id}/edit`);
    await expect(page.locator('input[name="last-name"]')).toHaveValue(student.lastName);

    expect((await request.delete(`${apiUrl}/api/students/${student.id}`)).status()).toBe(204);
    await page.locator('input[name="last-name"]').fill(unique());
    await page.getByTestId('save-student').click();

    await expect(page.getByText('Student not found')).toBeVisible();
    await expect(page.getByText("The API couldn't be reached. Check that it is running and try again.")).toHaveCount(0);
  });
});

test.describe('students: remove', { tag: '@critical' }, () => {
  test('removing asks for confirmation; cancel keeps the student', async ({ page, request }) => {
    const student = await createStudent(request, { firstName: 'Keep' });
    await page.goto('/students');
    await rowFor(page, student).getByTestId('remove-student').click();

    const dialog = page.getByRole('dialog');
    await expect(dialog.getByRole('heading', { name: 'Remove student?' })).toBeVisible();
    await expect(page.getByTestId('remove-confirmation')).toHaveText(
      `Keep ${student.lastName} (roll no. ${student.rollNumber}) will be removed permanently. This can't be undone.`,
    );

    await page.getByTestId('cancel-remove').click();
    await expect(dialog).toBeHidden();
    await expect(rowFor(page, student)).toBeVisible();
    expect((await request.get(`${apiUrl}/api/students/${student.id}`)).status()).toBe(200);
  });

  test('confirming removes the student from the list and the API', async ({ page, request }) => {
    const student = await createStudent(request, { firstName: 'Gone' });
    await page.goto('/students');
    await rowFor(page, student).getByTestId('remove-student').click();
    await expect(page.getByRole('dialog')).toBeVisible();

    await page.getByTestId('confirm-remove').click();

    await expect(page.getByText('Student removed', { exact: true })).toBeVisible();
    await expect(page.getByText(`Gone ${student.lastName} was removed.`, { exact: true })).toBeVisible();
    await expect(page.getByRole('dialog')).toBeHidden();
    await expect(rowFor(page, student)).toHaveCount(0);
    expect((await request.get(`${apiUrl}/api/students/${student.id}`)).status()).toBe(404);
  });

  test('removing while a search is active keeps the filter', async ({ page, request }) => {
    const last = unique();
    const keep = await createStudent(request, { firstName: 'Keep', lastName: last });
    const gone = await createStudent(request, { firstName: 'Gone', lastName: last });
    await page.goto('/students');
    await page.getByRole('searchbox', { name: 'Search students' }).fill(last);
    await expect(rowFor(page, keep)).toBeVisible();
    await expect(rowFor(page, gone)).toBeVisible();

    await rowFor(page, gone).getByTestId('remove-student').click();
    await page.getByTestId('confirm-remove').click();

    await expect(rowFor(page, gone)).toHaveCount(0);
    await expect(rowFor(page, keep)).toBeVisible();
    await expect(page.getByRole('searchbox', { name: 'Search students' })).toHaveValue(last);
    await expect(page.getByRole('row')).toHaveCount(2); // header + the remaining match
  });
});

test.describe('students: edit/remove API', () => {
  test('GET returns the student with its roll number and 404 for unknown ids', async ({ request }) => {
    const student = await createStudent(request);
    const found = await request.get(`${apiUrl}/api/students/${student.id}`);
    expect(found.status()).toBe(200);
    expect((await found.json()).rollNumber).toBe(student.rollNumber);
    expect((await request.get(`${apiUrl}/api/students/00000000-0000-0000-0000-000000000000`)).status()).toBe(404);
    expect((await request.get(`${apiUrl}/api/students/not-a-guid`)).status()).toBe(404);
  });

  test('PUT validates like POST, keeps the roll number, and 404s for unknown ids', async ({ request }) => {
    const student = await createStudent(request);
    const invalid = await request.put(`${apiUrl}/api/students/${student.id}`, {
      data: { firstName: 'Ed1t', lastName: 'Valid', className: 'Grade 99', email: `${'a'.repeat(243)}@example.com`, dateOfBirth: '2030-01-01' },
    });
    expect(invalid.status()).toBe(400);
    const errors = (await invalid.json()).errors;
    expect(errors.FirstName[0]).toBe('First name can only contain letters, spaces, hyphens and apostrophes.');
    expect(errors.ClassName[0]).toBe('Pick a class from the list.');
    expect(errors.Email[0]).toBe('Email must be at most 254 characters.');
    expect(errors.DateOfBirth[0]).toBe("Date of birth can't be in the future.");

    const ok = await request.put(`${apiUrl}/api/students/${student.id}`, {
      data: { firstName: '  Renamed ', lastName: student.lastName, className: 'Grade 9', email: '   ' },
    });
    expect(ok.status()).toBe(200);
    const body = (await ok.json()) as Student;
    expect(body.firstName).toBe('Renamed');
    expect(body.email).toBeNull();
    expect(body.rollNumber).toBe(student.rollNumber);

    const missing = await request.put(`${apiUrl}/api/students/00000000-0000-0000-0000-000000000000`, {
      data: { firstName: 'Ada', lastName: 'Lovelace', className: 'Grade 1' },
    });
    expect(missing.status()).toBe(404);
  });

  test('DELETE returns 204 once and 404 afterwards', async ({ request }) => {
    const student = await createStudent(request);
    expect((await request.delete(`${apiUrl}/api/students/${student.id}`)).status()).toBe(204);
    expect((await request.get(`${apiUrl}/api/students/${student.id}`)).status()).toBe(404);
    expect((await request.delete(`${apiUrl}/api/students/${student.id}`)).status()).toBe(404);
  });
});
