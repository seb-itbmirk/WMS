
import { useEffect, useMemo, useState } from 'react'
import api from '../services/api'

const inspectionStatuses = ['PENDING', 'APPROVED', 'REJECTED', 'QUARANTINED']

const emptyHeader = {
    receiptNumber: '',
    purchaseOrderId: '',
    warehouseId: '',
    receiptDate: new Date().toISOString().slice(0, 10),
    deliveryReference: '',
    receivedBy: ''
}

const emptyLine = {
    itemId: '',
    lotId: '',
    receivedQuantity: '',
    uomId: '',
    receivingBinId: '',
    inspectionStatus: 'PENDING',
    poLineId: ''
}

const emptyLot = {
    batchCode: '',
    supplierBatchCode: '',
    manufactureDate: '',
    expiryDate: '',
    receivedDate: new Date().toISOString().slice(0, 10),
    qualityStatus: 'PENDING',
    notes: ''
}

function formatDate(value) {
    if (!value) return '—'

    return new Date(value).toLocaleDateString('en-PH', {
        year: 'numeric',
        month: 'short',
        day: '2-digit'
    })
}

function formatQuantity(value) {
    return Number(value || 0).toLocaleString('en-PH', {
        maximumFractionDigits: 3
    })
}

function statusClass(status) {
    return `gr-status gr-status-${String(status || '').toLowerCase()}`
}

