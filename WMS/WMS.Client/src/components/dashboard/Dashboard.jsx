function Dashboard({
    warehouses,
    activeWarehouses,
    activeCategories,
    loading,
    apiError,
    productionDashboard,
    productionLoading,
    productionError,
    setPage
}) {
    return (
        <>
            <section className="intro">
                <div>
                    <div className="eyebrow">
                        <span></span>
                        OPERATIONS OVERVIEW
                    </div>

                    <h1>Warehouse<br />control center.</h1>

                    <p>
                        Monitor your warehouses, inventory,
                        and manufacturing operations in one place.
                    </p>
                </div>

                <div className="date-panel">
                    <span>TODAY'S DATE</span>
                    <strong>
                        {new Date().toLocaleDateString('en-PH', {
                            day: '2-digit',
                            month: 'long',
                            year: 'numeric'
                        })}
                    </strong>
                </div>
            </section>

            <section className="section-block">
                <div className="section-title">
                    <div>
                        <span className="eyebrow">YOUR FACILITY</span>
                        <h2>Operational summary</h2>
                    </div>

                    <span className="live-label">
                        <i></i> LIVE DATA
                    </span>
                </div>

                <div className="metrics-grid">
                    <div className="metric">
                        <span className="metric-label">ACTIVE WAREHOUSES</span>

                        <div className="metric-value">
                            {loading ? '—' : activeWarehouses}
                            <span className="metric-icon">▤</span>
                        </div>

                        <p>Registered facilities</p>
                    </div>

                    <div className="metric">
                        <span className="metric-label">ITEM CATEGORIES</span>

                        <div className="metric-value">
                            {loading ? '—' : activeCategories}
                            <span className="metric-icon">◫</span>
                        </div>

                        <p>Active material groups</p>
                    </div>

                    <div className="metric">
                        <span className="metric-label">INVENTORY LOTS</span>

                        <div className="metric-value">—</div>

                        <p>Inventory module pending</p>
                    </div>

                    <div
                        className="metric clickable-metric"
                        onClick={() => setPage('Production Orders')}
                    >
                        <span className="metric-label">PRODUCTION ORDERS</span>

                        <div className="metric-value">
                            {productionLoading
                                ? '—'
                                : productionError
                                    ? '!'
                                    : productionDashboard?.totalOrders ?? 0}

                            <span className="metric-icon">⚙</span>
                        </div>

                        <p>
                            {productionError
                                ? 'Unable to retrieve production data'
                                : 'Total registered production orders'}
                        </p>
                    </div>
                </div>
            </section>

            <section className="section-block">
                <div className="section-title">
                    <div>
                        <span className="eyebrow">MANUFACTURING</span>
                        <h2>Production status overview</h2>
                    </div>

                    <button
                        className="text-button"
                        onClick={() => setPage('Production Orders')}
                    >
                        View production orders →
                    </button>
                </div>

                {productionLoading ? (
                    <div className="panel empty-content">
                        Loading production summary...
                    </div>
                ) : productionError ? (
                    <div className="panel empty-content">
                        Unable to retrieve production summary.
                    </div>
                ) : (
                    <div className="production-status-grid">
                        <div className="production-status-card">
                            <span className="status-dot status-draft"></span>
                            <span>Draft</span>
                            <strong>
                                {productionDashboard?.draftOrders ?? 0}
                            </strong>
                        </div>

                        <div className="production-status-card">
                            <span className="status-dot status-released"></span>
                            <span>Released</span>
                            <strong>
                                {productionDashboard?.releasedOrders ?? 0}
                            </strong>
                        </div>

                        <div className="production-status-card">
                            <span className="status-dot status-progress"></span>
                            <span>In Progress</span>
                            <strong>
                                {productionDashboard?.inProgressOrders ?? 0}
                            </strong>
                        </div>

                        <div className="production-status-card">
                            <span className="status-dot status-completed"></span>
                            <span>Completed</span>
                            <strong>
                                {productionDashboard?.completedOrders ?? 0}
                            </strong>
                        </div>

                        <div className="production-status-card">
                            <span className="status-dot status-cancelled"></span>
                            <span>Cancelled</span>
                            <strong>
                                {productionDashboard?.cancelledOrders ?? 0}
                            </strong>
                        </div>
                    </div>
                )}
            </section>

            <section className="lower-section">
                <div className="panel facility-panel">
                    <div className="panel-heading">
                        <div>
                            <span className="eyebrow">FACILITY MANAGEMENT</span>
                            <h3>Warehouse directory</h3>
                        </div>

                        <button
                            className="text-button"
                            onClick={() => setPage('Warehouses')}
                        >
                            View all →
                        </button>
                    </div>

                    {loading ? (
                        <div className="empty-content">
                            Loading warehouse data...
                        </div>
                    ) : apiError ? (
                        <div className="empty-content">
                            Warehouse data unavailable.
                        </div>
                    ) : warehouses.length === 0 ? (
                        <div className="empty-content">
                            <span className="empty-symbol">▤</span>
                            <strong>No warehouses registered</strong>
                            <p>Your warehouse records will appear here.</p>

                            <button
                                className="text-button"
                                onClick={() => setPage('Warehouses')}
                            >
                                Register a warehouse →
                            </button>
                        </div>
                    ) : (
                        <div className="warehouse-list">
                            {warehouses.slice(0, 5).map(warehouse => (
                                <div className="warehouse-row" key={warehouse.warehouseId}>
                                    <button
                                        type="button"
                                        className="warehouse-symbol warehouse-clickable"
                                        disabled={!warehouse.isActive}
                                        onClick={() => onWarehouseSelect?.(warehouse)}
                                        title="Open warehouse dashboard"
                                    >
                                        W
                                    </button>

                                    <div className="warehouse-details">
                                        <strong>
                                            <button
                                                type="button"
                                                className="warehouse-name-button"
                                                disabled={!warehouse.isActive}
                                                onClick={() => onWarehouseSelect?.(warehouse)}
                                            >
                                                {warehouse.warehouseName}
                                            </button>
                                        </strong>
                                        <span>
                                            {warehouse.warehouseCode} · {warehouse.city || 'Location not specified'}
                                        </span>
                                    </div>
                                    <span
                                        className={
                                            warehouse.isActive
                                                ? 'status-active'
                                                : 'status-inactive'
                                        }
                                    >
                                        {warehouse.isActive ? 'Active' : 'Inactive'}
                                    </span>
                                </div>
                            ))}
                        </div>
                    )}
                </div>

                <div className="panel system-panel">
                    <div className="panel-heading">
                        <div>
                            <span className="eyebrow">SYSTEM HEALTH</span>
                            <h3>Connection status</h3>
                        </div>
                    </div>

                    <div className="health-list">
                        <div className="health-row">
                            <span>React frontend</span>
                            <strong className="health-good">Running</strong>
                        </div>

                        <div className="health-row">
                            <span>ASP.NET Core API</span>
                            <strong className={apiError ? 'health-bad' : 'health-good'}>
                                {loading
                                    ? 'Checking'
                                    : apiError
                                        ? 'Unavailable'
                                        : 'Connected'}
                            </strong>
                        </div>

                        <div className="health-row">
                            <span>PostgreSQL</span>
                            <strong className={apiError ? 'health-bad' : 'health-good'}>
                                {loading
                                    ? 'Checking'
                                    : apiError
                                        ? 'Unverified'
                                        : 'API accessible'}
                            </strong>
                        </div>
                    </div>

                    <div className="system-note">
                        <span>i</span>
                        Database status is inferred from successful API requests.
                    </div>
                </div>
            </section>
        </>
    )
}

export default Dashboard
