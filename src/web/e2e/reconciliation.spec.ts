import { test, expect } from "@playwright/test";

test("lost bank response is reconciled with named parties and user/date audit", async ({
  page,
  request,
}) => {
  const reference = `e2e-${crypto.randomUUID()}`;
  const result = await request.post(
    "/api/v1/payment-batches",
    {
      headers: {
        "Idempotency-Key": reference,
        "X-Demo-User": "alice.treasury",
      },
      data: {
        clientReference: reference,
        payments: [
          {
            clientReference: "lost",
            amount: 12.34,
            currency: "GBP",
            beneficiary: "Harbour Supplies Ltd",
            destinationCountry: "GB",
            provider: "bank-a",
            purpose: "VendorDisbursement",
            executionDate: new Date().toISOString().slice(0, 10),
            scenario: "lost-response",
            treasury: {
              fundingAccountId: "funding-company-uk-gbp-a",
              beneficiary: { reference: "vendor-1", accountIdentifierType: "TokenisedReference", accountIdentifier: "mock-account-vendor" },
            },
            parties: {
              payer: {
                reference: "company-uk",
                legalName: "Northstar Treasury UK Ltd",
                country: "GB",
                kind: "Company",
                groupReference: "northstar",
                accountReference: "mock-account-uk",
              },
              payee: {
                reference: "vendor-1",
                legalName: "Harbour Supplies Ltd",
                country: "GB",
                kind: "Vendor",
                groupReference: null,
                accountReference: "mock-account-vendor",
              },
              businessReference: "INVOICE-E2E-1",
            },
          },
        ],
      },
    },
  );
  expect(result.status()).toBe(202);
  await page.goto("/reconciliation");
  await expect(
    page.getByRole("button", { name: "Inspect", exact: true }),
  ).toBeVisible({ timeout: 20000 });
  await page.getByRole("button", { name: "Inspect", exact: true }).click();
  await expect(page.getByText("OutcomeUnknown", { exact: true })).toBeVisible();
  await expect(
    page.getByText("Payer: Northstar Treasury UK Ltd", { exact: false }),
  ).toBeVisible();
  await expect(
    page.getByText("Business reference: INVOICE-E2E-1"),
  ).toBeVisible();
  await expect(page.getByText("Funding account: funding-company-uk-gbp-a")).toBeVisible();
  await expect(page.getByText("Routing policy: mock-routing-v1")).toBeVisible();
  await expect(page.getByText(/^Created:/)).toContainText("Alice Morgan");
  await expect(page.getByText(/^Created:/)).toContainText("UTC");
  await expect(page.getByText(/^InstructionCreated \|/)).toBeVisible();
  await page.getByRole("combobox", { name: "Operator" }).click();
  await page.getByRole("option", { name: "Ben Patel", exact: true }).click();
  await page
    .getByRole("button", { name: "Recheck bank status", exact: true })
    .click();
  await page
    .getByRole("button", { name: "Confirm recheck", exact: true })
    .click();
  await expect(page.getByText("Accepted", { exact: true })).toBeVisible();
  await expect(
    page.getByText("Resolved", { exact: true }).first(),
  ).toBeVisible();
  await expect(page.getByText(/^ReconciliationCompleted \|/)).toBeVisible();
  await expect(page.getByText(/^Reconciliation completed:/)).toContainText(
    "Ben Patel",
  );
  await expect(page.getByText(/^Reconciliation completed:/)).not.toContainText(
    "Not completed",
  );
  await expect(
    page.getByRole("button", { name: "Recheck bank status", exact: true }),
  ).toBeDisabled();
  await page.screenshot({
    path: "test-results/reconciliation.png",
    fullPage: true,
  });
  await page.getByRole("link", { name: "Dashboard & Reports" }).click();
  await expect(
    page.getByRole("heading", { name: "Dashboard & Reports" }),
  ).toBeVisible();
});

test("generated batch submits named companies and three purposes for background processing", async ({
  page,
}) => {
  await page.goto("/process-batch");
  await page.getByLabel("Payment count").fill("5");
  await page.getByLabel("Random seed").fill("123");
  await page.getByRole("combobox", { name: "Outcome scenario" }).click();
  await page.getByRole("option", { name: "All accepted", exact: true }).click();
  await page
    .getByRole("button", { name: "Generate preview", exact: true })
    .click();
  await expect(
    page.getByRole("columnheader", { name: "Payer company", exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("cell", { name: /^Intercompany transfer TRF-/ }).first(),
  ).toBeVisible();
  const [response] = await Promise.all([
    page.waitForResponse(
      (result) =>
        result.url().endsWith("/api/v1/payment-batches") &&
        result.request().method() === "POST",
    ),
    page.getByRole("button", { name: "Submit batch", exact: true }).click(),
  ]);
  expect(response.status()).toBe(202);
  const input = response.request().postDataJSON();
  expect(input.payments).toHaveLength(5);
  expect(new Set(input.payments.map((payment: { currency: string }) => payment.currency))).toEqual(new Set(["GBP", "EUR", "USD"]));
  expect(new Set(input.payments.map((payment: { destinationCountry: string }) => payment.destinationCountry))).toEqual(new Set(["GB", "DE", "US"]));
  expect(input.payments.every((payment: { treasury?: { fundingAccountId: string } }) => !!payment.treasury?.fundingAccountId)).toBe(true);
  expect(
    new Set(
      input.payments.map((payment: { purpose: string }) => payment.purpose),
    ).size,
  ).toBe(3);
  expect(
    new Set(
      input.payments.map((payment: { provider: string }) => payment.provider),
    ).size,
  ).toBe(2);
  expect(
    input.payments.every(
      (payment: { parties: { payer: { legalName: string } } }) =>
        payment.parties.payer.legalName === "Northstar Treasury UK Ltd",
    ),
  ).toBe(true);
  await expect(
    page.getByText("SubmissionCompleted", { exact: true }),
  ).toBeVisible({ timeout: 20000 });
  await page.screenshot({
    path: "test-results/batch-processing.png",
    fullPage: true,
  });
  await page.getByRole("link", { name: "Open Batches", exact: true }).click();
  await page
    .getByRole("button", { name: input.clientReference, exact: true })
    .click();
  await expect(
    page.getByRole("columnheader", { name: "Payer company", exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("columnheader", { name: "Purpose", exact: true }),
  ).toBeVisible();
});
