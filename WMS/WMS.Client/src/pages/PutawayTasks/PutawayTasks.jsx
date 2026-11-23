
import { useEffect, useMemo, useState } from 'react'
import api from '../../services/api'

function getValue(object, camelCase, pascalCase) {
    return object?.[camelCase] ?? object?.[pascalCase]
}

function formatNumber(value) {
    return Number(value || 0).toLocaleString('en-PH', {
        minimumFractionDigits: 0,
        maximumFractionDigits: 3
    })
}

function statusClass(status) {
    return String(status || 'pending').toLowerCase().replaceAll(' ', '-')
}

export default function PutawayTasksPage() {
    const [tasks, setTasks] = useState([])
    const [receipts, setReceipts] = useState([])
    const [warehouses, setWarehouses] = useState([])
    const [zones, setZones] = useState([])
    const [bins, setBins] = useState([])

    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)
    const [completingId, setCompletingId] = useState(null)

    const [error, setError] = useState('')
    const [success, setSuccess] = useState('')

    const [search, setSearch] = useState('')
    const [statusFilter, setStatusFilter] = useState('ALL')

    const [showModal, setShowModal] = useState(false)

    const [form, setForm] = useState({
        receiptLineId: '',
        lotId: '',
        warehouseId: '',
        zoneId: '',
        fromBinId: '',
        toBinId: '',
        quantity: '',
        assignedTo: ''
    })

    const [selectedReceiptLine, setSelectedReceiptLine] = useState(null)

    useEffect(() => {
        loadPage()
    }, [])

    async function loadPage() {
        setLoading(true)
        setError('')

        try {
            const [taskResponse, receiptResponse, warehouseResponse] =
                await Promise.all([
                    api.get('/putaway-tasks'),
                    api.get('/goods-receipts'),
                    api.get('/warehouses')
                ])

            setTasks(taskResponse.data || [])
            setReceipts(receiptResponse.data || [])
            setWarehouses(warehouseResponse.data || [])
        } catch (err) {
            setError(
                err.response?.data?.message ||
                err.response?.data ||
                'Failed to load putaway data.'
            )
        } finally {
            setLoading(false)
        }
    }

    const receiptLines = useMemo(() => {
        const lines = []

        receipts.forEach(receipt => {
            const receiptStatus = getValue(receipt, 'status', 'Status')

            if (String(receiptStatus).toUpperCase() !== 'POSTED') {
                return
            }

            const receiptId = getValue(receipt, 'receiptId', 'ReceiptId')
            const receiptNumber = getValue(receipt, 'receiptNumber', 'ReceiptNumber')
            const warehouseId = getValue(receipt, 'warehouseId', 'WarehouseId')
            const warehouseName = getValue(receipt, 'warehouseName', 'WarehouseName')
            const receiptLineItems = getValue(receipt, 'lines', 'Lines') || []

            receiptLineItems.forEach(line => {
                const receiptLineId = getValue(line, 'receiptLineId', 'ReceiptLineId')
                const lotId = getValue(line, 'lotId', 'LotId')
                const itemCode = getValue(line, 'itemCode', 'ItemCode')
                const itemName = getValue(line, 'itemName', 'ItemName')
                const batchCode = getValue(line, 'batchCode', 'BatchCode')
                const receivedQuantity = Number(
                    getValue(line, 'receivedQuantity', 'ReceivedQuantity') || 0
                )
                const receivingBinId = getValue(line, 'receivingBinId', 'ReceivingBinId')

                
            const alreadyAssigned = tasks
                .filter(task =>
                    Number(getValue(task, 'receiptLineId', 'ReceiptLineId')) ===
                        Number(receiptLineId) &&
                    String(getValue(task, 'status', 'Status')).toUpperCase() !== 'CANCELLED'
                )
                .reduce((total, task) => {
                    const quantity = Number(
                        getValue(task, 'quantity', 'Quantity') || 0
                    )
                    return total + quantity
                }, 0)


                const remainingQuantity = Math.max(
                    0,
                    receivedQuantity - alreadyAssigned
                )

                if (remainingQuantity > 0) {
                    lines.push({
                        receiptId,
                        receiptNumber,
                        receiptLineId,
                        lotId,
                        itemCode,
                        itemName,
                        batchCode,
                        warehouseId,
                        warehouseName,
                        receivingBinId,
                        receivedQuantity,
                        remainingQuantity
                    })
                }
            })
        })

        return lines
    }, [receipts, tasks])

    const filteredTasks = useMemo(() => {
        return tasks.filter(task => {
            const itemCode = getValue(task, 'itemCode', 'ItemCode') || ''
            const itemName = getValue(task, 'itemName', 'ItemName') || ''
            const batchCode = getValue(task, 'batchCode', 'BatchCode') || ''
            const taskId = getValue(task, 'putawayId', 'PutawayId')
            const status = getValue(task, 'status', 'Status')

            const searchText =
                `${taskId} ${itemCode} ${itemName} ${batchCode}`.toLowerCase()

            const matchesSearch = searchText.includes(search.toLowerCase())
            const matchesStatus =
                statusFilter === 'ALL' ||
                String(status).toUpperCase() === statusFilter

            return matchesSearch && matchesStatus
        })
    }, [tasks, search, statusFilter])

    async function loadZones(warehouseId) {
        setZones([])
        setBins([])

        if (!warehouseId) return

        try {
            const response = await api.get(`/warehouses/${warehouseId}/zones`)
            setZones(response.data || [])
        } catch (err) {
            setError('Failed to load warehouse zones.')
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
            setError('Failed to load storage bins.')
        }
    }

    function openCreateModal() {
        setError('')
        setSuccess('')
        setSelectedReceiptLine(null)

        setForm({
            receiptLineId: '',
            lotId: '',
            warehouseId: '',
            zoneId: '',
            fromBinId: '',
            toBinId: '',
            quantity: '',
            assignedTo: ''
        })

        setZones([])
        setBins([])
        setShowModal(true)
    }

    function handleReceiptLineChange(value) {
        const line = receiptLines.find(
            item => String(item.receiptLineId) === String(value)
        )

        setSelectedReceiptLine(line || null)

        setForm(previous => ({
            ...previous,
            receiptLineId: value,
            lotId: line ? String(line.lotId) : '',
            warehouseId: line ? String(line.warehouseId) : '',
            fromBinId: '',
            zoneId: '',
            toBinId: '',
            quantity: line ? String(line.remainingQuantity) : ''
        }))

        if (line?.warehouseId) {
            loadZones(line.warehouseId)
        } else {
            setZones([])
            setBins([])
        }
    }

    async function handleWarehouseChange(value) {
        setForm(previous => ({
            ...previous,
            warehouseId: value,
            zoneId: '',
            toBinId: ''
        }))

        await loadZones(value)
    }

    async function handleZoneChange(value) {
        setForm(previous => ({
            ...previous,
            zoneId: value,
            toBinId: ''
        }))

        await loadBins(form.warehouseId, value)
    }

    async function createTask(event) {
        event.preventDefault()
        setError('')
        setSuccess('')

        if (!form.receiptLineId || !form.lotId || !form.toBinId) {
            setError('Please select a receipt line and destination bin.')
            return
        }

        if (!form.quantity || Number(form.quantity) <= 0) {
            setError('Quantity must be greater than zero.')
            return
        }

        if (
            selectedReceiptLine &&
            Number(form.quantity) > selectedReceiptLine.remainingQuantity
        ) {
            setError(
                `Quantity cannot exceed the remaining receipt quantity of ${formatNumber(selectedReceiptLine.remainingQuantity)}.`
            )
            return
        }

        setSaving(true)

        try {
            const payload = {
                receiptLineId: Number(form.receiptLineId),
                lotId: Number(form.lotId),
                fromBinId: form.fromBinId
                    ? Number(form.fromBinId)
                    : null,
                toBinId: Number(form.toBinId),
                quantity: Number(form.quantity),
                assignedTo: form.assignedTo
                    ? Number(form.assignedTo)
                    : null
            }

            await api.post('/putaway-tasks', payload)

            setShowModal(false)
            setSuccess('Putaway task created successfully.')
            await loadPage()
        } catch (err) {
            setError(
                err.response?.data?.message ||
                err.response?.data ||
                'Failed to create putaway task.'
            )
        } finally {
            setSaving(false)
        }
    }

    async function completeTask(task) {
        const putawayId = getValue(task, 'putawayId', 'PutawayId')

        const confirmed = window.confirm(
            `Complete Putaway Task #${putawayId}? This will move the inventory to the destination bin.`
        )

        if (!confirmed) return

        setError('')
        setSuccess('')
        setCompletingId(putawayId)

        try {
            const response = await api.post(
                `/putaway-tasks/${putawayId}/complete`,
                {}
            )

            const result = response.data || {}

            setSuccess(
                result.message ||
                `Putaway Task #${putawayId} completed successfully.`
            )

            await loadPage()
        } catch (err) {
            setError(
                err.response?.data?.message ||
                err.response?.data ||
                'Failed to complete putaway task.'
            )
        } finally {
            setCompletingId(null)
        }
    }

    function getWarehouseName(warehouseId) {
        const warehouse = warehouses.find(
            item =>
                Number(getValue(item, 'warehouseId', 'WarehouseId')) ===
                Number(warehouseId)
        )

        return warehouse
            ? getValue(warehouse, 'warehouseName', 'WarehouseName') ||
              getValue(warehouse, 'name', 'Name') ||
              getValue(warehouse, 'warehouseCode', 'WarehouseCode')
            : '—'
    }

    return (
        <div className="page-content putaway-page">
            <div className="putaway-heading">
                <div>
                    <span className="putaway-eyebrow">WAREHOUSE OPERATIONS</span>
                    <h1>Putaway Tasks</h1>
                    <p>
                        Move received inventory from receiving locations to
                        designated storage bins.
                    </p>
                </div>

                <button
                    className="putaway-primary-btn"
                    onClick={openCreateModal}
                >
                    + Create Putaway Task
                </button>
            </div>

            {error && (
                <div className="putaway-alert putaway-alert-error">
                    {typeof error === 'string' ? error : 'An error occurred.'}
                    <button onClick={() => setError('')}>×</button>
                </div>
            )}

            {success && (
                <div className="putaway-alert putaway-alert-success">
                    {success}
                    <button onClick={() => setSuccess('')}>×</button>
                </div>
            )}

            <div className="putaway-summary">
                <div className="putaway-summary-card">
                    <span>Total Tasks</span>
                    <strong>{tasks.length}</strong>
                </div>
                <div className="putaway-summary-card">
                    <span>Pending</span>
                    <strong>
                        {tasks.filter(task =>
                            String(getValue(task, 'status', 'Status')).toUpperCase() === 'PENDING'
                        ).length}
                    </strong>
                </div>
                <div className="putaway-summary-card">
                    <span>Completed</span>
                    <strong>
                        {tasks.filter(task =>
                            String(getValue(task, 'status', 'Status')).toUpperCase() === 'COMPLETED'
                        ).length}
                    </strong>
                </div>
            </div>

            <div className="putaway-panel">
                <div className="putaway-toolbar">
                    <input
                        type="text"
                        placeholder="Search task, item, or batch..."
                        value={search}
                        onChange={event => setSearch(event.target.value)}
                    />

                    <select
                        value={statusFilter}
                        onChange={event => setStatusFilter(event.target.value)}
                    >
                        <option value="ALL">All Statuses</option>
                        <option value="PENDING">Pending</option>
                        <option value="COMPLETED">Completed</option>
                    </select>

                    <button
                        className="putaway-secondary-btn"
                        onClick={loadPage}
                        disabled={loading}
                    >
                        Refresh
                    </button>
                </div>

                <div className="putaway-table-wrap">
                    <table className="putaway-table">
                        <thead>
                            <tr>
                                <th>Task ID</th>
                                <th>Item</th>
                                <th>Batch</th>
                                <th>Source Bin</th>
                                <th>Destination Bin</th>
                                <th>Quantity</th>
                                <th>Status</th>
                                <th>Action</th>
                            </tr>
                        </thead>

                        <tbody>
                            {loading ? (
                                <tr>
                                    <td colSpan="8" className="putaway-empty">
                                        Loading putaway tasks...
                                    </td>
                                </tr>
                            ) : filteredTasks.length === 0 ? (
                                <tr>
                                    <td colSpan="8" className="putaway-empty">
                                        No putaway tasks found.
                                    </td>
                                </tr>
                            ) : (
                                filteredTasks.map(task => {
                                    const id = getValue(task, 'putawayId', 'PutawayId')
                                    const status = getValue(task, 'status', 'Status')

                                    return (
                                        <tr key={id}>
                                            <td>#{id}</td>
                                            <td>
                                                <strong>
                                                    {getValue(task, 'itemCode', 'ItemCode') || '—'}
                                                </strong>
                                                <small>
                                                    {getValue(task, 'itemName', 'ItemName') || ''}
                                                </small>
                                            </td>
                                            <td>
                                                {getValue(task, 'batchCode', 'BatchCode') || '—'}
                                            </td>
                                            <td>
                                                {getValue(task, 'fromBinCode', 'FromBinCode') || 'Receiving Bin'}
                                            </td>
                                            <td>
                                                {getValue(task, 'toBinCode', 'ToBinCode') || '—'}
                                            </td>
                                            <td>
                                                {formatNumber(getValue(task, 'quantity', 'Quantity'))}
                                            </td>
                                            <td>
                                                <span className={`putaway-status ${statusClass(status)}`}>
                                                    {status}
                                                </span>
                                            </td>
                                            <td>
                                                {String(status).toUpperCase() === 'PENDING' ? (
                                                    <button
                                                        className="putaway-action-btn"
                                                        onClick={() => completeTask(task)}
                                                        disabled={completingId === id}
                                                    >
                                                        {completingId === id
                                                            ? 'Completing...'
                                                            : 'Complete'}
                                                    </button>
                                                ) : (
                                                    <span className="putaway-muted">
                                                        Completed
                                                    </span>
                                                )}
                                            </td>
                                        </tr>
                                    )
                                })
                            )}
                        </tbody>
                    </table>
                </div>
            </div>

            {showModal && (
                <div className="putaway-modal-backdrop">
                    <div className="putaway-modal">
                        <div className="putaway-modal-header">
                            <div>
                                <span className="putaway-eyebrow">WAREHOUSE OPERATIONS</span>
                                <h2>Create Putaway Task</h2>
                            </div>
                            <button
                                className="putaway-close"
                                onClick={() => setShowModal(false)}
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={createTask}>
                            <div className="putaway-form-section">
                                <h3>Receipt Information</h3>

                                <label>Posted Receipt Line *</label>
                                <select
                                    value={form.receiptLineId}
                                    onChange={event =>
                                        handleReceiptLineChange(event.target.value)
                                    }
                                    required
                                >
                                    <option value="">Select a receipt line</option>
                                    {receiptLines.map(line => (
                                        <option
                                            key={line.receiptLineId}
                                            value={line.receiptLineId}
                                        >
                                            {line.receiptNumber} — {line.itemCode} — {line.batchCode}
                                            {' '}({formatNumber(line.remainingQuantity)} remaining)
                                        </option>
                                    ))}
                                </select>

                                {selectedReceiptLine && (
                                    <div className="putaway-receipt-info">
                                        <div>
                                            <span>Item</span>
                                            <strong>
                                                {selectedReceiptLine.itemCode} — {selectedReceiptLine.itemName}
                                            </strong>
                                        </div>
                                        <div>
                                            <span>Batch</span>
                                            <strong>{selectedReceiptLine.batchCode}</strong>
                                        </div>
                                        <div>
                                            <span>Warehouse</span>
                                            <strong>
                                                {selectedReceiptLine.warehouseName ||
                                                    getWarehouseName(selectedReceiptLine.warehouseId)}
                                            </strong>
                                        </div>
                                        <div>
                                            <span>Remaining Quantity</span>
                                            <strong>
                                                {formatNumber(selectedReceiptLine.remainingQuantity)}
                                            </strong>
                                        </div>
                                    </div>
                                )}

                                <label>Quantity to Put Away *</label>
                                <input
                                    type="number"
                                    min="0.001"
                                    step="any"
                                    value={form.quantity}
                                    onChange={event =>
                                        setForm(previous => ({
                                            ...previous,
                                            quantity: event.target.value
                                        }))
                                    }
                                    required
                                />
                            </div>

                            <div className="putaway-form-section">
                                <h3>Destination Location</h3>

                                <label>Warehouse *</label>
                                <select
                                    value={form.warehouseId}
                                    disabled={!selectedReceiptLine}
                                    required
                                >
                                    <option value="">
                                        {selectedReceiptLine
                                            ? 'Receipt warehouse'
                                            : 'Select a receipt line first'}
                                    </option>

                                    {selectedReceiptLine && (
                                        <option value={selectedReceiptLine.warehouseId}>
                                            {selectedReceiptLine.warehouseName ||
                                                getWarehouseName(selectedReceiptLine.warehouseId)}
                                        </option>
                                    )}
                                </select>

                                <label>Zone *</label>
                                <select
                                    value={form.zoneId}
                                    onChange={event =>
                                        handleZoneChange(event.target.value)
                                    }
                                    disabled={!form.warehouseId}
                                    required
                                >
                                    <option value="">Select zone</option>
                                    {zones.map(zone => {
                                        const id = getValue(zone, 'zoneId', 'ZoneId')
                                        const code = getValue(zone, 'zoneCode', 'ZoneCode')
                                        const name = getValue(zone, 'zoneName', 'ZoneName')

                                        return (
                                            <option key={id} value={id}>
                                                {code ? `${code} — ` : ''}{name || `Zone ${id}`}
                                            </option>
                                        )
                                    })}
                                </select>

                                <label>Destination Bin *</label>
                                <select
                                    value={form.toBinId}
                                    onChange={event =>
                                        setForm(previous => ({
                                            ...previous,
                                            toBinId: event.target.value
                                        }))
                                    }
                                    disabled={!form.zoneId}
                                    required
                                >
                                    <option value="">Select destination bin</option>
                                    {bins.map(bin => {
                                        const id = getValue(bin, 'binId', 'BinId')
                                        const code = getValue(bin, 'binCode', 'BinCode')

                                        return (
                                            <option key={id} value={id}>
                                                {code || `Bin ${id}`}
                                            </option>
                                        )
                                    })}
                                </select>

                                <p className="putaway-help">
                                    Only active bins returned by your warehouse endpoints
                                    will appear here.
                                </p>
                            </div>

                            <div className="putaway-form-section">
                                <h3>Optional Details</h3>

                                <label>Source Bin</label>
                                <select
                                    value={form.fromBinId}
                                    onChange={event =>
                                        setForm(previous => ({
                                            ...previous,
                                            fromBinId: event.target.value
                                        }))
                                    }
                                >
                                    <option value="">
                                        Use receipt's receiving bin automatically
                                    </option>
                                    {bins.map(bin => {
                                        const id = getValue(bin, 'binId', 'BinId')
                                        const code = getValue(bin, 'binCode', 'BinCode')

                                        return (
                                            <option
                                                key={id}
                                                value={id}
                                                disabled={String(id) === String(form.toBinId)}
                                            >
                                                {code || `Bin ${id}`}
                                            </option>
                                        )
                                    })}
                                </select>

                                <label>Assigned User ID</label>
                                <input
                                    type="number"
                                    min="1"
                                    value={form.assignedTo}
                                    onChange={event =>
                                        setForm(previous => ({
                                            ...previous,
                                            assignedTo: event.target.value
                                        }))
                                    }
                                    placeholder="Optional"
                                />
                            </div>

                            <div className="putaway-modal-actions">
                                <button
                                    type="button"
                                    className="putaway-secondary-btn"
                                    onClick={() => setShowModal(false)}
                                    disabled={saving}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="submit"
                                    className="putaway-primary-btn"
                                    disabled={saving}
                                >
                                    {saving ? 'Creating...' : 'Create Task'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    )
}
