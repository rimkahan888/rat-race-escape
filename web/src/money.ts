/** Dollars and cents, never a minus sign. Cash and cash flow stay at zero. */
export function formatMoney(cents: number): string {
  const safe = Number.isFinite(cents) ? Math.max(0, Math.trunc(cents)) : 0
  const dollars = Math.floor(safe / 100)
  const remainder = safe % 100
  return `$${dollars.toLocaleString('en-US')}.${String(remainder).padStart(2, '0')}`
}

/** A flow that can be a cost. The amount is unsigned; a cost says "out". */
export function formatFlow(cents: number): string {
  if (!Number.isFinite(cents)) {
    return formatMoney(0)
  }
  if (cents < 0) {
    return `${formatMoney(-cents)} out`
  }
  return formatMoney(cents)
}
