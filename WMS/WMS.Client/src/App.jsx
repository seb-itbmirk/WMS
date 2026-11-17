
import { useEffect, useState } from 'react'
import api from './services/api'
import './App.css'

function App() {
    const [page, setPage] = useState('Dashboard')
    const [warehouses, setWarehouses] = useState([])
    const [categories, setCategories] = useState([])
    const [loading, setLoading] = useState(true)
    const [apiError, setApiError] = useState(false)

    const [showForm, setShowForm] = useState(false)
    const [formType, setFormType] = useState('')
    const [editingId, setEditingId] = useState(null)
    const [formData, setFormData] = useState({})
    const [saving, setSaving] = useState(false)
    const [message, setMessage] = useState('')

    // Load warehouse and category records
    const loadData = async () => {
        try {
            setLoading(true)
            setApiError(false)

            const [warehouseRes, categoryRes] = await Promise.all([
                api.get('/warehouses'),
                api.get('/item-categories')
            ])

            setWarehouses(warehouseRes.data)
            setCategories(categoryRes.data)
        } catch (error) {
            console.error('Unable to load data:', error)
            setApiError(true)
        } finally {
            setLoading(false)
        }
    }

    useEffect(() => {
        loadData()
    }, [])

    // Open the form for adding or editing
    const openForm = (type, record = null) => {
        setFormType(type)
        setEditingId(record ? (
            type === 'warehouse' ? record.warehouseId : record.categoryId
        ) : null)

        if (type === 'warehouse') {
            setFormData(record ? {
                warehouseCode: record.warehouseCode,
                warehouseName: record.warehouseName,
                city: record.city || '',
                address: record.address || ''
            } : {
                warehouseCode: '',
                warehouseName: '',
                city: '',
                address: ''
            })
        } else {
            setFormData(record ? {
                categoryCode: record.categoryCode,
                categoryName: record.categoryName,
                description: record.description || ''
            } : {
                categoryCode: '',
                categoryName: '',
                description: ''
            })
        }

        setMessage('')
        setShowForm(true)
    }

    // Update form values as the user types
    const handleInputChange = (event) => {
        const { name, value } = event.target

        setFormData(previous => ({
            ...previous,
            [name]: value
        }))
    }

    // Save a new record or update an existing one
    const handleSave = async (event) => {
        event.preventDefault()

        try {
            setSaving(true)
            setMessage('')

            const endpoint = formType === 'warehouse'
                ? '/warehouses'
                : '/item-categories'

            if (editingId) {
                await api.put(`${endpoint}/${editingId}`, formData)
            } else {
                await api.post(endpoint, formData)
            }

            setShowForm(false)
            await loadData()
        } catch (error) {
            console.error('Unable to save record:', error)
            setMessage(
                error.response?.data?.message ||
                'Unable to save. Check the required fields and API response.'
            )
        } finally {
            setSaving(false)
        }
    }

    // Deactivate a warehouse or category
    const handleDeactivate = async (type, id) => {
        const confirmed = window.confirm(
            'Are you sure you want to deactivate this record?'
        )

        if (!confirmed) return

        try {
            const endpoint = type === 'warehouse'
                ? '/warehouses'
                : '/item-categories'

            await api.patch(`${endpoint}/${id}/deactivate`)

            await loadData()
        } catch (error) {
            console.error('Unable to deactivate record:', error)
            alert(
                error.response?.data?.message ||
                'Unable to deactivate this record.'
            )
        }
    }

    const activeWarehouses = warehouses.filter(w => w.isActive).length
    const activeCategories = categories.filter(c => c.isActive).length

    const navigation = [
        {
            heading: 'WORKSPACE',
            links: [
                { name: 'Dashboard', icon: '◈' },
                { name: 'Warehouses', icon: '▤' },
                { name: 'Storage Locations', icon: '▦' }
            ]
        },
        {
            heading: 'INVENTORY',
            links: [
                { name: 'Items & Materials', icon: '□' },
                { name: 'Stock Overview', icon: '▧' },
                { name: 'Stock Transfers', icon: '⇄' }
            ]
        },
        {
            heading: 'MANUFACTURING',
            links: [
                { name: 'Production Orders', icon: '⚙' },
                { name: 'Bill of Materials', icon: '◫' }
            ]
        },
        {
            heading: 'CONFIGURATION',
            links: [
                { name: 'Item Categories', icon: '⌘' }
            ]
        }
    ]

    return (
        <div className="layout">

            <aside className="sidebar">
                <div className="brand">
                    <div className="brand-symbol">
                        <span>W</span>
                    </div>
                    <div>
                        <h2>WAREHOUSE</h2>
                        <p>MANAGEMENT SYSTEM</p>
                    </div>
                </div>

                <div className="navigation">
                    {navigation.map(group => (
                        <div key={group.heading}>
                            <span className="nav-heading">
                                {group.heading}
                            </span>

                            {group.links.map(link => (
                                <button
                                    key={link.name}
                                    className={`nav-link ${page === link.name ? 'selected' : ''}`}
                                    onClick={() => {
                                        setPage(link.name)
                                        setMessage('')
                                    }}
                                >
                                    <span className="nav-icon">{link.icon}</span>
                                    {link.name}
                                </button>
                            ))}
                        </div>
                    ))}
                </div>

                <div className="sidebar-bottom">
                    <div className="online-indicator"></div>
                    <div>
                        <strong>System status</strong>
                        <span>
                            {apiError ? 'API unavailable' :
                                loading ? 'Checking connection' : 'Operational'}
                        </span>
                    </div>
                </div>
            </aside>

            <main className="main-area">
                <header className="top-header">
                    <div className="breadcrumb">
                        Workspace <span>/</span> {page}
                    </div>

                    <div className="profile">
                        <div className="profile-avatar">A</div>
                        <div>
                            <strong>Administrator</strong>
                            <span>System administrator</span>
                        </div>
                    </div>
                </header>

                <div className="page-content">

                    {apiError && (
                        <div className="error-banner">
                            Unable to retrieve data. Check your backend API connection.
                        </div>
                    )}

                    {page === 'Dashboard' && (
                        <Dashboard
                            warehouses={warehouses}
                            activeWarehouses={activeWarehouses}
                            activeCategories={activeCategories}
                            loading={loading}
                            apiError={apiError}
                            setPage={setPage}
                        />
                    )}

                    {page === 'Warehouses' && (
                        <ManagementPage
                            title="Warehouse management"
                            subtitle="Manage your registered warehouse facilities."
                            records={warehouses}
                            type="warehouse"
                            loading={loading}
                            onAdd={() => openForm('warehouse')}
                            onEdit={(record) => openForm('warehouse', record)}
                            onDeactivate={(id) => handleDeactivate('warehouse', id)}
                        />
                    )}

                    {page === 'Item Categories' && (
                        <ManagementPage
                            title="Item categories"
                            subtitle="Organize materials and products into categories."
                            records={categories}
                            type="category"
                            loading={loading}
                            onAdd={() => openForm('category')}
                            onEdit={(record) => openForm('category', record)}
                            onDeactivate={(id) => handleDeactivate('category', id)}
                        />
                    )}

                    {!['Dashboard', 'Warehouses', 'Item Categories'].includes(page) && (
                        <section className="intro">
                            <div>
                                <div className="eyebrow">
                                    <span></span>
                                    MODULE NOT YET AVAILABLE
                                </div>
                                <h1>{page}</h1>
                                <p>
                                    This module will be connected as we build
                                    its backend functionality.
                                </p>
                                <button
                                    className="primary-button"
                                    onClick={() => setPage('Dashboard')}
                                >
                                    Return to dashboard
                                </button>
                            </div>
                        </section>
                    )}

                </div>
            </main>

            {showForm && (
                <div className="modal-overlay">
                    <div className="management-modal">
                        <div className="modal-heading">
                            <div>
                                <span className="eyebrow">
                                    {editingId ? 'UPDATE RECORD' : 'NEW RECORD'}
                                </span>
                                <h2>
                                    {editingId ? 'Edit ' : 'Add '}
                                    {formType === 'warehouse' ? 'warehouse' : 'item category'}
                                </h2>
                            </div>

                            <button
                                className="modal-close"
                                onClick={() => setShowForm(false)}
                                type="button"
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={handleSave}>
                            {formType === 'warehouse' ? (
                                <>
                                    <label>Warehouse code</label>
                                    <input
                                        name="warehouseCode"
                                        value={formData.warehouseCode}
                                        onChange={handleInputChange}
                                        required
                                        maxLength={30}
                                        placeholder="e.g. WH001"
                                    />

                                    <label>Warehouse name</label>
                                    <input
                                        name="warehouseName"
                                        value={formData.warehouseName}
                                        onChange={handleInputChange}
                                        required
                                        maxLength={150}
                                        placeholder="Enter warehouse name"
                                    />

                                    <label>City</label>
                                    <input
                                        name="city"
                                        value={formData.city}
                                        onChange={handleInputChange}
                                        placeholder="Enter city"
                                    />

                                    <label>Address</label>
                                    <textarea
                                        name="address"
                                        value={formData.address}
                                        onChange={handleInputChange}
                                        placeholder="Enter warehouse address"
                                    />
                                </>
                            ) : (
                                <>
                                    <label>Category code</label>
                                    <input
                                        name="categoryCode"
                                        value={formData.categoryCode}
                                        onChange={handleInputChange}
                                        required
                                        maxLength={30}
                                        placeholder="e.g. RAW"
                                    />

                                    <label>Category name</label>
                                    <input
                                        name="categoryName"
                                        value={formData.categoryName}
                                        onChange={handleInputChange}
                                        required
                                        maxLength={100}
                                        placeholder="Enter category name"
                                    />

                                    <label>Description</label>
                                    <textarea
                                        name="description"
                                        value={formData.description}
                                        onChange={handleInputChange}
                                        placeholder="Enter category description"
                                    />
                                </>
                            )}

                            {message && (
                                <div className="form-error">{message}</div>
                            )}

                            <div className="modal-actions">
                                <button
                                    type="button"
                                    className="secondary-button"
                                    onClick={() => setShowForm(false)}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="submit"
                                    className="primary-button"
                                    disabled={saving}
                                >
                                    {saving ? 'Saving...' : 'Save record'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

        </div>
    )
}

// Dashboard page
function Dashboard({
    warehouses,
    activeWarehouses,
    activeCategories,
    loading,
    apiError,
    setPage
}) {
    return (
        <>
            <section className="intro">
                <div>
                    <div className="eyebrow">
                        <span></span>
                        OPERATIONS OVERVIEW
                    </div>

                    <h1>Warehouse<br />control center.</h1>

                    <p>
                        Monitor your warehouses, inventory,
                        and manufacturing operations in one place.
                    </p>
                </div>

                <div className="date-panel">
                    <span>TODAY'S DATE</span>
                    <strong>
                        {new Date().toLocaleDateString('en-PH', {
                            day: '2-digit',
                            month: 'long',
                            year: 'numeric'
                        })}
                    </strong>
                </div>
            </section>

            <section className="section-block">
                <div className="section-title">
                    <div>
                        <span className="eyebrow">YOUR FACILITY</span>
                        <h2>Operational summary</h2>
                    </div>
                    <span className="live-label">
                        <i></i> LIVE DATA
                    </span>
                </div>

                <div className="metrics-grid">
                    <div className="metric">
                        <span className="metric-label">ACTIVE WAREHOUSES</span>
                        <div className="metric-value">
                            {loading ? '—' : activeWarehouses}
                            <span className="metric-icon">▤</span>
                        </div>
                        <p>Registered facilities</p>
                    </div>

                    <div className="metric">
                        <span className="metric-label">ITEM CATEGORIES</span>
                        <div className="metric-value">
                            {loading ? '—' : activeCategories}
                            <span className="metric-icon">◫</span>
                        </div>
                        <p>Active material groups</p>
                    </div>

                    <div className="metric">
                        <span className="metric-label">INVENTORY LOTS</span>
                        <div className="metric-value">—</div>
                        <p>Inventory module pending</p>
                    </div>

                    <div className="metric">
                        <span className="metric-label">PRODUCTION ORDERS</span>
                        <div className="metric-value">—</div>
                        <p>Manufacturing module pending</p>
                    </div>
                </div>
            </section>

            <section className="lower-section">
                <div className="panel facility-panel">
                    <div className="panel-heading">
                        <div>
                            <span className="eyebrow">FACILITY MANAGEMENT</span>
                            <h3>Warehouse directory</h3>
                        </div>
                        <button
                            className="text-button"
                            onClick={() => setPage('Warehouses')}
                        >
                            View all →
                        </button>
                    </div>

                    {loading ? (
                        <div className="empty-content">Loading warehouse data...</div>
                    ) : apiError ? (
                        <div className="empty-content">Warehouse data unavailable.</div>
                    ) : warehouses.length === 0 ? (
                        <div className="empty-content">
                            <span className="empty-symbol">▤</span>
                            <strong>No warehouses registered</strong>
                            <p>Your warehouse records will appear here.</p>
                            <button
                                className="text-button"
                                onClick={() => setPage('Warehouses')}
                            >
                                Register a warehouse →
                            </button>
                        </div>
                    ) : (
                        <div className="warehouse-list">
                            {warehouses.slice(0, 5).map(warehouse => (
                                <div className="warehouse-row" key={warehouse.warehouseId}>
                                    <div className="warehouse-symbol">W</div>
                                    <div className="warehouse-details">
                                        <strong>{warehouse.warehouseName}</strong>
                                        <span>
                                            {warehouse.warehouseCode} · {warehouse.city || 'Location not specified'}
                                        </span>
                                    </div>
                                    <span className={warehouse.isActive ? 'status-active' : 'status-inactive'}>
                                        {warehouse.isActive ? 'Active' : 'Inactive'}
                                    </span>
                                </div>
                            ))}
                        </div>
                    )}
                </div>

                <div className="panel system-panel">
                    <div className="panel-heading">
                        <div>
                            <span className="eyebrow">SYSTEM HEALTH</span>
                            <h3>Connection status</h3>
                        </div>
                    </div>

                    <div className="health-list">
                        <div className="health-row">
                            <span>React frontend</span>
                            <strong className="health-good">Running</strong>
                        </div>
                        <div className="health-row">
                            <span>ASP.NET Core API</span>
                            <strong className={apiError ? 'health-bad' : 'health-good'}>
                                {loading ? 'Checking' : apiError ? 'Unavailable' : 'Connected'}
                            </strong>
                        </div>
                        <div className="health-row">
                            <span>PostgreSQL</span>
                            <strong className={apiError ? 'health-bad' : 'health-good'}>
                                {loading ? 'Checking' : apiError ? 'Unverified' : 'API accessible'}
                            </strong>
                        </div>
                    </div>

                    <div className="system-note">
                        <span>i</span>
                        Database status is inferred from successful API requests.
                    </div>
                </div>
            </section>
        </>
    )
}

// Reusable warehouse/category management page
function ManagementPage({
    title,
    subtitle,
    records,
    type,
    loading,
    onAdd,
    onEdit,
    onDeactivate
}) {
    const isWarehouse = type === 'warehouse'

    return (
        <section className="management-page">
            <div className="management-page-heading">
                <div>
                    <span className="eyebrow">MASTER DATA</span>
                    <h1>{title}</h1>
                    <p>{subtitle}</p>
                </div>

                <button className="primary-button" onClick={onAdd}>
                    + Add {isWarehouse ? 'warehouse' : 'category'}
                </button>
            </div>

            <div className="management-summary">
                <div>
                    <span>TOTAL RECORDS</span>
                    <strong>{loading ? '—' : records.length}</strong>
                </div>
                <div>
                    <span>ACTIVE RECORDS</span>
                    <strong>
                        {loading ? '—' : records.filter(r => r.isActive).length}
                    </strong>
                </div>
            </div>

            <div className="panel management-table-panel">
                {loading ? (
                    <div className="empty-content">Loading records...</div>
                ) : records.length === 0 ? (
                    <div className="empty-content">
                        <strong>No records found</strong>
                        <p>Click the Add button to create your first record.</p>
                    </div>
                ) : (
                    <div className="table-scroll">
                        <table className="management-table">
                            <thead>
                                <tr>
                                    <th>CODE</th>
                                    <th>NAME</th>
                                    {isWarehouse && <th>CITY</th>}
                                    {!isWarehouse && <th>DESCRIPTION</th>}
                                    <th>STATUS</th>
                                    <th>ACTIONS</th>
                                </tr>
                            </thead>
                            <tbody>
                                {records.map(record => (
                                    <tr key={isWarehouse ? record.warehouseId : record.categoryId}>
                                        <td>
                                            {isWarehouse ? record.warehouseCode : record.categoryCode}
                                        </td>
                                        <td>
                                            {isWarehouse ? record.warehouseName : record.categoryName}
                                        </td>
                                        <td>
                                            {isWarehouse
                                                ? record.city || '—'
                                                : record.description || '—'}
                                        </td>
                                        <td>
                                            <span className={record.isActive ? 'status-active' : 'status-inactive'}>
                                                {record.isActive ? 'Active' : 'Inactive'}
                                            </span>
                                        </td>
                                        <td>
                                            <div className="table-actions">
                                                <button
                                                    className="edit-button"
                                                    onClick={() => onEdit(record)}
                                                >
                                                    Edit
                                                </button>

                                                {record.isActive && (
                                                    <button
                                                        className="deactivate-button"
                                                        onClick={() => onDeactivate(
                                                            isWarehouse
                                                                ? record.warehouseId
                                                                : record.categoryId
                                                        )}
                                                    >
                                                        Deactivate
                                                    </button>
                                                )}
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>
        </section>
    )
}

export default App
