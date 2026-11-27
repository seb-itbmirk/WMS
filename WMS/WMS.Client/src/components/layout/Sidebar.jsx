
import { useEffect, useState } from 'react'
import navigation from './navigation'
import { canAccessPage, isSuperAdmin } from '../../utils/accessControl'

function Sidebar({
    page,
    setPage,
    apiError,
    loading,
    currentUser
}) {
    const superAdmin = isSuperAdmin(currentUser)

    // Filter navigation items according to the logged-in user.
    const visibleNavigation = navigation
        .map(group => ({
            ...group,
            links: group.links.filter(link =>
                superAdmin || canAccessPage(link.name, currentUser)
            )
        }))
        .filter(group => group.links.length > 0)

    const [expandedGroups, setExpandedGroups] = useState(() => {
        const initialState = {}

        visibleNavigation.forEach(group => {
            initialState[group.heading] = superAdmin ||
                group.links.some(link => link.name === page)
        })

        return initialState
    })

    // Keep visible groups expanded for Super Admin.
    useEffect(() => {
        if (superAdmin) {
            const allExpanded = {}

            visibleNavigation.forEach(group => {
                allExpanded[group.heading] = true
            })

            setExpandedGroups(allExpanded)
            return
        }

        const activeGroup = visibleNavigation.find(group =>
            group.links.some(link => link.name === page)
        )

        if (activeGroup) {
            setExpandedGroups(previous => ({
                ...previous,
                [activeGroup.heading]: true
            }))
        }
    }, [page, superAdmin, currentUser])

    const toggleGroup = heading => {
        setExpandedGroups(previous => ({
            ...previous,
            [heading]: !previous[heading]
        }))
    }

    return (
        <aside className="sidebar">
            <div className="brand">
                <div className="brand-symbol">
                    <span>W</span>
                </div>

                <div>
                    <h2>WAREHOUSE</h2>
                    <p>MANAGEMENT SYSTEM</p>
                </div>
            </div>

            <div className="navigation">
                {visibleNavigation.map(group => {
                    const isExpanded = !!expandedGroups[group.heading]

                    return (
                        <div
                            key={group.heading}
                            className={`nav-group ${
                                isExpanded ? 'expanded' : 'collapsed'
                            }`}
                        >
                            <button
                                type="button"
                                className="nav-heading nav-heading-button"
                                onClick={() => toggleGroup(group.heading)}
                                aria-expanded={isExpanded}
                            >
                                <span>{group.heading}</span>

                                <span
                                    className={`nav-chevron ${
                                        isExpanded ? 'rotated' : ''
                                    }`}
                                >
                                    ▾
                                </span>
                            </button>

                            <div
                                className={`nav-group-links ${
                                    isExpanded ? 'open' : ''
                                }`}
                            >
                                {group.links.map(link => (
                                    <button
                                        key={link.name}
                                        type="button"
                                        className={`nav-link ${
                                            page === link.name
                                                ? 'selected'
                                                : ''
                                        }`}
                                        onClick={() => setPage(link.name)}
                                    >
                                        <span className="nav-icon">
                                            {link.icon}
                                        </span>

                                        {link.name}
                                    </button>
                                ))}
                            </div>
                        </div>
                    )
                })}
            </div>

            <div className="sidebar-bottom">
                <div className="online-indicator"></div>

                <div>
                    <strong>System status</strong>

                    <span>
                        {apiError
                            ? 'API unavailable'
                            : loading
                                ? 'Checking connection'
                                : 'Operational'}
                    </span>
                </div>
            </div>
        </aside>
    )
}

export default Sidebar
