import { createHash, randomInt } from 'node:crypto'
import { errorResult, type ApiResult } from './http.js'

const idPattern = /^[a-z0-9][a-z0-9-]{2,63}$/
const tokenPattern = /^[A-Z0-9]{6}$/
const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'
const playerKey = (id: string) => createHash('sha256').update(id).digest('hex')

export interface WeeklyResult {
  weeklyId: string; playerId: string; name: string; score: number
  timeSeconds: number; mistakes: number; hints: number; medal: string; updatedAt: string
}
export interface WeeklyInvite {
  token: string; weeklyId: string; challenger: string; score: number
  timeSeconds: number; mistakes: number; medal: string; createdAt: string; ownerKey?: string
}
export interface WeeklyRepository {
  saveBest(result: WeeklyResult): Promise<void>
  list(weeklyId: string): Promise<WeeklyResult[]>
  createInvite(invite: WeeklyInvite): Promise<boolean>
  getInvite(token: string): Promise<WeeklyInvite | null>
}
export interface WeeklyDependencies {
  authenticate(auth: string | null): Promise<string | null>
  repository: WeeklyRepository
}

function text(value: unknown, max: number): string | null {
  if (typeof value !== 'string') return null
  const clean = value.trim().replace(/[\u0000-\u001f\u007f]/g, '')
  return clean.length > 0 && clean.length <= max ? clean : null
}
function integer(value: unknown, min: number, max: number): number | null {
  return Number.isInteger(value) && (value as number) >= min && (value as number) <= max ? value as number : null
}
export function parseResult(body: unknown): Omit<WeeklyResult, 'playerId' | 'updatedAt'> | null {
  if (!body || typeof body !== 'object') return null
  const item = body as Record<string, unknown>
  const weeklyId = text(item.weeklyId, 64); const name = text(item.name, 32)
  const score = integer(item.score, 1, 50_000_000); const timeSeconds = integer(item.timeSeconds, 0, 99_999)
  const mistakes = integer(item.mistakes, 0, 4); const hints = integer(item.hints, 0, 2)
  const medal = text(item.medal, 8)
  if (!weeklyId || !idPattern.test(weeklyId) || !name || score === null || timeSeconds === null
      || mistakes === null || hints === null || !medal || !['gold', 'silver', 'bronze'].includes(medal)) return null
  const medalValue = ({ bronze: 1, silver: 2, gold: 3 } as Record<string, number>)[medal]
  const expected = medalValue * 10_000_000 + (4 - mistakes) * 1_000_000
    + (2 - hints) * 100_000 + (99_999 - timeSeconds)
  if (score !== expected) return null
  return { weeklyId, name, score, timeSeconds, mistakes, hints, medal }
}

export async function submitWeekly(auth: string | null, body: unknown, deps: WeeklyDependencies): Promise<ApiResult> {
  const parsed = parseResult(body)
  if (!parsed) return errorResult(400, 'invalid_result')
  const playerId = await deps.authenticate(auth)
  if (!playerId) return errorResult(401, 'invalid_session')
  await deps.repository.saveBest({ ...parsed, playerId: playerKey(playerId), updatedAt: new Date().toISOString() })
  return { status: 200, body: { accepted: true } }
}

export async function getWeeklyRanking(auth: string | null, weeklyId: string,
  deps: WeeklyDependencies): Promise<ApiResult> {
  if (!idPattern.test(weeklyId)) return errorResult(400, 'invalid_weekly')
  const authenticated = auth ? await deps.authenticate(auth) : null
  const playerId = authenticated ? playerKey(authenticated) : null
  const all = (await deps.repository.list(weeklyId)).sort((a, b) => b.score - a.score)
  const entries = all.slice(0, 50).map((entry, index) => ({ rank: index + 1, name: entry.name,
    score: entry.score, isCurrentPlayer: entry.playerId === playerId }))
  const ownIndex = playerId ? all.findIndex(entry => entry.playerId === playerId) : -1
  const currentPlayer = ownIndex < 0 ? null : { rank: ownIndex + 1, name: all[ownIndex].name,
    score: all[ownIndex].score, isCurrentPlayer: true }
  const percentile = ownIndex < 0 ? null : Math.max(1, Math.ceil((ownIndex + 1) * 100 / all.length))
  return { status: 200, body: { isAvailable: true, error: null, entries, currentPlayer, percentile } }
}

export async function createWeeklyInvite(auth: string | null, body: unknown,
  deps: WeeklyDependencies): Promise<ApiResult> {
  const parsed = parseResult({ ...(body as object), name: (body as Record<string, unknown>)?.challenger })
  if (!parsed) return errorResult(400, 'invalid_challenge')
  const playerId = await deps.authenticate(auth)
  if (!playerId) return errorResult(401, 'invalid_session')
  for (let attempt = 0; attempt < 4; attempt++) {
    let token = ''
    for (let index = 0; index < 6; index++) token += alphabet[randomInt(alphabet.length)]
    const invite: WeeklyInvite = { token, weeklyId: parsed.weeklyId, challenger: parsed.name,
      score: parsed.score, timeSeconds: parsed.timeSeconds, mistakes: parsed.mistakes,
      medal: parsed.medal, createdAt: new Date().toISOString(), ownerKey: playerKey(playerId) }
    if (await deps.repository.createInvite(invite)) return { status: 201, body: publicInvite(invite) }
  }
  return errorResult(503, 'token_unavailable')
}

export async function resolveWeeklyInvite(token: string, deps: WeeklyDependencies): Promise<ApiResult> {
  if (!tokenPattern.test(token)) return errorResult(400, 'invalid_challenge')
  const invite = await deps.repository.getInvite(token)
  return invite ? { status: 200, body: publicInvite(invite) } : errorResult(404, 'challenge_not_found')
}

function publicInvite(invite: WeeklyInvite): Record<string, unknown> {
  const { ownerKey: _, ...safe } = invite
  return safe
}
