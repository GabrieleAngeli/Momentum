import { test, expect } from '@playwright/test';

test.describe('web-core shell', () => {
  test('should render dashboard headline', async ({ page }) => {
    await page.goto('http://localhost:4200');
    await expect(page.getByRole('heading', { level: 2 })).toHaveText(/Telemetry/);
  });
});
