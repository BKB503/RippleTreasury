import type { Detail } from "../payments/types";
export type Case = {
  id: string;
  paymentId: string;
  status: string;
  version: number;
  openedAt: string;
  correlationId: string;
};
export type CaseDetail = { case: Case; detail: Detail };
