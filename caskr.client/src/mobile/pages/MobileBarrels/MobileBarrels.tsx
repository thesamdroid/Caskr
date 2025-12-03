import { useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { BarcodeScanner } from '../../components/BarcodeScanner'
import { BarrelSearch, BarrelDetailsSheet } from './components'
import { useBarrelLookup, useRickhouses } from './useBarrelLookup'
import styles from './MobileBarrels.module.css'

type ViewMode = 'scan' | 'search'

// Scan icon
const ScanIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.modeIcon}>
    <path d="M4 4h4V2H2v6h2V4zm0 12H2v6h6v-2H4v-4zm16 4h-4v2h6v-6h-2v4zm0-16V2h-6v2h4v4h2V4zM9 9h6v6H9V9zm-2 8h10V7H7v10z"/>
  </svg>
)

// Search icon
const SearchIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.modeIcon}>
    <path d="M15.5 14h-.79l-.28-.27a6.5 6.5 0 0 0 1.48-5.34c-.47-2.78-2.79-5-5.59-5.34a6.505 6.505 0 0 0-7.27 7.27c.34 2.8 2.56 5.12 5.34 5.59a6.5 6.5 0 0 0 5.34-1.48l.27.28v.79l4.25 4.25c.41.41 1.08.41 1.49 0 .41-.41.41-1.08 0-1.49L15.5 14zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
  </svg>
)

// Offline icon
const OfflineIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.offlineIcon}>
    <path d="M23.64 7c-.45-.34-4.93-4-11.64-4-1.5 0-2.89.19-4.15.48L18.18 13.8 23.64 7zm-6.6 8.22L3.27 1.44 2 2.72l2.05 2.06C1.91 5.76.59 6.82.36 7L12 21.5l3.07-4.04 3.52 3.52 1.26-1.27-2.81-2.81-.18.18v.04-.04l-.02.02.02-.02z"/>
  </svg>
)

/**
 * Mobile barrels page with scan/search modes
 */
export function MobileBarrels() {
  const navigate = useNavigate()
  const [viewMode, setViewMode] = useState<ViewMode>('scan')
  const { rickhouses } = useRickhouses()

  const {
    isScannerActive,
    searchQuery,
    setSearchQuery,
    searchResults,
    isSearching,
    filterStatus,
    setFilterStatus,
    filterRickhouse,
    setFilterRickhouse,
    selectedBarrel,
    barrelHistory,
    isLoadingDetail,
    loadBarrelDetail,
    loadBarrelBySku,
    closeDetail,
    recentBarrels,
    recordMovement,
    recordGauge,
    isOffline,
    pendingActions
  } = useBarrelLookup()

  // Handle scan result
  const handleScan = useCallback((value: string, _type: 'qr' | 'barcode') => {
    loadBarrelBySku(value)
  }, [loadBarrelBySku])

  // Handle barrel selection from search
  const handleBarrelSelect = useCallback((barrelId: number) => {
    loadBarrelDetail(barrelId)
  }, [loadBarrelDetail])

  // Handle view full history
  const handleViewFullHistory = useCallback(() => {
    if (selectedBarrel) {
      navigate(`/barrels/${selectedBarrel.id}/history`)
    }
  }, [selectedBarrel, navigate])

  // Handle print label
  const handlePrintLabel = useCallback(() => {
    if (selectedBarrel) {
      // Open print dialog or navigate to print page
      window.print()
    }
  }, [selectedBarrel])

  // Handle recent barrel tap
  const handleRecentBarrelTap = useCallback((barrelId: number) => {
    loadBarrelDetail(barrelId)
  }, [loadBarrelDetail])

  return (
    <div className={styles.container}>
      {/* Mode toggle */}
      <div className={styles.modeToggle}>
        <button
          type="button"
          className={`${styles.modeButton} ${viewMode === 'scan' ? styles.active : ''}`}
          onClick={() => setViewMode('scan')}
        >
          <ScanIcon />
          <span>Scan</span>
        </button>
        <button
          type="button"
          className={`${styles.modeButton} ${viewMode === 'search' ? styles.active : ''}`}
          onClick={() => setViewMode('search')}
        >
          <SearchIcon />
          <span>Search</span>
        </button>
      </div>

      {/* Offline indicator */}
      {isOffline && (
        <div className={styles.offlineIndicator}>
          <OfflineIcon />
          <span>Offline - {pendingActions.length} pending</span>
        </div>
      )}

      {/* Main content */}
      <div className={styles.content}>
        {viewMode === 'scan' && (
          <div className={styles.scanMode}>
            {/* Camera scanner */}
            <div className={styles.scannerArea}>
              <BarcodeScanner
                onScan={handleScan}
                mode="single"
                enabled={isScannerActive && !selectedBarrel}
              />
            </div>

            {/* Recent barrels */}
            {recentBarrels.length > 0 && !selectedBarrel && (
              <div className={styles.recentSection}>
                <h3 className={styles.recentTitle}>Recent</h3>
                <div className={styles.recentList}>
                  {recentBarrels.map(barrel => (
                    <button
                      key={barrel.id}
                      type="button"
                      className={styles.recentItem}
                      onClick={() => handleRecentBarrelTap(barrel.id)}
                    >
                      <span className={styles.recentSku}>{barrel.sku}</span>
                      <span className={styles.recentDetails}>
                        {barrel.rickhouseName} • {barrel.age}
                      </span>
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* Loading state for scanned barrel */}
            {isLoadingDetail && (
              <div className={styles.loadingOverlay}>
                <div className={styles.loadingSpinner} />
                <span>Loading barrel...</span>
              </div>
            )}
          </div>
        )}

        {viewMode === 'search' && (
          <BarrelSearch
            searchQuery={searchQuery}
            onSearchChange={setSearchQuery}
            searchResults={searchResults}
            isSearching={isSearching}
            filterStatus={filterStatus}
            onFilterStatusChange={setFilterStatus}
            filterRickhouse={filterRickhouse}
            onFilterRickhouseChange={setFilterRickhouse}
            rickhouses={rickhouses}
            onBarrelSelect={handleBarrelSelect}
          />
        )}
      </div>

      {/* Barrel details sheet */}
      {selectedBarrel && (
        <BarrelDetailsSheet
          barrel={selectedBarrel}
          history={barrelHistory}
          rickhouses={rickhouses}
          onClose={closeDetail}
          onRecordMovement={recordMovement}
          onRecordGauge={recordGauge}
          onViewFullHistory={handleViewFullHistory}
          onPrintLabel={handlePrintLabel}
        />
      )}
    </div>
  )
}

export default MobileBarrels
