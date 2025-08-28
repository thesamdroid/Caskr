import { useState } from 'react'
import { useNavigate } from 'react-router-dom'

function LoginPage() {
  const [email, setEmail] = useState('')
  const [message, setMessage] = useState('')
  const navigate = useNavigate()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const response = await fetch('api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email })
    })
    if (!response.ok) {
      setMessage('Login failed')
      return
    }
    const data = await response.json()
    localStorage.setItem('token', data.token)
    navigate('/landing')
  }

  return (
    <div>
      <h1>Login</h1>
      <form onSubmit={handleSubmit}>
        <input value={email} onChange={e => setEmail(e.target.value)} placeholder='Email' />
        <button type='submit'>Login</button>
      </form>
      {message && <p>{message}</p>}
    </div>
  )
}

export default LoginPage
