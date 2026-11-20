import { useEffect, useState } from 'react'
import api from '../services/api'

export default function StorageLocationsPage() {
    const [warehouses, setWarehouses] = useState([])
    const [zones, setZones] = useState([])
    const [bins, setBins] = useState([])

    const [selectedWarehouse, setSelectedWarehouse] = useState('')
    const [selectedZone, setSelectedZone] = useState('')

    const [loading, setLoading] = useState(false)
    const [error, setError] = useState('')
    const [success, setSuccess] = useState('')

    const [zoneModal, setZoneModal] = useState(false)
    const [binModal, setBinModal] = useState(false)
    const [editingZone, setEditingZone] = useState(null)
    const [editingBin, setEditingBin] = useState(null)

    const [zoneForm, setZoneForm] = useState({
        zoneCode: '',
        zoneName: ''
    })

    const [binForm, setBinForm] = useState({
        binCode: '',
        binType: '',
        isActive: true
    })

    useEffect(() => {
        loadWarehouses()
    }, [])

    useEffect(() => {
        if (selectedWarehouse) {
            loadZones(selectedWarehouse)
        } else {
            setZones([])
            setSelectedZone('')
            setBins([])
        }
    }, [selectedWarehouse])

    useEffect(() => {
        if (selectedWarehouse && selectedZone) {
            loadBins(selectedWarehouse, selectedZone)
        } else {
            setBins([])
        }
    }, [selectedWarehouse, selectedZone])

    async function loadWarehouses() {
        try {
            setLoading(true)
            setError('')

            const response = await api.get('/warehouses')
            const activeWarehouses = response.data.filter(
                warehouse => warehouse.isActive !== false
            )

            setWarehouses(activeWarehouses)

            if (activeWarehouses.length > 0) {
                setSelectedWarehouse(String(activeWarehouses[0].warehouseId))
            }
        } catch (err) {
            setError(err.response?.data?.message || 'Failed to load warehouses.')
        } finally {
            setLoading(false)
        }
    }

    async function loadZones(warehouseId) {
        try {
            setLoading(true)
            setError('')

            const response = await api.get(`/warehouses/${warehouseId}/zones`)
            setZones(response.data)

            setSelectedZone(current => {
                const stillExists = response.data.some(
                    zone => String(zone.zoneId) === String(current)
                )

                return stillExists
                    ? current
                    : response.data.length > 0
                        ? String(response.data[0].zoneId)
                        : ''
            })
        } catch (err) {
            setError(err.response?.data?.message || 'Failed to load zones.')
            setZones([])
            setSelectedZone('')
            setBins([])
        } finally {
            setLoading(false)
        }
    }

    async function loadBins(warehouseId, zoneId) {
        try {
            setLoading(true)
            setError('')

            const response = await api.get(
                `/warehouses/${warehouseId}/zones/${zoneId}/bins`
            )

            setBins(response.data)
        } catch (err) {
            setError(err.response?.data?.message || 'Failed to load storage bins.')
            setBins([])
        } finally {
            setLoading(false)
        }
    }

    function openAddZone() {
        setEditingZone(null)
        setZoneForm({
            zoneCode: '',
            zoneName: ''
        })
        setZoneModal(true)
    }

    function openEditZone(zone) {
        setEditingZone(zone)
        setZoneForm({
            zoneCode: zone.zoneCode || '',
            zoneName: zone.zoneName || ''
        })
        setZoneModal(true)
    }

    async function saveZone(event) {
        event.preventDefault()

        if (!selectedWarehouse) {
            setError('Please select a warehouse.')
            return
        }

        try {
            setError('')
            setSuccess('')

            if (editingZone) {
                await api.put(
                    `/warehouses/${selectedWarehouse}/zones/${editingZone.zoneId}`,
                    {
                        zoneName: zoneForm.zoneName
                    }
                )

                setSuccess('Zone updated successfully.')
            } else {
                await api.post(
                    `/warehouses/${selectedWarehouse}/zones`,
                    {
                        zoneCode: zoneForm.zoneCode,
                        zoneName: zoneForm.zoneName
                    }
                )

                setSuccess('Zone created successfully.')
            }

            setZoneModal(false)
            await loadZones(selectedWarehouse)
        } catch (err) {
            setError(
                err.response?.data?.message ||
                err.response?.data?.title ||
                'Failed to save zone.'
            )
        }
    }

    function openAddBin() {
        setEditingBin(null)
        setBinForm({
            binCode: '',
            binType: '',
            isActive: true
        })
        setBinModal(true)
    }

    function openEditBin(bin) {
        setEditingBin(bin)
        setBinForm({
            binCode: bin.binCode || '',
            binType: bin.binType || '',
            isActive: bin.isActive
        })
        setBinModal(true)
    }

    async function saveBin(event) {
        event.preventDefault()

        if (!selectedWarehouse || !selectedZone) {
            setError('Please select a warehouse and zone.')
            return
        }

        try {
            setError('')
            setSuccess('')

            if (editingBin) {
                await api.put(
                    `/warehouses/${selectedWarehouse}/zones/${selectedZone}/bins/${editingBin.binId}`,
                    {
                        binType: binForm.binType,
                        isActive: binForm.isActive
                    }
                )

                setSuccess('Storage bin updated successfully.')
            } else {
                await api.post(
                    `/warehouses/${selectedWarehouse}/zones/${selectedZone}/bins`,
                    {
                        binCode: binForm.binCode,
                        binType: binForm.binType
                    }
                )

                setSuccess('Storage bin created successfully.')
            }

            setBinModal(false)
            await loadBins(selectedWarehouse, selectedZone)
        } catch (err) {
            setError(
                err.response?.data?.message ||
                err.response?.data?.title ||
                'Failed to save storage bin.'
            )
        }
    }

    const selectedWarehouseData = warehouses.find(
        warehouse => String(warehouse.warehouseId) === String(selectedWarehouse)
    )

    const selectedZoneData = zones.find(
        zone => String(zone.zoneId) === String(selectedZone)
    )

    return (
        <div className="sl-page">
            <div className="sl-heading">
                <div>
                    <p className="sl-eyebrow">WAREHOUSE CONFIGURATION</p>
                    <h1>Storage Locations</h1>
                    <p>Manage warehouse zones and storage bins.</p>
                </div>

                <button
                    className="sl-primary-button"
                    onClick={openAddZone}
                    disabled={!selectedWarehouse}
                >
                    + Add Zone
                </button>
            </div>

            {error && (
                <div className="sl-alert sl-error">
                    {error}
                    <button onClick={() => setError('')}>×</button>
                </div>
            )}

            {success && (
                <div className="sl-alert sl-success">
                    {success}
                    <button onClick={() => setSuccess('')}>×</button>
                </div>
            )}

            <div className="sl-overview">
                <div className="sl-overview-card">
                    <span>WAREHOUSE</span>
                    <strong>{warehouses.length}</strong>
                    <small>Available warehouses</small>
                </div>

                <div className="sl-overview-card">
                    <span>ZONES</span>
                    <strong>{zones.length}</strong>
                    <small>In selected warehouse</small>
                </div>

                <div className="sl-overview-card">
                    <span>STORAGE BINS</span>
                    <strong>{bins.length}</strong>
                    <small>In selected zone</small>
                </div>

                <div className="sl-overview-card">
                    <span>ACTIVE BINS</span>
                    <strong>{bins.filter(bin => bin.isActive).length}</strong>
                    <small>Currently available locations</small>
                </div>
            </div>

            <div className="sl-content">
                <section className="sl-panel">
                    <div className="sl-panel-heading">
                        <div>
                            <h2>Warehouse</h2>
                            <p>Select a warehouse to manage its locations.</p>
                        </div>
                    </div>

                    <label className="sl-label" htmlFor="sl-warehouse">
                        Select Warehouse
                    </label>

                    <select
                        id="sl-warehouse"
                        className="sl-select"
                        value={selectedWarehouse}
                        onChange={event => {
                            setSelectedWarehouse(event.target.value)
                            setSelectedZone('')
                            setZones([])
                            setBins([])
                        }}
                    >
                        <option value="">Select warehouse</option>
                        {warehouses.map(warehouse => (
                            <option
                                key={warehouse.warehouseId}
                                value={warehouse.warehouseId}
                            >
                                {warehouse.warehouseCode} - {warehouse.warehouseName}
                            </option>
                        ))}
                    </select>

                    {selectedWarehouseData && (
                        <div className="sl-selected-info">
                            <span>SELECTED WAREHOUSE</span>
                            <strong>{selectedWarehouseData.warehouseName}</strong>
                            <small>{selectedWarehouseData.warehouseCode}</small>
                        </div>
                    )}
                </section>

                <section className="sl-panel">
                    <div className="sl-panel-heading">
                        <div>
                            <h2>Zones</h2>
                            <p>
                                {selectedWarehouseData
                                    ? `Zones under ${selectedWarehouseData.warehouseCode}`
                                    : 'Select a warehouse first.'}
                            </p>
                        </div>

                        <button
                            className="sl-secondary-button"
                            onClick={openAddZone}
                            disabled={!selectedWarehouse}
                        >
                            + Add Zone
                        </button>
                    </div>

                    <div className="sl-zone-list">
                        {zones.length === 0 ? (
                            <div className="sl-empty">
                                {loading ? 'Loading zones...' : 'No zones found.'}
                            </div>
                        ) : (
                            zones.map(zone => (
                                <div
                                    key={zone.zoneId}
                                    className={`sl-zone-item ${
                                        String(selectedZone) === String(zone.zoneId)
                                            ? 'selected'
                                            : ''
                                    }`}
                                >
                                    <button
                                        className="sl-zone-select"
                                        onClick={() => setSelectedZone(String(zone.zoneId))}
                                    >
                                        <span className="sl-zone-icon">▦</span>
                                        <span>
                                            <strong>{zone.zoneName}</strong>
                                            <small>{zone.zoneCode}</small>
                                        </span>
                                    </button>

                                    <button
                                        className="sl-edit-button"
                                        onClick={() => openEditZone(zone)}
                                    >
                                        Edit
                                    </button>
                                </div>
                            ))
                        )}
                    </div>
                </section>
            </div>

            <section className="sl-panel sl-bin-panel">
                <div className="sl-panel-heading">
                    <div>
                        <h2>Storage Bins</h2>
                        <p>
                            {selectedZoneData
                                ? `${selectedZoneData.zoneCode} - ${selectedZoneData.zoneName}`
                                : 'Select a zone to view its storage bins.'}
                        </p>
                    </div>

                    <button
                        className="sl-primary-button"
                        onClick={openAddBin}
                        disabled={!selectedZone}
                    >
                        + Add Bin
                    </button>
                </div>

                <div className="sl-table-wrapper">
                    <table className="sl-table">
                        <thead>
                            <tr>
                                <th>BIN CODE</th>
                                <th>BIN TYPE</th>
                                <th>STATUS</th>
                                <th>ACTION</th>
                            </tr>
                        </thead>

                        <tbody>
                            {bins.length === 0 ? (
                                <tr>
                                    <td colSpan="4" className="sl-empty-cell">
                                        {loading
                                            ? 'Loading storage bins...'
                                            : 'No storage bins found in this zone.'}
                                    </td>
                                </tr>
                            ) : (
                                bins.map(bin => (
                                    <tr key={bin.binId}>
                                        <td>
                                            <strong>{bin.binCode}</strong>
                                        </td>
                                        <td>{bin.binType}</td>
                                        <td>
                                            <span
                                                className={`sl-status ${
                                                    bin.isActive ? 'active' : 'inactive'
                                                }`}
                                            >
                                                {bin.isActive ? 'Active' : 'Inactive'}
                                            </span>
                                        </td>
                                        <td>
                                            <button
                                                className="sl-edit-button"
                                                onClick={() => openEditBin(bin)}
                                            >
                                                Edit
                                            </button>
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>
            </section>

            {zoneModal && (
                <div className="sl-modal-backdrop">
                    <div className="sl-modal">
                        <div className="sl-modal-heading">
                            <div>
                                <h2>{editingZone ? 'Edit Zone' : 'Add Zone'}</h2>
                                <p>
                                    {editingZone
                                        ? 'Update the zone name.'
                                        : 'Create a zone under the selected warehouse.'}
                                </p>
                            </div>
                            <button
                                className="sl-close-button"
                                onClick={() => setZoneModal(false)}
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={saveZone}>
                            {!editingZone && (
                                <div className="sl-form-group">
                                    <label>Zone Code</label>
                                    <input
                                        value={zoneForm.zoneCode}
                                        onChange={event =>
                                            setZoneForm({
                                                ...zoneForm,
                                                zoneCode: event.target.value
                                            })
                                        }
                                        placeholder="e.g. RAW-01"
                                        required
                                    />
                                </div>
                            )}

                            {editingZone && (
                                <div className="sl-form-group">
                                    <label>Zone Code</label>
                                    <input value={zoneForm.zoneCode} disabled />
                                </div>
                            )}

                            <div className="sl-form-group">
                                <label>Zone Name</label>
                                <input
                                    value={zoneForm.zoneName}
                                    onChange={event =>
                                        setZoneForm({
                                            ...zoneForm,
                                            zoneName: event.target.value
                                        })
                                    }
                                    placeholder="e.g. Raw Materials Storage"
                                    required
                                />
                            </div>

                            <div className="sl-modal-actions">
                                <button
                                    type="button"
                                    className="sl-cancel-button"
                                    onClick={() => setZoneModal(false)}
                                >
                                    Cancel
                                </button>
                                <button type="submit" className="sl-primary-button">
                                    {editingZone ? 'Save Changes' : 'Create Zone'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {binModal && (
                <div className="sl-modal-backdrop">
                    <div className="sl-modal">
                        <div className="sl-modal-heading">
                            <div>
                                <h2>{editingBin ? 'Edit Storage Bin' : 'Add Storage Bin'}</h2>
                                <p>
                                    {editingBin
                                        ? 'Update the bin type or status.'
                                        : 'Create a bin under the selected zone.'}
                                </p>
                            </div>
                            <button
                                className="sl-close-button"
                                onClick={() => setBinModal(false)}
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={saveBin}>
                            <div className="sl-form-group">
                                <label>Bin Code</label>
                                <input
                                    value={binForm.binCode}
                                    onChange={event =>
                                        setBinForm({
                                            ...binForm,
                                            binCode: event.target.value
                                        })
                                    }
                                    placeholder="e.g. A-01-02"
                                    disabled={!!editingBin}
                                    required
                                />
                            </div>

                            <div className="sl-form-group">
                                <label>Bin Type</label>
                                <input
                                    value={binForm.binType}
                                    onChange={event =>
                                        setBinForm({
                                            ...binForm,
                                            binType: event.target.value
                                        })
                                    }
                                    placeholder="e.g. STORAGE, RACK, FLOOR"
                                    required
                                />
                            </div>

                            {editingBin && (
                                <div className="sl-form-group sl-checkbox-group">
                                    <label>
                                        <input
                                            type="checkbox"
                                            checked={binForm.isActive}
                                            onChange={event =>
                                                setBinForm({
                                                    ...binForm,
                                                    isActive: event.target.checked
                                                })
                                            }
                                        />
                                        Active
                                    </label>
                                </div>
                            )}

                            <div className="sl-modal-actions">
                                <button
                                    type="button"
                                    className="sl-cancel-button"
                                    onClick={() => setBinModal(false)}
                                >
                                    Cancel
                                </button>
                                <button type="submit" className="sl-primary-button">
                                    {editingBin ? 'Save Changes' : 'Create Bin'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    )
}