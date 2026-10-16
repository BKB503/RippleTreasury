export type Audit = {
  id: string;
  at: string;
  action: string;
  before: string;
  after: string;
  reason: string;
  actor: string;
  actorDisplayName: string;
  actorKind: string;
  source: string;
  policyVersion: string;
  version: number;
  correlationId: string;
};
export type Detail = {
  payment: Payment;
  attempts: {
    id: string;
    startedAt: string;
    completedAt: string | null;
    outcome: string;
    kind: string;
    provider: string | null;
    fundingAccountId: string | null;
    requestedExecutionDate: string;
    routingPolicyVersion: string | null;
  }[];
  audit: Audit[];
  history: PaymentHistory | null;
};

export type Party = {
  reference: string;
  legalName: string;
  country: string;
  kind: string;
  groupReference: string | null;
  accountReference: string;
};
export type PaymentParties = {
  payer: Party;
  payee: Party;
  businessReference: string;
};
export type Payment = {
  id: string;
  batchId: string;
  executionId: string;
  clientReference: string;
  minorUnits: string;
  currency: string;
  beneficiary: string;
  destinationCountry: string;
  provider: string;
  purpose: string;
  status: string;
  version: number;
  bankReference: string | null;
  correlationId: string;
  executionDate: string;
  parties: PaymentParties | null;
  treasury: {
    fundingAccountId: string;
    fundingCurrency: string;
    sourceCountry: string;
    beneficiaryReference: string;
    accountIdentifierType: string;
    accountIdentifier: string;
    bic: string | null;
    isCrossBorder: boolean;
    routingPolicyVersion: string;
    routingReason: string;
  } | null;
};

export type Actor = { id: string; displayName: string; kind: string };
export type PaymentHistory = {
  createdAt: string;
  createdBy: Actor;
  lastUpdatedAt: string;
  lastUpdatedBy: Actor;
  lastReconciliationStartedAt: string | null;
  lastReconciliationCompletedAt: string | null;
  lastReconciliationBy: Actor | null;
};
