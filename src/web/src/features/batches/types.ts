import type { Total } from "../overview/types";
export type Batch = {
  id: string;
  clientReference: string;
  correlationId: string;
  receivedAt: string;
  status: string;
  paymentCount: number;
  totals: Total[];
};
