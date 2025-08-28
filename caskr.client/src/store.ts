import { configureStore } from '@reduxjs/toolkit'
import ordersReducer from './features/ordersSlice'
import statusReducer from './features/statusSlice'
import productsReducer from './features/productsSlice'
import usersReducer from './features/usersSlice'
import userTypesReducer from './features/userTypesSlice'
import spiritTypesReducer from './features/spiritTypesSlice'
import mashBillsReducer from './features/mashBillsSlice'
import barrelsReducer from './features/barrelsSlice'

export const store = configureStore({
  reducer: {
    orders: ordersReducer,
    statuses: statusReducer,
    products: productsReducer,
    users: usersReducer,
    userTypes: userTypesReducer,
    spiritTypes: spiritTypesReducer,
    mashBills: mashBillsReducer,
    barrels: barrelsReducer
  }
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch

