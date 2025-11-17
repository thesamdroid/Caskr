import { useEffect, useState } from 'react';
import { useAppDispatch, useAppSelector } from '../hooks';
import {
  fetchOrders,
  fetchOutstandingTasks,
  assignTask,
  completeTask
} from '../features/ordersSlice';
import { fetchStatuses } from '../features/statusSlice';
import { fetchUsers } from '../features/usersSlice';

export default function DashboardPage() {
  const dispatch = useAppDispatch();
  const orders = useAppSelector(state => state.orders.items);
  const statuses = useAppSelector(state => state.statuses.items);
  const tasks = useAppSelector(state => state.orders.outstandingTasks);
  const users = useAppSelector(state => state.users.items);
  
  const [hoveredTask, setHoveredTask] = useState<number | null>(null);
  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);

  useEffect(() => {
    dispatch(fetchOrders()).then(action => {
      if (fetchOrders.fulfilled.match(action)) {
        action.payload.forEach(order => dispatch(fetchOutstandingTasks(order.id)));
      }
    });
    dispatch(fetchStatuses());
    dispatch(fetchUsers());
  }, [dispatch]);

  const getStatusName = (id: number) =>
    statuses.find(s => s.id === id)?.name || 'Unknown';

  const getStatusColor = (id: number) => {
    const name = getStatusName(id).toLowerCase();
    if (name.includes('complete')) return 'completed';
    if (name.includes('progress')) return 'in-progress';
    if (name.includes('pending')) return 'pending';
    return 'default';
  };

  const handleTaskAssignment = async (taskId: number, userId: number) => {
    try {
      await dispatch(assignTask({ taskId, userId })).unwrap();
      // Refresh tasks for the order
      const task = Object.values(tasks)
        .flat()
        .find(t => t.id === taskId);
      if (task) {
        dispatch(fetchOutstandingTasks(task.orderId));
      }
    } catch (error) {
      console.error('Failed to assign task:', error);
    }
  };

  const handleTaskCompletion = async (taskId: number) => {
    try {
      await dispatch(completeTask(taskId)).unwrap();
      // Refresh tasks for the order
      const task = Object.values(tasks)
        .flat()
        .find(t => t.id === taskId);
      if (task) {
        dispatch(fetchOutstandingTasks(task.orderId));
      }
    } catch (error) {
      console.error('Failed to complete task:', error);
    }
  };

  const getUserName = (userId: number | null) => {
    if (!userId) return null;
    return users.find(u => u.id === userId)?.name || 'Unknown User';
  };

  const calculateProgress = (orderId: number) => {
    const orderTasks = tasks[orderId] || [];
    if (orderTasks.length === 0) return 100;
    const completedTasks = orderTasks.filter(t => t.isComplete).length;
    return Math.round((completedTasks / orderTasks.length) * 100);
  };

  const getTaskStats = (orderId: number) => {
    const orderTasks = tasks[orderId] || [];
    const completed = orderTasks.filter(t => t.isComplete).length;
    const total = orderTasks.length;
    return { completed, total, remaining: total - completed };
  };

  // Calculate dashboard statistics
  const stats = {
    activeOrders: orders.length,
    completedOrders: orders.filter(o => getStatusName(o.statusId).toLowerCase().includes('complete')).length,
    totalTasks: Object.values(tasks).flat().length,
    completedTasks: Object.values(tasks).flat().filter(t => t.isComplete).length
  };

  const completionRate = stats.totalTasks > 0 
    ? Math.round((stats.completedTasks / stats.totalTasks) * 100) 
    : 0;

  return (
    <div className="dashboard-container">
      {/* Dashboard Header */}
      <div className="dashboard-header">
        <div className="header-content">
          <h1 className="page-title">Dashboard</h1>
          <p className="page-subtitle">Manage your orders and track progress</p>
        </div>
      </div>

      {/* Stats Overview */}
      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-icon orders">
            <span className="icon">📦</span>
          </div>
          <div className="stat-content">
            <p className="stat-label">Active Orders</p>
            <h3 className="stat-value">{stats.activeOrders}</h3>
            <div className="stat-change positive">
              <span className="change-icon">↗</span>
              <span>+12% from last month</span>
            </div>
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-icon completed">
            <span className="icon">✓</span>
          </div>
          <div className="stat-content">
            <p className="stat-label">Completed Orders</p>
            <h3 className="stat-value">{stats.completedOrders}</h3>
            <div className="stat-change neutral">
              <span className="change-icon">−</span>
              <span>No change</span>
            </div>
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-icon pending">
            <span className="icon">⏱</span>
          </div>
          <div className="stat-content">
            <p className="stat-label">Total Tasks</p>
            <h3 className="stat-value">{stats.totalTasks}</h3>
            <div className="stat-change negative">
              <span className="change-icon">↘</span>
              <span>{stats.totalTasks - stats.completedTasks} remaining</span>
            </div>
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-icon progress">
            <span className="icon">📊</span>
          </div>
          <div className="stat-content">
            <p className="stat-label">Overall Progress</p>
            <h3 className="stat-value">{completionRate}%</h3>
            <div className={`stat-change ${completionRate >= 75 ? 'positive' : completionRate >= 50 ? 'neutral' : 'negative'}`}>
              <span className="change-icon">{completionRate >= 75 ? '↗' : '−'}</span>
              <span>Task completion rate</span>
            </div>
          </div>
        </div>
      </div>

      {/* Orders Grid */}
      <div className="orders-section">
        <div className="section-header">
          <h2 className="section-title">Current Orders</h2>
          <p className="section-subtitle">Track and manage all active orders</p>
        </div>

        {orders.length === 0 ? (
          <div className="empty-state">
            <span className="empty-icon">📦</span>
            <h3>No orders yet</h3>
            <p>Create your first order to get started</p>
          </div>
        ) : (
          <div className="orders-grid">
            {orders.map(order => {
              const progress = calculateProgress(order.id);
              const taskStats = getTaskStats(order.id);
              const statusColor = getStatusColor(order.statusId);
              const orderTasks = tasks[order.id] || [];
              const isExpanded = selectedOrderId === order.id;

              return (
                <div 
                  key={order.id} 
                  className={`order-card ${isExpanded ? 'expanded' : ''}`}
                >
                  {/* Order Header */}
                  <div className="order-header">
                    <div className="order-title-section">
                      <h3 className="order-title">{order.name}</h3>
                      <span className={`status-badge ${statusColor}`}>
                        {getStatusName(order.statusId)}
                      </span>
                    </div>
                    <button
                      onClick={() => setSelectedOrderId(isExpanded ? null : order.id)}
                      className="expand-button"
                      aria-label={isExpanded ? 'Collapse' : 'Expand'}
                    >
                      <span className={`expand-icon ${isExpanded ? 'rotated' : ''}`}>›</span>
                    </button>
                  </div>

                  {/* Progress Bar */}
                  <div className="progress-section">
                    <div className="progress-header">
                      <span className="progress-label">Progress</span>
                      <span className="progress-percentage">{progress}%</span>
                    </div>
                    <div className="progress-bar">
                      <div 
                        className="progress-fill"
                        style={{ width: `${progress}%` }}
                      />
                    </div>
                  </div>

                  {/* Task Summary */}
                  <div className="task-summary">
                    <div className="task-stat">
                      <span className="task-stat-icon completed">✓</span>
                      <span className="task-stat-value">{taskStats.completed}</span>
                      <span className="task-stat-label">Completed</span>
                    </div>
                    <div className="task-stat-divider" />
                    <div className="task-stat">
                      <span className="task-stat-icon pending">⏱</span>
                      <span className="task-stat-value">{taskStats.remaining}</span>
                      <span className="task-stat-label">Remaining</span>
                    </div>
                    <div className="task-stat-divider" />
                    <div className="task-stat">
                      <span className="task-stat-icon total">#</span>
                      <span className="task-stat-value">{taskStats.total}</span>
                      <span className="task-stat-label">Total</span>
                    </div>
                  </div>

                  {/* Expanded Task List */}
                  {isExpanded && (
                    <div className="tasks-list">
                      <div className="tasks-header">
                        <h4>Tasks</h4>
                      </div>
                      {orderTasks.length === 0 ? (
                        <p className="tasks-empty">No tasks for this order</p>
                      ) : (
                        <div className="tasks-items">
                          {orderTasks.map(task => (
                            <div
                              key={task.id}
                              className={`task-item ${task.isComplete ? 'completed' : ''}`}
                              onMouseEnter={() => setHoveredTask(task.id)}
                              onMouseLeave={() => setHoveredTask(null)}
                            >
                              {/* Task Checkbox */}
                              <div className="task-checkbox-container">
                                <input
                                  type="checkbox"
                                  checked={task.isComplete}
                                  onChange={() => handleTaskCompletion(task.id)}
                                  className="task-checkbox"
                                  aria-label={`Mark ${task.name} as ${task.isComplete ? 'incomplete' : 'complete'}`}
                                />
                              </div>

                              {/* Task Info */}
                              <div className="task-info">
                                <span className="task-name">{task.name}</span>
                                {task.assigneeId && (
                                  <span className="task-assignee">
                                    <span className="assignee-icon">👤</span>
                                    {getUserName(task.assigneeId)}
                                  </span>
                                )}
                              </div>

                              {/* Task Actions - Show on Hover */}
                              {hoveredTask === task.id && !task.isComplete && (
                                <div className="task-actions">
                                  {task.assigneeId ? (
                                    <button
                                      onClick={() => handleTaskAssignment(task.id, null!)}
                                      className="task-action-button unassign"
                                      title="Unassign task"
                                    >
                                      Unassign
                                    </button>
                                  ) : (
                                    <div className="assignee-dropdown">
                                      <select
                                        onChange={(e) => handleTaskAssignment(task.id, parseInt(e.target.value))}
                                        className="assignee-select"
                                        defaultValue=""
                                      >
                                        <option value="" disabled>
                                          Assign to...
                                        </option>
                                        {users.map(user => (
                                          <option key={user.id} value={user.id}>
                                            {user.name}
                                          </option>
                                        ))}
                                      </select>
                                    </div>
                                  )}
                                </div>
                              )}
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
