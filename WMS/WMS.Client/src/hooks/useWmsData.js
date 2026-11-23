
import { useCallback, useEffect, useState } from 'react'
import api from '../services/api'

function useWmsData() {
    const [warehouses, setWarehouses] = useState([])
    const [categories, setCategories] = useState([])

    const [loading, setLoading] = useState(true)
    const [apiError, setApiError] = useState('')

    const [productionDashboard, setProductionDashboard] = useState(null)
    const [productionLoading, setProductionLoading] = useState(true)
    const [productionError, setProductionError] = useState('')

    const [productionOrders, setProductionOrders] = useState([])
    const [productionOrdersLoading, setProductionOrdersLoading] = useState(false)
    const [productionOrdersError, setProductionOrdersError] = useState('')

    const loadData = useCallback(async () => {
        setLoading(true)
        setApiError('')

        try {
            const [warehouseResponse, categoryResponse] = await Promise.all([
                api.get('/warehouses'),
                api.get('/item-categories')
            ])

            setWarehouses(warehouseResponse.data)
            setCategories(categoryResponse.data)
        } catch (error) {
            console.error('Error loading WMS data:', error)
            setApiError(
                error.response?.data?.message ||
                error.message ||
                'Failed to load warehouse and category data.'
            )
        } finally {
            setLoading(false)
        }
    }, [])

    const loadProductionDashboard = useCallback(async () => {
        setProductionLoading(true)
        setProductionError('')

        try {
            const response = await api.get('/production-orders/dashboard')
            setProductionDashboard(response.data)
        } catch (error) {
            console.error('Error loading production dashboard:', error)
            setProductionError(
                error.response?.data?.message ||
                error.message ||
                'Failed to load production dashboard.'
            )
        } finally {
            setProductionLoading(false)
        }
    }, [])

    const loadProductionOrders = useCallback(async () => {
        setProductionOrdersLoading(true)
        setProductionOrdersError('')

        try {
            const response = await api.get('/production-orders')
            setProductionOrders(response.data)
        } catch (error) {
            console.error('Error loading production orders:', error)
            setProductionOrdersError(
                error.response?.data?.message ||
                error.message ||
                'Failed to load production orders.'
            )
        } finally {
            setProductionOrdersLoading(false)
        }
    }, [])

    useEffect(() => {
        loadData()
        loadProductionDashboard()
    }, [loadData, loadProductionDashboard])

    return {
        warehouses,
        setWarehouses,
        categories,
        setCategories,

        loading,
        apiError,
        setApiError,
        loadData,

        productionDashboard,
        productionLoading,
        productionError,
        loadProductionDashboard,

        productionOrders,
        productionOrdersLoading,
        productionOrdersError,
        loadProductionOrders
    }
}

export default useWmsData
