import { useState, useRef, useEffect, useCallback } from 'react'
import type { BarrelSearchResult, BarrelStatus, Rickhouse } from '../../types'
import styles from './BarrelSearch.module.css'

export interface BarrelSearchProps {
  searchQuery: string
  onSearchChange: (query: string) => void
  searchResults: BarrelSearchResult[]
  isSearching: boolean
  filterStatus: BarrelStatus | 'all'
  onFilterStatusChange: (status: BarrelStatus | 'all') => void
  filterRickhouse: number | null
  onFilterRickhouseChange: (id: number | null) => void
  rickhouses: Rickhouse[]
  onBarrelSelect: (barrelId: number) => void
}

// Search icon
const SearchIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.searchIcon}>
    <path d="M15.5 14h-.79l-.28-.27a6.5 6.5 0 0 0 1.48-5.34c-.47-2.78-2.79-5-5.59-5.34a6.505 6.505 0 0 0-7.27 7.27c.34 2.8 2.56 5.12 5.34 5.59a6.5 6.5 0 0 0 5.34-1.48l.27.28v.79l4.25 4.25c.41.41 1.08.41 1.49 0 .41-.41.41-1.08 0-1.49L15.5 14zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
  </svg>
)

// Clear icon
const ClearIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.clearIcon}>
    <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
  </svg>
)

// Voice icon
const VoiceIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.voiceIcon}>
    <path d="M12 15c1.66 0 2.99-1.34 2.99-3L15 5c0-1.66-1.34-3-3-3S9 3.34 9 5v7c0 1.66 1.34 3 3 3zm5.3-3c0 3-2.54 5.1-5.3 5.1S6.7 15 6.7 12H5c0 3.42 2.72 6.23 6 6.72V22h2v-3.28c3.28-.48 6-3.3 6-6.72h-1.7z"/>
  </svg>
)

// Chevron icon
const ChevronIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.chevronIcon}>
    <path d="M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z"/>
  </svg>
)

const statusLabels: Record<BarrelStatus | 'all', string> = {
  all: 'All',
  filled: 'Filled',
  empty: 'Empty',
  in_transit: 'In Transit',
  bottled: 'Bottled',
  aging: 'Aging'
}

const statusColors: Record<BarrelStatus, string> = {
  filled: '#10b981',
  empty: '#6b7280',
  in_transit: '#f59e0b',
  bottled: '#8b5cf6',
  aging: '#2563eb'
}

/**
 * Barrel search component with auto-focus, filters, and virtual scrolling
 */
