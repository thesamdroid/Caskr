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
    <section className='content-section'>
      <div className='section-header'>
        <h2 className='section-title'>User Types</h2>
      </div>
      <form onSubmit={handleAdd}>
        <input value={newName} onChange={e => setNewName(e.target.value)} placeholder='Name'/>
        <button type='submit'>Add</button>
      </form>
      <div className='table-container'>
        <table className='table'>
          <thead>
            <tr><th>Name</th><th>Actions</th></tr>
          </thead>
          <tbody>
            {userTypes.map(ut => (
              <tr key={ut.id}>
                <td>
                  {editing === ut.id ? (
                    <input value={editName} onChange={e => setEditName(e.target.value)} />
                  ) : (
                    ut.name
                  )}
                </td>
                <td>
                  {editing === ut.id ? (
                    <>
                      <button onClick={() => handleUpdate(ut.id)}>Save</button>
                      <button onClick={() => setEditing(null)}>Cancel</button>
                    </>
                  ) : (
                    <>
                      <button onClick={() => startEdit(ut)}>Edit</button>
                      <button onClick={() => dispatch(deleteUserType(ut.id))}>Delete</button>
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

export default UserTypesPage
