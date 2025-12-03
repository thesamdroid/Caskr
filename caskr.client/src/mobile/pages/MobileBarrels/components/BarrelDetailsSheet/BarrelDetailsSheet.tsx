import { useState, useRef, useCallback } from 'react'
import type { BarrelDetail, BarrelHistoryItem, GaugeRecordData, MovementFormData, Rickhouse } from '../../types'
import styles from './BarrelDetailsSheet.module.css'

export interface BarrelDetailsSheetProps {
  barrel: BarrelDetail
  history: BarrelHistoryItem[]
  rickhouses: Rickhouse[]
  onClose: () => void
  onRecordMovement: (barrelId: number, destinationId: number, notes?: string) => Promise<void>
  onRecordGauge: (barrelId: number, data: GaugeRecordData) => Promise<void>
  onViewFullHistory: () => void
  onPrintLabel: () => void
}

type SheetTab = 'info' | 'history'
type ActionSheet = 'movement' | 'gauge' | null

// Close icon
const CloseIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.closeIcon}>
    <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
  </svg>
)

// History type icons
const HistoryIcon = ({ type }: { type: BarrelHistoryItem['type'] }) => {
  const iconClasses = `${styles.historyIcon} ${styles[`icon${type.charAt(0).toUpperCase() + type.slice(1)}`]}`

  switch (type) {
    case 'movement':
      return (
        <svg viewBox="0 0 24 24" fill="currentColor" className={iconClasses}>
          <path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/>
        </svg>
      )
    case 'gauge':
      return (
        <svg viewBox="0 0 24 24" fill="currentColor" className={iconClasses}>
          <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm3.88-11.71L10 14.17l-1.88-1.88a.996.996 0 1 0-1.41 1.41l2.59 2.59c.39.39 1.02.39 1.41 0L17.29 9.7a.996.996 0 0 0 0-1.41c-.39-.38-1.03-.38-1.41 0z"/>
        </svg>
      )
    case 'fill':
      return (
        <svg viewBox="0 0 24 24" fill="currentColor" className={iconClasses}>
          <path d="M18 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm-1 18H7V4h10v16z"/>
        </svg>
      )
    default:
      return (
        <svg viewBox="0 0 24 24" fill="currentColor" className={iconClasses}>
          <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
        </svg>
      )
  }
}

// Status badge colors
const statusColors: Record<string, { bg: string; text: string }> = {
  filled: { bg: '#d1fae5', text: '#065f46' },
  empty: { bg: '#f3f4f6', text: '#374151' },
  in_transit: { bg: '#fef3c7', text: '#92400e' },
  bottled: { bg: '#ede9fe', text: '#5b21b6' },
  aging: { bg: '#dbeafe', text: '#1e40af' }
}

/**
 * Barrel details bottom sheet modal
 */
