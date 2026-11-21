
import { useCallback, useEffect, useMemo, useState } from 'react'
import api from '../services/api'

function SuppliersPage() {
    const [suppliers, setSuppliers] = useState([])
    const [loading, setLoading] = useState(true)
    const [refreshing, setRefreshing] = useState(false)
    const [error, setError] = useState('')
    const [search, setSearch] = useState('')
    const [statusFilter, setStatusFilter] = useState('ALL')

    const loadSuppliers = useCallback(async (isRefresh = false) => {
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

    return (
        <div className="suppliers-page">
            <div className="suppliers-header">
                <div>
                    <p className="suppliers-eyebrow">PROCUREMENT</p>
                    <h1>Supplier Management</h1>
                    <p>
                        View and search the suppliers registered in your
                        warehouse system.
                    </p>
                </div>

                <button
                    type="button"
                    className="suppliers-refresh-button"
                    onClick={() => loadSuppliers(true)}
                    disabled={loading || refreshing}
                >
                    {refreshing ? 'Refreshing...' : '↻ Refresh'}
                </button>
            </div>

            <div className="suppliers-notice">
                <strong>Read-only supplier directory</strong>
                <span>
                    The current supplier API supports viewing supplier records
                    only. Creating, editing, and changing supplier status are
                    not available until corresponding backend endpoints exist.
                </span>
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
                            </tr>
                        </thead>

                        <tbody>
                            {loading ? (
                                <tr>
                                    <td colSpan="6" className="suppliers-empty">
                                        Loading supplier records...
                                    </td>
                                </tr>
                            ) : filteredSuppliers.length === 0 ? (
                                <tr>
                                    <td colSpan="6" className="suppliers-empty">
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
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>
            </section>
        </div>
    )
}

export default SuppliersPage
