import { useState } from 'react'
import { login } from '../lib/auth'

interface Props {
    onLoggedIn: () => void
}

export function LoginGate({ onLoggedIn }: Props) {
    const [userId, setUserId] = useState('operator-1')
    const [error, setError]   = useState<string | null>(null)
    const [loading, setLoading] = useState(false)

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault()
        setError(null)
        setLoading(true)
        try {
            await login(userId)
            onLoggedIn()
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Login failed')
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="login-gate">
            <h2>Orbital Watch</h2>
            <p>Sign in to view live telemetry.</p>
            <form onSubmit={handleSubmit}>
                <input
                    type="text"
                    value={userId}
                    onChange={(e) => setUserId(e.target.value)}
                    placeholder="Operator ID"
                />
                <button type="submit" disabled={loading}>
                    {loading ? 'Signing in...' : 'Sign in'}
                </button>
            </form>
            {error && <p className="error">{error}</p>}
        </div>
    )
}