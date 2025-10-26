import { createAsyncThunk, createSlice } from '@reduxjs/toolkit';
import { authorizedFetch } from '../api/authorizedFetch';

export interface Order {
  id: number;
  name: string;
  statusId: number;
  ownerId: number;
  spiritTypeId: number;
  quantity: number;
  mashBillId: number;
}

export interface Task {
  id: number;
  name: string;
  orderId: number;
  assigneeId: number | null;
  isComplete: boolean;
  dueDate?: string;
}

export interface NewOrder {
  name: string;
  statusId: number;
  ownerId: number;
  spiritTypeId: number;
  quantity: number;
  mashBillId: number;
}

// Fetch all orders
export const fetchOrders = createAsyncThunk('orders/fetchOrders', async () => {
  const response = await authorizedFetch('api/orders');
  if (!response.ok) throw new Error('Failed to fetch orders');
  return (await response.json()) as Order[];
});

// Add a new order
export const addOrder = createAsyncThunk('orders/addOrder', async (order: NewOrder) => {
  const response = await authorizedFetch('api/orders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(order)
  });
  if (!response.ok) throw new Error('Failed to add order');
  return (await response.json()) as Order;
});

// Update an existing order
export const updateOrder = createAsyncThunk('orders/updateOrder', async (order: Order) => {
  const response = await authorizedFetch(`api/orders/${order.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(order)
  });
  if (!response.ok) throw new Error('Failed to update order');
  return (await response.json()) as Order;
});

// Delete an order
export const deleteOrder = createAsyncThunk('orders/deleteOrder', async (id: number) => {
  const response = await authorizedFetch(`api/orders/${id}`, {
    method: 'DELETE'
  });
  if (!response.ok) throw new Error('Failed to delete order');
  return id;
});

// Fetch outstanding tasks for an order
export const fetchOutstandingTasks = createAsyncThunk(
  'orders/fetchOutstandingTasks',
  async (orderId: number) => {
    const response = await authorizedFetch(`api/orders/${orderId}/tasks`);
    if (!response.ok) throw new Error('Failed to fetch tasks');
    const tasks = (await response.json()) as Task[];
    return { orderId, tasks };
  }
);

// Assign a task to a user
export const assignTask = createAsyncThunk(
  'orders/assignTask',
  async ({ taskId, userId }: { taskId: number; userId: number }) => {
    const response = await authorizedFetch(`api/tasks/${taskId}/assign`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ assigneeId: userId })
    });
    if (!response.ok) throw new Error('Failed to assign task');
    return (await response.json()) as Task;
  }
);

// Mark a task as complete
export const completeTask = createAsyncThunk(
  'orders/completeTask',
  async (taskId: number) => {
    const response = await authorizedFetch(`api/tasks/${taskId}/complete`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ isComplete: true })
    });
    if (!response.ok) throw new Error('Failed to complete task');
    return (await response.json()) as Task;
  }
);

interface OrdersState {
  items: Order[];
  outstandingTasks: Record<number, Task[]>;
  loading: boolean;
  error: string | null;
}

const initialState: OrdersState = {
  items: [],
  outstandingTasks: {},
  loading: false,
  error: null
};

const ordersSlice = createSlice({
  name: 'orders',
  initialState,
  reducers: {},
  extraReducers: builder => {
    // Fetch orders
    builder.addCase(fetchOrders.pending, state => {
      state.loading = true;
      state.error = null;
    });
    builder.addCase(fetchOrders.fulfilled, (state, action) => {
      state.items = action.payload;
      state.loading = false;
    });
    builder.addCase(fetchOrders.rejected, (state, action) => {
      state.loading = false;
      state.error = action.error.message || 'Failed to fetch orders';
    });

    // Add order
    builder.addCase(addOrder.fulfilled, (state, action) => {
      state.items.push(action.payload);
    });

    // Update order
    builder.addCase(updateOrder.fulfilled, (state, action) => {
      const index = state.items.findIndex(o => o.id === action.payload.id);
      if (index !== -1) {
        state.items[index] = action.payload;
      }
    });

    // Delete order
    builder.addCase(deleteOrder.fulfilled, (state, action) => {
      state.items = state.items.filter(o => o.id !== action.payload);
      delete state.outstandingTasks[action.payload];
    });

    // Fetch outstanding tasks
    builder.addCase(fetchOutstandingTasks.fulfilled, (state, action) => {
      state.outstandingTasks[action.payload.orderId] = action.payload.tasks;
    });

    // Assign task
    builder.addCase(assignTask.fulfilled, (state, action) => {
      const task = action.payload;
      const tasks = state.outstandingTasks[task.orderId];
      if (tasks) {
        const index = tasks.findIndex(t => t.id === task.id);
        if (index !== -1) {
          tasks[index] = task;
        }
      }
    });

    // Complete task
    builder.addCase(completeTask.fulfilled, (state, action) => {
      const task = action.payload;
      const tasks = state.outstandingTasks[task.orderId];
      if (tasks) {
        const index = tasks.findIndex(t => t.id === task.id);
        if (index !== -1) {
          tasks[index] = task;
        }
      }
    });
  }
});

export default ordersSlice.reducer;
