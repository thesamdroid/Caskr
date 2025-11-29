import { FormEvent, useEffect, useState } from 'react'
import { useAppDispatch } from '../hooks'
import {
  createTtbTransaction,
  updateTtbTransaction,
  TtbTransaction,
  TtbTransactionType,
  TtbSpiritsType
} from '../features/ttbTransactionsSlice'

interface TtbTransactionModalProps {
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
  transaction: TtbTransaction | null
  companyId: number
  defaultMonth: number
  defaultYear: number
}

const transactionTypeOptions = [
  { value: TtbTransactionType.Production, label: 'Production' },
  { value: TtbTransactionType.TransferIn, label: 'Transfer In' },
  { value: TtbTransactionType.TransferOut, label: 'Transfer Out' },
  { value: TtbTransactionType.Loss, label: 'Loss' },
  { value: TtbTransactionType.Gain, label: 'Gain' },
  { value: TtbTransactionType.TaxDetermination, label: 'Tax Determination' },
  { value: TtbTransactionType.Destruction, label: 'Destruction' },
  { value: TtbTransactionType.Bottling, label: 'Bottling' }
]

const spiritsTypeOptions = [
  { value: TtbSpiritsType.Under190Proof, label: 'Under 190 Proof' },
  { value: TtbSpiritsType.Neutral190OrMore, label: 'Neutral (190 Proof or More)' },
  { value: TtbSpiritsType.Alcohol, label: 'Alcohol' },
  { value: TtbSpiritsType.Wine, label: 'Wine' }
]

