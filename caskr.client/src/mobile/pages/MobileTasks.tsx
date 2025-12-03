import styles from './MobileTasks.module.css'

interface Task {
  id: string
  title: string
  description: string
  priority: 'high' | 'medium' | 'low'
  completed: boolean
}

const mockTasks: Task[] = [
  { id: '1', title: 'Fill barrel B-2024-045', description: 'Warehouse A, Rick 3', priority: 'high', completed: false },
  { id: '2', title: 'Gauge record for B-2024-032', description: 'Due by end of day', priority: 'high', completed: false },
  { id: '3', title: 'Inventory check - Rick 5', description: 'Monthly audit', priority: 'medium', completed: false },
  { id: '4', title: 'Move barrels to Warehouse B', description: '12 barrels total', priority: 'medium', completed: true },
  { id: '5', title: 'Update labels', description: 'New batch labels', priority: 'low', completed: true }
]

/**
 * Mobile tasks page
 */
export function MobileTasks() {
  const pendingTasks = mockTasks.filter(t => !t.completed)
  const completedTasks = mockTasks.filter(t => t.completed)

  return (
    <div className={styles.tasksPage}>
      <section className={styles.section}>
        <h2 className={styles.sectionTitle}>
          Pending ({pendingTasks.length})
        </h2>
        <div className={styles.taskList}>
          {pendingTasks.map(task => (
            <div key={task.id} className={styles.taskItem}>
              <div className={`${styles.priority} ${styles[task.priority]}`} />
              <div className={styles.checkbox}>
                <input type="checkbox" id={task.id} />
                <label htmlFor={task.id} />
              </div>
              <div className={styles.taskContent}>
                <span className={styles.taskTitle}>{task.title}</span>
                <span className={styles.taskDescription}>{task.description}</span>
              </div>
            </div>
          ))}
        </div>
      </section>

      <section className={styles.section}>
        <h2 className={styles.sectionTitle}>
          Completed ({completedTasks.length})
        </h2>
        <div className={styles.taskList}>
          {completedTasks.map(task => (
            <div key={task.id} className={`${styles.taskItem} ${styles.completed}`}>
              <div className={`${styles.priority} ${styles[task.priority]}`} />
              <div className={styles.checkbox}>
                <input type="checkbox" id={task.id} defaultChecked />
                <label htmlFor={task.id} />
              </div>
              <div className={styles.taskContent}>
                <span className={styles.taskTitle}>{task.title}</span>
                <span className={styles.taskDescription}>{task.description}</span>
              </div>
            </div>
          ))}
        </div>
      </section>
    </div>
  )
}

export default MobileTasks
