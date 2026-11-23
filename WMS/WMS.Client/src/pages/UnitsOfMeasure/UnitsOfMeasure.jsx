import { useEffect, useMemo, useState } from 'react'
import api from '../../services/api'

const emptyForm = {
    uomCode: '',
    uomName: '',
    unitType: ''
}

const getErrorMessage = (error) => {
    const responseData = error.response?.data

    if (typeof responseData === 'string') {
        return responseData
    }

    if (responseData?.message) {
        return responseData.message
    }

    if (responseData?.title) {
        return responseData.title
    }

    if (responseData?.errors) {
        return Object.values(responseData.errors).flat().join(' ')
    }

    return 'Something went wrong. Please try again.'
}

function UnitsOfMeasurePage() {
    const [units, setUnits] = useState([])
    const [loading, setLoading] = useState(true)
    const [refreshing, setRefreshing] = useState(false)
    const [saving, setSaving] = useState(false)
    const [error, setError] = useState('')
    const [search, setSearch] = useState('')
    const [statusFilter, setStatusFilter] = useState('all')
    const [showForm, setShowForm] = useState(false)
    const [editingId, setEditingId] = useState(null)
    const [formData, setFormData] = useState(emptyForm)
    const [formError, setFormError] = useState('')

    const loadUnits = async (isRefresh = false) => {
        try {
            if (isRefresh) {
                setRefreshing(true)
            } else {
                setLoading(true)
            }

            setError('')
            const response = await api.get('/units-of-measure')
            setUnits(Array.isArray(response.data) ? response.data : [])
        } catch (requestError) {
            console.error('Unable to load units of measure:', requestError)
            setError(getErrorMessage(requestError))
        } finally {
            setLoading(false)
            setRefreshing(false)
        }
    }

    useEffect(() => {
        loadUnits()
    }, [])

    const summary = useMemo(() => ({
        total: units.length,
        active: units.filter(unit => unit.isActive).length,
        inactive: units.filter(unit => !unit.isActive).length,
        unitTypes: new Set(units.map(unit => unit.unitType).filter(Boolean)).size
    }), [units])

    const filteredUnits = useMemo(() => {
        const normalizedSearch = search.trim().toLowerCase()

        return units.filter(unit => {
            const matchesSearch = !normalizedSearch || [
                unit.uomCode,
                unit.uomName,
                unit.unitType
            ].some(value => String(value || '').toLowerCase().includes(normalizedSearch))

            const matchesStatus = statusFilter === 'all' ||
                (statusFilter === 'active' && unit.isActive) ||
                (statusFilter === 'inactive' && !unit.isActive)

            return matchesSearch && matchesStatus
        })
    }, [units, search, statusFilter])

    const openCreateForm = () => {
        setEditingId(null)
        setFormData(emptyForm)
        setFormError('')
        setShowForm(true)
    }

    const openEditForm = (unit) => {
        setEditingId(unit.uomId)
        setFormData({
            uomCode: unit.uomCode || '',
            uomName: unit.uomName || '',
            unitType: unit.unitType || ''
        })
        setFormError('')
        setShowForm(true)
    }

    const closeForm = () => {
        if (saving) return
        setShowForm(false)
        setFormError('')
    }

    const handleInputChange = (event) => {
        const { name, value } = event.target
        setFormData(previous => ({
            ...previous,
            [name]: value
        }))
    }

    const handleSave = async (event) => {
        event.preventDefault()
        setFormError('')

        const payload = {
            uomCode: formData.uomCode.trim(),
            uomName: formData.uomName.trim(),
            unitType: formData.unitType.trim()
        }

        if (!payload.uomCode || !payload.uomName || !payload.unitType) {
            setFormError('UOM code, UOM name, and unit type are required.')
            return
        }

        try {
            setSaving(true)

            if (editingId) {
                await api.put(`/units-of-measure/${editingId}`, payload)
            } else {
                await api.post('/units-of-measure', payload)
            }

            setShowForm(false)
            setFormData(emptyForm)
            setEditingId(null)
            await loadUnits(true)
        } catch (requestError) {
            console.error('Unable to save unit of measure:', requestError)
            setFormError(getErrorMessage(requestError))
        } finally {
            setSaving(false)
        }
    }

    const handleDeactivate = async (unit) => {
        const confirmed = window.confirm(
            `Are you sure you want to deactivate ${unit.uomCode} - ${unit.uomName}?`
        )

        if (!confirmed) return

        try {
            await api.patch(`/units-of-measure/${unit.uomId}/deactivate`)
            await loadUnits(true)
        } catch (requestError) {
            console.error('Unable to deactivate unit of measure:', requestError)
            window.alert(getErrorMessage(requestError))
        }
    }

    return (
        <section className="uom-page">
            <div className="uom-header">
                <div>
                    <div className="uom-eyebrow">CONFIGURATION</div>
                    <h1>Units of Measure</h1>
                    <p>Manage the measurement units used by items, purchasing, inventory, and production.</p>
                </div>
                <div className="uom-header-actions">
                    <button
                        type="button"
                        className="uom-secondary-button"
                        onClick={() => loadUnits(true)}
                        disabled={refreshing}
                    >
                        <span className={refreshing ? 'uom-refresh-icon uom-spinning' : 'uom-refresh-icon'}>↻</span>
                        {refreshing ? 'Refreshing...' : 'Refresh'}
                    </button>
                    <button type="button" className="uom-primary-button" onClick={openCreateForm}>
                        <span>＋</span> Add Unit
                    </button>
                </div>
            </div>

            {error && (
                <div className="uom-error-banner" role="alert">
                    <span>{error}</span>
                    <button type="button" onClick={() => loadUnits()}>Try again</button>
                </div>
            )}

            <div className="uom-summary-grid">
                <div className="uom-summary-card">
                    <span className="uom-summary-icon uom-icon-blue">▤</span>
                    <div><span className="uom-summary-label">TOTAL UNITS</span><strong>{loading ? '—' : summary.total}</strong></div>
                </div>
                <div className="uom-summary-card">
                    <span className="uom-summary-icon uom-icon-green">✓</span>
                    <div><span className="uom-summary-label">ACTIVE UNITS</span><strong>{loading ? '—' : summary.active}</strong></div>
                </div>
                <div className="uom-summary-card">
                    <span className="uom-summary-icon uom-icon-gray">◌</span>
                    <div><span className="uom-summary-label">INACTIVE UNITS</span><strong>{loading ? '—' : summary.inactive}</strong></div>
                </div>
                <div className="uom-summary-card">
                    <span className="uom-summary-icon uom-icon-purple">⌗</span>
                    <div><span className="uom-summary-label">UNIT TYPES</span><strong>{loading ? '—' : summary.unitTypes}</strong></div>
                </div>
            </div>

            <div className="uom-panel">
                <div className="uom-panel-heading">
                    <div>
                        <h2>Unit of measure directory</h2>
                        <p>Maintain standard codes and descriptions for consistent inventory quantities.</p>
                    </div>
                    <span className="uom-record-count">{filteredUnits.length} {filteredUnits.length === 1 ? 'record' : 'records'}</span>
                </div>

                <div className="uom-filters">
                    <div className="uom-search-wrap">
                        <span>⌕</span>
                        <input
                            type="search"
                            value={search}
                            onChange={event => setSearch(event.target.value)}
                            placeholder="Search code, name, or unit type..."
                            aria-label="Search units of measure"
                        />
                    </div>
                    <select
                        value={statusFilter}
                        onChange={event => setStatusFilter(event.target.value)}
                        aria-label="Filter by status"
                    >
                        <option value="all">All statuses</option>
                        <option value="active">Active</option>
                        <option value="inactive">Inactive</option>
                    </select>
                </div>

                <div className="uom-table-wrapper">
                    <table className="uom-table">
                        <thead>
                            <tr>
                                <th>UOM CODE</th>
                                <th>UNIT NAME</th>
                                <th>UNIT TYPE</th>
                                <th>STATUS</th>
                                <th className="uom-actions-heading">ACTIONS</th>
                            </tr>
                        </thead>
                        <tbody>
                            {loading ? (
                                <tr><td colSpan="5" className="uom-empty-state">Loading units of measure...</td></tr>
                            ) : filteredUnits.length === 0 ? (
                                <tr>
                                    <td colSpan="5" className="uom-empty-state">
                                        {units.length === 0 ? 'No units of measure have been created yet.' : 'No units match your search or selected filter.'}
                                    </td>
                                </tr>
                            ) : filteredUnits.map(unit => (
                                <tr key={unit.uomId}>
                                    <td><span className="uom-code-pill">{unit.uomCode}</span></td>
                                    <td><strong className="uom-name">{unit.uomName}</strong></td>
                                    <td><span className="uom-type-text">{unit.unitType}</span></td>
                                    <td>
                                        <span className={`uom-status ${unit.isActive ? 'uom-status-active' : 'uom-status-inactive'}`}>
                                            {unit.isActive ? 'Active' : 'Inactive'}
                                        </span>
                                    </td>
                                    <td>
                                        <div className="uom-row-actions">
                                            <button type="button" onClick={() => openEditForm(unit)}>Edit</button>
                                            {unit.isActive && (
                                                <button type="button" className="uom-deactivate-action" onClick={() => handleDeactivate(unit)}>
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
            </div>

            <div className="uom-info-note">
                <span className="uom-info-symbol">i</span>
                <p>Units of measure are referenced by item records and operational documents. Deactivate a unit instead of deleting it to preserve existing transaction references.</p>
            </div>

            {showForm && (
                <div className="uom-modal-overlay" role="presentation" onMouseDown={event => {
                    if (event.target === event.currentTarget) closeForm()
                }}>
                    <div className="uom-modal" role="dialog" aria-modal="true" aria-labelledby="uom-modal-title">
                        <div className="uom-modal-heading">
                            <div>
                                <span className="uom-eyebrow">UNIT CONFIGURATION</span>
                                <h2 id="uom-modal-title">{editingId ? 'Edit Unit of Measure' : 'Add Unit of Measure'}</h2>
                                <p>Enter the standard code, display name, and measurement type.</p>
                            </div>
                            <button type="button" className="uom-modal-close" onClick={closeForm} disabled={saving} aria-label="Close">×</button>
                        </div>

                        <form onSubmit={handleSave}>
                            <div className="uom-form-grid">
                                <label>
                                    <span>UOM Code <b>*</b></span>
                                    <input
                                        name="uomCode"
                                        value={formData.uomCode}
                                        onChange={handleInputChange}
                                        required
                                        maxLength={20}
                                        placeholder="e.g. PCS"
                                        autoFocus
                                    />
                                    <small>Maximum 20 characters. Must be unique.</small>
                                </label>
                                <label>
                                    <span>Unit Name <b>*</b></span>
                                    <input
                                        name="uomName"
                                        value={formData.uomName}
                                        onChange={handleInputChange}
                                        required
                                        maxLength={80}
                                        placeholder="e.g. Pieces"
                                    />
                                    <small>Maximum 80 characters.</small>
                                </label>
                                <label className="uom-full-field">
                                    <span>Unit Type <b>*</b></span>
                                    <input
                                        name="unitType"
                                        value={formData.unitType}
                                        onChange={handleInputChange}
                                        required
                                        maxLength={30}
                                        placeholder="e.g. Quantity, Weight, Volume, Length"
                                    />
                                    <small>Maximum 30 characters. Use a consistent group for similar units.</small>
                                </label>
                            </div>

                            {formError && <div className="uom-form-error" role="alert">{formError}</div>}

                            <div className="uom-modal-actions">
                                <button type="button" className="uom-secondary-button" onClick={closeForm} disabled={saving}>Cancel</button>
                                <button type="submit" className="uom-primary-button" disabled={saving}>
                                    {saving ? 'Saving...' : editingId ? 'Save Changes' : 'Create Unit'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </section>
    )
}

export default UnitsOfMeasurePage
