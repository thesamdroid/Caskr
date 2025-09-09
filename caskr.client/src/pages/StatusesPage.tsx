import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchStatuses,
  addStatus,
  updateStatus,
  deleteStatus,
  Status
} from '../features/statusSlice'

function StatusesPage() {
  const dispatch = useAppDispatch()
  const statuses = useAppSelector(state => state.statuses.items)
  const [newName, setNewName] = useState('')
  const [editing, setEditing] = useState<number | null>(null)
  const [editName, setEditName] = useState('')

  useEffect(() => { dispatch(fetchStatuses()) }, [dispatch])

  const handleAdd = (e: React.FormEvent) => {
    e.preventDefault()
    dispatch(addStatus({ name: newName }))
    setNewName('')
  }

  const startEdit = (status: Status) => {
    setEditing(status.id)
    setEditName(status.name)
  }

  const handleUpdate = (id: number) => {
    dispatch(updateStatus({ id, name: editName }))
    setEditing(null)
  }

  return (
    <section className='content-section'>
      <div className='section-header'>
        <h2 className='section-title'>Statuses</h2>
      </div>
      <form onSubmit={handleAdd}>
        <input value={newName} onChange={e => setNewName(e.target.value)} placeholder='Name' />
        <button type='submit'>Add</button>
      </form>
      <div className='table-container'>
        <table className='table'>
          <thead>
            <tr><th>Name</th><th>Actions</th></tr>
          </thead>
          <tbody>
            {statuses.map(s => (
              <tr key={s.id}>
                <td>
                  {editing === s.id ? (
                    <input value={editName} onChange={e => setEditName(e.target.value)} />
                  ) : (
                    s.name
                  )}
                </td>
                <td>
                  {editing === s.id ? (
                    <>
                      <button onClick={() => handleUpdate(s.id)}>Save</button>
                      <button onClick={() => setEditing(null)}>Cancel</button>
                    </>
                  ) : (
                    <>
                      <button onClick={() => startEdit(s)}>Edit</button>
                      <button onClick={() => dispatch(deleteStatus(s.id))}>Delete</button>
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

export default StatusesPage
