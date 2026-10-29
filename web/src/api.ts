import type { GameState, SeatRequest } from './types'

const root = import.meta.env.VITE_API_URL ?? ''

export class TableError extends Error {
  constructor(message: string) {
    super(message)
    this.name = 'TableError'
  }
}

async function readBody(response: Response): Promise<unknown> {
  const text = await response.text()
  if (!text) {
    return null
  }
  try {
    return JSON.parse(text) as unknown
  } catch {
    return text
  }
}

function messageFrom(body: unknown, fallback: string): string {
  if (!body || typeof body !== 'object') {
    return fallback
  }
  const record = body as Record<string, unknown>
  if (typeof record.detail === 'string' && record.detail.length > 0) {
    return record.detail
  }
  if (Array.isArray(record.errors)) {
    return record.errors.map(String).join(' ')
  }
  if (typeof record.title === 'string') {
    return record.title
  }
  return fallback
}

async function send(path: string, init?: RequestInit): Promise<GameState> {
  const response = await fetch(`${root}${path}`, {
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    ...init,
  })
  const body = await readBody(response)
  if (!response.ok) {
    throw new TableError(messageFrom(body, 'The table could not do that.'))
  }
  return body as GameState
}

export function startGame(players: SeatRequest[], clone: boolean, seed: number | null): Promise<GameState> {
  return send('/games', {
    method: 'POST',
    body: JSON.stringify({
      players,
      clone,
      seed,
    }),
  })
}

export function roll(id: string, dieCount: number): Promise<GameState> {
  return send(`/games/${id}/roll`, { method: 'POST', body: JSON.stringify({ dieCount }) })
}

export function resolve(id: string, choice: string): Promise<GameState> {
  return send(`/games/${id}/resolve`, { method: 'POST', body: JSON.stringify({ choice }) })
}

export function endTurn(id: string): Promise<GameState> {
  return send(`/games/${id}/end-turn`, { method: 'POST' })
}

export function buy(id: string, shares: number | null): Promise<GameState> {
  return send(`/games/${id}/buy`, { method: 'POST', body: JSON.stringify({ shares }) })
}

export function passCard(id: string): Promise<GameState> {
  return send(`/games/${id}/pass`, { method: 'POST' })
}

export function settle(id: string): Promise<GameState> {
  return send(`/games/${id}/settle`, { method: 'POST' })
}

export function leave(id: string): Promise<GameState> {
  return send(`/games/${id}/leave`, { method: 'POST' })
}

export function borrow(id: string, increments: number): Promise<GameState> {
  return send(`/games/${id}/borrow`, { method: 'POST', body: JSON.stringify({ increments }) })
}

export function repay(id: string, increments: number): Promise<GameState> {
  return send(`/games/${id}/repay`, { method: 'POST', body: JSON.stringify({ increments }) })
}

export function buyFastTrackDeal(id: string, dealId: string): Promise<GameState> {
  return send(`/games/${id}/fast-track-deal`, { method: 'POST', body: JSON.stringify({ id: dealId }) })
}

export function cloneStep(id: string): Promise<GameState> {
  return send(`/games/${id}/clone-step`, { method: 'POST' })
}
