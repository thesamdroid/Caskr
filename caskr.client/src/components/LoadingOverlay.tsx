import { useSyncExternalStore } from 'react'
import { loadingManager } from '../loadingManager'

export default function LoadingOverlay() {
  const isLoading = useSyncExternalStore(
    loadingManager.subscribe,
    loadingManager.getSnapshot,
    loadingManager.getServerSnapshot
  )

  if (!isLoading) return null

  return (
    <div className='loading-overlay' aria-live='polite'>
      <div className='loading-spinner' role='status' aria-label='Loading'>
        <span className='visually-hidden'>Loading</span>
      </div>
    </div>
  )
}

