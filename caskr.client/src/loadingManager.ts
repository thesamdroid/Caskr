type Listener = () => void

let activeRequests = 0
const listeners = new Set<Listener>()

const notifyListeners = () => {
  for (const listener of listeners) {
    listener()
  }
}

export const loadingManager = {
  beginRequest() {
    activeRequests += 1
    notifyListeners()
  },
  endRequest() {
    activeRequests = Math.max(0, activeRequests - 1)
    notifyListeners()
  },
  subscribe(listener: Listener) {
    listeners.add(listener)
    return () => {
      listeners.delete(listener)
    }
  },
  getSnapshot() {
    return activeRequests > 0
  },
  getServerSnapshot() {
    return false
  }
}

