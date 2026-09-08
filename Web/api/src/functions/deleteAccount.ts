import { app, type HttpRequest, type HttpResponseInit, type InvocationContext } from '@azure/functions'
import { processAccountDeletion } from '../lib/deleteAccount.js'
import { safeJsonResponse } from '../lib/http.js'

export async function deleteAccount(request: HttpRequest, _context: InvocationContext): Promise<HttpResponseInit> {
  const result = await processAccountDeletion(request.headers.get('authorization'))
  return safeJsonResponse(result)
}

app.http('delete-account', {
  methods: ['POST'],
  authLevel: 'anonymous',
  route: 'account/delete',
  handler: deleteAccount,
})
