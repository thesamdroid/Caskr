import { useParams } from 'react-router-dom'
import styles from './MobileBarrelDetail.module.css'

/**
 * Mobile barrel detail page
 */
export function MobileBarrelDetail() {
  const { id } = useParams<{ id: string }>()

  // Mock data - would normally fetch from API
  const barrel = {
    id: id || 'Unknown',
    status: 'aging',
    spiritType: 'Bourbon',
    mashBill: 'Corn 75%, Rye 21%, Malt 4%',
    age: '2 years, 3 months',
    entryDate: '2022-09-15',
    entryProof: '125',
    currentProof: '118',
    volume: '53 gallons',
    warehouse: 'Warehouse A',
    rick: 'Rick 3, Position 24',
    cooperage: 'Independent Stave Company',
    charLevel: '4'
  }

  return (
    <div className={styles.detailPage}>
      {/* Header section */}
      <div className={styles.header}>
        <span className={styles.barrelId}>{barrel.id}</span>
        <span className={`${styles.status} ${styles[barrel.status]}`}>
          {barrel.status}
        </span>
      </div>

      {/* Info sections */}
      <div className={styles.sections}>
        <section className={styles.section}>
          <h3 className={styles.sectionTitle}>Spirit Information</h3>
          <div className={styles.infoGrid}>
            <div className={styles.infoItem}>
              <span className={styles.label}>Type</span>
              <span className={styles.value}>{barrel.spiritType}</span>
            </div>
            <div className={styles.infoItem}>
              <span className={styles.label}>Mash Bill</span>
              <span className={styles.value}>{barrel.mashBill}</span>
            </div>
            <div className={styles.infoItem}>
              <span className={styles.label}>Age</span>
              <span className={styles.value}>{barrel.age}</span>
            </div>
          </div>
        </section>

        <section className={styles.section}>
          <h3 className={styles.sectionTitle}>Measurements</h3>
          <div className={styles.infoGrid}>
            <div className={styles.infoItem}>
              <span className={styles.label}>Entry Proof</span>
              <span className={styles.value}>{barrel.entryProof}</span>
            </div>
            <div className={styles.infoItem}>
              <span className={styles.label}>Current Proof</span>
              <span className={styles.value}>{barrel.currentProof}</span>
            </div>
            <div className={styles.infoItem}>
              <span className={styles.label}>Volume</span>
              <span className={styles.value}>{barrel.volume}</span>
            </div>
          </div>
        </section>

        <section className={styles.section}>
          <h3 className={styles.sectionTitle}>Location</h3>
          <div className={styles.infoGrid}>
            <div className={styles.infoItem}>
              <span className={styles.label}>Warehouse</span>
              <span className={styles.value}>{barrel.warehouse}</span>
            </div>
            <div className={styles.infoItem}>
              <span className={styles.label}>Position</span>
              <span className={styles.value}>{barrel.rick}</span>
            </div>
          </div>
        </section>

        <section className={styles.section}>
          <h3 className={styles.sectionTitle}>Barrel Details</h3>
          <div className={styles.infoGrid}>
            <div className={styles.infoItem}>
              <span className={styles.label}>Cooperage</span>
              <span className={styles.value}>{barrel.cooperage}</span>
            </div>
            <div className={styles.infoItem}>
              <span className={styles.label}>Char Level</span>
              <span className={styles.value}>{barrel.charLevel}</span>
            </div>
            <div className={styles.infoItem}>
              <span className={styles.label}>Entry Date</span>
              <span className={styles.value}>{barrel.entryDate}</span>
            </div>
          </div>
        </section>
      </div>

      {/* Action buttons */}
      <div className={styles.actions}>
        <button className={styles.primaryAction}>Record Gauge</button>
        <button className={styles.secondaryAction}>View History</button>
      </div>
    </div>
  )
}

export default MobileBarrelDetail
