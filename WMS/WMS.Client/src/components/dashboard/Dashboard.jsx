
function getUserRoles(currentUser) {
    const roles = currentUser?.roles ?? currentUser?.Roles ?? []

    return roles.map(role =>
        typeof role === 'string'
            ? role.toLowerCase()
            : (role.roleName ?? role.RoleName ?? role.name ?? role.Name ?? '').toLowerCase()
    )
}

function getDashboardProfile(currentUser) {
    const roles = getUserRoles(currentUser)

    if (roles.includes('super admin')) {
        return {
            title: <>Warehouse<br />control center.</>,
            eyebrow: 'SYSTEM OVERVIEW',
            description: 'Monitor warehouse facilities, inventory, and manufacturing operations across the system.',
            showSystemHealth: true,
            showFacilityManagement: true,
            showProduction: true
        }
    }

    if (roles.includes('system admin')) {
        return {
            title: <>Organization<br />overview.</>,
            eyebrow: 'ADMINISTRATION OVERVIEW',
            description: 'Monitor organization facilities and the operational status of your WMS.',
            showSystemHealth: true,
            showFacilityManagement: true,
            showProduction: true
        }
    }

    if (roles.includes('warehouse manager')) {
        return {
            title: <>Warehouse<br />management.</>,
            eyebrow: 'WAREHOUSE OVERVIEW',
            description: 'Monitor your assigned warehouse facilities and their manufacturing activity.',
            showSystemHealth: false,
            showFacilityManagement: true,
            showProduction: true
        }
    }

    if (roles.includes('warehouse supervisor')) {
        return {
            title: <>Warehouse<br />operations.</>,
            eyebrow: 'OPERATIONS OVERVIEW',
            description: 'Review your assigned facilities and monitor ongoing production activity.',
            showSystemHealth: false,
            showFacilityManagement: true,
            showProduction: true
        }
    }

    if (roles.includes('inventory controller')) {
        return {
            title: <>Inventory<br />control center.</>,
            eyebrow: 'INVENTORY OVERVIEW',
            description: 'Review your assigned facilities and access the inventory modules available to your account.',
            showSystemHealth: false,
            showFacilityManagement: true,
            showProduction: false
        }
    }

    if (roles.includes('quality inspector')) {
        return {
            title: <>Quality<br />operations.</>,
            eyebrow: 'QUALITY OVERVIEW',
            description: 'Review your assigned facilities and access quality inspection activities.',
            showSystemHealth: false,
            showFacilityManagement: true,
            showProduction: false
        }
    }

    if (roles.includes('auditor')) {
        return {
            title: <>Warehouse<br />audit overview.</>,
            eyebrow: 'AUDIT OVERVIEW',
            description: 'Review available warehouse information and operational records.',
            showSystemHealth: false,
            showFacilityManagement: true,
            showProduction: true
        }
    }

    return {
        title: <>Warehouse<br />operations.</>,
        eyebrow: 'OPERATIONS OVERVIEW',
        description: 'Access the warehouse information and operational modules available to your account.',
        showSystemHealth: false,
        showFacilityManagement: true,
        showProduction: true
    }
}

function Dashboard({
    currentUser,
    warehouses = [],
    activeWarehouses,
    activeCategories,
    loading,
    apiError,
    productionDashboard,
    productionLoading,
    productionError,
    setPage
}) {
    const profile = getDashboardProfile(currentUser)

    return (
        <>
            <section className="intro">
                <div>
                    <div className="eyebrow">
                        <span></span>
                        {profile.eyebrow}
                    </div>

                    <h1>{profile.title}</h1>

                    <p>{profile.description}</p>
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
                    {profile.showFacilityManagement && (
                        <div
                            className="metric clickable-metric"
                            onClick={() => setPage('Warehouses')}
                        >
                            <span className="metric-label">ACTIVE WAREHOUSES</span>

                            <div className="metric-value">
                                {loading ? '—' : activeWarehouses}
                                <span className="metric-icon">▤</span>
                            </div>

                            <p>Accessible facilities</p>
                        </div>
                    )}

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

                        <p>Inventory statistics not yet connected</p>
                    </div>

                    {profile.showProduction && (
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
                    )}
                </div>
            </section>

            {profile.showProduction && (
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
            )}

            <section className="lower-section">
                {profile.showFacilityManagement && (
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
                                <strong>No accessible warehouses</strong>
                                <p>Warehouse records assigned to your account will appear here.</p>
                            </div>
                        ) : (
                            <div className="warehouse-list">
                                {warehouses.slice(0, 5).map(warehouse => (
                                    <div className="warehouse-row" key={warehouse.warehouseId}>
                                        <span className="warehouse-symbol">W</span>

                                        <div className="warehouse-details">
                                            <strong>{warehouse.warehouseName}</strong>
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
                )}

                {profile.showSystemHealth && (
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
                )}
            </section>
        </>
    )
}

export default Dashboard
