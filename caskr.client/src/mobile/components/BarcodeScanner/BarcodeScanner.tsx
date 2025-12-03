import { useState, useRef, useEffect, useCallback } from 'react'
import styles from './BarcodeScanner.module.css'

export interface BarcodeScannerProps {
  onScan: (value: string, type: 'qr' | 'barcode') => void
  onError?: (error: string) => void
  mode?: 'continuous' | 'single'
  enabled?: boolean
}

// Check if BarcodeDetector API is available
const isBarcodeDetectorSupported = 'BarcodeDetector' in window

// Flash/Torch icon
const FlashIcon = ({ enabled }: { enabled: boolean }) => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.buttonIcon}>
    {enabled ? (
      <path d="M7 2v11h3v9l7-12h-4l4-8z"/>
    ) : (
      <path d="M3.27 3L2 4.27l5 5V13h3v9l3.58-6.14L17.73 20 19 18.73 3.27 3zM17 10h-4l4-8H7v2.18l8.46 8.46L17 10z"/>
    )}
  </svg>
)

// Camera flip icon
const FlipCameraIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.buttonIcon}>
    <path d="M9 12c0 1.66 1.34 3 3 3s3-1.34 3-3-1.34-3-3-3-3 1.34-3 3zm3-7c-3.31 0-6 2.69-6 6 0 1.01.25 1.97.7 2.8L5.24 15.24c-.65-1.02-1-2.18-1-3.44C4.24 7.2 7.63 3.8 12 3.8c1.26 0 2.42.35 3.44 1L14.8 5.44C14.03 5.09 13.01 4.8 12 4.8V5zM18.76 8.76l1.46-1.46c.65 1.02 1 2.18 1 3.44 0 4.56-3.39 8-7.76 8-1.26 0-2.42-.35-3.44-1l.64-.64c.77.35 1.79.64 2.8.64v.2c3.31 0 6-2.69 6-6 0-1.01-.25-1.97-.7-2.8z"/>
  </svg>
)

// Manual entry icon
const KeyboardIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.buttonIcon}>
    <path d="M20 5H4c-1.1 0-1.99.9-1.99 2L2 17c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2zm-9 3h2v2h-2V8zm0 3h2v2h-2v-2zM8 8h2v2H8V8zm0 3h2v2H8v-2zm-1 2H5v-2h2v2zm0-3H5V8h2v2zm9 7H8v-2h8v2zm0-4h-2v-2h2v2zm0-3h-2V8h2v2zm3 3h-2v-2h2v2zm0-3h-2V8h2v2z"/>
  </svg>
)

/**
 * Barcode Scanner component using device camera
 * Supports QR codes and Code128 barcodes
 */
