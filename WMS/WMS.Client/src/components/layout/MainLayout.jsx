
import Sidebar from './Sidebar'
import TopHeader from './TopHeader'

function MainLayout({
    page,
    setPage,
    apiError,
    loading,
    currentUser,
    onLogout,
    children
}) {
    return (
        <div className="layout">
            <div className="sidebar-hover-zone">
                <Sidebar
                    page={page}
                    setPage={setPage}
                    apiError={apiError}
                    loading={loading}
                />
            </div>

            <main className="main-area">
                <TopHeader
                    page={page}
                    currentUser={currentUser}
                    onLogout={onLogout}
                />

                <div className="page-content">
                    {apiError && (
                        <div className="error-banner">
                            Unable to retrieve data.
                            Check your backend API connection.
                        </div>
                    )}

                    {children}
                </div>
            </main>
        </div>
    )
}

export default MainLayout
