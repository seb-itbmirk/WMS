
import { useCallback, useEffect, useMemo, useState } from 'react'
import api from '../../services/api'
import './userslistpage.css'

function UsersListPage() {
    const [users, setUsers] = useState([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState('')
    const [message, setMessage] = useState('')
    const [search, setSearch] = useState('')
    const [statusFilter, setStatusFilter] = useState('all')
    const [processingUserId, setProcessingUserId] = useState(null)

    const loadUsers = useCallback(async () => {
        try {
            setLoading(true)
            setError('')

            const response = await api.get('/users')
            setUsers(Array.isArray(response.data) ? response.data : [])
        } catch (err) {
            console.error('Unable to load users:', err)

            setError(
                err.response?.data?.message ||
                'Unable to retrieve user accounts.'
            )
        } finally {
            setLoading(false)
        }
    }, [])

    useEffect(() => {
        loadUsers()
    }, [loadUsers])

    const handleToggleStatus = async user => {
        const action = user.isActive ? 'deactivate' : 'activate'

        const confirmed = window.confirm(
            `Are you sure you want to ${action} ${user.fullName}'s account?`
        )

        if (!confirmed) return

        try {
            setProcessingUserId(user.userId)
            setError('')
            setMessage('')

            const response = await api.patch(
                `/users/${user.userId}/status`
            )

            setUsers(previousUsers =>
                previousUsers.map(existingUser =>
                    existingUser.userId === user.userId
                        ? {
                            ...existingUser,
                            isActive: response.data.isActive
                        }
                        : existingUser
                )
            )

            setMessage(
                response.data.message ||
                `Account ${action}d successfully.`
            )
        } catch (err) {
            console.error('Unable to change account status:', err)

            setError(
                err.response?.data?.message ||
                `Unable to ${action} the user account.`
            )
        } finally {
            setProcessingUserId(null)
        }
    }

    const filteredUsers = useMemo(() => {
        const keyword = search.trim().toLowerCase()

        return users.filter(user => {
            const matchesSearch = !keyword || [
                user.fullName,
                user.username,
                user.employeeCode,
                user.email,
                ...(user.roles || []).map(role => role.roleName),
                ...(user.warehouses || []).map(
                    warehouse => warehouse.warehouseName
                )
            ]
                .filter(Boolean)
                .some(value =>
                    String(value).toLowerCase().includes(keyword)
                )

            const matchesStatus =
                statusFilter === 'all' ||
                (statusFilter === 'active' && user.isActive) ||
                (statusFilter === 'inactive' && !user.isActive)

            return matchesSearch && matchesStatus
        })
    }, [users, search, statusFilter])

    const activeCount = users.filter(user => user.isActive).length
    const inactiveCount = users.length - activeCount

    return (
        <div className="users-list-page">
            <div className="users-list-header">
                <div>
                    <span className="users-list-eyebrow">
                        ADMINISTRATION
                    </span>

                    <h1>User List</h1>

                    <p>
                        View and monitor registered WMS user accounts.
                    </p>
                </div>

                <button
                    type="button"
                    className="users-list-refresh"
                    onClick={loadUsers}
                    disabled={loading}
                >
                    {loading ? 'Loading...' : '↻ Refresh'}
                </button>
            </div>

            <div className="users-list-statistics">
                <div className="users-list-stat-card">
                    <span>Total Users</span>
                    <strong>{users.length}</strong>
                </div>

                <div className="users-list-stat-card">
                    <span>Active Users</span>
                    <strong>{activeCount}</strong>
                </div>

                <div className="users-list-stat-card">
                    <span>Inactive Users</span>
                    <strong>{inactiveCount}</strong>
                </div>
            </div>

            <div className="users-list-toolbar">
                <input
                    type="search"
                    placeholder="Search name, username, employee code, role..."
                    value={search}
                    onChange={event => setSearch(event.target.value)}
                />
                <select
                    value={statusFilter}
                    onChange={event => setStatusFilter(event.target.value)}
                >
                    <option value="all">Asigned Warehouse</option>
                    <option value="active">Active accounts</option>
                    <option value="inactive">Inactive accounts</option>
                </select>
                <select
                    value={statusFilter}
                    onChange={event => setStatusFilter(event.target.value)}
                >
                    <option value="all">All accounts</option>
                    <option value="active">Active accounts</option>
                    <option value="inactive">Inactive accounts</option>
                </select>
            </div>

            {message && (
                <div className="users-list-success">
                    {message}
                    <button
                        type="button"
                        onClick={() => setMessage('')}
                    >
                        ×
                    </button>
                </div>
            )}

            {error && (
                <div className="users-list-error">
                    {error}
                    <button type="button" onClick={loadUsers}>
                        Try again
                    </button>
                </div>
            )}

            <div className="users-list-table-container">
                <table className="users-list-table">
                    <thead>
                        <tr>
                            <th>Employee</th>
                            <th>Username</th>
                            <th>Role</th>
                            <th>Assigned Warehouses</th>
                            <th>Status</th>
                            <th>Date Created</th>
                            <th>Actions</th>
                        </tr>
                    </thead>

                    <tbody>
                        {loading ? (
                            <tr>
                                <td colSpan="7" className="users-list-empty">
                                    Loading user accounts...
                                </td>
                            </tr>
                        ) : filteredUsers.length === 0 ? (
                            <tr>
                                <td colSpan="7" className="users-list-empty">
                                    No user accounts found.
                                </td>
                            </tr>
                        ) : (
                            filteredUsers.map(user => (
                                <tr key={user.userId}>
                                    <td>
                                        <div className="users-list-employee">
                                            <strong>{user.fullName}</strong>
                                            <span>
                                                {user.employeeCode || 'No employee code'}
                                            </span>
                                            <small>
                                                {user.email || 'No email address'}
                                            </small>
                                        </div>
                                    </td>

                                    <td>{user.username}</td>

                                    <td>
                                        <div className="users-list-tags">
                                            {(user.roles || []).length > 0
                                                ? user.roles.map(role => (
                                                    <span key={role.roleId}>
                                                        {role.roleName}
                                                    </span>
                                                ))
                                                : 'No role assigned'}
                                        </div>
                                    </td>

                                    
                                    <td>
                                        <div className="users-list-warehouses">
                                            {(user.roles || []).some(role =>
                                                role.roleName?.replace(/[^a-zA-Z0-9]/g, '').toLowerCase() === 'superadmin'
                                            ) ? (
                                                <span className="users-list-global-access">
                                                    GLOBAL ACCESS
                                                </span>
                                            ) : (user.warehouses || []).length > 0 ? (
                                                user.warehouses.map(warehouse => (
                                                    <span key={warehouse.warehouseId}>
                                                        {warehouse.warehouseName}
                                                    </span>
                                                ))
                                            ) : (
                                                <span className="users-list-no-warehouse">
                                                    No warehouse assigned
                                                </span>
                                            )}
                                        </div>
                                    </td>


                                    <td>
                                        <span
                                            className={`users-list-status ${
                                                user.isActive ? 'active' : 'inactive'
                                            }`}
                                        >
                                            {user.isActive ? 'Active' : 'Inactive'}
                                        </span>
                                    </td>

                                    <td>
                                        {user.createdAt
                                            ? new Date(user.createdAt).toLocaleDateString()
                                            : '—'}
                                    </td>

                                    <td>
                                        <button
                                            type="button"
                                            className={`users-list-action ${
                                                user.isActive
                                                    ? 'deactivate'
                                                    : 'activate'
                                            }`}
                                            onClick={() =>
                                                handleToggleStatus(user)
                                            }
                                            disabled={
                                                processingUserId === user.userId
                                            }
                                        >
                                            {processingUserId === user.userId
                                                ? 'Processing...'
                                                : user.isActive
                                                    ? 'Deactivate'
                                                    : 'Activate'}
                                        </button>
                                    </td>
                                </tr>
                            ))
                        )}
                    </tbody>
                </table>
            </div>

            <div className="users-list-footer">
                Showing {filteredUsers.length} of {users.length} users
            </div>
        </div>
    )
}

export default UsersListPage
