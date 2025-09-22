import { useEffect, useState } from 'react'
import { useAppSelector } from '../hooks'
import './CreateOrderModal.css'

type Props = {
  isOpen: boolean
  onClose: () => void
  orderName?: string
}

const LabelModal = ({ isOpen, onClose, orderName }: Props) => {
  const user = useAppSelector(state => state.users.items[0])
  const [brandName, setBrandName] = useState('')
  const [productName, setProductName] = useState('')
  const [alcoholContent, setAlcoholContent] = useState('')

  useEffect(() => {
    if (isOpen) {
      setProductName(orderName ?? '')
    }
  }, [isOpen, orderName])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!user) return
    const response = await fetch('/api/labels/ttb-form', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        companyId: user.companyId,
        brandName,
        productName,
        alcoholContent
      })
    })
    const blob = await response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'ttb_form_5100_31.pdf'
    a.click()
    window.URL.revokeObjectURL(url)
    onClose()
    setBrandName('')
    setProductName('')
    setAlcoholContent('')
  }

  if (!isOpen) return null

  return (
    <div className='modal-overlay'>
      <div className='modal'>
        <h2>Create Label</h2>
        <form onSubmit={handleSubmit}>
          <input value={brandName} onChange={e => setBrandName(e.target.value)} placeholder='Brand Name' />
          <input value={productName} onChange={e => setProductName(e.target.value)} placeholder='Product Name' />
          <input value={alcoholContent} onChange={e => setAlcoholContent(e.target.value)} placeholder='Alcohol Content' />
          <button type='submit'>Generate</button>
          <button type='button' onClick={onClose}>Cancel</button>
        </form>
      </div>
    </div>
  )
}

export default LabelModal
