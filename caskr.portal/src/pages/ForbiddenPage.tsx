import { Link } from 'react-router-dom'

function ForbiddenPage() {
  return (
    <div className="forbidden-page">
      <div className="forbidden-container">
        <div className="forbidden-icon">&#x1F512;</div>
        <h1>Access Denied</h1>
        <p>
          You don't have permission to access this page. This area is restricted
          to authorized administrators only.
        </p>
        <p>
          If you believe this is an error, please contact your system administrator.
        </p>
        <Link to="/dashboard" className="btn btn-primary">
          Return to Dashboard
        </Link>
      </div>
    </div>
  )
}

export default ForbiddenPage
