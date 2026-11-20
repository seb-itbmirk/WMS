
    import React, { useEffect, useState } from "react";
    import api from '../services/api'

    const StockAdjustments = () => {
    const [adjustments, setAdjustments] = useState([]);
    const [warehouses, setWarehouses] = useState([]);
    const [lots, setLots] = useState([]);
    const [zones, setZones] = useState([]);
    const [bins, setBins] = useState([]);
    const [selectedZoneId, setSelectedZoneId] = useState("");

    const [selectedAdjustment, setSelectedAdjustment] = useState(null);
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [showDetailsModal, setShowDetailsModal] = useState(false);
    const [showLineModal, setShowLineModal] = useState(false);

    const [editingLine, setEditingLine] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");

    const [createForm, setCreateForm] = useState({
        adjustmentNumber: "",
        warehouseId: "",
        adjustmentReason: "",
    });

    const [lineForm, setLineForm] = useState({
        lotId: "",
        binId: "",
        countedQuantity: "",
    });

    const [filterStatus, setFilterStatus] = useState("ALL");

    const getArray = (response) => {
        const data = response?.data ?? response;
        if (Array.isArray(data)) return data;
        if (Array.isArray(data?.$values)) return data.$values;
        return [];
    };

    const getValue = (object, ...keys) => {
        for (const key of keys) {
        if (object?.[key] !== undefined && object?.[key] !== null) {
            return object[key];
        }
        }
        return "";
    };

    const loadAdjustments = async () => {
        try {
        setLoading(true);
        const response = await api.get("/stock-adjustments");
        setAdjustments(getArray(response));
        } catch (err) {
        setError(err.response?.data?.message || "Failed to load stock adjustments.");
        } finally {
        setLoading(false);
        }
    };

    const loadDropdownData = async () => {
        try {
        const [warehouseResponse, lotResponse] = await Promise.all([
            api.get("/warehouses"),
            api.get("/inventory-lots"),
        ]);

        setWarehouses(getArray(warehouseResponse));
        setLots(getArray(lotResponse));
        } catch (err) {
        console.error("Initial dropdown loading error:", err.response?.data || err);
        setError(
            err.response?.data?.message ||
            "Failed to load warehouse or inventory lot data."
        );
        }
    };

    const loadZones = async (warehouseId) => {
        setZones([]);
        setBins([]);
        setSelectedZoneId("");

        if (!warehouseId) return [];

        try {
        const response = await api.get(`/warehouses/${warehouseId}/zones`);
        const loadedZones = getArray(response);
        setZones(loadedZones);
        return loadedZones;
        } catch (err) {
        console.error("Zone loading error:", err.response?.data || err);
        setError(err.response?.data?.message || "Failed to load warehouse zones.");
        return [];
        }
    };

    const loadBins = async (warehouseId, zoneId) => {
        setBins([]);
        setSelectedZoneId(String(zoneId || ""));

        if (!warehouseId || !zoneId) return [];

        try {
        const response = await api.get(
            `/warehouses/${warehouseId}/zones/${zoneId}/bins`
        );
        const loadedBins = getArray(response);
        setBins(loadedBins);
        return loadedBins;
        } catch (err) {
        console.error("Bin loading error:", err.response?.data || err);
        setError(err.response?.data?.message || "Failed to load storage bins.");
        return [];
        }
    };

    useEffect(() => {
        loadAdjustments();
        loadDropdownData();
    }, []);

    const openCreateModal = () => {
        setCreateForm({
        adjustmentNumber: "",
        warehouseId: "",
        adjustmentReason: "",
        });
        setError("");
        setShowCreateModal(true);
    };

    const createAdjustment = async (e) => {
        e.preventDefault();
        setError("");
        setSuccess("");

        try {
        const response = await api.post("/stock-adjustments", {
            adjustmentNumber: createForm.adjustmentNumber.trim(),
            warehouseId: Number(createForm.warehouseId),
            adjustmentReason: createForm.adjustmentReason.trim(),
        });

        setSuccess("Stock adjustment created successfully.");
        setShowCreateModal(false);
        await loadAdjustments();

        if (response.data?.adjustmentId) {
            await openDetails(response.data.adjustmentId);
        }
        } catch (err) {
        setError(
            err.response?.data?.message ||
            err.response?.data ||
            "Failed to create stock adjustment."
        );
        }
    };

    const openDetails = async (id) => {
        setError("");
        setSuccess("");

        try {
        const response = await api.get(`/stock-adjustments/${id}`);
        setSelectedAdjustment(response.data);
        setShowDetailsModal(true);
        } catch (err) {
        setError(err.response?.data?.message || "Failed to load adjustment details.");
        }
    };

    const openAddLine = async () => {
        setEditingLine(null);
        setLineForm({ lotId: "", binId: "", countedQuantity: "" });
        setError("");
        setBins([]);
        const warehouseId = getValue(
        selectedAdjustment,
        "warehouseId",
        "WarehouseId"
        );
        await loadZones(warehouseId);
        setShowLineModal(true);
    };

    const openEditLine = async (line) => {
        setEditingLine(line);
        setLineForm({
        lotId: String(getValue(line, "lotId", "LotId")),
        binId: String(getValue(line, "binId", "BinId")),
        countedQuantity: String(getValue(line, "countedQuantity", "CountedQuantity")),
        });
        setError("");

        const warehouseId = getValue(
        selectedAdjustment,
        "warehouseId",
        "WarehouseId"
        );
        const loadedZones = await loadZones(warehouseId);
        const binId = Number(getValue(line, "binId", "BinId"));

        // Find which zone contains the existing bin so the edit form can select it.
        for (const zone of loadedZones) {
        const zoneId = getValue(zone, "zoneId", "ZoneId");
        try {
            const response = await api.get(
            `/warehouses/${warehouseId}/zones/${zoneId}/bins`
            );
            const zoneBins = getArray(response);
            if (zoneBins.some((bin) => Number(getValue(bin, "binId", "BinId")) === binId)) {
            setBins(zoneBins);
            setSelectedZoneId(String(zoneId));
            break;
            }
        } catch (err) {
            console.error("Bin lookup error:", err.response?.data || err);
        }
        }

        setShowLineModal(true);
    };

    const saveLine = async (e) => {
        e.preventDefault();
        setError("");
        setSuccess("");

        const adjustmentId = getValue(
        selectedAdjustment,
        "adjustmentId",
        "AdjustmentId"
        );

        const payload = {
        lotId: Number(lineForm.lotId),
        binId: Number(lineForm.binId),
        countedQuantity: Number(lineForm.countedQuantity),
        };

        if (!payload.lotId || !payload.binId || payload.countedQuantity < 0) {
        setError("Please select a lot and bin and enter a valid counted quantity.");
        return;
        }

        try {
        if (editingLine) {
            const lineId = getValue(editingLine, "adjustmentLineId", "AdjustmentLineId");

            await api.put(
            `/stock-adjustments/${adjustmentId}/lines/${lineId}`,
            payload
            );
            setSuccess("Adjustment line updated successfully.");
        } else {
            await api.post(`/stock-adjustments/${adjustmentId}/lines`, payload);
            setSuccess("Adjustment line added successfully.");
        }

        setShowLineModal(false);
        await openDetails(adjustmentId);
        await loadAdjustments();
        } catch (err) {
        setError(
            err.response?.data?.message ||
            err.response?.data ||
            "Failed to save adjustment line."
        );
        }
    };

    const postAdjustment = async () => {
        if (!selectedAdjustment) return;

        const adjustmentId = getValue(
        selectedAdjustment,
        "adjustmentId",
        "AdjustmentId"
        );

        const adjustmentNumber = getValue(
        selectedAdjustment,
        "adjustmentNumber",
        "AdjustmentNumber"
        );

        const confirmed = window.confirm(
        `Are you sure you want to post ${adjustmentNumber}?\n\nThis will permanently update inventory quantities.`
        );

        if (!confirmed) return;

        setError("");
        setSuccess("");

        try {
        await api.post(`/stock-adjustments/${adjustmentId}/post`, {});
        setSuccess("Stock adjustment posted successfully.");
        await openDetails(adjustmentId);
        await loadAdjustments();
        } catch (err) {
        setError(
            err.response?.data?.message ||
            err.response?.data ||
            "Failed to post stock adjustment."
        );
        }
    };

    const getSelectedLot = () => {
        return lots.find(
        (lot) =>
            Number(getValue(lot, "lotId", "LotId")) === Number(lineForm.lotId)
        );
    };

    const getSelectedBin = () => {
        return bins.find(
        (bin) =>
            Number(getValue(bin, "binId", "BinId")) === Number(lineForm.binId)
        );
    };

    const getSystemQuantity = () => {
        const lot = getSelectedLot();
        const bin = getSelectedBin();

        if (!lot || !bin) return null;

        const lotId = Number(getValue(lot, "lotId", "LotId"));
        const binId = Number(getValue(bin, "binId", "BinId"));

        const balances = getValue(lot, "balances", "Balances");

        if (Array.isArray(balances)) {
        const balance = balances.find(
            (item) =>
            Number(getValue(item, "binId", "BinId")) === binId &&
            String(getValue(item, "stockStatus", "StockStatus")).toUpperCase() ===
                "AVAILABLE"
        );

        return Number(getValue(balance, "quantityOnHand", "QuantityOnHand") || 0);
        }

        // If the lot endpoint does not include balances, the backend remains
        // responsible for calculating SystemQuantity when the line is saved.
        return null;
    };

    const systemQuantity = getSystemQuantity();
    const countedQuantity =
        lineForm.countedQuantity === "" ? null : Number(lineForm.countedQuantity);

    const difference =
        systemQuantity !== null && countedQuantity !== null
        ? countedQuantity - systemQuantity
        : null;

    const adjustmentLines = getValue(
        selectedAdjustment,
        "lines",
        "Lines"
    ) || [];

    const filteredBins = bins.filter((bin) =>
        Boolean(getValue(bin, "isActive", "IsActive"))
    );

    const filteredAdjustments =
        filterStatus === "ALL"
        ? adjustments
        : adjustments.filter(
            (item) =>
                String(getValue(item, "status", "Status")).toUpperCase() ===
                filterStatus
            );

    const formatNumber = (value) =>
        Number(value || 0).toLocaleString(undefined, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
        });

    const closeDetails = () => {
        setShowDetailsModal(false);
        setSelectedAdjustment(null);
    };

    return (
        <div className="stock-adjustment-page">
        <div className="stock-adjustment-header">
            <div>
            <h1>Stock Adjustments</h1>
            <p>Manage inventory corrections and stock discrepancies.</p>
            </div>

            <button
            className="stock-adjustment-primary-btn"
            onClick={openCreateModal}
            >
            + Create Adjustment
            </button>
        </div>

        {error && (
            <div className="stock-adjustment-alert error">
            {typeof error === "string" ? error : "Something went wrong."}
            <button onClick={() => setError("")}>×</button>
            </div>
        )}

        {success && (
            <div className="stock-adjustment-alert success">
            {success}
            <button onClick={() => setSuccess("")}>×</button>
            </div>
        )}

        <div className="stock-adjustment-toolbar">
            <div>
            <label>Adjustment Status</label>
            <select
                value={filterStatus}
                onChange={(e) => setFilterStatus(e.target.value)}
            >
                <option value="ALL">All Adjustments</option>
                <option value="DRAFT">Draft</option>
                <option value="POSTED">Posted</option>
            </select>
            </div>

            <button
            className="stock-adjustment-secondary-btn"
            onClick={loadAdjustments}
            >
            Refresh
            </button>
        </div>

        <div className="stock-adjustment-table-container">
            <table className="stock-adjustment-table">
            <thead>
                <tr>
                <th>Adjustment Number</th>
                <th>Warehouse</th>
                <th>Reason</th>
                <th>Status</th>
                <th>Created At</th>
                <th>Action</th>
                </tr>
            </thead>

            <tbody>
                {loading ? (
                <tr>
                    <td colSpan="6" className="stock-adjustment-empty">
                    Loading adjustments...
                    </td>
                </tr>
                ) : filteredAdjustments.length === 0 ? (
                <tr>
                    <td colSpan="6" className="stock-adjustment-empty">
                    No stock adjustments found.
                    </td>
                </tr>
                ) : (
                filteredAdjustments.map((item) => {
                    const id = getValue(item, "adjustmentId", "AdjustmentId");
                    const status = getValue(item, "status", "Status");

                    return (
                    <tr key={id}>
                        <td className="stock-adjustment-number">
                        {getValue(item, "adjustmentNumber", "AdjustmentNumber")}
                        </td>
                        <td>
                        {getValue(item, "warehouseName", "WarehouseName") ||
                            getValue(item, "warehouseId", "WarehouseId")}
                        </td>
                        <td>
                        {getValue(item, "adjustmentReason", "AdjustmentReason")}
                        </td>
                        <td>
                        <span
                            className={`stock-adjustment-status ${String(
                            status
                            ).toLowerCase()}`}
                        >
                            {status}
                        </span>
                        </td>
                        <td>
                        {getValue(item, "createdAt", "CreatedAt")
                            ? new Date(
                                getValue(item, "createdAt", "CreatedAt")
                            ).toLocaleDateString()
                            : "-"}
                        </td>
                        <td>
                        <button
                            className="stock-adjustment-view-btn"
                            onClick={() => openDetails(id)}
                        >
                            View Details
                        </button>
                        </td>
                    </tr>
                    );
                })
                )}
            </tbody>
            </table>
        </div>

        {/* CREATE ADJUSTMENT MODAL */}
        {showCreateModal && (
            <div className="stock-adjustment-overlay">
            <div className="stock-adjustment-modal">
                <div className="stock-adjustment-modal-header">
                <div>
                    <h2>Create Stock Adjustment</h2>
                    <p>Enter the adjustment details.</p>
                </div>
                <button onClick={() => setShowCreateModal(false)}>×</button>
                </div>

                <form onSubmit={createAdjustment}>
                <div className="stock-adjustment-form-group">
                    <label>Adjustment Number *</label>
                    <input
                    type="text"
                    value={createForm.adjustmentNumber}
                    onChange={(e) =>
                        setCreateForm({
                        ...createForm,
                        adjustmentNumber: e.target.value,
                        })
                    }
                    placeholder="e.g. ADJ-2026-001"
                    required
                    />
                </div>

                <div className="stock-adjustment-form-group">
                    <label>Warehouse *</label>
                    <select
                    value={createForm.warehouseId}
                    onChange={(e) =>
                        setCreateForm({
                        ...createForm,
                        warehouseId: e.target.value,
                        })
                    }
                    required
                    >
                    <option value="">Select warehouse</option>
                    {warehouses.map((warehouse) => (
                        <option
                        key={getValue(warehouse, "warehouseId", "WarehouseId")}
                        value={getValue(warehouse, "warehouseId", "WarehouseId")}
                        >
                        {getValue(warehouse, "warehouseName", "WarehouseName") ||
                            getValue(warehouse, "warehouseCode", "WarehouseCode")}
                        </option>
                    ))}
                    </select>
                </div>

                <div className="stock-adjustment-form-group">
                    <label>Adjustment Reason *</label>
                    <textarea
                    value={createForm.adjustmentReason}
                    onChange={(e) =>
                        setCreateForm({
                        ...createForm,
                        adjustmentReason: e.target.value,
                        })
                    }
                    placeholder="Explain why this adjustment is needed..."
                    required
                    />
                </div>

                <div className="stock-adjustment-modal-actions">
                    <button
                    type="button"
                    className="stock-adjustment-cancel-btn"
                    onClick={() => setShowCreateModal(false)}
                    >
                    Cancel
                    </button>
                    <button
                    type="submit"
                    className="stock-adjustment-primary-btn"
                    >
                    Create Adjustment
                    </button>
                </div>
                </form>
            </div>
            </div>
        )}

        {/* ADJUSTMENT DETAILS MODAL */}
        {showDetailsModal && selectedAdjustment && (
            <div className="stock-adjustment-overlay">
            <div className="stock-adjustment-modal large">
                <div className="stock-adjustment-modal-header">
                <div>
                    <h2>
                    {getValue(
                        selectedAdjustment,
                        "adjustmentNumber",
                        "AdjustmentNumber"
                    )}
                    </h2>
                    <p>
                    {getValue(
                        selectedAdjustment,
                        "adjustmentReason",
                        "AdjustmentReason"
                    )}
                    </p>
                </div>
                <button onClick={closeDetails}>×</button>
                </div>

                <div className="stock-adjustment-details-grid">
                <div>
                    <span>Warehouse</span>
                    <strong>
                    {getValue(
                        selectedAdjustment,
                        "warehouseName",
                        "WarehouseName"
                    ) ||
                        getValue(
                        selectedAdjustment,
                        "warehouseId",
                        "WarehouseId"
                        )}
                    </strong>
                </div>
                <div>
                    <span>Status</span>
                    <strong>
                    <span
                        className={`stock-adjustment-status ${String(
                        getValue(selectedAdjustment, "status", "Status")
                        ).toLowerCase()}`}
                    >
                        {getValue(selectedAdjustment, "status", "Status")}
                    </span>
                    </strong>
                </div>
                <div>
                    <span>Created At</span>
                    <strong>
                    {getValue(selectedAdjustment, "createdAt", "CreatedAt")
                        ? new Date(
                            getValue(selectedAdjustment, "createdAt", "CreatedAt")
                        ).toLocaleString()
                        : "-"}
                    </strong>
                </div>
                </div>

                <div className="stock-adjustment-lines-header">
                <h3>Adjustment Lines</h3>

                {String(
                    getValue(selectedAdjustment, "status", "Status")
                ).toUpperCase() === "DRAFT" && (
                    <button
                    className="stock-adjustment-primary-btn"
                    onClick={openAddLine}
                    >
                    + Add Line
                    </button>
                )}
                </div>

                <div className="stock-adjustment-table-container">
                <table className="stock-adjustment-table">
                    <thead>
                    <tr>
                        <th>Item</th>
                        <th>Batch</th>
                        <th>Bin</th>
                        <th>System Qty</th>
                        <th>Counted Qty</th>
                        <th>Difference</th>
                        {String(
                        getValue(selectedAdjustment, "status", "Status")
                        ).toUpperCase() === "DRAFT" && <th>Action</th>}
                    </tr>
                    </thead>

                    <tbody>
                    {adjustmentLines.length === 0 ? (
                        <tr>
                        <td
                            colSpan="7"
                            className="stock-adjustment-empty"
                        >
                            No adjustment lines added.
                        </td>
                        </tr>
                    ) : (
                        adjustmentLines.map((line) => {
                        const lineId = getValue(
                            line,
                            "adjustmentLineId",
                            "AdjustmentLineId"
                        );
                        const difference = Number(
                            getValue(
                            line,
                            "differenceQuantity",
                            "DifferenceQuantity"
                            )
                        );

                        return (
                            <tr key={lineId}>
                            <td>
                                {getValue(line, "itemCode", "ItemCode") ||
                                getValue(line, "itemName", "ItemName") ||
                                getValue(line, "itemId", "ItemId")}
                            </td>
                            <td>
                                {getValue(line, "batchCode", "BatchCode") ||
                                getValue(line, "lotId", "LotId")}
                            </td>
                            <td>
                                {getValue(line, "binCode", "BinCode") ||
                                getValue(line, "binId", "BinId")}
                            </td>
                            <td>
                                {formatNumber(
                                getValue(line, "systemQuantity", "SystemQuantity")
                                )}
                            </td>
                            <td>
                                {formatNumber(
                                getValue(
                                    line,
                                    "countedQuantity",
                                    "CountedQuantity"
                                )
                                )}
                            </td>
                            <td>
                                <span
                                className={
                                    difference > 0
                                    ? "stock-adjustment-positive"
                                    : difference < 0
                                    ? "stock-adjustment-negative"
                                    : "stock-adjustment-neutral"
                                }
                                >
                                {difference > 0 ? "+" : ""}
                                {formatNumber(difference)}
                                </span>
                            </td>
                            {String(
                                getValue(selectedAdjustment, "status", "Status")
                            ).toUpperCase() === "DRAFT" && (
                                <td>
                                <button
                                    className="stock-adjustment-view-btn"
                                    onClick={() => openEditLine(line)}
                                >
                                    Edit
                                </button>
                                </td>
                            )}
                            </tr>
                        );
                        })
                    )}
                    </tbody>
                </table>
                </div>

                <div className="stock-adjustment-modal-actions">
                <button
                    className="stock-adjustment-cancel-btn"
                    onClick={closeDetails}
                >
                    Close
                </button>

                {String(
                    getValue(selectedAdjustment, "status", "Status")
                ).toUpperCase() === "DRAFT" &&
                    adjustmentLines.length > 0 && (
                    <button
                        className="stock-adjustment-post-btn"
                        onClick={postAdjustment}
                    >
                        Post Adjustment
                    </button>
                    )}
                </div>
            </div>
            </div>
        )}

        {/* ADD / EDIT LINE MODAL */}
        {showLineModal && (
            <div className="stock-adjustment-overlay nested">
            <div className="stock-adjustment-modal">
                <div className="stock-adjustment-modal-header">
                <div>
                    <h2>{editingLine ? "Edit Adjustment Line" : "Add Adjustment Line"}</h2>
                    <p>Select a batch and storage bin, then enter the physical count.</p>
                </div>
                <button onClick={() => setShowLineModal(false)}>×</button>
                </div>

                <form onSubmit={saveLine}>
                <div className="stock-adjustment-form-group">
                    <label>Inventory Lot / Batch *</label>
                    <select
                    value={lineForm.lotId}
                    onChange={(e) =>
                        setLineForm({ ...lineForm, lotId: e.target.value })
                    }
                    required
                    >
                    <option value="">Select lot</option>
                    {lots.map((lot) => (
                        <option
                        key={getValue(lot, "lotId", "LotId")}
                        value={getValue(lot, "lotId", "LotId")}
                        >
                        {getValue(lot, "batchCode", "BatchCode")} -
                        {" "}
                        {getValue(lot, "itemCode", "ItemCode") ||
                            getValue(lot, "itemId", "ItemId")}
                        </option>
                    ))}
                    </select>
                </div>

                <div className="stock-adjustment-form-group">
                    <label>Warehouse Zone *</label>
                    <select
                    value={selectedZoneId}
                    onChange={(e) => {
                        const zoneId = e.target.value;
                        setLineForm({ ...lineForm, binId: "" });
                        loadBins(
                        getValue(selectedAdjustment, "warehouseId", "WarehouseId"),
                        zoneId
                        );
                    }}
                    required
                    >
                    <option value="">Select zone</option>
                    {zones.map((zone) => (
                        <option
                        key={getValue(zone, "zoneId", "ZoneId")}
                        value={getValue(zone, "zoneId", "ZoneId")}
                        >
                        {getValue(zone, "zoneCode", "ZoneCode")} - {getValue(zone, "zoneName", "ZoneName")}
                        </option>
                    ))}
                    </select>
                </div>

                <div className="stock-adjustment-form-group">
                    <label>Storage Bin *</label>
                    <select
                    value={lineForm.binId}
                    onChange={(e) =>
                        setLineForm({ ...lineForm, binId: e.target.value })
                    }
                    disabled={!selectedZoneId}
                    required
                    >
                    <option value="">Select bin</option>
                    {filteredBins.map((bin) => (
                        <option
                        key={getValue(bin, "binId", "BinId")}
                        value={getValue(bin, "binId", "BinId")}
                        >
                        {getValue(bin, "binCode", "BinCode")}
                        </option>
                    ))}
                    </select>
                </div>

                <div className="stock-adjustment-form-group">
                    <label>Counted Quantity *</label>
                    <input
                    type="number"
                    min="0"
                    step="0.000001"
                    value={lineForm.countedQuantity}
                    onChange={(e) =>
                        setLineForm({
                        ...lineForm,
                        countedQuantity: e.target.value,
                        })
                    }
                    placeholder="Enter physical count"
                    required
                    />
                </div>

                <div className="stock-adjustment-calculation">
                    <div>
                    <span>System Quantity</span>
                    <strong>
                        {systemQuantity === null
                        ? "Provided by server"
                        : formatNumber(systemQuantity)}
                    </strong>
                    </div>
                    <div>
                    <span>Counted Quantity</span>
                    <strong>
                        {countedQuantity === null
                        ? "-"
                        : formatNumber(countedQuantity)}
                    </strong>
                    </div>
                    <div>
                    <span>Difference</span>
                    <strong
                        className={
                        difference > 0
                            ? "stock-adjustment-positive"
                            : difference < 0
                            ? "stock-adjustment-negative"
                            : ""
                        }
                    >
                        {difference === null
                        ? "Calculated by server"
                        : `${difference > 0 ? "+" : ""}${formatNumber(
                            difference
                            )}`}
                    </strong>
                    </div>
                </div>

                <p className="stock-adjustment-note">
                    The backend will calculate and validate the actual system
                    quantity when you save the line. Posting will recheck stock
                    to prevent applying an outdated count.
                </p>

                <div className="stock-adjustment-modal-actions">
                    <button
                    type="button"
                    className="stock-adjustment-cancel-btn"
                    onClick={() => setShowLineModal(false)}
                    >
                    Cancel
                    </button>
                    <button
                    type="submit"
                    className="stock-adjustment-primary-btn"
                    >
                    {editingLine ? "Save Changes" : "Add Line"}
                    </button>
                </div>
                </form>
            </div>
            </div>
        )}
        </div>
    );
    };

    export default StockAdjustments;
