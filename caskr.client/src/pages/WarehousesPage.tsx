import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchWarehouses,
  deactivateWarehouse,
  activateWarehouse,
  Warehouse
} from '../features/warehousesSlice'
import WarehouseFormModal from '../components/WarehouseFormModal'

// Helper to get occupancy bar color based on percentage
function getOccupancyColor(percentage: number): string {
  if (percentage >= 95) return 'var(--color-error)'
  if (percentage >= 80) return 'var(--color-warning)'
  return 'var(--color-success)'
}

// Helper to format warehouse type for display
function formatWarehouseType(type: string): string {
  return type.replace('_', ' ')
}

function WarehousesPage() {
  const dispatch = useAppDispatch()
  const warehouses = useAppSelector(state => state.warehouses.items)
  const loading = useAppSelector(state => state.warehouses.loading)
  const authUser = useAppSelector(state => state.auth.user)
  const companyId = authUser?.companyId ?? 1
  const [showModal, setShowModal] = useState(false)
  const [editingWarehouse, setEditingWarehouse] = useState<Warehouse | null>(null)
  const [showInactive, setShowInactive] = useState(false)
  const [confirmDeactivate, setConfirmDeactivate] = useState<Warehouse | null>(null)

  useEffect(() => {
    dispatch(fetchWarehouses({ companyId, includeInactive: showInactive }))
  }, [dispatch, companyId, showInactive])

  const handleCreateWarehouse = () => {
    setEditingWarehouse(null)
    setShowModal(true)
  }

  const handleEditWarehouse = (warehouse: Warehouse) => {
    setEditingWarehouse(warehouse)
    setShowModal(true)
  }

  const handleCloseModal = () => {
    setShowModal(false)
    setEditingWarehouse(null)
  }

  const handleDeactivateClick = (warehouse: Warehouse) => {
    setConfirmDeactivate(warehouse)
  }

  const handleConfirmDeactivate = async () => {
    if (confirmDeactivate) {
      try {
        await dispatch(deactivateWarehouse(confirmDeactivate.id)).unwrap()
        setConfirmDeactivate(null)
        dispatch(fetchWarehouses({ companyId, includeInactive: showInactive }))
      } catch (error: unknown) {
        const errorMessage = error && typeof error === 'object' && 'message' in error
          ? (error as { message: string }).message
          : 'Failed to deactivate warehouse'
        alert(errorMessage)
      }
    }
  }

  const handleActivate = async (warehouse: Warehouse) => {
    try {
      await dispatch(activateWarehouse(warehouse.id)).unwrap()
      dispatch(fetchWarehouses({ companyId, includeInactive: showInactive }))
    } catch (error: unknown) {
      const errorMessage = error && typeof error === 'object' && 'message' in error
        ? (error as { message: string }).message
        : 'Failed to activate warehouse'
      alert(errorMessage)
    }
  }

  // Calculate totals
  const totalCapacity = warehouses.filter(w => w.isActive).reduce((sum, w) => sum + w.totalCapacity, 0)
  const totalOccupied = warehouses.filter(w => w.isActive).reduce((sum, w) => sum + w.occupiedPositions, 0)
  const overallOccupancy = totalCapacity > 0 ? Math.round((totalOccupied / totalCapacity) * 100) : 0

  return (
    <>
      <section className="content-section" aria-labelledby="warehouses-title">
        <div className="section-header">
          <div>
            <h1 id="warehouses-title" className="section-title">Warehouses</h1>
            <p className="section-subtitle">Manage your storage facilities and track capacity utilization</p>
          </div>
          <div className="section-actions">
            <label className="checkbox-label" style={{ marginRight: '1rem' }}>
              <input
                type="checkbox"
                checked={showInactive}
                onChange={e => setShowInactive(e.target.checked)}
              />
              <span>Show inactive</span>
            </label>
            <button
              onClick={handleCreateWarehouse}
              className="button-primary"
              aria-label="Create new warehouse"
            >
              + Add Warehouse
            </button>
          </div>
        </div>

        {/* Summary Cards */}
        <div className="stats-grid" style={{ marginBottom: '2rem' }}>
          <div className="stat-card">
            <span className="stat-label">Total Warehouses</span>
            <span className="stat-value">{warehouses.filter(w => w.isActive).length}</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Total Capacity</span>
            <span className="stat-value">{totalCapacity.toLocaleString()}</span>
            <span className="stat-sublabel">barrel positions</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Total Occupied</span>
            <span className="stat-value">{totalOccupied.toLocaleString()}</span>
            <span className="stat-sublabel">barrels</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Overall Occupancy</span>
            <span className="stat-value" style={{ color: getOccupancyColor(overallOccupancy) }}>
              {overallOccupancy}%
            </span>
          </div>
        </div>

        {loading ? (
          <div className="loading-state">
            <p>Loading warehouses...</p>
          </div>
        ) : warehouses.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">🏭</div>
            <h3 className="empty-state-title">No warehouses configured</h3>
            <p className="empty-state-text">
              Add your first warehouse to start tracking barrel storage locations
            </p>
            <button onClick={handleCreateWarehouse} className="button-primary">
              + Add Warehouse
            </button>
          </div>
        ) : (
          <div className="table-container">
            <table className="table" role="table" aria-label="Warehouses">
              <thead>
                <tr>
                  <th scope="col">Name</th>
                  <th scope="col">Type</th>
                  <th scope="col">Location</th>
                  <th scope="col">Capacity</th>
                  <th scope="col">Occupancy</th>
                  <th scope="col">Status</th>
                  <th scope="col">Actions</th>
                </tr>
              </thead>
              <tbody>
                {warehouses.map(warehouse => (
                  <tr key={warehouse.id} className={!warehouse.isActive ? 'row-inactive' : ''}>
                    <td>
                      <span className="text-gold">{warehouse.name}</span>
                    </td>
                    <td>
                      <span className="badge badge-info">
                        {formatWarehouseType(warehouse.warehouseType)}
                      </span>
                    </td>
                    <td>
                      <span className="text-secondary">
                        {warehouse.city && warehouse.state
                          ? `${warehouse.city}, ${warehouse.state}`
                          : warehouse.fullAddress || '-'}
                      </span>
                    </td>
                    <td>
                      <span>{warehouse.totalCapacity.toLocaleString()} barrels</span>
                    </td>
                    <td>
                      <div className="occupancy-cell">
                        <div className="occupancy-bar-container">
                          <div
                            className="occupancy-bar"
                            style={{
                              width: `${Math.min(warehouse.occupancyPercentage, 100)}%`,
                              backgroundColor: getOccupancyColor(warehouse.occupancyPercentage)
                            }}
                          />
                        </div>
                        <span className="occupancy-text">
                          {warehouse.occupiedPositions.toLocaleString()} ({warehouse.occupancyPercentage}%)
                        </span>
                      </div>
                    </td>
                    <td>
                      {warehouse.isActive ? (
                        <span className="badge badge-success">Active</span>
                      ) : (
                        <span className="badge badge-inactive">Inactive</span>
                      )}
                    </td>
                    <td>
                      <div className="action-buttons">
                        <button
                          onClick={() => handleEditWarehouse(warehouse)}
                          className="button-small button-secondary"
                          title="Edit warehouse"
                        >
                          Edit
                        </button>
                        {warehouse.isActive ? (
                          <button
                            onClick={() => handleDeactivateClick(warehouse)}
                            className="button-small button-danger"
                            title="Deactivate warehouse"
                          >
                            Deactivate
                          </button>
                        ) : (
                          <button
                            onClick={() => handleActivate(warehouse)}
                            className="button-small button-success"
                            title="Activate warehouse"
                          >
                            Activate
                          </button>
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

      {/* Create/Edit Modal */}
      <WarehouseFormModal
        isOpen={showModal}
        onClose={handleCloseModal}
        warehouse={editingWarehouse}
        companyId={companyId}
      />

      {/* Deactivate Confirmation Dialog */}
      {confirmDeactivate && (
        <div className="modal-overlay" onClick={() => setConfirmDeactivate(null)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Confirm Deactivation</h2>
              <button
                onClick={() => setConfirmDeactivate(null)}
                className="modal-close"
                aria-label="Close"
              >
                &times;
              </button>
            </div>
            <div className="modal-body">
              <p>
                Are you sure you want to deactivate <strong>{confirmDeactivate.name}</strong>?
              </p>
              {confirmDeactivate.occupiedPositions > 0 && (
                <p className="text-warning">
                  Warning: This warehouse has {confirmDeactivate.occupiedPositions} barrels stored in it.
                  You must transfer them before deactivating.
                </p>
              )}
              <p className="text-secondary">
                Deactivated warehouses are hidden from dropdowns but remain in the database for historical records.
              </p>
            </div>
            <div className="modal-footer">
              <button
                onClick={() => setConfirmDeactivate(null)}
                className="button-secondary"
              >
                Cancel
              </button>
              <button
                onClick={handleConfirmDeactivate}
                className="button-danger"
                disabled={confirmDeactivate.occupiedPositions > 0}
              >
                Deactivate
              </button>
            </div>
          </div>
        </div>
      )}

      <style>{`
        .stats-grid {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
          gap: 1rem;
        }

        .stat-card {
          background: var(--color-surface);
          border: 1px solid var(--color-border);
          border-radius: 8px;
          padding: 1rem;
          display: flex;
          flex-direction: column;
          align-items: center;
        }

        .stat-label {
          font-size: 0.875rem;
          color: var(--color-text-secondary);
          margin-bottom: 0.25rem;
        }

        .stat-value {
          font-size: 1.75rem;
          font-weight: 600;
          color: var(--color-text);
        }

        .stat-sublabel {
          font-size: 0.75rem;
          color: var(--color-text-secondary);
        }

        .occupancy-cell {
          display: flex;
          flex-direction: column;
          gap: 0.25rem;
        }

        .occupancy-bar-container {
          width: 100px;
          height: 8px;
          background: var(--color-border);
          border-radius: 4px;
          overflow: hidden;
        }

        .occupancy-bar {
          height: 100%;
          border-radius: 4px;
          transition: width 0.3s ease;
        }

        .occupancy-text {
          font-size: 0.875rem;
          color: var(--color-text-secondary);
        }

        .row-inactive {
          opacity: 0.6;
        }

        .action-buttons {
          display: flex;
          gap: 0.5rem;
        }

        .button-small {
          padding: 0.25rem 0.5rem;
          font-size: 0.875rem;
          border-radius: 4px;
          border: none;
          cursor: pointer;
        }

        .button-secondary {
          background: var(--color-surface);
          border: 1px solid var(--color-border);
          color: var(--color-text);
        }

        .button-secondary:hover {
          background: var(--color-border);
        }

        .button-danger {
          background: var(--color-error);
          color: white;
        }

        .button-danger:hover {
          opacity: 0.9;
        }

        .button-danger:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }

        .button-success {
          background: var(--color-success);
          color: white;
        }

        .button-success:hover {
          opacity: 0.9;
        }

        .badge {
          padding: 0.25rem 0.5rem;
          border-radius: 4px;
          font-size: 0.75rem;
          font-weight: 500;
        }

        .badge-info {
          background: var(--color-info-bg, #e0f2fe);
          color: var(--color-info, #0284c7);
        }

        .badge-success {
          background: var(--color-success-bg, #dcfce7);
          color: var(--color-success, #16a34a);
        }

        .badge-inactive {
          background: var(--color-border);
          color: var(--color-text-secondary);
        }

        .checkbox-label {
          display: flex;
          align-items: center;
          gap: 0.5rem;
          cursor: pointer;
          font-size: 0.875rem;
        }

        .modal-overlay {
          position: fixed;
          top: 0;
          left: 0;
          right: 0;
          bottom: 0;
          background: rgba(0, 0, 0, 0.5);
          display: flex;
          align-items: center;
          justify-content: center;
          z-index: 1000;
        }

        .modal {
          background: var(--color-background);
          border-radius: 8px;
          max-width: 500px;
          width: 90%;
          max-height: 90vh;
          overflow-y: auto;
        }

        .modal-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding: 1rem;
          border-bottom: 1px solid var(--color-border);
        }

        .modal-header h2 {
          margin: 0;
          font-size: 1.25rem;
        }

        .modal-close {
          background: none;
          border: none;
          font-size: 1.5rem;
          cursor: pointer;
          color: var(--color-text-secondary);
        }

        .modal-body {
          padding: 1rem;
        }

        .modal-footer {
          display: flex;
          justify-content: flex-end;
          gap: 0.5rem;
          padding: 1rem;
          border-top: 1px solid var(--color-border);
        }

        .text-warning {
          color: var(--color-warning);
          margin: 1rem 0;
          padding: 0.5rem;
          background: var(--color-warning-bg, #fef3c7);
          border-radius: 4px;
        }

        .loading-state {
          text-align: center;
          padding: 2rem;
          color: var(--color-text-secondary);
        }
      `}</style>
    </>
  )
}

export default WarehousesPage
