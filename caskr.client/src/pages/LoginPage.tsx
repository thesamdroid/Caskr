import { useState } from 'react'
import { useNavigate } from 'react-router-dom'

function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [message, setMessage] = useState('')
  const navigate = useNavigate()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const response = await fetch('api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    })
    if (!response.ok) {
      setMessage('Login failed')
      return
    }
    const data = await response.json()
    localStorage.setItem('token', data.token)
    setPassword('')
    navigate('/landing')
  }

  return (
    <section className='content-section'>
      <div className='section-header'>
        <h2 className='section-title'>Login</h2>
      </div>
      <form onSubmit={handleSubmit}>
        <input value={email} onChange={e => setEmail(e.target.value)} placeholder='Email' />
        <input
          type='password'
          value={password}
          onChange={e => setPassword(e.target.value)}
          placeholder='Password'
        />
        <button type='submit'>Login</button>
      </form>
      {message && <p>{message}</p>}
    </section>
  )
}

export default LoginPage
