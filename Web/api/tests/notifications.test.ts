import { describe, expect, it, vi } from 'vitest'
import { sendNewChallengeNotification, type NotificationDependencies } from '../src/lib/notifications.js'

const valid = {
  weeklyId: 'weekly-cl-003',
  title: '¡Hay un reto nuevo!',
  body: 'Descubre las conexiones de esta semana.',
}

function setup() {
  const send = vi.fn(async () => 'projects/palabravo/messages/123')
  const deps: NotificationDependencies = { adminKey: 'a-long-random-secret', sender: { send } }
  return { deps, send }
}

describe('new challenge notifications', () => {
  it('requires the exact administrative bearer key', async () => {
    const { deps, send } = setup()
    expect((await sendNewChallengeNotification(null, valid, deps)).status).toBe(401)
    expect((await sendNewChallengeNotification('Bearer wrong', valid, deps)).status).toBe(401)
    expect(send).not.toHaveBeenCalled()
  })

  it('validates all public notification fields', async () => {
    const { deps, send } = setup()
    const response = await sendNewChallengeNotification('Bearer a-long-random-secret',
      { ...valid, weeklyId: '../bad' }, deps)
    expect(response.status).toBe(400)
    expect(send).not.toHaveBeenCalled()
  })

  it('sends a valid notification to the configured sender', async () => {
    const { deps, send } = setup()
    const response = await sendNewChallengeNotification('Bearer a-long-random-secret', valid, deps)
    expect(response.status).toBe(202)
    expect(response.body).toMatchObject({ accepted: true })
    expect(send).toHaveBeenCalledWith(valid)
  })
})
