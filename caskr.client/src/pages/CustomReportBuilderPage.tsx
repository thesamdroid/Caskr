import { useState, useMemo, DragEvent } from 'react'
import { useAppSelector } from '../hooks'
import { authorizedFetch } from '../api/authorizedFetch'

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

// Available data sources (tables) that can be queried
interface DataSource {
  id: string
  name: string
  displayName: string
  description: string
  columns: ColumnDefinition[]
  relationships: TableRelationship[]
}

// Column definition for each data source
interface ColumnDefinition {
  name: string
  displayName: string
  dataType: 'string' | 'number' | 'decimal' | 'date' | 'datetime' | 'boolean'
  sourceTable: string
  isPrimaryKey?: boolean
  isForeignKey?: boolean
  foreignKeyTable?: string
}

// Relationship between tables
interface TableRelationship {
  fromTable: string
  fromColumn: string
  toTable: string
  toColumn: string
  relationshipType: 'one-to-one' | 'one-to-many' | 'many-to-one'
}

// Selected column with configuration
interface SelectedColumn {
  id: string
  column: ColumnDefinition
  alias: string
  aggregation: AggregationType | null
  formatting: ColumnFormatting
}

// Aggregation types for numeric columns
type AggregationType = 'SUM' | 'AVG' | 'COUNT' | 'MIN' | 'MAX' | 'NONE'

// Formatting options for columns
interface ColumnFormatting {
  dateFormat?: string
  decimalPlaces?: number
  isCurrency?: boolean
  prefix?: string
  suffix?: string
}

// Filter definition
interface FilterDefinition {
  id: string
  column: ColumnDefinition
  operator: FilterOperator
  value: string | string[] | null
  logic: 'AND' | 'OR'
}

// Available filter operators
type FilterOperator = '=' | '!=' | '>' | '<' | '>=' | '<=' | 'IN' | 'BETWEEN' | 'LIKE' | 'IS NULL' | 'IS NOT NULL'

// Grouping definition
interface GroupByColumn {
  id: string
  column: ColumnDefinition
}

// Sorting definition
interface SortByColumn {
  id: string
  column: ColumnDefinition
  direction: 'ASC' | 'DESC'
}

// Report preview result
interface PreviewResult {
  columns: { name: string; displayName: string; dataType: string }[]
  rows: Record<string, unknown>[]
  totalRows: number
  executionTimeMs: number
  sql?: string
}

// Custom report template request
interface CreateCustomReportRequest {
  name: string
  description: string
  category: string
  dataSources: string[]
  selectedColumns: {
    columnName: string
    sourceTable: string
    alias: string
    aggregation: string | null
    formatting: ColumnFormatting
  }[]
  filters: {
    columnName: string
    sourceTable: string
    operator: string
    value: string | string[] | null
    logic: string
  }[]
  groupBy: { columnName: string; sourceTable: string }[]
  orderBy: { columnName: string; sourceTable: string; direction: string }[]
}

// ============================================================================
// STATIC DATA - AVAILABLE DATA SOURCES
// ============================================================================

