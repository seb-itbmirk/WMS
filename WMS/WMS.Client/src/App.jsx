
import { useEffect, useState } from 'react'
import api from './services/api'
import './App.css'
import useWmsData from './hooks/useWmsData'
import ManagementModal from './components/modals/ManagementModal'

import Dashboard from './components/dashboard/Dashboard'
import MainLayout from './components/layout/MainLayout'
import navigation from './components/layout/navigation'
import { canAccessPage } from './utils/accessControl'

import ManagementPage from './pages/Warehouses/ManagementPage'
import ProductionOrdersPage from './pages/ProductionOrders/ProductionOrders'
import StockTransfersPage from './pages/StockTransfers/StockTransfers'
import StorageLocationsPage from './pages/StorageLocations/StorageLocations'
import ItemsPage from './pages/Items/Items'
import InventoryOverviewPage from './pages/InventoryOverview/InventoryOverview'
import BillOfMaterialsPage from './pages/BillOfMaterials/BillOfMaterials'
import PurchaseOrdersPage from './pages/PurchaseOrders/PurchaseOrders'
import QualityInspections from './pages/QualityInspections/QualityInspections'
import StockAdjustmentsPage from './pages/StockAdjustments/StockAdjustments'
import GoodsReceiptsPage from './pages/GoodsReceipts/GoodsReceipts'
import PutawayTasksPage from './pages/PutawayTasks/PutawayTasks'
import StockCountsPage from './pages/StockCounts/StockCounts'
import CustomersPage from './pages/Customers/Customers'
import ReportsPage from './pages/Reports/Reports'
import ReplenishmentPage from './pages/Replenishment/Replenishment'
import SuppliersPage from './pages/Suppliers/Suppliers'
import UnitsOfMeasurePage from './pages/UnitsOfMeasure/UnitsOfMeasure'
import QualityParametersPage from './pages/QualityParameters/QualityParameters'
import UsersPage from './pages/Users/UsersPage'
import UsersListPage from './pages/Users/UsersListPage'
import ProductionReportsPage from './pages/Production/ProductionReportsPage'

const availablePages = [
    'Dashboard',
    'Warehouses',
    'Item Categories',
    'Production Orders',
    'Production Reports',
    'Stock Overview',
    'Stock Transfers',
    'Storage Locations',
    'Items & Materials',
    'Bill of Materials',
    'Purchase Orders',
    'Quality Inspections',
    'Stock Adjustments',
    'Goods Receipts',
    'Putaway Tasks',
    'Stock Counts',
    'Customers',
    'Reports & Analytics',
    'Replenishment',
    'Supplier Management',
    'Units of Measure',
    'Quality Parameters',
    'User Management',
    'User List'
]