export function BarrelSearch({
  searchQuery,
  onSearchChange,
  searchResults,
  isSearching,
  filterStatus,
  onFilterStatusChange,
  filterRickhouse,
  onFilterRickhouseChange,
  rickhouses,
  onBarrelSelect
}: BarrelSearchProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [showRickhouseDropdown, setShowRickhouseDropdown] = useState(false)
  const [isVoiceSupported] = useState(() => 'webkitSpeechRecognition' in window || 'SpeechRecognition' in window)

  // Auto-focus on mount
  useEffect(() => {
    inputRef.current?.focus()
  }, [])

  // Handle voice input
  const handleVoiceInput = useCallback(() => {
    if (!isVoiceSupported) return

    const SpeechRecognition = (window as Window & { webkitSpeechRecognition?: typeof window.SpeechRecognition }).webkitSpeechRecognition || window.SpeechRecognition
    const recognition = new SpeechRecognition()

    recognition.continuous = false
    recognition.interimResults = false

    recognition.onresult = (event: SpeechRecognitionEvent) => {
      const transcript = event.results[0][0].transcript
      onSearchChange(transcript)
    }

    recognition.onerror = () => {
      // Ignore errors
    }

    recognition.start()
  }, [isVoiceSupported, onSearchChange])

  // Clear search
  const handleClear = () => {
    onSearchChange('')
    inputRef.current?.focus()
  }

  // Get selected rickhouse name
  const selectedRickhouse = rickhouses.find(r => r.id === filterRickhouse)

  return (
    <div className={styles.searchContainer}>
      {/* Search input */}
      <div className={styles.searchBar}>
        <SearchIcon />
        <input
          ref={inputRef}
          type="text"
          value={searchQuery}
          onChange={e => onSearchChange(e.target.value)}
          placeholder="Search by SKU, batch, or location..."
          className={styles.searchInput}
          autoComplete="off"
          autoCorrect="off"
          spellCheck={false}
        />
        {searchQuery && (
          <button
            type="button"
            className={styles.clearButton}
            onClick={handleClear}
            aria-label="Clear search"
          >
            <ClearIcon />
          </button>
        )}
        {isVoiceSupported && !searchQuery && (
          <button
            type="button"
            className={styles.voiceButton}
            onClick={handleVoiceInput}
            aria-label="Voice search"
          >
            <VoiceIcon />
          </button>
        )}
      </div>

      {/* Filter chips */}
      <div className={styles.filters}>
        <div className={styles.filterChips}>
          {(Object.keys(statusLabels) as (BarrelStatus | 'all')[]).map(status => (
            <button
              key={status}
              type="button"
              className={`${styles.filterChip} ${filterStatus === status ? styles.active : ''}`}
              onClick={() => onFilterStatusChange(status)}
            >
              {statusLabels[status]}
            </button>
          ))}
        </div>

        {/* Rickhouse dropdown */}
        <div className={styles.rickhouseFilter}>
          <button
            type="button"
            className={styles.rickhouseButton}
            onClick={() => setShowRickhouseDropdown(!showRickhouseDropdown)}
          >
            <span>{selectedRickhouse?.name || 'All Rickhouses'}</span>
            <ChevronIcon />
          </button>

          {showRickhouseDropdown && (
            <div className={styles.rickhouseDropdown}>
              <button
                type="button"
                className={`${styles.rickhouseOption} ${filterRickhouse === null ? styles.active : ''}`}
                onClick={() => {
                  onFilterRickhouseChange(null)
                  setShowRickhouseDropdown(false)
                }}
              >
                All Rickhouses
              </button>
              {rickhouses.map(rickhouse => (
                <button
                  key={rickhouse.id}
                  type="button"
                  className={`${styles.rickhouseOption} ${filterRickhouse === rickhouse.id ? styles.active : ''}`}
                  onClick={() => {
                    onFilterRickhouseChange(rickhouse.id)
                    setShowRickhouseDropdown(false)
                  }}
                >
                  {rickhouse.name}
                  <span className={styles.warehouseName}>{rickhouse.warehouseName}</span>
                </button>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Results */}
      <div className={styles.results}>
        {isSearching && (
          <div className={styles.loadingState}>
            <div className={styles.spinner} />
            <span>Searching...</span>
          </div>
        )}

        {!isSearching && searchResults.length === 0 && searchQuery && (
          <div className={styles.emptyState}>
            <span className={styles.emptyTitle}>No barrels found</span>
            <span className={styles.emptySubtitle}>
              Try a different search term or adjust filters
            </span>
          </div>
        )}

        {!isSearching && searchResults.length > 0 && (
          <div className={styles.resultsList}>
            {searchResults.map(barrel => (
              <button
                key={barrel.id}
                type="button"
                className={styles.resultItem}
                onClick={() => onBarrelSelect(barrel.id)}
              >
                <div className={styles.resultMain}>
                  <span className={styles.resultSku}>{barrel.sku}</span>
                  <span
                    className={styles.resultStatus}
                    style={{ backgroundColor: statusColors[barrel.status] }}
                  >
                    {statusLabels[barrel.status]}
                  </span>
                </div>
                <div className={styles.resultDetails}>
                  <span>{barrel.rickhouseName}</span>
                  <span className={styles.separator}>•</span>
                  <span>{barrel.age}</span>
                  <span className={styles.separator}>•</span>
                  <span>{barrel.batchName}</span>
                </div>
                <ChevronIcon />
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

export default BarrelSearch
