import { loadingManager } from '../loadingManager'

export const authorizedFetch = async (input: RequestInfo | URL, init: RequestInit = {}) => {
  const token = localStorage.getItem('token')
  const headers = new Headers(init.headers ?? {})

  if (token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const requestUrl = input instanceof Request ? input.url : String(input)
  const requestMethod = init.method ?? (input instanceof Request ? input.method : 'GET')
  console.log('[authorizedFetch] Starting request', { url: requestUrl, method: requestMethod })
  loadingManager.beginRequest()
  try {
    const response = await fetch(input, { ...init, headers })
    console.log('[authorizedFetch] Received response', {
      url: requestUrl,
      method: requestMethod,
      status: response.status
    })
    return response
  } catch (error) {
    console.error('[authorizedFetch] Request failed', { url: requestUrl, method: requestMethod, error })
    throw error
  } finally {
    loadingManager.endRequest()
  }
}
