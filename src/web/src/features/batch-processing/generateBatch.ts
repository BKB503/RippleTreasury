import type { Party } from "../payments/types";
import type { Draft, GenerationOptions, Purpose } from "./types";

// Synthetic fixture generation only; the API/domain remain the validation and lifecycle authority.
export const companies: Party[] = [
  {
    reference: "company-uk",
    legalName: "Northstar Treasury UK Ltd",
    country: "GB",
    kind: "Company",
    groupReference: "northstar-group",
    accountReference: "mock-account-northstar-uk",
  },
  {
    reference: "company-de",
    legalName: "Northstar Germany GmbH",
    country: "DE",
    kind: "Company",
    groupReference: "northstar-group",
    accountReference: "mock-account-northstar-de",
  },
  {
    reference: "company-us",
    legalName: "Northstar USA Inc",
    country: "US",
    kind: "Company",
    groupReference: "northstar-group",
    accountReference: "mock-account-northstar-us",
  },
];
const vendors: Party[] = [
  {
    reference: "vendor-harbour",
    legalName: "Harbour Office Supplies Ltd",
    country: "GB",
    kind: "Vendor",
    groupReference: null,
    accountReference: "mock-account-harbour",
  },
  {
    reference: "vendor-meridian",
    legalName: "Meridian Logistics GmbH",
    country: "DE",
    kind: "Vendor",
    groupReference: null,
    accountReference: "mock-account-meridian",
  },
  {
    reference: "vendor-bluewater-us",
    legalName: "Bluewater Technology USA Inc",
    country: "US",
    kind: "Vendor",
    groupReference: null,
    accountReference: "mock-account-bluewater",
  },
];
const lenders: Party[] = [
  {
    reference: "lender-summit",
    legalName: "Summit Commercial Finance",
    country: "GB",
    kind: "Lender",
    groupReference: null,
    accountReference: "mock-account-summit",
  },
  {
    reference: "lender-atlas",
    legalName: "Atlas Lending Europe",
    country: "DE",
    kind: "Lender",
    groupReference: null,
    accountReference: "mock-account-atlas",
  },
  {
    reference: "lender-bridgeway",
    legalName: "Bridgeway Capital",
    country: "US",
    kind: "Lender",
    groupReference: null,
    accountReference: "mock-account-bridgeway",
  },
];
export function randomSeed() {
  return crypto.getRandomValues(new Uint32Array(1))[0];
}
function seededRandom(seed: number) {
  let state = seed >>> 0;
  return () => {
    state = (state + 0x6d2b79f5) | 0;
    let value = Math.imul(state ^ (state >>> 15), 1 | state);
    value ^= value + Math.imul(value ^ (value >>> 7), 61 | value);
    return ((value ^ (value >>> 14)) >>> 0) / 4294967296;
  };
}
export function generateBatch(options: GenerationOptions): Draft {
  const random = seededRandom(options.seed);
  const pick = <T>(items: T[]) => items[Math.floor(random() * items.length)];
  const payer = companies.find(
    (company) => company.reference === options.payerReference,
  )!;
  const intercompanyPayees = companies.map((company) => ({
    ...company,
    reference: `${company.reference}-services`,
    legalName: `${company.legalName} Services`,
    accountReference: `mock-account-${company.reference}-services`,
  }));
  const key = crypto.randomUUID();
  const purposes: Purpose[] = [
    "VendorDisbursement",
    "InterCompanyTransfer",
    "LoanRepayment",
  ];
  const input = {
    clientReference: `ui-${key}`,
    payments: Array.from({ length: options.count }, (_, index) => {
      const purpose = purposes[(index + Math.floor(index / 3)) % purposes.length];
      const currency = ["GBP", "EUR", "USD"][index % 3];
      const country = ["GB", "DE", "US"][index % 3];
      const payees = purpose === "InterCompanyTransfer" ? intercompanyPayees :
        purpose === "LoanRepayment" ? lenders : vendors;
      const payee = pick(payees.filter((party) => party.country === country));
      // Pick fictional source metadata from the API. Capability/ownership/FX decisions remain server-side.
      const source = pick(options.fundingAccounts.filter((account) =>
        account.ownerReference === payer.reference && account.currency === currency));
      const cents = 1000 + Math.floor(random() * 499001);
      const scenario =
        options.mode === "accepted"
          ? "accept"
          : index === 0
            ? "lost-response"
            : index === 1
              ? "reject"
              : index === 2
                ? "safe-transient"
                : random() < 0.1
                  ? "reject"
                  : "accept";
      const prefix =
        purpose === "VendorDisbursement"
          ? "INV"
          : purpose === "LoanRepayment"
            ? "LOAN"
            : "TRF";
      return {
        clientReference: `line-${index + 1}`,
        amount: Number((cents / 100).toFixed(2)),
        currency,
        beneficiary: payee.legalName,
        destinationCountry: payee.country,
        provider: source.provider,
        purpose,
        executionDate: options.executionDate,
        scenario,
        treasury: {
          fundingAccountId: source.id,
          beneficiary: {
            reference: payee.reference,
            accountIdentifierType: "TokenisedReference",
            accountIdentifier: payee.accountReference,
          },
        },
        parties: {
          payer,
          payee,
          businessReference: `${prefix}-${key.slice(0, 8)}-${index + 1}`,
        },
      };
    }),
  };
  // Keep this key/body until an explicit regeneration; a failed POST retry must reuse both.
  return { key, seed: options.seed, input };
}
