
import { useCallback, useEffect, useState } from 'react'
import App from '../../App'
import api from '../../services/api'
import LoginPage from '../../pages/Authentication/LoginPage'

const TOKEN_KEY = 'wms_access_token'
const USER_KEY = 'wms_user'

function AuthGate() {
    const [currentUser, setCurrentUser] = useState(null)
    const [checkingAuth, setCheckingAuth] = useState(true)

    const clearSession = useCallback(() => {
        localStorage.removeItem(TOKEN_KEY)
        localStorage.removeItem(USER_KEY)
        setCurrentUser(null)
    }, [])

    const loadCurrentUser = useCallback(async () => {
        const response = await api.get('/auth/me')

        if (!response.data) {
            throw new Error('Unable to retrieve the user profile.')
        }

        localStorage.setItem(USER_KEY, JSON.stringify(response.data))
        setCurrentUser(response.data)

        return response.data
    }, [])

    // Restore the session when the application starts.
    useEffect(() => {
        let active = true

        const initializeAuth = async () => {
            const token = localStorage.getItem(TOKEN_KEY)

            if (!token) {
                if (active) setCheckingAuth(false)
                return
            }

            try {
                const response = await api.get('/auth/me')

                if (!active) return

                localStorage.setItem(USER_KEY, JSON.stringify(response.data))
                setCurrentUser(response.data)
            } catch (error) {
                console.error('Authentication session expired:', error)

                localStorage.removeItem(TOKEN_KEY)
                localStorage.removeItem(USER_KEY)

                if (active) setCurrentUser(null)
            } finally {
                if (active) setCheckingAuth(false)
            }
        }

        initializeAuth()

        return () => {
            active = false
        }
    }, [])

    // Called by LoginPage after successful credential validation.
    const handleLogin = async (loginResponse) => {
        const token =
            loginResponse?.accessToken ??
            loginResponse?.AccessToken

        if (!token) {
            throw new Error('The login response did not contain an access token.')
        }

        localStorage.setItem(TOKEN_KEY, token)

        try {
            await loadCurrentUser()
        } catch (error) {
            clearSession()
            throw new Error(
                'Login succeeded, but the user profile could not be loaded. Please try again.'
            )
        }
    }

    // End the current session.
    const handleLogout = () => {
        clearSession()
    }

    if (checkingAuth) {
        return (
            <div className="auth-loading">
                <span>Loading WMS...</span>
            </div>
        )
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
