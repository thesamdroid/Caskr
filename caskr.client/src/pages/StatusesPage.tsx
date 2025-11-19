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
    <section className="content-section" aria-labelledby="statuses-title">
      <div className="section-header">
        <div>
          <h1 id="statuses-title" className="section-title">Statuses</h1>
          <p className="section-subtitle">Manage order status types</p>
        </div>
      </div>

      <form onSubmit={handleAdd} className="inline-form" aria-label="Add new status">
        <label htmlFor="status-name" className="visually-hidden">Status Name</label>
        <input
          id="status-name"
          value={newName}
          onChange={e => setNewName(e.target.value)}
          placeholder="Status name"
          required
          aria-required="true"
        />

        <button type="submit" className="button-primary">
          Add Status
        </button>
      </form>

      {statuses.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">📋</div>
          <h3 className="empty-state-title">No statuses yet</h3>
          <p className="empty-state-text">Create your first status using the form above</p>
        </div>
      ) : (
        <div className="table-container">
          <table className="table" role="table" aria-label="Statuses list">
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {statuses.map(s => (
                <tr key={s.id}>
                  <td>
                    {editing === s.id ? (
                      <>
                        <label htmlFor={`edit-status-${s.id}`} className="visually-hidden">Status Name</label>
                        <input
                          id={`edit-status-${s.id}`}
                          value={editName}
                          onChange={e => setEditName(e.target.value)}
                          aria-label="Edit status name"
                        />
                      </>
                    ) : (
                      <span className="text-primary">{s.name}</span>
                    )}
                  </td>
                  <td>
                    <div className="flex gap-2">
                      {editing === s.id ? (
                        <>
                          <button
                            onClick={() => handleUpdate(s.id)}
                            className="button-primary button-sm"
                            aria-label={`Save changes to status ${s.name}`}
                          >
                            Save
                          </button>
                          <button
                            onClick={() => setEditing(null)}
                            className="button-secondary button-sm"
                            aria-label="Cancel editing"
                          >
                            Cancel
                          </button>
                        </>
                      ) : (
                        <>
                          <button
                            onClick={() => startEdit(s)}
                            className="button-secondary button-sm"
                            aria-label={`Edit status ${s.name}`}
                          >
                            Edit
                          </button>
                          <button
                            onClick={() => dispatch(deleteStatus(s.id))}
                            className="button-danger button-sm"
                            aria-label={`Delete status ${s.name}`}
                          >
                            Delete
                          </button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

export default StatusesPage
