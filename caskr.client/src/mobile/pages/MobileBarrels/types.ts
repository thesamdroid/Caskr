/**
 * Types for Mobile Barrel Lookup
 */

export type BarrelStatus = 'filled' | 'empty' | 'in_transit' | 'bottled' | 'aging'

export interface BarrelDetail {
  id: number
  sku: string
  status: BarrelStatus
  rickhouseId: number
  rickhouseName: string
  warehouseId: number
  warehouseName: string
  batchId: number
  batchName: string
  spiritType: string
  fillDate: string
  age: string
  ageInDays: number
  currentProofGallons: number
  originalProofGallons: number
  lossPercentage: number
  proof: number
  temperature?: number
  orderId?: number
  orderName?: string
  lastGaugeDate?: string
  lastMovementDate?: string
}

export interface BarrelHistoryItem {
  id: string
  type: 'movement' | 'gauge' | 'fill' | 'bottle' | 'transfer'
  title: string
  description?: string
  timestamp: string
  user?: string
}

export interface BarrelSearchResult {
  id: number
  sku: string
  status: BarrelStatus
  rickhouseName: string
  age: string
  batchName: string
  spiritType: string
}

export interface ScanResult {
  type: 'qr' | 'barcode'
  value: string
  timestamp: Date
}

export type ScanMode = 'continuous' | 'single'

export interface UseBarrelLookupReturn {
  // Scan state
  scanResult: ScanResult | null
  isScannerActive: boolean
  toggleScanner: () => void
  clearScanResult: () => void

  // Search state
  searchQuery: string
  setSearchQuery: (query: string) => void
  searchResults: BarrelSearchResult[]
  isSearching: boolean
  searchError: string | null

  // Filter state
  filterStatus: BarrelStatus | 'all'
  setFilterStatus: (status: BarrelStatus | 'all') => void
  filterRickhouse: number | null
  setFilterRickhouse: (id: number | null) => void

  // Barrel detail state
  selectedBarrel: BarrelDetail | null
  barrelHistory: BarrelHistoryItem[]
  isLoadingDetail: boolean
  detailError: string | null
  loadBarrelDetail: (id: number) => Promise<void>
  loadBarrelBySku: (sku: string) => Promise<void>
  closeDetail: () => void

  // Recent barrels
  recentBarrels: BarrelSearchResult[]

  // Actions
  recordMovement: (barrelId: number, destinationId: number, notes?: string) => Promise<void>
  recordGauge: (barrelId: number, data: GaugeRecordData) => Promise<void>

  // Offline state
  isOffline: boolean
  pendingActions: PendingAction[]
}

export interface GaugeRecordData {
  proof: number
  temperature: number
  volume: number
  proofGallons: number
  notes?: string
}

export interface PendingAction {
  id: string
  type: 'movement' | 'gauge'
  barrelId: number
  data: GaugeRecordData | MovementFormData
  timestamp: string
}

export interface Rickhouse {
  id: number
  name: string
  warehouseId: number
  warehouseName: string
}

export interface MovementFormData {
  destinationRickhouseId: number
  notes?: string
}
