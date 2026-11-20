
import { useState } from 'react'
import api from '../services/api'

function ProductionOrdersPage({
    productionOrders,
    loading,
    error,
    onRefresh
}) {
    const [showCreateForm, setShowCreateForm] = useState(false)
    const [saving, setSaving] = useState(false)
    const [formError, setFormError] = useState('')
    const [formSuccess, setFormSuccess] = useState('')

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

    const loadFormOptions = async () => {
        try {
            setOptionsLoading(true)
            setFormError('')

            const [warehouseResponse, bomResponse] = await Promise.all([
                api.get('/warehouses'),
                api.get('/bill-of-materials')
            ])

            setWarehouses(warehouseResponse.data)
            setBoms(bomResponse.data)
        } catch (error) {
            console.error('Unable to load form options:', error)
            setFormError(
                'Unable to load warehouses or BOMs. Check the API endpoints.'
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
        setFormSuccess('')
        setShowCreateForm(true)

        loadFormOptions()
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
        setFormSuccess('')

        if (
            !formData.productionNumber.trim() ||
            !formData.bomId ||
            !formData.warehouseId ||
            !formData.plannedQuantity ||
            !formData.plannedStartDate ||
            !formData.plannedEndDate
        ) {
            setFormError('Please complete all required fields.')
            return
        }

        if (Number(formData.plannedQuantity) <= 0) {
            setFormError('Planned quantity must be greater than zero.')
            return
        }

        if (formData.plannedEndDate < formData.plannedStartDate) {
            setFormError('End date cannot be earlier than start date.')
            return
        }

        try {
            setSaving(true)

            const payload = {
                productionNumber: formData.productionNumber.trim(),
                bomId: Number(formData.bomId),
                warehouseId: Number(formData.warehouseId),
                plannedQuantity: Number(formData.plannedQuantity),
                plannedStartDate: formData.plannedStartDate,
                plannedEndDate: formData.plannedEndDate
            }

            await api.post('/production-orders', payload)

            setFormSuccess('Production order created successfully.')

            if (onRefresh) {
                await onRefresh()
            }

            setShowCreateForm(false)
        } catch (error) {
            console.error('Unable to create production order:', error)

            const responseMessage =
                error.response?.data?.message ||
                error.response?.data?.title ||
                'Unable to create production order. Please check the submitted information.'

            setFormError(responseMessage)
        } finally {
            setSaving(false)
        }
    }

    return (
        <div className="page-content production-orders-page">

            {/* Page Header */}
            <div className="page-header">
                <div>
                    <h1>Production Orders</h1>
                    <p>Monitor and manage manufacturing activities.</p>
                </div>

                <button
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
                            {productionOrders.length} record(s)
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
                                {productionOrders.length === 0 ? (
                                    <tr>
                                        <td colSpan="7">
                                            No production orders found.
                                        </td>
                                    </tr>
                                ) : (
                                    productionOrders.map((order) => (
                                        <tr key={order.productionOrderId}>
                                            <td>{order.productionNumber}</td>

                                            <td>
                                                {order.finishedItemCode} - {order.finishedItemName}
                                            </td>

                                            <td>{order.warehouseName}</td>

                                            <td>
                                                {order.plannedQuantity} {order.uomCode}
                                            </td>

                                            <td>
                                                {order.plannedStartDate
                                                    ? new Date(order.plannedStartDate).toLocaleDateString()
                                                    : '-'}
                                            </td>

                                            <td>
                                                {order.plannedEndDate
                                                    ? new Date(order.plannedEndDate).toLocaleDateString()
                                                    : '-'}
                                            </td>

                                            <td>
                                                <span className={`status-badge ${getStatusClass(order.status)}`}>
                                                    {order.status?.replace('_', ' ')}
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
                                onClick={() => setShowCreateForm(false)}
                                disabled={saving}
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
                                    <label>Production Number *</label>
                                    <input
                                        type="text"
                                        name="productionNumber"
                                        value={formData.productionNumber}
                                        onChange={handleInputChange}
                                        placeholder="e.g. PROD-2026-0002"
                                        required
                                    />
                                </div>

                                {/* Bill of Materials */}
                                <div className="form-group">
                                    <label>Bill of Materials *</label>
                                    <select
                                        name="bomId"
                                        value={formData.bomId}
                                        onChange={handleInputChange}
                                        required
                                    >
                                        <option value="">Select BOM</option>

                                        {boms.map((bom) => (
                                            <option
                                                key={bom.bomId}
                                                value={bom.bomId}
                                            >
                                                {bom.bomNumber} - {bom.finishedItemName}
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                {/* Warehouse */}
                                <div className="form-group">
                                    <label>Warehouse *</label>
                                    <select
                                        name="warehouseId"
                                        value={formData.warehouseId}
                                        onChange={handleInputChange}
                                        required
                                    >
                                        <option value="">Select Warehouse</option>

                                        {warehouses.map((warehouse) => (
                                            <option
                                                key={warehouse.warehouseId}
                                                value={warehouse.warehouseId}
                                            >
                                                {warehouse.warehouseCode} - {warehouse.warehouseName}
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                {/* Planned Quantity */}
                                <div className="form-group">
                                    <label>Planned Quantity *</label>
                                    <input
                                        type="number"
                                        name="plannedQuantity"
                                        value={formData.plannedQuantity}
                                        onChange={handleInputChange}
                                        min="1"
                                        step="any"
                                        placeholder="Enter planned quantity"
                                        required
                                    />
                                </div>

                                {/* Planned Dates */}
                                <div className="form-row">
                                    <div className="form-group">
                                        <label>Planned Start Date *</label>
                                        <input
                                            type="date"
                                            name="plannedStartDate"
                                            value={formData.plannedStartDate}
                                            onChange={handleInputChange}
                                            required
                                        />
                                    </div>

                                    <div className="form-group">
                                        <label>Planned End Date *</label>
                                        <input
                                            type="date"
                                            name="plannedEndDate"
                                            value={formData.plannedEndDate}
                                            onChange={handleInputChange}
                                            min={formData.plannedStartDate || undefined}
                                            required
                                        />
                                    </div>
                                </div>

                                {/* Form Error */}
                                {formError && (
                                    <p className="error-message">{formError}</p>
                                )}

                                {/* Form Actions */}
                                <div className="modal-actions">
                                    <button
                                        type="button"
                                        className="secondary-button"
                                        onClick={() => setShowCreateForm(false)}
                                        disabled={saving}
                                    >
                                        Cancel
                                    </button>

                                    <button
                                        type="submit"
                                        className="primary-button"
                                        disabled={saving}
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