const DATA_SOURCES: DataSource[] = [
  {
    id: 'barrels',
    name: 'Barrels',
    displayName: 'Barrels',
    description: 'Barrel inventory and aging data',
    columns: [
      { name: 'id', displayName: 'Barrel ID', dataType: 'number', sourceTable: 'barrels', isPrimaryKey: true },
      { name: 'barrel_number', displayName: 'Barrel Number', dataType: 'string', sourceTable: 'barrels' },
      { name: 'batch_id', displayName: 'Batch ID', dataType: 'number', sourceTable: 'barrels', isForeignKey: true, foreignKeyTable: 'batches' },
      { name: 'fill_date', displayName: 'Fill Date', dataType: 'date', sourceTable: 'barrels' },
      { name: 'empty_date', displayName: 'Empty Date', dataType: 'date', sourceTable: 'barrels' },
      { name: 'original_proof_gallons', displayName: 'Original Proof Gallons', dataType: 'decimal', sourceTable: 'barrels' },
      { name: 'current_proof_gallons', displayName: 'Current Proof Gallons', dataType: 'decimal', sourceTable: 'barrels' },
      { name: 'proof', displayName: 'Proof', dataType: 'decimal', sourceTable: 'barrels' },
      { name: 'warehouse_id', displayName: 'Warehouse ID', dataType: 'number', sourceTable: 'barrels' },
      { name: 'location', displayName: 'Location', dataType: 'string', sourceTable: 'barrels' },
      { name: 'status', displayName: 'Status', dataType: 'string', sourceTable: 'barrels' },
      { name: 'barrel_type', displayName: 'Barrel Type', dataType: 'string', sourceTable: 'barrels' },
      { name: 'char_level', displayName: 'Char Level', dataType: 'string', sourceTable: 'barrels' },
      { name: 'age_days', displayName: 'Age (Days)', dataType: 'number', sourceTable: 'barrels' },
      { name: 'created_at', displayName: 'Created At', dataType: 'datetime', sourceTable: 'barrels' }
    ],
    relationships: [
      { fromTable: 'barrels', fromColumn: 'batch_id', toTable: 'batches', toColumn: 'id', relationshipType: 'many-to-one' }
    ]
  },
  {
    id: 'batches',
    name: 'Batches',
    displayName: 'Batches',
    description: 'Production batch records',
    columns: [
      { name: 'id', displayName: 'Batch ID', dataType: 'number', sourceTable: 'batches', isPrimaryKey: true },
      { name: 'batch_number', displayName: 'Batch Number', dataType: 'string', sourceTable: 'batches' },
      { name: 'mash_bill_id', displayName: 'Mash Bill ID', dataType: 'number', sourceTable: 'batches', isForeignKey: true, foreignKeyTable: 'mashbills' },
      { name: 'product_id', displayName: 'Product ID', dataType: 'number', sourceTable: 'batches', isForeignKey: true, foreignKeyTable: 'products' },
      { name: 'production_date', displayName: 'Production Date', dataType: 'date', sourceTable: 'batches' },
      { name: 'total_gallons', displayName: 'Total Gallons', dataType: 'decimal', sourceTable: 'batches' },
      { name: 'proof', displayName: 'Proof', dataType: 'decimal', sourceTable: 'batches' },
      { name: 'status', displayName: 'Status', dataType: 'string', sourceTable: 'batches' },
      { name: 'notes', displayName: 'Notes', dataType: 'string', sourceTable: 'batches' },
      { name: 'created_at', displayName: 'Created At', dataType: 'datetime', sourceTable: 'batches' }
    ],
    relationships: [
      { fromTable: 'batches', fromColumn: 'mash_bill_id', toTable: 'mashbills', toColumn: 'id', relationshipType: 'many-to-one' },
      { fromTable: 'batches', fromColumn: 'product_id', toTable: 'products', toColumn: 'id', relationshipType: 'many-to-one' }
    ]
  },
  {
    id: 'orders',
    name: 'Orders',
    displayName: 'Orders',
    description: 'Customer orders and sales data',
    columns: [
      { name: 'id', displayName: 'Order ID', dataType: 'number', sourceTable: 'orders', isPrimaryKey: true },
      { name: 'order_number', displayName: 'Order Number', dataType: 'string', sourceTable: 'orders' },
      { name: 'customer_name', displayName: 'Customer Name', dataType: 'string', sourceTable: 'orders' },
      { name: 'order_date', displayName: 'Order Date', dataType: 'date', sourceTable: 'orders' },
      { name: 'ship_date', displayName: 'Ship Date', dataType: 'date', sourceTable: 'orders' },
      { name: 'status', displayName: 'Status', dataType: 'string', sourceTable: 'orders' },
      { name: 'total_cases', displayName: 'Total Cases', dataType: 'number', sourceTable: 'orders' },
      { name: 'total_bottles', displayName: 'Total Bottles', dataType: 'number', sourceTable: 'orders' },
      { name: 'total_amount', displayName: 'Total Amount', dataType: 'decimal', sourceTable: 'orders' },
      { name: 'notes', displayName: 'Notes', dataType: 'string', sourceTable: 'orders' },
      { name: 'created_at', displayName: 'Created At', dataType: 'datetime', sourceTable: 'orders' }
    ],
    relationships: []
  },
  {
    id: 'tasks',
    name: 'Tasks',
    displayName: 'Tasks',
    description: 'Production and operations tasks',
    columns: [
      { name: 'id', displayName: 'Task ID', dataType: 'number', sourceTable: 'tasks', isPrimaryKey: true },
      { name: 'title', displayName: 'Title', dataType: 'string', sourceTable: 'tasks' },
      { name: 'description', displayName: 'Description', dataType: 'string', sourceTable: 'tasks' },
      { name: 'task_type', displayName: 'Task Type', dataType: 'string', sourceTable: 'tasks' },
      { name: 'priority', displayName: 'Priority', dataType: 'string', sourceTable: 'tasks' },
      { name: 'status', displayName: 'Status', dataType: 'string', sourceTable: 'tasks' },
      { name: 'assigned_to', displayName: 'Assigned To', dataType: 'string', sourceTable: 'tasks' },
      { name: 'due_date', displayName: 'Due Date', dataType: 'date', sourceTable: 'tasks' },
      { name: 'completed_date', displayName: 'Completed Date', dataType: 'date', sourceTable: 'tasks' },
      { name: 'created_at', displayName: 'Created At', dataType: 'datetime', sourceTable: 'tasks' }
    ],
    relationships: []
  },
  {
    id: 'transfers',
    name: 'Transfers',
    displayName: 'Transfers',
    description: 'TTB transfer and movement records',
    columns: [
      { name: 'id', displayName: 'Transfer ID', dataType: 'number', sourceTable: 'transfers', isPrimaryKey: true },
      { name: 'transfer_number', displayName: 'Transfer Number', dataType: 'string', sourceTable: 'transfers' },
      { name: 'transfer_type', displayName: 'Transfer Type', dataType: 'string', sourceTable: 'transfers' },
      { name: 'from_location', displayName: 'From Location', dataType: 'string', sourceTable: 'transfers' },
      { name: 'to_location', displayName: 'To Location', dataType: 'string', sourceTable: 'transfers' },
      { name: 'barrel_count', displayName: 'Barrel Count', dataType: 'number', sourceTable: 'transfers' },
      { name: 'proof_gallons', displayName: 'Proof Gallons', dataType: 'decimal', sourceTable: 'transfers' },
      { name: 'transfer_date', displayName: 'Transfer Date', dataType: 'date', sourceTable: 'transfers' },
      { name: 'status', displayName: 'Status', dataType: 'string', sourceTable: 'transfers' },
      { name: 'notes', displayName: 'Notes', dataType: 'string', sourceTable: 'transfers' },
      { name: 'created_at', displayName: 'Created At', dataType: 'datetime', sourceTable: 'transfers' }
    ],
    relationships: []
  },
  {
    id: 'products',
    name: 'Products',
    displayName: 'Products',
    description: 'Product catalog and specifications',
    columns: [
      { name: 'id', displayName: 'Product ID', dataType: 'number', sourceTable: 'products', isPrimaryKey: true },
      { name: 'name', displayName: 'Product Name', dataType: 'string', sourceTable: 'products' },
      { name: 'sku', displayName: 'SKU', dataType: 'string', sourceTable: 'products' },
      { name: 'product_type', displayName: 'Product Type', dataType: 'string', sourceTable: 'products' },
      { name: 'proof', displayName: 'Proof', dataType: 'decimal', sourceTable: 'products' },
      { name: 'bottle_size_ml', displayName: 'Bottle Size (mL)', dataType: 'number', sourceTable: 'products' },
      { name: 'cases_per_pallet', displayName: 'Cases Per Pallet', dataType: 'number', sourceTable: 'products' },
      { name: 'price', displayName: 'Price', dataType: 'decimal', sourceTable: 'products' },
      { name: 'is_active', displayName: 'Is Active', dataType: 'boolean', sourceTable: 'products' },
      { name: 'created_at', displayName: 'Created At', dataType: 'datetime', sourceTable: 'products' }
    ],
    relationships: []
  },
  {
    id: 'mashbills',
    name: 'MashBills',
    displayName: 'Mash Bills',
    description: 'Grain recipes and formulations',
    columns: [
      { name: 'id', displayName: 'Mash Bill ID', dataType: 'number', sourceTable: 'mashbills', isPrimaryKey: true },
      { name: 'name', displayName: 'Mash Bill Name', dataType: 'string', sourceTable: 'mashbills' },
      { name: 'description', displayName: 'Description', dataType: 'string', sourceTable: 'mashbills' },
      { name: 'corn_percent', displayName: 'Corn %', dataType: 'decimal', sourceTable: 'mashbills' },
      { name: 'rye_percent', displayName: 'Rye %', dataType: 'decimal', sourceTable: 'mashbills' },
      { name: 'wheat_percent', displayName: 'Wheat %', dataType: 'decimal', sourceTable: 'mashbills' },
      { name: 'barley_percent', displayName: 'Barley %', dataType: 'decimal', sourceTable: 'mashbills' },
      { name: 'is_active', displayName: 'Is Active', dataType: 'boolean', sourceTable: 'mashbills' },
      { name: 'created_at', displayName: 'Created At', dataType: 'datetime', sourceTable: 'mashbills' }
    ],
    relationships: []
  },
  {
    id: 'companies',
    name: 'Companies',
    displayName: 'Companies',
    description: 'Company and partner information',
    columns: [
      { name: 'id', displayName: 'Company ID', dataType: 'number', sourceTable: 'companies', isPrimaryKey: true },
      { name: 'name', displayName: 'Company Name', dataType: 'string', sourceTable: 'companies' },
      { name: 'contact_name', displayName: 'Contact Name', dataType: 'string', sourceTable: 'companies' },
      { name: 'email', displayName: 'Email', dataType: 'string', sourceTable: 'companies' },
      { name: 'phone', displayName: 'Phone', dataType: 'string', sourceTable: 'companies' },
      { name: 'address', displayName: 'Address', dataType: 'string', sourceTable: 'companies' },
      { name: 'city', displayName: 'City', dataType: 'string', sourceTable: 'companies' },
      { name: 'state', displayName: 'State', dataType: 'string', sourceTable: 'companies' },
      { name: 'zip', displayName: 'ZIP', dataType: 'string', sourceTable: 'companies' },
      { name: 'is_active', displayName: 'Is Active', dataType: 'boolean', sourceTable: 'companies' },
      { name: 'created_at', displayName: 'Created At', dataType: 'datetime', sourceTable: 'companies' }
    ],
    relationships: []
  }
]

