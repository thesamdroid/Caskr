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
    <div>
      <h1>Statuses</h1>
      <form onSubmit={handleAdd}>
        <input value={newName} onChange={e => setNewName(e.target.value)} placeholder='Name'/>
        <button type='submit'>Add</button>
      </form>
      <ul>
        {statuses.map(s => (
          <li key={s.id}>
            {editing === s.id ? (
              <>
                <input value={editName} onChange={e => setEditName(e.target.value)} />
                <button onClick={() => handleUpdate(s.id)}>Save</button>
                <button onClick={() => setEditing(null)}>Cancel</button>
              </>
            ) : (
              <>
                {s.name}
                <button onClick={() => startEdit(s)}>Edit</button>
                <button onClick={() => dispatch(deleteStatus(s.id))}>Delete</button>
              </>
            )}
          </li>
        ))}
      </ul>
    </div>
  )
}

export default StatusesPage
