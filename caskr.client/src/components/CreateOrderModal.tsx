import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import { addOrder } from '../features/ordersSlice'
import { fetchSpiritTypes } from '../features/spiritTypesSlice'
import { fetchMashBills } from '../features/mashBillsSlice'
import './CreateOrderModal.css'

type Props = {
  isOpen: boolean
  onClose: () => void
}

const CreateOrderModal = ({ isOpen, onClose }: Props) => {
  const dispatch = useAppDispatch()
  const spiritTypes = useAppSelector(state => state.spiritTypes.items)
  const mashBills = useAppSelector(state => state.mashBills.items)
  const statuses = useAppSelector(state => state.statuses.items)
  const user = useAppSelector(state => state.users.items[0])

  const [name, setName] = useState('')
  const [quantity, setQuantity] = useState(1)
  const [spiritTypeId, setSpiritTypeId] = useState<number>(0)
  const [mashBillId, setMashBillId] = useState<number>(0)
  const [statusId, setStatusId] = useState<number>(0)

  useEffect(() => {
    if (isOpen) {
      dispatch(fetchSpiritTypes())
      dispatch(fetchMashBills())
    }
  }, [dispatch, isOpen])

  useEffect(() => {
    if (spiritTypes.length > 0 && spiritTypeId === 0) {
      setSpiritTypeId(spiritTypes[0].id)
    }
  }, [spiritTypes, spiritTypeId])

  useEffect(() => {
    if (mashBills.length > 0 && mashBillId === 0) {
      setMashBillId(mashBills[0].id)
    }
  }, [mashBills, mashBillId])

  useEffect(() => {
    if (statuses.length > 0 && statusId === 0) {
      setStatusId(statuses[0].id)
    }
  }, [statuses, statusId])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!user) return
    dispatch(addOrder({
      name,
      statusId,
      ownerId: user.id,
      spiritTypeId,
      quantity,
      mashBillId
    }))
    onClose()
    setName('')
    setQuantity(1)
  }

  if (!isOpen) return null

  return (
    <div className='modal-overlay'>
      <div className='modal'>
        <h2>Create Order for {user?.companyName}</h2>
        <form onSubmit={handleSubmit}>
          <input value={name} onChange={e => setName(e.target.value)} placeholder='Order name' />
          <input type='number' min={1} value={quantity} onChange={e => setQuantity(Number(e.target.value))} />
          <select value={spiritTypeId} onChange={e => setSpiritTypeId(Number(e.target.value))}>
            {spiritTypes.map(st => (
              <option key={st.id} value={st.id}>
                {st.name}
              </option>
            ))}
          </select>
          <select value={mashBillId} onChange={e => setMashBillId(Number(e.target.value))}>
            {mashBills.map(mb => (
              <option key={mb.id} value={mb.id}>
                {mb.name}
              </option>
            ))}
          </select>
          <select value={statusId} onChange={e => setStatusId(Number(e.target.value))}>
            {statuses.map(s => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
          <button type='submit'>Save</button>
          <button type='button' onClick={onClose}>Cancel</button>
        </form>
      </div>
    </div>
  )
}

export default CreateOrderModal
