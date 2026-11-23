import { useEffect, useMemo, useState } from 'react'
import api from '../../services/api'

export default function InventoryOverviewPage() {
    const [balances, setBalances] = useState([])
    const [warehouses, setWarehouses] = useState([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState('')

    const [search, setSearch] = useState('')
    const [warehouseFilter, setWarehouseFilter] = useState('ALL')
    const [statusFilter, setStatusFilter] = useState('ALL')

    async function loadData() {
        try {
            setLoading(true)
            setError('')

            const [balanceResponse, warehouseResponse] = await Promise.all([
                api.get('/inventory-balances'),
                api.get('/warehouses')
            ])

            setBalances(balanceResponse.data)
            setWarehouses(warehouseResponse.data)
        } catch (err) {
            setError(
                err.response?.data?.message ||
                err.response?.data?.title ||
                'Failed to load inventory balances.'
            )
        } finally {
            setLoading(false)
        }
    }

    useEffect(() => {
        loadData()
    }, [])

    const filteredBalances = useMemo(() => {
        return balances.filter(balance => {
            const term = search.trim().toLowerCase()

            const matchesSearch =
                !term ||
                balance.itemCode?.toLowerCase().includes(term) ||
                balance.itemName?.toLowerCase().includes(term) ||
                balance.batchCode?.toLowerCase().includes(term) ||
                balance.binCode?.toLowerCase().includes(term)

            const matchesWarehouse =
                warehouseFilter === 'ALL' ||
                String(balance.warehouseId) === warehouseFilter

            const matchesStatus =
                statusFilter === 'ALL' ||
                balance.stockStatus === statusFilter

            return matchesSearch && matchesWarehouse && matchesStatus
        })
    }, [balances, search, warehouseFilter, statusFilter])

    const quarantineCount = filteredBalances.filter(
        balance => balance.stockStatus === 'QUARANTINE'
    ).length

    const availableCount = filteredBalances.filter(
        balance => balance.stockStatus === 'AVAILABLE'
    ).length

    const uniqueItems = new Set(
        filteredBalances.map(balance => balance.itemId)
    ).size

    function formatQuantity(value) {
        return Number(value || 0).toLocaleString(undefined, {
            minimumFractionDigits: 0,
            maximumFractionDigits: 3
        })
    }

    function formatDate(value) {
        if (!value) return '—'

        return new Date(value).toLocaleDateString('en-PH', {
            year: 'numeric',
            month: 'short',
            day: '2-digit'
        })
    }

    function statusClass(status) {
        switch (status) {
            case 'AVAILABLE':
                return 'io-available'
            case 'QUARANTINE':
                return 'io-quarantine'
            default:
                return 'io-default'
        }
    }

    return (
        <div className="io-page">
            <div className="io-heading">
                <div>
                    <p className="io-eyebrow">INVENTORY MANAGEMENT</p>
                    <h1>Stock Overview</h1>
                    <p>
                        View inventory balances across all warehouses and storage locations.
                    </p>
                </div>

                <button className="io-refresh-button" onClick={loadData}>
                    ↻ Refresh
                </button>
            </div>

            {error && (
                <div className="io-alert">
                    <span>{error}</span>
                    <button onClick={() => setError('')}>×</button>
                </div>
            )}

            <div className="io-summary">
                <div className="io-summary-card">
                    <span>BALANCE RECORDS</span>
                    <strong>{filteredBalances.length}</strong>
                    <small>Records matching your filters</small>
                </div>

                <div className="io-summary-card">
                    <span>UNIQUE ITEMS</span>
                    <strong>{uniqueItems}</strong>
                    <small>Distinct items in the results</small>
                </div>

                <div className="io-summary-card">
                    <span>AVAILABLE RECORDS</span>
                    <strong>{availableCount}</strong>
                    <small>Balances available for operations</small>
                </div>

                <div className="io-summary-card">
                    <span>QUARANTINE RECORDS</span>
                    <strong>{quarantineCount}</strong>
                    <small>Balances awaiting quality clearance</small>
                </div>
            </div>

            <section className="io-panel">
                <div className="io-panel-heading">
                    <div>
                        <h2>Inventory Balances</h2>
                        <p>{filteredBalances.length} matching balance records</p>
                    </div>
                </div>

                <div className="io-filters">
                    <div className="io-search">
                        <label htmlFor="io-search">Search</label>
                        <input
                            id="io-search"
                            value={search}
                            onChange={event => setSearch(event.target.value)}
                            placeholder="Item, batch, or bin code..."
                        />
                    </div>

                    <div>
                        <label htmlFor="io-warehouse">Warehouse</label>
                        <select
                            id="io-warehouse"
                            value={warehouseFilter}
                            onChange={event => setWarehouseFilter(event.target.value)}
                        >
                            <option value="ALL">All warehouses</option>
                            {warehouses.map(warehouse => (
                                <option
                                    key={warehouse.warehouseId}
                                    value={warehouse.warehouseId}
                                >
                                    {warehouse.warehouseCode} - {warehouse.warehouseName}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div>
                        <label htmlFor="io-status">Stock Status</label>
                        <select
                            id="io-status"
                            value={statusFilter}
                            onChange={event => setStatusFilter(event.target.value)}
                        >
                            <option value="ALL">All statuses</option>
                            <option value="AVAILABLE">Available</option>
                            <option value="QUARANTINE">Quarantine</option>
                        </select>
                    </div>
                </div>

                <div className="io-table-wrapper">
                    <table className="io-table">
                        <thead>
                            <tr>
                                <th>ITEM</th>
                                <th>WAREHOUSE</th>
                                <th>BIN</th>
                                <th>BATCH</th>
                                <th>EXPIRY DATE</th>
                                <th>ON HAND</th>
                                <th>RESERVED</th>
                                <th>AVAILABLE</th>
                                <th>STATUS</th>
                            </tr>
                        </thead>

                        <tbody>
                            {loading ? (
                                <tr>
                                    <td colSpan="9" className="io-empty">
                                        Loading inventory...
                                    </td>
                                </tr>
                            ) : filteredBalances.length === 0 ? (
                                <tr>
                                    <td colSpan="9" className="io-empty">
                                        No inventory balances found.
                                    </td>
                                </tr>
                            ) : (
                                filteredBalances.map(balance => (
                                    <tr key={balance.balanceId}>
                                        <td>
                                            <div className="io-item-cell">
                                                <strong>{balance.itemCode}</strong>
                                                <small>{balance.itemName}</small>
                                            </div>
                                        </td>
                                        <td>
                                            <div className="io-item-cell">
                                                <strong>{balance.warehouseCode || '—'}</strong>
                                                <small>{balance.warehouseName || '—'}</small>
                                            </div>
                                        </td>
                                        <td>{balance.binCode || '—'}</td>
                                        <td>
                                            <div className="io-item-cell">
                                                <strong>{balance.batchCode || '—'}</strong>
                                                {balance.supplierBatchCode && (
                                                    <small>
                                                        Supplier: {balance.supplierBatchCode}
                                                    </small>
                                                )}
                                            </div>
                                        </td>
                                        <td>{formatDate(balance.expiryDate)}</td>
                                        <td className="io-number">
                                            {formatQuantity(balance.quantityOnHand)}
                                        </td>
                                        <td className="io-number">
                                            {formatQuantity(balance.quantityReserved)}
                                        </td>
                                        <td className="io-number io-available-number">
                                            {formatQuantity(balance.availableQuantity)}
                                        </td>
                                        <td>
                                            <span
                                                className={`io-status ${statusClass(balance.stockStatus)}`}
                                            >
                                                {balance.stockStatus || 'UNKNOWN'}
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