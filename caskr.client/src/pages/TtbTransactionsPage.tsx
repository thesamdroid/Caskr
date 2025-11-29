import { useEffect, useMemo, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchTtbTransactions,
  setSelectedMonth,
  setSelectedYear,
  TtbTransaction,
  TtbTransactionType,
  TtbSpiritsType
} from '../features/ttbTransactionsSlice'
import TtbTransactionModal from '../components/TtbTransactionModal'
import { deleteTtbTransaction } from '../features/ttbTransactionsSlice'

const transactionTypeLabels: Record<TtbTransactionType, string> = {
  [TtbTransactionType.Production]: 'Production',
  [TtbTransactionType.TransferIn]: 'Transfer In',
  [TtbTransactionType.TransferOut]: 'Transfer Out',
  [TtbTransactionType.Loss]: 'Loss',
  [TtbTransactionType.Gain]: 'Gain',
  [TtbTransactionType.TaxDetermination]: 'Tax Determination',
  [TtbTransactionType.Destruction]: 'Destruction',
  [TtbTransactionType.Bottling]: 'Bottling'
}

const spiritsTypeLabels: Record<TtbSpiritsType, string> = {
  [TtbSpiritsType.Under190Proof]: 'Under 190 Proof',
  [TtbSpiritsType.Neutral190OrMore]: 'Neutral (190+)',
  [TtbSpiritsType.Alcohol]: 'Alcohol',
  [TtbSpiritsType.Wine]: 'Wine'
}

const formatDate = (dateString: string) => {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  }).format(new Date(dateString))
}

const getSourceDisplay = (transaction: TtbTransaction) => {
  if (transaction.sourceEntityType === 'Manual') {
    return 'Manual Entry'
  }
  if (transaction.sourceEntityType && transaction.sourceEntityId) {
    return `${transaction.sourceEntityType} #${transaction.sourceEntityId}`
  }
  return 'Auto-generated'
}

