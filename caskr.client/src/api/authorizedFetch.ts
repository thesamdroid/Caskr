import { loadingManager } from '../loadingManager'

export const authorizedFetch = async (input: RequestInfo | URL, init: RequestInit = {}) => {
  const token = localStorage.getItem('token')
  const headers = new Headers(init.headers ?? {})

  if (token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  loadingManager.beginRequest()
  try {
    return await fetch(input, { ...init, headers })
  } finally {
    loadingManager.endRequest()
  }
}
