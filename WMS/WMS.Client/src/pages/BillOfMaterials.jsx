import { useEffect, useMemo, useState } from 'react'
import api from '../services/api'

const emptyBomForm = {
    bomCode: '',
    finishedItemId: '',
    versionNumber: 1,
    outputQuantity: 1,
    outputUomId: '',
    effectiveFrom: '',
    effectiveTo: ''
}

const emptyLineForm = {
    componentItemId: '',
    requiredQuantity: '',
    uomId: '',
    wastagePercentage: 0
}

function getItemId(item) {
    return item.itemId ?? item.ItemId
}

function getItemCode(item) {
    return item.itemCode ?? item.ItemCode ?? ''
}

function getItemName(item) {
    return item.itemName ?? item.ItemName ?? ''
}

function getCategoryCode(item) {
    return (
        item.categoryCode ??
        item.CategoryCode ??
        item.category?.categoryCode ??
        item.Category?.CategoryCode ??
        ''
    ).toUpperCase()
}

function getUomId(uom) {
    return uom.uomId ?? uom.UomId
}

function getUomCode(uom) {
    return uom.uomCode ?? uom.UomCode ?? ''
}

function formatDate(date) {
    if (!date) return '—'
    return String(date).slice(0, 10)
}

function formatQuantity(value) {
    return Number(value ?? 0).toLocaleString(undefined, {
        maximumFractionDigits: 6
    })
}

