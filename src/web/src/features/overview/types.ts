export type Total = { currency: string; minorUnits: string };
export type Summary = {
  total: number;
  statusCounts: Record<string, number>;
  totals: Total[];
};
