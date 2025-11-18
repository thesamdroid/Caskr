import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchUsers,
  addUser,
  updateUser,
  deleteUser,
  User
} from '../features/usersSlice'
import { fetchUserTypes } from '../features/userTypesSlice'

function UsersPage() {
  const dispatch = useAppDispatch()
  const users = useAppSelector(state => state.users.items)
  const userTypes = useAppSelector(state => state.userTypes.items)

  const [newName, setNewName] = useState('')
  const [newEmail, setNewEmail] = useState('')
  const [newType, setNewType] = useState<number>(0)
  const [newPassword, setNewPassword] = useState('')

  const [editing, setEditing] = useState<number | null>(null)
  const [editName, setEditName] = useState('')
  const [editEmail, setEditEmail] = useState('')
  const [editType, setEditType] = useState<number>(0)

  useEffect(() => {
    dispatch(fetchUsers())
    dispatch(fetchUserTypes())
  }, [dispatch])

  useEffect(() => {
    if (userTypes.length > 0 && newType === 0) {
      setNewType(userTypes[0].id)
    }
  }, [userTypes, newType])

  const handleAdd = (e: React.FormEvent) => {
    e.preventDefault()
    dispatch(addUser({ name: newName, email: newEmail, userTypeId: newType, temporaryPassword: newPassword }))
    setNewName('')
    setNewEmail('')
    setNewPassword('')
  }

  const startEdit = (user: User) => {
    setEditing(user.id)
    setEditName(user.name)
    setEditEmail(user.email)
    setEditType(user.userTypeId)
  }

  const handleUpdate = (id: number) => {
    const existing = users.find(u => u.id === id)
    if (!existing) return
    dispatch(updateUser({ ...existing, name: editName, email: editEmail, userTypeId: editType }))
    setEditing(null)
  }

  const typeName = (id: number) => userTypes.find(t => t.id === id)?.name || id

  return (
    <section className="content-section" aria-labelledby="users-title">
      <div className="section-header">
        <div>
          <h1 id="users-title" className="section-title">Users</h1>
          <p className="section-subtitle">Manage user accounts and permissions</p>
        </div>
      </div>

      <form onSubmit={handleAdd} className="inline-form" aria-label="Add new user">
        <label htmlFor="user-name" className="visually-hidden">User Name</label>
        <input
          id="user-name"
          value={newName}
          onChange={e => setNewName(e.target.value)}
          placeholder="Name"
          required
          aria-required="true"
        />

        <label htmlFor="user-email" className="visually-hidden">Email</label>
        <input
          id="user-email"
          type="email"
          value={newEmail}
          onChange={e => setNewEmail(e.target.value)}
          placeholder="Email"
          required
          aria-required="true"
        />

        <label htmlFor="user-password" className="visually-hidden">Temporary Password</label>
        <input
          id="user-password"
          type="password"
          value={newPassword}
          onChange={e => setNewPassword(e.target.value)}
          placeholder="Temporary password"
          required
          aria-required="true"
        />

        <label htmlFor="user-type" className="visually-hidden">User Type</label>
        <select
          id="user-type"
          value={newType}
          onChange={e => setNewType(Number(e.target.value))}
          required
          aria-required="true"
        >
          {userTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
        </select>

        <button type="submit" className="button-primary">
          Create User
        </button>
      </form>

      {users.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">👤</div>
          <h3 className="empty-state-title">No users yet</h3>
          <p className="empty-state-text">Create your first user using the form above</p>
        </div>
      ) : (
        <div className="table-container">
          <table className="table" role="table" aria-label="Users list">
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">Email</th>
                <th scope="col">Type</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.map(u => (
                <tr key={u.id}>
                  <td>
                    {editing === u.id ? (
                      <>
                        <label htmlFor={`edit-name-${u.id}`} className="visually-hidden">Name</label>
                        <input
                          id={`edit-name-${u.id}`}
                          value={editName}
                          onChange={e => setEditName(e.target.value)}
                          aria-label="Edit user name"
                        />
                      </>
                    ) : (
                      <span className="text-primary">{u.name}</span>
                    )}
                  </td>
                  <td>
                    {editing === u.id ? (
                      <>
                        <label htmlFor={`edit-email-${u.id}`} className="visually-hidden">Email</label>
                        <input
                          id={`edit-email-${u.id}`}
                          type="email"
                          value={editEmail}
                          onChange={e => setEditEmail(e.target.value)}
                          aria-label="Edit user email"
                        />
                      </>
                    ) : (
                      <span className="text-secondary">{u.email}</span>
                    )}
                  </td>
                  <td>
                    {editing === u.id ? (
                      <>
                        <label htmlFor={`edit-type-${u.id}`} className="visually-hidden">User Type</label>
                        <select
                          id={`edit-type-${u.id}`}
                          value={editType}
                          onChange={e => setEditType(Number(e.target.value))}
                          aria-label="Edit user type"
                        >
                          {userTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                        </select>
                      </>
                    ) : (
                      <span className="status-badge default">{typeName(u.userTypeId)}</span>
                    )}
                  </td>
                  <td>
                    <div className="flex gap-2">
                      {editing === u.id ? (
                        <>
                          <button
                            onClick={() => handleUpdate(u.id)}
                            className="button-primary button-sm"
                            aria-label={`Save changes to user ${u.name}`}
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
                            onClick={() => startEdit(u)}
                            className="button-secondary button-sm"
                            aria-label={`Edit user ${u.name}`}
                          >
                            Edit
                          </button>
                          <button
                            onClick={() => dispatch(deleteUser(u.id))}
                            className="button-danger button-sm"
                            aria-label={`Delete user ${u.name}`}
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

export default UsersPage
