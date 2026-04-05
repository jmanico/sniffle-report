import { useEffect, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'

import { createAdminNewsItem, deleteAdminNewsItem, updateAdminNewsItem } from '../api/admin'
import { queryKeys } from '../api/queryKeys'
import { adminNewsItemInputSchema, type AdminNewsItemInput } from '../api/types'
import { useAdminNews, useAdminNewsItem } from '../hooks/useAdminContent'
import { useRegions } from '../hooks/useRegions'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'
import { type AdminNoticeState, getFirstZodIssue, toInputDate } from './AdminFormUtils'
import { AdminLayout, AdminNotice, RegionSelect } from './AdminShared'

const emptyDraft: AdminNewsItemInput = {
  regionId: '',
  headline: '',
  content: '',
  sourceUrl: '',
  publishedAt: '',
}

export function AdminNewsPage() {
  const queryClient = useQueryClient()
  const regionsQuery = useRegions({ page: 1, pageSize: 100 })
  const [selectedNewsId, setSelectedNewsId] = useState('')
  const [headlineFilter, setHeadlineFilter] = useState('')
  const [page, setPage] = useState(1)
  const [draft, setDraft] = useState<AdminNewsItemInput>(emptyDraft)
  const [notice, setNotice] = useState<AdminNoticeState>(null)

  const newsQuery = useAdminNews({
    headline: headlineFilter.trim() || undefined,
    page,
    pageSize: 12,
  })
  const detailQuery = useAdminNewsItem(selectedNewsId)

  useEffect(() => {
    if (!detailQuery.data) {
      return
    }

    // eslint-disable-next-line react-hooks/set-state-in-effect -- the editor draft must be replaced when a different news item loads
    setDraft({
      regionId: detailQuery.data.regionId,
      headline: detailQuery.data.headline,
      content: detailQuery.data.content,
      sourceUrl: detailQuery.data.sourceUrl,
      publishedAt: toInputDate(detailQuery.data.publishedAt),
    })
  }, [detailQuery.data])

  const saveMutation = useMutation({
    mutationFn: async () => {
      const parsed = adminNewsItemInputSchema.safeParse(draft)
      if (!parsed.success) {
        throw new Error(getFirstZodIssue(parsed.error))
      }

      if (selectedNewsId) {
        return updateAdminNewsItem(selectedNewsId, parsed.data)
      }

      return createAdminNewsItem(parsed.data)
    },
    onSuccess: async (saved) => {
      setNotice({
        tone: 'success',
        message: selectedNewsId ? 'News item updated.' : 'News item created.',
      })
      setSelectedNewsId(saved.id)
      await queryClient.invalidateQueries({ queryKey: ['admin', 'news'] })
      await queryClient.invalidateQueries({ queryKey: queryKeys.adminNewsById(saved.id) })
    },
    onError: (error) => {
      setNotice({
        tone: 'error',
        message: error instanceof Error ? error.message : 'Unable to save this news item right now.',
      })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: async () => {
      if (!selectedNewsId) {
        return
      }

      const justification = window.prompt('Why are you deleting this news item?')
      if (!justification) {
        throw new Error('Deletion requires a justification.')
      }

      await deleteAdminNewsItem(selectedNewsId, justification)
    },
    onSuccess: async () => {
      setNotice({
        tone: 'success',
        message: 'News item deleted.',
      })
      setSelectedNewsId('')
      setDraft(emptyDraft)
      await queryClient.invalidateQueries({ queryKey: ['admin', 'news'] })
    },
    onError: (error) => {
      setNotice({
        tone: 'error',
        message: error instanceof Error ? error.message : 'Unable to delete this news item right now.',
      })
    },
  })

  const totalCount = newsQuery.data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / 12))
  const factCheckStatus = detailQuery.data?.factCheckStatus ?? null
  const sanitizedSourceUrl = draft.sourceUrl ? validateAndSanitizeUrl(draft.sourceUrl) : null

  return (
    <AdminLayout
      body="Publish and revise health news items while keeping the current fact-check state visible to editors."
      kicker="Admin news"
      title="Manage health news"
      actions={
        <button
          className="dashboard-link admin-action-button"
          onClick={() => {
            setSelectedNewsId('')
            setDraft(emptyDraft)
            setNotice(null)
          }}
          type="button"
        >
          New news item
        </button>
      }
    >
      <AdminNotice notice={notice} />

      <section className="admin-grid">
        <article className="page-panel admin-list-panel">
          <div className="admin-panel-header">
            <div>
              <span className="section-kicker">Editorial queue</span>
              <strong>News items</strong>
            </div>
            <label className="admin-inline-filter">
              <span>Headline</span>
              <input
                onChange={(event) => {
                  setHeadlineFilter(event.target.value)
                  setPage(1)
                }}
                placeholder="Filter by headline"
                type="search"
                value={headlineFilter}
              />
            </label>
          </div>

          <div className="admin-list">
            {newsQuery.data?.items.map((item) => (
              <button
                className={selectedNewsId === item.id ? 'admin-list-item admin-list-item--active' : 'admin-list-item'}
                key={item.id}
                onClick={() => setSelectedNewsId(item.id)}
                type="button"
              >
                <span className="section-kicker">{item.factCheckStatus ?? 'Pending fact-check'}</span>
                <strong>{item.headline}</strong>
                <span className="admin-list-meta">{new Date(item.publishedAt).toLocaleDateString()}</span>
              </button>
            ))}
            {!newsQuery.isLoading && newsQuery.data?.items.length === 0 ? (
              <div className="dashboard-empty">No news items match the current filter.</div>
            ) : null}
          </div>

          <div className="alerts-pagination">
            <span className="alerts-pagination__summary">
              Page {page} of {totalPages} · {totalCount} news items
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
              <span className="section-kicker">{selectedNewsId ? 'Edit item' : 'Create item'}</span>
              <strong>{selectedNewsId ? 'Update health news item' : 'Draft a health news item'}</strong>
            </div>
            <div className="admin-factcheck-state">
              <span className="section-kicker">Fact-check</span>
              <strong>{factCheckStatus ?? 'Not started'}</strong>
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
              <span>Headline</span>
              <input
                onChange={(event) => setDraft((current) => ({ ...current, headline: event.target.value }))}
                value={draft.headline}
              />
            </label>

            <label className="admin-field">
              <span>Source URL</span>
              <input
                onChange={(event) => setDraft((current) => ({ ...current, sourceUrl: event.target.value }))}
                value={draft.sourceUrl}
              />
            </label>

            <label className="admin-field">
              <span>Published date</span>
              <input
                onChange={(event) => setDraft((current) => ({ ...current, publishedAt: event.target.value }))}
                type="date"
                value={draft.publishedAt}
              />
            </label>

            <label className="admin-field">
              <span>Content</span>
              <textarea
                onChange={(event) => setDraft((current) => ({ ...current, content: event.target.value }))}
                rows={10}
                value={draft.content}
              />
            </label>

            {sanitizedSourceUrl && sanitizedSourceUrl !== '/' ? (
              <a className="dashboard-link admin-preview-link" href={sanitizedSourceUrl} rel="noreferrer" target="_blank">
                Preview source link
              </a>
            ) : null}

            <div className="admin-form-actions">
              <button className="landing-link landing-link--primary" disabled={saveMutation.isPending} type="submit">
                {saveMutation.isPending ? 'Saving…' : selectedNewsId ? 'Save changes' : 'Create news item'}
              </button>
              {selectedNewsId ? (
                <button
                  className="dashboard-link admin-action-button admin-action-button--danger"
                  disabled={deleteMutation.isPending}
                  onClick={() => {
                    if (window.confirm('This will soft-delete the news item. Continue?')) {
                      setNotice(null)
                      void deleteMutation.mutateAsync()
                    }
                  }}
                  type="button"
                >
                  Delete news item
                </button>
              ) : null}
            </div>
          </form>
        </article>
      </section>
    </AdminLayout>
  )
}
