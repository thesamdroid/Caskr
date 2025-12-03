import { useState } from 'react'
import styles from './MobileScan.module.css'

/**
 * Mobile barrel scanning page
 */
export function MobileScan() {
  const [scanResult, setScanResult] = useState<string | null>(null)
  const [isScanning, setIsScanning] = useState(false)

  const handleStartScan = () => {
    setIsScanning(true)
    // TODO: Implement actual camera/barcode scanning
    // For now, simulate a scan after 2 seconds
    setTimeout(() => {
      setScanResult('B-2024-0142')
      setIsScanning(false)
    }, 2000)
  }

  const handleManualEntry = () => {
    const barrelId = prompt('Enter Barrel ID:')
    if (barrelId) {
      setScanResult(barrelId)
    }
  }

  return (
    <div className={styles.scanPage}>
      <div className={styles.scanArea}>
        {isScanning ? (
          <div className={styles.scanning}>
            <div className={styles.scannerFrame}>
              <div className={styles.scanLine} />
            </div>
            <p className={styles.scanningText}>Scanning...</p>
          </div>
        ) : scanResult ? (
          <div className={styles.result}>
            <div className={styles.resultIcon}>✓</div>
            <p className={styles.resultLabel}>Barrel Found</p>
            <p className={styles.resultValue}>{scanResult}</p>
            <button
              className={styles.viewButton}
              onClick={() => window.location.href = `/barrels/${scanResult}`}
            >
              View Details
            </button>
            <button
              className={styles.scanAgainButton}
              onClick={() => setScanResult(null)}
            >
              Scan Another
            </button>
          </div>
        ) : (
          <div className={styles.prompt}>
            <div className={styles.cameraIcon}>
              <svg viewBox="0 0 24 24" fill="currentColor">
                <circle cx="12" cy="12" r="3.2"/>
                <path d="M9 2L7.17 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2h-3.17L15 2H9zm3 15c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5z"/>
              </svg>
            </div>
            <p className={styles.promptText}>
              Point camera at barrel barcode or QR code
            </p>
            <button
              className={styles.scanButton}
              onClick={handleStartScan}
            >
              Start Scanning
            </button>
            <button
              className={styles.manualButton}
              onClick={handleManualEntry}
            >
              Enter Manually
            </button>
          </div>
        )}
      </div>
    </div>
  )
}

export default MobileScan
