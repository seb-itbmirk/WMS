
function ManagementPage({
    title,
    subtitle,
    records,
    type,
    loading,
    onAdd,
    onEdit,
    onDeactivate
}) {
    const isWarehouse = type === 'warehouse'

    return (
        <section className="management-page">
            <div className="management-page-heading">
                <div>
                    <span className="eyebrow">MASTER DATA</span>
                    <h1>{title}</h1>
                    <p>{subtitle}</p>
                </div>

                <button className="primary-button" onClick={onAdd}>
                    + Add {isWarehouse ? 'warehouse' : 'category'}
                </button>
            </div>

            <div className="management-summary">
                <div>
                    <span>TOTAL RECORDS</span>
                    <strong>{loading ? '—' : records.length}</strong>
                </div>

                <div>
                    <span>ACTIVE RECORDS</span>
                    <strong>
                        {loading
                            ? '—'
                            : records.filter(r => r.isActive).length}
                    </strong>
                </div>
            </div>

            <div className="panel management-table-panel">
                {loading ? (
                    <div className="empty-content">
                        Loading records...
                    </div>
                ) : records.length === 0 ? (
                    <div className="empty-content">
                        <strong>No records found</strong>
                        <p>Click the Add button to create your first record.</p>
                    </div>
                ) : (
                    <div className="table-scroll">
                        <table className="management-table">
                            <thead>
                                <tr>
                                    <th>CODE</th>
                                    <th>NAME</th>
                                    {isWarehouse && <th>CITY</th>}
                                    {!isWarehouse && <th>DESCRIPTION</th>}
                                    <th>STATUS</th>
                                    <th>ACTIONS</th>
                                </tr>
                            </thead>

                            <tbody>
                                {records.map(record => (
                                    <tr
                                        key={
                                            isWarehouse
                                                ? record.warehouseId
                                                : record.categoryId
                                        }
                                    >
                                        <td>
                                            {isWarehouse
                                                ? record.warehouseCode
                                                : record.categoryCode}
                                        </td>

                                        <td>
                                            {isWarehouse
                                                ? record.warehouseName
                                                : record.categoryName}
                                        </td>

                                        <td>
                                            {isWarehouse
                                                ? record.city || '—'
                                                : record.description || '—'}
                                        </td>

                                        <td>
                                            <span
                                                className={
                                                    record.isActive
                                                        ? 'status-active'
                                                        : 'status-inactive'
                                                }
                                            >
                                                {record.isActive ? 'Active' : 'Inactive'}
                                            </span>
                                        </td>

                                        <td>
                                            <div className="table-actions">
                                                <button
                                                    className="edit-button"
                                                    onClick={() => onEdit(record)}
                                                >
                                                    Edit
                                                </button>

                                                {record.isActive && (
                                                    <button
                                                        className="deactivate-button"
                                                        onClick={() =>
                                                            onDeactivate(
                                                                isWarehouse
                                                                    ? record.warehouseId
                                                                    : record.categoryId
                                                            )
                                                        }
                                                    >
                                                        Deactivate
                                                    </button>
                                                )}
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>
        </section>
    )
}

export default ManagementPage
