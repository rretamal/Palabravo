import { createHash } from 'node:crypto'
import { errorResult, type ApiResult } from './http.js'

const playerKey = (id: string) => createHash('sha256').update(id).digest('hex')

export interface PathResult {
  playerId: string; name: string; score: number; completed: number
  gold: number; silver: number; bronze: number; updatedAt: string
}
export interface PathRankingRepository {
  save(result: PathResult): Promise<void>
  list(): Promise<PathResult[]>
}
export interface PathRankingDependencies {
  authenticate(auth: string | null): Promise<string | null>
  repository: PathRankingRepository
}

function integer(value: unknown, min: number, max: number): number | null {
  return Number.isInteger(value) && (value as number) >= min && (value as number) <= max ? value as number : null
}
function parse(body: unknown): Omit<PathResult, 'playerId' | 'updatedAt'> | null {
  if (!body || typeof body !== 'object') return null
  const item = body as Record<string, unknown>
  const name = typeof item.name === 'string'
    ? item.name.trim().replace(/[\u0000-\u001f\u007f]/g, '') : ''
  const score = integer(item.score, 110, 13_000); const completed = integer(item.completed, 1, 100)
  const gold = integer(item.gold, 0, 100); const silver = integer(item.silver, 0, 100)
  const bronze = integer(item.bronze, 0, 100)
  if (!name || name.length > 32 || score === null || completed === null || gold === null
      || silver === null || bronze === null || gold + silver + bronze !== completed) return null
  if (score !== completed * 100 + gold * 30 + silver * 20 + bronze * 10) return null
  return { name, score, completed, gold, silver, bronze }
}

export async function submitPathResult(auth: string | null, body: unknown,
  deps: PathRankingDependencies): Promise<ApiResult> {
  const parsed = parse(body)
  if (!parsed) return errorResult(400, 'invalid_result')
  const playerId = await deps.authenticate(auth)
  if (!playerId) return errorResult(401, 'invalid_session')
  await deps.repository.save({ ...parsed, playerId: playerKey(playerId), updatedAt: new Date().toISOString() })
  return { status: 200, body: { accepted: true } }
}

export async function getPathRanking(auth: string | null,
  deps: PathRankingDependencies): Promise<ApiResult> {
  const authenticated = auth ? await deps.authenticate(auth) : null
  const playerId = authenticated ? playerKey(authenticated) : null
  const all = (await deps.repository.list()).sort((a, b) => b.score - a.score || a.updatedAt.localeCompare(b.updatedAt))
  const entry = (result: PathResult, index: number) => ({ rank: index + 1, name: result.name,
    score: result.score, completed: result.completed, gold: result.gold, silver: result.silver,
    bronze: result.bronze, isCurrentPlayer: result.playerId === playerId })
  const entries = all.slice(0, 50).map(entry)
  const ownIndex = playerId ? all.findIndex(result => result.playerId === playerId) : -1
  const currentPlayer = ownIndex < 0 ? null : entry(all[ownIndex], ownIndex)
  const percentile = ownIndex < 0 ? null : Math.max(1, Math.ceil((ownIndex + 1) * 100 / all.length))
  const pointsToNext = ownIndex <= 0 ? null : all[ownIndex - 1].score - all[ownIndex].score + 1
  return { status: 200, body: { isAvailable: true, error: null, entries, currentPlayer,
    percentile, pointsToNext } }
}
