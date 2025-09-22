import { FormEvent, useEffect, useState } from 'react'

interface BarrelImportModalProps {
  isOpen: boolean
  requireMashBillId: boolean
  error: string | null
  onClose: () => void
  onSubmit: (payload: { file: File; batchId?: number; mashBillId?: number }) => Promise<void>
}

export default function BarrelImportModal({ isOpen, requireMashBillId, error, onClose, onSubmit }: BarrelImportModalProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [batchId, setBatchId] = useState('')
  const [mashBillId, setMashBillId] = useState('')
  const [localError, setLocalError] = useState<string | null>(null)

  useEffect(() => {
    if (isOpen) {
      setSelectedFile(null)
      setBatchId('')
      setMashBillId('')
      setLocalError(null)
    }
  }, [isOpen])

  if (!isOpen) return null

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!selectedFile) {
      setLocalError('Please choose a CSV file to upload.')
      return
    }

    if (requireMashBillId && mashBillId.trim() === '') {
      setLocalError('Mash bill ID is required to create a new batch.')
      return
    }

    const batchValue = batchId.trim() === '' ? undefined : parseInt(batchId, 10)
    if (batchValue !== undefined && Number.isNaN(batchValue)) {
      setLocalError('Batch ID must be a number.')
      return
    }

    const mashBillValue = mashBillId.trim() === '' ? undefined : parseInt(mashBillId, 10)
    if (mashBillValue !== undefined && Number.isNaN(mashBillValue)) {
      setLocalError('Mash bill ID must be a number.')
      return
    }

    setLocalError(null)

    await onSubmit({ file: selectedFile, batchId: batchValue, mashBillId: mashBillValue })
  }

  return (
    <div className='modal'>
      <form onSubmit={handleSubmit} className='modal-content'>
        <h3>Import Barrels</h3>
        <label>
          CSV File
          <input
            type='file'
            accept='.csv'
            onChange={event => setSelectedFile(event.target.files?.[0] ?? null)}
          />
        </label>
        <label>
          Batch ID
          <input
            type='number'
            min={1}
            placeholder='Optional'
            value={batchId}
            onChange={event => setBatchId(event.target.value)}
          />
        </label>
        {(requireMashBillId || mashBillId) && (
          <label>
            Mash Bill ID
            <input
              type='number'
              min={1}
              value={mashBillId}
              onChange={event => setMashBillId(event.target.value)}
            />
          </label>
        )}
        {(localError || error) && <p className='modal-error'>{localError ?? error}</p>}
        <div className='modal-actions'>
          <button type='submit'>Submit</button>
          <button type='button' onClick={onClose}>
            Cancel
          </button>
        </div>
      </form>
    </div>
  )
}
