import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchProducts,
  addProduct,
  updateProduct,
  deleteProduct,
  Product
} from '../features/productsSlice'

function ProductsPage() {
  const dispatch = useAppDispatch()
  const products = useAppSelector(state => state.products.items)

  const [newOwner, setNewOwner] = useState(0)
  const [newNotes, setNewNotes] = useState('')
  const [editing, setEditing] = useState<number | null>(null)
  const [editOwner, setEditOwner] = useState(0)
  const [editNotes, setEditNotes] = useState('')

  useEffect(() => {
    dispatch(fetchProducts())
  }, [dispatch])

  const handleAdd = (e: React.FormEvent) => {
    e.preventDefault()
    dispatch(addProduct({ ownerId: newOwner, notes: newNotes }))
    setNewOwner(0)
    setNewNotes('')
  }

  const startEdit = (product: Product) => {
    setEditing(product.id)
    setEditOwner(product.ownerId)
    setEditNotes(product.notes ?? '')
  }

  const handleUpdate = (id: number) => {
    dispatch(updateProduct({ id, ownerId: editOwner, notes: editNotes }))
    setEditing(null)
  }

  return (
    <div>
      <h1>Products</h1>
      <form onSubmit={handleAdd}>
        <input type='number' value={newOwner} onChange={e => setNewOwner(Number(e.target.value))} placeholder='Owner ID' />
        <input value={newNotes} onChange={e => setNewNotes(e.target.value)} placeholder='Notes' />
        <button type='submit'>Add</button>
      </form>
      <table className='table'>
        <thead>
          <tr><th>OwnerId</th><th>Notes</th><th>Actions</th></tr>
        </thead>
        <tbody>
          {products.map(p => (
            <tr key={p.id}>
              <td>
                {editing === p.id ? (
                  <input type='number' value={editOwner} onChange={e => setEditOwner(Number(e.target.value))} />
                ) : (
                  p.ownerId
                )}
              </td>
              <td>
                {editing === p.id ? (
                  <input value={editNotes} onChange={e => setEditNotes(e.target.value)} />
                ) : (
                  p.notes
                )}
              </td>
              <td>
                {editing === p.id ? (
                  <>
                    <button onClick={() => handleUpdate(p.id)}>Save</button>
                    <button onClick={() => setEditing(null)}>Cancel</button>
                  </>
                ) : (
                  <>
                    <button onClick={() => startEdit(p)}>Edit</button>
                    <button onClick={() => dispatch(deleteProduct(p.id))}>Delete</button>
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

export default ProductsPage
