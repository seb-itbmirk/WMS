import { useEffect, useState } from 'react'
import App from '../../App'
import api from '../../services/api'
import LoginPage from '../../pages/Authentication/LoginPage'

function AuthGate() {
    const [currentUser, setCurrentUser] = useState(null)
    const [checkingAuth, setCheckingAuth] = useState(true)

    useEffect(() => {
        const verifySession = async () => {
            const token = localStorage.getItem('wms_access_token')

            if (!token) {
                setCheckingAuth(false)
                return
            }

            try {
                const response = await api.get('/auth/me')
                setCurrentUser(response.data)
            } catch (error) {
                localStorage.removeItem('wms_access_token')
                localStorage.removeItem('wms_user')
                setCurrentUser(null)
            } finally {
                setCheckingAuth(false)
            }
        }

        verifySession()
    }, [])

    const handleLogin = (userData) => {
        const token = userData.accessToken ?? userData.AccessToken

        if (!token) {
            console.error('Login response does not contain an access token.')
            return
        }

        localStorage.setItem('wms_access_token', token)
        localStorage.setItem('wms_user', JSON.stringify(userData))
        setCurrentUser(userData)
    }

    const handleLogout = () => {
        localStorage.removeItem('wms_access_token')
        localStorage.removeItem('wms_user')
        setCurrentUser(null)
    }

    if (checkingAuth) {
        return <div className="auth-loading">Loading WMS...</div>
    }

    if (!currentUser) {
        return <LoginPage onLogin={handleLogin} />
    }

    return (
        <App
            currentUser={currentUser}
            onLogout={handleLogout}
        />
    )
}

export default AuthGate