import { configureStore } from '@reduxjs/toolkit'
import ordersReducer from './features/ordersSlice'
import statusReducer from './features/statusSlice'

export const store = configureStore({
  reducer: {
    orders: ordersReducer,
    statuses: statusReducer
  }
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch

