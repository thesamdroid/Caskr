import { useState, useEffect } from 'react'

/**
 * Safe area insets for devices with notches/home indicators
 */
export interface SafeAreaInsets {
  top: number
  right: number
  bottom: number
  left: number
}

/**
 * Hook to get safe area insets for notched devices (iPhone X+, etc.)
 * Uses CSS environment variables when available
 */
export function useSafeArea(): SafeAreaInsets {
  const [insets, setInsets] = useState<SafeAreaInsets>({
    top: 0,
    right: 0,
    bottom: 0,
    left: 0
  })

  useEffect(() => {
    const updateInsets = () => {
      // Check if CSS environment variables are supported
      const supportsEnv = CSS.supports('padding-top', 'env(safe-area-inset-top)')

      if (supportsEnv) {
        // Use a div to compute the actual pixel values
        const computeInset = (envVar: string): number => {
          const temp = document.createElement('div')
          temp.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            padding-top: env(${envVar}, 0px);
            visibility: hidden;
            pointer-events: none;
          `
          document.body.appendChild(temp)
          const value = parseFloat(window.getComputedStyle(temp).paddingTop) || 0
          document.body.removeChild(temp)
          return value
        }

        setInsets({
          top: computeInset('safe-area-inset-top'),
          right: computeInset('safe-area-inset-right'),
          bottom: computeInset('safe-area-inset-bottom'),
          left: computeInset('safe-area-inset-left')
        })
      } else {
        // Fallback values for devices without notches
        setInsets({
          top: 0,
          right: 0,
          bottom: 0,
          left: 0
        })
      }
    }

    // Initial calculation
    updateInsets()

    // Recalculate on orientation change
    const handleOrientationChange = () => {
      // Small delay to allow the browser to update
      setTimeout(updateInsets, 100)
    }

    window.addEventListener('orientationchange', handleOrientationChange)
    window.addEventListener('resize', handleOrientationChange)

    return () => {
      window.removeEventListener('orientationchange', handleOrientationChange)
      window.removeEventListener('resize', handleOrientationChange)
    }
  }, [])

  return insets
}

export default useSafeArea