function App({ currentUser, onLogout }) {
    const [page, setPage] = useState('Dashboard')

    const {
        warehouses,
        categories,
        loading,
        apiError,
        loadData,
        productionDashboard,
        productionLoading,
        productionError,
        productionOrders,
        productionOrdersLoading: ordersLoading,
        productionOrdersError: ordersError,
        loadProductionOrders
    } = useWmsData()

    const [showForm, setShowForm] = useState(false)
    const [formType, setFormType] = useState('')
    const [editingId, setEditingId] = useState(null)
    const [formData, setFormData] = useState({})
    const [saving, setSaving] = useState(false)
    const [message, setMessage] = useState('')

    useEffect(() => {
        loadData()
    }, [loadData])

    useEffect(() => {
        if (page === 'Production Orders') {
            loadProductionOrders()
        }
    }, [page, loadProductionOrders])

    const openForm = (type, record = null) => {
        setFormType(type)

        setEditingId(
            record
                ? type === 'warehouse'
                    ? record.warehouseId
                    : record.categoryId
                : null
        )

        if (type === 'warehouse') {
            setFormData(record ? {
                warehouseCode: record.warehouseCode,
                warehouseName: record.warehouseName,
                city: record.city || '',
                address: record.address || ''
            } : {
                warehouseCode: '',
                warehouseName: '',
                city: '',
                address: ''
            })
        } else {
            setFormData(record ? {
                categoryCode: record.categoryCode,
                categoryName: record.categoryName,
                description: record.description || ''
            } : {
                categoryCode: '',
                categoryName: '',
                description: ''
            })
        }

        setMessage('')
        setShowForm(true)
    }

    const handleInputChange = event => {
        const { name, value } = event.target

        setFormData(previous => ({
            ...previous,
            [name]: value
        }))
    }

    const handleSave = async event => {
        event.preventDefault()

        try {
            setSaving(true)
            setMessage('')

            const endpoint = formType === 'warehouse'
                ? '/warehouses'
                : '/item-categories'

            if (editingId) {
                await api.put(`${endpoint}/${editingId}`, formData)
            } else {
                await api.post(endpoint, formData)
            }

            setShowForm(false)
            await loadData()
        } catch (error) {
            console.error('Unable to save record:', error)

            setMessage(
                error.response?.data?.message ||
                'Unable to save. Check the required fields and API response.'
            )
        } finally {
            setSaving(false)
        }
    }

    const handleDeactivate = async (type, id) => {
        const confirmed = window.confirm(
            'Are you sure you want to deactivate this record?'
        )

        if (!confirmed) return

        try {
            const endpoint = type === 'warehouse'
                ? '/warehouses'
                : '/item-categories'

            await api.patch(`${endpoint}/${id}/deactivate`)
            await loadData()
        } catch (error) {
            console.error('Unable to deactivate record:', error)

            alert(
                error.response?.data?.message ||
                'Unable to deactivate this record.'
            )
        }
    }

    const activeWarehouses = warehouses.filter(w => w.isActive).length
    const activeCategories = categories.filter(c => c.isActive).length

    // Page navigation and access control.
    const handlePageChange = newPage => {
        if (!availablePages.includes(newPage)) {
            console.warn('Unknown page:', newPage)
            return
        }

        // Dashboard is always accessible.
        if (newPage === 'Dashboard') {
            setPage(newPage)
            setMessage('')
            return
        }

        // IMPORTANT:
        // canAccessPage expects (page, user), not (user, permission).
        if (!canAccessPage(newPage, currentUser)) {
            console.warn('Access denied to page:', newPage)
            setPage('Dashboard')
            setMessage('')
            return
        }

        setPage(newPage)
        setMessage('')
    }

    // Validate the selected page on render as well.
    const visiblePage =
        page === 'Dashboard' ||
        canAccessPage(page, currentUser)
            ? page
            : 'Dashboard'

    return (
        <>
            <MainLayout
                page={visiblePage}
                setPage={handlePageChange}
                apiError={apiError}
                loading={loading}
                currentUser={currentUser}
                onLogout={onLogout}
            >
                {visiblePage === 'Dashboard' && (
                    <Dashboard
                        currentUser={currentUser}
                        warehouses={warehouses}
                        activeWarehouses={activeWarehouses}
                        activeCategories={activeCategories}
                        loading={loading}
                        apiError={apiError}
                        productionDashboard={productionDashboard}
                        productionLoading={productionLoading}
                        productionError={productionError}
                        setPage={handlePageChange}
                    />
                )}

                {visiblePage === 'Warehouses' && (
                    <ManagementPage
                        title="Warehouse management"
                        subtitle="Manage your registered warehouse facilities."
                        records={warehouses}
                        type="warehouse"
                        loading={loading}
                        onAdd={() => openForm('warehouse')}
                        onEdit={record => openForm('warehouse', record)}
                        onDeactivate={id => handleDeactivate('warehouse', id)}
                    />
                )}

                {visiblePage === 'Item Categories' && (
                    <ManagementPage
                        title="Item categories"
                        subtitle="Organize materials and products into categories."
                        records={categories}
                        type="category"
                        loading={loading}
                        onAdd={() => openForm('category')}
                        onEdit={record => openForm('category', record)}
                        onDeactivate={id => handleDeactivate('category', id)}
                    />
                )}

                {visiblePage === 'Production Orders' && (
                    <ProductionOrdersPage
                        productionOrders={productionOrders}
                        loading={ordersLoading}
                        error={ordersError}
                        onRefresh={loadProductionOrders}
                    />
                )}

                {visiblePage === 'Stock Transfers' && (
                    <StockTransfersPage />
                )}

                {visiblePage === 'Storage Locations' && (
                    <StorageLocationsPage />
                )}

                {visiblePage === 'Items & Materials' && (
                    <ItemsPage />
                )}

                {visiblePage === 'Stock Overview' && (
                    <InventoryOverviewPage />
                )}

                {visiblePage === 'Stock Adjustments' && (
                    <StockAdjustmentsPage />
                )}

                {visiblePage === 'Goods Receipts' && (
                    <GoodsReceiptsPage />
                )}

                {visiblePage === 'Putaway Tasks' && (
                    <PutawayTasksPage />
                )}

                {visiblePage === 'Stock Counts' && (
                    <StockCountsPage />
                )}

                {visiblePage === 'Replenishment' && (
                    <ReplenishmentPage />
                )}

                {visiblePage === 'Bill of Materials' && (
                    <BillOfMaterialsPage />
                )}

                {visiblePage === 'Purchase Orders' && (
                    <PurchaseOrdersPage />
                )}

                {visiblePage === 'Quality Inspections' && (
                    <QualityInspections />
                )}

                {visiblePage === 'Customers' && (
                    <CustomersPage />
                )}

                {visiblePage === 'Supplier Management' && (
                    <SuppliersPage />
                )}

                {visiblePage === 'Units of Measure' && (
                    <UnitsOfMeasurePage />
                )}

                {visiblePage === 'Quality Parameters' && (
                    <QualityParametersPage />
                )}

                {visiblePage === 'Reports & Analytics' && (
                    <ReportsPage />
                )}

                {visiblePage === 'User Management' && (
                    <UsersPage />
                )}

                {visiblePage === 'User List' && (
                    <UsersListPage />
                )}

                {visiblePage === 'Production Reports' && (
                    <ProductionReportsPage />
                )}

                {!availablePages.includes(visiblePage) && (
                    <section className="intro">
                        <div>
                            <div className="eyebrow">
                                <span></span>
                                MODULE NOT YET AVAILABLE
                            </div>

                            <h1>{visiblePage}</h1>

                            <p>
                                This module will be connected as we build
                                its backend functionality.
                            </p>

                            <button
                                className="primary-button"
                                onClick={() => handlePageChange('Dashboard')}
                            >
                                Return to dashboard
                            </button>
                        </div>
                    </section>
                )}
            </MainLayout>

            <ManagementModal
                showForm={showForm}
                formType={formType}
                editingId={editingId}
                formData={formData}
                saving={saving}
                message={message}
                onClose={() => setShowForm(false)}
                onInputChange={handleInputChange}
                onSave={handleSave}
            />
        </>
    )
}

export default App
