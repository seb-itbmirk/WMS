
import { useEffect, useState } from 'react'
import navigation from './navigation'

function Sidebar({ page, setPage, apiError, loading }) {

    // Track which navigation groups are expanded.
    const [expandedGroups, setExpandedGroups] = useState(() => {
        const initialState = {}

        navigation.forEach((group) => {
            initialState[group.heading] = group.links.some(
                (link) => link.name === page
            )
        })

        return initialState
    })

    // Automatically expand the group containing the selected page.
    useEffect(() => {
        const activeGroup = navigation.find((group) =>
            group.links.some((link) => link.name === page)
        )

        if (activeGroup) {
            setExpandedGroups((previous) => ({
                ...previous,
                [activeGroup.heading]: true
            }))
        }
    }, [page])

    // Toggle a navigation group.
    const toggleGroup = (heading) => {
        setExpandedGroups((previous) => ({
            ...previous,
            [heading]: !previous[heading]
        }))
    }

    return (
        <aside className="sidebar">

            {/* Brand */}
            <div className="brand">
                <div className="brand-symbol">
                    <span>W</span>
                </div>

                <div>
                    <h2>WAREHOUSE</h2>
                    <p>MANAGEMENT SYSTEM</p>
                </div>
            </div>

            {/* Navigation */}
            <div className="navigation">
                {navigation.map((group) => {
                    const isExpanded = !!expandedGroups[group.heading]

                    return (
                        <div
                            key={group.heading}
                            className={`nav-group ${
                                isExpanded ? 'expanded' : 'collapsed'
                            }`}
                        >
                            {/* Dropdown Heading */}
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

                            {/* Dropdown Links */}
                            <div
                                className={`nav-group-links ${
                                    isExpanded ? 'open' : ''
                                }`}
                            >
                                {group.links.map((link) => (
                                    <button
                                        key={link.name}
                                        type="button"
                                        className={`nav-link ${
                                            page === link.name ? 'selected' : ''
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

            {/* System Status */}
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
