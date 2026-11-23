
import { useEffect, useMemo, useState } from 'react'
import api from '../../services/api'
import './stocktransfer.css'

function StockTransfersPage() {
    const [transfers, setTransfers] = useState([])
    const [warehouses, setWarehouses] = useState([])
    const [lots, setLots] = useState([])
    const [items, setItems] = useState([])
    const [balances, setBalances] = useState([])

    const [loading, setLoading] = useState(true)
    const [error, setError] = useState('')
    const [message, setMessage] = useState('')

    const [showCreate, setShowCreate] = useState(false)
    const [showReceive, setShowReceive] = useState(false)
    const [selectedTransfer, setSelectedTransfer] = useState(null)

    const [saving, setSaving] = useState(false)

    const [form, setForm] = useState({
        transferNumber: '',
        sourceWarehouseId: '',
        destinationWarehouseId: '',
        lines: []
    })

    const [lineForm, setLineForm] = useState({
        lotId: '',
        sourceZoneId: '',
        sourceBinId: '',
        requestedQuantity: ''
    })

    const [sourceZones, setSourceZones] = useState([])
    const [sourceBins, setSourceBins] = useState([])

    const [receiveForm, setReceiveForm] = useState({
        transferLineId: '',
        destinationZoneId: '',
        destinationBinId: '',
        receivedQuantity: '',
        discrepancyReason: ''
    })

    const [destinationZones, setDestinationZones] = useState([])
    const [destinationBins, setDestinationBins] = useState([])

    const loadData = async () => {
        try {
            setLoading(true)
            setError('')

            const [
                transferRes,
                warehouseRes,
                lotRes,
                itemRes,
                balanceRes
            ] = await Promise.all([
                api.get('/stock-transfers'),
                api.get('/warehouses'),
                api.get('/inventory-lots'),
                api.get('/items'),
                api.get('/inventory-balances')
            ])

            setTransfers(transferRes.data)
            setWarehouses(warehouseRes.data)
            setLots(lotRes.data)
            setItems(itemRes.data)
            setBalances(balanceRes.data)
        } catch (err) {
            console.error('Unable to load stock transfer data:', err)
            setError(
                err.response?.data?.message ||
                'Unable to load stock transfer data.'
            )
        } finally {
            setLoading(false)
        }
    }

    useEffect(() => {
        loadData()
    }, [])

    const activeWarehouses = warehouses.filter(w => w.isActive)

    const getItem = (itemId) =>
        items.find(i => Number(i.itemId) === Number(itemId))

    const getLot = (lotId) =>
        lots.find(l => Number(l.lotId) === Number(lotId))

    const getAvailableBalance = (lotId, binId) => {
        return balances
            .filter(b =>
                Number(b.lotId) === Number(lotId) &&
                Number(b.binId) === Number(binId) &&
                String(b.stockStatus).toUpperCase() === 'AVAILABLE'
            )
            .reduce((total, b) => total + Number(b.availableQuantity || 0), 0)
    }

    const eligibleLots = useMemo(() => {
        return lots.filter(lot => {
            const approved =
                String(lot.qualityStatus).toUpperCase() === 'APPROVED'

            const notExpired =
                !lot.expiryDate ||
                new Date(lot.expiryDate) >= new Date(new Date().toDateString())

            return approved && notExpired
        })
    }, [lots])

    const selectedLot = getLot(lineForm.lotId)
    const selectedItem = selectedLot ? getItem(selectedLot.itemId) : null

    const availableSourceBins = sourceBins.filter(bin => {
        if (!lineForm.lotId) return false
        return getAvailableBalance(lineForm.lotId, bin.binId) > 0
    })

    const selectedSourceAvailable = lineForm.sourceBinId
        ? getAvailableBalance(lineForm.lotId, lineForm.sourceBinId)
        : 0

    const updateForm = (name, value) => {
        setForm(previous => ({
            ...previous,
            [name]: value
        }))
    }

    const loadZones = async (warehouseId, setter) => {
        if (!warehouseId) {
            setter([])
            return
        }

        try {
            const response = await api.get(
                `/warehouses/${warehouseId}/zones`
            )
            setter(response.data.filter(zone => zone.isActive !== false))
        } catch (err) {
            console.error('Unable to load warehouse zones:', err)
            setter([])
        }
    }

    const loadBins = async (warehouseId, zoneId, setter) => {
        if (!warehouseId || !zoneId) {
            setter([])
            return
        }

        try {
            const response = await api.get(
                `/warehouses/${warehouseId}/zones/${zoneId}/bins`
            )

            setter(response.data.filter(bin => bin.isActive))
        } catch (err) {
            console.error('Unable to load storage bins:', err)
            setter([])
        }
    }

    const handleSourceWarehouseChange = async (warehouseId) => {
        setForm(previous => ({
            ...previous,
            sourceWarehouseId: warehouseId,
            lines: []
        }))

        setLineForm({
            lotId: '',
            sourceZoneId: '',
            sourceBinId: '',
            requestedQuantity: ''
        })

        setSourceBins([])
        await loadZones(warehouseId, setSourceZones)
    }

    const handleDestinationWarehouseChange = (warehouseId) => {
        setForm(previous => ({
            ...previous,
            destinationWarehouseId: warehouseId
        }))
    }

    const handleSourceZoneChange = async (zoneId) => {
        setLineForm(previous => ({
            ...previous,
            sourceZoneId: zoneId,
            sourceBinId: ''
        }))

        setSourceBins([])

        await loadBins(
            form.sourceWarehouseId,
            zoneId,
            setSourceBins
        )
    }

    const addTransferLine = () => {
        if (
            !lineForm.lotId ||
            !lineForm.sourceBinId ||
            !lineForm.requestedQuantity
        ) {
            setMessage('Complete all transfer line fields first.')
            return
        }

        const quantity = Number(lineForm.requestedQuantity)

        if (!Number.isFinite(quantity) || quantity <= 0) {
            setMessage('Requested quantity must be greater than zero.')
            return
        }

        if (quantity > selectedSourceAvailable) {
            setMessage(
                `Insufficient available stock. Only ${selectedSourceAvailable} is available.`
            )
            return
        }

        const lot = getLot(lineForm.lotId)
        const item = lot ? getItem(lot.itemId) : null

        if (!lot || !item) {
            setMessage('Unable to identify the selected item or lot.')
            return
        }

        const duplicate = form.lines.some(line =>
            Number(line.lotId) === Number(lineForm.lotId) &&
            Number(line.sourceBinId) === Number(lineForm.sourceBinId)
        )

        if (duplicate) {
            setMessage('This lot and source bin are already in the transfer.')
            return
        }

        setForm(previous => ({
            ...previous,
            lines: [
                ...previous.lines,
                {
                    lotId: Number(lineForm.lotId),
                    sourceBinId: Number(lineForm.sourceBinId),
                    requestedQuantity: quantity,
                    uomId: item.baseUomId,
                    itemCode: item.itemCode,
                    itemName: item.itemName,
                    batchCode: lot.batchCode,
                    uomCode: item.uomCode
                }
            ]
        }))

        setLineForm({
            lotId: '',
            sourceZoneId: '',
            sourceBinId: '',
            requestedQuantity: ''
        })

        setSourceBins([])
        setMessage('')
    }

    const removeTransferLine = (index) => {
        setForm(previous => ({
            ...previous,
            lines: previous.lines.filter((_, i) => i !== index)
        }))
    }

    const createTransfer = async (event) => {
        event.preventDefault()

        if (!form.sourceWarehouseId || !form.destinationWarehouseId) {
            setMessage('Select both source and destination warehouses.')
            return
        }

        if (
            Number(form.sourceWarehouseId) ===
            Number(form.destinationWarehouseId)
        ) {
            setMessage('Source and destination warehouses must be different.')
            return
        }

        if (form.lines.length === 0) {
            setMessage('Add at least one transfer line.')
            return
        }

        try {
            setSaving(true)
            setMessage('')

            const payload = {
                transferNumber: form.transferNumber.trim(),
                sourceWarehouseId: Number(form.sourceWarehouseId),
                destinationWarehouseId: Number(form.destinationWarehouseId),
                lines: form.lines.map(line => ({
                    lotId: line.lotId,
                    sourceBinId: line.sourceBinId,
                    requestedQuantity: line.requestedQuantity,
                    uomId: line.uomId
                }))
            }

            await api.post('/stock-transfers', payload)

            setShowCreate(false)
            setForm({
                transferNumber: '',
                sourceWarehouseId: '',
                destinationWarehouseId: '',
                lines: []
            })
            setSourceZones([])
            setSourceBins([])
            setMessage('Transfer created successfully.')

            await loadData()
        } catch (err) {
            console.error('Unable to create transfer:', err)
            setMessage(
                err.response?.data?.message ||
                (typeof err.response?.data === 'string'
                    ? err.response.data
                    : 'Unable to create transfer.')
            )
        } finally {
            setSaving(false)
        }
    }

    const dispatchTransfer = async (transfer) => {
        const confirmed = window.confirm(
            `Dispatch transfer ${transfer.transferNumber}? This will deduct the requested stock from the source warehouse.`
        )

        if (!confirmed) return

        try {
            setSaving(true)
            setError('')
            setMessage('')

            const payload = {
                lines: (transfer.lines || []).map(line => ({
                    transferLineId: line.transferLineId,
                    dispatchedQuantity: Number(line.requestedQuantity)
                }))
            }

            await api.post(
                `/stock-transfers/${transfer.transferId}/dispatch`,
                payload
            )

            setMessage('Transfer dispatched successfully.')
            await loadData()
        } catch (err) {
            console.error('Unable to dispatch transfer:', err)
            setError(
                err.response?.data?.message ||
                (typeof err.response?.data === 'string'
                    ? err.response.data
                    : 'Unable to dispatch transfer.')
            )
        } finally {
            setSaving(false)
        }
    }

    const openReceive = async (transfer) => {
        setSelectedTransfer(transfer)
        setReceiveForm({
            transferLineId: '',
            destinationZoneId: '',
            destinationBinId: '',
            receivedQuantity: '',
            discrepancyReason: ''
        })
        setDestinationBins([])
        setShowReceive(true)

        await loadZones(
            transfer.destinationWarehouseId,
            setDestinationZones
        )
    }

    const handleDestinationZoneChange = async (zoneId) => {
        setReceiveForm(previous => ({
            ...previous,
            destinationZoneId: zoneId,
            destinationBinId: ''
        }))

        setDestinationBins([])

        await loadBins(
            selectedTransfer.destinationWarehouseId,
            zoneId,
            setDestinationBins
        )
    }

    const selectedReceiveLine = selectedTransfer?.lines?.find(
        line =>
            Number(line.transferLineId) ===
            Number(receiveForm.transferLineId)
    )

    const remainingReceiveQuantity = selectedReceiveLine
        ? Number(selectedReceiveLine.dispatchedQuantity || 0) -
          Number(selectedReceiveLine.receivedQuantity || 0)
        : 0

    const receiveTransfer = async (event) => {
        event.preventDefault()

        if (!selectedTransfer) return

        const quantity = Number(receiveForm.receivedQuantity)

        if (!receiveForm.transferLineId || !receiveForm.destinationBinId) {
            setMessage('Select a transfer line and destination bin.')
            return
        }

        if (!Number.isFinite(quantity) || quantity <= 0) {
            setMessage('Received quantity must be greater than zero.')
            return
        }

        if (quantity > remainingReceiveQuantity) {
            setMessage(
                `Received quantity cannot exceed the remaining quantity of ${remainingReceiveQuantity}.`
            )
            return
        }

        try {
            setSaving(true)
            setMessage('')

            const payload = {
                lines: [{
                    transferLineId: Number(receiveForm.transferLineId),
                    destinationBinId: Number(receiveForm.destinationBinId),
                    receivedQuantity: quantity,
                    discrepancyReason:
                        receiveForm.discrepancyReason.trim() || null
                }]
            }

            await api.post(
                `/stock-transfers/${selectedTransfer.transferId}/receive`,
                payload
            )

            setShowReceive(false)
            setSelectedTransfer(null)
            setMessage('Transfer receipt recorded successfully.')

            await loadData()
        } catch (err) {
            console.error('Unable to receive transfer:', err)
            setMessage(
                err.response?.data?.message ||
                (typeof err.response?.data === 'string'
                    ? err.response.data
                    : 'Unable to receive transfer.')
            )
        } finally {
            setSaving(false)
        }
    }

    const getStatusClass = (status) => {
        switch (String(status).toUpperCase()) {
            case 'DRAFT':
                return 'st-status st-draft'
            case 'DISPATCHED':
                return 'st-status st-dispatched'
            case 'PARTIALLY_RECEIVED':
                return 'st-status st-partial'
            case 'RECEIVED':
                return 'st-status st-received'
            default:
                return 'st-status'
        }
    }

    const getWarehouseName = (warehouseId) => {
        return warehouses.find(
            w => Number(w.warehouseId) === Number(warehouseId)
        )?.warehouseName || '—'
    }

    return (
        <section className="st-page">
            <div className="st-heading">
                <div>
                    <span className="eyebrow">INVENTORY OPERATIONS</span>
                    <h1>Stock transfers</h1>
                    <p>
                        Move approved inventory between warehouse facilities
                        and track dispatch and receiving activities.
                    </p>
                </div>

                <div className="st-heading-actions">
                    <button
                        className="secondary-button"
                        type="button"
                        onClick={loadData}
                        disabled={loading}
                    >
                        ↻ Refresh
                    </button>

                    <button
                        className="primary-button"
                        type="button"
                        onClick={() => {
                            setMessage('')
                            setForm({
                                transferNumber: '',
                                sourceWarehouseId: '',
                                destinationWarehouseId: '',
                                lines: []
                            })
                            setShowCreate(true)
                        }}
                    >
                        + New transfer
                    </button>
                </div>
            </div>

            {message && !showCreate && !showReceive && (
                <div className="st-message">{message}</div>
            )}

            {error && <div className="st-error">{error}</div>}

            <div className="st-summary">
                <div>
                    <span>TOTAL TRANSFERS</span>
                    <strong>{transfers.length}</strong>
                </div>
                <div>
                    <span>DRAFT</span>
                    <strong>
                        {transfers.filter(t =>
                            String(t.status).toUpperCase() === 'DRAFT'
                        ).length}
                    </strong>
                </div>
                <div>
                    <span>IN TRANSIT</span>
                    <strong>
                        {transfers.filter(t =>
                            ['DISPATCHED', 'PARTIALLY_RECEIVED'].includes(
                                String(t.status).toUpperCase()
                            )
                        ).length}
                    </strong>
                </div>
                <div>
                    <span>RECEIVED</span>
                    <strong>
                        {transfers.filter(t =>
                            String(t.status).toUpperCase() === 'RECEIVED'
                        ).length}
                    </strong>
                </div>
            </div>

            <div className="panel st-table-panel">
                <div className="st-table-heading">
                    <div>
                        <span className="eyebrow">TRANSFER REGISTER</span>
                        <h2>Transfer records</h2>
                    </div>
                    <span className="st-record-count">
                        {transfers.length} records
                    </span>
                </div>

                {loading ? (
                    <div className="empty-content">
                        Loading stock transfers...
                    </div>
                ) : transfers.length === 0 ? (
                    <div className="empty-content">
                        <strong>No stock transfers found</strong>
                        <p>Create a transfer to move inventory between warehouses.</p>
                    </div>
                ) : (
                    <div className="table-scroll">
                        <table className="management-table st-table">
                            <thead>
                                <tr>
                                    <th>TRANSFER NUMBER</th>
                                    <th>SOURCE WAREHOUSE</th>
                                    <th>DESTINATION</th>
                                    <th>TRANSFER DATE</th>
                                    <th>LINES</th>
                                    <th>STATUS</th>
                                    <th>ACTIONS</th>
                                </tr>
                            </thead>
                            <tbody>
                                {transfers.map(transfer => {
                                    const status =
                                        String(transfer.status).toUpperCase()

                                    return (
                                        <tr key={transfer.transferId}>
                                            <td>
                                                <strong>
                                                    {transfer.transferNumber}
                                                </strong>
                                            </td>
                                            <td>
                                                {transfer.sourceWarehouse ||
                                                    getWarehouseName(transfer.sourceWarehouseId)}
                                            </td>
                                            <td>
                                                {transfer.destinationWarehouse ||
                                                    getWarehouseName(transfer.destinationWarehouseId)}
                                            </td>
                                            <td>
                                                {transfer.transferDate
                                                    ? new Date(transfer.transferDate).toLocaleDateString('en-PH')
                                                    : '—'}
                                            </td>
                                            <td>
                                                {transfer.lines?.length || 0}
                                            </td>
                                            <td>
                                                <span className={getStatusClass(status)}>
                                                    {status.replaceAll('_', ' ')}
                                                </span>
                                            </td>
                                            <td>
                                                <div className="st-actions">
                                                    {status === 'DRAFT' && (
                                                        <button
                                                            className="edit-button"
                                                            disabled={saving}
                                                            onClick={() =>
                                                                dispatchTransfer(transfer)
                                                            }
                                                        >
                                                            Dispatch
                                                        </button>
                                                    )}

                                                    {[
                                                        'DISPATCHED',
                                                        'PARTIALLY_RECEIVED'
                                                    ].includes(status) && (
                                                        <button
                                                            className="edit-button"
                                                            disabled={saving}
                                                            onClick={() =>
                                                                openReceive(transfer)
                                                            }
                                                        >
                                                            Receive
                                                        </button>
                                                    )}

                                                    <button
                                                        className="st-view-button"
                                                        onClick={() =>
                                                            setSelectedTransfer(transfer)
                                                        }
                                                    >
                                                        Details
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>
                                    )
                                })}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>

            {/* CREATE TRANSFER MODAL */}
            {showCreate && (
                <div className="modal-overlay">
                    <div className="management-modal st-modal">
                        <div className="modal-heading">
                            <div>
                                <span className="eyebrow">INVENTORY MOVEMENT</span>
                                <h2>Create stock transfer</h2>
                            </div>
                            <button
                                className="modal-close"
                                type="button"
                                onClick={() => setShowCreate(false)}
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={createTransfer}>
                            <label>Transfer number</label>
                            <input
                                value={form.transferNumber}
                                onChange={e =>
                                    updateForm('transferNumber', e.target.value)
                                }
                                required
                                placeholder="e.g. TRF-2026-0001"
                            />

                            <div className="st-form-grid">
                                <div>
                                    <label>Source warehouse</label>
                                    <select
                                        value={form.sourceWarehouseId}
                                        onChange={e =>
                                            handleSourceWarehouseChange(e.target.value)
                                        }
                                        required
                                    >
                                        <option value="">Select source</option>
                                        {activeWarehouses.map(w => (
                                            <option
                                                key={w.warehouseId}
                                                value={w.warehouseId}
                                            >
                                                {w.warehouseCode} — {w.warehouseName}
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                <div>
                                    <label>Destination warehouse</label>
                                    <select
                                        value={form.destinationWarehouseId}
                                        onChange={e =>
                                            handleDestinationWarehouseChange(e.target.value)
                                        }
                                        required
                                    >
                                        <option value="">Select destination</option>
                                        {activeWarehouses
                                            .filter(w =>
                                                Number(w.warehouseId) !==
                                                Number(form.sourceWarehouseId)
                                            )
                                            .map(w => (
                                                <option
                                                    key={w.warehouseId}
                                                    value={w.warehouseId}
                                                >
                                                    {w.warehouseCode} — {w.warehouseName}
                                                </option>
                                            ))}
                                    </select>
                                </div>
                            </div>

                            <div className="st-divider"></div>

                            <h3 className="st-subheading">Add transfer line</h3>

                            <label>Approved inventory lot</label>
                            <select
                                value={lineForm.lotId}
                                onChange={e =>
                                    setLineForm(previous => ({
                                        ...previous,
                                        lotId: e.target.value,
                                        sourceBinId: ''
                                    }))
                                }
                                required={false}
                            >
                                <option value="">Select item / batch</option>
                                {eligibleLots.map(lot => {
                                    const item = getItem(lot.itemId)

                                    return (
                                        <option key={lot.lotId} value={lot.lotId}>
                                            {item?.itemCode || 'Item'} — {item?.itemName || ''}
                                            {' | Batch: '}{lot.batchCode}
                                        </option>
                                    )
                                })}
                            </select>

                            {selectedItem && (
                                <div className="st-item-info">
                                    <span>
                                        {selectedItem.itemCode} — {selectedItem.itemName}
                                    </span>
                                    <strong>
                                        Base UOM: {selectedItem.uomCode}
                                    </strong>
                                </div>
                            )}

                            <div className="st-form-grid">
                                <div>
                                    <label>Source zone</label>
                                    <select
                                        value={lineForm.sourceZoneId}
                                        onChange={e =>
                                            handleSourceZoneChange(e.target.value)
                                        }
                                        disabled={!form.sourceWarehouseId}
                                    >
                                        <option value="">Select zone</option>
                                        {sourceZones.map(zone => (
                                            <option
                                                key={zone.zoneId}
                                                value={zone.zoneId}
                                            >
                                                {zone.zoneCode} — {zone.zoneName}
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                <div>
                                    <label>Source bin</label>
                                    <select
                                        value={lineForm.sourceBinId}
                                        onChange={e =>
                                            setLineForm(previous => ({
                                                ...previous,
                                                sourceBinId: e.target.value
                                            }))
                                        }
                                        disabled={!lineForm.sourceZoneId || !lineForm.lotId}
                                    >
                                        <option value="">Select bin</option>
                                        {availableSourceBins.map(bin => (
                                            <option
                                                key={bin.binId}
                                                value={bin.binId}
                                            >
                                                {bin.binCode} — Available: {
                                                    getAvailableBalance(
                                                        lineForm.lotId,
                                                        bin.binId
                                                    )
                                                }
                                            </option>
                                        ))}
                                    </select>
                                </div>
                            </div>

                            <label>Requested quantity</label>
                            <input
                                type="number"
                                min="0"
                                step="any"
                                value={lineForm.requestedQuantity}
                                onChange={e =>
                                    setLineForm(previous => ({
                                        ...previous,
                                        requestedQuantity: e.target.value
                                    }))
                                }
                                placeholder="Enter quantity"
                            />

                            {lineForm.sourceBinId && (
                                <p className="st-availability">
                                    Available stock: {selectedSourceAvailable}
                                    {' '}{selectedItem?.uomCode}
                                </p>
                            )}

                            <button
                                type="button"
                                className="secondary-button st-add-line"
                                onClick={addTransferLine}
                                disabled={
                                    !form.sourceWarehouseId ||
                                    !form.destinationWarehouseId ||
                                    !lineForm.lotId ||
                                    !lineForm.sourceBinId
                                }
                            >
                                + Add line
                            </button>

                            <div className="st-line-list">
                                <h3 className="st-subheading">
                                    Transfer lines ({form.lines.length})
                                </h3>

                                {form.lines.length === 0 ? (
                                    <p className="st-empty-lines">
                                        No transfer lines added yet.
                                    </p>
                                ) : (
                                    form.lines.map((line, index) => (
                                        <div className="st-line-row" key={`${line.lotId}-${line.sourceBinId}`}>
                                            <div>
                                                <strong>
                                                    {line.itemCode} — {line.itemName}
                                                </strong>
                                                <span>
                                                    Batch: {line.batchCode} · Bin ID: {line.sourceBinId}
                                                </span>
                                            </div>
                                            <strong>
                                                {line.requestedQuantity} {line.uomCode}
                                            </strong>
                                            <button
                                                type="button"
                                                onClick={() => removeTransferLine(index)}
                                            >
                                                Remove
                                            </button>
                                        </div>
                                    ))
                                )}
                            </div>

                            {message && (
                                <div className="form-error">{message}</div>
                            )}

                            <div className="modal-actions">
                                <button
                                    type="button"
                                    className="secondary-button"
                                    onClick={() => setShowCreate(false)}
                                >
                                    Cancel
                                </button>
                                <button
                                    type="submit"
                                    className="primary-button"
                                    disabled={saving || form.lines.length === 0}
                                >
                                    {saving ? 'Creating...' : 'Create transfer'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {/* RECEIVE TRANSFER MODAL */}
            {showReceive && selectedTransfer && (
                <div className="modal-overlay">
                    <div className="management-modal st-modal">
                        <div className="modal-heading">
                            <div>
                                <span className="eyebrow">WAREHOUSE RECEIVING</span>
                                <h2>Receive transfer</h2>
                                <p>{selectedTransfer.transferNumber}</p>
                            </div>
                            <button
                                className="modal-close"
                                type="button"
                                onClick={() => setShowReceive(false)}
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={receiveTransfer}>
                            <label>Transfer line</label>
                            <select
                                value={receiveForm.transferLineId}
                                onChange={e =>
                                    setReceiveForm(previous => ({
                                        ...previous,
                                        transferLineId: e.target.value
                                    }))
                                }
                                required
                            >
                                <option value="">Select line to receive</option>
                                {(selectedTransfer.lines || [])
                                    .filter(line =>
                                        Number(line.dispatchedQuantity || 0) >
                                        Number(line.receivedQuantity || 0)
                                    )
                                    .map(line => (
                                        <option
                                            key={line.transferLineId}
                                            value={line.transferLineId}
                                        >
                                            {line.batchCode || `Lot ${line.lotId}`}
                                            {' — Remaining: '}
                                            {Number(line.dispatchedQuantity || 0) -
                                                Number(line.receivedQuantity || 0)}
                                        </option>
                                    ))}
                            </select>

                            {selectedReceiveLine && (
                                <div className="st-receive-info">
                                    <div>
                                        <span>Dispatched</span>
                                        <strong>
                                            {selectedReceiveLine.dispatchedQuantity}
                                        </strong>
                                    </div>
                                    <div>
                                        <span>Previously received</span>
                                        <strong>
                                            {selectedReceiveLine.receivedQuantity}
                                        </strong>
                                    </div>
                                    <div>
                                        <span>Remaining</span>
                                        <strong>{remainingReceiveQuantity}</strong>
                                    </div>
                                </div>
                            )}

                            <label>Destination zone</label>
                            <select
                                value={receiveForm.destinationZoneId}
                                onChange={e =>
                                    handleDestinationZoneChange(e.target.value)
                                }
                                required
                            >
                                <option value="">Select destination zone</option>
                                {destinationZones.map(zone => (
                                    <option
                                        key={zone.zoneId}
                                        value={zone.zoneId}
                                    >
                                        {zone.zoneCode} — {zone.zoneName}
                                    </option>
                                ))}
                            </select>

                            <label>Destination bin</label>
                            <select
                                value={receiveForm.destinationBinId}
                                onChange={e =>
                                    setReceiveForm(previous => ({
                                        ...previous,
                                        destinationBinId: e.target.value
                                    }))
                                }
                                disabled={!receiveForm.destinationZoneId}
                                required
                            >
                                <option value="">Select destination bin</option>
                                {destinationBins.map(bin => (
                                    <option key={bin.binId} value={bin.binId}>
                                        {bin.binCode} — {bin.binType}
                                    </option>
                                ))}
                            </select>

                            <label>Received quantity</label>
                            <input
                                type="number"
                                min="0"
                                step="any"
                                value={receiveForm.receivedQuantity}
                                onChange={e =>
                                    setReceiveForm(previous => ({
                                        ...previous,
                                        receivedQuantity: e.target.value
                                    }))
                                }
                                required
                            />

                            <label>Discrepancy reason (optional)</label>
                            <textarea
                                value={receiveForm.discrepancyReason}
                                onChange={e =>
                                    setReceiveForm(previous => ({
                                        ...previous,
                                        discrepancyReason: e.target.value
                                    }))
                                }
                                placeholder="Enter reason for shortage or discrepancy, if any"
                            />

                            {message && (
                                <div className="form-error">{message}</div>
                            )}

                            <div className="modal-actions">
                                <button
                                    type="button"
                                    className="secondary-button"
                                    onClick={() => setShowReceive(false)}
                                >
                                    Cancel
                                </button>
                                <button
                                    type="submit"
                                    className="primary-button"
                                    disabled={saving || !selectedReceiveLine}
                                >
                                    {saving ? 'Saving...' : 'Confirm receipt'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {/* TRANSFER DETAILS MODAL */}
            {!showReceive && !showCreate && selectedTransfer && (
                <div className="modal-overlay">
                    <div className="management-modal st-modal">
                        <div className="modal-heading">
                            <div>
                                <span className="eyebrow">TRANSFER INFORMATION</span>
                                <h2>{selectedTransfer.transferNumber}</h2>
                            </div>
                            <button
                                className="modal-close"
                                type="button"
                                onClick={() => setSelectedTransfer(null)}
                            >
                                ×
                            </button>
                        </div>

                        <div className="st-detail-grid">
                            <div>
                                <span>Source warehouse</span>
                                <strong>
                                    {selectedTransfer.sourceWarehouse ||
                                        getWarehouseName(selectedTransfer.sourceWarehouseId)}
                                </strong>
                            </div>
                            <div>
                                <span>Destination warehouse</span>
                                <strong>
                                    {selectedTransfer.destinationWarehouse ||
                                        getWarehouseName(selectedTransfer.destinationWarehouseId)}
                                </strong>
                            </div>
                            <div>
                                <span>Transfer status</span>
                                <strong>{selectedTransfer.status}</strong>
                            </div>
                            <div>
                                <span>Transfer date</span>
                                <strong>
                                    {selectedTransfer.transferDate
                                        ? new Date(selectedTransfer.transferDate).toLocaleDateString('en-PH')
                                        : '—'}
                                </strong>
                            </div>
                        </div>

                        <h3 className="st-subheading">Transfer lines</h3>

                        <div className="table-scroll">
                            <table className="management-table">
                                <thead>
                                    <tr>
                                        <th>BATCH</th>
                                        <th>REQUESTED</th>
                                        <th>DISPATCHED</th>
                                        <th>RECEIVED</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {(selectedTransfer.lines || []).map(line => (
                                        <tr key={line.transferLineId}>
                                            <td>{line.batchCode || `Lot ${line.lotId}`}</td>
                                            <td>{line.requestedQuantity}</td>
                                            <td>{line.dispatchedQuantity}</td>
                                            <td>{line.receivedQuantity}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        <div className="modal-actions">
                            <button
                                className="secondary-button"
                                onClick={() => setSelectedTransfer(null)}
                            >
                                Close
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </section>
    )
}

export default StockTransfersPage
