
import { useState } from 'react'
import api from '../../services/api'
import './loginpage.css'

function LoginPage({ onLogin }) {
    const [formData, setFormData] = useState({
        username: '',
        password: ''
    })

    const [showPassword, setShowPassword] = useState(false)
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState('')

    const handleChange = (event) => {
        const { name, value } = event.target

        setFormData(previous => ({
            ...previous,
            [name]: value
        }))

        if (error) setError('')
    }

    const handleSubmit = async (event) => {
        event.preventDefault()
        setError('')

        const username = formData.username.trim()
        const password = formData.password

        if (!username || !password) {
            setError('Please enter your username and password.')
            return
        }

        try {
            setLoading(true)

            const response = await api.post('/auth/login', {
                username,
                password
            })

            const loginData = response.data
            const accessToken =
                loginData?.accessToken ??
                loginData?.AccessToken

            if (!accessToken) {
                throw new Error(
                    'Invalid login response. The server did not return an access token.'
                )
            }

            if (typeof onLogin !== 'function') {
                throw new Error('Authentication handler is unavailable.')
            }

            // Wait until AuthGate finishes loading the user's profile.
            await onLogin(loginData)

        } catch (err) {
            console.error('Login failed:', err)

            const responseData = err.response?.data

            setError(
                responseData?.message ||
                responseData?.Message ||
                responseData?.error ||
                err.message ||
                'Unable to sign in. Please check your credentials.'
            )
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="login-page">
            <div className="login-background">
                <div className="login-decoration login-decoration-one" />
                <div className="login-decoration login-decoration-two" />
            </div>

            <div className="login-container">

                <section className="login-brand-panel">
                    <div className="login-brand">
                        <div className="login-brand-icon">W</div>

                        <div>
                            <h2>WAREHOUSE</h2>
                            <p>MANAGEMENT SYSTEM</p>
                        </div>
                    </div>

                    <div className="login-brand-content">
                        <span className="login-eyebrow">
                            SMARTER WAREHOUSE OPERATIONS
                        </span>

                        <h1>
                            Manage your warehouse.
                            <span> Control your operations.</span>
                        </h1>

                        <p>
                            A centralized platform for managing inventory,
                            warehouse operations, manufacturing, and
                            business resources.
                        </p>

                        <div className="login-feature-list">
                            <div>
                                <span>✓</span>
                                Inventory visibility and control
                            </div>

                            <div>
                                <span>✓</span>
                                Production and warehouse management
                            </div>

                            <div>
                                <span>✓</span>
                                Secure role-based access
                            </div>
                        </div>
                    </div>

                    <div className="login-brand-footer">
                        WMS · Warehouse Management System
                    </div>
                </section>

                <section className="login-form-panel">
                    <div className="login-form-wrapper">

                        <div className="login-mobile-brand">
                            <div className="login-brand-icon">W</div>
                            <span>WAREHOUSE MANAGEMENT SYSTEM</span>
                        </div>

                        <div className="login-heading">
                            <span>WELCOME BACK</span>
                            <h1>Sign in to your account</h1>
                            <p>
                                Enter the credentials provided by your system administrator.
                            </p>
                        </div>

                        <form onSubmit={handleSubmit}>

                            {error && (
                                <div className="login-error" role="alert">
                                    <span>!</span>
                                    {error}
                                </div>
                            )}

                            <div className="login-field">
                                <label htmlFor="username">
                                    Username
                                </label>

                                <input
                                    id="username"
                                    name="username"
                                    type="text"
                                    placeholder="Enter your username"
                                    value={formData.username}
                                    onChange={handleChange}
                                    autoComplete="username"
                                    autoFocus
                                    disabled={loading}
                                    required
                                />
                            </div>

                            <div className="login-field">
                                <label htmlFor="password">
                                    Password
                                </label>

                                <div className="login-password-wrapper">
                                    <input
                                        id="password"
                                        name="password"
                                        type={showPassword ? 'text' : 'password'}
                                        placeholder="Enter your password"
                                        value={formData.password}
                                        onChange={handleChange}
                                        autoComplete="current-password"
                                        disabled={loading}
                                        required
                                    />

                                    <button
                                        type="button"
                                        className="password-toggle"
                                        onClick={() => setShowPassword(previous => !previous)}
                                        disabled={loading}
                                        aria-label={showPassword ? 'Hide password' : 'Show password'}
                                    >
                                        {showPassword ? 'Hide' : 'Show'}
                                    </button>
                                </div>
                            </div>

                            <button
                                type="submit"
                                className="login-submit"
                                disabled={loading}
                            >
                                {loading ? (
                                    <>
                                        <span className="login-spinner" />
                                        Signing in...
                                    </>
                                ) : (
                                    <>
                                        Sign in
                                        <span>→</span>
                                    </>
                                )}
                            </button>

                        </form>

                        <div className="login-security-note">
                            <span>▣</span>
                            Secure access to your warehouse workspace
                        </div>

                        <div className="login-copyright">
                            © {new Date().getFullYear()} Warehouse Management
                            System. All rights reserved.
                        </div>

                    </div>
                </section>

            </div>
        </div>
    )
}

export default LoginPage
