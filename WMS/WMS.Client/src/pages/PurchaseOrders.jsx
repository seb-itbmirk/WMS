
import React, { useEffect, useMemo, useState } from "react";
import api from "../services/api";

const getValue = (obj, name) => {
    if (!obj) return undefined;

    const camelName = name.charAt(0).toLowerCase() + name.slice(1);
    return obj[name] ?? obj[camelName];
};

const getArray = (data) => {
    if (Array.isArray(data)) return data;
    if (Array.isArray(data?.$values)) return data.$values;
    if (Array.isArray(data?.data)) return data.data;
    if (Array.isArray(data?.data?.$values)) return data.data.$values;
    return [];
};

const getErrorMessage = (err) =>
    err.response?.data?.message ||
    err.response?.data?.title ||
    err.response?.data ||
    err.message ||
    "An unexpected error occurred.";

const getToday = () => {
    const today = new Date();
    const year = today.getFullYear();
    const month = String(today.getMonth() + 1).padStart(2, "0");
    const day = String(today.getDate()).padStart(2, "0");

    return `${year}-${month}-${day}`;
};

const emptyLine = () => ({
    itemId: "",
    orderedQuantity: "",
    uomId: "",
    unitCost: ""
});

const emptyPurchaseOrder = () => ({
    poNumber: "",
    supplierId: "",
    orderDate: getToday(),
    expectedDeliveryDate: "",
    lines: [emptyLine()]
});

const formatDate = (date) => {
    if (!date) return "—";

    const value = String(date).substring(0, 10);
    const parsed = new Date(`${value}T00:00:00`);

    if (Number.isNaN(parsed.getTime())) return value;

    return parsed.toLocaleDateString("en-US", {
        year: "numeric",
        month: "short",
        day: "numeric"
    });
};

