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
    <section className="content-section" aria-labelledby="products-title">
      <div className="section-header">
        <div>
          <h1 id="products-title" className="section-title">Products</h1>
          <p className="section-subtitle">Manage your product catalog</p>
        </div>
      </div>

      <form onSubmit={handleAdd} className="inline-form" aria-label="Add new product">
        <label htmlFor="product-owner" className="visually-hidden">Owner ID</label>
        <input
          id="product-owner"
          type="number"
          value={newOwner || ''}
          onChange={e => setNewOwner(Number(e.target.value))}
          placeholder="Owner ID"
          required
          aria-required="true"
        />

        <label htmlFor="product-notes" className="visually-hidden">Notes</label>
        <input
          id="product-notes"
          value={newNotes}
          onChange={e => setNewNotes(e.target.value)}
          placeholder="Product notes"
        />

        <button type="submit" className="button-primary">
          Add Product
        </button>
      </form>

      {products.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">🏷️</div>
          <h3 className="empty-state-title">No products yet</h3>
          <p className="empty-state-text">Add your first product using the form above</p>
        </div>
      ) : (
        <div className="table-container">
          <table className="table" role="table" aria-label="Products list">
            <thead>
              <tr>
                <th scope="col">Owner ID</th>
                <th scope="col">Notes</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {products.map(p => (
                <tr key={p.id}>
                  <td>
                    {editing === p.id ? (
                      <>
                        <label htmlFor={`edit-owner-${p.id}`} className="visually-hidden">Owner ID</label>
                        <input
                          id={`edit-owner-${p.id}`}
                          type="number"
                          value={editOwner}
                          onChange={e => setEditOwner(Number(e.target.value))}
                          aria-label="Edit owner ID"
                        />
                      </>
                    ) : (
                      p.ownerId
                    )}
                  </td>
                  <td>
                    {editing === p.id ? (
                      <>
                        <label htmlFor={`edit-notes-${p.id}`} className="visually-hidden">Notes</label>
                        <input
                          id={`edit-notes-${p.id}`}
                          value={editNotes}
                          onChange={e => setEditNotes(e.target.value)}
                          aria-label="Edit product notes"
                        />
                      </>
                    ) : (
                      <span className="text-secondary">{p.notes || <span className="text-muted">No notes</span>}</span>
                    )}
                  </td>
                  <td>
                    <div className="flex gap-2">
                      {editing === p.id ? (
                        <>
                          <button
                            onClick={() => handleUpdate(p.id)}
                            className="button-primary button-sm"
                            aria-label={`Save changes to product ${p.id}`}
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
                            onClick={() => startEdit(p)}
                            className="button-secondary button-sm"
                            aria-label={`Edit product ${p.id}`}
                          >
                            Edit
                          </button>
                          <button
                            onClick={() => dispatch(deleteProduct(p.id))}
                            className="button-danger button-sm"
                            aria-label={`Delete product ${p.id}`}
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

export default ProductsPage
