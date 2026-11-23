
function ManagementModal({
    showForm,
    formType,
    editingId,
    formData,
    saving,
    message,
    onClose,
    onInputChange,
    onSave
}) {
    if (!showForm) return null

    return (
        <div className="modal-overlay">
            <div className="management-modal">
                <div className="modal-heading">
                    <div>
                        <span className="eyebrow">
                            {editingId ? 'UPDATE RECORD' : 'NEW RECORD'}
                        </span>

                        <h2>
                            {editingId ? 'Edit ' : 'Add '}
                            {formType === 'warehouse'
                                ? 'warehouse'
                                : 'item category'}
                        </h2>
                    </div>

                    <button
                        className="modal-close"
                        onClick={onClose}
                        type="button"
                    >
                        ×
                    </button>
                </div>

                <form onSubmit={onSave}>
                    {formType === 'warehouse' ? (
                        <>
                            <label>Warehouse code</label>
                            <input
                                name="warehouseCode"
                                value={formData.warehouseCode || ''}
                                onChange={onInputChange}
                                required
                                maxLength={30}
                                placeholder="e.g. WH001"
                            />

                            <label>Warehouse name</label>
                            <input
                                name="warehouseName"
                                value={formData.warehouseName || ''}
                                onChange={onInputChange}
                                required
                                maxLength={150}
                                placeholder="Enter warehouse name"
                            />

                            <label>City</label>
                            <input
                                name="city"
                                value={formData.city || ''}
                                onChange={onInputChange}
                                placeholder="Enter city"
                            />

                            <label>Address</label>
                            <textarea
                                name="address"
                                value={formData.address || ''}
                                onChange={onInputChange}
                                placeholder="Enter warehouse address"
                            />
                        </>
                    ) : (
                        <>
                            <label>Category code</label>
                            <input
                                name="categoryCode"
                                value={formData.categoryCode || ''}
                                onChange={onInputChange}
                                required
                                maxLength={30}
                                placeholder="e.g. RAW"
                            />

                            <label>Category name</label>
                            <input
                                name="categoryName"
                                value={formData.categoryName || ''}
                                onChange={onInputChange}
                                required
                                maxLength={100}
                                placeholder="Enter category name"
                            />

                            <label>Description</label>
                            <textarea
                                name="description"
                                value={formData.description || ''}
                                onChange={onInputChange}
                                placeholder="Enter category description"
                            />
                        </>
                    )}

                    {message && (
                        <div className="form-error">
                            {message}
                        </div>
                    )}

                    <div className="modal-actions">
                        <button
                            type="button"
                            className="secondary-button"
                            onClick={onClose}
                        >
                            Cancel
                        </button>

                        <button
                            type="submit"
                            className="primary-button"
                            disabled={saving}
                        >
                            {saving ? 'Saving...' : 'Save record'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    )
}

export default ManagementModal