// Filter operators
const FILTER_OPERATORS: { value: FilterOperator; label: string; requiresValue: boolean; requiresMultiple: boolean }[] = [
  { value: '=', label: 'Equals', requiresValue: true, requiresMultiple: false },
  { value: '!=', label: 'Not Equals', requiresValue: true, requiresMultiple: false },
  { value: '>', label: 'Greater Than', requiresValue: true, requiresMultiple: false },
  { value: '<', label: 'Less Than', requiresValue: true, requiresMultiple: false },
  { value: '>=', label: 'Greater Than or Equal', requiresValue: true, requiresMultiple: false },
  { value: '<=', label: 'Less Than or Equal', requiresValue: true, requiresMultiple: false },
  { value: 'IN', label: 'In List', requiresValue: true, requiresMultiple: true },
  { value: 'BETWEEN', label: 'Between', requiresValue: true, requiresMultiple: true },
  { value: 'LIKE', label: 'Contains', requiresValue: true, requiresMultiple: false },
  { value: 'IS NULL', label: 'Is Empty', requiresValue: false, requiresMultiple: false },
  { value: 'IS NOT NULL', label: 'Is Not Empty', requiresValue: false, requiresMultiple: false }
]

// Report categories
const REPORT_CATEGORIES = ['Custom Reports', 'Financial', 'Inventory', 'Production', 'Compliance', 'General']

// ============================================================================
// UTILITY FUNCTIONS
// ============================================================================

// Generate unique ID
const generateId = () => `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`

// Format cell value for display
const formatCellValue = (value: unknown, dataType: string): string => {
  if (value === null || value === undefined) return '—'

  switch (dataType) {
    case 'datetime': {
      const dt = new Date(value as string)
      if (isNaN(dt.getTime())) return String(value)
      return new Intl.DateTimeFormat('en-US', {
        month: 'short', day: 'numeric', year: 'numeric',
        hour: 'numeric', minute: '2-digit'
      }).format(dt)
    }
    case 'date': {
      const d = new Date(value as string)
      if (isNaN(d.getTime())) return String(value)
      return new Intl.DateTimeFormat('en-US', {
        month: 'short', day: 'numeric', year: 'numeric'
      }).format(d)
    }
    case 'decimal':
      return new Intl.NumberFormat('en-US', {
        minimumFractionDigits: 2, maximumFractionDigits: 2
      }).format(Number(value))
    case 'number':
      return new Intl.NumberFormat('en-US').format(Number(value))
    case 'boolean':
      return value ? 'Yes' : 'No'
    default:
      return String(value)
  }
}

// ============================================================================
// WIZARD STEPS
// ============================================================================

type WizardStep = 'sources' | 'columns' | 'filters' | 'grouping' | 'sorting' | 'preview' | 'save'

const WIZARD_STEPS: { id: WizardStep; title: string; number: number }[] = [
  { id: 'sources', title: 'Data Sources', number: 1 },
  { id: 'columns', title: 'Select Columns', number: 2 },
  { id: 'filters', title: 'Filters', number: 3 },
  { id: 'grouping', title: 'Grouping', number: 4 },
  { id: 'sorting', title: 'Sorting', number: 5 },
  { id: 'preview', title: 'Preview', number: 6 },
  { id: 'save', title: 'Save', number: 7 }
]

// ============================================================================
// MAIN COMPONENT
// ============================================================================

