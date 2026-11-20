
import { useEffect, useState } from 'react'
import api from '../services/api'

function InventoryPage() {
    const [inventory, setInventory] = useState([])
    const [warehouses, setWarehouses] = useState([])
    const [selectedWarehouse, setSelectedWarehouse] = useState('')
    const [searchTerm, setSearchTerm] = useState('')
    const [balanceId, setBalanceId] = useState('')
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState('')

    useEffect(() => {
        loadWarehouses()
        loadInventory()
    }, [])

    const loadWarehouses = async () => {
        try {
            const response = await api.get('/warehouses')
            setWarehouses(response.data)
        } catch (error) {
            console.error('Unable to load warehouses:', error)
        }
    }

    // SHOW ALL INVENTORY
    const loadInventory = async () => {
        try {
            setLoading(true)
            setError('')

            const response = await api.get('/inventory-balances')
            setInventory(response.data)
            setBalanceId('')
        } catch (error) {
            console.error('Unable to load inventory:', error)
            setError('Unable to load inventory balances.')
            setInventory([])
        } finally {
            setLoading(false)
        }
    }

    // SHOW INVENTORY BY BALANCE ID
    const loadInventoryById = async () => {
        if (!balanceId.trim()) {
            setError('Please enter an Inventory Balance ID.')
            return
        }

        try {
            setLoading(true)
            setError('')

            const response = await api.get(
                `/inventory-balances/${balanceId.trim()}`
            )

            // Replace the table data with only the selected record
            setInventory([response.data])

        } catch (error) {
            console.error('Unable to load inventory balance:', error)

            setInventory([])

            if (error.response?.status === 404) {
                setError(`Inventory Balance ID ${balanceId} was not found.`)
            } else {
                setError('Unable to retrieve inventory balance.')
            }
        } finally {
            setLoading(false)
        }
    }

    const filteredInventory = inventory.filter((item) => {
        const search = searchTerm.toLowerCase()

        const matchesSearch =
            item.itemCode?.toLowerCase().includes(search) ||
            item.itemName?.toLowerCase().includes(search) ||
            item.batchCode?.toLowerCase().includes(search) ||
            item.supplierBatchCode?.toLowerCase().includes(search) ||
            item.binCode?.toLowerCase().includes(search)

        const matchesWarehouse =
            selectedWarehouse === '' ||
            String(item.warehouseId) === selectedWarehouse

        return matchesSearch && matchesWarehouse
    })

    const formatQuantity = (quantity) =>
        Number(quantity || 0).toLocaleString(undefined, {
            maximumFractionDigits: 3
        })

    return (
        <div className="page-content">
            <div className="page-header">
                <div>
                    <h1>Inventory Overview</h1>
                    <p>Monitor stock quantities, batches, and warehouse locations.</p>
                </div>

                <button
                    className="primary-button"
                    onClick={loadInventory}
                >
                    Show All / Refresh
                </button>
            </div>

            <div className="inventory-filters">
                <input
                    type="text"
                    placeholder="Search item, batch, or bin..."
                    value={searchTerm}
                    onChange={(event) => setSearchTerm(event.target.value)}
                />

                <select
                    value={selectedWarehouse}
                    onChange={(event) => setSelectedWarehouse(event.target.value)}
                >
                    <option value="">All Warehouses</option>

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

            {/* SEARCH BY INVENTORY BALANCE ID */}
            <div className="inventory-filters">
                <input
                    type="number"
                    min="1"
                    placeholder="Enter Inventory Balance ID..."
                    value={balanceId}
                    onChange={(event) => setBalanceId(event.target.value)}
                />

                <button
                    className="primary-button"
                    onClick={loadInventoryById}
                    disabled={loading}
                >
                    Show by ID
                </button>

                <button
                    className="secondary-button"
                    onClick={loadInventory}
                    disabled={loading}
                >
                    Show All
                </button>
            </div>

            {loading && <p>Loading inventory...</p>}

            {error && (
                <div className="error-message">
                    {error}
                </div>
            )}

            {!loading && !error && (
                <div className="panel">
                    <div className="panel-header">
                        <h2>Inventory Balances</h2>
                        <span>{filteredInventory.length} records</span>
                    </div>

                    <div className="table-wrapper">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Balance ID</th>
                                    <th>Item Code</th>
                                    <th>Item Description</th>
                                    <th>Warehouse</th>
                                    <th>Bin Location</th>
                                    <th>Batch Number</th>
                                    <th>Supplier Batch</th>
                                    <th>Expiry Date</th>
                                    <th>On Hand</th>
                                    <th>Reserved</th>
                                    <th>Available</th>
                                    <th>Stock Status</th>
                                </tr>
                            </thead>

                            <tbody>
                                {filteredInventory.length === 0 ? (
                                    <tr>
                                        <td colSpan="12">
                                            No inventory records found.
                                        </td>
                                    </tr>
                                ) : (
                                    filteredInventory.map((item) => (
                                        <tr key={item.balanceId}>
                                            <td>{item.balanceId}</td>
                                            <td>{item.itemCode}</td>
                                            <td>{item.itemName}</td>
                                            <td>{item.warehouseName || '-'}</td>
                                            <td>{item.binCode || '-'}</td>
                                            <td>{item.batchCode || '-'}</td>
                                            <td>{item.supplierBatchCode || '-'}</td>
                                            <td>
                                                {item.expiryDate
                                                    ? new Date(item.expiryDate).toLocaleDateString()
                                                    : '-'}
                                            </td>
                                            <td>{formatQuantity(item.quantityOnHand)}</td>
                                            <td>{formatQuantity(item.quantityReserved)}</td>
                                            <td>{formatQuantity(item.availableQuantity)}</td>
                                            <td>{item.stockStatus}</td>
                                        </tr>
                                    ))
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}
        </div>
    )
}

export default InventoryPage