export default function GoodsReceiptsPage() {
    const [receipts, setReceipts] = useState([])
    const [warehouses, setWarehouses] = useState([])
    const [purchaseOrders, setPurchaseOrders] = useState([])
    const [items, setItems] = useState([])
    const [uoms, setUoms] = useState([])
    const [lots, setLots] = useState([])

    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)
    const [postingId, setPostingId] = useState(null)

    const [error, setError] = useState('')
    const [success, setSuccess] = useState('')

    const [search, setSearch] = useState('')
    const [statusFilter, setStatusFilter] = useState('ALL')

    const [showCreateModal, setShowCreateModal] = useState(false)
    const [showDetailsModal, setShowDetailsModal] = useState(false)
    const [showLotModal, setShowLotModal] = useState(false)

    const [header, setHeader] = useState(emptyHeader)
    const [lines, setLines] = useState([])
    const [newLine, setNewLine] = useState(emptyLine)
    const [newLot, setNewLot] = useState(emptyLot)

    const [zones, setZones] = useState([])
    const [bins, setBins] = useState([])

    const [details, setDetails] = useState(null)
    const [detailsLoading, setDetailsLoading] = useState(false)

    const [lotItemId, setLotItemId] = useState('')

    useEffect(() => {
        loadPage()
    }, [])

    async function loadPage() {
        setLoading(true)
        setError('')

        try {
            const [
                receiptResponse,
                warehouseResponse,
                poResponse,
                itemResponse,
                uomResponse,
                lotResponse
            ] = await Promise.all([
                api.get('/goods-receipts'),
                api.get('/warehouses'),
                api.get('/purchase-orders'),
                api.get('/items'),
                api.get('/units-of-measure'),
                api.get('/inventory-lots')
            ])

            setReceipts(receiptResponse.data || [])
            setWarehouses(warehouseResponse.data || [])
            setPurchaseOrders(poResponse.data || [])
            setItems(itemResponse.data || [])
            setUoms(uomResponse.data || [])
            setLots(lotResponse.data || [])
        } catch (err) {
            setError(
                err.response?.data?.message ||
                err.response?.data ||
                'Unable to load goods receipt data.'
            )
        } finally {
            setLoading(false)
        }
    }

    const activeItems = useMemo(
        () => items.filter(item => item.isActive),
        [items]
    )

    const activeWarehouses = useMemo(
        () => warehouses.filter(warehouse => warehouse.isActive),
        [warehouses]
    )

    const activeUoms = useMemo(
        () => uoms.filter(uom => uom.isActive),
        [uoms]
    )

    const activeSuppliers = useMemo(
        () => purchaseOrders.filter(order => order.status !== 'CANCELLED'),
        [purchaseOrders]
    )

    const filteredReceipts = useMemo(() => {
        return receipts.filter(receipt => {
            const searchValue = search.toLowerCase()

            const matchesSearch =
                String(receipt.receiptNumber || '').toLowerCase().includes(searchValue) ||
                String(receipt.poNumber || '').toLowerCase().includes(searchValue) ||
                String(receipt.warehouseName || '').toLowerCase().includes(searchValue)

            const matchesStatus =
                statusFilter === 'ALL' ||
                receipt.status === statusFilter

            return matchesSearch && matchesStatus
        })
    }, [receipts, search, statusFilter])

    const draftCount = receipts.filter(r => r.status === 'DRAFT').length
    const postedCount = receipts.filter(r => r.status === 'POSTED').length

    function getItem(itemId) {
        return items.find(item => Number(item.itemId) === Number(itemId))
    }

    function getLot(lotId) {
        return lots.find(lot => Number(lot.lotId) === Number(lotId))
    }

    function getUom(uomId) {
        return uoms.find(uom => Number(uom.uomId) === Number(uomId))
    }

    function getWarehouse(warehouseId) {
        return warehouses.find(
            warehouse => Number(warehouse.warehouseId) === Number(warehouseId)
        )
    }

    function getItemLabel(item) {
        if (!item) return 'Unknown item'
        return `${item.itemCode} - ${item.itemName}`
    }

    function showMessage(message) {
        setSuccess(message)
        setError('')
    }

    function showError(message) {
        setError(
            typeof message === 'string'
                ? message
                : 'Something went wrong.'
        )
        setSuccess('')
    }

    function openCreateModal() {
        setHeader(emptyHeader)
        setLines([])
        setNewLine(emptyLine)
        setZones([])
        setBins([])
        setError('')
        setSuccess('')
        setShowCreateModal(true)
    }

    function closeCreateModal() {
        if (saving) return

        setShowCreateModal(false)
        setShowLotModal(false)
    }

    async function loadZones(warehouseId) {
        setZones([])
        setBins([])

        if (!warehouseId) return

        try {
            const response = await api.get(
                `/warehouses/${warehouseId}/zones`
            )
            setZones(response.data || [])
        } catch (err) {
            showError(
                err.response?.data?.message ||
                'Unable to load warehouse zones.'
            )
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
            showError(
                err.response?.data?.message ||
                'Unable to load receiving bins.'
            )
        }
    }

    async function handleWarehouseChange(value) {
        setHeader(prev => ({
            ...prev,
            warehouseId: value
        }))

        setZones([])
        setBins([])
        setNewLine(prev => ({
            ...prev,
            receivingBinId: ''
        }))

        await loadZones(value)
    }

    function handleItemChange(value) {
        const selectedItem = getItem(value)

        setNewLine(prev => ({
            ...prev,
            itemId: value,
            lotId: '',
            uomId: selectedItem?.baseUomId
                ? String(selectedItem.baseUomId)
                : ''
        }))
    }

    function handlePurchaseOrderChange(value) {
        setHeader(prev => ({
            ...prev,
            purchaseOrderId: value
        }))

        setNewLine(prev => ({
            ...prev,
            poLineId: ''
        }))
    }

    const selectedPurchaseOrder = purchaseOrders.find(
        order => Number(order.purchaseOrderId) === Number(header.purchaseOrderId)
    )

    const [poLines, setPoLines] = useState([])

    useEffect(() => {
        async function loadPoLines() {
            setPoLines([])

            if (!header.purchaseOrderId) return

            try {
                const response = await api.get(
                    `/purchase-orders/${header.purchaseOrderId}`
                )

                setPoLines(response.data?.lines || response.data?.Lines || [])
            } catch {
                setPoLines([])
            }
        }

        loadPoLines()
    }, [header.purchaseOrderId])

    async function loadLotsForItem(itemId) {
        if (!itemId) {
            setLots([])
            return
        }

        try {
            const response = await api.get(
                `/inventory-lots/item/${itemId}`
            )

            setLots(currentLots => {
                const otherLots = currentLots.filter(
                    lot => Number(lot.itemId) !== Number(itemId)
                )

                return [...otherLots, ...(response.data || [])]
            })
        } catch (err) {
            showError(
                err.response?.data?.message ||
                'Unable to load inventory lots.'
            )
        }
    }

    function openLotModal() {
        if (!newLine.itemId) {
            showError('Select an item before registering an inventory lot.')
            return
        }

        setLotItemId(newLine.itemId)
        setNewLot(emptyLot)
        setShowLotModal(true)
    }

    async function saveNewLot(event) {
        event.preventDefault()

        if (!lotItemId || !newLot.batchCode.trim()) {
            showError('Item and batch code are required.')
            return
        }

        setSaving(true)
        setError('')

        try {
            const payload = {
                itemId: Number(lotItemId),
                batchCode: newLot.batchCode.trim(),
                supplierBatchCode: newLot.supplierBatchCode.trim() || null,
                manufactureDate: newLot.manufactureDate || null,
                expiryDate: newLot.expiryDate || null,
                receivedDate: newLot.receivedDate,
                qualityStatus: newLot.qualityStatus,
                notes: newLot.notes.trim() || null
            }

            const response = await api.post('/inventory-lots', payload)

            const createdLotId =
                response.data?.lotId ??
                response.data?.LotId

            await loadLotsForItem(lotItemId)

            setNewLine(prev => ({
                ...prev,
                lotId: String(createdLotId)
            }))

            setShowLotModal(false)
            showMessage('Inventory lot registered successfully.')
        } catch (err) {
            showError(
                err.response?.data?.message ||
                err.response?.data ||
                'Unable to register inventory lot.'
            )
        } finally {
            setSaving(false)
        }
    }


