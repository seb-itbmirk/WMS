import { useEffect, useMemo, useState } from 'react'
import api from '../services/api'

const emptyForm = {
    customerCode: '',
    customerName: '',
    contactPerson: '',
    phone: '',
    email: '',
    address: ''
}

const getErrorMessage = (error) =>
    error.response?.data?.message ||
    error.response?.data?.title ||
    error.message ||
    'An unexpected error occurred.'

function CustomersPage() {
    const [customers, setCustomers] = useState([])
    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)
    const [error, setError] = useState('')
    const [success, setSuccess] = useState('')
    const [searchTerm, setSearchTerm] = useState('')
    const [statusFilter, setStatusFilter] = useState('ALL')
    const [showModal, setShowModal] = useState(false)
    const [editingCustomer, setEditingCustomer] = useState(null)
    const [form, setForm] = useState(emptyForm)

    const loadCustomers = async () => {
        try {
            setLoading(true)
            setError('')

            const response = await api.get('/customers')

            setCustomers(Array.isArray(response.data) ? response.data : [])
        } catch (err) {
            setError(getErrorMessage(err))
        } finally {
            setLoading(false)
        }
    }

    useEffect(() => {
        loadCustomers()
    }, [])

    const filteredCustomers = useMemo(() => {
        const term = searchTerm.trim().toLowerCase()

        return customers.filter(customer => {
            const matchesSearch = !term || [
                customer.customerCode,
                customer.customerName,
                customer.contactPerson,
                customer.phone,
                customer.email
            ].some(value =>
                String(value || '').toLowerCase().includes(term)
            )

            const matchesStatus =
                statusFilter === 'ALL' ||
                (statusFilter === 'ACTIVE' && customer.isActive) ||
                (statusFilter === 'INACTIVE' && !customer.isActive)

            return matchesSearch && matchesStatus
        })
    }, [customers, searchTerm, statusFilter])

    const activeCount = customers.filter(
        customer => customer.isActive
    ).length

    const inactiveCount = customers.length - activeCount

    const openCreateModal = () => {
        setEditingCustomer(null)
        setForm(emptyForm)
        setError('')
        setSuccess('')
        setShowModal(true)
    }

    const openEditModal = (customer) => {
        setEditingCustomer(customer)

        setForm({
            customerCode: customer.customerCode || '',
            customerName: customer.customerName || '',
            contactPerson: customer.contactPerson || '',
            phone: customer.phone || '',
            email: customer.email || '',
            address: customer.address || ''
        })

        setError('')
        setSuccess('')
        setShowModal(true)
    }

    const handleInputChange = (event) => {
        const { name, value } = event.target

        setForm(previous => ({
            ...previous,
            [name]: value
        }))
    }

    const handleSave = async (event) => {
        event.preventDefault()

        try {
            setSaving(true)
            setError('')
            setSuccess('')

            const payload = {
                customerName: form.customerName.trim(),
                contactPerson: form.contactPerson.trim() || null,
                phone: form.phone.trim() || null,
                email: form.email.trim() || null,
                address: form.address.trim() || null
            }

            if (editingCustomer) {
                await api.put(
                    `/customers/${editingCustomer.customerId}`,
                    payload
                )

                setSuccess('Customer updated successfully.')
            } else {
                await api.post('/customers', {
                    customerCode: form.customerCode.trim(),
                    ...payload
                })

                setSuccess('Customer created successfully.')
            }

            setShowModal(false)
            await loadCustomers()
        } catch (err) {
            setError(getErrorMessage(err))
        } finally {
            setSaving(false)
        }
    }

    const handleStatusChange = async (customer) => {
        const action = customer.isActive ? 'deactivate' : 'activate'

        const confirmed = window.confirm(
            `Are you sure you want to ${action} ${customer.customerName}?`
        )

        if (!confirmed) return

        try {
            setError('')
            setSuccess('')

            await api.patch(
                `/customers/${customer.customerId}/${action}`
            )

            setSuccess(
                customer.isActive
                    ? 'Customer deactivated successfully.'
                    : 'Customer activated successfully.'
            )

            await loadCustomers()
        } catch (err) {
            setError(getErrorMessage(err))
        }
    }

    return (
        <section className="customers-page">

            <div className="customers-page-header">
                <div>
                    <span className="customers-eyebrow">
                        OUTBOUND MANAGEMENT
                    </span>

                    <h1>Customer Management</h1>

                    <p>
                        Manage the brands and customers your business supplies.
                    </p>
                </div>

                <button
                    className="customers-primary-button"
                    onClick={openCreateModal}
                >
                    + Add Customer
                </button>
            </div>

            {error && (
                <div className="customers-alert customers-alert-error">
                    {error}
                </div>
            )}

            {success && (
                <div className="customers-alert customers-alert-success">
                    {success}
                </div>
            )}

            <div className="customers-summary">

                <article className="customers-summary-card">
                    <span>Total Customers</span>
                    <strong>{customers.length}</strong>
                </article>

                <article className="customers-summary-card">
                    <span>Active Customers</span>
                    <strong>{activeCount}</strong>
                </article>

                <article className="customers-summary-card">
                    <span>Inactive Customers</span>
                    <strong>{inactiveCount}</strong>
                </article>

            </div>

            <div className="customers-panel">

                <div className="customers-toolbar">
                    <div>
                        <h2>Customer Directory</h2>
                        <p>
                            View and maintain registered customer records.
                        </p>
                    </div>

                    <div className="customers-filters">
                        <input
                            type="search"
                            value={searchTerm}
                            onChange={event =>
                                setSearchTerm(event.target.value)
                            }
                            placeholder="Search customers..."
                        />

                        <select
                            value={statusFilter}
                            onChange={event =>
                                setStatusFilter(event.target.value)
                            }
                        >
                            <option value="ALL">All Status</option>
                            <option value="ACTIVE">Active</option>
                            <option value="INACTIVE">Inactive</option>
                        </select>
                    </div>
                </div>

                <div className="customers-table-wrap">
                    <table className="customers-table">

                        <thead>
                            <tr>
                                <th>Customer Code</th>
                                <th>Customer Name</th>
                                <th>Contact Person</th>
                                <th>Contact Details</th>
                                <th>Status</th>
                                <th>Actions</th>
                            </tr>
                        </thead>

                        <tbody>
                            {loading ? (
                                <tr>
                                    <td colSpan="6" className="customers-empty">
                                        Loading customers...
                                    </td>
                                </tr>
                            ) : filteredCustomers.length === 0 ? (
                                <tr>
                                    <td colSpan="6" className="customers-empty">
                                        No customers found.
                                    </td>
                                </tr>
                            ) : (
                                filteredCustomers.map(customer => (
                                    <tr key={customer.customerId}>

                                        <td>
                                            <strong>
                                                {customer.customerCode}
                                            </strong>
                                        </td>

                                        <td>{customer.customerName}</td>

                                        <td>
                                            {customer.contactPerson || '—'}
                                        </td>

                                        <td>
                                            <span>
                                                {customer.email || '—'}
                                            </span>

                                            {customer.phone && (
                                                <small>{customer.phone}</small>
                                            )}
                                        </td>

                                        <td>
                                            <span className={`customers-status ${
                                                customer.isActive
                                                    ? 'is-active'
                                                    : 'is-inactive'
                                            }`}>
                                                {customer.isActive
                                                    ? 'Active'
                                                    : 'Inactive'}
                                            </span>
                                        </td>

                                        <td>
                                            <div className="customers-row-actions">

                                                <button
                                                    className="customers-edit-button"
                                                    onClick={() =>
                                                        openEditModal(customer)
                                                    }
                                                >
                                                    Edit
                                                </button>

                                                <button
                                                    className={
                                                        customer.isActive
                                                            ? 'customers-deactivate-button'
                                                            : 'customers-activate-button'
                                                    }
                                                    onClick={() =>
                                                        handleStatusChange(customer)
                                                    }
                                                >
                                                    {customer.isActive
                                                        ? 'Deactivate'
                                                        : 'Activate'}
                                                </button>

                                            </div>
                                        </td>

                                    </tr>
                                ))
                            )}
                        </tbody>

                    </table>
                </div>

                <div className="customers-table-footer">
                    Showing {filteredCustomers.length} of {customers.length} customers
                </div>

            </div>

            {showModal && (
                <div
                    className="customers-modal-backdrop"
                    onMouseDown={event => {
                        if (
                            event.target === event.currentTarget &&
                            !saving
                        ) {
                            setShowModal(false)
                        }
                    }}
                >
                    <form
                        className="customers-modal"
                        onSubmit={handleSave}
                    >

                        <div className="customers-modal-header">
                            <div>
                                <span className="customers-eyebrow">
                                    CUSTOMER RECORD
                                </span>

                                <h2>
                                    {editingCustomer
                                        ? 'Edit Customer'
                                        : 'Add Customer'}
                                </h2>
                            </div>

                            <button
                                type="button"
                                className="customers-close-button"
                                onClick={() => setShowModal(false)}
                                disabled={saving}
                            >
                                ×
                            </button>
                        </div>

                        {error && (
                            <div className="customers-alert customers-alert-error">
                                {error}
                            </div>
                        )}

                        <div className="customers-form-grid">

                            <label>
                                Customer Code

                                <input
                                    name="customerCode"
                                    value={form.customerCode}
                                    onChange={handleInputChange}
                                    maxLength={40}
                                    required
                                    disabled={Boolean(editingCustomer)}
                                    placeholder="e.g. NUTRIASIA"
                                />
                            </label>

                            <label>
                                Customer Name

                                <input
                                    name="customerName"
                                    value={form.customerName}
                                    onChange={handleInputChange}
                                    maxLength={200}
                                    required
                                    placeholder="Enter customer or brand name"
                                />
                            </label>

                            <label>
                                Contact Person

                                <input
                                    name="contactPerson"
                                    value={form.contactPerson}
                                    onChange={handleInputChange}
                                    maxLength={150}
                                />
                            </label>

                            <label>
                                Phone

                                <input
                                    name="phone"
                                    value={form.phone}
                                    onChange={handleInputChange}
                                    maxLength={50}
                                />
                            </label>

                            <label className="customers-full-field">
                                Email

                                <input
                                    type="email"
                                    name="email"
                                    value={form.email}
                                    onChange={handleInputChange}
                                    maxLength={150}
                                />
                            </label>

                            <label className="customers-full-field">
                                Address

                                <textarea
                                    name="address"
                                    value={form.address}
                                    onChange={handleInputChange}
                                    rows={3}
                                />
                            </label>

                        </div>

                        <div className="customers-modal-actions">

                            <button
                                type="button"
                                className="customers-secondary-button"
                                onClick={() => setShowModal(false)}
                                disabled={saving}
                            >
                                Cancel
                            </button>

                            <button
                                type="submit"
                                className="customers-primary-button"
                                disabled={saving}
                            >
                                {saving
                                    ? 'Saving...'
                                    : editingCustomer
                                        ? 'Save Changes'
                                        : 'Create Customer'}
                            </button>

                        </div>

                    </form>
                </div>
            )}

        </section>
    )
}

export default CustomersPage
