import type { Party } from "../payments/types";

export type Purpose =
  "VendorDisbursement" | "InterCompanyTransfer" | "LoanRepayment";
export type FundingAccount = {
  id: string;
  ownerReference: string;
  currency: string;
  country: string;
  provider: string;
};
export type PaymentInput = {
  clientReference: string;
  amount: number;
  currency: string;
  beneficiary: string;
  destinationCountry: string;
  provider: string;
  purpose: Purpose;
  executionDate: string;
  scenario: string;
  parties: { payer: Party; payee: Party; businessReference: string };
  treasury: {
    fundingAccountId: string;
    beneficiary: { reference: string; accountIdentifierType: string; accountIdentifier: string };
  };
};
export type BatchInput = { clientReference: string; payments: PaymentInput[] };
export type Draft = { key: string; seed: number; input: BatchInput };
export type Receipt = { id: string; replayed: boolean };
export type GenerationOptions = {
  fundingAccounts: FundingAccount[];
  count: number;
  seed: number;
  payerReference: string;
  executionDate: string;
  mode: "accepted" | "mixed";
};
export const purposeNames: Record<Purpose, string> = {
  VendorDisbursement: "Vendor disbursement",
  InterCompanyTransfer: "Intercompany transfer",
  LoanRepayment: "Loan repayment",
};
