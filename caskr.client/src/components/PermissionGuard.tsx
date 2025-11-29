import { Navigate, useLocation } from 'react-router-dom'
import { useAppSelector } from '../hooks'
import { userHasPermission } from '../features/authSlice'

interface PermissionGuardProps {
  children: JSX.Element
  requiredPermission: string
}

export default function PermissionGuard({ children, requiredPermission }: PermissionGuardProps) {
  const user = useAppSelector(state => state.auth.user)
  const isAuthenticated = useAppSelector(state => state.auth.isAuthenticated)
  const location = useLocation()

  if (!isAuthenticated || !user) {
    return <Navigate to='/login' state={{ from: location }} replace />
  }

  if (!userHasPermission(user, requiredPermission)) {
    return <Navigate to='/' replace />
  }

  return children
}
