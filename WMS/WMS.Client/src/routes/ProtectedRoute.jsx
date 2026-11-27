
import {
    canAccessPage,
    hasAnyPermission,
    hasRole
} from '../utils/accessControl'

function ProtectedRoute({
    currentUser,
    requiredPermission,
    requiredPermissions = [],
    requiredRoles = [],
    requireAllPermissions = false,
    fallback = null,
    children
}) {
    // User must be authenticated.
    if (!currentUser) {
        return fallback
    }

    // Check a required role, if specified.
    if (
        requiredRoles.length > 0 &&
        !requiredRoles.some(role => hasRole(currentUser, role))
    ) {
        return fallback
    }

    // Check a single required permission.
    if (
        requiredPermission &&
        !canAccessPage(currentUser, requiredPermission)
    ) {
        return fallback
    }

    // Check multiple required permissions.
    if (requiredPermissions.length > 0) {
        const hasAccess = requireAllPermissions
            ? requiredPermissions.every(permission =>
                hasAnyPermission(currentUser, [permission])
            )
            : hasAnyPermission(currentUser, requiredPermissions)

        if (!hasAccess) {
            return fallback
        }
    }

    return children
}

export default ProtectedRoute
