
import { useEffect, useMemo, useState } from 'react';
import api from '../../services/api';
import './productionreports.css';

const emptyForm = {
    productionOrderId: '',
    producedQuantity: '',
    rejectedQuantity: '0',
    productionDate: new Date().toISOString().slice(0, 10),
    destinationBinId: '',
    remarks: ''
};

const formatNumber = (value) =>
    Number(value || 0).toLocaleString(undefined, {
        maximumFractionDigits: 3
    });

const getOrderId = (order) =>
    order.productionOrderId ?? order.id;

const getOrderNumber = (order) =>
    order.productionNumber ?? `PO-${getOrderId(order)}`;

export default function ProductionReportsPage() {
    const [reports, setReports] = useState([]);
    const [orders, setOrders] = useState([]);
    const [bins, setBins] = useState([]);

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState('');
    const [modalOpen, setModalOpen] = useState(false);

    const [search, setSearch] = useState('');
    const [statusFilter, setStatusFilter] = useState('ALL');

    const [form, setForm] = useState(emptyForm);

    useEffect(() => {
        loadData();
    }, []);

    async function loadData() {
        setLoading(true);
        setError('');

        try {
            const [reportsRes, ordersRes, binsRes] =
                await Promise.all([
                    api.get('/production-reports'),
                    api.get('/production-orders'),
                    api.get('/storage-bins')
                ]);

            setReports(Array.isArray(reportsRes.data)
                ? reportsRes.data
                : []);

            setOrders(Array.isArray(ordersRes.data)
                ? ordersRes.data
                : []);

            setBins(Array.isArray(binsRes.data)
                ? binsRes.data
                : []);
        } catch (err) {
            console.error('Failed to load production reports:', err);

            setError(
                err.response?.data?.message ||
                'Failed to load production reporting data.'
            );
        } finally {
            setLoading(false);
        }
    }

    const filteredReports = useMemo(() => {
        return reports.filter((report) => {
            const orderNumber =
                report.productionNumber ||
                report.productionOrder?.productionNumber ||
                '';

            const reportNumber =
                report.reportNumber ||
                report.outputId ||
                '';

            const searchMatch =
                `${orderNumber} ${reportNumber}`
                    .toLowerCase()
                    .includes(search.toLowerCase());

            const status =
                report.reportStatus ||
                report.postingStatus ||
                report.qualityStatus ||
                'PENDING';

            const statusMatch =
                statusFilter === 'ALL' ||
                status.toUpperCase() === statusFilter;

            return searchMatch && statusMatch;
        });
    }, [reports, search, statusFilter]);

    const summary = useMemo(() => {
        const totalProduced = reports.reduce(
            (sum, report) =>
                sum + Number(report.producedQuantity || 0),
            0
        );

        const totalRejected = reports.reduce(
            (sum, report) =>
                sum + Number(report.rejectedQuantity || 0),
            0
        );

        const postedCount = reports.filter(
            (report) =>
                (report.reportStatus ||
                    report.postingStatus ||
                    '').toUpperCase() === 'POSTED'
        ).length;

        const pendingCount = reports.filter(
            (report) =>
                (report.reportStatus ||
                    report.postingStatus ||
                    report.qualityStatus ||
                    'PENDING').toUpperCase() !== 'POSTED'
        ).length;

        return {
            totalReports: reports.length,
            totalProduced,
            totalRejected,
            postedCount,
            pendingCount
        };
    }, [reports]);

    const selectedOrder = useMemo(() => {
        return orders.find(
            (order) =>
                String(getOrderId(order)) ===
                String(form.productionOrderId)
        );
    }, [orders, form.productionOrderId]);

    function openModal() {
        setForm({
            ...emptyForm,
            productionDate: new Date().toISOString().slice(0, 10)
        });

        setError('');
        setModalOpen(true);
    }

    function closeModal() {
        if (saving) return;

        setModalOpen(false);
        setForm(emptyForm);
    }

    function handleChange(e) {
        const { name, value } = e.target;

        setForm((previous) => ({
            ...previous,
            [name]: value
        }));
    }

    async function handleSubmit(e) {
        e.preventDefault();
        setError('');

        const produced = Number(form.producedQuantity);
        const rejected = Number(form.rejectedQuantity);

        if (!form.productionOrderId) {
            setError('Please select a production order.');
            return;
        }

        if (!form.destinationBinId) {
            setError('Please select a destination bin.');
            return;
        }

        if (!form.productionDate) {
            setError('Production date is required.');
            return;
        }

        if (!Number.isFinite(produced) || produced <= 0) {
            setError('Produced quantity must be greater than zero.');
            return;
        }

        if (!Number.isFinite(rejected) || rejected < 0) {
            setError('Rejected quantity cannot be negative.');
            return;
        }

        const totalOutput = produced + rejected;

        if (
            selectedOrder?.plannedQuantity != null &&
            totalOutput >
                Number(selectedOrder.plannedQuantity)
        ) {
            setError(
                'Produced and rejected quantities exceed the planned quantity.'
            );
            return;
        }

        const payload = {
            productionOrderId: Number(form.productionOrderId),
            producedQuantity: produced,
            rejectedQuantity: rejected,
            productionDate: form.productionDate,
            destinationBinId: Number(form.destinationBinId),
            remarks: form.remarks.trim()
        };

        setSaving(true);

        try {
            await api.post('/production-reports', payload);

            setModalOpen(false);
            setForm(emptyForm);

            await loadData();
        } catch (err) {
            console.error('Failed to save production report:', err);

            setError(
                err.response?.data?.message ||
                'Failed to save production report.'
            );
        } finally {
            setSaving(false);
        }
    }

    function getStatus(report) {
        return (
            report.reportStatus ||
            report.postingStatus ||
            report.qualityStatus ||
            'PENDING'
        ).toUpperCase();
    }

    function getStatusClass(status) {
        switch (status) {
            case 'POSTED':
            case 'APPROVED':
                return 'pr-status-posted';

            case 'REJECTED':
            case 'CANCELLED':
                return 'pr-status-rejected';

            case 'DRAFT':
                return 'pr-status-draft';

            default:
                return 'pr-status-pending';
        }
    }

    function getReportNumber(report) {
        return (
            report.reportNumber ||
            `PR-${report.outputId || report.productionOutputId || report.id || '-'}`
        );
    }

    function getProductionNumber(report) {
        return (
            report.productionNumber ||
            report.productionOrder?.productionNumber ||
            '-'
        );
    }

    function getDate(report) {
        const date =
            report.productionDate ||
            report.createdAt;

        if (!date) return '-';

        return new Date(date).toLocaleDateString();
    }

    function getBinCode(report) {
        return (
            report.destinationBin?.binCode ||
            report.binCode ||
            report.finishedLot?.binCode ||
            report.destinationBinCode ||
            '-'
        );
    }

    return (
        <div className="pr-page">

            {/* PAGE HEADER */}
            <div className="pr-header">
                <div>
                    <span className="pr-eyebrow">
                        MANUFACTURING
                    </span>

                    <h1>Production Reporting</h1>

                    <p>
                        Record production output, track rejects,
                        and report finished goods.
                    </p>
                </div>

                <div className="pr-header-actions">
                    <button
                        type="button"
                        className="pr-refresh-btn"
                        onClick={loadData}
                        disabled={loading}
                    >
                        ↻ Refresh
                    </button>

                    <button
                        type="button"
                        className="pr-primary-btn"
                        onClick={openModal}
                    >
                        + New Production Report
                    </button>
                </div>
            </div>

            {/* ERROR */}
            {error && !modalOpen && (
                <div className="pr-alert">
                    {error}
                </div>
            )}

            {/* SUMMARY CARDS */}
            <div className="pr-summary-grid">

                <div className="pr-summary-card">
                    <span className="pr-summary-label">
                        Total Reports
                    </span>

                    <strong>
                        {summary.totalReports}
                    </strong>

                    <small>Recorded reports</small>
                </div>

                <div className="pr-summary-card">
                    <span className="pr-summary-label">
                        Total Produced
                    </span>

                    <strong className="pr-value-green">
                        {formatNumber(summary.totalProduced)}
                    </strong>

                    <small>Good output quantity</small>
                </div>

                <div className="pr-summary-card">
                    <span className="pr-summary-label">
                        Total Rejected
                    </span>

                    <strong className="pr-value-red">
                        {formatNumber(summary.totalRejected)}
                    </strong>

                    <small>Reported rejected quantity</small>
                </div>

                <div className="pr-summary-card">
                    <span className="pr-summary-label">
                        Pending Reports
                    </span>

                    <strong className="pr-value-orange">
                        {summary.pendingCount}
                    </strong>

                    <small>
                        {summary.postedCount} posted
                    </small>
                </div>

            </div>

            {/* REPORT TABLE */}
            <div className="pr-panel">

                <div className="pr-toolbar">

                    <div className="pr-search">
                        <input
                            type="text"
                            placeholder="Search production order or report..."
                            value={search}
                            onChange={(e) =>
                                setSearch(e.target.value)
                            }
                        />
                    </div>

                    <select
                        value={statusFilter}
                        onChange={(e) =>
                            setStatusFilter(e.target.value)
                        }
                    >
                        <option value="ALL">All Statuses</option>
                        <option value="POSTED">Posted</option>
                        <option value="PENDING">Pending</option>
                        <option value="DRAFT">Draft</option>
                        <option value="REJECTED">Rejected</option>
                    </select>

                </div>

                <div className="pr-table-wrapper">

                    <table className="pr-table">
                        <thead>
                            <tr>
                                <th>Report No.</th>
                                <th>Production Order</th>
                                <th>Production Date</th>
                                <th>Produced</th>
                                <th>Rejected</th>
                                <th>Destination Bin</th>
                                <th>Status</th>
                            </tr>
                        </thead>

                        <tbody>
                            {loading ? (
                                <tr>
                                    <td
                                        colSpan="7"
                                        className="pr-empty"
                                    >
                                        Loading production reports...
                                    </td>
                                </tr>
                            ) : filteredReports.length === 0 ? (
                                <tr>
                                    <td
                                        colSpan="7"
                                        className="pr-empty"
                                    >
                                        No production reports found.
                                    </td>
                                </tr>
                            ) : (
                                filteredReports.map((report) => {
                                    const status = getStatus(report);

                                    return (
                                        <tr
                                            key={
                                                report.outputId ||
                                                report.productionOutputId ||
                                                report.id
                                            }
                                        >
                                            <td>
                                                <strong>
                                                    {getReportNumber(report)}
                                                </strong>
                                            </td>

                                            <td>
                                                {getProductionNumber(report)}
                                            </td>

                                            <td>
                                                {getDate(report)}
                                            </td>

                                            <td className="pr-number pr-produced">
                                                {formatNumber(
                                                    report.producedQuantity
                                                )}
                                            </td>

                                            <td className="pr-number pr-rejected">
                                                {formatNumber(
                                                    report.rejectedQuantity
                                                )}
                                            </td>

                                            <td>
                                                {getBinCode(report)}
                                            </td>

                                            <td>
                                                <span
                                                    className={`pr-status ${getStatusClass(status)}`}
                                                >
                                                    {status}
                                                </span>
                                            </td>
                                        </tr>
                                    );
                                })
                            )}
                        </tbody>
                    </table>

                </div>
            </div>

            {/* =================================================
                MODAL
            ================================================= */}
            {modalOpen && (
                <div
                    className="pr-modal-backdrop"
                    onMouseDown={(e) => {
                        if (e.target === e.currentTarget) {
                            closeModal();
                        }
                    }}
                >
                    <div
                        className="pr-modal"
                        role="dialog"
                        aria-modal="true"
                        aria-labelledby="pr-modal-title"
                    >

                        {/* MODAL HEADER */}
                        <div className="pr-modal-header">
                            <div>
                                <span className="pr-eyebrow">
                                    MANUFACTURING
                                </span>

                                <h2 id="pr-modal-title">
                                    New Production Report
                                </h2>

                                <p>
                                    Record output for a production order.
                                </p>
                            </div>

                            <button
                                type="button"
                                className="pr-modal-close"
                                onClick={closeModal}
                                disabled={saving}
                                aria-label="Close modal"
                            >
                                ×
                            </button>
                        </div>

                        {/* MODAL BODY */}
                        <form
                            className="pr-modal-form"
                            onSubmit={handleSubmit}
                        >

                            <div className="pr-modal-body">

                                {error && (
                                    <div className="pr-alert">
                                        {error}
                                    </div>
                                )}

                                {/* PRODUCTION ORDER */}
                                <div className="pr-form-group">
                                    <label htmlFor="productionOrderId">
                                        Production Order
                                        <span className="pr-required">
                                            *
                                        </span>
                                    </label>

                                    <select
                                        id="productionOrderId"
                                        name="productionOrderId"
                                        value={form.productionOrderId}
                                        onChange={handleChange}
                                        required
                                    >
                                        <option value="">
                                            Select production order
                                        </option>

                                        {orders
                                            .filter((order) => {
                                                const status = (
                                                    order.productionStatus ||
                                                    order.status ||
                                                    ''
                                                ).toUpperCase();

                                                return ![
                                                    'COMPLETED',
                                                    'CANCELLED'
                                                ].includes(status);
                                            })
                                            .map((order) => (
                                                <option
                                                    key={getOrderId(order)}
                                                    value={getOrderId(order)}
                                                >
                                                    {getOrderNumber(order)}
                                                    {' — '}
                                                    Planned:{' '}
                                                    {formatNumber(
                                                        order.plannedQuantity
                                                    )}
                                                </option>
                                            ))}
                                    </select>
                                </div>

                                {/* ORDER DETAILS */}
                                {selectedOrder && (
                                    <div className="pr-order-info">

                                        <div>
                                            <span>
                                                Planned Quantity
                                            </span>

                                            <strong>
                                                {formatNumber(
                                                    selectedOrder.plannedQuantity
                                                )}
                                            </strong>
                                        </div>

                                        <div>
                                            <span>
                                                Order Status
                                            </span>

                                            <strong>
                                                {selectedOrder.productionStatus ||
                                                    selectedOrder.status ||
                                                    '-'}
                                            </strong>
                                        </div>

                                        {selectedOrder.finishedItem && (
                                            <div>
                                                <span>
                                                    Finished Item
                                                </span>

                                                <strong>
                                                    {selectedOrder.finishedItem.itemCode ||
                                                        selectedOrder.finishedItem.itemName ||
                                                        '-'}
                                                </strong>
                                            </div>
                                        )}
                                    </div>
                                )}

                                {/* QUANTITY FIELDS */}
                                <div className="pr-form-row">

                                    <div className="pr-form-group">
                                        <label htmlFor="producedQuantity">
                                            Produced Quantity
                                            <span className="pr-required">
                                                *
                                            </span>
                                        </label>

                                        <input
                                            type="number"
                                            id="producedQuantity"
                                            name="producedQuantity"
                                            value={form.producedQuantity}
                                            onChange={handleChange}
                                            min="0.000001"
                                            step="0.000001"
                                            required
                                            placeholder="0.00"
                                        />
                                    </div>

                                    <div className="pr-form-group">
                                        <label htmlFor="rejectedQuantity">
                                            Rejected Quantity
                                        </label>

                                        <input
                                            type="number"
                                            id="rejectedQuantity"
                                            name="rejectedQuantity"
                                            value={form.rejectedQuantity}
                                            onChange={handleChange}
                                            min="0"
                                            step="0.000001"
                                            placeholder="0.00"
                                        />
                                    </div>

                                </div>

                                {/* DATE */}
                                <div className="pr-form-group">
                                    <label htmlFor="productionDate">
                                        Production Date
                                        <span className="pr-required">
                                            *
                                        </span>
                                    </label>

                                    <input
                                        type="date"
                                        id="productionDate"
                                        name="productionDate"
                                        value={form.productionDate}
                                        onChange={handleChange}
                                        required
                                    />
                                </div>

                                {/* DESTINATION BIN */}
                                <div className="pr-form-group">
                                    <label htmlFor="destinationBinId">
                                        Finished Goods Destination Bin
                                        <span className="pr-required">
                                            *
                                        </span>
                                    </label>

                                    <select
                                        id="destinationBinId"
                                        name="destinationBinId"
                                        value={form.destinationBinId}
                                        onChange={handleChange}
                                        required
                                    >
                                        <option value="">
                                            Select destination bin
                                        </option>

                                        {bins.map((bin) => (
                                            <option
                                                key={bin.binId}
                                                value={bin.binId}
                                            >
                                                {bin.binCode}
                                                {' — '}
                                                {bin.zoneCode || 'Zone'}
                                                {' — Warehouse '}
                                                {bin.warehouseId}
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                {/* REMARKS */}
                                <div className="pr-form-group">
                                    <label htmlFor="remarks">
                                        Remarks
                                    </label>

                                    <textarea
                                        id="remarks"
                                        name="remarks"
                                        value={form.remarks}
                                        onChange={handleChange}
                                        rows="3"
                                        placeholder="Optional production notes..."
                                    />
                                </div>

                                {/* TOTAL */}
                                <div className="pr-total-output">
                                    <div>
                                        <span>Good Output</span>
                                        <strong>
                                            {formatNumber(
                                                form.producedQuantity
                                            )}
                                        </strong>
                                    </div>

                                    <div>
                                        <span>Rejected</span>
                                        <strong>
                                            {formatNumber(
                                                form.rejectedQuantity
                                            )}
                                        </strong>
                                    </div>

                                    <div>
                                        <span>Total Reported</span>
                                        <strong>
                                            {formatNumber(
                                                Number(
                                                    form.producedQuantity || 0
                                                ) +
                                                Number(
                                                    form.rejectedQuantity || 0
                                                )
                                            )}
                                        </strong>
                                    </div>
                                </div>

                            </div>

                            {/* MODAL FOOTER */}
                            <div className="pr-modal-footer">

                                <button
                                    type="button"
                                    className="pr-cancel-btn"
                                    onClick={closeModal}
                                    disabled={saving}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="submit"
                                    className="pr-primary-btn"
                                    disabled={saving}
                                >
                                    {saving
                                        ? 'Saving...'
                                        : 'Save Production Report'}
                                </button>

                            </div>

                        </form>
                    </div>
                </div>
            )}

        </div>
    );
}
