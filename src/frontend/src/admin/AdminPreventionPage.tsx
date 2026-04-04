import { useEffect, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'

import {
  createAdminPreventionGuide,
  deleteAdminPreventionGuide,
  updateAdminPreventionGuide,
} from '../api/admin'
import { queryKeys } from '../api/queryKeys'
import { adminPreventionGuideInputSchema, type AdminCostTierInput, type AdminPreventionGuideInput } from '../api/types'
import { useAdminPrevention, useAdminPreventionGuide } from '../hooks/useAdminContent'
import { useRegions } from '../hooks/useRegions'
import { AdminLayout, AdminNotice, type AdminNoticeState, getFirstZodIssue, RegionSelect } from './AdminShared'

const emptyTier: AdminCostTierInput = {
  type: 'Free',
  price: 0,
  provider: '',
  notes: null,
}

const emptyDraft: AdminPreventionGuideInput = {
  regionId: '',
  disease: '',
  title: '',
  content: '',
  costTiers: [],
}

export function AdminPreventionPage() {
  const queryClient = useQueryClient()
  const regionsQuery = useRegions({ page: 1, pageSize: 100 })
  const [selectedGuideId, setSelectedGuideId] = useState('')
  const [diseaseFilter, setDiseaseFilter] = useState('')
  const [page, setPage] = useState(1)
  const [draft, setDraft] = useState<AdminPreventionGuideInput>(emptyDraft)
  const [notice, setNotice] = useState<AdminNoticeState>(null)

  const guidesQuery = useAdminPrevention({
    disease: diseaseFilter.trim() || undefined,
    page,
    pageSize: 12,
  })
  const detailQuery = useAdminPreventionGuide(selectedGuideId)

  useEffect(() => {
    if (!detailQuery.data) {
      return
    }

    setDraft({
      regionId: detailQuery.data.regionId,
      disease: detailQuery.data.disease,
      title: detailQuery.data.title,
      content: detailQuery.data.content,
      costTiers: detailQuery.data.costTiers.map((tier) => ({
        type: tier.type,
        price: tier.price,
        provider: tier.provider,
        notes: tier.notes,
      })),
    })
  }, [detailQuery.data])

  const saveMutation = useMutation({
    mutationFn: async () => {
      const parsed = adminPreventionGuideInputSchema.safeParse(draft)
      if (!parsed.success) {
        throw new Error(getFirstZodIssue(parsed.error))
      }

      if (selectedGuideId) {
        return updateAdminPreventionGuide(selectedGuideId, parsed.data)
      }

      return createAdminPreventionGuide(parsed.data)
    },
    onSuccess: async (saved) => {
      setNotice({
        tone: 'success',
        message: selectedGuideId ? 'Prevention guide updated.' : 'Prevention guide created.',
      })
      setSelectedGuideId(saved.id)
      await queryClient.invalidateQueries({ queryKey: ['admin', 'prevention'] })
      await queryClient.invalidateQueries({ queryKey: queryKeys.adminPreventionById(saved.id) })
    },
    onError: (error) => {
      setNotice({
        tone: 'error',
        message: error instanceof Error ? error.message : 'Unable to save this prevention guide right now.',
      })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: async () => {
      if (!selectedGuideId) {
        return
      }

      const justification = window.prompt('Why are you deleting this prevention guide?')
      if (!justification) {
        throw new Error('Deletion requires a justification.')
      }

      await deleteAdminPreventionGuide(selectedGuideId, justification)
    },
    onSuccess: async () => {
      setNotice({
        tone: 'success',
        message: 'Prevention guide deleted.',
      })
      setSelectedGuideId('')
      setDraft(emptyDraft)
      await queryClient.invalidateQueries({ queryKey: ['admin', 'prevention'] })
    },
    onError: (error) => {
      setNotice({
        tone: 'error',
        message: error instanceof Error ? error.message : 'Unable to delete this prevention guide right now.',
      })
    },
  })

  const totalCount = guidesQuery.data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / 12))

  return (
    <AdminLayout
      body="Manage prevention guidance and edit the cost tier matrix that the public prevention detail pages display."
      kicker="Admin prevention"
      title="Manage prevention guides"
      actions={
        <button
          className="dashboard-link admin-action-button"
          onClick={() => {
            setSelectedGuideId('')
            setDraft(emptyDraft)
            setNotice(null)
          }}
          type="button"
        >
          New guide
        </button>
      }
    >
      <AdminNotice notice={notice} />

      <section className="admin-grid">
        <article className="page-panel admin-list-panel">
          <div className="admin-panel-header">
            <div>
              <span className="section-kicker">Catalog</span>
              <strong>Prevention guides</strong>
            </div>
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
          </div>

          <div className="admin-list">
            {guidesQuery.data?.items.map((guide) => (
              <button
                className={selectedGuideId === guide.id ? 'admin-list-item admin-list-item--active' : 'admin-list-item'}
                key={guide.id}
                onClick={() => setSelectedGuideId(guide.id)}
                type="button"
              >
                <span className="section-kicker">{guide.disease}</span>
                <strong>{guide.title}</strong>
                <span className="admin-list-meta">
                  {guide.costTiers.length} cost tier{guide.costTiers.length === 1 ? '' : 's'}
                </span>
              </button>
            ))}
            {!guidesQuery.isLoading && guidesQuery.data?.items.length === 0 ? (
              <div className="dashboard-empty">No prevention guides match the current filter.</div>
            ) : null}
          </div>

          <div className="alerts-pagination">
            <span className="alerts-pagination__summary">
              Page {page} of {totalPages} · {totalCount} guides
            </span>
            <div className="alerts-pagination__controls">
              <button
                className="dashboard-link alerts-pagination__button"
                disabled={page === 1}
                onClick={() => setPage((current) => Math.max(1, current - 1))}
                type="button"
              >
                Previous
              </button>
              <button
                className="dashboard-link alerts-pagination__button"
                disabled={page >= totalPages}
                onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
                type="button"
              >
                Next
              </button>
            </div>
          </div>
        </article>

        <article className="page-panel admin-form-panel">
          <div className="admin-panel-header">
            <div>
              <span className="section-kicker">{selectedGuideId ? 'Edit guide' : 'Create guide'}</span>
              <strong>{selectedGuideId ? 'Update prevention guide' : 'Draft a new prevention guide'}</strong>
            </div>
          </div>

          <form
            className="admin-form"
            onSubmit={(event) => {
              event.preventDefault()
              setNotice(null)
              void saveMutation.mutateAsync()
            }}
          >
            <RegionSelect
              onChange={(regionId) => setDraft((current) => ({ ...current, regionId }))}
              regions={regionsQuery.data?.items ?? []}
              value={draft.regionId}
            />

            <label className="admin-field">
              <span>Disease</span>
              <input
                onChange={(event) => setDraft((current) => ({ ...current, disease: event.target.value }))}
                value={draft.disease}
              />
            </label>

            <label className="admin-field">
              <span>Title</span>
              <input
                onChange={(event) => setDraft((current) => ({ ...current, title: event.target.value }))}
                value={draft.title}
              />
            </label>

            <label className="admin-field">
              <span>Content</span>
              <textarea
                onChange={(event) => setDraft((current) => ({ ...current, content: event.target.value }))}
                rows={8}
                value={draft.content}
              />
            </label>

            <div className="admin-subpanel">
              <div className="admin-subpanel__header">
                <strong>Cost tiers</strong>
                <button
                  className="dashboard-link admin-action-button"
                  onClick={() =>
                    setDraft((current) => ({ ...current, costTiers: [...current.costTiers, { ...emptyTier }] }))
                  }
                  type="button"
                >
                  Add tier
                </button>
              </div>

              <div className="admin-tier-list">
                {draft.costTiers.map((tier, index) => (
                  <div className="admin-tier-card" key={`${tier.type}-${index}`}>
                    <div className="admin-tier-grid">
                      <label className="admin-field">
                        <span>Type</span>
                        <select
                          onChange={(event) =>
                            setDraft((current) => ({
                              ...current,
                              costTiers: current.costTiers.map((item, itemIndex) =>
                                itemIndex === index ? { ...item, type: event.target.value as AdminCostTierInput['type'] } : item,
                              ),
                            }))
                          }
                          value={tier.type}
                        >
                          <option value="Free">Free</option>
                          <option value="Insured">Insured</option>
                          <option value="OutOfPocket">OutOfPocket</option>
                          <option value="Promotional">Promotional</option>
                        </select>
                      </label>
                      <label className="admin-field">
                        <span>Price</span>
                        <input
                          min="0"
                          onChange={(event) =>
                            setDraft((current) => ({
                              ...current,
                              costTiers: current.costTiers.map((item, itemIndex) =>
                                itemIndex === index ? { ...item, price: Number(event.target.value || 0) } : item,
                              ),
                            }))
                          }
                          step="0.01"
                          type="number"
                          value={tier.price}
                        />
                      </label>
                    </div>
                    <label className="admin-field">
                      <span>Provider</span>
                      <input
                        onChange={(event) =>
                          setDraft((current) => ({
                            ...current,
                            costTiers: current.costTiers.map((item, itemIndex) =>
                              itemIndex === index ? { ...item, provider: event.target.value } : item,
                            ),
                          }))
                        }
                        value={tier.provider}
                      />
                    </label>
                    <label className="admin-field">
                      <span>Notes</span>
                      <input
                        onChange={(event) =>
                          setDraft((current) => ({
                            ...current,
                            costTiers: current.costTiers.map((item, itemIndex) =>
                              itemIndex === index ? { ...item, notes: event.target.value || null } : item,
                            ),
                          }))
                        }
                        value={tier.notes ?? ''}
                      />
                    </label>
                    <button
                      className="dashboard-link admin-action-button admin-action-button--danger"
                      onClick={() =>
                        setDraft((current) => ({
                          ...current,
                          costTiers: current.costTiers.filter((_, itemIndex) => itemIndex !== index),
                        }))
                      }
                      type="button"
                    >
                      Remove tier
                    </button>
                  </div>
                ))}
              </div>
            </div>

            <div className="admin-form-actions">
              <button className="landing-link landing-link--primary" disabled={saveMutation.isPending} type="submit">
                {saveMutation.isPending ? 'Saving…' : selectedGuideId ? 'Save changes' : 'Create guide'}
              </button>
              {selectedGuideId ? (
                <button
                  className="dashboard-link admin-action-button admin-action-button--danger"
                  disabled={deleteMutation.isPending}
                  onClick={() => {
                    if (window.confirm('This will soft-delete the prevention guide. Continue?')) {
                      setNotice(null)
                      void deleteMutation.mutateAsync()
                    }
                  }}
                  type="button"
                >
                  Delete guide
                </button>
              ) : null}
            </div>
          </form>
        </article>
      </section>
    </AdminLayout>
  )
}
