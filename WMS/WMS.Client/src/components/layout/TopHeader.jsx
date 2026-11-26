
function TopHeader({ page, currentUser, onLogout }) {
    const fullName = currentUser?.fullName || currentUser?.username || 'User'

    const roles = currentUser?.roles || []

    const roleLabel = roles.length
        ? roles.join(', ')
        : 'WMS User'

    return (
        <header className="top-header">
            <div className="breadcrumb">
                Workspace <span>/</span> {page}
            </div>

            <div className="profile">
                <div className="profile-avatar">
                    {fullName.charAt(0).toUpperCase()}
                </div>

                <div className="profile-details">
                    <strong>{fullName}</strong>
                    <span>{roleLabel}</span>
                </div>

                <button
                    type="button"
                    className="logout-button"
                    onClick={onLogout}
                    title="Sign out"
                >
                    Sign out
                </button>
            </div>
        </header>
    )
}

export default TopHeader
