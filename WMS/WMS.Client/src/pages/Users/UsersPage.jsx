
import { useEffect, useState } from 'react'
import api from '../../services/api'
import './UsersPage.css'

const initialForm = {
    employeeCode: '',
    fullName: '',
    username: '',
    email: '',
    password: '',
    roleId: '',
    warehouseIds: []
}

function UsersPage() {
    const [form, setForm] = useState(initialForm)
    const [roles, setRoles] = useState([])
    const [warehouses, setWarehouses] = useState([])
    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)
    const [message, setMessage] = useState('')
    const [messageType, setMessageType] = useState('success')

    useEffect(() => {
        loadFormOptions()
    }, [])

    const loadFormOptions = async () => {
        try {
            setLoading(true)
            setMessage('')

            const [rolesResponse, warehousesResponse] = await Promise.all([
                api.get('/users/assignable-roles'),
                api.get('/warehouses')
            ])

            console.log('Roles response:', rolesResponse.data)
            console.log('Warehouses response:', warehousesResponse.data)

            const activeWarehouses = (warehousesResponse.data || [])
                .filter(warehouse => warehouse.isActive === true)

            console.log('Active warehouses:', activeWarehouses)
            console.log('Active warehouse count:', activeWarehouses.length)

            setRoles(rolesResponse.data || [])
            setWarehouses(activeWarehouses)

        } catch (error) {
            console.error('Unable to load user creation options:', error)

            setMessage(
                error.response?.data?.message ||
                'Unable to load available roles and warehouses.'
            )

            setMessageType('error')
        } finally {
            setLoading(false)
        }
    }

    const handleChange = (event) => {
        const { name, value } = event.target

        setForm(previous => ({
            ...previous,
            [name]: value
        }))
    }

    const handleWarehouseChange = (warehouseId) => {
        setForm(previous => {
            const selected = previous.warehouseIds.includes(warehouseId)

            return {
                ...previous,
                warehouseIds: selected
                    ? previous.warehouseIds.filter(id => id !== warehouseId)
                    : [...previous.warehouseIds, warehouseId]
            }
        })
    }

    const selectedRole = roles.find(
        role => String(role.roleId) === String(form.roleId)
    )

    const requiresWarehouse = [
        'Warehouse Manager',
        'Warehouse Supervisor',
        'Warehouse Staff',
        'QC Inspector',
        'Picker / Packer',
        'Inventory Controller',
        'Auditor'
    ].some(name =>
        name.toLowerCase() === selectedRole?.roleName?.toLowerCase()
    )

    const handleSubmit = async (event) => {
        event.preventDefault()
        setMessage('')

        if (!form.roleId) {
            setMessage('Please select a role.')
            setMessageType('error')
            return
        }

        if (requiresWarehouse && form.warehouseIds.length === 0) {
            setMessage('Please assign at least one warehouse.')
            setMessageType('error')
            return
        }

        const payload = {
            employeeCode: form.employeeCode.trim() || null,
            fullName: form.fullName.trim(),
            username: form.username.trim(),
            email: form.email.trim() || null,
            password: form.password,
            roleIds: [Number(form.roleId)],
            warehouseIds: form.warehouseIds
        }

        try {
            setSaving(true)

            const response = await api.post('/users', payload)

            setMessage(
                response.data?.message ||
                'User account created successfully.'
            )

            setMessageType('success')
            setForm(initialForm)
        } catch (error) {
            console.error('Unable to create user:', error)

            setMessage(
                error.response?.data?.message ||
                'Unable to create user account. Please check the information and try again.'
            )

            setMessageType('error')
        } finally {
            setSaving(false)
        }
    }

    if (loading) {
        return (
            <div className="users-page">
                <div className="users-loading">
                    Loading account creation options...
                </div>
            </div>
        )
    }

    return (
        <div className="users-page">
            <div className="users-page-header">
                <div>
                    <span className="users-eyebrow">
                        ACCOUNT ADMINISTRATION
                    </span>

                    <h1>User Management</h1>

                    <p>
                        Create and assign WMS accounts to authorized personnel.
                    </p>
                </div>
            </div>

            <div className="users-security-notice">
                <span className="users-security-icon">🔒</span>

                <div>
                    <strong>Restricted account creation</strong>
                    <p>
                        Only authorized administrators can create accounts.
                        Users cannot register themselves through the login page.
                    </p>
                </div>
            </div>

            {message && (
                <div className={`users-message ${messageType}`}>
                    {message}
                </div>
            )}

            <form className="users-form" onSubmit={handleSubmit}>
                <div className="users-form-heading">
                    <div>
                        <h2>Create New User</h2>
                        <p>Enter the employee's account information.</p>
                    </div>

                    <span className="users-required">
                        * Required fields
                    </span>
                </div>

                <div className="users-form-grid">
                    <div className="users-field">
                        <label>Employee Code</label>
                        <input
                            name="employeeCode"
                            value={form.employeeCode}
                            onChange={handleChange}
                            placeholder="e.g. EMP-0001"
                            maxLength={40}
                        />
                    </div>

                    <div className="users-field">
                        <label>Full Name *</label>
                        <input
                            name="fullName"
                            value={form.fullName}
                            onChange={handleChange}
                            placeholder="Enter full name"
                            maxLength={150}
                            required
                        />
                    </div>

                    <div className="users-field">
                        <label>Username *</label>
                        <input
                            name="username"
                            value={form.username}
                            onChange={handleChange}
                            placeholder="Enter username"
                            maxLength={80}
                            required
                        />
                    </div>

                    <div className="users-field">
                        <label>Email Address</label>
                        <input
                            type="email"
                            name="email"
                            value={form.email}
                            onChange={handleChange}
                            placeholder="employee@company.com"
                            maxLength={150}
                        />
                    </div>

                    <div className="users-field">
                        <label>Temporary Password *</label>
                        <input
                            type="password"
                            name="password"
                            value={form.password}
                            onChange={handleChange}
                            placeholder="Minimum 8 characters"
                            minLength={8}
                            required
                        />
                    </div>

                    <div className="users-field">
                        <label>Assigned Role *</label>
                        <select
                            name="roleId"
                            value={form.roleId}
                            onChange={(event) => {
                                setForm(previous => ({
                                    ...previous,
                                    roleId: event.target.value,
                                    warehouseIds: []
                                }))
                            }}
                            required
                        >
                            <option value="">Select a role</option>

                            {roles.map(role => (
                                <option
                                    key={role.roleId}
                                    value={role.roleId}
                                >
                                    {role.roleName}
                                </option>
                            ))}
                        </select>
                    </div>
                </div>

                <div className="users-section-divider" />

                <div className="users-warehouse-heading">
                    <div>
                        <h3>Warehouse Assignments</h3>
                        <p>
                            Select the warehouses this employee is authorized to access.
                        </p>
                    </div>

                    <span>
                        {form.warehouseIds.length} selected
                    </span>
                </div>

                {!form.roleId ? (
                    <div className="users-empty">
                        Select a role first to configure warehouse assignments.
                    </div>
                ) : !requiresWarehouse ? (
                    <div className="users-empty">
                        Warehouse assignment is not required for this role.
                    </div>
                ) : warehouses.length === 0 ? (
                    <div className="users-empty">
                        No active warehouses are available within your assigned scope.
                    </div>
                ) : (
                    <div className="users-warehouse-list">
                        {warehouses.map(warehouse => (
                            <label
                                className="users-warehouse-option"
                                key={warehouse.warehouseId}
                            >
                                <input
                                    type="checkbox"
                                    checked={form.warehouseIds.includes(
                                        warehouse.warehouseId
                                    )}
                                    onChange={() =>
                                        handleWarehouseChange(Number(warehouse.warehouseId))
                                    }
                                />

                                <span className="users-warehouse-check" />

                                <span className="users-warehouse-details">
                                    <strong>{warehouse.warehouseName}</strong>
                                    <small>
                                        {warehouse.warehouseCode}
                                        {warehouse.city
                                            ? ` · ${warehouse.city}`
                                            : ''}
                                    </small>
                                </span>
                            </label>
                        ))}
                    </div>
                )}

                <div className="users-form-footer">
                    <button
                        type="button"
                        className="users-reset-button"
                        disabled={saving}
                        onClick={() => {
                            setForm(initialForm)
                            setMessage('')
                        }}
                    >
                        Clear Form
                    </button>

                    <button
                        type="submit"
                        className="users-submit-button"
                        disabled={saving || roles.length === 0}
                    >
                        {saving ? 'Creating Account...' : 'Create User Account'}
                    </button>
                </div>
            </form>
        </div>
    )
}

export default UsersPage
