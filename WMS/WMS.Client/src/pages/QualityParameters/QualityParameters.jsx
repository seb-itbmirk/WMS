
import { useEffect, useMemo, useState } from 'react'
import api from '../../services/api'

const emptyForm = {
    parameterCode: '',
    parameterName: '',
    description: '',
    unitOfMeasure: ''
}

const getErrorMessage = (error) => {
    return error.response?.data?.message ||
        error.response?.data?.title ||
        error.message ||
        'Something went wrong. Please try again.'
}

function QualityParametersPage() {
    const [parameters, setParameters] = useState([])
    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)
    const [error, setError] = useState('')
    const [search, setSearch] = useState('')

    const [showModal, setShowModal] = useState(false)
    const [editingParameter, setEditingParameter] = useState(null)
    const [form, setForm] = useState(emptyForm)
    const [formError, setFormError] = useState('')

    const loadParameters = async () => {
        try {
            setError('')
            const response = await api.get('/quality-parameters')
            setParameters(Array.isArray(response.data) ? response.data : [])
        } catch (err) {
            setError(getErrorMessage(err))
        } finally {
            setLoading(false)
        }
    }

    useEffect(() => {
        loadParameters()
    }, [])

    const filteredParameters = useMemo(() => {
        const keyword = search.trim().toLowerCase()

        if (!keyword) return parameters

        return parameters.filter((parameter) =>
            [
                parameter.parameterCode,
                parameter.parameterName,
                parameter.description,
                parameter.unitOfMeasure
            ].some((value) =>
                String(value || '').toLowerCase().includes(keyword)
            )
        )
    }, [parameters, search])

    const openAddModal = () => {
        setEditingParameter(null)
        setForm(emptyForm)
        setFormError('')
        setShowModal(true)
    }

    const openEditModal = (parameter) => {
        setEditingParameter(parameter)
        setForm({
            parameterCode: parameter.parameterCode || '',
            parameterName: parameter.parameterName || '',
            description: parameter.description || '',
            unitOfMeasure: parameter.unitOfMeasure || ''
        })
        setFormError('')
        setShowModal(true)
    }

    const closeModal = () => {
        if (saving) return

        setShowModal(false)
        setEditingParameter(null)
        setForm(emptyForm)
        setFormError('')
    }

    const handleChange = (event) => {
        const { name, value } = event.target

        setForm((previous) => ({
            ...previous,
            [name]: value
        }))
    }

    const handleSubmit = async (event) => {
        event.preventDefault()
        setFormError('')

        const parameterCode = form.parameterCode.trim()
        const parameterName = form.parameterName.trim()

        if (!parameterCode || !parameterName) {
            setFormError('Parameter code and parameter name are required.')
            return
        }

        if (parameterCode.length > 40) {
            setFormError('Parameter code cannot exceed 40 characters.')
            return
        }

        if (parameterName.length > 120) {
            setFormError('Parameter name cannot exceed 120 characters.')
            return
        }

        const payload = {
            parameterCode,
            parameterName,
            description: form.description.trim() || null,
            unitOfMeasure: form.unitOfMeasure.trim() || null
        }

        try {
            setSaving(true)

            if (editingParameter) {
                await api.put(
                    `/quality-parameters/${editingParameter.parameterId}`,
                    payload
                )
            } else {
                await api.post('/quality-parameters', payload)
            }

            setShowModal(false)
            setEditingParameter(null)
            setForm(emptyForm)

            await loadParameters()
        } catch (err) {
            setFormError(getErrorMessage(err))
        } finally {
            setSaving(false)
        }
    }

    return (
        <div className="qp-page">
            <div className="qp-page-header">
                <div>
                    <h1>Quality Parameters</h1>
                    <p>
                        Manage the inspection criteria used in your
                        warehouse and manufacturing quality processes.
                    </p>
                </div>

                <button
                    type="button"
                    className="qp-primary-button"
                    onClick={openAddModal}
                >
                    <span>+</span> Add Parameter
                </button>
            </div>

            <div className="qp-summary-grid">
                <div className="qp-summary-card">
                    <div className="qp-summary-icon">✓</div>
                    <div>
                        <span>Total Parameters</span>
                        <h2>{parameters.length}</h2>
                    </div>
                </div>

                <div className="qp-summary-card">
                    <div className="qp-summary-icon">⌕</div>
                    <div>
                        <span>Search Results</span>
                        <h2>{filteredParameters.length}</h2>
                    </div>
                </div>
            </div>

            <div className="qp-content-card">
                <div className="qp-toolbar">
                    <div>
                        <h2>Inspection Parameters</h2>
                        <p>
                            Define the measurements and inspection
                            criteria used by your quality team.
                        </p>
                    </div>

                    <div className="qp-search-wrapper">
                        <span>⌕</span>
                        <input
                            type="text"
                            placeholder="Search parameters..."
                            value={search}
                            onChange={(event) => setSearch(event.target.value)}
                        />
                    </div>
                </div>

                {error && (
                    <div className="qp-error-message">
                        <span>{error}</span>
                        <button
                            type="button"
                            onClick={loadParameters}
                        >
                            Retry
                        </button>
                    </div>
                )}

                {loading ? (
                    <div className="qp-loading">
                        Loading quality parameters...
                    </div>
                ) : (
                    <div className="qp-table-wrapper">
                        <table className="qp-table">
                            <thead>
                                <tr>
                                    <th>PARAMETER CODE</th>
                                    <th>PARAMETER NAME</th>
                                    <th>DESCRIPTION</th>
                                    <th>UNIT OF MEASURE</th>
                                    <th>ACTIONS</th>
                                </tr>
                            </thead>

                            <tbody>
                                {filteredParameters.length === 0 ? (
                                    <tr>
                                        <td colSpan="5">
                                            <div className="qp-empty-state">
                                                <div className="qp-empty-icon">
                                                    ✓
                                                </div>
                                                <h3>No parameters found</h3>
                                                <p>
                                                    {search
                                                        ? 'Try a different search term.'
                                                        : 'Add your first quality parameter to get started.'}
                                                </p>
                                            </div>
                                        </td>
                                    </tr>
                                ) : (
                                    filteredParameters.map((parameter) => (
                                        <tr key={parameter.parameterId}>
                                            <td>
                                                <span className="qp-code">
                                                    {parameter.parameterCode}
                                                </span>
                                            </td>

                                            <td className="qp-name-cell">
                                                {parameter.parameterName}
                                            </td>

                                            <td>
                                                {parameter.description || '—'}
                                            </td>

                                            <td>
                                                {parameter.unitOfMeasure || '—'}
                                            </td>

                                            <td>
                                                <button
                                                    type="button"
                                                    className="qp-edit-button"
                                                    onClick={() =>
                                                        openEditModal(parameter)
                                                    }
                                                >
                                                    Edit
                                                </button>
                                            </td>
                                        </tr>
                                    ))
                                )}
                            </tbody>
                        </table>
                    </div>
                )}

                <div className="qp-table-footer">
                    Showing {filteredParameters.length} of {parameters.length} parameters
                </div>
            </div>

            {showModal && (
                <div
                    className="qp-modal-overlay"
                    onMouseDown={(event) => {
                        if (event.target === event.currentTarget) {
                            closeModal()
                        }
                    }}
                >
                    <div
                        className="qp-modal"
                        role="dialog"
                        aria-modal="true"
                        aria-labelledby="qp-modal-title"
                    >
                        <div className="qp-modal-header">
                            <div>
                                <h2 id="qp-modal-title">
                                    {editingParameter
                                        ? 'Edit Quality Parameter'
                                        : 'Add Quality Parameter'}
                                </h2>
                                <p>
                                    Enter the inspection parameter details below.
                                </p>
                            </div>

                            <button
                                type="button"
                                className="qp-close-button"
                                onClick={closeModal}
                                disabled={saving}
                                aria-label="Close"
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={handleSubmit}>
                            <div className="qp-modal-body">
                                {formError && (
                                    <div className="qp-error-message">
                                        {formError}
                                    </div>
                                )}

                                <div className="qp-form-group">
                                    <label htmlFor="parameterCode">
                                        Parameter Code <span>*</span>
                                    </label>
                                    <input
                                        id="parameterCode"
                                        name="parameterCode"
                                        type="text"
                                        value={form.parameterCode}
                                        onChange={handleChange}
                                        placeholder="e.g. QC-FAB-001"
                                        maxLength={40}
                                        required
                                    />
                                </div>

                                <div className="qp-form-group">
                                    <label htmlFor="parameterName">
                                        Parameter Name <span>*</span>
                                    </label>
                                    <input
                                        id="parameterName"
                                        name="parameterName"
                                        type="text"
                                        value={form.parameterName}
                                        onChange={handleChange}
                                        placeholder="Enter parameter name"
                                        maxLength={120}
                                        required
                                    />
                                </div>

                                <div className="qp-form-group">
                                    <label htmlFor="description">
                                        Description
                                    </label>
                                    <textarea
                                        id="description"
                                        name="description"
                                        value={form.description}
                                        onChange={handleChange}
                                        placeholder="Describe what needs to be inspected..."
                                        rows={3}
                                    />
                                </div>

                                <div className="qp-form-group">
                                    <label htmlFor="unitOfMeasure">
                                        Unit of Measure
                                    </label>
                                    <input
                                        id="unitOfMeasure"
                                        name="unitOfMeasure"
                                        type="text"
                                        value={form.unitOfMeasure}
                                        onChange={handleChange}
                                        placeholder="e.g. cm, kg, %, or leave blank"
                                    />
                                </div>

                                <p className="qp-required-note">
                                    * Required fields
                                </p>
                            </div>

                            <div className="qp-modal-footer">
                                <button
                                    type="button"
                                    className="qp-secondary-button"
                                    onClick={closeModal}
                                    disabled={saving}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="submit"
                                    className="qp-primary-button"
                                    disabled={saving}
                                >
                                    {saving
                                        ? 'Saving...'
                                        : editingParameter
                                            ? 'Save Changes'
                                            : 'Create Parameter'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    )
}

export default QualityParametersPage
