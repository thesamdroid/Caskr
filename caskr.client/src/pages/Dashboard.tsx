
export default function Dashboard() {
  return (
    <>
    <main className="main-container">
        <div className="dashboard-grid">
          <div className="stat-card">
            <div className="stat-header">
              <div className="stat-title">Active Orders</div>
              <div className="stat-icon orders">📦</div>
            </div>
            <div className="stat-value">247</div>
            <div className="stat-change positive">↗ +12% from last month</div>
          </div>

          <div className="stat-card">
            <div className="stat-header">
              <div className="stat-title">Barrels in Stock</div>
              <div className="stat-icon barrels">🥃</div>
            </div>
            <div className="stat-value">5,432</div>
            <div className="stat-change negative">↘ -3% from last month</div>
          </div>

          <div className="stat-card">
            <div className="stat-header">
              <div className="stat-title">Avg. Completion</div>
              <div className="stat-icon status">✅</div>
            </div>
            <div className="stat-value">82%</div>
            <div className="stat-change positive">↗ +5% from last month</div>
          </div>

          <div className="stat-card">
            <div className="stat-header">
              <div className="stat-title">Forecast Accuracy</div>
              <div className="stat-icon forecast">📈</div>
            </div>
            <div className="stat-value">91%</div>
            <div className="stat-change positive">↗ +2% from last month</div>
          </div>
        </div>

        <div className="content-section">
          <div className="section-header">
            <h2 className="section-title">Recent Orders</h2>
            <div className="section-actions">
              <button className="btn btn-secondary">View All</button>
              <button className="btn btn-primary">New Order</button>
            </div>
          </div>
          <div className="table-container">
            <table className="table">
              <thead>
                <tr>
                  <th>Order ID</th>
                  <th>Name</th>
                  <th>Status</th>
                  <th>Progress</th>
                  <th>Outstanding</th>
                  <th>Last Update</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td className="font-medium">#ORD-2024-001</td>
                  <td>Barrel Aging Batch 1</td>
                  <td><span className="status-badge in-progress">In Progress</span></td>
                  <td>
                    <div className="progress-bar">
                      <div className="progress-fill" style={{ width: '60%' }} />
                    </div>
                    <span className="text-xs text-gray mt-1">60% Complete</span>
                  </td>
                  <td className="text-danger text-sm">3 tasks</td>
                  <td className="text-sm">Mar 15, 2024</td>
                  <td>
                    <div className="flex gap-2">
                      <button className="btn btn-secondary btn-sm">Details</button>
                      <button className="btn btn-primary btn-sm">Approve</button>
                    </div>
                  </td>
                </tr>
                <tr>
                  <td className="font-medium">#ORD-2024-002</td>
                  <td>Bourbon Prep</td>
                  <td><span className="status-badge pending">Pending</span></td>
                  <td>
                    <div className="progress-bar">
                      <div className="progress-fill" style={{ width: '20%' }} />
                    </div>
                    <span className="text-xs text-gray mt-1">20% Complete</span>
                  </td>
                  <td className="text-warning text-sm">7 tasks</td>
                  <td className="text-sm">Mar 14, 2024</td>
                  <td>
                    <div className="flex gap-2">
                      <button className="btn btn-secondary btn-sm">Details</button>
                      <button className="btn btn-primary btn-sm">Approve</button>
                    </div>
                  </td>
                </tr>
                <tr>
                  <td className="font-medium">#ORD-2024-003</td>
                  <td>Rye Whiskey Reserve</td>
                  <td><span className="status-badge completed">Completed</span></td>
                  <td>
                    <div className="progress-bar">
                      <div className="progress-fill" style={{ width: '100%' }} />
                    </div>
                    <span className="text-xs text-gray mt-1">100% Complete</span>
                  </td>
                  <td className="text-gray text-sm">No outstanding tasks</td>
                  <td className="text-sm">Feb 28, 2024</td>
                  <td>
                    <div className="flex gap-2">
                      <button className="btn btn-secondary btn-sm">Archive</button>
                      <button className="btn btn-primary btn-sm">View</button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div className="content-section">
          <div className="section-header">
            <h2 className="section-title">Barrel Inventory</h2>
            <div className="section-actions">
              <button className="btn btn-secondary">Export Report</button>
              <button className="btn btn-primary">Forecasting</button>
            </div>
          </div>
          <div className="table-container">
            <table className="table">
              <thead>
                <tr>
                  <th>SKU</th>
                  <th>Spirit Type</th>
                  <th>Age</th>
                  <th>Location</th>
                  <th>Status</th>
                  <th>Last Updated</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td className="font-medium">BRL-2024-001</td>
                  <td>Bourbon</td>
                  <td>4 years</td>
                  <td>Warehouse A-12</td>
                  <td><span className="status-badge in-progress">Aging</span></td>
                  <td className="text-sm">2 days ago</td>
                  <td>
                    <button className="btn btn-secondary btn-sm">View Details</button>
                  </td>
                </tr>
                <tr>
                  <td className="font-medium">BRL-2024-002</td>
                  <td>Rye Whiskey</td>
                  <td>6 years</td>
                  <td>Warehouse B-08</td>
                  <td><span className="status-badge completed">Ready</span></td>
                  <td className="text-sm">1 week ago</td>
                  <td>
                    <button className="btn btn-secondary btn-sm">View Details</button>
                  </td>
                </tr>
                <tr>
                  <td className="font-medium">BRL-2024-003</td>
                  <td>Single Malt</td>
                  <td>8 years</td>
                  <td>Warehouse C-15</td>
                  <td><span className="status-badge pending">Quality Check</span></td>
                  <td className="text-sm">3 days ago</td>
                  <td>
                    <button className="btn btn-secondary btn-sm">View Details</button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </main>

      <div className="modal-overlay" style={{ display: 'none' }}>
        <div className="modal">
          <div className="modal-header">
            <h3 className="modal-title">Create New Order</h3>
          </div>
          <div className="modal-body">
            <form>
              <div className="form-group">
                <label className="form-label">Order Name</label>
                <input type="text" className="form-input" placeholder="Enter order name" />
              </div>
              <div className="form-group">
                <label className="form-label">Spirit Type</label>
                <select className="form-select">
                  <option>Select spirit type</option>
                  <option>Bourbon</option>
                  <option>Rye Whiskey</option>
                  <option>Single Malt</option>
                </select>
              </div>
              <div className="form-group">
                <label className="form-label">Quantity</label>
                <input type="number" className="form-input" placeholder="Enter quantity" />
              </div>
            </form>
          </div>
          <div className="modal-footer">
            <button className="btn btn-secondary">Cancel</button>
            <button className="btn btn-primary">Create Order</button>
          </div>
        </div>
      </div>
    </>
  )
}