export default function TtbTransactionModal({
  isOpen,
  onClose,
  onSuccess,
  transaction,
  companyId,
  defaultMonth,
  defaultYear
}: TtbTransactionModalProps) {
  const dispatch = useAppDispatch()

  const [transactionDate, setTransactionDate] = useState<string>('')
  const [transactionType, setTransactionType] = useState<TtbTransactionType>(TtbTransactionType.Production)
  const [productType, setProductType] = useState<string>('')
  const [spiritsType, setSpiritsType] = useState<TtbSpiritsType>(TtbSpiritsType.Under190Proof)
  const [proofGallons, setProofGallons] = useState<string>('')
  const [wineGallons, setWineGallons] = useState<string>('')
  const [notes, setNotes] = useState<string>('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const isEditMode = transaction !== null

  useEffect(() => {
    if (isOpen) {
      if (transaction) {
        // Edit mode - populate with transaction data
        const date = new Date(transaction.transactionDate)
        const year = date.getFullYear()
        const month = (date.getMonth() + 1).toString().padStart(2, '0')
        const day = date.getDate().toString().padStart(2, '0')
        setTransactionDate(`${year}-${month}-${day}`)
        setTransactionType(transaction.transactionType)
        setProductType(transaction.productType)
        setSpiritsType(transaction.spiritsType)
        setProofGallons(transaction.proofGallons.toString())
        setWineGallons(transaction.wineGallons.toString())
        setNotes(transaction.notes || '')
      } else {
        // Create mode - set defaults
        const date = new Date(defaultYear, defaultMonth - 1, 1)
        const year = date.getFullYear()
        const month = (date.getMonth() + 1).toString().padStart(2, '0')
        const day = '01'
        setTransactionDate(`${year}-${month}-${day}`)
        setTransactionType(TtbTransactionType.Production)
        setProductType('')
        setSpiritsType(TtbSpiritsType.Under190Proof)
        setProofGallons('')
        setWineGallons('')
        setNotes('')
      }
      setError(null)
      setIsSubmitting(false)
    }
  }, [isOpen, transaction, defaultMonth, defaultYear])

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)

    // Validation
    if (!transactionDate) {
      setError('Transaction date is required.')
      return
    }

    if (!productType.trim()) {
      setError('Product type is required.')
      return
    }

    const proofGallonsNum = parseFloat(proofGallons)
    const wineGallonsNum = parseFloat(wineGallons)

    if (isNaN(proofGallonsNum) || proofGallonsNum < 0) {
      setError('Proof gallons must be a valid non-negative number.')
      return
    }

    if (isNaN(wineGallonsNum) || wineGallonsNum < 0) {
      setError('Wine gallons must be a valid non-negative number.')
      return
    }

    setIsSubmitting(true)

    try {
      if (isEditMode && transaction) {
        // Update existing transaction
        await dispatch(
          updateTtbTransaction({
            id: transaction.id,
            request: {
              transactionDate,
              transactionType,
              productType: productType.trim(),
              spiritsType,
              proofGallons: proofGallonsNum,
              wineGallons: wineGallonsNum,
              notes: notes.trim() || undefined
            }
          })
        ).unwrap()
      } else {
        // Create new transaction
        await dispatch(
          createTtbTransaction({
            companyId,
            transactionDate,
            transactionType,
            productType: productType.trim(),
            spiritsType,
            proofGallons: proofGallonsNum,
            wineGallons: wineGallonsNum,
            notes: notes.trim() || undefined
          })
        ).unwrap()
      }

      onSuccess()
    } catch (err) {
      console.error('[TtbTransactionModal] Failed to save transaction', err)
      setError(isEditMode ? 'Failed to update transaction. Please try again.' : 'Failed to create transaction. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  if (!isOpen) return null

  return (
    <div className='modal' role='dialog' aria-modal='true' aria-labelledby='transaction-modal-heading'>
      <form className='modal-content' onSubmit={handleSubmit}>
        <div className='modal-header'>
          <h3 id='transaction-modal-heading'>
            {isEditMode ? 'Edit Transaction' : 'Add Transaction'}
          </h3>
          <p className='modal-subtitle'>
            {isEditMode ? 'Update the transaction details below.' : 'Enter transaction details for manual entry.'}
          </p>
        </div>

        <div className='modal-grid'>
          <label className='form-label' htmlFor='transaction-date'>
            Transaction date
            <input
              type='date'
              id='transaction-date'
              value={transactionDate}
              onChange={e => setTransactionDate(e.target.value)}
              required
              aria-required='true'
            />
          </label>

          <label className='form-label' htmlFor='transaction-type'>
            Transaction type
            <select
              id='transaction-type'
              value={transactionType}
              onChange={e => setTransactionType(Number(e.target.value) as TtbTransactionType)}
              required
              aria-required='true'
            >
              {transactionTypeOptions.map(option => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>

          <label className='form-label' htmlFor='product-type'>
            Product type
            <input
              type='text'
              id='product-type'
              value={productType}
              onChange={e => setProductType(e.target.value)}
              placeholder='e.g., Bourbon, Vodka, Rum'
              required
              aria-required='true'
              maxLength={100}
            />
          </label>

          <label className='form-label' htmlFor='spirits-type'>
            Spirits type
            <select
              id='spirits-type'
              value={spiritsType}
              onChange={e => setSpiritsType(Number(e.target.value) as TtbSpiritsType)}
              required
              aria-required='true'
            >
              {spiritsTypeOptions.map(option => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>

          <label className='form-label' htmlFor='proof-gallons'>
            Proof gallons
            <input
              type='number'
              id='proof-gallons'
              value={proofGallons}
              onChange={e => setProofGallons(e.target.value)}
              step='0.01'
              min='0'
              placeholder='0.00'
              required
              aria-required='true'
            />
          </label>

          <label className='form-label' htmlFor='wine-gallons'>
            Wine gallons
            <input
              type='number'
              id='wine-gallons'
              value={wineGallons}
              onChange={e => setWineGallons(e.target.value)}
              step='0.01'
              min='0'
              placeholder='0.00'
              required
              aria-required='true'
            />
          </label>

          <label className='form-label form-label-full' htmlFor='notes'>
            Notes (optional)
            <textarea
              id='notes'
              value={notes}
              onChange={e => setNotes(e.target.value)}
              placeholder='Additional notes about this transaction'
              rows={3}
              maxLength={500}
            />
          </label>
        </div>

        {error && (
          <div className='alert alert-error' role='alert' aria-live='assertive'>
            {error}
          </div>
        )}

        <div className='modal-actions'>
          <button type='submit' className='button-primary' disabled={isSubmitting}>
            {isSubmitting ? 'Saving...' : isEditMode ? 'Update Transaction' : 'Add Transaction'}
          </button>
          <button type='button' onClick={onClose} className='button-secondary' disabled={isSubmitting}>
            Cancel
          </button>
        </div>
      </form>
    </div>
  )
}