export function BarrelDetailsSheet({
  barrel,
  history,
  rickhouses,
  onClose,
  onRecordMovement,
  onRecordGauge,
  onViewFullHistory,
  onPrintLabel
}: BarrelDetailsSheetProps) {
  const sheetRef = useRef<HTMLDivElement>(null)
  const [activeTab, setActiveTab] = useState<SheetTab>('info')
  const [actionSheet, setActionSheet] = useState<ActionSheet>(null)
  const [isExpanded, setIsExpanded] = useState(false)
  const [isSaving, setIsSaving] = useState(false)

  // Movement form state
  const [movementForm, setMovementForm] = useState<MovementFormData>({
    destinationRickhouseId: 0,
    notes: ''
  })

  // Gauge form state
  const [gaugeForm, setGaugeForm] = useState<GaugeRecordData>({
    proof: 0,
    temperature: 60,
    volume: 0,
    proofGallons: 0,
    notes: ''
  })

  // Touch handling for drag
  const touchStartY = useRef<number | null>(null)

  const handleTouchStart = useCallback((e: React.TouchEvent) => {
    touchStartY.current = e.touches[0].clientY
  }, [])

  const handleTouchEnd = useCallback((e: React.TouchEvent) => {
    if (touchStartY.current === null) return
    const deltaY = e.changedTouches[0].clientY - touchStartY.current

    if (deltaY > 100 && !isExpanded) {
      onClose()
    } else if (deltaY < -50 && !isExpanded) {
      setIsExpanded(true)
    } else if (deltaY > 50 && isExpanded) {
      setIsExpanded(false)
    }

    touchStartY.current = null
  }, [isExpanded, onClose])

  // Calculate proof gallons
  const calculateProofGallons = (proof: number, volume: number) => {
    return (proof * volume) / 100
  }

  const handleGaugeProofChange = (proof: number) => {
    const proofGallons = calculateProofGallons(proof, gaugeForm.volume)
    setGaugeForm(prev => ({ ...prev, proof, proofGallons }))
  }

  const handleGaugeVolumeChange = (volume: number) => {
    const proofGallons = calculateProofGallons(gaugeForm.proof, volume)
    setGaugeForm(prev => ({ ...prev, volume, proofGallons }))
  }

  // Submit handlers
  const handleMovementSubmit = async () => {
    if (!movementForm.destinationRickhouseId) return

    setIsSaving(true)
    try {
      await onRecordMovement(barrel.id, movementForm.destinationRickhouseId, movementForm.notes)
      setActionSheet(null)
      setMovementForm({ destinationRickhouseId: 0, notes: '' })
    } finally {
      setIsSaving(false)
    }
  }

  const handleGaugeSubmit = async () => {
    if (!gaugeForm.proof || !gaugeForm.volume) return

    setIsSaving(true)
    try {
      await onRecordGauge(barrel.id, gaugeForm)
      setActionSheet(null)
      setGaugeForm({ proof: 0, temperature: 60, volume: 0, proofGallons: 0, notes: '' })
    } finally {
      setIsSaving(false)
    }
  }

  const statusStyle = statusColors[barrel.status] || statusColors.aging

  return (
    <div className={styles.overlay} onClick={onClose}>
      <div
        ref={sheetRef}
        className={`${styles.sheet} ${isExpanded ? styles.expanded : ''}`}
        onClick={e => e.stopPropagation()}
        onTouchStart={handleTouchStart}
        onTouchEnd={handleTouchEnd}
      >
        {/* Drag handle */}
        <div className={styles.dragHandle}>
          <div className={styles.handle} />
        </div>

        {/* Header */}
        <div className={styles.header}>
          <div className={styles.headerInfo}>
            <h2 className={styles.sku}>{barrel.sku}</h2>
            <span
              className={styles.status}
              style={{ backgroundColor: statusStyle.bg, color: statusStyle.text }}
            >
              {barrel.status.replace('_', ' ')}
            </span>
          </div>
          <button
            type="button"
            className={styles.closeButton}
            onClick={onClose}
            aria-label="Close"
          >
            <CloseIcon />
          </button>
        </div>

        {/* Quick actions */}
        <div className={styles.quickActions}>
          <button
            type="button"
            className={styles.actionButton}
            onClick={() => setActionSheet('movement')}
          >
            Record Movement
          </button>
          <button
            type="button"
            className={styles.actionButton}
            onClick={() => setActionSheet('gauge')}
          >
            Record Gauge
          </button>
        </div>

        {/* Tabs */}
        <div className={styles.tabs}>
          <button
            type="button"
            className={`${styles.tab} ${activeTab === 'info' ? styles.active : ''}`}
            onClick={() => setActiveTab('info')}
          >
            Info
          </button>
          <button
            type="button"
            className={`${styles.tab} ${activeTab === 'history' ? styles.active : ''}`}
            onClick={() => setActiveTab('history')}
          >
            History
          </button>
        </div>

        {/* Content */}
        <div className={styles.content}>
          {activeTab === 'info' && (
            <div className={styles.infoTab}>
              <div className={styles.infoSection}>
                <h3 className={styles.sectionTitle}>Details</h3>
                <div className={styles.infoGrid}>
                  <div className={styles.infoItem}>
                    <span className={styles.infoLabel}>Fill Date</span>
                    <span className={styles.infoValue}>
                      {new Date(barrel.fillDate).toLocaleDateString()}
                    </span>
                  </div>
                  <div className={styles.infoItem}>
                    <span className={styles.infoLabel}>Age</span>
                    <span className={styles.infoValue}>{barrel.age}</span>
                  </div>
                  <div className={styles.infoItem}>
                    <span className={styles.infoLabel}>Location</span>
                    <span className={styles.infoValue}>
                      {barrel.warehouseName} / {barrel.rickhouseName}
                    </span>
                  </div>
                  <div className={styles.infoItem}>
                    <span className={styles.infoLabel}>Batch</span>
                    <span className={styles.infoValue}>{barrel.batchName}</span>
                  </div>
                  <div className={styles.infoItem}>
                    <span className={styles.infoLabel}>Spirit Type</span>
                    <span className={styles.infoValue}>{barrel.spiritType}</span>
                  </div>
                  {barrel.orderName && (
                    <div className={styles.infoItem}>
                      <span className={styles.infoLabel}>Order</span>
                      <span className={styles.infoValue}>{barrel.orderName}</span>
                    </div>
                  )}
                </div>
              </div>

              <div className={styles.infoSection}>
                <h3 className={styles.sectionTitle}>Volume</h3>
                <div className={styles.volumeStats}>
                  <div className={styles.volumeStat}>
                    <span className={styles.volumeValue}>{barrel.currentProofGallons.toFixed(2)}</span>
                    <span className={styles.volumeLabel}>Current PG</span>
                  </div>
                  <div className={styles.volumeDivider} />
                  <div className={styles.volumeStat}>
                    <span className={styles.volumeValue}>{barrel.originalProofGallons.toFixed(2)}</span>
                    <span className={styles.volumeLabel}>Original PG</span>
                  </div>
                  <div className={styles.volumeDivider} />
                  <div className={styles.volumeStat}>
                    <span className={`${styles.volumeValue} ${styles.loss}`}>
                      -{barrel.lossPercentage.toFixed(1)}%
                    </span>
                    <span className={styles.volumeLabel}>Loss</span>
                  </div>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'history' && (
            <div className={styles.historyTab}>
              {history.length === 0 ? (
                <div className={styles.emptyHistory}>
                  <span>No history available</span>
                </div>
              ) : (
                <div className={styles.historyList}>
                  {history.slice(0, 5).map(item => (
                    <div key={item.id} className={styles.historyItem}>
                      <HistoryIcon type={item.type} />
                      <div className={styles.historyContent}>
                        <span className={styles.historyTitle}>{item.title}</span>
                        {item.description && (
                          <span className={styles.historyDescription}>{item.description}</span>
                        )}
                        <span className={styles.historyTime}>
                          {new Date(item.timestamp).toLocaleDateString()} • {item.user}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
              <button
                type="button"
                className={styles.viewAllButton}
                onClick={onViewFullHistory}
              >
                View Full History
              </button>
            </div>
          )}
        </div>

        {/* Sticky footer actions */}
        <div className={styles.footer}>
          <button
            type="button"
            className={styles.footerButton}
            onClick={onPrintLabel}
          >
            Print Label
          </button>
        </div>

        {/* Movement action sheet */}
        {actionSheet === 'movement' && (
          <div className={styles.actionSheetOverlay} onClick={() => setActionSheet(null)}>
            <div className={styles.actionSheetContent} onClick={e => e.stopPropagation()}>
              <h3 className={styles.actionSheetTitle}>Record Movement</h3>
              <div className={styles.formGroup}>
                <label className={styles.formLabel}>Destination Rickhouse</label>
                <select
                  className={styles.formSelect}
                  value={movementForm.destinationRickhouseId}
                  onChange={e => setMovementForm(prev => ({ ...prev, destinationRickhouseId: Number(e.target.value) }))}
                >
                  <option value={0}>Select destination...</option>
                  {rickhouses
                    .filter(r => r.id !== barrel.rickhouseId)
                    .map(r => (
                      <option key={r.id} value={r.id}>
                        {r.warehouseName} / {r.name}
                      </option>
                    ))}
                </select>
              </div>
              <div className={styles.formGroup}>
                <label className={styles.formLabel}>Notes (optional)</label>
                <textarea
                  className={styles.formTextarea}
                  value={movementForm.notes}
                  onChange={e => setMovementForm(prev => ({ ...prev, notes: e.target.value }))}
                  placeholder="Add notes..."
                  rows={3}
                />
              </div>
              <div className={styles.actionSheetButtons}>
                <button
                  type="button"
                  className={styles.cancelButton}
                  onClick={() => setActionSheet(null)}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  className={styles.confirmButton}
                  onClick={handleMovementSubmit}
                  disabled={!movementForm.destinationRickhouseId || isSaving}
                >
                  {isSaving ? 'Saving...' : 'Confirm'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Gauge action sheet */}
        {actionSheet === 'gauge' && (
          <div className={styles.actionSheetOverlay} onClick={() => setActionSheet(null)}>
            <div className={styles.actionSheetContent} onClick={e => e.stopPropagation()}>
              <h3 className={styles.actionSheetTitle}>Record Gauge</h3>
              <div className={styles.formRow}>
                <div className={styles.formGroup}>
                  <label className={styles.formLabel}>Proof</label>
                  <input
                    type="number"
                    className={styles.formInput}
                    value={gaugeForm.proof || ''}
                    onChange={e => handleGaugeProofChange(Number(e.target.value))}
                    placeholder="0.0"
                    step="0.1"
                    min="0"
                    max="200"
                  />
                </div>
                <div className={styles.formGroup}>
                  <label className={styles.formLabel}>Temp (°F)</label>
                  <input
                    type="number"
                    className={styles.formInput}
                    value={gaugeForm.temperature || ''}
                    onChange={e => setGaugeForm(prev => ({ ...prev, temperature: Number(e.target.value) }))}
                    placeholder="60"
                    step="1"
                  />
                </div>
              </div>
              <div className={styles.formRow}>
                <div className={styles.formGroup}>
                  <label className={styles.formLabel}>Volume (gal)</label>
                  <input
                    type="number"
                    className={styles.formInput}
                    value={gaugeForm.volume || ''}
                    onChange={e => handleGaugeVolumeChange(Number(e.target.value))}
                    placeholder="0.00"
                    step="0.01"
                    min="0"
                  />
                </div>
                <div className={styles.formGroup}>
                  <label className={styles.formLabel}>Proof Gallons</label>
                  <input
                    type="number"
                    className={styles.formInput}
                    value={gaugeForm.proofGallons.toFixed(2)}
                    readOnly
                    disabled
                  />
                </div>
              </div>
              <div className={styles.formGroup}>
                <label className={styles.formLabel}>Notes (optional)</label>
                <textarea
                  className={styles.formTextarea}
                  value={gaugeForm.notes}
                  onChange={e => setGaugeForm(prev => ({ ...prev, notes: e.target.value }))}
                  placeholder="Add notes..."
                  rows={2}
                />
              </div>
              <div className={styles.actionSheetButtons}>
                <button
                  type="button"
                  className={styles.cancelButton}
                  onClick={() => setActionSheet(null)}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  className={styles.confirmButton}
                  onClick={handleGaugeSubmit}
                  disabled={!gaugeForm.proof || !gaugeForm.volume || isSaving}
                >
                  {isSaving ? 'Saving...' : 'Save'}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

export default BarrelDetailsSheet
