import { app, type HttpRequest, type HttpResponseInit, type InvocationContext } from '@azure/functions'
import { processDeletionRequest, type DeletionRequestBody } from '../lib/deletionRequest.js'
import { errorResult, safeJsonResponse } from '../lib/http.js'

export async function deletionRequests(request: HttpRequest, _context: InvocationContext): Promise<HttpResponseInit> {
  const rawBody = await request.text()
  if (rawBody.length > 12_000)
    return safeJsonResponse(errorResult(413, 'request_too_large'))

  let body: DeletionRequestBody
  try {
    body = JSON.parse(rawBody) as DeletionRequestBody
  } catch {
    return safeJsonResponse(errorResult(400, 'invalid_request'))
  }

  const result = await processDeletionRequest(body, {
    origin: request.headers.get('origin'),
    contentLength: String(rawBody.length),
  })
  return safeJsonResponse(result)
}

app.http('deletion-requests', {
  methods: ['POST'],
  authLevel: 'anonymous',
  route: 'deletion-requests',
  handler: deletionRequests,
})
