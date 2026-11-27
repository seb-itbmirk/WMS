import { useEffect, useMemo, useState } from 'react'
import api from '../../services/api'
import './stockcounts.css'

function valueOf(obj, camel, pascal) {
    return obj?.[camel] ?? obj?.[pascal]
}

function number(value) {
    return Number(value || 0).toLocaleString('en-PH', {
        minimumFractionDigits: 0,
        maximumFractionDigits: 3
    })
}

function dateValue(value) {
    if (!value) return '—'
    return new Date(value).toLocaleDateString('en-PH')
}

function statusClass(status) {
    return String(status || '').toLowerCase().replaceAll(' ', '-').replaceAll('_', '-')
}

export default function StockCountsPage() {
    const [counts, setCounts] = useState([])
    const [warehouses, setWarehouses] = useState([])
    const [lots, setLots] = useState([])
    const [zones, setZones] = useState([])
    const [bins, setBins] = useState([])

    const [selectedCount, setSelectedCount] = useState(null)
    const [details, setDetails] = useState(null)

    const [loading, setLoading] = useState(true)
    const [detailsLoading, setDetailsLoading] = useState(false)
    const [saving, setSaving] = useState(false)
    const [posting, setPosting] = useState(false)

    const [error, setError] = useState('')
    const [success, setSuccess] = useState('')

    const [search, setSearch] = useState('')
    const [statusFilter, setStatusFilter] = useState('ALL')

    const [showCreateModal, setShowCreateModal] = useState(false)
    const [showLineModal, setShowLineModal] = useState(false)
    const [showDetails, setShowDetails] = useState(false)

    const [createForm, setCreateForm] = useState({
        countNumber: '',
        warehouseId: '',
        countDate: new Date().toISOString().slice(0, 16),
        createdBy: ''
    })

    const [lineForm, setLineForm] = useState({
        lotId: '',
        zoneId: '',
        binId: ''
    })

    const [countInputs, setCountInputs] = useState({})
    const [countedByInputs, setCountedByInputs] = useState({})

    useEffect(() => {
        loadPage()
    }, [])

    async function loadPage() {
        setLoading(true)
        setError('')

        try {
            const [countResponse, warehouseResponse, lotResponse] =
                await Promise.all([
                    api.get('/stock-counts'),
                    api.get('/warehouses'),
                    api.get('/inventory-lots')
                ])

            setCounts(countResponse.data || [])
            setWarehouses(warehouseResponse.data || [])
            setLots(lotResponse.data || [])
        } catch (err) {
            setError(getError(err, 'Failed to load stock count data.'))
        } finally {
            setLoading(false)
        }
    }

    function getError(err, fallback) {
        const data = err.response?.data
        if (typeof data === 'string') return data
        return data?.message || data?.Message || fallback
    }

    async function loadZones(warehouseId) {
        setZones([])
        setBins([])
        if (!warehouseId) return

        try {
            const response = await api.get(`/warehouses/${warehouseId}/zones`)
            setZones(response.data || [])
        } catch (err) {
            setError(getError(err, 'Failed to load warehouse zones.'))
        }
    }

    async function loadBins(warehouseId, zoneId) {
        setBins([])
        if (!warehouseId || !zoneId) return

        try {
            const response = await api.get(
                `/warehouses/${warehouseId}/zones/${zoneId}/bins`
            )
            setBins(response.data || [])
        } catch (err) {
            setError(getError(err, 'Failed to load storage bins.'))
        }
    }

    async function openCount(count) {
        const id = valueOf(count, 'stockCountId', 'StockCountId')

        setSelectedCount(count)
        setShowDetails(true)
        setDetails(null)
        setError('')
        setDetailsLoading(true)

        try {
            const response = await api.get(`/stock-counts/${id}`)
            setDetails(response.data)

            const lines = valueOf(response.data, 'lines', 'Lines') || []
            const quantities = {}
            const users = {}

            lines.forEach(line => {
                const lineId = valueOf(line, 'stockCountLineId', 'StockCountLineId')
                const counted = valueOf(line, 'countedQuantity', 'CountedQuantity')
                const countedBy = valueOf(line, 'countedBy', 'CountedBy')

                quantities[lineId] = counted ?? ''
                users[lineId] = countedBy ?? ''
            })

            setCountInputs(quantities)
            setCountedByInputs(users)
        } catch (err) {
            setError(getError(err, 'Failed to load stock count details.'))
        } finally {
            setDetailsLoading(false)
        }
    }

    async function createCount(event) {
        event.preventDefault()
        setError('')
        setSuccess('')

        if (!createForm.countNumber.trim() || !createForm.warehouseId) {
            setError('Count number and warehouse are required.')
            return
        }

        setSaving(true)

        try {
            const payload = {
                countNumber: createForm.countNumber.trim(),
                warehouseId: Number(createForm.warehouseId),
                countDate: new Date(createForm.countDate).toISOString(),
                createdBy: createForm.createdBy
                    ? Number(createForm.createdBy)
                    : null
            }

            await api.post('/stock-counts', payload)

            setShowCreateModal(false)
            setCreateForm({
                countNumber: '',
                warehouseId: '',
                countDate: new Date().toISOString().slice(0, 16),
                createdBy: ''
            })

            setSuccess('Stock count created successfully.')
            await loadPage()
        } catch (err) {
            setError(getError(err, 'Failed to create stock count.'))
        } finally {
            setSaving(false)
        }
    }

    function openAddLine() {
        setLineForm({
            lotId: '',
            zoneId: '',
            binId: ''
        })
        setZones([])
        setBins([])
        setShowLineModal(true)

        const currentWarehouseId = valueOf(details, 'warehouseId', 'WarehouseId')
        if (currentWarehouseId) loadZones(currentWarehouseId)
    }

    async function addLine(event) {
        event.preventDefault()
        if (!details) return

        const countId = valueOf(details, 'stockCountId', 'StockCountId')

        if (!lineForm.lotId || !lineForm.binId) {
            setError('Please select an inventory lot and bin.')
            return
        }

        setSaving(true)
        setError('')

        try {
            await api.post(`/stock-counts/${countId}/lines`, {
                lotId: Number(lineForm.lotId),
                binId: Number(lineForm.binId)
            })

            setShowLineModal(false)
            setSuccess('Stock count line added successfully.')
            await openCount(details)
            await loadPage()
        } catch (err) {
            setError(getError(err, 'Failed to add stock count line.'))
        } finally {
            setSaving(false)
        }
    }

    async function recordCount(line) {
        const countId = valueOf(details, 'stockCountId', 'StockCountId')
        const lineId = valueOf(line, 'stockCountLineId', 'StockCountLineId')
        const countedQuantity = countInputs[lineId]

        if (countedQuantity === '' || countedQuantity === undefined) {
            setError('Enter the physical count before saving.')
            return
        }

        if (!Number.isFinite(Number(countedQuantity)) || Number(countedQuantity) < 0) {
            setError('Counted quantity must be zero or greater.')
            return
        }

        setSaving(true)
        setError('')
        setSuccess('')

        try {
            await api.put(
                `/stock-counts/${countId}/lines/${lineId}`,
                {
                    countedQuantity: Number(countedQuantity),
                    countedBy: countedByInputs[lineId]
                        ? Number(countedByInputs[lineId])
                        : null
                }
            )

            setSuccess(`Physical count for line #${lineId} saved.`)
            await openCount(details)
            await loadPage()
        } catch (err) {
            setError(getError(err, 'Failed to record physical count.'))
        } finally {
            setSaving(false)
        }
    }

    async function completeCount() {
        const countId = valueOf(details, 'stockCountId', 'StockCountId')

        if (!window.confirm(
            'Complete this stock count? No more physical counts can be recorded afterward.'
        )) return

        setSaving(true)
        setError('')
        setSuccess('')

        try {
            const response = await api.post(`/stock-counts/${countId}/complete`, {})
            setSuccess(
                valueOf(response.data, 'message', 'Message') ||
                'Stock count completed successfully.'
            )
            await openCount(details)
            await loadPage()
        } catch (err) {
            setError(getError(err, 'Failed to complete stock count.'))
        } finally {
            setSaving(false)
        }
    }

    async function postCount() {
        const countId = valueOf(details, 'stockCountId', 'StockCountId')

        if (!window.confirm(
            'Post this stock count? This will apply inventory differences and create a COUNT_VARIANCE transaction.'
        )) return

        setPosting(true)
        setError('')
        setSuccess('')

        try {
            const response = await api.post(
                `/stock-counts/${countId}/post`,
                { postedBy: null }
            )

            const result = response.data || {}
            const transactionNumber = valueOf(
                result,
                'inventoryTransactionNumber',
                'InventoryTransactionNumber'
            )

            setSuccess(
                `${valueOf(result, 'message', 'Message') || 'Stock count posted successfully.'}` +
                (transactionNumber ? ` Transaction: ${transactionNumber}` : '')
            )

            await openCount(details)
            await loadPage()
        } catch (err) {
            setError(getError(err, 'Failed to post stock count.'))
        } finally {
            setPosting(false)
        }
    }

    const filteredCounts = useMemo(() => {
        return counts.filter(count => {
            const countNumber = valueOf(count, 'countNumber', 'CountNumber') || ''
            const warehouseName = valueOf(count, 'warehouseName', 'WarehouseName') || ''
            const status = valueOf(count, 'status', 'Status') || ''

            const matchesSearch =
                `${countNumber} ${warehouseName}`.toLowerCase().includes(search.toLowerCase())

            const matchesStatus =
                statusFilter === 'ALL' ||
                String(status).toUpperCase() === statusFilter

            return matchesSearch && matchesStatus
        })
    }, [counts, search, statusFilter])

    const lines = valueOf(details, 'lines', 'Lines') || []
    const detailStatus = valueOf(details, 'status', 'Status')
    const canEdit = ['OPEN', 'IN_PROGRESS'].includes(
        String(detailStatus).toUpperCase()
    )

    const countedLines = lines.filter(
        line => valueOf(line, 'countedQuantity', 'CountedQuantity') !== null &&
                valueOf(line, 'countedQuantity', 'CountedQuantity') !== undefined
    ).length

    const totalDifference = lines.reduce((total, line) => {
        const counted = valueOf(line, 'countedQuantity', 'CountedQuantity')
        if (counted === null || counted === undefined) return total

        return total + Number(counted) -
            Number(valueOf(line, 'systemQuantity', 'SystemQuantity') || 0)
    }, 0)

    const warehouseId = valueOf(details, 'warehouseId', 'WarehouseId')

    return (
        <div className="page-content stock-counts-page">
            <div className="stock-counts-heading">
                <div>
                    <span className="stock-counts-eyebrow">INVENTORY CONTROL</span>
                    <h1>Stock Counts</h1>
                    <p>Compare physical inventory against recorded warehouse balances.</p>
                </div>

                <button
                    className="stock-counts-primary-btn"
                    onClick={() => {
                        setError('')
                        setSuccess('')
                        setShowCreateModal(true)
                    }}
                >
                    + Create Stock Count
                </button>
            </div>

            {error && (
                <div className="stock-counts-alert error">
                    {error}
                    <button onClick={() => setError('')}>×</button>
                </div>
            )}

            {success && (
                <div className="stock-counts-alert success">
                    {success}
                    <button onClick={() => setSuccess('')}>×</button>
                </div>
            )}

            <div className="stock-counts-summary">
                <div>
                    <span>Total Counts</span>
                    <strong>{counts.length}</strong>
                </div>
                <div>
                    <span>In Progress</span>
                    <strong>
                        {counts.filter(c =>
                            String(valueOf(c, 'status', 'Status')).toUpperCase() === 'IN_PROGRESS'
                        ).length}
                    </strong>
                </div>
                <div>
                    <span>Completed</span>
                    <strong>
                        {counts.filter(c =>
                            String(valueOf(c, 'status', 'Status')).toUpperCase() === 'COMPLETED'
                        ).length}
                    </strong>
                </div>
                <div>
                    <span>Posted</span>
                    <strong>
                        {counts.filter(c =>
                            String(valueOf(c, 'status', 'Status')).toUpperCase() === 'POSTED'
                        ).length}
                    </strong>
                </div>
            </div>

            <div className="stock-counts-panel">
                <div className="stock-counts-table-header">
                    <div className="stock-counts-table-title">
                        <h2>Stock Count Records</h2>
                        <p>Review and manage physical inventory counts.</p>
                    </div>

                    <div className="stock-counts-toolbar">
                        <input
                            placeholder="Search count number or warehouse..."
                            value={search}
                            onChange={e => setSearch(e.target.value)}
                        />

                        <select
                            value={statusFilter}
                            onChange={e => setStatusFilter(e.target.value)}
                        >
                            <option value="ALL">All Statuses</option>
                            <option value="OPEN">Open</option>
                            <option value="IN_PROGRESS">In Progress</option>
                            <option value="COMPLETED">Completed</option>
                            <option value="POSTED">Posted</option>
                        </select>

                        <button
                            className="stock-counts-secondary-btn"
                            onClick={loadPage}
                            disabled={loading}
                        >
                            {loading ? 'Refreshing...' : 'Refresh'}
                        </button>
                    </div>
                </div>

                <div className="stock-counts-table-wrap">
                    <table className="stock-counts-table">
                        <thead>
                            <tr>
                                <th>Count Number</th>
                                <th>Warehouse</th>
                                <th>Count Date</th>
                                <th>Status</th>
                                <th>Action</th>
                            </tr>
                        </thead>
                        <tbody>
                            {loading ? (
                                <tr>
                                    <td colSpan="5" className="stock-counts-empty">
                                        Loading stock counts...
                                    </td>
                                </tr>
                            ) : filteredCounts.length === 0 ? (
                                <tr>
                                    <td colSpan="5" className="stock-counts-empty">
                                        No stock counts found.
                                    </td>
                                </tr>
                            ) : filteredCounts.map(count => {
                                const id = valueOf(count, 'stockCountId', 'StockCountId')
                                const status = valueOf(count, 'status', 'Status')

                                return (
                                    <tr key={id}>
                                        <td>
                                            <strong>{valueOf(count, 'countNumber', 'CountNumber')}</strong>
                                        </td>
                                        <td>{valueOf(count, 'warehouseName', 'WarehouseName')}</td>
                                        <td>{dateValue(valueOf(count, 'countDate', 'CountDate'))}</td>
                                        <td>
                                            <span className={`stock-counts-status ${statusClass(status)}`}>
                                                {status}
                                            </span>
                                        </td>
                                        <td>
                                            <button
                                                className="stock-counts-view-btn"
                                                onClick={() => openCount(count)}
                                            >
                                                View Details
                                            </button>
                                        </td>
                                    </tr>
                                )
                            })}
                        </tbody>
                    </table>
                </div>
            </div>

            {showCreateModal && (
                <div className="stock-counts-backdrop">
                    <div className="stock-counts-modal">
                        <div className="stock-counts-modal-header">
                            <h2>Create Stock Count</h2>
                            <button onClick={() => setShowCreateModal(false)}>×</button>
                        </div>

                        <form onSubmit={createCount}>
                            <label>Count Number *</label>
                            <input
                                value={createForm.countNumber}
                                onChange={e => setCreateForm({
                                    ...createForm,
                                    countNumber: e.target.value
                                })}
                                placeholder="e.g. SC-2026-001"
                                required
                            />

                            <label>Warehouse *</label>
                            <select
                                value={createForm.warehouseId}
                                onChange={e => setCreateForm({
                                    ...createForm,
                                    warehouseId: e.target.value
                                })}
                                required
                            >
                                <option value="">Select warehouse</option>
                                {warehouses.map(warehouse => {
                                    const id = valueOf(warehouse, 'warehouseId', 'WarehouseId')
                                    const code = valueOf(warehouse, 'warehouseCode', 'WarehouseCode')
                                    const name = valueOf(warehouse, 'warehouseName', 'WarehouseName')

                                    return (
                                        <option key={id} value={id}>
                                            {code ? `${code} — ` : ''}{name || `Warehouse ${id}`}
                                        </option>
                                    )
                                })}
                            </select>

                            <label>Count Date *</label>
                            <input
                                type="datetime-local"
                                value={createForm.countDate}
                                onChange={e => setCreateForm({
                                    ...createForm,
                                    countDate: e.target.value
                                })}
                                required
                            />

                            <label>Created By User ID</label>
                            <input
                                type="number"
                                min="1"
                                value={createForm.createdBy}
                                onChange={e => setCreateForm({
                                    ...createForm,
                                    createdBy: e.target.value
                                })}
                                placeholder="Optional"
                            />

                            <div className="stock-counts-modal-actions">
                                <button
                                    type="button"
                                    className="stock-counts-secondary-btn"
                                    onClick={() => setShowCreateModal(false)}
                                >
                                    Cancel
                                </button>
                                <button
                                    type="submit"
                                    className="stock-counts-primary-btn"
                                    disabled={saving}
                                >
                                    {saving ? 'Creating...' : 'Create Count'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {showDetails && (
                <div className="stock-counts-backdrop">
                    <div className="stock-counts-modal stock-counts-details-modal">
                        <div className="stock-counts-modal-header">
                            <div>
                                <span className="stock-counts-eyebrow">COUNT DETAILS</span>
                                <h2>
                                    {details
                                        ? valueOf(details, 'countNumber', 'CountNumber')
                                        : 'Loading...'}
                                </h2>
                            </div>
                            <button onClick={() => setShowDetails(false)}>×</button>
                        </div>

                        {detailsLoading || !details ? (
                            <div className="stock-counts-empty">Loading count details...</div>
                        ) : (
                            <>
                                <div className="stock-counts-detail-info">
                                    <div>
                                        <span>Warehouse</span>
                                        <strong>{valueOf(details, 'warehouseName', 'WarehouseName')}</strong>
                                    </div>
                                    <div>
                                        <span>Count Date</span>
                                        <strong>{dateValue(valueOf(details, 'countDate', 'CountDate'))}</strong>
                                    </div>
                                    <div>
                                        <span>Status</span>
                                        <strong>{detailStatus}</strong>
                                    </div>
                                    <div>
                                        <span>Counted Lines</span>
                                        <strong>{countedLines} / {lines.length}</strong>
                                    </div>
                                    <div>
                                        <span>Net Difference</span>
                                        <strong>{number(totalDifference)}</strong>
                                    </div>
                                </div>

                                <div className="stock-counts-lines-heading">
                                    <h3>Inventory Count Lines</h3>
                                    {canEdit && (
                                        <button
                                            className="stock-counts-primary-btn"
                                            onClick={openAddLine}
                                        >
                                            + Add Line
                                        </button>
                                    )}
                                </div>

                                <div className="stock-counts-table-wrap">
                                    <table className="stock-counts-table">
                                        <thead>
                                            <tr>
                                                <th>Item</th>
                                                <th>Batch</th>
                                                <th>Bin</th>
                                                <th>System Qty</th>
                                                <th>Physical Qty</th>
                                                <th>Difference</th>
                                                {canEdit && <th>Action</th>}
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {lines.length === 0 ? (
                                                <tr>
                                                    <td colSpan={canEdit ? 7 : 6} className="stock-counts-empty">
                                                        No count lines added.
                                                    </td>
                                                </tr>
                                            ) : lines.map(line => {
                                                const lineId = valueOf(line, 'stockCountLineId', 'StockCountLineId')
                                                const systemQty = Number(valueOf(line, 'systemQuantity', 'SystemQuantity') || 0)
                                                const countedQty = valueOf(line, 'countedQuantity', 'CountedQuantity')
                                                const difference = countedQty == null
                                                    ? null
                                                    : Number(countedQty) - systemQty

                                                return (
                                                    <tr key={lineId}>
                                                        <td>
                                                            <strong>{valueOf(line, 'itemCode', 'ItemCode')}</strong>
                                                            <small>{valueOf(line, 'itemName', 'ItemName')}</small>
                                                        </td>
                                                        <td>{valueOf(line, 'batchCode', 'BatchCode')}</td>
                                                        <td>{valueOf(line, 'binCode', 'BinCode')}</td>
                                                        <td>{number(systemQty)}</td>
                                                        <td>
                                                            {canEdit ? (
                                                                <input
                                                                    className="stock-counts-quantity-input"
                                                                    type="number"
                                                                    min="0"
                                                                    step="any"
                                                                    value={countInputs[lineId] ?? ''}
                                                                    onChange={e => setCountInputs({
                                                                        ...countInputs,
                                                                        [lineId]: e.target.value
                                                                    })}
                                                                />
                                                            ) : number(countedQty)}
                                                        </td>
                                                        <td>
                                                            {difference === null
                                                                ? 'Not counted'
                                                                : (
                                                                    <span className={
                                                                        difference < 0 ? 'negative' :
                                                                        difference > 0 ? 'positive' : ''
                                                                    }>
                                                                        {difference > 0 ? '+' : ''}{number(difference)}
                                                                    </span>
                                                                )}
                                                        </td>
                                                        {canEdit && (
                                                            <td>
                                                                <button
                                                                    className="stock-counts-view-btn"
                                                                    onClick={() => recordCount(line)}
                                                                    disabled={saving}
                                                                >
                                                                    Save Count
                                                                </button>
                                                            </td>
                                                        )}
                                                    </tr>
                                                )
                                            })}
                                        </tbody>
                                    </table>
                                </div>

                                {canEdit && (
                                    <div className="stock-counts-workflow-actions">
                                        <button
                                            className="stock-counts-primary-btn"
                                            onClick={completeCount}
                                            disabled={saving || lines.length === 0 || countedLines !== lines.length}
                                        >
                                            {saving ? 'Processing...' : 'Complete Count'}
                                        </button>
                                    </div>
                                )}

                                {String(detailStatus).toUpperCase() === 'COMPLETED' && (
                                    <div className="stock-counts-post-area">
                                        <p>
                                            All physical counts are recorded. Posting will apply
                                            the differences to inventory.
                                        </p>
                                        <button
                                            className="stock-counts-primary-btn"
                                            onClick={postCount}
                                            disabled={posting}
                                        >
                                            {posting ? 'Posting...' : 'Post Stock Count'}
                                        </button>
                                    </div>
                                )}
                            </>
                        )}
                    </div>
                </div>
            )}

            {showLineModal && (
                <div className="stock-counts-backdrop">
                    <div className="stock-counts-modal">
                        <div className="stock-counts-modal-header">
                            <h2>Add Inventory Line</h2>
                            <button onClick={() => setShowLineModal(false)}>×</button>
                        </div>

                        <form onSubmit={addLine}>
                            <label>Inventory Lot *</label>
                            <select
                                value={lineForm.lotId}
                                onChange={e => setLineForm({
                                    ...lineForm,
                                    lotId: e.target.value
                                })}
                                required
                            >
                                <option value="">Select lot</option>
                                {lots.map(lot => {
                                    const id = valueOf(lot, 'lotId', 'LotId')
                                    const batch = valueOf(lot, 'batchCode', 'BatchCode')
                                    const itemCode = valueOf(lot, 'itemCode', 'ItemCode')
                                    const itemName = valueOf(lot, 'itemName', 'ItemName')

                                    return (
                                        <option key={id} value={id}>
                                            {batch} — {itemCode || itemName || `Lot ${id}`}
                                        </option>
                                    )
                                })}
                            </select>

                            <label>Zone *</label>
                            <select
                                value={lineForm.zoneId}
                                onChange={async e => {
                                    const zoneId = e.target.value
                                    setLineForm(previous => ({
                                        ...previous,
                                        zoneId,
                                        binId: ''
                                    }))
                                    await loadBins(warehouseId, zoneId)
                                }}
                                required
                            >
                                <option value="">Select zone</option>
                                {zones.map(zone => {
                                    const id = valueOf(zone, 'zoneId', 'ZoneId')
                                    const code = valueOf(zone, 'zoneCode', 'ZoneCode')
                                    const name = valueOf(zone, 'zoneName', 'ZoneName')

                                    return (
                                        <option key={id} value={id}>
                                            {code ? `${code} — ` : ''}{name || `Zone ${id}`}
                                        </option>
                                    )
                                })}
                            </select>

                            <label>Bin *</label>
                            <select
                                value={lineForm.binId}
                                onChange={e => setLineForm({
                                    ...lineForm,
                                    binId: e.target.value
                                })}
                                disabled={!lineForm.zoneId}
                                required
                            >
                                <option value="">Select bin</option>
                                {bins.map(bin => {
                                    const id = valueOf(bin, 'binId', 'BinId')
                                    const code = valueOf(bin, 'binCode', 'BinCode')

                                    return (
                                        <option key={id} value={id}>
                                            {code || `Bin ${id}`}
                                        </option>
                                    )
                                })}
                            </select>

                            <p className="stock-counts-help">
                                The system quantity will be retrieved from the AVAILABLE
                                inventory balance for this lot and bin.
                            </p>

                            <div className="stock-counts-modal-actions">
                                <button
                                    type="button"
                                    className="stock-counts-secondary-btn"
                                    onClick={() => setShowLineModal(false)}
                                >
                                    Cancel
                                </button>
                                <button
                                    type="submit"
                                    className="stock-counts-primary-btn"
                                    disabled={saving}
                                >
                                    {saving ? 'Adding...' : 'Add Line'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    )
}
