
function TopHeader({ page }) {
    return (
        <header className="top-header">
            <div className="breadcrumb">
                Workspace <span>/</span> {page}
            </div>

            <div className="profile">
                <div className="profile-avatar">A</div>

                <div>
                    <strong>Administrator</strong>
                    <span>System administrator</span>
                </div>
            </div>
        </header>
    )
}

export default TopHeader
