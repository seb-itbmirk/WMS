
import { useState } from 'react'
import api from '../../services/api'
import './productionorder.css'

function ProductionOrdersPage({
    productionOrders = [],
    loading,
    error,
    onRefresh
}) {
    const [showCreateForm, setShowCreateForm] = useState(false)
    const [saving, setSaving] = useState(false)
    const [formError, setFormError] = useState('')

    const [warehouses, setWarehouses] = useState([])
    const [boms, setBoms] = useState([])
    const [optionsLoading, setOptionsLoading] = useState(false)

    const [formData, setFormData] = useState({
        productionNumber: '',
        bomId: '',
        warehouseId: '',
        plannedQuantity: '',
        plannedStartDate: '',
        plannedEndDate: ''
    })

    const getStatusClass = (status) => {
        switch (status?.toUpperCase()) {
            case 'COMPLETED':
                return 'status-completed'
            case 'IN_PROGRESS':
                return 'status-progress'
            case 'RELEASED':
                return 'status-released'
            case 'CANCELLED':
                return 'status-cancelled'
            default:
                return 'status-draft'
        }
    }

    const formatDate = (dateValue) => {
        if (!dateValue) {
            return '-'
        }

        // Parse date-only values without introducing timezone shifts.
        const datePart = String(dateValue).split('T')[0]
        const [year, month, day] = datePart.split('-').map(Number)

        if (!year || !month || !day) {
            return '-'
        }

        return new Date(year, month - 1, day).toLocaleDateString()
    }

    const getErrorMessage = (error, fallbackMessage) => {
        const data = error.response?.data

        if (typeof data === 'string' && data.trim()) {
            return data
        }

        if (data?.message) {
            return data.message
        }

        if (data?.title) {
            return data.title
        }

        if (data?.errors) {
            const validationMessages = Object.values(data.errors)
                .flat()
                .filter(Boolean)

            if (validationMessages.length > 0) {
                return validationMessages.join(' ')
            }
        }

        return fallbackMessage
    }

    const loadFormOptions = async () => {
        try {
            setOptionsLoading(true)
            setFormError('')

            const [warehouseResponse, bomResponse] = await Promise.all([
                api.get('/warehouses'),
                api.get('/bill-of-materials')
            ])

            const warehouseData = Array.isArray(warehouseResponse.data)
                ? warehouseResponse.data
                : []

            const bomData = Array.isArray(bomResponse.data)
                ? bomResponse.data
                : []

            setWarehouses(
                warehouseData.filter((warehouse) => warehouse.isActive)
            )

            // The production API only accepts ACTIVE BOMs.
            setBoms(
                bomData.filter(
                    (bom) => bom.status?.toUpperCase() === 'ACTIVE'
                )
            )
        } catch (error) {
            console.error('Unable to load production order options:', error)

            setWarehouses([])
            setBoms([])

            setFormError(
                getErrorMessage(
                    error,
                    'Unable to load warehouses or BOMs. Please check the API connection.'
                )
            )
        } finally {
            setOptionsLoading(false)
        }
    }

    const openCreateForm = () => {
        setFormData({
            productionNumber: '',
            bomId: '',
            warehouseId: '',
            plannedQuantity: '',
            plannedStartDate: '',
            plannedEndDate: ''
        })

        setFormError('')
        setShowCreateForm(true)

        loadFormOptions()
    }

    const closeCreateForm = () => {
        if (saving) {
            return
        }

        setShowCreateForm(false)
        setFormError('')
    }

    const handleInputChange = (event) => {
        const { name, value } = event.target

        setFormData((previous) => ({
            ...previous,
            [name]: value
        }))
    }

    const handleCreateOrder = async (event) => {
        event.preventDefault()

        setFormError('')

        const productionNumber = formData.productionNumber.trim()
        const plannedQuantity = Number(formData.plannedQuantity)
        const bomId = Number(formData.bomId)
        const warehouseId = Number(formData.warehouseId)

        if (
            !productionNumber ||
            !formData.bomId ||
            !formData.warehouseId ||
            !formData.plannedQuantity ||
            !formData.plannedStartDate ||
            !formData.plannedEndDate
        ) {
            setFormError('Please complete all required fields.')
            return
        }

        if (!Number.isFinite(plannedQuantity) || plannedQuantity <= 0) {
            setFormError('Planned quantity must be greater than zero.')
            return
        }

        if (!Number.isInteger(bomId) || bomId <= 0) {
            setFormError('Please select a valid bill of materials.')
            return
        }

        if (!Number.isInteger(warehouseId) || warehouseId <= 0) {
            setFormError('Please select a valid warehouse.')
            return
        }

        if (formData.plannedEndDate < formData.plannedStartDate) {
            setFormError('End date cannot be earlier than start date.')
            return
        }

        try {
            setSaving(true)

            // Property names match CreateProductionOrderDto.
            const payload = {
                productionNumber,
                bomId,
                warehouseId,
                plannedQuantity,
                plannedStartDate: formData.plannedStartDate,
                plannedEndDate: formData.plannedEndDate
            }

            await api.post('/production-orders', payload)

            setShowCreateForm(false)

            if (onRefresh) {
                try {
                    await onRefresh()
                } catch (refreshError) {
                    console.error(
                        'Production order was created, but the list could not be refreshed:',
                        refreshError
                    )
                }
            }
        } catch (error) {
            console.error('Unable to create production order:', error)

            setFormError(
                getErrorMessage(
                    error,
                    'Unable to create production order. Please check the submitted information.'
                )
            )
        } finally {
            setSaving(false)
        }
    }

    const orders = Array.isArray(productionOrders)
        ? productionOrders
        : []

    return (
        <div className="page-content production-orders-page">

            {/* Page Header */}
            <div className="page-header">
                <div>
                    <h1>Production Orders</h1>
                    <p>Monitor and manage manufacturing activities.</p>
                </div>

                <button
                    type="button"
                    className="primary-button"
                    onClick={openCreateForm}
                >
                    + Create Production Order
                </button>
            </div>

            {/* Loading and Error Messages */}
            {loading && <p>Loading production orders...</p>}

            {error && <p className="error-message">{error}</p>}

            {/* Production Order Table */}
            {!loading && !error && (
                <div className="panel">
                    <div className="panel-header">
                        <h2>Production Order List</h2>

                        <span className="record-count">
                            {orders.length} record(s)
                        </span>
                    </div>

                    <div className="table-wrapper">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Production No.</th>
                                    <th>Finished Product</th>
                                    <th>Warehouse</th>
                                    <th>Planned Quantity</th>
                                    <th>Start Date</th>
                                    <th>End Date</th>
                                    <th>Status</th>
                                </tr>
                            </thead>

                            <tbody>
                                {orders.length === 0 ? (
                                    <tr>
                                        <td colSpan="7">
                                            No production orders found.
                                        </td>
                                    </tr>
                                ) : (
                                    orders.map((order) => (
                                        <tr key={order.productionOrderId}>
                                            <td>
                                                {order.productionNumber}
                                            </td>

                                            <td>
                                                {order.finishedItemCode} - {order.finishedItemName}
                                            </td>

                                            <td>
                                                {order.warehouseName}
                                            </td>

                                            <td>
                                                {order.plannedQuantity} {order.uomCode}
                                            </td>

                                            <td>
                                                {formatDate(order.plannedStartDate)}
                                            </td>

                                            <td>
                                                {formatDate(order.plannedEndDate)}
                                            </td>

                                            <td>
                                                <span
                                                    className={`status-badge ${getStatusClass(order.productionStatus)}`}
                                                >
                                                    {order.productionStatus
                                                        ?.replaceAll('_', ' ') || 'DRAFT'}
                                                </span>
                                            </td>
                                        </tr>
                                    ))
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}

            {/* Create Production Order Modal */}
            {showCreateForm && (
                <div className="modal-overlay">
                    <div className="modal production-modal">

                        <div className="modal-header">
                            <div>
                                <h2>Create Production Order</h2>
                                <p>Enter the manufacturing order details.</p>
                            </div>

                            <button
                                type="button"
                                onClick={closeCreateForm}
                                disabled={saving}
                                aria-label="Close"
                            >
                                ✕
                            </button>
                        </div>

                        {optionsLoading ? (
                            <p>Loading warehouses and BOMs...</p>
                        ) : (
                            <form onSubmit={handleCreateOrder}>

                                {/* Production Number */}
                                <div className="form-group">
                                    <label htmlFor="productionNumber">
                                        Production Number *
                                    </label>

                                    <input
                                        id="productionNumber"
                                        type="text"
                                        name="productionNumber"
                                        value={formData.productionNumber}
                                        onChange={handleInputChange}
                                        placeholder="e.g. PROD-2026-0002"
                                        maxLength={50}
                                        required
                                        disabled={saving}
                                    />
                                </div>

                                {/* Bill of Materials */}
                                <div className="form-group">
                                    <label htmlFor="bomId">
                                        Bill of Materials *
                                    </label>

                                    <select
                                        id="bomId"
                                        name="bomId"
                                        value={formData.bomId}
                                        onChange={handleInputChange}
                                        required
                                        disabled={saving || boms.length === 0}
                                    >
                                        <option value="">
                                            Select BOM
                                        </option>

                                        {boms.map((bom) => (
                                            <option
                                                key={bom.bomId}
                                                value={bom.bomId}
                                            >
                                                {bom.bomCode} (Version {bom.versionNumber}) - {bom.finishedItemName}
                                            </option>
                                        ))}
                                    </select>

                                    {boms.length === 0 && (
                                        <small>
                                            No active BOMs are available.
                                        </small>
                                    )}
                                </div>

                                {/* Warehouse */}
                                <div className="form-group">
                                    <label htmlFor="warehouseId">
                                        Warehouse *
                                    </label>

                                    <select
                                        id="warehouseId"
                                        name="warehouseId"
                                        value={formData.warehouseId}
                                        onChange={handleInputChange}
                                        required
                                        disabled={saving || warehouses.length === 0}
                                    >
                                        <option value="">
                                            Select Warehouse
                                        </option>

                                        {warehouses.map((warehouse) => (
                                            <option
                                                key={warehouse.warehouseId}
                                                value={warehouse.warehouseId}
                                            >
                                                {warehouse.warehouseCode} - {warehouse.warehouseName}
                                            </option>
                                        ))}
                                    </select>

                                    {warehouses.length === 0 && (
                                        <small>
                                            No active warehouses are available.
                                        </small>
                                    )}
                                </div>

                                {/* Planned Quantity */}
                                <div className="form-group">
                                    <label htmlFor="plannedQuantity">
                                        Planned Quantity *
                                    </label>

                                    <input
                                        id="plannedQuantity"
                                        type="number"
                                        name="plannedQuantity"
                                        value={formData.plannedQuantity}
                                        onChange={handleInputChange}
                                        min="0.000001"
                                        step="any"
                                        placeholder="Enter planned quantity"
                                        required
                                        disabled={saving}
                                    />
                                </div>

                                {/* Planned Dates */}
                                <div className="form-row">
                                    <div className="form-group">
                                        <label htmlFor="plannedStartDate">
                                            Planned Start Date *
                                        </label>

                                        <input
                                            id="plannedStartDate"
                                            type="date"
                                            name="plannedStartDate"
                                            value={formData.plannedStartDate}
                                            onChange={handleInputChange}
                                            required
                                            disabled={saving}
                                        />
                                    </div>

                                    <div className="form-group">
                                        <label htmlFor="plannedEndDate">
                                            Planned End Date *
                                        </label>

                                        <input
                                            id="plannedEndDate"
                                            type="date"
                                            name="plannedEndDate"
                                            value={formData.plannedEndDate}
                                            onChange={handleInputChange}
                                            min={formData.plannedStartDate || undefined}
                                            required
                                            disabled={saving}
                                        />
                                    </div>
                                </div>

                                {/* Form Error */}
                                {formError && (
                                    <p className="error-message">
                                        {formError}
                                    </p>
                                )}

                                {/* Form Actions */}
                                <div className="modal-actions">
                                    <button
                                        type="button"
                                        className="secondary-button"
                                        onClick={closeCreateForm}
                                        disabled={saving}
                                    >
                                        Cancel
                                    </button>

                                    <button
                                        type="submit"
                                        className="primary-button"
                                        disabled={
                                            saving ||
                                            boms.length === 0 ||
                                            warehouses.length === 0
                                        }
                                    >
                                        {saving
                                            ? 'Creating...'
                                            : 'Create Production Order'}
                                    </button>
                                </div>

                            </form>
                        )}
                    </div>
                </div>
            )}

        </div>
    )
}

export default ProductionOrdersPage
