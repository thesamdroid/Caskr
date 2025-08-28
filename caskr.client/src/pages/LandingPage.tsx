import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import { fetchOrders, Order, fetchOutstandingTasks } from '../features/ordersSlice'
import { fetchStatuses } from '../features/statusSlice'
import { fetchUsers } from '../features/usersSlice'
import CreateOrderModal from '../components/CreateOrderModal'
import TransferModal from '../components/TransferModal'
import './LandingPage.css'

function LandingPage() {
  const dispatch = useAppDispatch()
  const orders = useAppSelector(state => state.orders.items)
  const statuses = useAppSelector(state => state.statuses.items)
  const outstandingTasks = useAppSelector(state => state.orders.outstandingTasks)

  const [isModalOpen, setIsModalOpen] = useState(false)
  const [isTransferOpen, setIsTransferOpen] = useState(false)

  useEffect(() => {
    dispatch(fetchOrders())
    dispatch(fetchStatuses())
    dispatch(fetchUsers())
  }, [dispatch])

  useEffect(() => {
    orders.forEach(o => {
      if (!outstandingTasks[o.id]) {
        dispatch(fetchOutstandingTasks(o.id))
      }
    })
  }, [orders, outstandingTasks, dispatch])

  const getStatusName = (id: number) => {
    return statuses.find(s => s.id === id)?.name || id
  }

  return (
    <div className='landing'>
      <h1>Current Orders</h1>
      <table className='orders-table'>
        <thead>
          <tr>
            <th>Name</th>
            <th>Status</th>
            <th>Outstanding Tasks</th>
          </tr>
        </thead>
        <tbody>
          {orders.map((o: Order) => (
            <tr key={o.id}>
              <td>{o.name}</td>
              <td>{getStatusName(o.statusId)}</td>
              <td>
                <ul className='tasks-list'>
                  {(outstandingTasks[o.id] ?? []).map(t => (
                    <li key={t.id}>{t.name}</li>
                  ))}
                </ul>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <button className='create-button' onClick={() => setIsModalOpen(true)}>
        Create New Order
      </button>
      <button className='transfer-button' onClick={() => setIsTransferOpen(true)}>
        Initiate Transfer
      </button>
      <CreateOrderModal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} />
      <TransferModal isOpen={isTransferOpen} onClose={() => setIsTransferOpen(false)} />
    </div>
  )
}

export default LandingPage
