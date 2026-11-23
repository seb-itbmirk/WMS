
import { useEffect, useMemo, useState } from 'react'
import api from '../../services/api'
import './replenishment.css'

function ReplenishmentPage() {
    const [items, setItems] = useState([])
    const [balances, setBalances] = useState([])
    const [loading, setLoading] = useState(true)
    const [refreshing, setRefreshing] = useState(false)
    const [error, setError] = useState('')
    const [searchTerm, setSearchTerm] = useState('')
    const [statusFilter, setStatusFilter] = useState('NEEDS_REPLENISHMENT')

    const getErrorMessage = (err) =>
        err.response?.data?.message ||
        err.response?.data?.title ||
        err.message ||
        'Unable to load item and inventory data.'

    const loadData = async (isRefresh = false) => {
        try {
            if (isRefresh) {
                setRefreshing(true)
            } else {
                setLoading(true)
            }

            setError('')

            const [itemsResponse, balancesResponse] = await Promise.all([
                api.get('/items'),
                api.get('/inventory-balances')
            ])

            setItems(
                Array.isArray(itemsResponse.data)
                    ? itemsResponse.data
                    : []
            )

            setBalances(
                Array.isArray(balancesResponse.data)
                    ? balancesResponse.data
                    : []
            )
        } catch (err) {
            console.error('Unable to load replenishment data:', err)
            setError(getErrorMessage(err))
        } finally {
            setLoading(false)
            setRefreshing(false)
        }
    }

    useEffect(() => {
        loadData()
    }, [])

    const replenishmentItems = useMemo(() => {
        const availableByItem = new Map()

        balances.forEach(balance => {
            if (
                String(balance.stockStatus || '').toUpperCase() !==
                'AVAILABLE'
            ) {
                return
            }

            const itemId = String(balance.itemId)

            const availableQuantity = Number(
                balance.availableQuantity ??
                (
                    Number(balance.quantityOnHand || 0) -
                    Number(balance.quantityReserved || 0)
                )
            )

            availableByItem.set(
                itemId,
                (availableByItem.get(itemId) || 0) +
                (Number.isFinite(availableQuantity) ? availableQuantity : 0)
            )
        })

        return items
            .filter(item => item.isActive)
            .map(item => {
                const availableQuantity =
                    availableByItem.get(String(item.itemId)) || 0

                const reorderLevel = Number(item.reorderLevel || 0)
                const hasReorderLevel = reorderLevel > 0

                const suggestedQuantity = hasReorderLevel
                    ? Math.max(reorderLevel - availableQuantity, 0)
                    : 0

                return {
                    ...item,
                    availableQuantity,
                    reorderLevel,
                    hasReorderLevel,
                    suggestedQuantity,
                    needsReplenishment:
                        hasReorderLevel &&
                        availableQuantity <= reorderLevel
                }
            })
            .sort((a, b) => {
                if (a.needsReplenishment !== b.needsReplenishment) {
                    return a.needsReplenishment ? -1 : 1
                }

                return (a.itemCode || '').localeCompare(b.itemCode || '')
            })
    }, [items, balances])

    const filteredItems = useMemo(() => {
        const term = searchTerm.trim().toLowerCase()

        return replenishmentItems.filter(item => {
            const matchesSearch = [
                item.itemCode,
                item.itemName,
                item.categoryName
            ].some(value =>
                String(value || '').toLowerCase().includes(term)
            )

            const matchesStatus =
                statusFilter === 'ALL' ||
                (
                    statusFilter === 'NEEDS_REPLENISHMENT' &&
                    item.needsReplenishment
                ) ||
                (
                    statusFilter === 'NO_REORDER_LEVEL' &&
                    !item.hasReorderLevel
                )

            return matchesSearch && matchesStatus
        })
    }, [replenishmentItems, searchTerm, statusFilter])

    const itemsNeedingReplenishment = replenishmentItems.filter(
        item => item.needsReplenishment
    )

    const totalSuggestedQuantity = itemsNeedingReplenishment.reduce(
        (total, item) => total + item.suggestedQuantity,
        0
    )

    const itemsWithoutReorderLevel = replenishmentItems.filter(
        item => !item.hasReorderLevel
    ).length

    const formatQuantity = (value) =>
        Number(value || 0).toLocaleString(undefined, {
            minimumFractionDigits: 0,
            maximumFractionDigits: 3
        })

    const getStockStatus = (item) => {
        if (!item.hasReorderLevel) {
            return {
                label: 'Not configured',
                className: 'rp-status-unconfigured'
            }
        }

        if (item.availableQuantity <= 0) {
            return {
                label: 'Out of stock',
                className: 'rp-status-out'
            }
        }

        if (item.needsReplenishment) {
            return {
                label: 'Reorder required',
                className: 'rp-status-low'
            }
        }

        return {
            label: 'Stock sufficient',
            className: 'rp-status-healthy'
        }
    }

    return (
        <section className="rp-page">

            <div className="rp-header">
                <div>
                    <span className="rp-eyebrow">
                        INVENTORY MANAGEMENT
                    </span>

                    <h1>Inventory Replenishment</h1>

                    <p>
                        Identify active items whose available stock has reached
                        or fallen below their configured reorder level.
                    </p>
                </div>

                <button
                    type="button"
                    className="rp-refresh-button"
                    onClick={() => loadData(true)}
                    disabled={loading || refreshing}
                >
                    {refreshing ? 'Refreshing...' : '↻ Refresh'}
                </button>
            </div>

            <div className="rp-notice">
                <strong>Recommendation only</strong>
                <span>
                    Suggested quantities are calculated to bring available
                    stock back up to the item's reorder level. This screen
                    does not create purchase orders or change inventory.
                </span>
            </div>

            {error && (
                <div className="rp-error">
                    {error}
                </div>
            )}

            <div className="rp-summary">

                <article className="rp-summary-card">
                    <span>Total Active Items</span>
                    <strong>
                        {loading ? '—' : replenishmentItems.length}
                    </strong>
                </article>

                <article className="rp-summary-card rp-summary-warning">
                    <span>Need Replenishment</span>
                    <strong>
                        {loading ? '—' : itemsNeedingReplenishment.length}
                    </strong>
                </article>

                <article className="rp-summary-card">
                    <span>Suggested Top-Up</span>
                    <strong>
                        {loading ? '—' : formatQuantity(totalSuggestedQuantity)}
                    </strong>
                </article>

                <article className="rp-summary-card">
                    <span>No Reorder Level</span>
                    <strong>
                        {loading ? '—' : itemsWithoutReorderLevel}
                    </strong>
                </article>

            </div>

            <div className="rp-panel">

                <div className="rp-toolbar">
                    <div>
                        <h2>Replenishment Review</h2>
                        <p>
                            {loading
                                ? 'Loading item and stock records...'
                                : `Showing ${filteredItems.length} of ${replenishmentItems.length} items`}
                        </p>
                    </div>

                    <div className="rp-filters">
                        <input
                            type="search"
                            value={searchTerm}
                            onChange={event =>
                                setSearchTerm(event.target.value)
                            }
                            placeholder="Search items..."
                        />

                        <select
                            value={statusFilter}
                            onChange={event =>
                                setStatusFilter(event.target.value)
                            }
                        >
                            <option value="NEEDS_REPLENISHMENT">
                                Needs Replenishment
                            </option>
                            <option value="ALL">
                                All Active Items
                            </option>
                            <option value="NO_REORDER_LEVEL">
                                No Reorder Level
                            </option>
                        </select>
                    </div>
                </div>

                <div className="rp-table-wrapper">
                    <table className="rp-table">

                        <thead>
                            <tr>
                                <th>Item Code</th>
                                <th>Item Name</th>
                                <th>Category</th>
                                <th>Available Stock</th>
                                <th>Reorder Level</th>
                                <th>Suggested Top-Up</th>
                                <th>Status</th>
                            </tr>
                        </thead>

                        <tbody>
                            {loading ? (
                                <tr>
                                    <td colSpan="7" className="rp-empty">
                                        Loading replenishment data...
                                    </td>
                                </tr>
                            ) : filteredItems.length === 0 ? (
                                <tr>
                                    <td colSpan="7" className="rp-empty">
                                        No matching items found.
                                    </td>
                                </tr>
                            ) : (
                                filteredItems.map(item => {
                                    const status = getStockStatus(item)

                                    return (
                                        <tr key={item.itemId}>
                                            <td>
                                                <strong>{item.itemCode}</strong>
                                            </td>

                                            <td>{item.itemName}</td>

                                            <td>
                                                {item.categoryName || '—'}
                                            </td>

                                            <td>
                                                {formatQuantity(item.availableQuantity)}
                                                {' '}
                                                {item.uomCode || ''}
                                            </td>

                                            <td>
                                                {item.hasReorderLevel
                                                    ? formatQuantity(item.reorderLevel)
                                                    : '—'}
                                            </td>

                                            <td>
                                                {item.hasReorderLevel
                                                    ? formatQuantity(item.suggestedQuantity)
                                                    : '—'}
                                            </td>

                                            <td>
                                                <span
                                                    className={`rp-status ${status.className}`}
                                                >
                                                    {status.label}
                                                </span>
                                            </td>
                                        </tr>
                                    )
                                })
                            )}
                        </tbody>

                    </table>
                </div>

            </div>

        </section>
    )
}

export default ReplenishmentPage