function addReceiptLine() {
    const item = getItem(newLine.itemId)
    const lot = getLot(newLine.lotId)

    if (!newLine.itemId || !newLine.lotId || !newLine.uomId) {
        showError('Select an item, inventory lot, and UOM.')
        return
    }

    if (!newLine.receivedQuantity || Number(newLine.receivedQuantity) <= 0) {
        showError('Received quantity must be greater than zero.')
        return
    }

    if (!newLine.receivingBinId) {
        showError('Select a receiving bin for this receipt line.')
        return
    }

    if (!item || !lot) {
        showError('The selected item or inventory lot could not be found.')
        return
    }

    if (Number(lot.itemId) !== Number(item.itemId)) {
        showError('The selected lot does not belong to this item.')
        return
    }

    if (Number(newLine.uomId) !== Number(item.baseUomId)) {
        showError('Goods receipt quantity must use the item base UOM.')
        return
    }

    const selectedPoLine = poLines.find(
        line => Number(line.poLineId) === Number(newLine.poLineId)
    )

    if (header.purchaseOrderId && newLine.poLineId && !selectedPoLine) {
        showError('The selected PO line is invalid.')
        return
    }

    if (selectedPoLine) {
        if (Number(selectedPoLine.itemId) !== Number(newLine.itemId)) {
            showError('The selected PO line belongs to a different item.')
            return
        }

        if (Number(selectedPoLine.uomId) !== Number(newLine.uomId)) {
            showError('The selected PO line uses a different UOM.')
            return
        }
    }

    const line = {
        ...newLine,
        itemId: Number(newLine.itemId),
        lotId: Number(newLine.lotId),
        receivedQuantity: Number(newLine.receivedQuantity),
        uomId: Number(newLine.uomId),
        receivingBinId: Number(newLine.receivingBinId),
        poLineId: newLine.poLineId
            ? Number(newLine.poLineId)
            : null,
        itemCode: item.itemCode,
        itemName: item.itemName,
        batchCode: lot.batchCode,
        uomCode: getUom(newLine.uomId)?.uomCode || ''
    }

    setLines(prev => [...prev, line])
    setNewLine(emptyLine)
    showMessage('Receipt line added.')
}


    function removeReceiptLine(index) {
        setLines(prev => prev.filter((_, i) => i !== index))
    }

    async function saveReceipt(event) {
        event.preventDefault()

        if (!header.receiptNumber.trim()) {
            showError('Receipt number is required.')
            return
        }

        if (!header.warehouseId) {
            showError('Select a warehouse.')
            return
        }

        if (!lines.length) {
            showError('Add at least one receipt line.')
            return
        }

        setSaving(true)
        setError('')

        try {
            const payload = {
                receiptNumber: header.receiptNumber.trim(),
                purchaseOrderId: header.purchaseOrderId
                    ? Number(header.purchaseOrderId)
                    : null,
                warehouseId: Number(header.warehouseId),
                receiptDate: header.receiptDate
                    ? `${header.receiptDate}T00:00:00`
                    : null,
                deliveryReference: header.deliveryReference.trim() || null,
                receivedBy: header.receivedBy
                    ? Number(header.receivedBy)
                    : null,
                lines: lines.map(line => ({
                    itemId: Number(line.itemId),
                    lotId: Number(line.lotId),
                    receivedQuantity: Number(line.receivedQuantity),
                    receivingBinId: Number(line.receivingBinId),
                    uomId: Number(line.uomId),
                    inspectionStatus: line.inspectionStatus,
                    poLineId: line.poLineId
                        ? Number(line.poLineId)
                        : null
                }))
            }

            await api.post('/goods-receipts', payload)

            setShowCreateModal(false)
            showMessage('Goods receipt created successfully.')
            await loadPage()
        } catch (err) {
            showError(
                err.response?.data?.message ||
                err.response?.data ||
                'Unable to create goods receipt.'
            )
        } finally {
            setSaving(false)
        }
    }

    async function openDetails(receiptId) {
        setDetails(null)
        setDetailsLoading(true)
        setShowDetailsModal(true)
        setError('')

        try {
            const response = await api.get(`/goods-receipts/${receiptId}`)
            setDetails(response.data)
        } catch (err) {
            showError(
                err.response?.data?.message ||
                'Unable to load receipt details.'
            )
            setShowDetailsModal(false)
        } finally {
            setDetailsLoading(false)
        }
    }

    async function postReceipt(receipt) {
        const confirmed = window.confirm(
            `Post goods receipt ${receipt.receiptNumber}? This will update inventory balances and create an inventory transaction.`
        )

        if (!confirmed) return

        setPostingId(receipt.receiptId)
        setError('')
        setSuccess('')

        try {
            const response = await api.post(
                `/goods-receipts/${receipt.receiptId}/post`
            )

            showMessage(
                response.data?.message ||
                'Goods receipt posted successfully.'
            )

            await loadPage()

            if (details?.receiptId === receipt.receiptId) {
                await openDetails(receipt.receiptId)
            }
        } catch (err) {
            showError(
                err.response?.data?.message ||
                err.response?.data ||
                'Unable to post goods receipt.'
            )
        } finally {
            setPostingId(null)
        }
    }

    const totalQuantity = lines.reduce(
        (total, line) => total + Number(line.receivedQuantity || 0),
        0
    )

    return (
        <div className="page-content gr-page">
            <div className="gr-header">
                <div>
                    <span className="gr-eyebrow">PROCUREMENT / INBOUND</span>
                    <h1>Goods Receipts</h1>
                    <p>Manage incoming deliveries and record received inventory.</p>
                </div>

                <button
                    className="gr-primary-btn"
                    onClick={openCreateModal}
                >
                    + Create Goods Receipt
                </button>
            </div>

            {error && (
                <div className="gr-alert gr-alert-error">
                    {String(error)}
                    <button onClick={() => setError('')}>×</button>
                </div>
            )}

            {success && (
                <div className="gr-alert gr-alert-success">
                    {success}
                    <button onClick={() => setSuccess('')}>×</button>
                </div>
            )}

            <div className="gr-summary">
                <div className="gr-summary-card">
                    <span>Total Receipts</span>
                    <strong>{receipts.length}</strong>
                    <small>All recorded receipts</small>
                </div>

                <div className="gr-summary-card">
                    <span>Draft Receipts</span>
                    <strong>{draftCount}</strong>
                    <small>Awaiting posting</small>
                </div>

                <div className="gr-summary-card">
                    <span>Posted Receipts</span>
                    <strong>{postedCount}</strong>
                    <small>Inventory transactions created</small>
                </div>
            </div>

            <div className="gr-panel">
                <div className="gr-toolbar">
                    <div>
                        <h2>Receipt Records</h2>
                        <p>Review and manage incoming deliveries.</p>
                    </div>

                    <div className="gr-toolbar-controls">
                        <input
                            type="text"
                            placeholder="Search receipt, PO, warehouse..."
                            value={search}
                            onChange={e => setSearch(e.target.value)}
                        />

                        <select
                            value={statusFilter}
                            onChange={e => setStatusFilter(e.target.value)}
                        >
                            <option value="ALL">All Statuses</option>
                            <option value="DRAFT">Draft</option>
                            <option value="POSTED">Posted</option>
                        </select>

                        <button
                            className="gr-secondary-btn"
                            onClick={loadPage}
                        >
                            Refresh
                        </button>
                    </div>
                </div>

                {loading ? (
                    <div className="gr-empty">Loading goods receipts...</div>
                ) : (
                    <div className="gr-table-wrap">
                        <table className="gr-table">
                            <thead>
                                <tr>
                                    <th>Receipt Number</th>
                                    <th>Purchase Order</th>
                                    <th>Warehouse</th>
                                    <th>Receipt Date</th>
                                    <th>Delivery Reference</th>
                                    <th>Lines</th>
                                    <th>Status</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>

                            <tbody>
                                {filteredReceipts.map(receipt => (
                                    <tr key={receipt.receiptId}>
                                        <td className="gr-primary-cell">
                                            {receipt.receiptNumber}
                                        </td>
                                        <td>{receipt.poNumber || '—'}</td>
                                        <td>{receipt.warehouseName}</td>
                                        <td>{formatDate(receipt.receiptDate)}</td>
                                        <td>{receipt.deliveryReference || '—'}</td>
                                        <td>{receipt.lines?.length || 0}</td>
                                        <td>
                                            <span className={statusClass(receipt.status)}>
                                                {receipt.status}
                                            </span>
                                        </td>
                                        <td>
                                            <div className="gr-actions">
                                                <button
                                                    className="gr-view-btn"
                                                    onClick={() => openDetails(receipt.receiptId)}
                                                >
                                                    View
                                                </button>

                                                {receipt.status === 'DRAFT' && (
                                                    <button
                                                        className="gr-post-btn"
                                                        disabled={postingId === receipt.receiptId}
                                                        onClick={() => postReceipt(receipt)}
                                                    >
                                                        {postingId === receipt.receiptId
                                                            ? 'Posting...'
                                                            : 'Post'}
                                                    </button>
                                                )}
                                            </div>
                                        </td>
                                    </tr>
                                ))}

                                {filteredReceipts.length === 0 && (
                                    <tr>
                                        <td colSpan="8" className="gr-empty">
                                            No goods receipts found.
                                        </td>
                                    </tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>

            {showCreateModal && (
                <div className="gr-modal-backdrop">
                    <div className="gr-modal gr-create-modal">
                        <div className="gr-modal-header">
                            <div>
                                <span className="gr-eyebrow">INBOUND OPERATIONS</span>
                                <h2>Create Goods Receipt</h2>
                                <p>Record a new incoming delivery.</p>
                            </div>

                            <button
                                className="gr-close-btn"
                                onClick={closeCreateModal}
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={saveReceipt}>
                            <div className="gr-form-body">
                                <section className="gr-form-section">
                                    <h3>Receipt Information</h3>

                                    <div className="gr-form-grid">
                                        <div className="gr-field">
                                            <label>Receipt Number *</label>
                                            <input
                                                value={header.receiptNumber}
                                                onChange={e => setHeader({
                                                    ...header,
                                                    receiptNumber: e.target.value
                                                })}
                                                placeholder="GR-2026-001"
                                                required
                                            />
                                        </div>

                                        <div className="gr-field">
                                            <label>Receipt Date *</label>
                                            <input
                                                type="date"
                                                value={header.receiptDate}
                                                onChange={e => setHeader({
                                                    ...header,
                                                    receiptDate: e.target.value
                                                })}
                                                required
                                            />
                                        </div>

                                        <div className="gr-field">
                                            <label>Warehouse *</label>
                                            <select
                                                value={header.warehouseId}
                                                onChange={e => handleWarehouseChange(e.target.value)}
                                                required
                                            >
                                                <option value="">Select warehouse</option>
                                                {activeWarehouses.map(warehouse => (
                                                    <option
                                                        key={warehouse.warehouseId}
                                                        value={warehouse.warehouseId}
                                                    >
                                                        {warehouse.warehouseCode
                                                            ? `${warehouse.warehouseCode} - `
                                                            : ''}
                                                        {warehouse.warehouseName}
                                                    </option>
                                                ))}
                                            </select>
                                        </div>

                                        <div className="gr-field">
                                            <label>Purchase Order</label>
                                            <select
                                                value={header.purchaseOrderId}
                                                onChange={e => handlePurchaseOrderChange(e.target.value)}
                                            >
                                                <option value="">No Purchase Order</option>
                                                {activeSuppliers.map(order => (
                                                    <option
                                                        key={order.purchaseOrderId}
                                                        value={order.purchaseOrderId}
                                                    >
                                                        {order.poNumber} ({order.status})
                                                    </option>
                                                ))}
                                            </select>
                                        </div>

                                        <div className="gr-field">
                                            <label>Delivery Reference</label>
                                            <input
                                                value={header.deliveryReference}
                                                onChange={e => setHeader({
                                                    ...header,
                                                    deliveryReference: e.target.value
                                                })}
                                                placeholder="Delivery receipt / DR number"
                                            />
                                        </div>

                                        <div className="gr-field">
                                            <label>Received By (User ID)</label>
                                            <input
                                                type="number"
                                                min="1"
                                                value={header.receivedBy}
                                                onChange={e => setHeader({
                                                    ...header,
                                                    receivedBy: e.target.value
                                                })}
                                                placeholder="Optional user ID"
                                            />
                                        </div>
                                    </div>
                                </section>

                                <section className="gr-form-section">
                                    <div className="gr-section-heading">
                                        <div>
                                            <h3>Receipt Lines</h3>
                                            <p>Add the items and batches received.</p>
                                        </div>
                                        <span className="gr-line-count">
                                            {lines.length} line(s)
                                        </span>
                                    </div>

                                    <div className="gr-line-entry">
                                        <div className="gr-form-grid">
                                            <div className="gr-field gr-field-wide">
                                                <label>Item *</label>
                                                <select
                                                    value={newLine.itemId}
                                                    onChange={e => {
                                                        handleItemChange(e.target.value)
                                                        loadLotsForItem(e.target.value)
                                                    }}
                                                >
                                                    <option value="">Select item</option>
                                                    {activeItems.map(item => (
                                                        <option key={item.itemId} value={item.itemId}>
                                                            {getItemLabel(item)}
                                                        </option>
                                                    ))}
                                                </select>
                                            </div>

                                            <div className="gr-field">
                                                <label>Inventory Lot *</label>
                                                <select
                                                    value={newLine.lotId}
                                                    onChange={e => setNewLine({
                                                        ...newLine,
                                                        lotId: e.target.value
                                                    })}
                                                    disabled={!newLine.itemId}
                                                >
                                                    <option value="">Select lot</option>
                                                    {lots
                                                        .filter(lot => Number(lot.itemId) === Number(newLine.itemId))
                                                        .map(lot => (
                                                            <option key={lot.lotId} value={lot.lotId}>
                                                                {lot.batchCode}
                                                            </option>
                                                        ))}
                                                </select>

                                                <button
                                                    type="button"
                                                    className="gr-text-btn"
                                                    onClick={openLotModal}
                                                >
                                                    + Register New Lot
                                                </button>
                                            </div>

                                            <div className="gr-field">
                                                <label>Received Quantity *</label>
                                                <input
                                                    type="number"
                                                    min="0.001"
                                                    step="any"
                                                    value={newLine.receivedQuantity}
                                                    onChange={e => setNewLine({
                                                        ...newLine,
                                                        receivedQuantity: e.target.value
                                                    })}
                                                    placeholder="0.000"
                                                />
                                            </div>

                                            <div className="gr-field">
                                                <label>UOM *</label>
                                                <select
                                                    value={newLine.uomId}
                                                    onChange={e => setNewLine({
                                                        ...newLine,
                                                        uomId: e.target.value
                                                    })}
                                                >
                                                    <option value="">Select UOM</option>
                                                    {activeUoms.map(uom => (
                                                        <option key={uom.uomId} value={uom.uomId}>
                                                            {uom.uomCode} - {uom.uomName}
                                                        </option>
                                                    ))}
                                                </select>
                                                <small>Use the item's base UOM.</small>
                                            </div>

                                            <div className="gr-field">
                                                <label>Zone *</label>
                                                <select
                                                    value={newLine.zoneId || ''}
                                                    onChange={e => {
                                                        setNewLine(prev => ({
                                                            ...prev,
                                                            zoneId: e.target.value,
                                                            receivingBinId: ''
                                                        }))
                                                        loadBins(header.warehouseId, e.target.value)
                                                    }}
                                                    disabled={!header.warehouseId}
                                                >
                                                    <option value="">Select zone</option>
                                                    {zones.map(zone => (
                                                        <option key={zone.zoneId} value={zone.zoneId}>
                                                            {zone.zoneCode} - {zone.zoneName}
                                                        </option>
                                                    ))}
                                                </select>
                                            </div>

                                            <div className="gr-field">
                                                <label>Receiving Bin *</label>
                                                <select
                                                    value={newLine.receivingBinId}
                                                    onChange={e => setNewLine({
                                                        ...newLine,
                                                        receivingBinId: e.target.value
                                                    })}
                                                    disabled={!newLine.zoneId}
                                                >
                                                    <option value="">Select bin</option>
                                                    {bins.map(bin => (
                                                        <option key={bin.binId} value={bin.binId}>
                                                            {bin.binCode}
                                                        </option>
                                                    ))}
                                                </select>
                                            </div>

                                            <div className="gr-field">
                                                <label>Inspection Status *</label>
                                                <select
                                                    value={newLine.inspectionStatus}
                                                    onChange={e => setNewLine({
                                                        ...newLine,
                                                        inspectionStatus: e.target.value
                                                    })}
                                                >
                                                    {inspectionStatuses.map(status => (
                                                        <option key={status} value={status}>
                                                            {status}
                                                        </option>
                                                    ))}
                                                </select>
                                            </div>

                                            {header.purchaseOrderId && (
                                                <div className="gr-field gr-field-wide">
                                                    <label>Purchase Order Line</label>
                                                    <select
                                                        value={newLine.poLineId}
                                                        onChange={e => setNewLine({
                                                            ...newLine,
                                                            poLineId: e.target.value
                                                        })}
                                                    >
                                                        <option value="">No PO line reference</option>
                                                        {poLines.map(line => (
                                                            <option key={line.poLineId} value={line.poLineId}>
                                                                {line.itemCode} - {line.itemName} (Ordered: {formatQuantity(line.orderedQuantity)})
                                                            </option>
                                                        ))}
                                                    </select>
                                                </div>
                                            )}
                                        </div>

                                        <div className="gr-add-line-row">
                                            <button
                                                type="button"
                                                className="gr-primary-btn"
                                                onClick={addReceiptLine}
                                            >
                                                + Add Receipt Line
                                            </button>
                                        </div>
                                    </div>

                                    <div className="gr-table-wrap gr-line-table-wrap">
                                        <table className="gr-table gr-line-table">
                                            <thead>
                                                <tr>
                                                    <th>Item</th>
                                                    <th>Batch</th>
                                                    <th>Quantity</th>
                                                    <th>UOM</th>
                                                    <th>Receiving Bin</th>
                                                    <th>Inspection</th>
                                                    <th></th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {lines.map((line, index) => (
                                                    <tr key={`${line.itemId}-${line.lotId}-${index}`}>
                                                        <td>
                                                            <strong>{line.itemCode}</strong>
                                                            <small>{line.itemName}</small>
                                                        </td>
                                                        <td>{line.batchCode}</td>
                                                        <td>{formatQuantity(line.receivedQuantity)}</td>
                                                        <td>{line.uomCode}</td>
                                                        <td>
                                                            {bins.find(bin => Number(bin.binId) === Number(line.receivingBinId))?.binCode
                                                                || line.receivingBinId}
                                                        </td>
                                                        <td>
                                                            <span className={statusClass(line.inspectionStatus)}>
                                                                {line.inspectionStatus}
                                                            </span>
                                                        </td>
                                                        <td>
                                                            <button
                                                                type="button"
                                                                className="gr-delete-btn"
                                                                onClick={() => removeReceiptLine(index)}
                                                            >
                                                                Remove
                                                            </button>
                                                        </td>
                                                    </tr>
                                                ))}

                                                {!lines.length && (
                                                    <tr>
                                                        <td colSpan="7" className="gr-empty">
                                                            No receipt lines added yet.
                                                        </td>
                                                    </tr>
                                                )}
                                            </tbody>
                                        </table>
                                    </div>

                                    <div className="gr-total-row">
                                        <span>Total received quantity</span>
                                        <strong>{formatQuantity(totalQuantity)}</strong>
                                    </div>
                                </section>
                            </div>

                            <div className="gr-modal-footer">
                                <button
                                    type="button"
                                    className="gr-secondary-btn"
                                    onClick={closeCreateModal}
                                    disabled={saving}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="submit"
                                    className="gr-primary-btn"
                                    disabled={saving}
                                >
                                    {saving ? 'Saving...' : 'Save as Draft'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {showLotModal && (
                <div className="gr-modal-backdrop gr-lot-backdrop">
                    <div className="gr-modal gr-lot-modal">
                        <div className="gr-modal-header">
                            <div>
                                <span className="gr-eyebrow">INVENTORY MANAGEMENT</span>
                                <h2>Register Inventory Lot</h2>
                                <p>{getItemLabel(getItem(lotItemId))}</p>
                            </div>

                            <button
                                className="gr-close-btn"
                                type="button"
                                onClick={() => setShowLotModal(false)}
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={saveNewLot}>
                            <div className="gr-form-body">
                                <div className="gr-form-grid">
                                    <div className="gr-field">
                                        <label>Batch Code *</label>
                                        <input
                                            value={newLot.batchCode}
                                            onChange={e => setNewLot({
                                                ...newLot,
                                                batchCode: e.target.value
                                            })}
                                            required
                                        />
                                    </div>

                                    <div className="gr-field">
                                        <label>Supplier Batch Code</label>
                                        <input
                                            value={newLot.supplierBatchCode}
                                            onChange={e => setNewLot({
                                                ...newLot,
                                                supplierBatchCode: e.target.value
                                            })}
                                        />
                                    </div>

                                    <div className="gr-field">
                                        <label>Manufacture Date</label>
                                        <input
                                            type="date"
                                            value={newLot.manufactureDate}
                                            onChange={e => setNewLot({
                                                ...newLot,
                                                manufactureDate: e.target.value
                                            })}
                                        />
                                    </div>

                                    <div className="gr-field">
                                        <label>Expiry Date</label>
                                        <input
                                            type="date"
                                            value={newLot.expiryDate}
                                            onChange={e => setNewLot({
                                                ...newLot,
                                                expiryDate: e.target.value
                                            })}
                                        />
                                    </div>

                                    <div className="gr-field">
                                        <label>Received Date *</label>
                                        <input
                                            type="date"
                                            value={newLot.receivedDate}
                                            onChange={e => setNewLot({
                                                ...newLot,
                                                receivedDate: e.target.value
                                            })}
                                            required
                                        />
                                    </div>

                                    <div className="gr-field">
                                        <label>Quality Status *</label>
                                        <select
                                            value={newLot.qualityStatus}
                                            onChange={e => setNewLot({
                                                ...newLot,
                                                qualityStatus: e.target.value
                                            })}
                                        >
                                            {inspectionStatuses.map(status => (
                                                <option key={status} value={status}>
                                                    {status}
                                                </option>
                                            ))}
                                        </select>
                                    </div>

                                    <div className="gr-field gr-field-full">
                                        <label>Notes</label>
                                        <textarea
                                            rows="3"
                                            value={newLot.notes}
                                            onChange={e => setNewLot({
                                                ...newLot,
                                                notes: e.target.value
                                            })}
                                        />
                                    </div>
                                </div>
                            </div>

                            <div className="gr-modal-footer">
                                <button
                                    type="button"
                                    className="gr-secondary-btn"
                                    onClick={() => setShowLotModal(false)}
                                    disabled={saving}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="submit"
                                    className="gr-primary-btn"
                                    disabled={saving}
                                >
                                    {saving ? 'Registering...' : 'Register Lot'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {showDetailsModal && (
                <div className="gr-modal-backdrop">
                    <div className="gr-modal gr-details-modal">
                        <div className="gr-modal-header">
                            <div>
                                <span className="gr-eyebrow">RECEIPT DETAILS</span>
                                <h2>{details?.receiptNumber || 'Goods Receipt'}</h2>
                                <p>Review receipt information and received items.</p>
                            </div>

                            <button
                                className="gr-close-btn"
                                onClick={() => setShowDetailsModal(false)}
                            >
                                ×
                            </button>
                        </div>

                        {detailsLoading ? (
                            <div className="gr-empty">Loading receipt details...</div>
                        ) : details && (
                            <>
                                <div className="gr-form-body">
                                    <div className="gr-detail-grid">
                                        <div>
                                            <span>Receipt Number</span>
                                            <strong>{details.receiptNumber}</strong>
                                        </div>
                                        <div>
                                            <span>Purchase Order</span>
                                            <strong>{details.poNumber || '—'}</strong>
                                        </div>
                                        <div>
                                            <span>Warehouse</span>
                                            <strong>{details.warehouseName}</strong>
                                        </div>
                                        <div>
                                            <span>Receipt Date</span>
                                            <strong>{formatDate(details.receiptDate)}</strong>
                                        </div>
                                        <div>
                                            <span>Delivery Reference</span>
                                            <strong>{details.deliveryReference || '—'}</strong>
                                        </div>
                                        <div>
                                            <span>Status</span>
                                            <strong>
                                                <span className={statusClass(details.status)}>
                                                    {details.status}
                                                </span>
                                            </strong>
                                        </div>
                                    </div>

                                    <section className="gr-form-section">
                                        <h3>Received Items</h3>

                                        <div className="gr-table-wrap">
                                            <table className="gr-table">
                                                <thead>
                                                    <tr>
                                                        <th>Item</th>
                                                        <th>Batch</th>
                                                        <th>Quantity</th>
                                                        <th>UOM</th>
                                                        <th>Inspection</th>
                                                        <th>Receiving Bin</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                                    {(details.lines || []).map(line => (
                                                        <tr key={line.receiptLineId}>
                                                            <td>
                                                                <strong>{line.itemCode}</strong>
                                                                <small>{line.itemName}</small>
                                                            </td>
                                                            <td>{line.batchCode}</td>
                                                            <td>{formatQuantity(line.receivedQuantity)}</td>
                                                            <td>{getUom(line.uomId)?.uomCode || line.uomId}</td>
                                                            <td>
                                                                <span className={statusClass(line.inspectionStatus)}>
                                                                    {line.inspectionStatus}
                                                                </span>
                                                            </td>
                                                            <td>{line.receivingBinId || '—'}</td>
                                                        </tr>
                                                    ))}

                                                    {!details.lines?.length && (
                                                        <tr>
                                                            <td colSpan="6" className="gr-empty">
                                                                No receipt lines found.
                                                            </td>
                                                        </tr>
                                                    )}
                                                </tbody>
                                            </table>
                                        </div>
                                    </section>
                                </div>

                                <div className="gr-modal-footer">
                                    <button
                                        className="gr-secondary-btn"
                                        onClick={() => setShowDetailsModal(false)}
                                    >
                                        Close
                                    </button>

                                    {details.status === 'DRAFT' && (
                                        <button
                                            className="gr-primary-btn"
                                            disabled={postingId === details.receiptId}
                                            onClick={() => postReceipt(details)}
                                        >
                                            {postingId === details.receiptId
                                                ? 'Posting...'
                                                : 'Post Receipt'}
                                        </button>
                                    )}
                                </div>
                            </>
                        )}
                    </div>
                </div>
            )}
        </div>
    )
}
