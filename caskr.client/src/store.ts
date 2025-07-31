import { configureStore } from '@reduxjs/toolkit'
import ordersReducer from './features/ordersSlice'
import statusReducer from './features/statusSlice'
import productsReducer from './features/productsSlice'
import usersReducer from './features/usersSlice'
import userTypesReducer from './features/userTypesSlice'

export const store = configureStore({
  reducer: {
    orders: ordersReducer,
    statuses: statusReducer,
    products: productsReducer,
    users: usersReducer,
    userTypes: userTypesReducer
  }
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch

