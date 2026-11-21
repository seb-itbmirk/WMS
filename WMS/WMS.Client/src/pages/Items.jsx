import { useEffect, useMemo, useState } from 'react'
import api from '../services/api'

export default function ItemsPage() {
    const [items, setItems] = useState([])
    const [categories, setCategories] = useState([])
    const [loading, setLoading] = useState(true)
    const [itemsLoadError, setItemsLoadError] = useState(false)
    const [error, setError] = useState('')
    const [success, setSuccess] = useState('')

    const [search, setSearch] = useState('')
    const [categoryFilter, setCategoryFilter] = useState('ALL')
    const [statusFilter, setStatusFilter] = useState('ALL')

    async function loadData() {
        try {
            setLoading(true)
            setError('')
            setItemsLoadError(false)

            const results = await Promise.allSettled([
                api.get('/items'),
                api.get('/item-categories')
            ])

            const [itemsResult, categoriesResult] = results
            const loadErrors = []

            if (itemsResult.status === 'fulfilled') {
                setItems(itemsResult.value.data)
                setItemsLoadError(false)
            } else {
                setItemsLoadError(true)
                console.error('Unable to load items:', itemsResult.reason)

                loadErrors.push(
                    itemsResult.reason.response?.data?.message ||
                    itemsResult.reason.response?.data?.title ||
                    'Failed to load items.'
                )
            }

            if (categoriesResult.status === 'fulfilled') {
                setCategories(categoriesResult.value.data)
            } else {
                console.error(
                    'Unable to load item categories:',
                    categoriesResult.reason
                )

                loadErrors.push(
                    categoriesResult.reason.response?.data?.message ||
                    categoriesResult.reason.response?.data?.title ||
                    'Failed to load item categories.'
                )
            }

            if (loadErrors.length > 0) {
                setError(loadErrors.join(' '))
            }
        } finally {
            setLoading(false)
        }
    }

    useEffect(() => {
        loadData()
    }, [])

    const filteredItems = useMemo(() => {
        return items.filter(item => {
            const searchValue = search.trim().toLowerCase()

            const matchesSearch =
                !searchValue ||
                item.itemCode?.toLowerCase().includes(searchValue) ||
                item.itemName?.toLowerCase().includes(searchValue) ||
                item.description?.toLowerCase().includes(searchValue)

            const matchesCategory =
                categoryFilter === 'ALL' ||
                String(item.categoryId) === categoryFilter

            const matchesStatus =
                statusFilter === 'ALL' ||
                (statusFilter === 'ACTIVE' && item.isActive) ||
                (statusFilter === 'INACTIVE' && !item.isActive)

            return matchesSearch && matchesCategory && matchesStatus
        })
    }, [items, search, categoryFilter, statusFilter])

    const activeCount = items.filter(item => item.isActive).length
    const inactiveCount = items.length - activeCount

    async function deactivateItem(item) {
        const confirmed = window.confirm(
            `Deactivate ${item.itemCode} - ${item.itemName}?`
        )

        if (!confirmed) return

        try {
            setError('')
            setSuccess('')

            await api.patch(`/items/${item.itemId}/deactivate`)

            setSuccess(`${item.itemCode} was deactivated successfully.`)
            await loadData()
        } catch (err) {
            setError(
                err.response?.data?.message ||
                err.response?.data?.title ||
                'Failed to deactivate item.'
            )
        }
    }

    return (
        <div className="items-page">
            <div className="items-heading">
                <div>
                    <p className="items-eyebrow">MASTER DATA MANAGEMENT</p>
                    <h1>Items & Materials</h1>
                    <p>
                        Manage raw materials, packaging materials, and finished goods.
                    </p>
                </div>
            </div>

            {error && (
                <div className="items-alert items-error">
                    {error}
                    <button onClick={() => setError('')}>×</button>
                </div>
            )}

            {success && (
                <div className="items-alert items-success">
                    {success}
                    <button onClick={() => setSuccess('')}>×</button>
                </div>
            )}

            <div className="items-summary">
                <div className="items-summary-card">
                    <span>TOTAL ITEMS</span>
                    <strong>{items.length}</strong>
                    <small>All catalog records</small>
                </div>

                <div className="items-summary-card">
                    <span>ACTIVE ITEMS</span>
                    <strong>{activeCount}</strong>
                    <small>Available for operations</small>
                </div>

                <div className="items-summary-card">
                    <span>INACTIVE ITEMS</span>
                    <strong>{inactiveCount}</strong>
                    <small>Deactivated records</small>
                </div>

                <div className="items-summary-card">
                    <span>CATEGORIES</span>
                    <strong>{categories.length}</strong>
                    <small>Configured item categories</small>
                </div>
            </div>

            <section className="items-panel">
                <div className="items-panel-heading">
                    <div>
                        <h2>Item Catalog</h2>
                        <p>{filteredItems.length} matching records</p>
                    </div>

                    <button
                        className="items-refresh-button"
                        onClick={loadData}
                    >
                        ↻ Refresh
                    </button>
                </div>

                <div className="items-filters">
                    <div className="items-search">
                        <label htmlFor="item-search">Search items</label>
                        <input
                            id="item-search"
                            value={search}
                            onChange={event => setSearch(event.target.value)}
                            placeholder="Search item code, name, or description..."
                        />
                    </div>

                    <div>
                        <label htmlFor="item-category-filter">Category</label>
                        <select
                            id="item-category-filter"
                            value={categoryFilter}
                            onChange={event => setCategoryFilter(event.target.value)}
                        >
                            <option value="ALL">All categories</option>

                            {categories.map(category => (
                                <option
                                    key={category.categoryId}
                                    value={category.categoryId}
                                >
                                    {category.categoryCode
                                        ? `${category.categoryCode} - ${category.categoryName}`
                                        : category.categoryName}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div>
                        <label htmlFor="item-status-filter">Status</label>
                        <select
                            id="item-status-filter"
                            value={statusFilter}
                            onChange={event => setStatusFilter(event.target.value)}
                        >
                            <option value="ALL">All statuses</option>
                            <option value="ACTIVE">Active</option>
                            <option value="INACTIVE">Inactive</option>
                        </select>
                    </div>
                </div>

                <div className="items-table-wrapper">
                    <table className="items-table">
                        <thead>
                            <tr>
                                <th>ITEM CODE</th>
                                <th>ITEM NAME</th>
                                <th>CATEGORY</th>
                                <th>BASE UOM</th>
                                <th>STOCK ROTATION</th>
                                <th>EXPIRY</th>
                                <th>QUALITY CHECK</th>
                                <th>STATUS</th>
                                <th>ACTION</th>
                            </tr>
                        </thead>

                        <tbody>
                            {loading ? (
                                <tr>
                                    <td colSpan="9" className="items-empty">
                                        Loading item catalog...
                                    </td>
                                </tr>
                            ) : itemsLoadError ? (
                                <tr>
                                    <td colSpan="9" className="items-empty">
                                        Unable to load item records. Check the error
                                        message above and refresh.
                                    </td>
                                </tr>
                            ) : filteredItems.length === 0 ? (
                                <tr>
                                    <td colSpan="9" className="items-empty">
                                        No matching items found.
                                    </td>
                                </tr>
                            ) : (
                                filteredItems.map(item => (
                                    <tr key={item.itemId}>
                                        <td>
                                            <strong>{item.itemCode}</strong>
                                        </td>

                                        <td>
                                            <div className="items-name">
                                                <strong>{item.itemName}</strong>

                                                {item.description && (
                                                    <small>{item.description}</small>
                                                )}
                                            </div>
                                        </td>

                                        <td>{item.categoryName || '—'}</td>
                                        <td>{item.uomCode || '—'}</td>
                                        <td>{item.stockRotationMethod || '—'}</td>

                                        <td>
                                            {item.requiresExpiry ? 'Required' : 'No'}
                                        </td>

                                        <td>
                                            {item.requiresQualityInspection
                                                ? 'Required'
                                                : 'No'}
                                        </td>

                                        <td>
                                            <span
                                                className={`items-status ${
                                                    item.isActive
                                                        ? 'active'
                                                        : 'inactive'
                                                }`}
                                            >
                                                {item.isActive ? 'Active' : 'Inactive'}
                                            </span>
                                        </td>

                                        <td>
                                            {item.isActive ? (
                                                <button
                                                    className="items-deactivate-button"
                                                    onClick={() => deactivateItem(item)}
                                                >
                                                    Deactivate
                                                </button>
                                            ) : (
                                                <span className="items-no-action">
                                                    —
                                                </span>
                                            )}
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