export default function BillOfMaterialsPage() {
    const [boms, setBoms] = useState([])
    const [items, setItems] = useState([])
    const [uoms, setUoms] = useState([])

    const [loading, setLoading] = useState(true)
    const [error, setError] = useState('')
    const [success, setSuccess] = useState('')

    const [search, setSearch] = useState('')
    const [statusFilter, setStatusFilter] = useState('ALL')

    const [selectedBom, setSelectedBom] = useState(null)
    const [bomDetails, setBomDetails] = useState(null)
    const [detailsLoading, setDetailsLoading] = useState(false)

    const [showBomForm, setShowBomForm] = useState(false)
    const [editingBomId, setEditingBomId] = useState(null)
    const [bomForm, setBomForm] = useState(emptyBomForm)
    const [savingBom, setSavingBom] = useState(false)

    const [showLineForm, setShowLineForm] = useState(false)
    const [editingLineId, setEditingLineId] = useState(null)
    const [lineForm, setLineForm] = useState(emptyLineForm)
    const [savingLine, setSavingLine] = useState(false)

    const [activating, setActivating] = useState(false)

    const activeItems = useMemo(
        () => items.filter(item => item.isActive ?? item.IsActive),
        [items]
    )

    const finishedGoods = useMemo(
        () => activeItems.filter(item => getCategoryCode(item) === 'FG'),
        [activeItems]
    )

    const componentItems = useMemo(
        () => activeItems.filter(item => getCategoryCode(item) !== 'FG'),
        [activeItems]
    )

    const activeUoms = useMemo(
        () => uoms.filter(uom => uom.isActive ?? uom.IsActive),
        [uoms]
    )

    const filteredBoms = useMemo(() => {
        const term = search.trim().toLowerCase()

        return boms.filter(bom => {
            const matchesSearch =
                !term ||
                String(bom.bomCode ?? '').toLowerCase().includes(term) ||
                String(bom.finishedItemCode ?? '').toLowerCase().includes(term) ||
                String(bom.finishedItemName ?? '').toLowerCase().includes(term)

            const matchesStatus =
                statusFilter === 'ALL' || bom.status === statusFilter

            return matchesSearch && matchesStatus
        })
    }, [boms, search, statusFilter])

    async function loadData() {
        setLoading(true)
        setError('')

        try {
            const [bomResponse, itemResponse, uomResponse] = await Promise.all([
                api.get('/bill-of-materials'),
                api.get('/items'),
                api.get('/units-of-measure')
            ])

            setBoms(Array.isArray(bomResponse.data) ? bomResponse.data : [])
            setItems(Array.isArray(itemResponse.data) ? itemResponse.data : [])
            setUoms(Array.isArray(uomResponse.data) ? uomResponse.data : [])
        } catch (err) {
            setError(
                err.response?.data?.message ||
                'Unable to load BOM information. Check your API connection.'
            )
        } finally {
            setLoading(false)
        }
    }

    async function loadBomDetails(bomId) {
        setDetailsLoading(true)
        setError('')

        try {
            const response = await api.get(`/bill-of-materials/${bomId}`)
            setBomDetails(response.data)
            setSelectedBom(bomId)
        } catch (err) {
            setError(
                err.response?.data?.message ||
                'Unable to load BOM details.'
            )
        } finally {
            setDetailsLoading(false)
        }
    }

    useEffect(() => {
        loadData()
    }, [])

    function openCreateForm() {
        setEditingBomId(null)
        setBomForm(emptyBomForm)
        setShowBomForm(true)
        setError('')
        setSuccess('')
    }

    function openEditForm(bom) {
        setEditingBomId(bom.bomId)
        setBomForm({
            bomCode: bom.bomCode ?? '',
            finishedItemId: String(bom.finishedItemId ?? ''),
            versionNumber: bom.versionNumber ?? 1,
            outputQuantity: bom.outputQuantity ?? 1,
            outputUomId: String(bom.outputUomId ?? ''),
            effectiveFrom: formatDate(bom.effectiveFrom) === '—'
                ? ''
                : formatDate(bom.effectiveFrom),
            effectiveTo: formatDate(bom.effectiveTo) === '—'
                ? ''
                : formatDate(bom.effectiveTo)
        })
        setShowBomForm(true)
        setError('')
        setSuccess('')
    }

    function updateBomForm(field, value) {
        setBomForm(previous => ({
            ...previous,
            [field]: value
        }))
    }

    async function saveBom(event) {
        event.preventDefault()
        setSavingBom(true)
        setError('')
        setSuccess('')

        const payload = {
            bomCode: bomForm.bomCode.trim(),
            finishedItemId: Number(bomForm.finishedItemId),
            versionNumber: Number(bomForm.versionNumber),
            outputQuantity: Number(bomForm.outputQuantity),
            outputUomId: Number(bomForm.outputUomId),
            effectiveFrom: bomForm.effectiveFrom || null,
            effectiveTo: bomForm.effectiveTo || null
        }

        try {
            if (editingBomId) {
                await api.put(
                    `/bill-of-materials/${editingBomId}`,
                    payload
                )
                setSuccess('BOM updated successfully.')
                await loadData()
                await loadBomDetails(editingBomId)
            } else {
                const response = await api.post(
                    '/bill-of-materials',
                    payload
                )
                const newBomId =
                    response.data.bomId ?? response.data.BomId

                setSuccess('BOM created successfully.')
                await loadData()

                if (newBomId) {
                    await loadBomDetails(newBomId)
                }
            }

            setShowBomForm(false)
        } catch (err) {
            setError(
                err.response?.data?.message ||
                'Unable to save BOM.'
            )
        } finally {
            setSavingBom(false)
        }
    }

    function openAddLineForm() {
        setEditingLineId(null)
        setLineForm(emptyLineForm)
        setShowLineForm(true)
        setError('')
        setSuccess('')
    }

    function openEditLineForm(line) {
        setEditingLineId(line.bomLineId)
        setLineForm({
            componentItemId: String(line.componentItemId ?? ''),
            requiredQuantity: line.requiredQuantity ?? '',
            uomId: String(line.uomId ?? ''),
            wastagePercentage: line.wastagePercentage ?? 0
        })
        setShowLineForm(true)
        setError('')
        setSuccess('')
    }

    async function saveLine(event) {
        event.preventDefault()

        if (!selectedBom) return

        setSavingLine(true)
        setError('')
        setSuccess('')

        const payload = {
            componentItemId: Number(lineForm.componentItemId),
            requiredQuantity: Number(lineForm.requiredQuantity),
            uomId: Number(lineForm.uomId),
            wastagePercentage: Number(lineForm.wastagePercentage)
        }

        try {
            if (editingLineId) {
                await api.put(
                    `/bill-of-materials/${selectedBom}/lines/${editingLineId}`,
                    payload
                )
                setSuccess('BOM component updated successfully.')
            } else {
                await api.post(
                    `/bill-of-materials/${selectedBom}/lines`,
                    payload
                )
                setSuccess('BOM component added successfully.')
            }

            setShowLineForm(false)
            await loadBomDetails(selectedBom)
            await loadData()
        } catch (err) {
            setError(
                err.response?.data?.message ||
                'Unable to save BOM component.'
            )
        } finally {
            setSavingLine(false)
        }
    }

    async function deleteLine(line) {
        const confirmed = window.confirm(
            `Remove ${line.componentItemCode} from this BOM?`
        )

        if (!confirmed) return

        setError('')
        setSuccess('')

        try {
            await api.delete(
                `/bill-of-materials/${selectedBom}/lines/${line.bomLineId}`
            )
            setSuccess('BOM component removed successfully.')
            await loadBomDetails(selectedBom)
            await loadData()
        } catch (err) {
            setError(
                err.response?.data?.message ||
                'Unable to remove BOM component.'
            )
        }
    }

    async function activateBom() {
        if (!selectedBom) return

        const confirmed = window.confirm(
            'Are you sure you want to activate this BOM? Draft BOMs can no longer be edited after activation.'
        )

        if (!confirmed) return

        setActivating(true)
        setError('')
        setSuccess('')

        try {
            await api.post(
                `/bill-of-materials/${selectedBom}/activate`
            )

            setSuccess('BOM activated successfully.')
            await loadBomDetails(selectedBom)
            await loadData()
        } catch (err) {
            setError(
                err.response?.data?.message ||
                'Unable to activate BOM.'
            )
        } finally {
            setActivating(false)
        }
    }

    function closeDetails() {
        setSelectedBom(null)
        setBomDetails(null)
        setShowBomForm(false)
        setShowLineForm(false)
    }

    const draftCount = boms.filter(bom => bom.status === 'DRAFT').length
    const activeCount = boms.filter(bom => bom.status === 'ACTIVE').length

    return (
        <div className="bom-page">
            <div className="bom-header">
                <div>
                    <div className="bom-eyebrow">MANUFACTURING / PRODUCTION</div>
                    <h1>Bill of Materials</h1>
                    <p>Manage finished products and their required components.</p>
                </div>

                <button
                    className="bom-primary-btn"
                    onClick={openCreateForm}
                >
                    + Create BOM
                </button>
            </div>

            {error && (
                <div className="bom-alert bom-alert-error">
                    {error}
                    <button onClick={() => setError('')}>×</button>
                </div>
            )}

            {success && (
                <div className="bom-alert bom-alert-success">
                    {success}
                    <button onClick={() => setSuccess('')}>×</button>
                </div>
            )}

            <div className="bom-summary">
                <div className="bom-summary-card">
                    <span>TOTAL BOMs</span>
                    <strong>{boms.length}</strong>
                </div>
                <div className="bom-summary-card">
                    <span>ACTIVE BOMs</span>
                    <strong>{activeCount}</strong>
                </div>
                <div className="bom-summary-card">
                    <span>DRAFT BOMs</span>
                    <strong>{draftCount}</strong>
                </div>
            </div>

            <div className="bom-toolbar">
                <input
                    type="text"
                    placeholder="Search BOM code or finished product..."
                    value={search}
                    onChange={event => setSearch(event.target.value)}
                />

                <select
                    value={statusFilter}
                    onChange={event => setStatusFilter(event.target.value)}
                >
                    <option value="ALL">All statuses</option>
                    <option value="DRAFT">Draft</option>
                    <option value="ACTIVE">Active</option>
                </select>

                <button
                    className="bom-secondary-btn"
                    onClick={loadData}
                    disabled={loading}
                >
                    Refresh
                </button>
            </div>

            <div className="bom-table-card">
                <div className="bom-section-heading">
                    <div>
                        <h2>BOM Records</h2>
                        <p>{filteredBoms.length} record(s) found</p>
                    </div>
                </div>

                {loading ? (
                    <div className="bom-empty">Loading BOM records...</div>
                ) : filteredBoms.length === 0 ? (
                    <div className="bom-empty">
                        No BOM records found.
                    </div>
                ) : (
                    <div className="bom-table-wrapper">
                        <table className="bom-table">
                            <thead>
                                <tr>
                                    <th>BOM CODE</th>
                                    <th>FINISHED PRODUCT</th>
                                    <th>VERSION</th>
                                    <th>OUTPUT</th>
                                    <th>COMPONENTS</th>
                                    <th>EFFECTIVE FROM</th>
                                    <th>STATUS</th>
                                    <th>ACTION</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredBoms.map(bom => (
                                    <tr key={bom.bomId}>
                                        <td className="bom-code">
                                            {bom.bomCode}
                                        </td>
                                        <td>
                                            <strong>{bom.finishedItemCode}</strong>
                                            <small>{bom.finishedItemName}</small>
                                        </td>
                                        <td>V{bom.versionNumber}</td>
                                        <td>
                                            {formatQuantity(bom.outputQuantity)}{' '}
                                            {bom.outputUom}
                                        </td>
                                        <td>{bom.componentCount}</td>
                                        <td>{formatDate(bom.effectiveFrom)}</td>
                                        <td>
                                            <span className={`bom-status ${String(bom.status).toLowerCase()}`}>
                                                {bom.status}
                                            </span>
                                        </td>
                                        <td>
                                            <button
                                                className="bom-table-action"
                                                onClick={() => loadBomDetails(bom.bomId)}
                                            >
                                                View
                                            </button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>

            {showBomForm && (
                <div className="bom-modal-backdrop">
                    <div className="bom-modal">
                        <div className="bom-modal-header">
                            <div>
                                <span className="bom-eyebrow">BOM CONFIGURATION</span>
                                <h2>{editingBomId ? 'Edit BOM' : 'Create BOM'}</h2>
                            </div>
                            <button
                                className="bom-close-btn"
                                onClick={() => setShowBomForm(false)}
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={saveBom}>
                            <div className="bom-form-grid">
                                <label>
                                    BOM Code *
                                    <input
                                        required
                                        value={bomForm.bomCode}
                                        onChange={e => updateBomForm('bomCode', e.target.value)}
                                        placeholder="e.g. BOM-FG-001"
                                    />
                                </label>

                                <label>
                                    Version *
                                    <input
                                        required
                                        type="number"
                                        min="1"
                                        step="1"
                                        value={bomForm.versionNumber}
                                        onChange={e => updateBomForm('versionNumber', e.target.value)}
                                    />
                                </label>

                                <label className="bom-full-width">
                                    Finished Product *
                                    <select
                                        required
                                        value={bomForm.finishedItemId}
                                        onChange={e => updateBomForm('finishedItemId', e.target.value)}
                                    >
                                        <option value="">Select finished product</option>
                                        {finishedGoods.map(item => (
                                            <option
                                                key={getItemId(item)}
                                                value={getItemId(item)}
                                            >
                                                {getItemCode(item)} — {getItemName(item)}
                                            </option>
                                        ))}
                                    </select>
                                </label>

                                <label>
                                    Output Quantity *
                                    <input
                                        required
                                        type="number"
                                        min="0.000001"
                                        step="any"
                                        value={bomForm.outputQuantity}
                                        onChange={e => updateBomForm('outputQuantity', e.target.value)}
                                    />
                                </label>

                                <label>
                                    Output UOM *
                                    <select
                                        required
                                        value={bomForm.outputUomId}
                                        onChange={e => updateBomForm('outputUomId', e.target.value)}
                                    >
                                        <option value="">Select UOM</option>
                                        {activeUoms.map(uom => (
                                            <option key={getUomId(uom)} value={getUomId(uom)}>
                                                {getUomCode(uom)}
                                            </option>
                                        ))}
                                    </select>
                                </label>

                                <label>
                                    Effective From
                                    <input
                                        type="date"
                                        value={bomForm.effectiveFrom}
                                        onChange={e => updateBomForm('effectiveFrom', e.target.value)}
                                    />
                                </label>

                                <label>
                                    Effective To
                                    <input
                                        type="date"
                                        min={bomForm.effectiveFrom || undefined}
                                        value={bomForm.effectiveTo}
                                        onChange={e => updateBomForm('effectiveTo', e.target.value)}
                                    />
                                </label>
                            </div>

                            <div className="bom-modal-actions">
                                <button
                                    type="button"
                                    className="bom-secondary-btn"
                                    onClick={() => setShowBomForm(false)}
                                >
                                    Cancel
                                </button>
                                <button
                                    type="submit"
                                    className="bom-primary-btn"
                                    disabled={savingBom}
                                >
                                    {savingBom ? 'Saving...' : 'Save BOM'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {showLineForm && (
                <div className="bom-modal-backdrop">
                    <div className="bom-modal">
                        <div className="bom-modal-header">
                            <div>
                                <span className="bom-eyebrow">BOM COMPONENT</span>
                                <h2>{editingLineId ? 'Edit Component' : 'Add Component'}</h2>
                            </div>
                            <button
                                className="bom-close-btn"
                                onClick={() => setShowLineForm(false)}
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={saveLine}>
                            <div className="bom-form-grid">
                                <label className="bom-full-width">
                                    Component Item *
                                    <select
                                        required
                                        value={lineForm.componentItemId}
                                        onChange={e => setLineForm(previous => ({
                                            ...previous,
                                            componentItemId: e.target.value
                                        }))}
                                    >
                                        <option value="">Select component</option>
                                        {componentItems.map(item => (
                                            <option
                                                key={getItemId(item)}
                                                value={getItemId(item)}
                                            >
                                                {getItemCode(item)} — {getItemName(item)}
                                            </option>
                                        ))}
                                    </select>
                                </label>

                                <label>
                                    Required Quantity *
                                    <input
                                        required
                                        type="number"
                                        min="0.000001"
                                        step="any"
                                        value={lineForm.requiredQuantity}
                                        onChange={e => setLineForm(previous => ({
                                            ...previous,
                                            requiredQuantity: e.target.value
                                        }))}
                                    />
                                </label>

                                <label>
                                    Unit of Measure *
                                    <select
                                        required
                                        value={lineForm.uomId}
                                        onChange={e => setLineForm(previous => ({
                                            ...previous,
                                            uomId: e.target.value
                                        }))}
                                    >
                                        <option value="">Select UOM</option>
                                        {activeUoms.map(uom => (
                                            <option key={getUomId(uom)} value={getUomId(uom)}>
                                                {getUomCode(uom)}
                                            </option>
                                        ))}
                                    </select>
                                </label>

                                <label className="bom-full-width">
                                    Wastage Percentage (%)
                                    <input
                                        type="number"
                                        min="0"
                                        max="100"
                                        step="any"
                                        value={lineForm.wastagePercentage}
                                        onChange={e => setLineForm(previous => ({
                                            ...previous,
                                            wastagePercentage: e.target.value
                                        }))}
                                    />
                                </label>
                            </div>

                            <div className="bom-modal-actions">
                                <button
                                    type="button"
                                    className="bom-secondary-btn"
                                    onClick={() => setShowLineForm(false)}
                                >
                                    Cancel
                                </button>
                                <button
                                    type="submit"
                                    className="bom-primary-btn"
                                    disabled={savingLine}
                                >
                                    {savingLine ? 'Saving...' : 'Save Component'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {selectedBom && (
                <div className="bom-modal-backdrop">
                    <div className="bom-modal bom-details-modal">
                        <div className="bom-modal-header">
                            <div>
                                <span className="bom-eyebrow">BILL OF MATERIALS DETAILS</span>
                                <h2>{bomDetails?.bomCode ?? 'BOM Details'}</h2>
                            </div>
                            <button
                                className="bom-close-btn"
                                onClick={closeDetails}
                            >
                                ×
                            </button>
                        </div>

                        {detailsLoading || !bomDetails ? (
                            <div className="bom-empty">Loading BOM details...</div>
                        ) : (
                            <>
                                <div className="bom-detail-grid">
                                    <div>
                                        <span>FINISHED PRODUCT</span>
                                        <strong>{bomDetails.finishedItemCode}</strong>
                                        <small>{bomDetails.finishedItemName}</small>
                                    </div>
                                    <div>
                                        <span>VERSION</span>
                                        <strong>V{bomDetails.versionNumber}</strong>
                                    </div>
                                    <div>
                                        <span>OUTPUT QUANTITY</span>
                                        <strong>
                                            {formatQuantity(bomDetails.outputQuantity)}{' '}
                                            {bomDetails.outputUom}
                                        </strong>
                                    </div>
                                    <div>
                                        <span>STATUS</span>
                                        <strong>
                                            <span className={`bom-status ${String(bomDetails.status).toLowerCase()}`}>
                                                {bomDetails.status}
                                            </span>
                                        </strong>
                                    </div>
                                    <div>
                                        <span>EFFECTIVE FROM</span>
                                        <strong>{formatDate(bomDetails.effectiveFrom)}</strong>
                                    </div>
                                    <div>
                                        <span>EFFECTIVE TO</span>
                                        <strong>{formatDate(bomDetails.effectiveTo)}</strong>
                                    </div>
                                </div>

                                <div className="bom-components-heading">
                                    <div>
                                        <h3>Required Components</h3>
                                        <p>{bomDetails.lines?.length ?? 0} component(s)</p>
                                    </div>

                                    {bomDetails.status === 'DRAFT' && (
                                        <button
                                            className="bom-primary-btn"
                                            onClick={openAddLineForm}
                                        >
                                            + Add Component
                                        </button>
                                    )}
                                </div>

                                {!bomDetails.lines?.length ? (
                                    <div className="bom-empty">
                                        No components have been added to this BOM.
                                    </div>
                                ) : (
                                    <div className="bom-table-wrapper">
                                        <table className="bom-table">
                                            <thead>
                                                <tr>
                                                    <th>COMPONENT</th>
                                                    <th>REQUIRED QTY</th>
                                                    <th>UOM</th>
                                                    <th>WASTAGE</th>
                                                    {bomDetails.status === 'DRAFT' && <th>ACTION</th>}
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {bomDetails.lines.map(line => (
                                                    <tr key={line.bomLineId}>
                                                        <td>
                                                            <strong>{line.componentItemCode}</strong>
                                                            <small>{line.componentItemName}</small>
                                                        </td>
                                                        <td>{formatQuantity(line.requiredQuantity)}</td>
                                                        <td>{line.uomCode}</td>
                                                        <td>{formatQuantity(line.wastagePercentage)}%</td>
                                                        {bomDetails.status === 'DRAFT' && (
                                                            <td>
                                                                <div className="bom-line-actions">
                                                                    <button
                                                                        className="bom-table-action"
                                                                        onClick={() => openEditLineForm(line)}
                                                                    >
                                                                        Edit
                                                                    </button>
                                                                    <button
                                                                        className="bom-table-action bom-delete-action"
                                                                        onClick={() => deleteLine(line)}
                                                                    >
                                                                        Remove
                                                                    </button>
                                                                </div>
                                                            </td>
                                                        )}
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    </div>
                                )}

                                <div className="bom-modal-actions">
                                    <button
                                        className="bom-secondary-btn"
                                        onClick={closeDetails}
                                    >
                                        Close
                                    </button>

                                    {bomDetails.status === 'DRAFT' && (
                                        <>
                                            <button
                                                className="bom-secondary-btn"
                                                onClick={() => openEditForm(bomDetails)}
                                            >
                                                Edit BOM
                                            </button>
                                            <button
                                                className="bom-primary-btn"
                                                onClick={activateBom}
                                                disabled={activating}
                                            >
                                                {activating ? 'Activating...' : 'Activate BOM'}
                                            </button>
                                        </>
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
