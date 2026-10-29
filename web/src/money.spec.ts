import { describe, expect, it } from 'vitest'
import { formatFlow, formatMoney } from './money'

describe('money', () => {
  it('never prints a minus sign', () => {
    expect(formatMoney(-21_000)).toBe('$0.00')
    expect(formatMoney(63_000)).toBe('$630.00')
    expect(formatFlow(-8_000)).not.toContain('-')
    expect(formatFlow(-8_000)).toContain('out')
    expect(formatFlow(50_000)).toBe('$500.00')
  })
})
