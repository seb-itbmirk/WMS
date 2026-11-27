
import { useCallback, useEffect, useMemo, useState } from 'react'
import api from '../../services/api'
import './supplier.css'

const initialFormData = {
    supplierCode: '',
    supplierName: '',
    contactPerson: '',
    phone: '',
    email: '',
    address: '',
    isActive: true
}

function SuppliersPage() {
    const [suppliers, setSuppliers] = useState([])
    const [loading, setLoading] = useState(true)
    const [refreshing, setRefreshing] = useState(false)
    const [saving, setSaving] = useState(false)
    const [error, setError] = useState('')
    const [formError, setFormError] = useState('')
    const [search, setSearch] = useState('')
    const [statusFilter, setStatusFilter] = useState('ALL')

    const [showForm, setShowForm] = useState(false)
    const [editingSupplier, setEditingSupplier] = useState(null)
    const [formData, setFormData] = useState(initialFormData)

    const loadSuppliers = useCallback(async (isRefresh = false) =>        {
        try {
            if (isRefresh) {
                setRefreshing(true)
            } else {
                setLoading(true)
            }

            setError('')

            const response = await api.get('/suppliers')
            setSuppliers(Array.isArray(response.data) ? response.data : [])
        } catch (err) {
            console.error('Unable to load suppliers:', err)

            setError(
                err.response?.data?.message ||
                err.response?.data?.title ||
                'Unable to load supplier records.'
            )
        } finally {
            setLoading(false)
            setRefreshing(false)
        }
    }, [])

    useEffect(() => {
        loadSuppliers()
    }, [loadSuppliers])

    const activeCount = useMemo(
        () => suppliers.filter(supplier => supplier.isActive).length,
        [suppliers]
    )

    const inactiveCount = suppliers.length - activeCount

    const filteredSuppliers = useMemo(() => {
        const term = search.trim().toLowerCase()

        return suppliers.filter(supplier => {
            const matchesSearch =
                !term ||
                supplier.supplierCode?.toLowerCase().includes(term) ||
                supplier.supplierName?.toLowerCase().includes(term) ||
                supplier.contactPerson?.toLowerCase().includes(term) ||
                supplier.email?.toLowerCase().includes(term) ||
                supplier.phone?.toLowerCase().includes(term)

            const matchesStatus =
                statusFilter === 'ALL' ||
                (statusFilter === 'ACTIVE' && supplier.isActive) ||
                (statusFilter === 'INACTIVE' && !supplier.isActive)

            return matchesSearch && matchesStatus
        })
    }, [suppliers, search, statusFilter])

    const openCreateForm = () => {
        setEditingSupplier(null)
        setFormData({ ...initialFormData })
        setFormError('')
        setShowForm(true)
    }

    const openEditForm = (supplier) => {
        setEditingSupplier(supplier)
        setFormData({
            supplierCode: supplier.supplierCode || '',
            supplierName: supplier.supplierName || '',
            contactPerson: supplier.contactPerson || '',
            phone: supplier.phone || '',
            email: supplier.email || '',
            address: supplier.address || '',
            isActive: supplier.isActive ?? true
        })
        setFormError('')
        setShowForm(true)
    }

    const closeForm = () => {
        if (saving) return

        setShowForm(false)
        setEditingSupplier(null)
        setFormData({ ...initialFormData })
        setFormError('')
    }

    const handleChange = (event) => {
        const { name, value, type, checked } = event.target

        setFormData(prev => ({
            ...prev,
            [name]: type === 'checkbox' ? checked : value
        }))
    }

    const validateForm = () => {
        if (!formData.supplierCode.trim()) {
            return 'Supplier code is required.'
        }

        if (!formData.supplierName.trim()) {
            return 'Supplier name is required.'
        }

        if (
            suppliers.some(supplier =>
                supplier.supplierCode?.trim().toLowerCase() ===
                formData.supplierCode.trim().toLowerCase() &&
                supplier.supplierId !== editingSupplier?.supplierId
            )
        ) {
            return 'Supplier code already exists.'
        }

        if (formData.email.trim()) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

            if (!emailRegex.test(formData.email.trim())) {
                return 'Please enter a valid email address.'
            }
        }

        return ''
    }

    const handleSubmit = async (event) => {
        event.preventDefault()

        const validationError = validateForm()

        if (validationError) {
            setFormError(validationError)
            return
        }

        try {
            setSaving(true)
            setFormError('')

            const payload = {
                supplierCode: formData.supplierCode.trim(),
                supplierName: formData.supplierName.trim(),
                contactPerson: formData.contactPerson.trim() || null,
                phone: formData.phone.trim() || null,
                email: formData.email.trim() || null,
                address: formData.address.trim() || null,
                ...(editingSupplier
                    ? { isActive: formData.isActive }
                    : {})
            }

            if (editingSupplier) {
                await api.put(
                    `/suppliers/${editingSupplier.supplierId}`,
                    payload
                )
            } else {
                await api.post('/suppliers', payload)
            }

            setShowForm(false)
            setEditingSupplier(null)
            setFormData({ ...initialFormData })

            await loadSuppliers(true)
        } catch (err) {
            console.error('Unable to save supplier:', err)

            setFormError(
                err.response?.data?.message ||
                err.response?.data?.title ||
                'Unable to save supplier. Please try again.'
            )
        } finally {
            setSaving(false)
        }
    }

    return (
        <div className="suppliers-page">
            <div className="suppliers-header">
                <div>
                    <p className="suppliers-eyebrow">PROCUREMENT</p>
                    <h1>Supplier Management</h1>
                    <p>
                        View and manage suppliers registered in your
                        warehouse system.
                    </p>
                </div>

                <div className="suppliers-header-actions">
                    <button
                        type="button"
                        className="suppliers-refresh-button"
                        onClick={() => loadSuppliers(true)}
                        disabled={loading || refreshing}
                    >
                        {refreshing ? 'Refreshing...' : '↻ Refresh'}
                    </button>

                    <button
                        type="button"
                        className="suppliers-primary-button"
                        onClick={openCreateForm}
                    >
                        + Add Supplier
                    </button>
                </div>
            </div>

            {error && (
                <div className="suppliers-error">
                    <span>{error}</span>
                    <button
                        type="button"
                        onClick={() => setError('')}
                        aria-label="Dismiss error"
                    >
                        ×
                    </button>
                </div>
            )}

            <div className="suppliers-summary">
                <div className="suppliers-summary-card">
                    <span>TOTAL SUPPLIERS</span>
                    <strong>{loading ? '—' : suppliers.length}</strong>
                    <small>All registered suppliers</small>
                </div>

                <div className="suppliers-summary-card">
                    <span>ACTIVE SUPPLIERS</span>
                    <strong>{loading ? '—' : activeCount}</strong>
                    <small>Currently active records</small>
                </div>

                <div className="suppliers-summary-card">
                    <span>INACTIVE SUPPLIERS</span>
                    <strong>{loading ? '—' : inactiveCount}</strong>
                    <small>Currently inactive records</small>
                </div>
            </div>

            <section className="suppliers-panel">
                <div className="suppliers-panel-heading">
                    <div>
                        <h2>Supplier Directory</h2>
                        <p>
                            {loading
                                ? 'Loading supplier records...'
                                : `${filteredSuppliers.length} matching suppliers`}
                        </p>
                    </div>
                </div>

                <div className="suppliers-filters">
                    <div className="suppliers-search">
                        <label htmlFor="suppliers-search">
                            Search suppliers
                        </label>
                        <input
                            id="suppliers-search"
                            type="search"
                            value={search}
                            onChange={event => setSearch(event.target.value)}
                            placeholder="Search code, name, contact, email, or phone..."
                        />
                    </div>

                    <div className="suppliers-status-filter">
                        <label htmlFor="suppliers-status">
                            Supplier status
                        </label>
                        <select
                            id="suppliers-status"
                            value={statusFilter}
                            onChange={event => setStatusFilter(event.target.value)}
                        >
                            <option value="ALL">All suppliers</option>
                            <option value="ACTIVE">Active only</option>
                            <option value="INACTIVE">Inactive only</option>
                        </select>
                    </div>
                </div>

                <div className="suppliers-table-wrapper">
                    <table className="suppliers-table">
                        <thead>
                            <tr>
                                <th>SUPPLIER</th>
                                <th>CONTACT PERSON</th>
                                <th>PHONE</th>
                                <th>EMAIL</th>
                                <th>ADDRESS</th>
                                <th>STATUS</th>
                                <th>ACTIONS</th>
                            </tr>
                        </thead>

                        <tbody>
                            {loading ? (
                                <tr>
                                    <td colSpan="7" className="suppliers-empty">
                                        Loading supplier records...
                                    </td>
                                </tr>
                            ) : filteredSuppliers.length === 0 ? (
                                <tr>
                                    <td colSpan="7" className="suppliers-empty">
                                        No suppliers match your search.
                                    </td>
                                </tr>
                            ) : (
                                filteredSuppliers.map(supplier => (
                                    <tr key={supplier.supplierId}>
                                        <td>
                                            <div className="suppliers-name-cell">
                                                <strong>
                                                    {supplier.supplierName}
                                                </strong>
                                                <small>
                                                    {supplier.supplierCode}
                                                </small>
                                            </div>
                                        </td>

                                        <td>
                                            {supplier.contactPerson || '—'}
                                        </td>

                                        <td>{supplier.phone || '—'}</td>

                                        <td>
                                            {supplier.email ? (
                                                <a href={`mailto:${supplier.email}`}>
                                                    {supplier.email}
                                                </a>
                                            ) : '—'}
                                        </td>

                                        <td className="suppliers-address">
                                            {supplier.address || '—'}
                                        </td>

                                        <td>
                                            <span
                                                className={
                                                    supplier.isActive
                                                        ? 'suppliers-status suppliers-status-active'
                                                        : 'suppliers-status suppliers-status-inactive'
                                                }
                                            >
                                                {supplier.isActive
                                                    ? 'Active'
                                                    : 'Inactive'}
                                            </span>
                                        </td>

                                        <td>
                                            <button
                                                type="button"
                                                className="suppliers-edit-button"
                                                onClick={() => openEditForm(supplier)}
                                            >
                                                Edit
                                            </button>
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>
            </section>

            {showForm && (
                <div
                    className="suppliers-modal-overlay"
                    onMouseDown={event => {
                        if (
                            event.target === event.currentTarget &&
                            !saving
                        ) {
                            closeForm()
                        }
                    }}
                >
                    <div
                        className="suppliers-modal"
                        role="dialog"
                        aria-modal="true"
                        aria-labelledby="supplier-form-title"
                    >
                        <div className="suppliers-modal-header">
                            <div>
                                <p className="suppliers-eyebrow">
                                    PROCUREMENT
                                </p>
                                <h2 id="supplier-form-title">
                                    {editingSupplier
                                        ? 'Edit Supplier'
                                        : 'Add Supplier'}
                                </h2>
                                <p>
                                    {editingSupplier
                                        ? 'Update supplier information.'
                                        : 'Enter the supplier information below.'}
                                </p>
                            </div>

                            <button
                                type="button"
                                className="suppliers-modal-close"
                                onClick={closeForm}
                                disabled={saving}
                                aria-label="Close modal"
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={handleSubmit}>
                            <div className="suppliers-form-grid">
                                <div className="suppliers-form-group">
                                    <label htmlFor="supplierCode">
                                        Supplier Code <span>*</span>
                                    </label>
                                    <input
                                        id="supplierCode"
                                        name="supplierCode"
                                        value={formData.supplierCode}
                                        onChange={handleChange}
                                        placeholder="e.g. SUP-001"
                                        maxLength={50}
                                        required
                                        disabled={saving}
                                    />
                                </div>

                                <div className="suppliers-form-group">
                                    <label htmlFor="supplierName">
                                        Supplier Name <span>*</span>
                                    </label>
                                    <input
                                        id="supplierName"
                                        name="supplierName"
                                        value={formData.supplierName}
                                        onChange={handleChange}
                                        placeholder="Enter supplier name"
                                        maxLength={200}
                                        required
                                        disabled={saving}
                                    />
                                </div>

                                <div className="suppliers-form-group">
                                    <label htmlFor="contactPerson">
                                        Contact Person
                                    </label>
                                    <input
                                        id="contactPerson"
                                        name="contactPerson"
                                        value={formData.contactPerson}
                                        onChange={handleChange}
                                        placeholder="Enter contact person"
                                        maxLength={150}
                                        disabled={saving}
                                    />
                                </div>

                                <div className="suppliers-form-group">
                                    <label htmlFor="phone">
                                        Phone Number
                                    </label>
                                    <input
                                        id="phone"
                                        name="phone"
                                        value={formData.phone}
                                        onChange={handleChange}
                                        placeholder="Enter phone number"
                                        maxLength={50}
                                        disabled={saving}
                                    />
                                </div>

                                <div className="suppliers-form-group">
                                    <label htmlFor="email">
                                        Email Address
                                    </label>
                                    <input
                                        id="email"
                                        name="email"
                                        type="email"
                                        value={formData.email}
                                        onChange={handleChange}
                                        placeholder="Enter email address"
                                        maxLength={150}
                                        disabled={saving}
                                    />
                                </div>

                                <div className="suppliers-form-group suppliers-form-full">
                                    <label htmlFor="address">
                                        Address
                                    </label>
                                    <textarea
                                        id="address"
                                        name="address"
                                        value={formData.address}
                                        onChange={handleChange}
                                        placeholder="Enter supplier address"
                                        maxLength={500}
                                        rows={3}
                                        disabled={saving}
                                    />
                                </div>

                                {editingSupplier && (
                                    <div className="suppliers-form-group suppliers-form-full">
                                        <label className="suppliers-checkbox-label">
                                            <input
                                                type="checkbox"
                                                name="isActive"
                                                checked={formData.isActive}
                                                onChange={handleChange}
                                                disabled={saving}
                                            />
                                            <span>
                                                Supplier is active
                                            </span>
                                        </label>
                                        <small>
                                            Inactive suppliers will no longer
                                            be available for new transactions
                                            where active suppliers are required.
                                        </small>
                                    </div>
                                )}
                            </div>

                            {formError && (
                                <div className="suppliers-form-error">
                                    {formError}
                                </div>
                            )}

                            <div className="suppliers-modal-actions">
                                <button
                                    type="button"
                                    className="suppliers-secondary-button"
                                    onClick={closeForm}
                                    disabled={saving}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="submit"
                                    className="suppliers-primary-button"
                                    disabled={saving}
                                >
                                    {saving
                                        ? 'Saving...'
                                        : editingSupplier
                                            ? 'Save Changes'
                                            : 'Create Supplier'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    )
}

export default SuppliersPage
