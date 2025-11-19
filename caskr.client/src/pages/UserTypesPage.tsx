import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchUserTypes,
  addUserType,
  updateUserType,
  deleteUserType,
  UserType
} from '../features/userTypesSlice'

function UserTypesPage() {
  const dispatch = useAppDispatch()
  const userTypes = useAppSelector(state => state.userTypes.items)
  const [newName, setNewName] = useState('')
  const [editing, setEditing] = useState<number | null>(null)
  const [editName, setEditName] = useState('')

  useEffect(() => { dispatch(fetchUserTypes()) }, [dispatch])

  const handleAdd = (e: React.FormEvent) => {
    e.preventDefault()
    dispatch(addUserType({ name: newName }))
    setNewName('')
  }

  const startEdit = (ut: UserType) => {
    setEditing(ut.id)
    setEditName(ut.name ?? '')
  }

  const handleUpdate = (id: number) => {
    dispatch(updateUserType({ id, name: editName }))
    setEditing(null)
  }

  return (
    <section className="content-section" aria-labelledby="user-types-title">
      <div className="section-header">
        <div>
          <h1 id="user-types-title" className="section-title">User Types</h1>
          <p className="section-subtitle">Manage user role types</p>
        </div>
      </div>

      <form onSubmit={handleAdd} className="inline-form" aria-label="Add new user type">
        <label htmlFor="user-type-name" className="visually-hidden">User Type Name</label>
        <input
          id="user-type-name"
          value={newName}
          onChange={e => setNewName(e.target.value)}
          placeholder="User type name"
          required
          aria-required="true"
        />

        <button type="submit" className="button-primary">
          Add User Type
        </button>
      </form>

      {userTypes.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">👥</div>
          <h3 className="empty-state-title">No user types yet</h3>
          <p className="empty-state-text">Create your first user type using the form above</p>
        </div>
      ) : (
        <div className="table-container">
          <table className="table" role="table" aria-label="User types list">
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {userTypes.map(ut => (
                <tr key={ut.id}>
                  <td>
                    {editing === ut.id ? (
                      <>
                        <label htmlFor={`edit-user-type-${ut.id}`} className="visually-hidden">User Type Name</label>
                        <input
                          id={`edit-user-type-${ut.id}`}
                          value={editName}
                          onChange={e => setEditName(e.target.value)}
                          aria-label="Edit user type name"
                        />
                      </>
                    ) : (
                      <span className="text-primary">{ut.name}</span>
                    )}
                  </td>
                  <td>
                    <div className="flex gap-2">
                      {editing === ut.id ? (
                        <>
                          <button
                            onClick={() => handleUpdate(ut.id)}
                            className="button-primary button-sm"
                            aria-label={`Save changes to user type ${ut.name}`}
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
                            onClick={() => startEdit(ut)}
                            className="button-secondary button-sm"
                            aria-label={`Edit user type ${ut.name}`}
                          >
                            Edit
                          </button>
                          <button
                            onClick={() => dispatch(deleteUserType(ut.id))}
                            className="button-danger button-sm"
                            aria-label={`Delete user type ${ut.name}`}
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

export default UserTypesPage