export function BarcodeScanner({
  onScan,
  onError,
  mode = 'single',
  enabled = true
}: BarcodeScannerProps) {
  const videoRef = useRef<HTMLVideoElement>(null)
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const detectorRef = useRef<BarcodeDetector | null>(null)
  const animationFrameRef = useRef<number | null>(null)
  const lastScanRef = useRef<string | null>(null)

  const [hasPermission, setHasPermission] = useState<boolean | null>(null)
  const [isTorchEnabled, setIsTorchEnabled] = useState(false)
  const [isTorchSupported, setIsTorchSupported] = useState(false)
  const [facingMode, setFacingMode] = useState<'environment' | 'user'>('environment')
  const [isScanning, setIsScanning] = useState(false)
  const [showManualEntry, setShowManualEntry] = useState(false)
  const [manualValue, setManualValue] = useState('')
  const [scanFeedback, setScanFeedback] = useState<string | null>(null)

  // Initialize barcode detector
  useEffect(() => {
    if (isBarcodeDetectorSupported) {
      detectorRef.current = new BarcodeDetector({
        formats: ['qr_code', 'code_128', 'code_39', 'ean_13', 'ean_8']
      })
    }
  }, [])

  // Start camera stream
  const startCamera = useCallback(async () => {
    if (!enabled) return

    try {
      // Stop any existing stream
      if (streamRef.current) {
        streamRef.current.getTracks().forEach(track => track.stop())
      }

      const constraints: MediaStreamConstraints = {
        video: {
          facingMode,
          width: { ideal: 1280 },
          height: { ideal: 720 }
        },
        audio: false
      }

      const stream = await navigator.mediaDevices.getUserMedia(constraints)
      streamRef.current = stream

      if (videoRef.current) {
        videoRef.current.srcObject = stream
        await videoRef.current.play()
      }

      setHasPermission(true)
      setIsScanning(true)

      // Check torch support
      const videoTrack = stream.getVideoTracks()[0]
      const capabilities = videoTrack.getCapabilities() as MediaTrackCapabilities & { torch?: boolean }
      setIsTorchSupported(capabilities.torch === true)

      // Start scanning loop
      if (isBarcodeDetectorSupported) {
        scanFrame()
      }
    } catch (error) {
      console.error('[BarcodeScanner] Camera error:', error)

      if (error instanceof DOMException) {
        if (error.name === 'NotAllowedError') {
          setHasPermission(false)
          onError?.('Camera permission denied. Please allow camera access to scan barcodes.')
        } else if (error.name === 'NotFoundError') {
          onError?.('No camera found on this device.')
          setShowManualEntry(true)
        } else {
          onError?.(`Camera error: ${error.message}`)
        }
      }
    }
  }, [enabled, facingMode, onError])

  // Stop camera stream
  const stopCamera = useCallback(() => {
    if (streamRef.current) {
      streamRef.current.getTracks().forEach(track => track.stop())
      streamRef.current = null
    }
    if (animationFrameRef.current) {
      cancelAnimationFrame(animationFrameRef.current)
      animationFrameRef.current = null
    }
    setIsScanning(false)
  }, [])

  // Scan a single frame for barcodes
  const scanFrame = useCallback(async () => {
    if (!videoRef.current || !detectorRef.current || !isScanning) return

    try {
      const barcodes = await detectorRef.current.detect(videoRef.current)

      if (barcodes.length > 0) {
        const barcode = barcodes[0]
        const value = barcode.rawValue

        // Prevent duplicate scans
        if (value !== lastScanRef.current) {
          lastScanRef.current = value

          // Determine type
          const type = barcode.format === 'qr_code' ? 'qr' : 'barcode'

          // Haptic feedback
          if ('vibrate' in navigator) {
            navigator.vibrate(50)
          }

          // Audio feedback
          playBeep()

          // Visual feedback
          setScanFeedback(value)
          setTimeout(() => setScanFeedback(null), 1500)

          // Notify parent
          onScan(value, type)

          // Stop if single mode
          if (mode === 'single') {
            stopCamera()
            return
          }

          // Reset after delay for continuous mode
          setTimeout(() => {
            lastScanRef.current = null
          }, 2000)
        }
      }
    } catch (error) {
      console.error('[BarcodeScanner] Scan error:', error)
    }

    // Continue scanning
    if (isScanning) {
      animationFrameRef.current = requestAnimationFrame(scanFrame)
    }
  }, [isScanning, mode, onScan, stopCamera])

  // Play beep sound on successful scan
  const playBeep = () => {
    try {
      const audioContext = new AudioContext()
      const oscillator = audioContext.createOscillator()
      const gainNode = audioContext.createGain()

      oscillator.connect(gainNode)
      gainNode.connect(audioContext.destination)

      oscillator.frequency.value = 1200
      oscillator.type = 'sine'
      gainNode.gain.value = 0.1

      oscillator.start()
      oscillator.stop(audioContext.currentTime + 0.1)
    } catch {
      // Ignore audio errors
    }
  }

  // Toggle torch/flashlight
  const toggleTorch = useCallback(async () => {
    if (!streamRef.current || !isTorchSupported) return

    const videoTrack = streamRef.current.getVideoTracks()[0]
    const newTorchState = !isTorchEnabled

    try {
      await videoTrack.applyConstraints({
        advanced: [{ torch: newTorchState } as MediaTrackConstraintSet]
      })
      setIsTorchEnabled(newTorchState)
    } catch (error) {
      console.error('[BarcodeScanner] Torch error:', error)
    }
  }, [isTorchSupported, isTorchEnabled])

  // Flip camera
  const flipCamera = useCallback(() => {
    setFacingMode(prev => prev === 'environment' ? 'user' : 'environment')
  }, [])

  // Handle manual entry
  const handleManualSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (manualValue.trim()) {
      onScan(manualValue.trim(), 'barcode')
      setManualValue('')
      setShowManualEntry(false)
    }
  }

  // Start camera on mount
  useEffect(() => {
    if (enabled) {
      startCamera()
    }

    return () => {
      stopCamera()
    }
  }, [enabled, startCamera, stopCamera])

  // Restart camera when facing mode changes
  useEffect(() => {
    if (enabled && hasPermission) {
      startCamera()
    }
  }, [facingMode, enabled, hasPermission, startCamera])

  // Update scanning loop
  useEffect(() => {
    if (isScanning && isBarcodeDetectorSupported) {
      animationFrameRef.current = requestAnimationFrame(scanFrame)
    }

    return () => {
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current)
      }
    }
  }, [isScanning, scanFrame])

  // Manual entry fallback UI
  if (showManualEntry || !isBarcodeDetectorSupported) {
    return (
      <div className={styles.manualEntry}>
        <div className={styles.manualHeader}>
          <KeyboardIcon />
          <span>Enter Barrel SKU</span>
        </div>
        <form onSubmit={handleManualSubmit} className={styles.manualForm}>
          <input
            type="text"
            value={manualValue}
            onChange={e => setManualValue(e.target.value)}
            placeholder="B-2024-001"
            className={styles.manualInput}
            autoFocus
            autoComplete="off"
            autoCorrect="off"
            spellCheck={false}
          />
          <button type="submit" className={styles.manualSubmit} disabled={!manualValue.trim()}>
            Search
          </button>
        </form>
        {isBarcodeDetectorSupported && (
          <button
            type="button"
            className={styles.switchToCamera}
            onClick={() => setShowManualEntry(false)}
          >
            Switch to camera
          </button>
        )}
      </div>
    )
  }

  // Permission denied UI
  if (hasPermission === false) {
    return (
      <div className={styles.permissionDenied}>
        <div className={styles.permissionIcon}>
          <svg viewBox="0 0 24 24" fill="currentColor">
            <path d="M18 10.48V6c0-1.1-.9-2-2-2H6.83l2 2H16v4.48l2 2zM20.49 21.31L3.51 4.03a.996.996 0 0 0-1.41 0c-.39.39-.39 1.02 0 1.41l1.56 1.56H3c-.55 0-1 .45-1 1v9c0 .55.45 1 1 1h14.17l1.9 1.9c.39.39 1.02.39 1.41 0 .4-.39.4-1.02.01-1.41zM4 16V9h1.17l7 7H4z"/>
          </svg>
        </div>
        <span className={styles.permissionTitle}>Camera access needed</span>
        <span className={styles.permissionText}>
          Please allow camera access to scan barrel barcodes
        </span>
        <button
          type="button"
          className={styles.retryButton}
          onClick={startCamera}
        >
          Try again
        </button>
        <button
          type="button"
          className={styles.manualEntryButton}
          onClick={() => setShowManualEntry(true)}
        >
          Enter manually instead
        </button>
      </div>
    )
  }

  return (
    <div className={styles.scanner}>
      {/* Camera viewfinder */}
      <div className={styles.viewfinder}>
        <video
          ref={videoRef}
          className={styles.video}
          playsInline
          muted
          autoPlay
        />
        <canvas ref={canvasRef} className={styles.canvas} />

        {/* Scan region overlay */}
        <div className={styles.overlay}>
          <div className={styles.scanRegion}>
            <div className={`${styles.corner} ${styles.topLeft}`} />
            <div className={`${styles.corner} ${styles.topRight}`} />
            <div className={`${styles.corner} ${styles.bottomLeft}`} />
            <div className={`${styles.corner} ${styles.bottomRight}`} />
            {isScanning && <div className={styles.scanLine} />}
          </div>
        </div>

        {/* Scan feedback */}
        {scanFeedback && (
          <div className={styles.scanFeedback}>
            <svg viewBox="0 0 24 24" fill="currentColor" className={styles.checkIcon}>
              <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/>
            </svg>
            <span>{scanFeedback}</span>
          </div>
        )}
      </div>

      {/* Controls */}
      <div className={styles.controls}>
        {isTorchSupported && (
          <button
            type="button"
            className={`${styles.controlButton} ${isTorchEnabled ? styles.active : ''}`}
            onClick={toggleTorch}
            aria-label={isTorchEnabled ? 'Turn off flashlight' : 'Turn on flashlight'}
          >
            <FlashIcon enabled={isTorchEnabled} />
          </button>
        )}

        <button
          type="button"
          className={styles.controlButton}
          onClick={flipCamera}
          aria-label="Flip camera"
        >
          <FlipCameraIcon />
        </button>

        <button
          type="button"
          className={styles.controlButton}
          onClick={() => setShowManualEntry(true)}
          aria-label="Enter manually"
        >
          <KeyboardIcon />
        </button>
      </div>

      {/* Instructions */}
      <div className={styles.instructions}>
        Point camera at barrel barcode or QR code
      </div>
    </div>
  )
}

export default BarcodeScanner
