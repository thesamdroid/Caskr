/**
 * TaskFormSheet Component
 *
 * Bottom sheet modal for creating or editing tasks.
 */

import React, {
  useCallback,
  useEffect,
  useRef,
  useState,
  memo,
  FormEvent,
} from 'react'
import { MobileTask, TaskPriority, CreateTaskData, UpdateTaskData } from '../../types'
import styles from './TaskFormSheet.module.css'

export interface TaskFormSheetProps {
  isOpen: boolean
  onClose: () => void
  onSave: (data: CreateTaskData | UpdateTaskData) => Promise<void>
  editingTask?: MobileTask | null
  orders?: { id: number; name: string }[]
  assignees?: { id: number; name: string }[]
}

interface FormState {
  title: string
  description: string
  dueDate: string
  dueTime: string
  priority: TaskPriority
  orderId: string
  assigneeId: string
}

const initialFormState: FormState = {
  title: '',
  description: '',
  dueDate: new Date().toISOString().split('T')[0],
  dueTime: '',
  priority: 'medium',
  orderId: '',
  assigneeId: '',
}

function TaskFormSheetComponent({
  isOpen,
  onClose,
  onSave,
  editingTask,
  orders = [],
  assignees = [],
}: TaskFormSheetProps) {
  const sheetRef = useRef<HTMLDivElement>(null)
  const titleInputRef = useRef<HTMLInputElement>(null)
  const [formState, setFormState] = useState<FormState>(initialFormState)
  const [isSaving, setIsSaving] = useState(false)
  const [errors, setErrors] = useState<Partial<Record<keyof FormState, string>>>({})
  const [translateY, setTranslateY] = useState(0)
  const [isDragging, setIsDragging] = useState(false)
  const touchStartY = useRef(0)
  const currentTranslateY = useRef(0)

  // Initialize form state when editing task changes
  useEffect(() => {
    if (editingTask) {
      setFormState({
        title: editingTask.title,
        description: editingTask.description || '',
        dueDate: editingTask.dueDate,
        dueTime: editingTask.dueTime || '',
        priority: editingTask.priority,
        orderId: editingTask.orderId?.toString() || '',
        assigneeId: editingTask.assigneeId?.toString() || '',
      })
    } else {
      setFormState(initialFormState)
    }
    setErrors({})
  }, [editingTask, isOpen])

  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden'
      // Focus title input after animation
      setTimeout(() => {
        titleInputRef.current?.focus()
      }, 300)
    } else {
      document.body.style.overflow = ''
    }

    return () => {
      document.body.style.overflow = ''
    }
  }, [isOpen])

  const handleBackdropClick = useCallback(() => {
    if (!isSaving) {
      onClose()
    }
  }, [isSaving, onClose])

  const handleDragStart = useCallback((e: React.TouchEvent) => {
    touchStartY.current = e.touches[0].clientY
    currentTranslateY.current = 0
    setIsDragging(true)
  }, [])

  const handleDragMove = useCallback(
    (e: React.TouchEvent) => {
      if (!isDragging) return

      const deltaY = e.touches[0].clientY - touchStartY.current

      if (deltaY > 0) {
        currentTranslateY.current = deltaY
        setTranslateY(deltaY)
      }
    },
    [isDragging]
  )

  const handleDragEnd = useCallback(() => {
    setIsDragging(false)

    const sheetHeight = sheetRef.current?.offsetHeight || 400

    if (currentTranslateY.current > sheetHeight * 0.3 && !isSaving) {
      onClose()
    }

    setTranslateY(0)
    currentTranslateY.current = 0
  }, [isSaving, onClose])

  const handleInputChange = useCallback(
    (field: keyof FormState) =>
      (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
        setFormState((prev) => ({
          ...prev,
          [field]: e.target.value,
        }))
        // Clear error for this field
        setErrors((prev) => ({ ...prev, [field]: undefined }))
      },
    []
  )

  const handlePriorityChange = useCallback((priority: TaskPriority) => {
    setFormState((prev) => ({
      ...prev,
      priority,
    }))
  }, [])

  const validate = (): boolean => {
    const newErrors: Partial<Record<keyof FormState, string>> = {}

    if (!formState.title.trim()) {
      newErrors.title = 'Title is required'
    }

    if (!formState.dueDate) {
      newErrors.dueDate = 'Due date is required'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = useCallback(
    async (e: FormEvent) => {
      e.preventDefault()

      if (!validate()) return

      setIsSaving(true)

      try {
        const data: CreateTaskData | UpdateTaskData = {
          title: formState.title.trim(),
          description: formState.description.trim() || undefined,
          dueDate: formState.dueDate,
          dueTime: formState.dueTime || undefined,
          priority: formState.priority,
          orderId: formState.orderId ? parseInt(formState.orderId, 10) : undefined,
          assigneeId: formState.assigneeId
            ? parseInt(formState.assigneeId, 10)
            : undefined,
        }

        await onSave(data)
        onClose()
      } catch (error) {
        console.error('Failed to save task:', error)
        setErrors({ title: 'Failed to save task. Please try again.' })
      } finally {
        setIsSaving(false)
      }
    },
    [formState, onSave, onClose]
  )

  return (
    <div
      className={`${styles.overlay} ${isOpen ? styles.open : ''}`}
      onClick={handleBackdropClick}
    >
      <div
        ref={sheetRef}
        className={`${styles.sheet} ${isDragging ? styles.dragging : ''}`}
        style={{ transform: `translateY(${translateY}px)` }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Drag handle */}
        <div
          className={styles.dragHandle}
          onTouchStart={handleDragStart}
          onTouchMove={handleDragMove}
          onTouchEnd={handleDragEnd}
          onTouchCancel={handleDragEnd}
        >
          <div className={styles.handleBar} />
        </div>

        {/* Header */}
        <div className={styles.header}>
          <button
            className={styles.cancelButton}
            onClick={onClose}
            disabled={isSaving}
          >
            Cancel
          </button>
          <h2 className={styles.headerTitle}>
            {editingTask ? 'Edit Task' : 'New Task'}
          </h2>
          <button
            className={styles.saveButton}
            onClick={handleSubmit}
            disabled={isSaving || !formState.title.trim()}
          >
            {isSaving ? 'Saving...' : 'Save'}
          </button>
        </div>

        {/* Form */}
        <form className={styles.form} onSubmit={handleSubmit}>
          {/* Title */}
          <div className={styles.field}>
            <label className={styles.label} htmlFor="title">
              Title <span className={styles.required}>*</span>
            </label>
            <input
              ref={titleInputRef}
              id="title"
              type="text"
              className={`${styles.input} ${errors.title ? styles.inputError : ''}`}
              placeholder="Enter task title"
              value={formState.title}
              onChange={handleInputChange('title')}
              disabled={isSaving}
            />
            {errors.title && (
              <span className={styles.errorMessage}>{errors.title}</span>
            )}
          </div>

          {/* Description */}
          <div className={styles.field}>
            <label className={styles.label} htmlFor="description">
              Description
            </label>
            <textarea
              id="description"
              className={styles.textarea}
              placeholder="Add details..."
              rows={3}
              value={formState.description}
              onChange={handleInputChange('description')}
              disabled={isSaving}
            />
          </div>

          {/* Due date and time */}
          <div className={styles.fieldRow}>
            <div className={styles.field}>
              <label className={styles.label} htmlFor="dueDate">
                Due Date <span className={styles.required}>*</span>
              </label>
              <input
                id="dueDate"
                type="date"
                className={`${styles.input} ${errors.dueDate ? styles.inputError : ''}`}
                value={formState.dueDate}
                onChange={handleInputChange('dueDate')}
                disabled={isSaving}
              />
              {errors.dueDate && (
                <span className={styles.errorMessage}>{errors.dueDate}</span>
              )}
            </div>

            <div className={styles.field}>
              <label className={styles.label} htmlFor="dueTime">
                Time
              </label>
              <input
                id="dueTime"
                type="time"
                className={styles.input}
                value={formState.dueTime}
                onChange={handleInputChange('dueTime')}
                disabled={isSaving}
              />
            </div>
          </div>

          {/* Priority */}
          <div className={styles.field}>
            <label className={styles.label}>Priority</label>
            <div className={styles.priorityButtons}>
              {(['low', 'medium', 'high'] as TaskPriority[]).map((p) => (
                <button
                  key={p}
                  type="button"
                  className={`${styles.priorityButton} ${styles[p]} ${
                    formState.priority === p ? styles.selected : ''
                  }`}
                  onClick={() => handlePriorityChange(p)}
                  disabled={isSaving}
                >
                  {p.charAt(0).toUpperCase() + p.slice(1)}
                </button>
              ))}
            </div>
          </div>

          {/* Order */}
          {orders.length > 0 && (
            <div className={styles.field}>
              <label className={styles.label} htmlFor="orderId">
                Related Order
              </label>
              <select
                id="orderId"
                className={styles.select}
                value={formState.orderId}
                onChange={handleInputChange('orderId')}
                disabled={isSaving}
              >
                <option value="">None</option>
                {orders.map((order) => (
                  <option key={order.id} value={order.id}>
                    {order.name}
                  </option>
                ))}
              </select>
            </div>
          )}

          {/* Assignee */}
          {assignees.length > 0 && (
            <div className={styles.field}>
              <label className={styles.label} htmlFor="assigneeId">
                Assign To
              </label>
              <select
                id="assigneeId"
                className={styles.select}
                value={formState.assigneeId}
                onChange={handleInputChange('assigneeId')}
                disabled={isSaving}
              >
                <option value="">Unassigned</option>
                {assignees.map((assignee) => (
                  <option key={assignee.id} value={assignee.id}>
                    {assignee.name}
                  </option>
                ))}
              </select>
            </div>
          )}
        </form>
      </div>
    </div>
  )
}

export const TaskFormSheet = memo(TaskFormSheetComponent)
