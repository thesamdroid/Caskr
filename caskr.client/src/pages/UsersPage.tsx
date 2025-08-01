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
    dispatch(addUser({ name: newName, email: newEmail, userTypeId: newType }))
    setNewName('')
    setNewEmail('')
  }

  const startEdit = (user: User) => {
    setEditing(user.id)
    setEditName(user.name)
    setEditEmail(user.email)
    setEditType(user.userTypeId)
  }

  const handleUpdate = (id: number) => {
    dispatch(updateUser({ id, name: editName, email: editEmail, userTypeId: editType }))
    setEditing(null)
  }

  const typeName = (id: number) => userTypes.find(t => t.id === id)?.name || id

  return (
    <div>
      <h1>Users</h1>
      <form onSubmit={handleAdd}>
        <input value={newName} onChange={e => setNewName(e.target.value)} placeholder='Name'/>
        <input value={newEmail} onChange={e => setNewEmail(e.target.value)} placeholder='Email'/>
        <select value={newType} onChange={e => setNewType(Number(e.target.value))}>
          {userTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
        </select>
        <button type='submit'>Add</button>
      </form>
      <table className='table'>
        <thead>
          <tr><th>Name</th><th>Email</th><th>Type</th><th>Actions</th></tr>
        </thead>
        <tbody>
          {users.map(u => (
            <tr key={u.id}>
              <td>
                {editing === u.id ? (
                  <input value={editName} onChange={e => setEditName(e.target.value)} />
                ) : (
                  u.name
                )}
              </td>
              <td>
                {editing === u.id ? (
                  <input value={editEmail} onChange={e => setEditEmail(e.target.value)} />
                ) : (
                  u.email
                )}
              </td>
              <td>
                {editing === u.id ? (
                  <select value={editType} onChange={e => setEditType(Number(e.target.value))}>
                    {userTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                  </select>
                ) : (
                  typeName(u.userTypeId)
                )}
              </td>
              <td>
                {editing === u.id ? (
                  <>
                    <button onClick={() => handleUpdate(u.id)}>Save</button>
                    <button onClick={() => setEditing(null)}>Cancel</button>
                  </>
                ) : (
                  <>
                    <button onClick={() => startEdit(u)}>Edit</button>
                    <button onClick={() => dispatch(deleteUser(u.id))}>Delete</button>
                  </>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export default UsersPage
