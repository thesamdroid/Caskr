import { useState, useCallback, useEffect, useRef } from 'react'

export interface UseDrawerOptions {
  onOpen?: () => void
  onClose?: () => void
  closeOnEscape?: boolean
  closeOnOutsideClick?: boolean
}

export interface UseDrawerReturn {
  isOpen: boolean
  open: () => void
  close: () => void
  toggle: () => void
  drawerRef: React.RefObject<HTMLDivElement>
  backdropRef: React.RefObject<HTMLDivElement>
}

/**
 * Hook to manage drawer open/close state with gesture support
 */
export function useDrawer(options: UseDrawerOptions = {}): UseDrawerReturn {
  const {
    onOpen,
    onClose,
    closeOnEscape = true,
    closeOnOutsideClick = true
  } = options

  const [isOpen, setIsOpen] = useState(false)
  const drawerRef = useRef<HTMLDivElement>(null)
  const backdropRef = useRef<HTMLDivElement>(null)

  // Touch state for swipe detection
  const touchStartX = useRef<number | null>(null)
  const touchCurrentX = useRef<number | null>(null)

  const open = useCallback(() => {
    setIsOpen(true)
    onOpen?.()
    // Prevent body scroll when drawer is open
    document.body.style.overflow = 'hidden'
  }, [onOpen])

  const close = useCallback(() => {
    setIsOpen(false)
    onClose?.()
    // Restore body scroll
    document.body.style.overflow = ''
  }, [onClose])

  const toggle = useCallback(() => {
    if (isOpen) {
      close()
    } else {
      open()
    }
  }, [isOpen, open, close])

  // Handle escape key
  useEffect(() => {
    if (!closeOnEscape || !isOpen) return

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        close()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [closeOnEscape, isOpen, close])

  // Handle outside click (backdrop)
  useEffect(() => {
    if (!closeOnOutsideClick || !isOpen) return

    const handleClick = (e: MouseEvent) => {
      if (backdropRef.current && e.target === backdropRef.current) {
        close()
      }
    }

    document.addEventListener('click', handleClick)
    return () => document.removeEventListener('click', handleClick)
  }, [closeOnOutsideClick, isOpen, close])

  // Handle swipe to close
  useEffect(() => {
    if (!isOpen) return

    const drawer = drawerRef.current
    if (!drawer) return

    const handleTouchStart = (e: TouchEvent) => {
      touchStartX.current = e.touches[0].clientX
      touchCurrentX.current = e.touches[0].clientX
    }

    const handleTouchMove = (e: TouchEvent) => {
      if (touchStartX.current === null) return
      touchCurrentX.current = e.touches[0].clientX

      // Calculate swipe distance (positive = swiping right, closing the drawer)
      const deltaX = touchCurrentX.current - touchStartX.current

      // Only track swipes that go from left to right (closing)
      // The drawer opens from the right, so swiping right closes it
      if (deltaX > 0) {
        // Apply transform to create swipe effect
        const transform = Math.min(deltaX, 300)
        drawer.style.transform = `translateX(${transform}px)`
      }
    }

    const handleTouchEnd = () => {
      if (touchStartX.current === null || touchCurrentX.current === null) {
        touchStartX.current = null
        touchCurrentX.current = null
        return
      }

      const deltaX = touchCurrentX.current - touchStartX.current

      // If swiped more than 100px, close the drawer
      if (deltaX > 100) {
        close()
      }

      // Reset transform
      drawer.style.transform = ''
      touchStartX.current = null
      touchCurrentX.current = null
    }

    drawer.addEventListener('touchstart', handleTouchStart, { passive: true })
    drawer.addEventListener('touchmove', handleTouchMove, { passive: true })
    drawer.addEventListener('touchend', handleTouchEnd, { passive: true })

    return () => {
      drawer.removeEventListener('touchstart', handleTouchStart)
      drawer.removeEventListener('touchmove', handleTouchMove)
      drawer.removeEventListener('touchend', handleTouchEnd)
    }
  }, [isOpen, close])

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      document.body.style.overflow = ''
    }
  }, [])

  return {
    isOpen,
    open,
    close,
    toggle,
    drawerRef,
    backdropRef
  }
}

export default useDrawer
