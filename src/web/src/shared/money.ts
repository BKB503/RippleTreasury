import type { Total } from "../features/overview/types";
export function money(total: Total) {
  const n = BigInt(total.minorUnits);
  const scale = total.currency === "JPY" ? 1n : 100n;
  return `${total.currency} ${(n / scale).toLocaleString("en-GB")}${scale === 100n ? "." + (n % scale).toString().padStart(2, "0") : ""}`;
}