function TtbTransactionsPage() {
  const dispatch = useAppDispatch()
  const transactions = useAppSelector(state => state.ttbTransactions.items)
  const isLoading = useAppSelector(state => state.ttbTransactions.isLoading)
  const error = useAppSelector(state => state.ttbTransactions.error)
  const selectedMonth = useAppSelector(state => state.ttbTransactions.selectedMonth)
  const selectedYear = useAppSelector(state => state.ttbTransactions.selectedYear)
  const authUser = useAppSelector(state => state.auth.user)

  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingTransaction, setEditingTransaction] = useState<TtbTransaction | null>(null)

  const companyId = authUser?.companyId ?? 1

  useEffect(() => {
    dispatch(fetchTtbTransactions({ companyId, month: selectedMonth, year: selectedYear }))
  }, [dispatch, companyId, selectedMonth, selectedYear])

  const monthOptions = useMemo(() => {
    return Array.from({ length: 12 }, (_, i) => ({
      value: i + 1,
      label: new Date(2000, i, 1).toLocaleString('en-US', { month: 'long' })
    }))
  }, [])

  const yearOptions = useMemo(() => {
    const currentYear = new Date().getFullYear()
    return Array.from({ length: 10 }, (_, i) => currentYear - i)
  }, [])

  const handleAddTransaction = () => {
    setEditingTransaction(null)
    setIsModalOpen(true)
  }

  const handleEditTransaction = (transaction: TtbTransaction) => {
    if (transaction.sourceEntityType !== 'Manual') {
      alert('Only manual transactions can be edited.')
      return
    }
    setEditingTransaction(transaction)
    setIsModalOpen(true)
  }

  const handleDeleteTransaction = async (id: number) => {
    if (window.confirm('Are you sure you want to delete this transaction? This action cannot be undone.')) {
      try {
        await dispatch(deleteTtbTransaction(id)).unwrap()
      } catch (err) {
        console.error('Failed to delete transaction:', err)
        alert('Failed to delete transaction. Please try again.')
      }
    }
  }

  const handleModalClose = () => {
    setIsModalOpen(false)
    setEditingTransaction(null)
  }

  const handleModalSuccess = () => {
    setIsModalOpen(false)
    setEditingTransaction(null)
    dispatch(fetchTtbTransactions({ companyId, month: selectedMonth, year: selectedYear }))
  }

  const summary = useMemo(() => {
    const totals = {
      production: 0,
      transferIn: 0,
      transferOut: 0,
      losses: 0,
      gains: 0,
      taxDetermination: 0,
      destruction: 0,
      bottling: 0
    }

    transactions.forEach(t => {
      switch (t.transactionType) {
        case TtbTransactionType.Production:
          totals.production += t.proofGallons
          break
        case TtbTransactionType.TransferIn:
          totals.transferIn += t.proofGallons
          break
        case TtbTransactionType.TransferOut:
          totals.transferOut += t.proofGallons
          break
        case TtbTransactionType.Loss:
          totals.losses += t.proofGallons
          break
        case TtbTransactionType.Gain:
          totals.gains += t.proofGallons
          break
        case TtbTransactionType.TaxDetermination:
          totals.taxDetermination += t.proofGallons
          break
        case TtbTransactionType.Destruction:
          totals.destruction += t.proofGallons
          break
        case TtbTransactionType.Bottling:
          totals.bottling += t.proofGallons
          break
      }
    })

    return totals
  }, [transactions])

  const exportToCsv = () => {
    const headers = [
      'Date',
      'Type',
      'Product',
      'Spirits Type',
      'Proof Gallons',
      'Wine Gallons',
      'Source',
      'Notes'
    ]

    const rows = transactions.map(t => [
      formatDate(t.transactionDate),
      transactionTypeLabels[t.transactionType],
      t.productType,
      spiritsTypeLabels[t.spiritsType],
      t.proofGallons.toFixed(2),
      t.wineGallons.toFixed(2),
      getSourceDisplay(t),
      t.notes || ''
    ])

    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.map(cell => `"${cell}"`).join(','))
    ].join('\n')

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
    const link = document.createElement('a')
    const url = URL.createObjectURL(blob)
    link.setAttribute('href', url)
    link.setAttribute('download', `ttb_transactions_${selectedYear}_${selectedMonth.toString().padStart(2, '0')}.csv`)
    link.style.visibility = 'hidden'
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
  }

  return (
    <div className='page-container'>
      <div className='page-header'>
        <h1>TTB Transactions</h1>
        <p className='page-subtitle'>
          Manually add, edit, or delete TTB transactions for corrections and non-automated events
        </p>
      </div>

      {error && (
        <div className='alert alert-error' role='alert'>
          {error}
        </div>
      )}

      <div className='controls-bar'>
        <div className='filters'>
          <div className='form-group'>
            <label htmlFor='month-select'>Month</label>
            <select
              id='month-select'
              value={selectedMonth}
              onChange={e => dispatch(setSelectedMonth(Number(e.target.value)))}
              className='form-control'
            >
              {monthOptions.map(({ value, label }) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </div>

          <div className='form-group'>
            <label htmlFor='year-select'>Year</label>
            <select
              id='year-select'
              value={selectedYear}
              onChange={e => dispatch(setSelectedYear(Number(e.target.value)))}
              className='form-control'
            >
              {yearOptions.map(year => (
                <option key={year} value={year}>
                  {year}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className='actions'>
          <button
            onClick={exportToCsv}
            className='button-secondary'
            disabled={transactions.length === 0}
          >
            Export to CSV
          </button>
          <button
            onClick={handleAddTransaction}
            className='button-primary'
            data-testid='add-transaction-button'
          >
            Add Transaction
          </button>
        </div>
      </div>

      {isLoading ? (
        <div className='loading-message'>Loading transactions...</div>
      ) : transactions.length === 0 ? (
        <div className='empty-state'>
          <p>No transactions found for {monthOptions[selectedMonth - 1].label} {selectedYear}</p>
          <p>Click "Add Transaction" to create a manual entry.</p>
        </div>
      ) : (
        <>
          <div className='table-container'>
            <table className='data-table'>
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Type</th>
                  <th>Product</th>
                  <th>Spirits Type</th>
                  <th>Proof Gallons</th>
                  <th>Wine Gallons</th>
                  <th>Source</th>
                  <th>Notes</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {transactions.map(transaction => (
                  <tr key={transaction.id}>
                    <td>{formatDate(transaction.transactionDate)}</td>
                    <td>{transactionTypeLabels[transaction.transactionType]}</td>
                    <td>{transaction.productType}</td>
                    <td>{spiritsTypeLabels[transaction.spiritsType]}</td>
                    <td className='text-right'>{transaction.proofGallons.toFixed(2)}</td>
                    <td className='text-right'>{transaction.wineGallons.toFixed(2)}</td>
                    <td>{getSourceDisplay(transaction)}</td>
                    <td>{transaction.notes || '—'}</td>
                    <td className='actions-cell'>
                      {transaction.sourceEntityType === 'Manual' ? (
                        <>
                          <button
                            onClick={() => handleEditTransaction(transaction)}
                            className='button-link'
                            title='Edit transaction'
                          >
                            Edit
                          </button>
                          <button
                            onClick={() => handleDeleteTransaction(transaction.id)}
                            className='button-link button-link-danger'
                            title='Delete transaction'
                          >
                            Delete
                          </button>
                        </>
                      ) : (
                        <span className='text-muted'>Auto-generated</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className='summary-footer'>
            <h3>Monthly Summary (Proof Gallons)</h3>
            <div className='summary-grid'>
              <div className='summary-item'>
                <span className='summary-label'>Total Production:</span>
                <span className='summary-value'>{summary.production.toFixed(2)}</span>
              </div>
              <div className='summary-item'>
                <span className='summary-label'>Total Transfers In:</span>
                <span className='summary-value'>{summary.transferIn.toFixed(2)}</span>
              </div>
              <div className='summary-item'>
                <span className='summary-label'>Total Transfers Out:</span>
                <span className='summary-value'>{summary.transferOut.toFixed(2)}</span>
              </div>
              <div className='summary-item'>
                <span className='summary-label'>Total Losses:</span>
                <span className='summary-value'>{summary.losses.toFixed(2)}</span>
              </div>
              <div className='summary-item'>
                <span className='summary-label'>Total Gains:</span>
                <span className='summary-value'>{summary.gains.toFixed(2)}</span>
              </div>
              <div className='summary-item'>
                <span className='summary-label'>Total Tax Determination:</span>
                <span className='summary-value'>{summary.taxDetermination.toFixed(2)}</span>
              </div>
              <div className='summary-item'>
                <span className='summary-label'>Total Destruction:</span>
                <span className='summary-value'>{summary.destruction.toFixed(2)}</span>
              </div>
              <div className='summary-item'>
                <span className='summary-label'>Total Bottling:</span>
                <span className='summary-value'>{summary.bottling.toFixed(2)}</span>
              </div>
            </div>
          </div>
        </>
      )}

      <TtbTransactionModal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        onSuccess={handleModalSuccess}
        transaction={editingTransaction}
        companyId={companyId}
        defaultMonth={selectedMonth}
        defaultYear={selectedYear}
      />
    </div>
  )
}

export default TtbTransactionsPage
