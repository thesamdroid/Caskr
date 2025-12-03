import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import styles from './MobileBarrels.module.css'

interface Barrel {
  id: string
  status: 'aging' | 'ready' | 'bottled'
  spiritType: string
  age: string
  warehouse: string
}

const mockBarrels: Barrel[] = [
  { id: 'B-2024-001', status: 'aging', spiritType: 'Bourbon', age: '2y 3m', warehouse: 'Warehouse A' },
  { id: 'B-2024-002', status: 'ready', spiritType: 'Rye', age: '4y 1m', warehouse: 'Warehouse A' },
  { id: 'B-2024-003', status: 'aging', spiritType: 'Bourbon', age: '1y 8m', warehouse: 'Warehouse B' },
  { id: 'B-2024-004', status: 'bottled', spiritType: 'Bourbon', age: '5y', warehouse: 'Warehouse A' },
  { id: 'B-2024-005', status: 'aging', spiritType: 'Wheat', age: '3y 2m', warehouse: 'Warehouse B' }
]

/**
 * Mobile barrels list page
 */
export function MobileBarrels() {
  const navigate = useNavigate()
  const [searchQuery, setSearchQuery] = useState('')
  const [filterStatus, setFilterStatus] = useState<string>('all')

  const filteredBarrels = mockBarrels.filter(barrel => {
    const matchesSearch = barrel.id.toLowerCase().includes(searchQuery.toLowerCase()) ||
                         barrel.spiritType.toLowerCase().includes(searchQuery.toLowerCase())
    const matchesFilter = filterStatus === 'all' || barrel.status === filterStatus
    return matchesSearch && matchesFilter
  })

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'aging': return styles.statusAging
      case 'ready': return styles.statusReady
      case 'bottled': return styles.statusBottled
      default: return ''
    }
  }

  return (
    <div className={styles.barrelsPage}>
      {/* Search bar */}
      <div className={styles.searchBar}>
        <input
          type="search"
          placeholder="Search barrels..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className={styles.searchInput}
        />
      </div>

      {/* Filter chips */}
      <div className={styles.filters}>
        {['all', 'aging', 'ready', 'bottled'].map(status => (
          <button
            key={status}
            className={`${styles.filterChip} ${filterStatus === status ? styles.active : ''}`}
            onClick={() => setFilterStatus(status)}
          >
            {status === 'all' ? 'All' : status.charAt(0).toUpperCase() + status.slice(1)}
          </button>
        ))}
      </div>

      {/* Barrel list */}
      <div className={styles.barrelList}>
        {filteredBarrels.map(barrel => (
          <button
            key={barrel.id}
            className={styles.barrelCard}
            onClick={() => navigate(`/barrels/${barrel.id}`)}
          >
            <div className={styles.barrelHeader}>
              <span className={styles.barrelId}>{barrel.id}</span>
              <span className={`${styles.status} ${getStatusColor(barrel.status)}`}>
                {barrel.status}
              </span>
            </div>
            <div className={styles.barrelDetails}>
              <span className={styles.detailItem}>{barrel.spiritType}</span>
              <span className={styles.detailItem}>{barrel.age}</span>
              <span className={styles.detailItem}>{barrel.warehouse}</span>
            </div>
          </button>
        ))}
      </div>
    </div>
  )
}

export default MobileBarrels
