
import Sidebar from './Sidebar'
import TopHeader from './TopHeader'

function MainLayout({
    page,
    setPage,
    apiError,
    loading,
    children
}) {
    return (
        <div className="layout">
            <Sidebar
                page={page}
                setPage={setPage}
                apiError={apiError}
                loading={loading}
            />

            <main className="main-area">
                <TopHeader page={page} />

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
