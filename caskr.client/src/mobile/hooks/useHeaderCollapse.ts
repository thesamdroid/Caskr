import { useState, useEffect, useCallback, useRef } from 'react'

export interface HeaderCollapseState {
  isCollapsed: boolean
  scrollY: number
  collapseProgress: number // 0 = fully expanded, 1 = fully collapsed
}

export interface UseHeaderCollapseOptions {
  threshold?: number // Scroll threshold to trigger collapse (default: 50px)
  expandedHeight?: number // Height when expanded (default: 96px)
  collapsedHeight?: number // Height when collapsed (default: 56px)
  enabled?: boolean // Whether collapse behavior is enabled
}

/**
 * Hook to manage header collapse on scroll
 */
export function useHeaderCollapse(options: UseHeaderCollapseOptions = {}): HeaderCollapseState {
  const {
    threshold = 50,
    expandedHeight: _expandedHeight = 96, // Reserved for future animation use
    collapsedHeight: _collapsedHeight = 56, // Reserved for future animation use
    enabled = true
  } = options
  // Suppress unused variable warnings - these are reserved for future animation implementation
  void _expandedHeight
  void _collapsedHeight

  const [state, setState] = useState<HeaderCollapseState>({
    isCollapsed: false,
    scrollY: 0,
    collapseProgress: 0
  })

  const lastScrollY = useRef(0)
  const ticking = useRef(false)

  const updateScrollState = useCallback(() => {
    const scrollY = window.scrollY

    // Calculate collapse progress (0 to 1)
    let collapseProgress = 0
    if (scrollY > 0) {
      collapseProgress = Math.min(scrollY / threshold, 1)
    }

    const isCollapsed = scrollY > threshold

    setState({
      isCollapsed,
      scrollY,
      collapseProgress
    })

    lastScrollY.current = scrollY
    ticking.current = false
  }, [threshold])

  const handleScroll = useCallback(() => {
    if (!enabled) return

    if (!ticking.current) {
      window.requestAnimationFrame(() => {
        updateScrollState()
      })
      ticking.current = true
    }
  }, [enabled, updateScrollState])

  useEffect(() => {
    if (!enabled) {
      setState({
        isCollapsed: false,
        scrollY: 0,
        collapseProgress: 0
      })
      return
    }

    // Initial state
    updateScrollState()

    window.addEventListener('scroll', handleScroll, { passive: true })

    return () => {
      window.removeEventListener('scroll', handleScroll)
    }
  }, [enabled, handleScroll, updateScrollState])

  return state
}

export default useHeaderCollapse
