import styles from './MobileDashboard.module.css'

/**
 * Mobile dashboard page with quick stats and recent activity
 */
export function MobileDashboard() {
  return (
    <div className={styles.dashboard}>
      <section className={styles.quickStats}>
        <h2 className={styles.sectionTitle}>Quick Stats</h2>
        <div className={styles.statsGrid}>
          <div className={styles.statCard}>
            <span className={styles.statValue}>142</span>
            <span className={styles.statLabel}>Active Barrels</span>
          </div>
          <div className={styles.statCard}>
            <span className={styles.statValue}>8</span>
            <span className={styles.statLabel}>Tasks Today</span>
          </div>
          <div className={styles.statCard}>
            <span className={styles.statValue}>3</span>
            <span className={styles.statLabel}>Open Orders</span>
          </div>
          <div className={styles.statCard}>
            <span className={styles.statValue}>24</span>
            <span className={styles.statLabel}>Ready to Bottle</span>
          </div>
        </div>
      </section>

      <section className={styles.recentActivity}>
        <h2 className={styles.sectionTitle}>Recent Activity</h2>
        <div className={styles.activityList}>
          <div className={styles.activityItem}>
            <div className={styles.activityIcon}>📦</div>
            <div className={styles.activityContent}>
              <span className={styles.activityTitle}>Barrel B-2024-001 filled</span>
              <span className={styles.activityTime}>2 hours ago</span>
            </div>
          </div>
          <div className={styles.activityItem}>
            <div className={styles.activityIcon}>📋</div>
            <div className={styles.activityContent}>
              <span className={styles.activityTitle}>Inventory check completed</span>
              <span className={styles.activityTime}>4 hours ago</span>
            </div>
          </div>
          <div className={styles.activityItem}>
            <div className={styles.activityIcon}>🚚</div>
            <div className={styles.activityContent}>
              <span className={styles.activityTitle}>Transfer T-001 received</span>
              <span className={styles.activityTime}>Yesterday</span>
            </div>
          </div>
        </div>
      </section>
    </div>
  )
}

export default MobileDashboard
