
import { useEffect, useState } from "react";
import api from "../../services/api";

export default function QualityInspections() {
    const [inspections, setInspections] = useState([]);
    const [lots, setLots] = useState([]);
    const [parameters, setParameters] = useState([]);

    const [selectedInspection, setSelectedInspection] = useState(null);

    const [showCreateForm, setShowCreateForm] = useState(false);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");

    const [createForm, setCreateForm] = useState({
        lotId: "",
        inspectionType: "RECEIVING",
        receiptId: "",
        productionOrderId: "",
        remarks: ""
    });

    const [lineForm, setLineForm] = useState({
        parameterId: "",
        measuredValue: "",
        minimumValue: "",
        maximumValue: "",
        remarks: ""
    });

    const [finalizeForm, setFinalizeForm] = useState({
        lotQualityStatus: "APPROVED",
        reason: "",
        remarks: ""
    });

    useEffect(() => {
        loadInitialData();
    }, []);

    const loadInitialData = async () => {
        try {
            setLoading(true);
            setError("");

            const [
                inspectionResponse,
                lotResponse,
                parameterResponse
            ] = await Promise.all([
                api.get("/quality-inspections"),
                api.get("/inventory-lots"),
                api.get("/quality-parameters")
            ]);

            setInspections(inspectionResponse.data);
            setLots(lotResponse.data);
            setParameters(parameterResponse.data);
        } catch (err) {
            setError(
                err.response?.data?.message ||
                "Unable to load quality inspection data."
            );
        } finally {
            setLoading(false);
        }
    };

    const loadInspectionDetails = async (id) => {
        try {
            setError("");
            setSuccess("");

            const response = await api.get(
                `/quality-inspections/${id}`
            );

            setSelectedInspection(response.data);
        } catch (err) {
            setError(
                err.response?.data?.message ||
                "Unable to load inspection details."
            );
        }
    };

    const handleCreateInspection = async (e) => {
        e.preventDefault();

        try {
            setError("");
            setSuccess("");

            const payload = {
                lotId: Number(createForm.lotId),
                inspectionType: createForm.inspectionType,
                receiptId: createForm.receiptId
                    ? Number(createForm.receiptId)
                    : null,
                productionOrderId: createForm.productionOrderId
                    ? Number(createForm.productionOrderId)
                    : null,
                remarks: createForm.remarks || null
            };

            const response = await api.post(
                "/quality-inspections",
                payload
            );

            setSuccess("Quality inspection created successfully.");
            setShowCreateForm(false);

            setCreateForm({
                lotId: "",
                inspectionType: "RECEIVING",
                receiptId: "",
                productionOrderId: "",
                remarks: ""
            });

            await loadInitialData();

            if (response.data.inspectionId) {
                await loadInspectionDetails(
                    response.data.inspectionId
                );
            }
        } catch (err) {
            setError(
                err.response?.data?.message ||
                "Unable to create quality inspection."
            );
        }
    };

    const handleAddLine = async (e) => {
        e.preventDefault();

        if (!selectedInspection) return;

        try {
            setError("");
            setSuccess("");

            const payload = {
                parameterId: Number(lineForm.parameterId),

                measuredValue: lineForm.measuredValue === ""
                    ? null
                    : Number(lineForm.measuredValue),

                minimumValue: lineForm.minimumValue === ""
                    ? null
                    : Number(lineForm.minimumValue),

                maximumValue: lineForm.maximumValue === ""
                    ? null
                    : Number(lineForm.maximumValue),

                remarks: lineForm.remarks || null
            };

            await api.post(
                `/quality-inspections/${selectedInspection.inspectionId}/lines`,
                payload
            );

            setSuccess(
                "Inspection measurement added successfully."
            );

            setLineForm({
                parameterId: "",
                measuredValue: "",
                minimumValue: "",
                maximumValue: "",
                remarks: ""
            });

            await loadInspectionDetails(
                selectedInspection.inspectionId
            );

            await loadInitialData();
        } catch (err) {
            setError(
                err.response?.data?.message ||
                "Unable to add inspection measurement."
            );
        }
    };

    const handleFinalize = async (e) => {
        e.preventDefault();

        if (!selectedInspection) return;

        const confirmed = window.confirm(
            "Are you sure you want to finalize this inspection? This action cannot be undone."
        );

        if (!confirmed) return;

        try {
            setError("");
            setSuccess("");

            const payload = {
                lotQualityStatus: finalizeForm.lotQualityStatus,
                reason: finalizeForm.reason || null,
                remarks: finalizeForm.remarks || null
            };

            await api.post(
                `/quality-inspections/${selectedInspection.inspectionId}/finalize`,
                payload
            );

            setSuccess(
                "Quality inspection finalized successfully."
            );

            await loadInspectionDetails(
                selectedInspection.inspectionId
            );

            await loadInitialData();
        } catch (err) {
            setError(
                err.response?.data?.message ||
                "Unable to finalize quality inspection."
            );
        }
    };

    const selectedParameter = parameters.find(
        p => String(p.parameterId) === String(lineForm.parameterId)
    );

    const formatDate = (date) => {
        if (!date) return "-";
        return new Date(date).toLocaleString();
    };

    const statusClass = (status) => {
        switch (status) {
            case "PASSED":
            case "APPROVED":
                return "status-approved";

            case "FAILED":
            case "REJECTED":
                return "status-rejected";

            case "PENDING":
                return "status-pending";

            case "QUARANTINED":
                return "status-quarantined";

            default:
                return "";
        }
    };

    return (
        <div className="quality-page">

            {/* PAGE HEADER */}

            <div className="quality-header">
                <div>
                    <h1>Quality Inspections</h1>
                    <p>
                        Inspect inventory lots and manage quality decisions.
                    </p>
                </div>

                <button
                    className="quality-primary-btn"
                    onClick={() => {
                        setShowCreateForm(true);
                        setError("");
                        setSuccess("");
                    }}
                >
                    + New Inspection
                </button>
            </div>

            {/* ALERT MESSAGES */}

            {error && (
                <div className="quality-alert error">
                    {error}
                </div>
            )}

            {success && (
                <div className="quality-alert success">
                    {success}
                </div>
            )}

            {/* CREATE INSPECTION MODAL */}

            {showCreateForm && (
                <div
                    className="quality-modal-backdrop"
                    onClick={() => setShowCreateForm(false)}
                >
                    <div
                        className="quality-modal"
                        onClick={(e) => e.stopPropagation()}
                    >
                        <div className="quality-modal-header">
                            <div>
                                <h2>Create Quality Inspection</h2>
                                <p>
                                    Record a new inventory quality inspection.
                                </p>
                            </div>

                            <button
                                type="button"
                                className="quality-modal-close"
                                onClick={() => setShowCreateForm(false)}
                            >
                                &times;
                            </button>
                        </div>

                        <form onSubmit={handleCreateInspection}>
                            <div className="quality-modal-body">
                                <div className="quality-form-grid">

                                    <div className="quality-field">
                                        <label>Inventory Lot *</label>

                                        <select
                                            value={createForm.lotId}
                                            onChange={(e) =>
                                                setCreateForm({
                                                    ...createForm,
                                                    lotId: e.target.value
                                                })
                                            }
                                            required
                                        >
                                            <option value="">
                                                Select inventory lot
                                            </option>

                                            {lots.map(lot => (
                                                <option
                                                    key={lot.lotId}
                                                    value={lot.lotId}
                                                >
                                                    {lot.itemCode} - {lot.itemName}
                                                    {" | "}
                                                    {lot.batchCode}
                                                    {" | "}
                                                    {lot.qualityStatus}
                                                </option>
                                            ))}
                                        </select>
                                    </div>

                                    <div className="quality-field">
                                        <label>Inspection Type *</label>

                                        <select
                                            value={createForm.inspectionType}
                                            onChange={(e) =>
                                                setCreateForm({
                                                    ...createForm,
                                                    inspectionType: e.target.value
                                                })
                                            }
                                        >
                                            <option value="RECEIVING">
                                                Receiving
                                            </option>

                                            <option value="IN_PROCESS">
                                                In Process
                                            </option>

                                            <option value="FINAL">
                                                Final
                                            </option>
                                        </select>
                                    </div>

                                    <div className="quality-field">
                                        <label>Goods Receipt ID</label>

                                        <input
                                            type="number"
                                            min="1"
                                            value={createForm.receiptId}
                                            onChange={(e) =>
                                                setCreateForm({
                                                    ...createForm,
                                                    receiptId: e.target.value
                                                })
                                            }
                                            placeholder="Optional"
                                        />
                                    </div>

                                    <div className="quality-field">
                                        <label>Production Order ID</label>

                                        <input
                                            type="number"
                                            min="1"
                                            value={createForm.productionOrderId}
                                            onChange={(e) =>
                                                setCreateForm({
                                                    ...createForm,
                                                    productionOrderId: e.target.value
                                                })
                                            }
                                            placeholder="Optional"
                                        />
                                    </div>

                                    <div className="quality-field full-width">
                                        <label>Remarks</label>

                                        <textarea
                                            value={createForm.remarks}
                                            onChange={(e) =>
                                                setCreateForm({
                                                    ...createForm,
                                                    remarks: e.target.value
                                                })
                                            }
                                            placeholder="Enter inspection remarks..."
                                        />
                                    </div>

                                </div>
                            </div>

                            <div className="quality-modal-footer">
                                <button
                                    type="button"
                                    className="quality-secondary-btn"
                                    onClick={() => setShowCreateForm(false)}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="submit"
                                    className="quality-primary-btn"
                                    disabled={loading}
                                >
                                    Create Inspection
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {/* INSPECTION RECORDS TABLE */}

            <div className="quality-panel">
                <div className="quality-panel-header">
                    <h2>Inspection Records</h2>

                    <button
                        className="quality-secondary-btn"
                        onClick={loadInitialData}
                    >
                        Refresh
                    </button>
                </div>

                {loading ? (
                    <p>Loading inspections...</p>
                ) : inspections.length === 0 ? (
                    <p className="quality-empty">
                        No inspection records found.
                    </p>
                ) : (
                    <div className="quality-table-wrapper">
                        <table className="quality-table">
                            <thead>
                                <tr>
                                    <th>ID</th>
                                    <th>Item</th>
                                    <th>Batch</th>
                                    <th>Type</th>
                                    <th>Inspection Date</th>
                                    <th>Result</th>
                                    <th>Action</th>
                                </tr>
                            </thead>

                            <tbody>
                                {inspections.map(inspection => (
                                    <tr key={inspection.inspectionId}>
                                        <td>
                                            QI-{inspection.inspectionId}
                                        </td>

                                        <td>
                                            <strong>
                                                {inspection.itemCode}
                                            </strong>
                                            <small>
                                                {inspection.itemName}
                                            </small>
                                        </td>

                                        <td>
                                            {inspection.lotBatchCode}
                                        </td>

                                        <td>
                                            {inspection.inspectionType}
                                        </td>

                                        <td>
                                            {formatDate(inspection.inspectionDate)}
                                        </td>

                                        <td>
                                            <span
                                                className={`quality-status ${statusClass(inspection.overallResult)}`}
                                            >
                                                {inspection.overallResult}
                                            </span>
                                        </td>

                                        <td>
                                            <button
                                                className="quality-view-btn"
                                                onClick={() =>
                                                    loadInspectionDetails(
                                                        inspection.inspectionId
                                                    )
                                                }
                                            >
                                                View
                                            </button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>

            {/* INSPECTION DETAILS MODAL */}

            {selectedInspection && (
                <div
                    className="quality-detail-backdrop"
                    onClick={() => setSelectedInspection(null)}
                >
                    <div
                        className="quality-detail-modal"
                        onClick={(e) => e.stopPropagation()}
                    >

                        {/* MODAL HEADER */}

                        <div className="quality-panel-header">
                            <div>
                                <h2>
                                    Inspection QI-{selectedInspection.inspectionId}
                                </h2>

                                <p>
                                    {selectedInspection.itemCode} -{" "}
                                    {selectedInspection.itemName}
                                </p>
                            </div>

                            <button
                                className="quality-secondary-btn"
                                onClick={() => setSelectedInspection(null)}
                            >
                                Close Details
                            </button>
                        </div>

                        {/* INSPECTION DETAILS */}

                        <div className="quality-details-grid">
                            <div>
                                <label>Batch Code</label>
                                <strong>
                                    {selectedInspection.lotBatchCode}
                                </strong>
                            </div>

                            <div>
                                <label>Inspection Type</label>
                                <strong>
                                    {selectedInspection.inspectionType}
                                </strong>
                            </div>

                            <div>
                                <label>Inspection Date</label>
                                <strong>
                                    {formatDate(selectedInspection.inspectionDate)}
                                </strong>
                            </div>

                            <div>
                                <label>Overall Result</label>

                                <span
                                    className={`quality-status ${statusClass(selectedInspection.overallResult)}`}
                                >
                                    {selectedInspection.overallResult}
                                </span>
                            </div>
                        </div>

                        {/* INSPECTION MEASUREMENTS */}

                        <h3>Inspection Measurements</h3>

                        {selectedInspection.lines?.length > 0 ? (
                            <div className="quality-table-wrapper">
                                <table className="quality-table">
                                    <thead>
                                        <tr>
                                            <th>Parameter</th>
                                            <th>Measured Value</th>
                                            <th>Minimum</th>
                                            <th>Maximum</th>
                                            <th>Unit</th>
                                            <th>Result</th>
                                            <th>Remarks</th>
                                        </tr>
                                    </thead>

                                    <tbody>
                                        {selectedInspection.lines.map(line => (
                                            <tr key={line.inspectionLineId}>
                                                <td>
                                                    <strong>
                                                        {line.parameterCode}
                                                    </strong>
                                                    <small>
                                                        {line.parameterName}
                                                    </small>
                                                </td>

                                                <td>
                                                    {line.measuredValue ?? "-"}
                                                </td>

                                                <td>
                                                    {line.minimumValue ?? "-"}
                                                </td>

                                                <td>
                                                    {line.maximumValue ?? "-"}
                                                </td>

                                                <td>
                                                    {line.unitOfMeasure || "-"}
                                                </td>

                                                <td>
                                                    <span
                                                        className={`quality-status ${
                                                            line.result === "PASS"
                                                                ? "status-approved"
                                                                : line.result === "FAIL"
                                                                    ? "status-rejected"
                                                                    : "status-pending"
                                                        }`}
                                                    >
                                                        {line.result}
                                                    </span>
                                                </td>

                                                <td>
                                                    {line.remarks || "-"}
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        ) : (
                            <p className="quality-empty">
                                No measurements have been recorded.
                            </p>
                        )}

                        {/* ADD MEASUREMENT AND FINALIZE */}

                        {selectedInspection.overallResult === "PENDING" && (
                            <>
                                <div className="quality-divider" />

                                <h3>Add Inspection Measurement</h3>

                                <form onSubmit={handleAddLine}>
                                    <div className="quality-form-grid">

                                        <div className="quality-field full-width">
                                            <label>
                                                Quality Parameter *
                                            </label>

                                            <select
                                                value={lineForm.parameterId}
                                                onChange={(e) =>
                                                    setLineForm({
                                                        ...lineForm,
                                                        parameterId: e.target.value
                                                    })
                                                }
                                                required
                                            >
                                                <option value="">
                                                    Select parameter
                                                </option>

                                                {parameters.map(parameter => (
                                                    <option
                                                        key={parameter.parameterId}
                                                        value={parameter.parameterId}
                                                    >
                                                        {parameter.parameterCode} -{" "}
                                                        {parameter.parameterName}
                                                    </option>
                                                ))}
                                            </select>
                                        </div>

                                        {selectedParameter && (
                                            <div className="quality-parameter-info">
                                                <strong>
                                                    {selectedParameter.parameterName}
                                                </strong>

                                                <span>
                                                    Unit:{" "}
                                                    {selectedParameter.unitOfMeasure || "Not specified"}
                                                </span>

                                                {selectedParameter.description && (
                                                    <p>
                                                        {selectedParameter.description}
                                                    </p>
                                                )}
                                            </div>
                                        )}

                                        <div className="quality-field">
                                            <label>Measured Value</label>

                                            <input
                                                type="number"
                                                step="any"
                                                value={lineForm.measuredValue}
                                                onChange={(e) =>
                                                    setLineForm({
                                                        ...lineForm,
                                                        measuredValue: e.target.value
                                                    })
                                                }
                                                placeholder="Leave blank if not applicable"
                                            />
                                        </div>

                                        <div className="quality-field">
                                            <label>Minimum Accepted Value</label>

                                            <input
                                                type="number"
                                                step="any"
                                                value={lineForm.minimumValue}
                                                onChange={(e) =>
                                                    setLineForm({
                                                        ...lineForm,
                                                        minimumValue: e.target.value
                                                    })
                                                }
                                            />
                                        </div>

                                        <div className="quality-field">
                                            <label>Maximum Accepted Value</label>

                                            <input
                                                type="number"
                                                step="any"
                                                value={lineForm.maximumValue}
                                                onChange={(e) =>
                                                    setLineForm({
                                                        ...lineForm,
                                                        maximumValue: e.target.value
                                                    })
                                                }
                                            />
                                        </div>

                                        <div className="quality-field full-width">
                                            <label>Remarks</label>

                                            <textarea
                                                value={lineForm.remarks}
                                                onChange={(e) =>
                                                    setLineForm({
                                                        ...lineForm,
                                                        remarks: e.target.value
                                                    })
                                                }
                                            />
                                        </div>

                                    </div>

                                    <button
                                        type="submit"
                                        className="quality-primary-btn"
                                    >
                                        Add Measurement
                                    </button>
                                </form>

                                <div className="quality-divider" />

                                <h3>Finalize Inspection</h3>

                                <form onSubmit={handleFinalize}>
                                    <div className="quality-form-grid">

                                        <div className="quality-field">
                                            <label>Lot Quality Status *</label>

                                            <select
                                                value={finalizeForm.lotQualityStatus}
                                                onChange={(e) =>
                                                    setFinalizeForm({
                                                        ...finalizeForm,
                                                        lotQualityStatus: e.target.value
                                                    })
                                                }
                                            >
                                                <option value="APPROVED">
                                                    Approved
                                                </option>

                                                <option value="REJECTED">
                                                    Rejected
                                                </option>

                                                <option value="QUARANTINED">
                                                    Quarantined
                                                </option>
                                            </select>
                                        </div>

                                        <div className="quality-field">
                                            <label>Reason</label>

                                            <input
                                                value={finalizeForm.reason}
                                                onChange={(e) =>
                                                    setFinalizeForm({
                                                        ...finalizeForm,
                                                        reason: e.target.value
                                                    })
                                                }
                                                placeholder="Reason for quality decision"
                                            />
                                        </div>

                                        <div className="quality-field full-width">
                                            <label>Final Remarks</label>

                                            <textarea
                                                value={finalizeForm.remarks}
                                                onChange={(e) =>
                                                    setFinalizeForm({
                                                        ...finalizeForm,
                                                        remarks: e.target.value
                                                    })
                                                }
                                            />
                                        </div>

                                    </div>

                                    <button
                                        type="submit"
                                        className="quality-finalize-btn"
                                    >
                                        Finalize Inspection
                                    </button>
                                </form>
                            </>
                        )}

                    </div>
                </div>
            )}

        </div>
    );
}
