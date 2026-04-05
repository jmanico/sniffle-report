import { useEffect, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'

import {
  createAdminAlert,
  deleteAdminAlert,
  updateAdminAlert,
  updateAdminAlertStatus,
} from '../api/admin'
import { queryKeys } from '../api/queryKeys'
import {
  adminAlertInputSchema,
  adminAlertStatusInputSchema,
  type AdminAlertInput,
  type AlertSeverity,
  type AlertStatus,
} from '../api/types'
import { useAdminAlert, useAdminAlerts } from '../hooks/useAdminContent'
import { useRegions } from '../hooks/useRegions'
import { type AdminNoticeState, getFirstZodIssue, toInputDate } from './AdminFormUtils'
import { AdminLayout, AdminNotice, RegionSelect } from './AdminShared'

const emptyDraft: AdminAlertInput = {
  regionId: '',
  disease: '',
  title: '',
  summary: '',
  severity: 'Moderate',
  caseCount: 0,
  sourceAttribution: '',
  sourceDate: '',
  status: 'Draft',
}

export function AdminAlertsPage() {
  const queryClient = useQueryClient()
  const regionsQuery = useRegions({ page: 1, pageSize: 100 })
  const [selectedAlertId, setSelectedAlertId] = useState('')
  const [statusFilter, setStatusFilter] = useState<AlertStatus | ''>('')
  const [diseaseFilter, setDiseaseFilter] = useState('')
  const [page, setPage] = useState(1)
  const [draft, setDraft] = useState<AdminAlertInput>(emptyDraft)
  const [notice, setNotice] = useState<AdminNoticeState>(null)

  const alertsQuery = useAdminAlerts({
    disease: diseaseFilter.trim() || undefined,
    status: statusFilter || undefined,
    page,
    pageSize: 12,
  })
  const detailQuery = useAdminAlert(selectedAlertId)

  useEffect(() => {
    if (!detailQuery.data) {
      return
    }

    // eslint-disable-next-line react-hooks/set-state-in-effect -- the editor draft must be replaced when a different alert loads
    setDraft({
      regionId: detailQuery.data.regionId,
      disease: detailQuery.data.disease,
      title: detailQuery.data.title,
      summary: detailQuery.data.summary,
      severity: detailQuery.data.severity,
      caseCount: detailQuery.data.caseCount,
      sourceAttribution: detailQuery.data.sourceAttribution,
      sourceDate: toInputDate(detailQuery.data.sourceDate),
      status: detailQuery.data.status,
    })
  }, [detailQuery.data])

  const saveMutation = useMutation({
    mutationFn: async () => {
      const parsed = adminAlertInputSchema.safeParse(draft)
      if (!parsed.success) {
        throw new Error(getFirstZodIssue(parsed.error))
      }

      if (selectedAlertId) {
        return updateAdminAlert(selectedAlertId, parsed.data)
      }

      return createAdminAlert(parsed.data)
    },
    onSuccess: async (saved) => {
      setNotice({
        tone: 'success',
        message: selectedAlertId ? 'Alert updated.' : 'Alert created.',
      })
      setSelectedAlertId(saved.id)
      await queryClient.invalidateQueries({ queryKey: ['admin', 'alerts'] })
      await queryClient.invalidateQueries({ queryKey: queryKeys.adminAlertById(saved.id) })
    },
    onError: (error) => {
      setNotice({
        tone: 'error',
        message: error instanceof Error ? error.message : 'Unable to save this alert right now.',
      })
    },
  })

  const statusMutation = useMutation({
    mutationFn: async (status: AlertStatus) => {
      if (!selectedAlertId) {
        throw new Error('Select an alert first.')
      }

      const needsJustification = status === 'Draft' || status === 'Archived'
      const justification = needsJustification ? window.prompt(`Provide a justification for moving this alert to ${status}.`) : ''
      const parsed = adminAlertStatusInputSchema.safeParse({ status, justification })

      if (!parsed.success) {
        throw new Error(getFirstZodIssue(parsed.error))
      }

      return updateAdminAlertStatus(selectedAlertId, parsed.data)
    },
    onSuccess: async (saved) => {
      setNotice({
        tone: 'success',
        message: `Alert status updated to ${saved.status}.`,
      })
      setDraft((current) => ({ ...current, status: saved.status }))
      await queryClient.invalidateQueries({ queryKey: ['admin', 'alerts'] })
      await queryClient.invalidateQueries({ queryKey: queryKeys.adminAlertById(saved.id) })
    },
    onError: (error) => {
      setNotice({
        tone: 'error',
        message: error instanceof Error ? error.message : 'Unable to update alert status right now.',
      })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: async () => {
      if (!selectedAlertId) {
        return
      }

      const justification = window.prompt('Why are you deleting this alert?')
      if (!justification) {
        throw new Error('Deletion requires a justification.')
      }

      await deleteAdminAlert(selectedAlertId, justification)
    },
    onSuccess: async () => {
      setNotice({
        tone: 'success',
        message: 'Alert deleted.',
      })
      setSelectedAlertId('')
      setDraft(emptyDraft)
      await queryClient.invalidateQueries({ queryKey: ['admin', 'alerts'] })
    },
    onError: (error) => {
      setNotice({
        tone: 'error',
        message: error instanceof Error ? error.message : 'Unable to delete this alert right now.',
      })
    },
  })

  const totalCount = alertsQuery.data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / 12))

  return (
    <AdminLayout
      body="Create, revise, publish, archive, and soft-delete the health alerts that drive the public regional dashboard."
      kicker="Admin alerts"
      title="Manage health alerts"
      actions={
        <button
          className="dashboard-link admin-action-button"
          onClick={() => {
            setSelectedAlertId('')
            setDraft(emptyDraft)
            setNotice(null)
          }}
          type="button"
        >
          New alert
        </button>
      }
    >
      <AdminNotice notice={notice} />

      <section className="admin-grid">
        <article className="page-panel admin-list-panel">
          <div className="admin-panel-header">
            <div>
              <span className="section-kicker">Alert queue</span>
              <strong>All alerts</strong>
            </div>
            <div className="admin-filter-cluster">
              <label className="admin-inline-filter">
                <span>Disease</span>
                <input
                  onChange={(event) => {
                    setDiseaseFilter(event.target.value)
                    setPage(1)
                  }}
                  placeholder="Filter by disease"
                  type="search"
                  value={diseaseFilter}
                />
              </label>
              <label className="admin-inline-filter">
                <span>Status</span>
                <select
                  onChange={(event) => {
                    setStatusFilter((event.target.value as AlertStatus | '') || '')
                    setPage(1)
                  }}
                  value={statusFilter}
                >
                  <option value="">All</option>
                  <option value="Draft">Draft</option>
                  <option value="Published">Published</option>
                  <option value="Archived">Archived</option>
                </select>
              </label>
            </div>
          </div>

          <div className="admin-list">
            {alertsQuery.data?.items.map((alert) => (
              <button
                className={selectedAlertId === alert.id ? 'admin-list-item admin-list-item--active' : 'admin-list-item'}
                key={alert.id}
                onClick={() => setSelectedAlertId(alert.id)}
                type="button"
              >
                <span className="section-kicker">{alert.status}</span>
                <strong>{alert.title}</strong>
                <span className="admin-list-meta">
                  {alert.disease} · {alert.caseCount} cases · {alert.severity}
                </span>
              </button>
            ))}
            {!alertsQuery.isLoading && alertsQuery.data?.items.length === 0 ? (
              <div className="dashboard-empty">No alerts match the current filters.</div>
            ) : null}
          </div>

          <div className="alerts-pagination">
            <span className="alerts-pagination__summary">
              Page {page} of {totalPages} · {totalCount} alerts
            </span>
            <div className="alerts-pagination__controls">
              <button className="dashboard-link alerts-pagination__button" disabled={page === 1} onClick={() => setPage((c) => Math.max(1, c - 1))} type="button">
                Previous
              </button>
              <button className="dashboard-link alerts-pagination__button" disabled={page >= totalPages} onClick={() => setPage((c) => Math.min(totalPages, c + 1))} type="button">
                Next
              </button>
            </div>
          </div>
        </article>

        <article className="page-panel admin-form-panel">
          <div className="admin-panel-header">
            <div>
              <span className="section-kicker">{selectedAlertId ? 'Edit alert' : 'Create alert'}</span>
              <strong>{selectedAlertId ? 'Update alert details' : 'Create a new health alert'}</strong>
            </div>
            <div className="admin-filter-cluster">
              <button className="dashboard-link admin-action-button" disabled={!selectedAlertId || statusMutation.isPending} onClick={() => void statusMutation.mutateAsync('Published')} type="button">
                Publish
              </button>
              <button className="dashboard-link admin-action-button" disabled={!selectedAlertId || statusMutation.isPending} onClick={() => void statusMutation.mutateAsync('Archived')} type="button">
                Archive
              </button>
              <button className="dashboard-link admin-action-button" disabled={!selectedAlertId || statusMutation.isPending} onClick={() => void statusMutation.mutateAsync('Draft')} type="button">
                Revert to draft
              </button>
            </div>
          </div>

          <form className="admin-form" onSubmit={(event) => { event.preventDefault(); setNotice(null); void saveMutation.mutateAsync() }}>
            <RegionSelect onChange={(regionId) => setDraft((current) => ({ ...current, regionId }))} regions={regionsQuery.data?.items ?? []} value={draft.regionId} />

            <label className="admin-field">
              <span>Title</span>
              <input onChange={(event) => setDraft((current) => ({ ...current, title: event.target.value }))} value={draft.title} />
            </label>

            <div className="admin-two-column">
              <label className="admin-field">
                <span>Disease</span>
                <input onChange={(event) => setDraft((current) => ({ ...current, disease: event.target.value }))} value={draft.disease} />
              </label>
              <label className="admin-field">
                <span>Severity</span>
                <select onChange={(event) => setDraft((current) => ({ ...current, severity: event.target.value as AlertSeverity }))} value={draft.severity}>
                  <option value="Low">Low</option>
                  <option value="Moderate">Moderate</option>
                  <option value="High">High</option>
                  <option value="Critical">Critical</option>
                </select>
              </label>
            </div>

            <div className="admin-two-column">
              <label className="admin-field">
                <span>Case count</span>
                <input min="0" onChange={(event) => setDraft((current) => ({ ...current, caseCount: Number(event.target.value || 0) }))} type="number" value={draft.caseCount} />
              </label>
              <label className="admin-field">
                <span>Status</span>
                <select onChange={(event) => setDraft((current) => ({ ...current, status: event.target.value as AlertStatus }))} value={draft.status}>
                  <option value="Draft">Draft</option>
                  <option value="Published">Published</option>
                  <option value="Archived">Archived</option>
                </select>
              </label>
            </div>

            <label className="admin-field">
              <span>Summary</span>
              <textarea onChange={(event) => setDraft((current) => ({ ...current, summary: event.target.value }))} rows={8} value={draft.summary} />
            </label>

            <div className="admin-two-column">
              <label className="admin-field">
                <span>Source attribution</span>
                <input onChange={(event) => setDraft((current) => ({ ...current, sourceAttribution: event.target.value }))} value={draft.sourceAttribution} />
              </label>
              <label className="admin-field">
                <span>Source date</span>
                <input onChange={(event) => setDraft((current) => ({ ...current, sourceDate: event.target.value }))} type="date" value={draft.sourceDate} />
              </label>
            </div>

            <div className="admin-form-actions">
              <button className="landing-link landing-link--primary" disabled={saveMutation.isPending} type="submit">
                {saveMutation.isPending ? 'Saving…' : selectedAlertId ? 'Save changes' : 'Create alert'}
              </button>
              {selectedAlertId ? (
                <button
                  className="dashboard-link admin-action-button admin-action-button--danger"
                  disabled={deleteMutation.isPending}
                  onClick={() => {
                    if (window.confirm('This will soft-delete the alert. Continue?')) {
                      setNotice(null)
                      void deleteMutation.mutateAsync()
                    }
                  }}
                  type="button"
                >
                  Delete alert
                </button>
              ) : null}
            </div>
          </form>
        </article>
      </section>
    </AdminLayout>
  )
}
