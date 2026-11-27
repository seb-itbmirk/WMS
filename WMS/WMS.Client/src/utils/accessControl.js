
const pageAccess = {
    Dashboard: {
        any: ['dashboard.view'],
        roles: [
            'System Admin', 'Warehouse Manager', 'Warehouse Supervisor',
            'Warehouse Staff', 'Inventory Controller', 'Production Staff',
            'Quality Inspector', 'Auditor'
        ]
    },
    Warehouses: {
        any: ['warehouses.view'],
        roles: ['System Admin', 'Warehouse Manager']
    },
    'Storage Locations': {
        any: ['inventory.view'],
        roles: [
            'System Admin', 'Warehouse Manager', 'Warehouse Supervisor',
            'Warehouse Staff', 'Inventory Controller', 'Auditor'
        ]
    },
    'Items & Materials': {
        any: ['inventory.view'],
        roles: [
            'System Admin', 'Warehouse Manager', 'Warehouse Supervisor',
            'Warehouse Staff', 'Inventory Controller', 'Production Staff',
            'Quality Inspector', 'Auditor'
        ]
    },
    'Stock Overview': {
        any: ['inventory.view'],
        roles: [
            'System Admin', 'Warehouse Manager', 'Warehouse Supervisor',
            'Warehouse Staff', 'Inventory Controller', 'Auditor'
        ]
    },
    'Stock Transfers': {
        any: ['inventory.view', 'inventory.transfer'],
        roles: [
            'System Admin', 'Warehouse Manager', 'Warehouse Supervisor',
            'Warehouse Staff', 'Inventory Controller', 'Auditor'
        ]
    },
    'Stock Adjustments': {
        any: ['inventory.view', 'inventory.adjust'],
        roles: ['System Admin', 'Warehouse Manager', 'Inventory Controller', 'Auditor']
    },
    'Goods Receipts': {
        any: ['receiving.view', 'receiving.manage'],
        roles: [
            'System Admin', 'Warehouse Manager', 'Warehouse Supervisor',
            'Warehouse Staff', 'Auditor'
        ]
    },
    'Putaway Tasks': {
        any: ['putaway.view', 'putaway.manage'],
        roles: [
            'System Admin', 'Warehouse Manager', 'Warehouse Supervisor',
            'Warehouse Staff', 'Auditor'
        ]
    },
    'Stock Counts': {
        any: ['inventory.view', 'inventory.count'],
        roles: [
            'System Admin', 'Warehouse Manager', 'Warehouse Supervisor',
            'Warehouse Staff', 'Inventory Controller', 'Auditor'
        ]
    },
    Replenishment: {
        any: ['inventory.replenish', 'inventory.replenish.execute'],
        roles: [
            'System Admin', 'Warehouse Manager', 'Warehouse Supervisor',
            'Warehouse Staff', 'Inventory Controller'
        ]
    },
    'Production Orders': {
        roles: ['System Admin', 'Warehouse Manager', 'Production Staff', 'Auditor']
    },
    'Bill of Materials': {
        roles: ['System Admin', 'Production Staff', 'Auditor']
    },
    'Purchase Orders': {
        roles: ['System Admin', 'Warehouse Manager', 'Auditor']
    },
    'Quality Inspections': {
        roles: [
            'System Admin', 'Warehouse Manager', 'Warehouse Supervisor',
            'Quality Inspector', 'Auditor'
        ]
    },
    Customers: {
        roles: ['System Admin', 'Warehouse Manager', 'Auditor']
    },
    'Supplier Management': {
        roles: ['System Admin', 'Warehouse Manager', 'Auditor']
    },
    'Reports & Analytics': {
        roles: [
            'System Admin', 'Warehouse Manager', 'Warehouse Supervisor',
            'Inventory Controller', 'Quality Inspector', 'Auditor'
        ]
    },
    'Item Categories': {
        any: ['inventory.manage'],
        roles: ['System Admin', 'Warehouse Manager']
    },
    'Units of Measure': {
        roles: ['System Admin']
    },
    'Quality Parameters': {
        roles: ['System Admin', 'Quality Inspector']
    },
    'User Management': {
        any: ['users.view', 'users.invite'],
        roles: ['System Admin']
    },
    'User List': {
        any: ['users.view'],
        roles: ['System Admin']
    }
}

function normalizeRole(role) {
    if (typeof role === 'string') {
        return role.trim().toLowerCase().replace(/[^a-z0-9]/g, '')
    }

    if (role && typeof role === 'object') {
        const name =
            role.roleName ??
            role.RoleName ??
            role.name ??
            role.Name ??
            role.role ??
            role.Role ??
            ''

        return String(name).trim().toLowerCase().replace(/[^a-z0-9]/g, '')
    }

    return ''
}

export function getRoles(user) {
    const roles = user?.roles ?? user?.Roles ?? []

    if (!Array.isArray(roles)) return []

    return roles.map(normalizeRole).filter(Boolean)
}

export function isSuperAdmin(user) {
    return getRoles(user).includes('superadmin')
}

export function getPermissions(user) {
    const permissions = user?.permissions ?? user?.Permissions ?? []

    if (!Array.isArray(permissions)) return []

    return permissions.map(permission => {
        if (typeof permission === 'string') {
            return permission.trim().toLowerCase()
        }

        return String(
            permission.permissionCode ??
            permission.PermissionCode ??
            permission.code ??
            permission.Code ??
            ''
        ).trim().toLowerCase()
    }).filter(Boolean)
}

// Super Admin bypasses every frontend navigation restriction.
export function canAccessPage(page, user) {
    if (!user) return false

    if (isSuperAdmin(user)) return true

    const rule = pageAccess[page]

    if (!rule) return false

    const roles = getRoles(user)
    const permissions = getPermissions(user)

    if (rule.any?.length && permissions.length > 0) {
        return rule.any.some(permission =>
            permissions.includes(permission)
        )
    }

    return rule.roles?.some(role =>
        roles.includes(normalizeRole(role))
    ) ?? false
}

export function canPerform(permission, user) {
    if (!user) return false

    if (isSuperAdmin(user)) return true

    return getPermissions(user).includes(
        String(permission).trim().toLowerCase()
    )
}

export function hasPermission(user, permission) {
    if (!user || !permission) return false

    if (isSuperAdmin(user)) return true

    return getPermissions(user).includes(
        String(permission).trim().toLowerCase()
    )
}

export const getUserRoles = getRoles
export const getUserPermissions = getPermissions

export function hasAnyPermission(user, permissions = []) {
    if (!user) return false
    if (isSuperAdmin(user)) return true

    return permissions.some(permission =>
        hasPermission(user, permission)
    )
}

export function hasAllPermissions(user, permissions = []) {
    if (!user) return false
    if (isSuperAdmin(user)) return true

    return permissions.every(permission =>
        hasPermission(user, permission)
    )
}

export function canAccessWarehouse(user, warehouseId) {
    if (!user) return false

    if (isSuperAdmin(user)) return true

    const warehouseIds =
        user.warehouseIds ??
        user.WarehouseIds ??
        []

    return warehouseIds.some(id =>
        Number(id) === Number(warehouseId)
    )
}

export { pageAccess }