const formatMoney = (value) => {
    if (value === null || value === undefined || value === "") return "—";

    return Number(value).toLocaleString("en-PH", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
};

const getStatusClass = (status) => {
    switch (String(status || "").toUpperCase()) {
        case "DRAFT":
            return "po-status-draft";
        case "APPROVED":
        case "RELEASED":
        case "COMPLETED":
            return "po-status-approved";
        case "CANCELLED":
        case "REJECTED":
            return "po-status-cancelled";
        case "PARTIALLY_RECEIVED":
        case "PARTIAL":
            return "po-status-partial";
        case "RECEIVED":
            return "po-status-received";
        default:
            return "po-status-default";
    }
};

function PurchaseOrdersPage() {
    const [purchaseOrders, setPurchaseOrders] = useState([]);
    const [suppliers, setSuppliers] = useState([]);
    const [items, setItems] = useState([]);
    const [units, setUnits] = useState([]);

    const [loading, setLoading] = useState(true);
    const [optionsLoading, setOptionsLoading] = useState(false);
    const [saving, setSaving] = useState(false);

    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");

    const [searchTerm, setSearchTerm] = useState("");
    const [filterStatus, setFilterStatus] = useState("ALL");

    const [showCreateModal, setShowCreateModal] = useState(false);
    const [showDetailsModal, setShowDetailsModal] = useState(false);
    const [showEditHeaderModal, setShowEditHeaderModal] = useState(false);
    const [showLineModal, setShowLineModal] = useState(false);

    const [selectedOrder, setSelectedOrder] = useState(null);
    const [editingLine, setEditingLine] = useState(null);

    const [createForm, setCreateForm] = useState(emptyPurchaseOrder());
    const [headerForm, setHeaderForm] = useState({
        poNumber: "",
        supplierId: "",
        orderDate: "",
        expectedDeliveryDate: ""
    });
    const [lineForm, setLineForm] = useState(emptyLine());

    const loadPurchaseOrders = async () => {
        try {
            setLoading(true);
            const response = await api.get("/purchase-orders");
            setPurchaseOrders(getArray(response.data));
            setError("");
        } catch (err) {
            setError(getErrorMessage(err));
        } finally {
            setLoading(false);
        }
    };

    const loadDropdownData = async () => {
        try {
            setOptionsLoading(true);

            const [supplierResponse, itemResponse, unitResponse] =
                await Promise.all([
                    api.get("/suppliers/active"),
                    api.get("/items"),
                    api.get("/units-of-measure")
                ]);

            setSuppliers(getArray(supplierResponse.data));

            setItems(
                getArray(itemResponse.data).filter(
                    (item) => getValue(item, "IsActive") === true
                )
            );

            setUnits(
                getArray(unitResponse.data).filter(
                    (unit) => getValue(unit, "IsActive") === true
                )
            );
        } catch (err) {
            setError(
                `Unable to load supplier, item, or UOM data. ${getErrorMessage(err)}`
            );
        } finally {
            setOptionsLoading(false);
        }
    };

    useEffect(() => {
        loadPurchaseOrders();
        loadDropdownData();
    }, []);

    const supplierName = (supplierId) => {
        const supplier = suppliers.find(
            (s) => Number(getValue(s, "SupplierId")) === Number(supplierId)
        );

        if (!supplier) return `Supplier #${supplierId}`;

        return `${getValue(supplier, "SupplierCode")} - ${getValue(supplier, "SupplierName")}`;
    };

    const filteredOrders = useMemo(() => {
        return purchaseOrders.filter((order) => {
            const poNumber = String(getValue(order, "PoNumber") || "").toLowerCase();
            const supplier = supplierName(getValue(order, "SupplierId")).toLowerCase();
            const status = String(getValue(order, "Status") || "").toUpperCase();

            const matchesSearch =
                poNumber.includes(searchTerm.toLowerCase()) ||
                supplier.includes(searchTerm.toLowerCase());

            const matchesStatus =
                filterStatus === "ALL" || status === filterStatus;

            return matchesSearch && matchesStatus;
        });
    }, [purchaseOrders, suppliers, searchTerm, filterStatus]);

    const draftCount = purchaseOrders.filter(
        (order) => String(getValue(order, "Status")).toUpperCase() === "DRAFT"
    ).length;

    const otherCount = purchaseOrders.length - draftCount;

    const openCreateModal = () => {
        setCreateForm(emptyPurchaseOrder());
        setError("");
        setSuccess("");
        setShowCreateModal(true);
    };

    const closeCreateModal = () => {
        if (saving) return;
        setShowCreateModal(false);
    };

    const updateCreateField = (field, value) => {
        setCreateForm((prev) => ({
            ...prev,
            [field]: value
        }));
    };

    const updateCreateLine = (index, field, value) => {
        setCreateForm((prev) => {
            const updatedLines = [...prev.lines];
            const updatedLine = { ...updatedLines[index] };

            updatedLine[field] = value;

            if (field === "itemId") {
                const selectedItem = items.find(
                    (item) => Number(getValue(item, "ItemId")) === Number(value)
                );

                updatedLine.uomId = selectedItem
                    ? String(getValue(selectedItem, "BaseUomId"))
                    : "";
            }

            updatedLines[index] = updatedLine;

            return {
                ...prev,
                lines: updatedLines
            };
        });
    };

    const addCreateLine = () => {
        setCreateForm((prev) => ({
            ...prev,
            lines: [...prev.lines, emptyLine()]
        }));
    };

    const removeCreateLine = (index) => {
        setCreateForm((prev) => ({
            ...prev,
            lines: prev.lines.filter((_, i) => i !== index)
        }));
    };

    const handleCreatePurchaseOrder = async (e) => {
        e.preventDefault();
        setError("");
        setSuccess("");

        if (!createForm.poNumber.trim()) {
            setError("Purchase order number is required.");
            return;
        }

        if (!createForm.supplierId) {
            setError("Please select a supplier.");
            return;
        }

        if (!createForm.orderDate) {
            setError("Order date is required.");
            return;
        }

        if (
            createForm.expectedDeliveryDate &&
            createForm.expectedDeliveryDate < createForm.orderDate
        ) {
            setError("Expected delivery date cannot be earlier than order date.");
            return;
        }

        if (createForm.lines.length === 0) {
            setError("Please add at least one purchase order item.");
            return;
        }

        for (let i = 0; i < createForm.lines.length; i++) {
            const line = createForm.lines[i];

            if (!line.itemId || !line.uomId) {
                setError(`Please select an item and UOM for line ${i + 1}.`);
                return;
            }

            if (!line.orderedQuantity || Number(line.orderedQuantity) <= 0) {
                setError(`Line ${i + 1} must have a quantity greater than zero.`);
                return;
            }

            if (line.unitCost !== "" && Number(line.unitCost) < 0) {
                setError(`Line ${i + 1} cannot have a negative unit cost.`);
                return;
            }
        }

        const payload = {
            poNumber: createForm.poNumber.trim(),
            supplierId: Number(createForm.supplierId),
            orderDate: createForm.orderDate,
            expectedDeliveryDate: createForm.expectedDeliveryDate || null,
            lines: createForm.lines.map((line) => ({
                itemId: Number(line.itemId),
                orderedQuantity: Number(line.orderedQuantity),
                uomId: Number(line.uomId),
                unitCost: line.unitCost === "" ? null : Number(line.unitCost)
            }))
        };

        try {
            setSaving(true);

            await api.post("/purchase-orders", payload);

            setShowCreateModal(false);
            setCreateForm(emptyPurchaseOrder());
            setSuccess("Purchase order created successfully.");

            await loadPurchaseOrders();
        } catch (err) {
            setError(getErrorMessage(err));
        } finally {
            setSaving(false);
        }
    };

    const openDetails = async (id) => {
        try {
            setError("");
            setSuccess("");

            const response = await api.get(`/purchase-orders/${id}`);
            setSelectedOrder(response.data);
            setShowDetailsModal(true);
        } catch (err) {
            setError(getErrorMessage(err));
        }
    };

    const refreshSelectedOrder = async (id) => {
        const response = await api.get(`/purchase-orders/${id}`);
        setSelectedOrder(response.data);
        await loadPurchaseOrders();
    };

    const openEditHeader = () => {
        if (!selectedOrder) return;

        setHeaderForm({
            poNumber: getValue(selectedOrder, "PoNumber") || "",
            supplierId: String(getValue(selectedOrder, "SupplierId") || ""),
            orderDate: String(getValue(selectedOrder, "OrderDate") || "").substring(0, 10),
            expectedDeliveryDate: getValue(selectedOrder, "ExpectedDeliveryDate")
                ? String(getValue(selectedOrder, "ExpectedDeliveryDate")).substring(0, 10)
                : ""
        });

        setShowEditHeaderModal(true);
    };

    const handleUpdateHeader = async (e) => {
        e.preventDefault();

        if (!selectedOrder) return;

        if (!headerForm.poNumber.trim() || !headerForm.supplierId || !headerForm.orderDate) {
            setError("Please complete all required purchase order fields.");
            return;
        }

        if (
            headerForm.expectedDeliveryDate &&
            headerForm.expectedDeliveryDate < headerForm.orderDate
        ) {
            setError("Expected delivery date cannot be earlier than order date.");
            return;
        }

        const id = getValue(selectedOrder, "PurchaseOrderId");

        try {
            setSaving(true);
            setError("");

            await api.put(`/purchase-orders/${id}`, {
                poNumber: headerForm.poNumber.trim(),
                supplierId: Number(headerForm.supplierId),
                orderDate: headerForm.orderDate,
                expectedDeliveryDate: headerForm.expectedDeliveryDate || null
            });

            setShowEditHeaderModal(false);
            await refreshSelectedOrder(id);
            setSuccess("Purchase order updated successfully.");
        } catch (err) {
            setError(getErrorMessage(err));
        } finally {
            setSaving(false);
        }
    };

    const openAddLine = () => {
        setEditingLine(null);
        setLineForm(emptyLine());
        setError("");
        setShowLineModal(true);
    };

    const openEditLine = (line) => {
        setEditingLine(line);

        setLineForm({
            itemId: String(getValue(line, "ItemId") || ""),
            orderedQuantity: String(getValue(line, "OrderedQuantity") ?? ""),
            uomId: String(getValue(line, "UomId") || ""),
            unitCost:
                getValue(line, "UnitCost") === null ||
                getValue(line, "UnitCost") === undefined
                    ? ""
                    : String(getValue(line, "UnitCost"))
        });

        setError("");
        setShowLineModal(true);
    };

    const updateLineField = (field, value) => {
        setLineForm((prev) => {
            const updated = { ...prev, [field]: value };

            if (field === "itemId") {
                const selectedItem = items.find(
                    (item) => Number(getValue(item, "ItemId")) === Number(value)
                );

                updated.uomId = selectedItem
                    ? String(getValue(selectedItem, "BaseUomId"))
                    : "";
            }

            return updated;
        });
    };

    const handleSaveLine = async (e) => {
        e.preventDefault();

        if (!selectedOrder) return;

        if (!lineForm.itemId || !lineForm.uomId) {
            setError("Please select an item and unit of measure.");
            return;
        }

        if (!lineForm.orderedQuantity || Number(lineForm.orderedQuantity) <= 0) {
            setError("Ordered quantity must be greater than zero.");
            return;
        }

        if (lineForm.unitCost !== "" && Number(lineForm.unitCost) < 0) {
            setError("Unit cost cannot be negative.");
            return;
        }

        const orderId = getValue(selectedOrder, "PurchaseOrderId");

        const payload = {
            itemId: Number(lineForm.itemId),
            orderedQuantity: Number(lineForm.orderedQuantity),
            uomId: Number(lineForm.uomId),
            unitCost: lineForm.unitCost === "" ? null : Number(lineForm.unitCost)
        };

        try {
            setSaving(true);
            setError("");

            if (editingLine) {
                const lineId = getValue(editingLine, "PoLineId");

                await api.put(
                    `/purchase-orders/${orderId}/lines/${lineId}`,
                    payload
                );
            } else {
                await api.post(`/purchase-orders/${orderId}/lines`, payload);
            }

            setShowLineModal(false);
            setEditingLine(null);

            await refreshSelectedOrder(orderId);
            setSuccess(
                editingLine
                    ? "Purchase order item updated successfully."
                    : "Purchase order item added successfully."
            );
        } catch (err) {
            setError(getErrorMessage(err));
        } finally {
            setSaving(false);
        }
    };

    const handleDeleteLine = async (line) => {
        if (!selectedOrder) return;

        const lineId = getValue(line, "PoLineId");
        const itemCode = getValue(line, "ItemCode") || "this item";

        if (!window.confirm(`Remove ${itemCode} from this purchase order?`)) {
            return;
        }

        const orderId = getValue(selectedOrder, "PurchaseOrderId");

        try {
            setError("");

            await api.delete(`/purchase-orders/${orderId}/lines/${lineId}`);

            await refreshSelectedOrder(orderId);
            setSuccess("Purchase order item removed successfully.");
        } catch (err) {
            setError(getErrorMessage(err));
        }
    };

    const selectedOrderLines = getArray(
        getValue(selectedOrder, "Lines")
    );

    const isSelectedOrderDraft =
        String(getValue(selectedOrder, "Status") || "").toUpperCase() === "DRAFT";

    const getItemName = (itemId) => {
        const item = items.find(
            (i) => Number(getValue(i, "ItemId")) === Number(itemId)
        );

        return item
            ? `${getValue(item, "ItemCode")} - ${getValue(item, "ItemName")}`
            : "";
    };

    return (
        <div className="page-content purchase-orders-page">
            <div className="po-page-header">
                <div>
                    <h1>Purchase Orders</h1>
                    <p>Manage supplier orders and incoming materials.</p>
                </div>

                <button
                    type="button"
                    className="po-primary-button"
                    onClick={openCreateModal}
                >
                    + Create Purchase Order
                </button>
            </div>

            {error && !showCreateModal && !showEditHeaderModal && !showLineModal && (
                <div className="po-alert po-alert-error">
                    {error}
                    <button type="button" onClick={() => setError("")}>×</button>
                </div>
            )}

            {success && (
                <div className="po-alert po-alert-success">
                    {success}
                    <button type="button" onClick={() => setSuccess("")}>×</button>
                </div>
            )}

            <div className="po-summary-grid">
                <div className="po-summary-card">
                    <span>Total Purchase Orders</span>
                    <strong>{purchaseOrders.length}</strong>
                </div>

                <div className="po-summary-card">
                    <span>Draft Orders</span>
                    <strong>{draftCount}</strong>
                </div>

                <div className="po-summary-card">
                    <span>Other Statuses</span>
                    <strong>{otherCount}</strong>
                </div>
            </div>

            <section className="po-section">
                <div className="po-section-heading">
                    <div>
                        <h2>Purchase Order List</h2>
                        <p>View and manage your purchase orders.</p>
                    </div>

                    <span className="po-record-count">
                        {filteredOrders.length} Records
                    </span>
                </div>

                <div className="po-toolbar">
                    <input
                        type="text"
                        placeholder="Search PO number or supplier..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                    />

                    <select
                        value={filterStatus}
                        onChange={(e) => setFilterStatus(e.target.value)}
                    >
                        <option value="ALL">All Statuses</option>
                        <option value="DRAFT">Draft</option>
                        <option value="APPROVED">Approved</option>
                        <option value="RELEASED">Released</option>
                        <option value="PARTIALLY_RECEIVED">Partially Received</option>
                        <option value="RECEIVED">Received</option>
                        <option value="CANCELLED">Cancelled</option>
                    </select>

                    <button
                        type="button"
                        className="po-secondary-button"
                        onClick={loadPurchaseOrders}
                        disabled={loading}
                    >
                        Refresh
                    </button>
                </div>

                {loading ? (
                    <div className="po-empty-state">Loading purchase orders...</div>
                ) : filteredOrders.length === 0 ? (
                    <div className="po-empty-state">
                        No purchase orders found.
                    </div>
                ) : (
                    <div className="po-table-wrapper">
                        <table className="po-table">
                            <thead>
                                <tr>
                                    <th>PO Number</th>
                                    <th>Supplier</th>
                                    <th>Order Date</th>
                                    <th>Expected Delivery</th>
                                    <th>Items</th>
                                    <th>Status</th>
                                    <th>Action</th>
                                </tr>
                            </thead>

                            <tbody>
                                {filteredOrders.map((order) => {
                                    const id = getValue(order, "PurchaseOrderId");
                                    const status = getValue(order, "Status");

                                    return (
                                        <tr key={id}>
                                            <td className="po-number-cell">
                                                {getValue(order, "PoNumber")}
                                            </td>
                                            <td>
                                                {supplierName(getValue(order, "SupplierId"))}
                                            </td>
                                            <td>
                                                {formatDate(getValue(order, "OrderDate"))}
                                            </td>
                                            <td>
                                                {formatDate(getValue(order, "ExpectedDeliveryDate"))}
                                            </td>
                                            <td>
                                                {getValue(order, "LineCount") ?? 0}
                                            </td>
                                            <td>
                                                <span className={`po-status-badge ${getStatusClass(status)}`}>
                                                    {status || "UNKNOWN"}
                                                </span>
                                            </td>
                                            <td>
                                                <button
                                                    type="button"
                                                    className="po-view-button"
                                                    onClick={() => openDetails(id)}
                                                >
                                                    View Details
                                                </button>
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>
                )}
            </section>

            {/* CREATE PURCHASE ORDER MODAL */}
            {showCreateModal && (
                <div className="po-modal-overlay">
                    <div className="po-modal po-create-modal">
                        <div className="po-modal-header">
                            <div>
                                <h2>Create Purchase Order</h2>
                                <p>Enter the purchase order details and items.</p>
                            </div>

                            <button
                                type="button"
                                className="po-close-button"
                                onClick={closeCreateModal}
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={handleCreatePurchaseOrder}>
                            <div className="po-modal-body">
                                {error && (
                                    <div className="po-alert po-alert-error">
                                        {error}
                                    </div>
                                )}

                                {optionsLoading ? (
                                    <div className="po-empty-state">
                                        Loading dropdown options...
                                    </div>
                                ) : (
                                    <>
                                        <div className="po-form-grid">
                                            <div className="po-form-group">
                                                <label>Purchase Order Number *</label>
                                                <input
                                                    type="text"
                                                    value={createForm.poNumber}
                                                    onChange={(e) =>
                                                        updateCreateField("poNumber", e.target.value)
                                                    }
                                                    placeholder="Enter PO number"
                                                    required
                                                />
                                            </div>

                                            <div className="po-form-group">
                                                <label>Supplier *</label>
                                                <select
                                                    value={createForm.supplierId}
                                                    onChange={(e) =>
                                                        updateCreateField("supplierId", e.target.value)
                                                    }
                                                    required
                                                >
                                                    <option value="">Select supplier</option>
                                                    {suppliers.map((supplier) => (
                                                        <option
                                                            key={getValue(supplier, "SupplierId")}
                                                            value={getValue(supplier, "SupplierId")}
                                                        >
                                                            {getValue(supplier, "SupplierCode")} - {getValue(supplier, "SupplierName")}
                                                        </option>
                                                    ))}
                                                </select>
                                            </div>

                                            <div className="po-form-group">
                                                <label>Order Date *</label>
                                                <input
                                                    type="date"
                                                    value={createForm.orderDate}
                                                    onChange={(e) =>
                                                        updateCreateField("orderDate", e.target.value)
                                                    }
                                                    required
                                                />
                                            </div>

                                            <div className="po-form-group">
                                                <label>Expected Delivery Date</label>
                                                <input
                                                    type="date"
                                                    min={createForm.orderDate}
                                                    value={createForm.expectedDeliveryDate}
                                                    onChange={(e) =>
                                                        updateCreateField("expectedDeliveryDate", e.target.value)
                                                    }
                                                />
                                            </div>
                                        </div>

                                        <div className="po-lines-heading">
                                            <div>
                                                <h3>Purchase Order Items</h3>
                                                <p>Add one or more items to this order.</p>
                                            </div>

                                            <button
                                                type="button"
                                                className="po-secondary-button"
                                                onClick={addCreateLine}
                                            >
                                                + Add Item
                                            </button>
                                        </div>

                                        {createForm.lines.map((line, index) => (
                                            <div className="po-edit-line-card" key={index}>
                                                <div className="po-edit-line-heading">
                                                    <strong>Item {index + 1}</strong>

                                                    {createForm.lines.length > 1 && (
                                                        <button
                                                            type="button"
                                                            className="po-remove-button"
                                                            onClick={() => removeCreateLine(index)}
                                                        >
                                                            Remove
                                                        </button>
                                                    )}
                                                </div>

                                                <div className="po-form-grid">
                                                    <div className="po-form-group po-full-width">
                                                        <label>Item *</label>
                                                        <select
                                                            value={line.itemId}
                                                            onChange={(e) =>
                                                                updateCreateLine(index, "itemId", e.target.value)
                                                            }
                                                            required
                                                        >
                                                            <option value="">Select item</option>
                                                            {items.map((item) => (
                                                                <option
                                                                    key={getValue(item, "ItemId")}
                                                                    value={getValue(item, "ItemId")}
                                                                >
                                                                    {getValue(item, "ItemCode")} - {getValue(item, "ItemName")}
                                                                </option>
                                                            ))}
                                                        </select>
                                                    </div>

                                                    <div className="po-form-group">
                                                        <label>Ordered Quantity *</label>
                                                        <input
                                                            type="number"
                                                            min="0.000001"
                                                            step="0.000001"
                                                            value={line.orderedQuantity}
                                                            onChange={(e) =>
                                                                updateCreateLine(index, "orderedQuantity", e.target.value)
                                                            }
                                                            required
                                                        />
                                                    </div>

                                                    <div className="po-form-group">
                                                        <label>Unit of Measure *</label>
                                                        <select
                                                            value={line.uomId}
                                                            onChange={(e) =>
                                                                updateCreateLine(index, "uomId", e.target.value)
                                                            }
                                                            required
                                                        >
                                                            <option value="">Select UOM</option>
                                                            {units.map((unit) => (
                                                                <option
                                                                    key={getValue(unit, "UomId")}
                                                                    value={getValue(unit, "UomId")}
                                                                >
                                                                    {getValue(unit, "UomCode")} - {getValue(unit, "UomName")}
                                                                </option>
                                                            ))}
                                                        </select>
                                                    </div>

                                                    <div className="po-form-group">
                                                        <label>Unit Cost</label>
                                                        <input
                                                            type="number"
                                                            min="0"
                                                            step="0.01"
                                                            value={line.unitCost}
                                                            onChange={(e) =>
                                                                updateCreateLine(index, "unitCost", e.target.value)
                                                            }
                                                            placeholder="Optional"
                                                        />
                                                    </div>
                                                </div>
                                            </div>
                                        ))}
                                    </>
                                )}
                            </div>

                            <div className="po-modal-actions">
                                <button
                                    type="button"
                                    className="po-secondary-button"
                                    onClick={closeCreateModal}
                                    disabled={saving}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="submit"
                                    className="po-primary-button"
                                    disabled={saving || optionsLoading}
                                >
                                    {saving ? "Creating..." : "Create Purchase Order"}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {/* PURCHASE ORDER DETAILS MODAL */}
            {showDetailsModal && selectedOrder && (
                <div className="po-modal-overlay">
                    <div className="po-modal po-details-modal">
                        <div className="po-modal-header">
                            <div>
                                <h2>Purchase Order Details</h2>
                                <p>{getValue(selectedOrder, "PoNumber")}</p>
                            </div>

                            <button
                                type="button"
                                className="po-close-button"
                                onClick={() => setShowDetailsModal(false)}
                            >
                                ×
                            </button>
                        </div>

                        <div className="po-modal-body">
                            {error && (
                                <div className="po-alert po-alert-error">
                                    {error}
                                </div>
                            )}

                            <div className="po-detail-grid">
                                <div>
                                    <span>PO Number</span>
                                    <strong>{getValue(selectedOrder, "PoNumber")}</strong>
                                </div>

                                <div>
                                    <span>Supplier</span>
                                    <strong>{supplierName(getValue(selectedOrder, "SupplierId"))}</strong>
                                </div>

                                <div>
                                    <span>Order Date</span>
                                    <strong>{formatDate(getValue(selectedOrder, "OrderDate"))}</strong>
                                </div>

                                <div>
                                    <span>Expected Delivery</span>
                                    <strong>{formatDate(getValue(selectedOrder, "ExpectedDeliveryDate"))}</strong>
                                </div>

                                <div>
                                    <span>Status</span>
                                    <strong>
                                        <span className={`po-status-badge ${getStatusClass(getValue(selectedOrder, "Status"))}`}>
                                            {getValue(selectedOrder, "Status")}
                                        </span>
                                    </strong>
                                </div>

                                <div>
                                    <span>Created At</span>
                                    <strong>{formatDate(getValue(selectedOrder, "CreatedAt"))}</strong>
                                </div>
                            </div>

                            <div className="po-lines-heading">
                                <div>
                                    <h3>Ordered Items</h3>
                                    <p>{selectedOrderLines.length} item lines</p>
                                </div>

                                {isSelectedOrderDraft && (
                                    <button
                                        type="button"
                                        className="po-secondary-button"
                                        onClick={openAddLine}
                                    >
                                        + Add Item
                                    </button>
                                )}
                            </div>

                            {selectedOrderLines.length === 0 ? (
                                <div className="po-empty-state">
                                    No purchase order lines found.
                                </div>
                            ) : (
                                <div className="po-table-wrapper">
                                    <table className="po-table po-detail-table">
                                        <thead>
                                            <tr>
                                                <th>Item Code</th>
                                                <th>Item Name</th>
                                                <th>Quantity</th>
                                                <th>UOM</th>
                                                <th>Unit Cost</th>
                                                {isSelectedOrderDraft && <th>Actions</th>}
                                            </tr>
                                        </thead>

                                        <tbody>
                                            {selectedOrderLines.map((line) => (
                                                <tr key={getValue(line, "PoLineId")}>
                                                    <td className="po-number-cell">
                                                        {getValue(line, "ItemCode")}
                                                    </td>
                                                    <td>{getValue(line, "ItemName")}</td>
                                                    <td>
                                                        {Number(getValue(line, "OrderedQuantity")).toLocaleString()}
                                                    </td>
                                                    <td>{getValue(line, "UomCode")}</td>
                                                    <td>{formatMoney(getValue(line, "UnitCost"))}</td>

                                                    {isSelectedOrderDraft && (
                                                        <td>
                                                            <div className="po-line-actions">
                                                                <button
                                                                    type="button"
                                                                    className="po-view-button"
                                                                    onClick={() => openEditLine(line)}
                                                                >
                                                                    Edit
                                                                </button>

                                                                <button
                                                                    type="button"
                                                                    className="po-remove-button"
                                                                    onClick={() => handleDeleteLine(line)}
                                                                >
                                                                    Delete
                                                                </button>
                                                            </div>
                                                        </td>
                                                    )}
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                </div>
                            )}
                        </div>

                        <div className="po-modal-actions">
                            {isSelectedOrderDraft && (
                                <button
                                    type="button"
                                    className="po-secondary-button"
                                    onClick={openEditHeader}
                                >
                                    Edit Purchase Order
                                </button>
                            )}

                            <button
                                type="button"
                                className="po-primary-button"
                                onClick={() => setShowDetailsModal(false)}
                            >
                                Close
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* EDIT PURCHASE ORDER HEADER MODAL */}
            {showEditHeaderModal && (
                <div className="po-modal-overlay po-nested-modal">
                    <div className="po-modal po-small-modal">
                        <div className="po-modal-header">
                            <div>
                                <h2>Edit Purchase Order</h2>
                                <p>Update the draft purchase order information.</p>
                            </div>

                            <button
                                type="button"
                                className="po-close-button"
                                onClick={() => setShowEditHeaderModal(false)}
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={handleUpdateHeader}>
                            <div className="po-modal-body">
                                {error && (
                                    <div className="po-alert po-alert-error">{error}</div>
                                )}

                                <div className="po-form-grid">
                                    <div className="po-form-group po-full-width">
                                        <label>Purchase Order Number *</label>
                                        <input
                                            type="text"
                                            value={headerForm.poNumber}
                                            onChange={(e) =>
                                                setHeaderForm({ ...headerForm, poNumber: e.target.value })
                                            }
                                            required
                                        />
                                    </div>

                                    <div className="po-form-group po-full-width">
                                        <label>Supplier *</label>
                                        <select
                                            value={headerForm.supplierId}
                                            onChange={(e) =>
                                                setHeaderForm({ ...headerForm, supplierId: e.target.value })
                                            }
                                            required
                                        >
                                            <option value="">Select supplier</option>
                                            {suppliers.map((supplier) => (
                                                <option
                                                    key={getValue(supplier, "SupplierId")}
                                                    value={getValue(supplier, "SupplierId")}
                                                >
                                                    {getValue(supplier, "SupplierCode")} - {getValue(supplier, "SupplierName")}
                                                </option>
                                            ))}
                                        </select>
                                    </div>

                                    <div className="po-form-group">
                                        <label>Order Date *</label>
                                        <input
                                            type="date"
                                            value={headerForm.orderDate}
                                            onChange={(e) =>
                                                setHeaderForm({ ...headerForm, orderDate: e.target.value })
                                            }
                                            required
                                        />
                                    </div>

                                    <div className="po-form-group">
                                        <label>Expected Delivery</label>
                                        <input
                                            type="date"
                                            min={headerForm.orderDate}
                                            value={headerForm.expectedDeliveryDate}
                                            onChange={(e) =>
                                                setHeaderForm({ ...headerForm, expectedDeliveryDate: e.target.value })
                                            }
                                        />
                                    </div>
                                </div>
                            </div>

                            <div className="po-modal-actions">
                                <button
                                    type="button"
                                    className="po-secondary-button"
                                    onClick={() => setShowEditHeaderModal(false)}
                                    disabled={saving}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="submit"
                                    className="po-primary-button"
                                    disabled={saving}
                                >
                                    {saving ? "Saving..." : "Save Changes"}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {/* ADD / EDIT PURCHASE ORDER LINE MODAL */}
            {showLineModal && (
                <div className="po-modal-overlay po-nested-modal">
                    <div className="po-modal po-small-modal">
                        <div className="po-modal-header">
                            <div>
                                <h2>{editingLine ? "Edit Item" : "Add Item"}</h2>
                                <p>Enter the purchase order item information.</p>
                            </div>

                            <button
                                type="button"
                                className="po-close-button"
                                onClick={() => setShowLineModal(false)}
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={handleSaveLine}>
                            <div className="po-modal-body">
                                {error && (
                                    <div className="po-alert po-alert-error">{error}</div>
                                )}

                                <div className="po-form-grid">
                                    <div className="po-form-group po-full-width">
                                        <label>Item *</label>
                                        <select
                                            value={lineForm.itemId}
                                            onChange={(e) =>
                                                updateLineField("itemId", e.target.value)
                                            }
                                            required
                                        >
                                            <option value="">Select item</option>
                                            {items.map((item) => (
                                                <option
                                                    key={getValue(item, "ItemId")}
                                                    value={getValue(item, "ItemId")}
                                                >
                                                    {getValue(item, "ItemCode")} - {getValue(item, "ItemName")}
                                                </option>
                                            ))}
                                        </select>
                                    </div>

                                    <div className="po-form-group">
                                        <label>Ordered Quantity *</label>
                                        <input
                                            type="number"
                                            min="0.000001"
                                            step="0.000001"
                                            value={lineForm.orderedQuantity}
                                            onChange={(e) =>
                                                updateLineField("orderedQuantity", e.target.value)
                                            }
                                            required
                                        />
                                    </div>

                                    <div className="po-form-group">
                                        <label>Unit of Measure *</label>
                                        <select
                                            value={lineForm.uomId}
                                            onChange={(e) =>
                                                updateLineField("uomId", e.target.value)
                                            }
                                            required
                                        >
                                            <option value="">Select UOM</option>
                                            {units.map((unit) => (
                                                <option
                                                    key={getValue(unit, "UomId")}
                                                    value={getValue(unit, "UomId")}
                                                >
                                                    {getValue(unit, "UomCode")} - {getValue(unit, "UomName")}
                                                </option>
                                            ))}
                                        </select>
                                    </div>

                                    <div className="po-form-group po-full-width">
                                        <label>Unit Cost</label>
                                        <input
                                            type="number"
                                            min="0"
                                            step="0.01"
                                            value={lineForm.unitCost}
                                            onChange={(e) =>
                                                updateLineField("unitCost", e.target.value)
                                            }
                                            placeholder="Optional"
                                        />
                                    </div>
                                </div>

                                {lineForm.itemId && (
                                    <p className="po-selected-item-note">
                                        Selected item: {getItemName(lineForm.itemId)}
                                    </p>
                                )}
                            </div>

                            <div className="po-modal-actions">
                                <button
                                    type="button"
                                    className="po-secondary-button"
                                    onClick={() => setShowLineModal(false)}
                                    disabled={saving}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="submit"
                                    className="po-primary-button"
                                    disabled={saving}
                                >
                                    {saving ? "Saving..." : editingLine ? "Save Changes" : "Add Item"}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}

export default PurchaseOrdersPage;
