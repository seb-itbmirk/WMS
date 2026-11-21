
import { useCallback, useEffect, useMemo, useState } from 'react'
import api from '../services/api'

const numberFormat = new Intl.NumberFormat('en-PH', {
    maximumFractionDigits: 2
})

const formatNumber = value => numberFormat.format(Number(value) || 0)

const formatDate = value => {
    if (!value) return '—'

    const date = new Date(value)

    if (Number.isNaN(date.getTime())) return '—'

    return date.toLocaleDateString('en-PH', {
        day: '2-digit',
        month: 'short',
        year: 'numeric'
    })
}

const getArray = result => {
    if (result.status !== 'fulfilled') return []

    return Array.isArray(result.value.data)
        ? result.value.data
        : []
}

function ReportsPage() {
    const [data, setData] = useState({
        inventory: [],
        productionOrders: [],
        goodsReceipts: [],
        purchaseOrders: [],
        stockTransfers: [],
        stockAdjustments: [],
        stockCounts: []
    })

    const [loading, setLoading] = useState(true)
    const [loadErrors, setLoadErrors] = useState([])
    const [lastUpdated, setLastUpdated] = useState(null)

    const loadReports = useCallback(async () => {
        setLoading(true)

        const endpoints = [
            { key: 'inventory', url: '/inventory-balances', label: 'Inventory balances' },
            { key: 'productionOrders', url: '/production-orders', label: 'Production orders' },
            { key: 'goodsReceipts', url: '/goods-receipts', label: 'Goods receipts' },
            { key: 'purchaseOrders', url: '/purchase-orders', label: 'Purchase orders' },
            { key: 'stockTransfers', url: '/stock-transfers', label: 'Stock transfers' },
            { key: 'stockAdjustments', url: '/stock-adjustments', label: 'Stock adjustments' },
            { key: 'stockCounts', url: '/stock-counts', label: 'Stock counts' }
        ]

        try {
            const results = await Promise.allSettled(
                endpoints.map(endpoint => api.get(endpoint.url))
            )

            const nextData = {}
            const errors = []

            endpoints.forEach((endpoint, index) => {
                const result = results[index]
                nextData[endpoint.key] = getArray(result)

                if (result.status === 'rejected') {
                    errors.push(endpoint.label)
                    console.error(
                        `Unable to load ${endpoint.label}:`,
                        result.reason
                    )
                }
            })

            setData(nextData)
            setLoadErrors(errors)
            setLastUpdated(new Date())
        } finally {
            setLoading(false)
        }
    }, [])

    useEffect(() => {
        loadReports()
    }, [loadReports])

    const inventorySummary = useMemo(() => {
        const summary = data.inventory.reduce((result, balance) => {
            const onHand = Number(balance.quantityOnHand) || 0
            const reserved = Number(balance.quantityReserved) || 0
            const available = Number(
                balance.availableQuantity ?? (onHand - reserved)
            )

            result.quantityOnHand += onHand
            result.quantityReserved += reserved
            result.availableQuantity += available

            return result
        }, {
            quantityOnHand: 0,
            quantityReserved: 0,
            availableQuantity: 0
        })

        return {
            ...summary,
            balanceRecords: data.inventory.length,
            uniqueItems: new Set(
                data.inventory.map(balance => balance.itemId)
            ).size,
            uniqueLots: new Set(
                data.inventory.map(balance => balance.lotId)
            ).size
        }
    }, [data.inventory])

    const warehouseSummary = useMemo(() => {
        const grouped = new Map()

        data.inventory.forEach(balance => {
            const key = balance.warehouseId ?? 'unknown'
            const name = balance.warehouseName || 'Unassigned warehouse'

            if (!grouped.has(key)) {
                grouped.set(key, {
                    warehouseId: key,
                    warehouseName: name,
                    quantityOnHand: 0,
                    quantityReserved: 0,
                    availableQuantity: 0,
                    balanceRecords: 0
                })
            }

            const warehouse = grouped.get(key)
            const onHand = Number(balance.quantityOnHand) || 0
            const reserved = Number(balance.quantityReserved) || 0
            const available = Number(
                balance.availableQuantity ?? (onHand - reserved)
            )

            warehouse.quantityOnHand += onHand
            warehouse.quantityReserved += reserved
            warehouse.availableQuantity += available
            warehouse.balanceRecords += 1
        })

        return Array.from(grouped.values())
            .sort((a, b) => b.quantityOnHand - a.quantityOnHand)
    }, [data.inventory])

    const stockStatusSummary = useMemo(() => {
        const grouped = new Map()

        data.inventory.forEach(balance => {
            const status = balance.stockStatus || 'UNSPECIFIED'
            const quantity = Number(balance.quantityOnHand) || 0

            grouped.set(
                status,
                (grouped.get(status) || 0) + quantity
            )
        })

        return Array.from(grouped.entries())
            .map(([status, quantity]) => ({ status, quantity }))
            .sort((a, b) => b.quantity - a.quantity)
    }, [data.inventory])

    const productionSummary = useMemo(() => {
        const grouped = new Map()

        data.productionOrders.forEach(order => {
            const status = order.productionStatus || 'UNSPECIFIED'
            grouped.set(status, (grouped.get(status) || 0) + 1)
        })

        return Array.from(grouped.entries())
            .map(([status, count]) => ({ status, count }))
            .sort((a, b) => b.count - a.count)
    }, [data.productionOrders])

    const recentProductionOrders = useMemo(() => {
        return [...data.productionOrders]
            .sort((a, b) =>
                new Date(b.createdAt || 0) - new Date(a.createdAt || 0)
            )
            .slice(0, 6)
    }, [data.productionOrders])

    const activitySummary = [
        {
            label: 'Purchase Orders',
            count: data.purchaseOrders.length
        },
        {
            label: 'Goods Receipts',
            count: data.goodsReceipts.length
        },
        {
            label: 'Stock Transfers',
            count: data.stockTransfers.length
        },
        {
            label: 'Stock Adjustments',
            count: data.stockAdjustments.length
        },
        {
            label: 'Stock Counts',
            count: data.stockCounts.length
        }
    ]

    const maxWarehouseQuantity = Math.max(
        ...warehouseSummary.map(item => item.quantityOnHand),
        1
    )

    const maxStatusQuantity = Math.max(
        ...stockStatusSummary.map(item => item.quantity),
        1
    )

    return (
        <section className="reports-page">

            <div className="reports-header">
                <div>
                    <span className="eyebrow">SYSTEM INSIGHTS</span>
                    <h1>Reports &amp; Analytics</h1>
                    <p>
                        Review inventory, manufacturing, and warehouse activity
                        using existing WMS records.
                    </p>
                </div>

                <div className="reports-header-actions">
                    {lastUpdated && (
                        <span className="reports-updated">
                            Updated {lastUpdated.toLocaleTimeString('en-PH')}
                        </span>
                    )}

                    <button
                        type="button"
                        className="primary-button"
                        onClick={loadReports}
                        disabled={loading}
                    >
                        {loading ? 'Refreshing...' : '↻ Refresh Reports'}
                    </button>
                </div>
            </div>

            {loadErrors.length > 0 && (
                <div className="reports-warning">
                    Some reports could not be loaded:
                    {' '}
                    {loadErrors.join(', ')}.
                    Other available reports are still displayed.
                </div>
            )}

            <div className="reports-section-heading">
                <div>
                    <span className="eyebrow">INVENTORY</span>
                    <h2>Inventory summary</h2>
                </div>
                <span className="reports-source-label">
                    Source: Inventory Balances
                </span>
            </div>

            <div className="reports-metrics-grid">

                <article className="reports-metric-card">
                    <span>Total Quantity on Hand</span>
                    <strong>
                        {loading ? '—' : formatNumber(inventorySummary.quantityOnHand)}
                    </strong>
                    <small>Recorded physical stock quantity</small>
                </article>

                <article className="reports-metric-card">
                    <span>Reserved Quantity</span>
                    <strong>
                        {loading ? '—' : formatNumber(inventorySummary.quantityReserved)}
                    </strong>
                    <small>Quantity currently reserved</small>
                </article>

                <article className="reports-metric-card">
                    <span>Available Quantity</span>
                    <strong>
                        {loading ? '—' : formatNumber(inventorySummary.availableQuantity)}
                    </strong>
                    <small>On hand less reserved quantity</small>
                </article>

                <article className="reports-metric-card">
                    <span>Inventory Lots</span>
                    <strong>
                        {loading ? '—' : formatNumber(inventorySummary.uniqueLots)}
                    </strong>
                    <small>Distinct lots in inventory balances</small>
                </article>

            </div>

            <div className="reports-two-column">

                <article className="reports-panel">
                    <div className="reports-panel-heading">
                        <div>
                            <h3>Inventory by Warehouse</h3>
                            <p>Quantity currently recorded per warehouse.</p>
                        </div>
                    </div>

                    {loading ? (
                        <div className="reports-empty">Loading inventory...</div>
                    ) : warehouseSummary.length === 0 ? (
                        <div className="reports-empty">
                            No inventory balance records found.
                        </div>
                    ) : (
                        <div className="reports-chart-list">
                            {warehouseSummary.map(warehouse => (
                                <div
                                    className="reports-chart-row"
                                    key={warehouse.warehouseId}
                                >
                                    <div className="reports-chart-label">
                                        <span>{warehouse.warehouseName}</span>
                                        <strong>
                                            {formatNumber(warehouse.quantityOnHand)}
                                        </strong>
                                    </div>

                                    <div className="reports-bar-track">
                                        <div
                                            className="reports-bar-fill"
                                            style={{
                                                width: `${
                                                    warehouse.quantityOnHand /
                                                    maxWarehouseQuantity * 100
                                                }%`
                                            }}
                                        />
                                    </div>

                                    <small>
                                        Available: {formatNumber(warehouse.availableQuantity)}
                                        {' · '}
                                        Reserved: {formatNumber(warehouse.quantityReserved)}
                                    </small>
                                </div>
                            ))}
                        </div>
                    )}
                </article>

                <article className="reports-panel">
                    <div className="reports-panel-heading">
                        <div>
                            <h3>Stock Status Breakdown</h3>
                            <p>Quantity grouped by the recorded stock status.</p>
                        </div>
                    </div>

                    {loading ? (
                        <div className="reports-empty">Loading stock status...</div>
                    ) : stockStatusSummary.length === 0 ? (
                        <div className="reports-empty">
                            No stock status records found.
                        </div>
                    ) : (
                        <div className="reports-chart-list">
                            {stockStatusSummary.map(item => (
                                <div
                                    className="reports-chart-row"
                                    key={item.status}
                                >
                                    <div className="reports-chart-label">
                                        <span>{item.status.replaceAll('_', ' ')}</span>
                                        <strong>{formatNumber(item.quantity)}</strong>
                                    </div>

                                    <div className="reports-bar-track">
                                        <div
                                            className="reports-bar-fill reports-bar-secondary"
                                            style={{
                                                width: `${
                                                    item.quantity /
                                                    maxStatusQuantity * 100
                                                }%`
                                            }}
                                        />
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </article>

            </div>

            <div className="reports-section-heading">
                <div>
                    <span className="eyebrow">MANUFACTURING</span>
                    <h2>Production overview</h2>
                </div>
                <span className="reports-source-label">
                    Source: Production Orders
                </span>
            </div>

            <div className="reports-two-column">

                <article className="reports-panel">
                    <div className="reports-panel-heading">
                        <div>
                            <h3>Production Order Status</h3>
                            <p>Number of production orders per current status.</p>
                        </div>
                    </div>

                    {loading ? (
                        <div className="reports-empty">Loading production orders...</div>
                    ) : productionSummary.length === 0 ? (
                        <div className="reports-empty">
                            No production orders found.
                        </div>
                    ) : (
                        <div className="reports-status-list">
                            {productionSummary.map(item => (
                                <div
                                    className="reports-status-row"
                                    key={item.status}
                                >
                                    <span className="reports-status-name">
                                        {item.status.replaceAll('_', ' ')}
                                    </span>
                                    <strong>{item.count}</strong>
                                </div>
                            ))}
                        </div>
                    )}

                    <div className="reports-total-row">
                        <span>Total Production Orders</span>
                        <strong>
                            {loading ? '—' : data.productionOrders.length}
                        </strong>
                    </div>
                </article>

                <article className="reports-panel">
                    <div className="reports-panel-heading">
                        <div>
                            <h3>Warehouse Activity</h3>
                            <p>Existing operational records by module.</p>
                        </div>
                    </div>

                    <div className="reports-activity-list">
                        {activitySummary.map(item => (
                            <div
                                className="reports-activity-row"
                                key={item.label}
                            >
                                <span>{item.label}</span>
                                <strong>{loading ? '—' : item.count}</strong>
                            </div>
                        ))}
                    </div>
                </article>

            </div>

            <article className="reports-panel reports-recent-panel">
                <div className="reports-panel-heading">
                    <div>
                        <span className="eyebrow">LATEST RECORDS</span>
                        <h3>Recent Production Orders</h3>
                        <p>Most recently created production orders.</p>
                    </div>
                </div>

                {loading ? (
                    <div className="reports-empty">
                        Loading production order history...
                    </div>
                ) : recentProductionOrders.length === 0 ? (
                    <div className="reports-empty">
                        No production order records found.
                    </div>
                ) : (
                    <div className="reports-table-wrap">
                        <table className="reports-table">
                            <thead>
                                <tr>
                                    <th>Production Number</th>
                                    <th>Finished Item</th>
                                    <th>Warehouse</th>
                                    <th>Planned Quantity</th>
                                    <th>Status</th>
                                    <th>Created</th>
                                </tr>
                            </thead>

                            <tbody>
                                {recentProductionOrders.map(order => (
                                    <tr key={order.productionOrderId}>
                                        <td>
                                            <strong>{order.productionNumber}</strong>
                                        </td>
                                        <td>
                                            <strong>{order.finishedItemCode}</strong>
                                            <small>{order.finishedItemName}</small>
                                        </td>
                                        <td>{order.warehouseName || '—'}</td>
                                        <td>
                                            {formatNumber(order.plannedQuantity)}
                                            {' '}
                                            {order.uomCode || ''}
                                        </td>
                                        <td>
                                            <span className="reports-order-status">
                                                {(order.productionStatus || 'UNSPECIFIED')
                                                    .replaceAll('_', ' ')}
                                            </span>
                                        </td>
                                        <td>{formatDate(order.createdAt)}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </article>

            <p className="reports-disclaimer">
                These reports are calculated from existing inventory and
                operational records. They do not represent customer sales,
                shipments, or deliveries because those records are not part
                of the current database.
            </p>

        </section>
    )
}

export default ReportsPage
