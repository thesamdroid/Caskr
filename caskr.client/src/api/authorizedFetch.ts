export const authorizedFetch = async (input: RequestInfo | URL, init: RequestInit = {}) => {
  const token = localStorage.getItem('token')
  const headers = new Headers(init.headers ?? {})

  if (token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  return fetch(input, { ...init, headers })
}
