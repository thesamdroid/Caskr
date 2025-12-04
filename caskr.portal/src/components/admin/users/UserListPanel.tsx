import { useState } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import {
  fetchUsers,
  fetchUserById,
  setFilters,
  resetFilters
} from '../../../features/userAdminSlice'
import { exportApi } from '../../../api/userAdminApi'
import type { UserSearchFilters, UserRole, UserStatus, SubscriptionStatus } from '../../../types/userAdmin'
import {
  formatUserRole,
  formatUserStatus,
  getStatusBadgeClass,
  getRoleBadgeClass,
  getSubscriptionBadgeClass,
  formatRelativeTime
} from '../../../types/userAdmin'

function UserListPanel() {
  const dispatch = useAppDispatch()
  const {
    users,
    totalUsers,
    totalPages,
    filters,
    isLoadingList,
    selectedUser
  } = useAppSelector(state => state.userAdmin)

  const [showFilters, setShowFilters] = useState(false)
  const [localFilters, setLocalFilters] = useState<UserSearchFilters>(filters)

  const handleFilterChange = (key: keyof UserSearchFilters, value: string | number) => {
    setLocalFilters(prev => ({ ...prev, [key]: value }))
  }

  const handleApplyFilters = () => {
    dispatch(setFilters({ ...localFilters, page: 1 }))
    dispatch(fetchUsers({ ...localFilters, page: 1 }))
  }

  const handleResetFilters = () => {
    dispatch(resetFilters())
    setLocalFilters({
      search: '',
      role: '',
      status: '',
      subscriptionStatus: '',
      subscriptionTier: '',
      createdAfter: '',
      createdBefore: '',
      sortBy: 'createdAt',
      sortOrder: 'desc',
      page: 1,
      pageSize: 25
    })
    dispatch(fetchUsers({}))
  }

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    handleApplyFilters()
  }

  const handlePageChange = (newPage: number) => {
    dispatch(setFilters({ page: newPage }))
    dispatch(fetchUsers({ ...filters, page: newPage }))
  }

  const handleSelectUser = (userId: string) => {
    dispatch(fetchUserById(userId))
  }

  const handleExportCsv = async () => {
    try {
      const result = await exportApi.usersList(filters)
      const blob = new Blob([result.csv], { type: 'text/csv' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `users-export-${new Date().toISOString().slice(0, 10)}.csv`
      a.click()
      URL.revokeObjectURL(url)
    } catch (error) {
      console.error('Export failed:', error)
    }
  }

  const handleSort = (column: UserSearchFilters['sortBy']) => {
    const newOrder = filters.sortBy === column && filters.sortOrder === 'asc' ? 'desc' : 'asc'
    dispatch(setFilters({ sortBy: column, sortOrder: newOrder }))
    dispatch(fetchUsers({ ...filters, sortBy: column, sortOrder: newOrder }))
  }

  const getSortIndicator = (column: UserSearchFilters['sortBy']) => {
    if (filters.sortBy !== column) return null
    return filters.sortOrder === 'asc' ? ' \u25B2' : ' \u25BC'
  }

  return (
    <div className="user-list-panel">
      {/* Search Bar */}
      <div className="list-header">
        <form className="search-form" onSubmit={handleSearch}>
          <input
            type="text"
            placeholder="Search by email, name, or distillery..."
            value={localFilters.search}
            onChange={e => handleFilterChange('search', e.target.value)}
            className="search-input"
          />
          <button type="submit" className="btn btn-primary">
            Search
          </button>
        </form>
        <div className="list-actions">
          <button
            type="button"
            className={`btn btn-secondary ${showFilters ? 'active' : ''}`}
            onClick={() => setShowFilters(!showFilters)}
          >
            Filters {showFilters ? '\u25B2' : '\u25BC'}
          </button>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={handleExportCsv}
          >
            Export CSV
          </button>
        </div>
      </div>

      {/* Advanced Filters */}
      {showFilters && (
        <div className="advanced-filters">
          <div className="filter-row">
            <div className="filter-group">
              <label htmlFor="filter-role">Role</label>
              <select
                id="filter-role"
                value={localFilters.role}
                onChange={e => handleFilterChange('role', e.target.value as UserRole | '')}
              >
                <option value="">All Roles</option>
                <option value="User">User</option>
                <option value="Admin">Admin</option>
                <option value="PricingManager">Pricing Manager</option>
                <option value="SuperAdmin">Super Admin</option>
              </select>
            </div>

            <div className="filter-group">
              <label htmlFor="filter-status">Status</label>
              <select
                id="filter-status"
                value={localFilters.status}
                onChange={e => handleFilterChange('status', e.target.value as UserStatus | '')}
              >
                <option value="">All Statuses</option>
                <option value="Active">Active</option>
                <option value="Suspended">Suspended</option>
                <option value="PendingVerification">Pending Verification</option>
                <option value="Deleted">Deleted</option>
              </select>
            </div>

            <div className="filter-group">
              <label htmlFor="filter-subscription">Subscription</label>
              <select
                id="filter-subscription"
                value={localFilters.subscriptionStatus}
                onChange={e => handleFilterChange('subscriptionStatus', e.target.value as SubscriptionStatus | '')}
              >
                <option value="">All Subscriptions</option>
                <option value="Active">Active</option>
                <option value="Trialing">Trialing</option>
                <option value="PastDue">Past Due</option>
                <option value="Cancelled">Cancelled</option>
                <option value="None">None</option>
              </select>
            </div>
          </div>

          <div className="filter-row">
            <div className="filter-group">
              <label htmlFor="filter-created-after">Created After</label>
              <input
                id="filter-created-after"
                type="date"
                value={localFilters.createdAfter}
                onChange={e => handleFilterChange('createdAfter', e.target.value)}
              />
            </div>

            <div className="filter-group">
              <label htmlFor="filter-created-before">Created Before</label>
              <input
                id="filter-created-before"
                type="date"
                value={localFilters.createdBefore}
                onChange={e => handleFilterChange('createdBefore', e.target.value)}
              />
            </div>

            <div className="filter-group">
              <label htmlFor="filter-page-size">Per Page</label>
              <select
                id="filter-page-size"
                value={localFilters.pageSize}
                onChange={e => handleFilterChange('pageSize', parseInt(e.target.value))}
              >
                <option value={10}>10</option>
                <option value={25}>25</option>
                <option value={50}>50</option>
                <option value={100}>100</option>
              </select>
            </div>
          </div>

          <div className="filter-actions">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={handleResetFilters}
            >
              Reset
            </button>
            <button
              type="button"
              className="btn btn-primary"
              onClick={handleApplyFilters}
            >
              Apply Filters
            </button>
          </div>
        </div>
      )}

      {/* Results count */}
      <div className="results-summary">
        <span>{totalUsers} user{totalUsers !== 1 ? 's' : ''} found</span>
      </div>

      {/* User List */}
      {isLoadingList ? (
        <div className="loading-state">
          <div className="loading-spinner" />
          <p>Loading users...</p>
        </div>
      ) : (
        <>
          <div className="user-table-container">
            <table className="user-table">
              <thead>
                <tr>
                  <th
                    className="sortable"
                    onClick={() => handleSort('email')}
                  >
                    User{getSortIndicator('email')}
                  </th>
                  <th>Role</th>
                  <th>Status</th>
                  <th>Subscription</th>
                  <th
                    className="sortable"
                    onClick={() => handleSort('distilleryName')}
                  >
                    Distillery{getSortIndicator('distilleryName')}
                  </th>
                  <th
                    className="sortable"
                    onClick={() => handleSort('lastLoginAt')}
                  >
                    Last Login{getSortIndicator('lastLoginAt')}
                  </th>
                  <th
                    className="sortable"
                    onClick={() => handleSort('createdAt')}
                  >
                    Created{getSortIndicator('createdAt')}
                  </th>
                </tr>
              </thead>
              <tbody>
                {users.map(user => (
                  <tr
                    key={user.id}
                    className={`user-row ${selectedUser?.id === user.id ? 'selected' : ''}`}
                    onClick={() => handleSelectUser(user.id)}
                  >
                    <td className="user-cell">
                      <div className="user-info">
                        <span className="user-name">
                          {user.firstName} {user.lastName}
                        </span>
                        <span className="user-email">{user.email}</span>
                      </div>
                    </td>
                    <td>
                      <span className={`badge ${getRoleBadgeClass(user.role)}`}>
                        {formatUserRole(user.role)}
                      </span>
                    </td>
                    <td>
                      <span className={`badge ${getStatusBadgeClass(user.status)}`}>
                        {formatUserStatus(user.status)}
                      </span>
                    </td>
                    <td>
                      <span className={`badge ${getSubscriptionBadgeClass(user.subscriptionStatus)}`}>
                        {user.subscriptionTier || user.subscriptionStatus}
                      </span>
                    </td>
                    <td className="distillery-cell">
                      {user.distilleryName || '-'}
                    </td>
                    <td className="date-cell">
                      {formatRelativeTime(user.lastLoginAt)}
                    </td>
                    <td className="date-cell">
                      {new Date(user.createdAt).toLocaleDateString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {users.length === 0 && (
            <div className="empty-state">
              <p>No users found matching your criteria.</p>
            </div>
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="pagination">
              <button
                type="button"
                className="btn btn-secondary btn-small"
                disabled={filters.page <= 1}
                onClick={() => handlePageChange(filters.page - 1)}
              >
                Previous
              </button>
              <span className="page-info">
                Page {filters.page} of {totalPages}
              </span>
              <button
                type="button"
                className="btn btn-secondary btn-small"
                disabled={filters.page >= totalPages}
                onClick={() => handlePageChange(filters.page + 1)}
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
    </div>
  )
}

export default UserListPanel
