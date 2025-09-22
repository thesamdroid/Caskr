import { useState } from 'react'
import { useAppSelector } from '../hooks'
import { authorizedFetch } from '../api/authorizedFetch'
import { downloadBlob } from '../utils/downloadBlob'
import './CreateOrderModal.css'

type Props = {
  isOpen: boolean
  onClose: () => void
}

const TransferModal = ({ isOpen, onClose }: Props) => {
  const user = useAppSelector(state => state.users.items[0])
  const [toCompanyName, setToCompanyName] = useState('')
  const [permitNumber, setPermitNumber] = useState('')
  const [address, setAddress] = useState('')
  const [barrelCount, setBarrelCount] = useState(1)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!user) return
    const response = await authorizedFetch('/api/transfers/ttb-form', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        fromCompanyId: user.companyId,
        toCompanyName,
        permitNumber,
        address,
        barrelCount
      })
    })
    if (!response.ok) {
      throw new Error('Failed to generate transfer document')
    }
    const blob = await response.blob()
    downloadBlob(blob, 'ttb_form_5100_16.pdf')
    onClose()
    setToCompanyName('')
    setPermitNumber('')
    setAddress('')
    setBarrelCount(1)
  }

  if (!isOpen) return null

  return (
    <div className='modal-overlay'>
      <div className='modal'>
        <h2>Transfer Stock</h2>
        <form onSubmit={handleSubmit}>
          <input value={toCompanyName} onChange={e => setToCompanyName(e.target.value)} placeholder='Destination Company' />
          <input value={permitNumber} onChange={e => setPermitNumber(e.target.value)} placeholder='Permit Number' />
          <input value={address} onChange={e => setAddress(e.target.value)} placeholder='Destination Address' />
          <input type='number' min={1} value={barrelCount} onChange={e => setBarrelCount(Number(e.target.value))} />
          <button type='submit'>Generate</button>
          <button type='button' onClick={onClose}>Cancel</button>
        </form>
      </div>
    </div>
  )
}

export default TransferModal
