import { useEffect, useMemo, useState } from 'react'
import api from '../../services/api'

const getValue = (obj, name) => obj?.[name] ?? obj?.[name.charAt(0).toLowerCase() + name.slice(1)];
const getArray = (data) => Array.isArray(data) ? data : Array.isArray(data?.$values) ? data.$values : Array.isArray(data?.data) ? data.data : Array.isArray(data?.data?.$values) ? data.data.$values : [];
const errorMessage = (err) => typeof err?.response?.data === "string" ? err.response.data : err?.response?.data?.message || err?.response?.data?.title || err?.message || "An unexpected error occurred.";
const today = () => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,"0")}-${String(d.getDate()).padStart(2,"0")}`; };
const blankLine = (rowKey = 1) => ({ rowKey, itemId: "", orderedQuantity: "", uomId: "", unitCost: "" });
const blankOrder = () => ({ poNumber: "", supplierId: "", orderDate: today(), expectedDeliveryDate: "", lines: [blankLine()] });
const dateText = (v) => { if (!v) return "—"; const s = String(v).slice(0,10); const d = new Date(`${s}T00:00:00`); return Number.isNaN(d.getTime()) ? s : d.toLocaleDateString("en-US",{year:"numeric",month:"short",day:"numeric"}); };
const money = (v) => v == null || v === "" ? "—" : Number(v).toLocaleString("en-PH",{minimumFractionDigits:2,maximumFractionDigits:2});
const statusClass = (s) => ({ DRAFT:"po-status-draft", APPROVED:"po-status-approved", RELEASED:"po-status-approved", COMPLETED:"po-status-approved", PARTIAL:"po-status-partial", PARTIALLY_RECEIVED:"po-status-partial", RECEIVED:"po-status-received", CANCELLED:"po-status-cancelled", REJECTED:"po-status-cancelled" }[String(s || "").toUpperCase()] || "po-status-default");

function PurchaseOrdersPage() {
    const [purchaseOrders,setPurchaseOrders] = useState([]);
    const [suppliers,setSuppliers] = useState([]);
    const [items,setItems] = useState([]);
    const [units,setUnits] = useState([]);
    const [loading,setLoading] = useState(true);
    const [optionsLoading,setOptionsLoading] = useState(false);
    const [saving,setSaving] = useState(false);
    const [error,setError] = useState("");
    const [success,setSuccess] = useState("");
    const [searchTerm,setSearchTerm] = useState("");
    const [filterStatus,setFilterStatus] = useState("ALL");
    const [showCreateModal,setShowCreateModal] = useState(false);
    const [showDetailsModal,setShowDetailsModal] = useState(false);
    const [showEditHeaderModal,setShowEditHeaderModal] = useState(false);
    const [showLineModal,setShowLineModal] = useState(false);
    const [selectedOrder,setSelectedOrder] = useState(null);
    const [editingLine,setEditingLine] = useState(null);
    const [createForm,setCreateForm] = useState(blankOrder);
    const [headerForm,setHeaderForm] = useState({poNumber:"",supplierId:"",orderDate:"",expectedDeliveryDate:""});
    const [lineForm,setLineForm] = useState({itemId:"",orderedQuantity:"",uomId:"",unitCost:""});

    const loadPurchaseOrders = async () => {
        try { setLoading(true); const r = await api.get("/purchase-orders"); setPurchaseOrders(getArray(r.data)); setError(""); }
        catch(e) { setError(errorMessage(e)); }
        finally { setLoading(false); }
    };

    const loadDropdownData = async () => {
        setOptionsLoading(true);
        const results = await Promise.allSettled([api.get("/suppliers/active"),api.get("/items"),api.get("/units-of-measure")]);
        if (results[0].status === "fulfilled") setSuppliers(getArray(results[0].value.data));
        else setError(`Unable to load suppliers. ${errorMessage(results[0].reason)}`);
        if (results[1].status === "fulfilled") setItems(getArray(results[1].value.data).filter(x => getValue(x,"IsActive") !== false));
        else setError(`Unable to load items. ${errorMessage(results[1].reason)}`);
        if (results[2].status === "fulfilled") setUnits(getArray(results[2].value.data).filter(x => getValue(x,"IsActive") !== false));
        else setError(`Unable to load UOMs. ${errorMessage(results[2].reason)}`);
        setOptionsLoading(false);
    };

    useEffect(() => { loadPurchaseOrders(); loadDropdownData(); }, []);

    const supplierName = (id) => {
        const s = suppliers.find(x => Number(getValue(x,"SupplierId")) === Number(id));
        return s ? `${getValue(s,"SupplierCode")} - ${getValue(s,"SupplierName")}` : `Supplier #${id}`;
    };

    const filteredOrders = useMemo(() => purchaseOrders.filter(o => {
        const no = String(getValue(o,"PoNumber") || "").toLowerCase();
        const supplier = supplierName(getValue(o,"SupplierId")).toLowerCase();
        const status = String(getValue(o,"Status") || "").toUpperCase();
        return (no.includes(searchTerm.toLowerCase()) || supplier.includes(searchTerm.toLowerCase())) && (filterStatus === "ALL" || status === filterStatus);
    }),[purchaseOrders,suppliers,searchTerm,filterStatus]);

    const draftCount = purchaseOrders.filter(o => String(getValue(o,"Status") || "").toUpperCase() === "DRAFT").length;
    const openCreate = () => { setCreateForm(blankOrder()); setError(""); setSuccess(""); setShowCreateModal(true); };
        const closeCreate = () => { if (!saving) setShowCreateModal(false); };
    const updateHeaderField = (field,value) => setCreateForm(p => ({...p,[field]:value}));

    // Use a stable row key and functional updates; no array mutation.
    const addCreateLine = (e) => {
        e.preventDefault();
        e.stopPropagation();
        setCreateForm(p => {
            const next = p.lines.reduce((max,line) => Math.max(max,line.rowKey),0) + 1;
            return {...p,lines:[...p.lines,blankLine(next)]};
        });
    };

    const removeCreateLine = (e,rowKey) => {
        e.preventDefault();
        e.stopPropagation();
        setCreateForm(p => p.lines.length <= 1 ? p : {...p,lines:p.lines.filter(line => line.rowKey !== rowKey)});
    };

    const updateCreateLine = (rowKey,field,value) => setCreateForm(p => ({
        ...p,
        lines:p.lines.map(line => {
            if (line.rowKey !== rowKey) return line;
            const next = {...line,[field]:value};
            if (field === "itemId") {
                const item = items.find(x => Number(getValue(x,"ItemId")) === Number(value));
                next.uomId = item ? String(getValue(item,"BaseUomId") ?? "") : "";
            }
            return next;
        })
    }));

    const handleCreate = async (e) => {
        e.preventDefault(); setError(""); setSuccess("");
        if (!createForm.poNumber.trim()) return setError("Purchase order number is required.");
        if (!createForm.supplierId) return setError("Please select a supplier.");
        if (!createForm.orderDate) return setError("Order date is required.");
        if (createForm.expectedDeliveryDate && createForm.expectedDeliveryDate < createForm.orderDate) return setError("Expected delivery date cannot be earlier than order date.");
        for (let i=0;i<createForm.lines.length;i++) {
            const line=createForm.lines[i];
            if (!line.itemId || !line.uomId) return setError(`Please select an item and UOM for line ${i+1}.`);
            if (!line.orderedQuantity || Number(line.orderedQuantity)<=0) return setError(`Line ${i+1} must have a quantity greater than zero.`);
            if (line.unitCost !== "" && Number(line.unitCost)<0) return setError(`Line ${i+1} cannot have a negative unit cost.`);
        }
        const payload = {
            poNumber:createForm.poNumber.trim(), supplierId:Number(createForm.supplierId), orderDate:createForm.orderDate,
            expectedDeliveryDate:createForm.expectedDeliveryDate || null,
            lines:createForm.lines.map(l => ({itemId:Number(l.itemId),orderedQuantity:Number(l.orderedQuantity),uomId:Number(l.uomId),unitCost:l.unitCost === "" ? null : Number(l.unitCost)}))
        };
        try { setSaving(true); await api.post("/purchase-orders",payload); setShowCreateModal(false); setCreateForm(blankOrder()); setSuccess("Purchase order created successfully."); await loadPurchaseOrders(); }
        catch(e) { setError(errorMessage(e)); } finally { setSaving(false); }
    };

    const openDetails = async (id) => {
        try { setError(""); const r=await api.get(`/purchase-orders/${id}`); setSelectedOrder(r.data); setShowDetailsModal(true); }
        catch(e) { setError(errorMessage(e)); }
    };
    const refreshSelected = async (id) => { const r=await api.get(`/purchase-orders/${id}`); setSelectedOrder(r.data); await loadPurchaseOrders(); };
    const selectedLines = getArray(getValue(selectedOrder,"Lines"));
    const isDraft = String(getValue(selectedOrder,"Status") || "").toUpperCase() === "DRAFT";

    const openEditHeader = () => {
        if (!selectedOrder) return;
        setHeaderForm({
            poNumber:getValue(selectedOrder,"PoNumber") || "",
            supplierId:String(getValue(selectedOrder,"SupplierId") || ""),
            orderDate:String(getValue(selectedOrder,"OrderDate") || "").slice(0,10),
            expectedDeliveryDate:getValue(selectedOrder,"ExpectedDeliveryDate") ? String(getValue(selectedOrder,"ExpectedDeliveryDate")).slice(0,10) : ""
        });
        setShowEditHeaderModal(true);
        };

    const saveHeader = async (e) => {
        e.preventDefault();
        if (!selectedOrder) return;
        if (!headerForm.poNumber.trim() || !headerForm.supplierId || !headerForm.orderDate) return setError("Please complete all required purchase order fields.");
        if (headerForm.expectedDeliveryDate && headerForm.expectedDeliveryDate < headerForm.orderDate) return setError("Expected delivery date cannot be earlier than order date.");
        const id=getValue(selectedOrder,"PurchaseOrderId");
        try {
            setSaving(true); setError("");
            await api.put(`/purchase-orders/${id}`,{poNumber:headerForm.poNumber.trim(),supplierId:Number(headerForm.supplierId),orderDate:headerForm.orderDate,expectedDeliveryDate:headerForm.expectedDeliveryDate || null});
            setShowEditHeaderModal(false); await refreshSelected(id); setSuccess("Purchase order updated successfully.");
        } catch(e) { setError(errorMessage(e)); } finally { setSaving(false); }
    };

    const openAddLine = () => { setEditingLine(null); setLineForm({itemId:"",orderedQuantity:"",uomId:"",unitCost:""}); setError(""); setShowLineModal(true); };
    const openEditLine = (line) => {
        setEditingLine(line);
        setLineForm({itemId:String(getValue(line,"ItemId") || ""),orderedQuantity:String(getValue(line,"OrderedQuantity") ?? ""),uomId:String(getValue(line,"UomId") || ""),unitCost:getValue(line,"UnitCost") == null ? "" : String(getValue(line,"UnitCost"))});
        setError(""); setShowLineModal(true);
    };
    const updateLineField = (field,value) => setLineForm(p => {
        const n={...p,[field]:value};
        if (field==="itemId") { const item=items.find(x=>Number(getValue(x,"ItemId"))===Number(value)); n.uomId=item?String(getValue(item,"BaseUomId") ?? ""):""; }
        return n;
    });
    const saveLine = async (e) => {
        e.preventDefault();
        if (!selectedOrder) return;
        if (!lineForm.itemId || !lineForm.uomId) return setError("Please select an item and unit of measure.");
        if (!lineForm.orderedQuantity || Number(lineForm.orderedQuantity)<=0) return setError("Ordered quantity must be greater than zero.");
        if (lineForm.unitCost!=="" && Number(lineForm.unitCost)<0) return setError("Unit cost cannot be negative.");
        const id=getValue(selectedOrder,"PurchaseOrderId");
        const payload={itemId:Number(lineForm.itemId),orderedQuantity:Number(lineForm.orderedQuantity),uomId:Number(lineForm.uomId),unitCost:lineForm.unitCost===""?null:Number(lineForm.unitCost)};
        try {
            setSaving(true); setError("");
            if (editingLine) await api.put(`/purchase-orders/${id}/lines/${getValue(editingLine,"PoLineId")}`,payload);
            else await api.post(`/purchase-orders/${id}/lines`,payload);
            setShowLineModal(false); setEditingLine(null); await refreshSelected(id);
            setSuccess(editingLine?"Purchase order item updated successfully.":"Purchase order item added successfully.");
        } catch(e) { setError(errorMessage(e)); } finally { setSaving(false); }
    };
    const deleteLine = async (line) => {
        if (!selectedOrder) return;
        const id=getValue(selectedOrder,"PurchaseOrderId");
        if (!window.confirm(`Remove ${getValue(line,"ItemCode") || "this item"} from this purchase order?`)) return;
        try { setError(""); await api.delete(`/purchase-orders/${id}/lines/${getValue(line,"PoLineId")}`); await refreshSelected(id); setSuccess("Purchase order item removed successfully."); }
        catch(e) { setError(errorMessage(e)); }
    };
    const itemLabel = (id) => { const x=items.find(i=>Number(getValue(i,"ItemId"))===Number(id)); return x?`${getValue(x,"ItemCode")} - ${getValue(x,"ItemName")}`:""; };

    const modal = (title,subtitle,content,actions,close) => (
        <div className="po-modal-overlay">
            <div className="po-modal po-small-modal" role="dialog" aria-modal="true">
                <div className="po-modal-header"><div><h2>{title}</h2><p>{subtitle}</p></div><button type="button" className="po-modal-close" onClick={close}>×</button></div>
                {content}
                {actions}
            </div>
        </div>
    );

    return <div className="page-content purchase-orders-page">
        <div className="po-page-header"><div><h1>Purchase Orders</h1><p>Manage supplier orders and incoming materials.</p></div><button type="button" className="po-primary-button" onClick={openCreate}>+ Create Purchase Order</button></div>
        {error && !showCreateModal && !showEditHeaderModal && !showLineModal && <div className="po-alert po-alert-error">{error}<button type="button" onClick={()=>setError("")}>×</button></div>}
        {success && <div className="po-alert po-alert-success">{success}<button type="button" onClick={()=>setSuccess("")}>×</button></div>}
        <div className="po-summary-grid">
            <div className="po-summary-card"><span>Total Purchase Orders</span><strong>{purchaseOrders.length}</strong></div>
            <div className="po-summary-card"><span>Draft Orders</span><strong>{draftCount}</strong></div>
            <div className="po-summary-card"><span>Other Statuses</span><strong>{purchaseOrders.length-draftCount}</strong></div>
        </div>
                <section className="po-section">
            <div className="po-section-heading"><div><h2>Purchase Order List</h2><p>View and manage your purchase orders.</p></div><span>{filteredOrders.length} Records</span></div>
            <div className="po-toolbar">
                <input value={searchTerm} onChange={e=>setSearchTerm(e.target.value)} placeholder="Search PO number or supplier..."/>
                <select value={filterStatus} onChange={e=>setFilterStatus(e.target.value)}><option value="ALL">All Statuses</option><option value="DRAFT">Draft</option><option value="APPROVED">Approved</option><option value="RELEASED">Released</option><option value="PARTIALLY_RECEIVED">Partially Received</option><option value="RECEIVED">Received</option><option value="CANCELLED">Cancelled</option></select>
                <button type="button" className="po-secondary-button" onClick={loadPurchaseOrders} disabled={loading}>Refresh</button>
            </div>
            {loading?<div className="po-empty-state">Loading purchase orders...</div>:filteredOrders.length===0?<div className="po-empty-state">No purchase orders found.</div>:<div className="po-table-wrapper"><table className="po-table">
                <thead><tr><th>PO Number</th><th>Supplier</th><th>Order Date</th><th>Expected Delivery</th><th>Items</th><th>Status</th><th>Action</th></tr></thead>
                <tbody>{filteredOrders.map(o=><tr key={getValue(o,"PurchaseOrderId")}><td>{getValue(o,"PoNumber")}</td><td>{supplierName(getValue(o,"SupplierId"))}</td><td>{dateText(getValue(o,"OrderDate"))}</td><td>{dateText(getValue(o,"ExpectedDeliveryDate"))}</td><td>{getValue(o,"LineCount")??0}</td><td><span className={`po-status-badge ${statusClass(getValue(o,"Status"))}`}>{getValue(o,"Status")||"UNKNOWN"}</span></td><td><button type="button" className="po-view-button" onClick={()=>openDetails(getValue(o,"PurchaseOrderId"))}>View Details</button></td></tr>)}</tbody>
            </table></div>}
        </section>

        {showCreateModal && <div className="po-modal-overlay" onMouseDown={e=>{if(e.target===e.currentTarget)closeCreate();}}>
            <div className="po-modal po-create-modal" role="dialog" aria-modal="true">
                <div className="po-modal-header"><div><h2>Create Purchase Order</h2><p>Enter the purchase order details and items.</p></div><button type="button" className="po-modal-close" onClick={closeCreate} disabled={saving}>×</button></div>
                <form className="po-modal-form" onSubmit={handleCreate}>
                    <div className="po-modal-body">
                        {error&&<div className="po-alert po-alert-error">{error}</div>}
                        {optionsLoading?<div className="po-empty-state">Loading dropdown options...</div>:<>
                            <div className="po-form-grid">
                                <div className="po-form-group"><label>Purchase Order Number *</label><input value={createForm.poNumber} onChange={e=>updateHeaderField("poNumber",e.target.value)} placeholder="Enter PO number" required/></div>
                                <div className="po-form-group"><label>Supplier *</label><select value={createForm.supplierId} onChange={e=>updateHeaderField("supplierId",e.target.value)} required><option value="">Select supplier</option>{suppliers.map(s=><option key={getValue(s,"SupplierId")} value={getValue(s,"SupplierId")}>{getValue(s,"SupplierCode")} - {getValue(s,"SupplierName")}</option>)}</select></div>
                                <div className="po-form-group"><label>Order Date *</label><input type="date" value={createForm.orderDate} onChange={e=>updateHeaderField("orderDate",e.target.value)} required/></div>
                                <div className="po-form-group"><label>Expected Delivery Date</label><input type="date" min={createForm.orderDate} value={createForm.expectedDeliveryDate} onChange={e=>updateHeaderField("expectedDeliveryDate",e.target.value)}/></div>
                            </div>
                            <div className="po-lines-heading"><div><h3>Purchase Order Items</h3><p>Add one or more items to this order.</p></div><button type="button" className="po-secondary-button" onClick={addCreateLine}>+ Add Item</button></div>
                            <div className="po-edit-lines">{createForm.lines.map((line,index)=><div className="po-edit-line-card" key={line.rowKey}>
                                <div className="po-edit-line-heading"><strong>Item {index+1}</strong><button type="button" className="po-remove-button" onClick={e=>removeCreateLine(e,line.rowKey)} disabled={createForm.lines.length===1||saving}>Remove</button></div>
                                <div className="po-form-grid">
                                    <div className="po-form-group po-full-width"><label>Item *</label><select value={line.itemId} onChange={e=>updateCreateLine(line.rowKey,"itemId",e.target.value)} required><option value="">Select item</option>{items.map(i=><option key={getValue(i,"ItemId")} value={getValue(i,"ItemId")}>{getValue(i,"ItemCode")} - {getValue(i,"ItemName")}</option>)}</select></div>
                                    <div className="po-form-group"><label>Ordered Quantity *</label><input type="number" min="0.000001" step="0.000001" value={line.orderedQuantity} onChange={e=>updateCreateLine(line.rowKey,"orderedQuantity",e.target.value)} required/></div>
                                    <div className="po-form-group"><label>Unit of Measure *</label><select value={line.uomId} onChange={e=>updateCreateLine(line.rowKey,"uomId",e.target.value)} required><option value="">Select UOM</option>{units.map(u=><option key={getValue(u,"UomId")} value={getValue(u,"UomId")}>{getValue(u,"UomCode")} - {getValue(u,"UomName")}</option>)}</select></div>
                                    <div className="po-form-group"><label>Unit Cost</label><input type="number" min="0" step="0.01" value={line.unitCost} onChange={e=>updateCreateLine(line.rowKey,"unitCost",e.target.value)} placeholder="Optional"/></div>
                                </div>
                            </div>)}</div>
                        </>}
                    </div>
                    <div className="po-modal-actions"><button type="button" className="po-secondary-button" onClick={closeCreate} disabled={saving}>Cancel</button><button type="submit" className="po-primary-button" disabled={saving||optionsLoading}>{saving?"Creating...":"Create Purchase Order"}</button></div>
                </form>
            </div>
        </div>}

        {showDetailsModal&&selectedOrder&&<div className="po-modal-overlay"><div className="po-modal" role="dialog" aria-modal="true">
            <div className="po-modal-header"><div><h2>Purchase Order Details</h2><p>{getValue(selectedOrder,"PoNumber")}</p></div><button type="button" className="po-modal-close" onClick={()=>setShowDetailsModal(false)}>×</button></div>
            <div className="po-modal-body">
                {error&&<div className="po-alert po-alert-error">{error}</div>}
                <div className="po-detail-grid"><div><span>PO Number</span><strong>{getValue(selectedOrder,"PoNumber")}</strong></div><div><span>Supplier</span><strong>{supplierName(getValue(selectedOrder,"SupplierId"))}</strong></div><div><span>Order Date</span><strong>{dateText(getValue(selectedOrder,"OrderDate"))}</strong></div><div><span>Expected Delivery</span><strong>{dateText(getValue(selectedOrder,"ExpectedDeliveryDate"))}</strong></div><div><span>Status</span><strong>{getValue(selectedOrder,"Status")}</strong></div><div><span>Created At</span><strong>{dateText(getValue(selectedOrder,"CreatedAt"))}</strong></div></div>
                <div className="po-lines-heading"><div><h3>Ordered Items</h3><p>{selectedLines.length} item lines</p></div>{isDraft&&<button type="button" className="po-secondary-button" onClick={openAddLine}>+ Add Item</button>}</div>
                {selectedLines.length===0?<div className="po-empty-state">No purchase order lines found.</div>:<div className="po-table-wrapper"><table className="po-table"><thead><tr><th>Item Code</th><th>Item Name</th><th>Quantity</th><th>UOM</th><th>Unit Cost</th>{isDraft&&<th>Actions</th>}</tr></thead><tbody>{selectedLines.map(l=><tr key={getValue(l,"PoLineId")}><td>{getValue(l,"ItemCode")}</td><td>{getValue(l,"ItemName")}</td><td>{Number(getValue(l,"OrderedQuantity")).toLocaleString()}</td><td>{getValue(l,"UomCode")}</td><td>{money(getValue(l,"UnitCost"))}</td>{isDraft&&<td className="po-line-actions"><button type="button" className="po-view-button" onClick={()=>openEditLine(l)}>Edit</button><button type="button" className="po-remove-button" onClick={()=>deleteLine(l)}>Delete</button></td>}</tr>)}</tbody></table></div>}
            </div>
            <div className="po-modal-actions">{isDraft&&<button type="button" className="po-secondary-button" onClick={openEditHeader}>Edit Purchase Order</button>}<button type="button" className="po-primary-button" onClick={()=>setShowDetailsModal(false)}>Close</button></div>
        </div></div>}

        {showEditHeaderModal&&modal("Edit Purchase Order","Update the draft purchase order information.",
            <form className="po-modal-form" onSubmit={saveHeader}><div className="po-modal-body">{error&&<div className="po-alert po-alert-error">{error}</div>}<div className="po-form-grid">
                <div className="po-form-group po-full-width"><label>Purchase Order Number *</label><input value={headerForm.poNumber} onChange={e=>setHeaderForm(p=>({...p,poNumber:e.target.value}))} required/></div>
                <div className="po-form-group po-full-width"><label>Supplier *</label><select value={headerForm.supplierId} onChange={e=>setHeaderForm(p=>({...p,supplierId:e.target.value}))} required><option value="">Select supplier</option>{suppliers.map(s=><option key={getValue(s,"SupplierId")} value={getValue(s,"SupplierId")}>{getValue(s,"SupplierCode")} - {getValue(s,"SupplierName")}</option>)}</select></div>
                <div className="po-form-group"><label>Order Date *</label><input type="date" value={headerForm.orderDate} onChange={e=>setHeaderForm(p=>({...p,orderDate:e.target.value}))} required/></div>
                <div className="po-form-group"><label>Expected Delivery</label><input type="date" min={headerForm.orderDate} value={headerForm.expectedDeliveryDate} onChange={e=>setHeaderForm(p=>({...p,expectedDeliveryDate:e.target.value}))}/></div>
            </div></div><div className="po-modal-actions"><button type="button" className="po-secondary-button" onClick={()=>setShowEditHeaderModal(false)}>Cancel</button><button type="submit" className="po-primary-button" disabled={saving}>{saving?"Saving...":"Save Changes"}</button></div></form>,
            ()=>setShowEditHeaderModal(false))}

        {showLineModal&&modal(editingLine?"Edit Item":"Add Item","Enter the purchase order item information.",
            <form className="po-modal-form" onSubmit={saveLine}><div className="po-modal-body">{error&&<div className="po-alert po-alert-error">{error}</div>}<div className="po-form-grid">
                <div className="po-form-group po-full-width"><label>Item *</label><select value={lineForm.itemId} onChange={e=>updateLineField("itemId",e.target.value)} required><option value="">Select item</option>{items.map(i=><option key={getValue(i,"ItemId")} value={getValue(i,"ItemId")}>{getValue(i,"ItemCode")} - {getValue(i,"ItemName")}</option>)}</select></div>
                <div className="po-form-group"><label>Ordered Quantity *</label><input type="number" min="0.000001" step="0.000001" value={lineForm.orderedQuantity} onChange={e=>updateLineField("orderedQuantity",e.target.value)} required/></div>
                <div className="po-form-group"><label>Unit of Measure *</label><select value={lineForm.uomId} onChange={e=>updateLineField("uomId",e.target.value)} required><option value="">Select UOM</option>{units.map(u=><option key={getValue(u,"UomId")} value={getValue(u,"UomId")}>{getValue(u,"UomCode")} - {getValue(u,"UomName")}</option>)}</select></div>
                <div className="po-form-group po-full-width"><label>Unit Cost</label><input type="number" min="0" step="0.01" value={lineForm.unitCost} onChange={e=>updateLineField("unitCost",e.target.value)} placeholder="Optional"/></div>
            </div>{lineForm.itemId&&<p className="po-selected-item-note">Selected item: {itemLabel(lineForm.itemId)}</p>}</div><div className="po-modal-actions"><button type="button" className="po-secondary-button" onClick={()=>setShowLineModal(false)}>Cancel</button><button type="submit" className="po-primary-button" disabled={saving}>{saving?"Saving...":editingLine?"Save Changes":"Add Item"}</button></div></form>,
            ()=>setShowLineModal(false))}
    </div>;
}

export default PurchaseOrdersPage;