function CustomReportBuilderPage() {
  const authUser = useAppSelector(state => state.auth.user)
  const companyId = authUser?.companyId ?? 1

  // Wizard state
  const [currentStep, setCurrentStep] = useState<WizardStep>('sources')

  // Data source selection (Step 1)
  const [selectedSources, setSelectedSources] = useState<Set<string>>(new Set())

  // Column selection (Step 2)
  const [selectedColumns, setSelectedColumns] = useState<SelectedColumn[]>([])
  const [editingColumn, setEditingColumn] = useState<string | null>(null)

  // Filters (Step 3)
  const [filters, setFilters] = useState<FilterDefinition[]>([])

  // Grouping (Step 4)
  const [groupByColumns, setGroupByColumns] = useState<GroupByColumn[]>([])

  // Sorting (Step 5)
  const [sortByColumns, setSortByColumns] = useState<SortByColumn[]>([])

  // Preview (Step 6)
  const [previewResult, setPreviewResult] = useState<PreviewResult | null>(null)
  const [isPreviewLoading, setIsPreviewLoading] = useState(false)
  const [previewError, setPreviewError] = useState<string | null>(null)

  // Save (Step 7)
  const [reportName, setReportName] = useState('')
  const [reportDescription, setReportDescription] = useState('')
  const [reportCategory, setReportCategory] = useState('Custom Reports')
  const [isSaving, setIsSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [saveSuccess, setSaveSuccess] = useState(false)

  // Drag and drop state
  const [draggedColumn, setDraggedColumn] = useState<ColumnDefinition | null>(null)
  const [dragOverTarget, setDragOverTarget] = useState<string | null>(null)

  // Available columns from selected sources
  const availableColumns = useMemo(() => {
    const columns: ColumnDefinition[] = []
    selectedSources.forEach(sourceId => {
      const source = DATA_SOURCES.find(s => s.id === sourceId)
      if (source) {
        columns.push(...source.columns)
      }
    })
    return columns
  }, [selectedSources])

  // Detected relationships between selected sources
  const detectedRelationships = useMemo(() => {
    const relationships: TableRelationship[] = []
    selectedSources.forEach(sourceId => {
      const source = DATA_SOURCES.find(s => s.id === sourceId)
      if (source) {
        source.relationships.forEach(rel => {
          if (selectedSources.has(rel.toTable)) {
            relationships.push(rel)
          }
        })
      }
    })
    return relationships
  }, [selectedSources])

  // ============================================================================
  // STEP NAVIGATION
  // ============================================================================

  const canProceedToStep = (step: WizardStep): boolean => {
    switch (step) {
      case 'sources':
        return true
      case 'columns':
        return selectedSources.size > 0
      case 'filters':
        return selectedColumns.length > 0
      case 'grouping':
        return selectedColumns.length > 0
      case 'sorting':
        return selectedColumns.length > 0
      case 'preview':
        return selectedColumns.length > 0
      case 'save':
        return selectedColumns.length > 0 && previewResult !== null
      default:
        return false
    }
  }

  const goToStep = (step: WizardStep) => {
    if (canProceedToStep(step)) {
      setCurrentStep(step)
    }
  }

  const goToNextStep = () => {
    const currentIndex = WIZARD_STEPS.findIndex(s => s.id === currentStep)
    if (currentIndex < WIZARD_STEPS.length - 1) {
      const nextStep = WIZARD_STEPS[currentIndex + 1]
      if (canProceedToStep(nextStep.id)) {
        setCurrentStep(nextStep.id)
      }
    }
  }

  const goToPreviousStep = () => {
    const currentIndex = WIZARD_STEPS.findIndex(s => s.id === currentStep)
    if (currentIndex > 0) {
      setCurrentStep(WIZARD_STEPS[currentIndex - 1].id)
    }
  }

  // ============================================================================
  // DATA SOURCE HANDLERS
  // ============================================================================

  const toggleDataSource = (sourceId: string) => {
    const newSelected = new Set(selectedSources)
    if (newSelected.has(sourceId)) {
      newSelected.delete(sourceId)
      // Remove columns from deselected source
      setSelectedColumns(cols => cols.filter(c => c.column.sourceTable !== sourceId))
      setFilters(f => f.filter(flt => flt.column.sourceTable !== sourceId))
      setGroupByColumns(g => g.filter(grp => grp.column.sourceTable !== sourceId))
      setSortByColumns(s => s.filter(srt => srt.column.sourceTable !== sourceId))
    } else {
      newSelected.add(sourceId)
    }
    setSelectedSources(newSelected)
  }

  // ============================================================================
  // DRAG AND DROP HANDLERS
  // ============================================================================

  const handleDragStart = (e: DragEvent, column: ColumnDefinition) => {
    setDraggedColumn(column)
    e.dataTransfer.effectAllowed = 'copy'
    e.dataTransfer.setData('text/plain', JSON.stringify(column))
    if (e.currentTarget instanceof HTMLElement) {
      e.currentTarget.classList.add('dragging')
    }
  }

  const handleDragEnd = (e: DragEvent) => {
    setDraggedColumn(null)
    setDragOverTarget(null)
    if (e.currentTarget instanceof HTMLElement) {
      e.currentTarget.classList.remove('dragging')
    }
  }

  const handleDragOver = (e: DragEvent, targetId: string) => {
    e.preventDefault()
    e.dataTransfer.dropEffect = 'copy'
    setDragOverTarget(targetId)
  }

  const handleDragLeave = (_e: DragEvent) => {
    setDragOverTarget(null)
  }

  const handleDropOnSelectedColumns = (e: DragEvent) => {
    e.preventDefault()
    setDragOverTarget(null)

    if (!draggedColumn) return

    // Check if already added
    if (selectedColumns.some(c => c.column.name === draggedColumn.name && c.column.sourceTable === draggedColumn.sourceTable)) {
      return
    }

    const newColumn: SelectedColumn = {
      id: generateId(),
      column: draggedColumn,
      alias: draggedColumn.displayName,
      aggregation: null,
      formatting: {}
    }

    setSelectedColumns([...selectedColumns, newColumn])
  }

  const handleDropOnGroupBy = (e: DragEvent) => {
    e.preventDefault()
    setDragOverTarget(null)

    if (!draggedColumn) return

    // Check if already added
    if (groupByColumns.some(g => g.column.name === draggedColumn.name && g.column.sourceTable === draggedColumn.sourceTable)) {
      return
    }

    const newGroup: GroupByColumn = {
      id: generateId(),
      column: draggedColumn
    }

    setGroupByColumns([...groupByColumns, newGroup])
  }

  const handleDropOnSortBy = (e: DragEvent) => {
    e.preventDefault()
    setDragOverTarget(null)

    if (!draggedColumn) return

    // Check if already added
    if (sortByColumns.some(s => s.column.name === draggedColumn.name && s.column.sourceTable === draggedColumn.sourceTable)) {
      return
    }

    const newSort: SortByColumn = {
      id: generateId(),
      column: draggedColumn,
      direction: 'ASC'
    }

    setSortByColumns([...sortByColumns, newSort])
  }

  // ============================================================================
  // COLUMN CONFIGURATION HANDLERS
  // ============================================================================

  const removeSelectedColumn = (columnId: string) => {
    setSelectedColumns(cols => cols.filter(c => c.id !== columnId))
  }

  const updateColumnAlias = (columnId: string, alias: string) => {
    setSelectedColumns(cols => cols.map(c =>
      c.id === columnId ? { ...c, alias } : c
    ))
  }

  const updateColumnAggregation = (columnId: string, aggregation: AggregationType | null) => {
    setSelectedColumns(cols => cols.map(c =>
      c.id === columnId ? { ...c, aggregation } : c
    ))
  }

  const updateColumnFormatting = (columnId: string, formatting: Partial<ColumnFormatting>) => {
    setSelectedColumns(cols => cols.map(c =>
      c.id === columnId ? { ...c, formatting: { ...c.formatting, ...formatting } } : c
    ))
  }

  const moveColumn = (columnId: string, direction: 'up' | 'down') => {
    const index = selectedColumns.findIndex(c => c.id === columnId)
    if (index === -1) return

    const newIndex = direction === 'up' ? index - 1 : index + 1
    if (newIndex < 0 || newIndex >= selectedColumns.length) return

    const newColumns = [...selectedColumns]
    const [removed] = newColumns.splice(index, 1)
    newColumns.splice(newIndex, 0, removed)
    setSelectedColumns(newColumns)
  }

  // ============================================================================
  // FILTER HANDLERS
  // ============================================================================

  const addFilter = () => {
    if (availableColumns.length === 0) return

    const newFilter: FilterDefinition = {
      id: generateId(),
      column: availableColumns[0],
      operator: '=',
      value: '',
      logic: 'AND'
    }

    setFilters([...filters, newFilter])
  }

  const updateFilter = (filterId: string, updates: Partial<FilterDefinition>) => {
    setFilters(f => f.map(flt =>
      flt.id === filterId ? { ...flt, ...updates } : flt
    ))
  }

  const removeFilter = (filterId: string) => {
    setFilters(f => f.filter(flt => flt.id !== filterId))
  }

  // ============================================================================
  // GROUPING HANDLERS
  // ============================================================================

  const removeGroupBy = (groupId: string) => {
    setGroupByColumns(g => g.filter(grp => grp.id !== groupId))
  }

  // ============================================================================
  // SORTING HANDLERS
  // ============================================================================

  const removeSortBy = (sortId: string) => {
    setSortByColumns(s => s.filter(srt => srt.id !== sortId))
  }

  const toggleSortDirection = (sortId: string) => {
    setSortByColumns(s => s.map(srt =>
      srt.id === sortId ? { ...srt, direction: srt.direction === 'ASC' ? 'DESC' : 'ASC' } : srt
    ))
  }

  // ============================================================================
  // PREVIEW HANDLER
  // ============================================================================

  const executePreview = async () => {
    setIsPreviewLoading(true)
    setPreviewError(null)

    try {
      const request: CreateCustomReportRequest = {
        name: 'Preview',
        description: '',
        category: 'Custom Reports',
        dataSources: Array.from(selectedSources),
        selectedColumns: selectedColumns.map(c => ({
          columnName: c.column.name,
          sourceTable: c.column.sourceTable,
          alias: c.alias,
          aggregation: c.aggregation,
          formatting: c.formatting
        })),
        filters: filters.map(f => ({
          columnName: f.column.name,
          sourceTable: f.column.sourceTable,
          operator: f.operator,
          value: f.value,
          logic: f.logic
        })),
        groupBy: groupByColumns.map(g => ({
          columnName: g.column.name,
          sourceTable: g.column.sourceTable
        })),
        orderBy: sortByColumns.map(s => ({
          columnName: s.column.name,
          sourceTable: s.column.sourceTable,
          direction: s.direction
        }))
      }

      const response = await authorizedFetch(`api/reports/custom/preview/company/${companyId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request)
      })

      if (!response.ok) {
        const error = await response.json()
        throw new Error(error.error || 'Preview failed')
      }

      const result = await response.json()
      setPreviewResult(result)
    } catch (err) {
      setPreviewError(err instanceof Error ? err.message : 'An error occurred')
    } finally {
      setIsPreviewLoading(false)
    }
  }

  // ============================================================================
  // SAVE HANDLER
  // ============================================================================

  const saveCustomReport = async () => {
    if (!reportName.trim()) {
      setSaveError('Report name is required')
      return
    }

    setIsSaving(true)
    setSaveError(null)
    setSaveSuccess(false)

    try {
      const request: CreateCustomReportRequest = {
        name: reportName.trim(),
        description: reportDescription.trim(),
        category: reportCategory,
        dataSources: Array.from(selectedSources),
        selectedColumns: selectedColumns.map(c => ({
          columnName: c.column.name,
          sourceTable: c.column.sourceTable,
          alias: c.alias,
          aggregation: c.aggregation,
          formatting: c.formatting
        })),
        filters: filters.map(f => ({
          columnName: f.column.name,
          sourceTable: f.column.sourceTable,
          operator: f.operator,
          value: f.value,
          logic: f.logic
        })),
        groupBy: groupByColumns.map(g => ({
          columnName: g.column.name,
          sourceTable: g.column.sourceTable
        })),
        orderBy: sortByColumns.map(s => ({
          columnName: s.column.name,
          sourceTable: s.column.sourceTable,
          direction: s.direction
        }))
      }

      const response = await authorizedFetch(`api/reports/custom/company/${companyId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request)
      })

      if (!response.ok) {
        const error = await response.json()
        throw new Error(error.error || 'Failed to save report')
      }

      setSaveSuccess(true)
      // Reset form after successful save
      setTimeout(() => {
        setReportName('')
        setReportDescription('')
        setSelectedSources(new Set())
        setSelectedColumns([])
        setFilters([])
        setGroupByColumns([])
        setSortByColumns([])
        setPreviewResult(null)
        setCurrentStep('sources')
        setSaveSuccess(false)
      }, 2000)
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : 'An error occurred')
    } finally {
      setIsSaving(false)
    }
  }

  // ============================================================================
  // RENDER HELPERS
  // ============================================================================

  const renderFilterValueInput = (filter: FilterDefinition) => {
    const operator = FILTER_OPERATORS.find(op => op.value === filter.operator)

    if (!operator?.requiresValue) {
      return null
    }

    if (filter.column.dataType === 'date' || filter.column.dataType === 'datetime') {
      if (filter.operator === 'BETWEEN') {
        const values = (filter.value as string[] || ['', ''])
        return (
          <div className="filter-value-between">
            <input
              type="date"
              className="form-input"
              value={values[0] || ''}
              onChange={e => updateFilter(filter.id, { value: [e.target.value, values[1] || ''] })}
            />
            <span>and</span>
            <input
              type="date"
              className="form-input"
              value={values[1] || ''}
              onChange={e => updateFilter(filter.id, { value: [values[0] || '', e.target.value] })}
            />
          </div>
        )
      }
      return (
        <input
          type="date"
          className="form-input"
          value={(filter.value as string) || ''}
          onChange={e => updateFilter(filter.id, { value: e.target.value })}
        />
      )
    }

    if (filter.column.dataType === 'number' || filter.column.dataType === 'decimal') {
      if (filter.operator === 'BETWEEN') {
        const values = (filter.value as string[] || ['', ''])
        return (
          <div className="filter-value-between">
            <input
              type="number"
              className="form-input"
              placeholder="Min"
              value={values[0] || ''}
              onChange={e => updateFilter(filter.id, { value: [e.target.value, values[1] || ''] })}
            />
            <span>and</span>
            <input
              type="number"
              className="form-input"
              placeholder="Max"
              value={values[1] || ''}
              onChange={e => updateFilter(filter.id, { value: [values[0] || '', e.target.value] })}
            />
          </div>
        )
      }
      if (filter.operator === 'IN') {
        return (
          <input
            type="text"
            className="form-input"
            placeholder="Enter comma-separated values"
            value={Array.isArray(filter.value) ? filter.value.join(', ') : (filter.value || '')}
            onChange={e => updateFilter(filter.id, { value: e.target.value.split(',').map(v => v.trim()) })}
          />
        )
      }
      return (
        <input
          type="number"
          className="form-input"
          value={(filter.value as string) || ''}
          onChange={e => updateFilter(filter.id, { value: e.target.value })}
        />
      )
    }

    if (filter.column.dataType === 'boolean') {
      return (
        <select
          className="form-select"
          value={(filter.value as string) || ''}
          onChange={e => updateFilter(filter.id, { value: e.target.value })}
        >
          <option value="">Select...</option>
          <option value="true">Yes</option>
          <option value="false">No</option>
        </select>
      )
    }

    // Default text input
    if (filter.operator === 'IN') {
      return (
        <input
          type="text"
          className="form-input"
          placeholder="Enter comma-separated values"
          value={Array.isArray(filter.value) ? filter.value.join(', ') : (filter.value || '')}
          onChange={e => updateFilter(filter.id, { value: e.target.value.split(',').map(v => v.trim()) })}
        />
      )
    }

    return (
      <input
        type="text"
        className="form-input"
        placeholder={filter.operator === 'LIKE' ? 'Enter search text...' : 'Enter value...'}
        value={(filter.value as string) || ''}
        onChange={e => updateFilter(filter.id, { value: e.target.value })}
      />
    )
  }

  // ============================================================================
  // RENDER STEP CONTENT
  // ============================================================================

  const renderStepContent = () => {
    switch (currentStep) {
      case 'sources':
        return (
          <div className="step-content step-sources">
            <h3 className="step-title">Select Data Sources</h3>
            <p className="step-description">
              Choose the tables you want to include in your report. The system will automatically detect relationships between tables.
            </p>

            <div className="data-sources-grid">
              {DATA_SOURCES.map(source => (
                <div
                  key={source.id}
                  className={`data-source-card ${selectedSources.has(source.id) ? 'selected' : ''}`}
                  onClick={() => toggleDataSource(source.id)}
                  role="checkbox"
                  aria-checked={selectedSources.has(source.id)}
                  tabIndex={0}
                  onKeyDown={e => e.key === 'Enter' && toggleDataSource(source.id)}
                >
                  <div className="source-checkbox">
                    {selectedSources.has(source.id) ? '✓' : ''}
                  </div>
                  <div className="source-info">
                    <h4 className="source-name">{source.displayName}</h4>
                    <p className="source-description">{source.description}</p>
                    <span className="source-columns">{source.columns.length} columns</span>
                  </div>
                </div>
              ))}
            </div>

            {detectedRelationships.length > 0 && (
              <div className="detected-relationships">
                <h4>Detected Relationships</h4>
                <ul>
                  {detectedRelationships.map((rel, idx) => (
                    <li key={idx}>
                      {rel.fromTable}.{rel.fromColumn} → {rel.toTable}.{rel.toColumn}
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        )

      case 'columns':
        return (
          <div className="step-content step-columns">
            <h3 className="step-title">Select and Configure Columns</h3>
            <p className="step-description">
              Drag columns from the left panel to the selected columns panel. Configure display names, aggregations, and formatting.
            </p>

            <div className="columns-layout">
              {/* Available Columns Panel */}
              <div className="columns-panel available-columns">
                <h4 className="panel-title">Available Columns</h4>
                <div className="columns-list">
                  {availableColumns.map(column => (
                    <div
                      key={`${column.sourceTable}-${column.name}`}
                      className="column-item"
                      draggable
                      onDragStart={e => handleDragStart(e, column)}
                      onDragEnd={handleDragEnd}
                    >
                      <span className="column-source">{column.sourceTable}</span>
                      <span className="column-name">{column.displayName}</span>
                      <span className="column-type">{column.dataType}</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Selected Columns Panel */}
              <div
                className={`columns-panel selected-columns-panel ${dragOverTarget === 'selected' ? 'drag-over' : ''}`}
                onDragOver={e => handleDragOver(e, 'selected')}
                onDragLeave={handleDragLeave}
                onDrop={handleDropOnSelectedColumns}
              >
                <h4 className="panel-title">Selected Columns</h4>
                {selectedColumns.length === 0 ? (
                  <div className="drop-zone-empty">
                    <span className="drop-icon">+</span>
                    <p>Drag columns here</p>
                  </div>
                ) : (
                  <div className="selected-columns-list">
                    {selectedColumns.map((col, index) => (
                      <div key={col.id} className="selected-column-item">
                        <div className="column-header">
                          <div className="column-move-buttons">
                            <button
                              className="move-btn"
                              onClick={() => moveColumn(col.id, 'up')}
                              disabled={index === 0}
                              title="Move up"
                            >
                              ▲
                            </button>
                            <button
                              className="move-btn"
                              onClick={() => moveColumn(col.id, 'down')}
                              disabled={index === selectedColumns.length - 1}
                              title="Move down"
                            >
                              ▼
                            </button>
                          </div>
                          <span className="column-source-badge">{col.column.sourceTable}</span>
                          <span className="column-original-name">{col.column.displayName}</span>
                          <button
                            className="edit-btn"
                            onClick={() => setEditingColumn(editingColumn === col.id ? null : col.id)}
                            title="Configure column"
                          >
                            {editingColumn === col.id ? '▼' : '▶'}
                          </button>
                          <button
                            className="remove-btn"
                            onClick={() => removeSelectedColumn(col.id)}
                            title="Remove column"
                          >
                            ×
                          </button>
                        </div>

                        {editingColumn === col.id && (
                          <div className="column-config">
                            <div className="config-row">
                              <label>Display Name:</label>
                              <input
                                type="text"
                                className="form-input"
                                value={col.alias}
                                onChange={e => updateColumnAlias(col.id, e.target.value)}
                              />
                            </div>

                            {(col.column.dataType === 'number' || col.column.dataType === 'decimal') && (
                              <div className="config-row">
                                <label>Aggregation:</label>
                                <select
                                  className="form-select"
                                  value={col.aggregation || ''}
                                  onChange={e => updateColumnAggregation(col.id, (e.target.value || null) as AggregationType | null)}
                                >
                                  <option value="">None</option>
                                  <option value="SUM">Sum</option>
                                  <option value="AVG">Average</option>
                                  <option value="COUNT">Count</option>
                                  <option value="MIN">Minimum</option>
                                  <option value="MAX">Maximum</option>
                                </select>
                              </div>
                            )}

                            {col.column.dataType === 'decimal' && (
                              <div className="config-row">
                                <label>Decimal Places:</label>
                                <input
                                  type="number"
                                  className="form-input"
                                  min="0"
                                  max="6"
                                  value={col.formatting.decimalPlaces ?? 2}
                                  onChange={e => updateColumnFormatting(col.id, { decimalPlaces: parseInt(e.target.value) })}
                                />
                              </div>
                            )}

                            {col.column.dataType === 'decimal' && (
                              <div className="config-row checkbox-row">
                                <label>
                                  <input
                                    type="checkbox"
                                    checked={col.formatting.isCurrency || false}
                                    onChange={e => updateColumnFormatting(col.id, { isCurrency: e.target.checked })}
                                  />
                                  Format as Currency
                                </label>
                              </div>
                            )}

                            {(col.column.dataType === 'date' || col.column.dataType === 'datetime') && (
                              <div className="config-row">
                                <label>Date Format:</label>
                                <select
                                  className="form-select"
                                  value={col.formatting.dateFormat || 'short'}
                                  onChange={e => updateColumnFormatting(col.id, { dateFormat: e.target.value })}
                                >
                                  <option value="short">Short (Jan 1, 2024)</option>
                                  <option value="long">Long (January 1, 2024)</option>
                                  <option value="iso">ISO (2024-01-01)</option>
                                </select>
                              </div>
                            )}
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        )

      case 'filters':
        return (
          <div className="step-content step-filters">
            <h3 className="step-title">Configure Filters</h3>
            <p className="step-description">
              Add filters to narrow down your report data. Combine multiple filters with AND/OR logic.
            </p>

            <div className="filters-container">
              {filters.length === 0 ? (
                <div className="no-filters">
                  <p>No filters configured. Click the button below to add a filter.</p>
                </div>
              ) : (
                <div className="filters-list">
                  {filters.map((filter, index) => (
                    <div key={filter.id} className="filter-row">
                      {index > 0 && (
                        <select
                          className="form-select logic-select"
                          value={filter.logic}
                          onChange={e => updateFilter(filter.id, { logic: e.target.value as 'AND' | 'OR' })}
                        >
                          <option value="AND">AND</option>
                          <option value="OR">OR</option>
                        </select>
                      )}

                      <select
                        className="form-select column-select"
                        value={`${filter.column.sourceTable}.${filter.column.name}`}
                        onChange={e => {
                          const [table, name] = e.target.value.split('.')
                          const column = availableColumns.find(c => c.sourceTable === table && c.name === name)
                          if (column) {
                            updateFilter(filter.id, { column, value: '' })
                          }
                        }}
                      >
                        {availableColumns.map(col => (
                          <option key={`${col.sourceTable}.${col.name}`} value={`${col.sourceTable}.${col.name}`}>
                            {col.sourceTable}.{col.displayName}
                          </option>
                        ))}
                      </select>

                      <select
                        className="form-select operator-select"
                        value={filter.operator}
                        onChange={e => updateFilter(filter.id, { operator: e.target.value as FilterOperator, value: '' })}
                      >
                        {FILTER_OPERATORS.map(op => (
                          <option key={op.value} value={op.value}>{op.label}</option>
                        ))}
                      </select>

                      <div className="filter-value">
                        {renderFilterValueInput(filter)}
                      </div>

                      <button
                        className="button-ghost remove-filter-btn"
                        onClick={() => removeFilter(filter.id)}
                        title="Remove filter"
                      >
                        ×
                      </button>
                    </div>
                  ))}
                </div>
              )}

              <button
                className="button-secondary add-filter-btn"
                onClick={addFilter}
              >
                + Add Filter
              </button>
            </div>
          </div>
        )

      case 'grouping':
        return (
          <div className="step-content step-grouping">
            <h3 className="step-title">Configure Grouping</h3>
            <p className="step-description">
              Drag columns here to group your data. Grouping is required when using aggregation functions.
            </p>

            <div className="grouping-layout">
              {/* Available Columns for Grouping */}
              <div className="columns-panel available-columns">
                <h4 className="panel-title">Available Columns</h4>
                <div className="columns-list">
                  {selectedColumns.filter(c => !c.aggregation).map(col => (
                    <div
                      key={col.id}
                      className="column-item"
                      draggable
                      onDragStart={e => handleDragStart(e, col.column)}
                      onDragEnd={handleDragEnd}
                    >
                      <span className="column-source">{col.column.sourceTable}</span>
                      <span className="column-name">{col.alias}</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Group By Panel */}
              <div
                className={`columns-panel group-by-panel ${dragOverTarget === 'groupby' ? 'drag-over' : ''}`}
                onDragOver={e => handleDragOver(e, 'groupby')}
                onDragLeave={handleDragLeave}
                onDrop={handleDropOnGroupBy}
              >
                <h4 className="panel-title">Group By</h4>
                {groupByColumns.length === 0 ? (
                  <div className="drop-zone-empty">
                    <span className="drop-icon">G</span>
                    <p>Drag columns here to group</p>
                  </div>
                ) : (
                  <div className="group-by-list">
                    {groupByColumns.map(group => (
                      <div key={group.id} className="group-by-item">
                        <span className="column-source-badge">{group.column.sourceTable}</span>
                        <span className="column-name">{group.column.displayName}</span>
                        <button
                          className="remove-btn"
                          onClick={() => removeGroupBy(group.id)}
                          title="Remove grouping"
                        >
                          ×
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>

            {selectedColumns.some(c => c.aggregation) && groupByColumns.length === 0 && (
              <div className="alert alert-warning">
                You have aggregation functions selected. You should add non-aggregated columns to Group By.
              </div>
            )}
          </div>
        )

      case 'sorting':
        return (
          <div className="step-content step-sorting">
            <h3 className="step-title">Configure Sorting</h3>
            <p className="step-description">
              Drag columns here to define the sort order of your report results.
            </p>

            <div className="sorting-layout">
              {/* Available Columns for Sorting */}
              <div className="columns-panel available-columns">
                <h4 className="panel-title">Available Columns</h4>
                <div className="columns-list">
                  {selectedColumns.map(col => (
                    <div
                      key={col.id}
                      className="column-item"
                      draggable
                      onDragStart={e => handleDragStart(e, col.column)}
                      onDragEnd={handleDragEnd}
                    >
                      <span className="column-source">{col.column.sourceTable}</span>
                      <span className="column-name">{col.alias}</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Sort By Panel */}
              <div
                className={`columns-panel sort-by-panel ${dragOverTarget === 'sortby' ? 'drag-over' : ''}`}
                onDragOver={e => handleDragOver(e, 'sortby')}
                onDragLeave={handleDragLeave}
                onDrop={handleDropOnSortBy}
              >
                <h4 className="panel-title">Sort By</h4>
                {sortByColumns.length === 0 ? (
                  <div className="drop-zone-empty">
                    <span className="drop-icon">↕</span>
                    <p>Drag columns here to sort</p>
                  </div>
                ) : (
                  <div className="sort-by-list">
                    {sortByColumns.map(sort => (
                      <div key={sort.id} className="sort-by-item">
                        <span className="column-source-badge">{sort.column.sourceTable}</span>
                        <span className="column-name">{sort.column.displayName}</span>
                        <button
                          className={`direction-btn ${sort.direction === 'ASC' ? 'asc' : 'desc'}`}
                          onClick={() => toggleSortDirection(sort.id)}
                          title={`Sort ${sort.direction === 'ASC' ? 'ascending' : 'descending'}`}
                        >
                          {sort.direction === 'ASC' ? '↑ ASC' : '↓ DESC'}
                        </button>
                        <button
                          className="remove-btn"
                          onClick={() => removeSortBy(sort.id)}
                          title="Remove sorting"
                        >
                          ×
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        )

      case 'preview':
        return (
          <div className="step-content step-preview">
            <h3 className="step-title">Preview Report</h3>
            <p className="step-description">
              Preview your report results before saving. Limited to 100 rows.
            </p>

            <div className="preview-actions">
              <button
                className="button-primary"
                onClick={executePreview}
                disabled={isPreviewLoading}
              >
                {isPreviewLoading ? 'Loading...' : 'Generate Preview'}
              </button>
            </div>

            {previewError && (
              <div className="alert alert-error">
                {previewError}
              </div>
            )}

            {previewResult && (
              <div className="preview-results">
                <div className="preview-meta">
                  <span>{previewResult.totalRows.toLocaleString()} row{previewResult.totalRows !== 1 ? 's' : ''}</span>
                  <span>{previewResult.executionTimeMs}ms</span>
                </div>

                {previewResult.rows.length === 0 ? (
                  <div className="empty-state">
                    <p>No results found. Try adjusting your filters.</p>
                  </div>
                ) : (
                  <div className="table-container">
                    <table className="table preview-table">
                      <thead>
                        <tr>
                          {previewResult.columns.map(col => (
                            <th key={col.name}>{col.displayName}</th>
                          ))}
                        </tr>
                      </thead>
                      <tbody>
                        {previewResult.rows.map((row, idx) => (
                          <tr key={idx}>
                            {previewResult.columns.map(col => (
                              <td key={col.name}>
                                {formatCellValue(row[col.name], col.dataType)}
                              </td>
                            ))}
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            )}
          </div>
        )

      case 'save':
        return (
          <div className="step-content step-save">
            <h3 className="step-title">Save Custom Report</h3>
            <p className="step-description">
              Enter a name and description for your custom report template.
            </p>

            {saveSuccess ? (
              <div className="alert alert-success">
                Report saved successfully! Redirecting...
              </div>
            ) : (
              <div className="save-form">
                <div className="form-field">
                  <label htmlFor="report-name" className="form-label">
                    Report Name <span className="required">*</span>
                  </label>
                  <input
                    id="report-name"
                    type="text"
                    className="form-input"
                    value={reportName}
                    onChange={e => setReportName(e.target.value)}
                    placeholder="Enter report name"
                  />
                </div>

                <div className="form-field">
                  <label htmlFor="report-description" className="form-label">
                    Description
                  </label>
                  <textarea
                    id="report-description"
                    className="form-textarea"
                    value={reportDescription}
                    onChange={e => setReportDescription(e.target.value)}
                    placeholder="Enter report description"
                    rows={3}
                  />
                </div>

                <div className="form-field">
                  <label htmlFor="report-category" className="form-label">
                    Category
                  </label>
                  <select
                    id="report-category"
                    className="form-select"
                    value={reportCategory}
                    onChange={e => setReportCategory(e.target.value)}
                  >
                    {REPORT_CATEGORIES.map(cat => (
                      <option key={cat} value={cat}>{cat}</option>
                    ))}
                  </select>
                </div>

                {saveError && (
                  <div className="alert alert-error">
                    {saveError}
                  </div>
                )}

                <div className="save-summary">
                  <h4>Report Summary</h4>
                  <ul>
                    <li><strong>Data Sources:</strong> {Array.from(selectedSources).join(', ')}</li>
                    <li><strong>Columns:</strong> {selectedColumns.length}</li>
                    <li><strong>Filters:</strong> {filters.length}</li>
                    <li><strong>Group By:</strong> {groupByColumns.length} columns</li>
                    <li><strong>Sort By:</strong> {sortByColumns.length} columns</li>
                  </ul>
                </div>

                <button
                  className="button-primary save-btn"
                  onClick={saveCustomReport}
                  disabled={isSaving || !reportName.trim()}
                >
                  {isSaving ? 'Saving...' : 'Save Custom Report'}
                </button>
              </div>
            )}
          </div>
        )

      default:
        return null
    }
  }

  // ============================================================================
  // MAIN RENDER
  // ============================================================================

  return (
    <div className="custom-report-builder-page">
      {/* Page Header */}
      <section className="content-section" aria-labelledby="builder-title">
        <div className="section-header">
          <div>
            <h1 id="builder-title" className="section-title">Custom Report Builder</h1>
            <p className="section-subtitle">
              Create custom reports by selecting data sources, columns, and configuring filters
            </p>
          </div>
        </div>

        {/* Wizard Progress */}
        <div className="wizard-progress" role="navigation" aria-label="Report builder steps">
          {WIZARD_STEPS.map((step, index) => (
            <div
              key={step.id}
              className={`wizard-step ${currentStep === step.id ? 'active' : ''} ${canProceedToStep(step.id) ? 'completed' : 'disabled'}`}
              onClick={() => goToStep(step.id)}
              role="button"
              tabIndex={canProceedToStep(step.id) ? 0 : -1}
              aria-current={currentStep === step.id ? 'step' : undefined}
            >
              <span className="step-number">{step.number}</span>
              <span className="step-label">{step.title}</span>
              {index < WIZARD_STEPS.length - 1 && <span className="step-connector" />}
            </div>
          ))}
        </div>

        {/* Step Content */}
        <div className="wizard-content">
          {renderStepContent()}
        </div>

        {/* Navigation Buttons */}
        <div className="wizard-navigation">
          <button
            className="button-secondary"
            onClick={goToPreviousStep}
            disabled={currentStep === 'sources'}
          >
            Previous
          </button>

          <div className="nav-spacer" />

          {currentStep !== 'save' && (
            <button
              className="button-primary"
              onClick={goToNextStep}
              disabled={!canProceedToStep(WIZARD_STEPS[WIZARD_STEPS.findIndex(s => s.id === currentStep) + 1]?.id)}
            >
              {currentStep === 'preview' ? 'Continue to Save' : 'Next'}
            </button>
          )}
        </div>
      </section>
    </div>
  )
}

export default CustomReportBuilderPage
