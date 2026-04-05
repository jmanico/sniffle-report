import { useEffect, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'

import { createAdminResource, deleteAdminResource, updateAdminResource } from '../api/admin'
import { queryKeys } from '../api/queryKeys'
import { adminResourceInputSchema, type AdminResourceInput, type ResourceHours, type ResourceType } from '../api/types'
import { useAdminResource, useAdminResources } from '../hooks/useAdminContent'
import { useRegions } from '../hooks/useRegions'
import { type AdminNoticeState, getFirstZodIssue } from './AdminFormUtils'
import { AdminLayout, AdminNotice, RegionSelect } from './AdminShared'

const emptyHours: ResourceHours = {
  mon: null,
  tue: null,
  wed: null,
  thu: null,
  fri: null,
  sat: null,
  sun: null,
}

const emptyDraft: AdminResourceInput = {
  regionId: '',
  name: '',
  type: 'Clinic',
  address: '',
  phone: undefined,
  website: undefined,
  latitude: null,
  longitude: null,
  hours: emptyHours,
  services: [],
}

const dayLabels: Array<keyof ResourceHours> = ['mon', 'tue', 'wed', 'thu', 'fri', 'sat', 'sun']

export function AdminResourcesPage() {
  const queryClient = useQueryClient()
  const regionsQuery = useRegions({ page: 1, pageSize: 100 })
  const [selectedResourceId, setSelectedResourceId] = useState('')
  const [typeFilter, setTypeFilter] = useState<ResourceType | ''>('')
  const [nameFilter, setNameFilter] = useState('')
  const [page, setPage] = useState(1)
  const [serviceInput, setServiceInput] = useState('')
  const [draft, setDraft] = useState<AdminResourceInput>(emptyDraft)
  const [notice, setNotice] = useState<AdminNoticeState>(null)

  const resourcesQuery = useAdminResources({
    type: typeFilter || undefined,
    name: nameFilter.trim() || undefined,
    page,
    pageSize: 12,
  })
  const detailQuery = useAdminResource(selectedResourceId)

  useEffect(() => {
    if (!detailQuery.data) {
      return
    }

    // eslint-disable-next-line react-hooks/set-state-in-effect -- the editor draft must be replaced when a different resource loads
    setDraft({
      regionId: detailQuery.data.regionId,
      name: detailQuery.data.name,
      type: detailQuery.data.type,
      address: detailQuery.data.address,
      phone: detailQuery.data.phone ?? undefined,
      website: detailQuery.data.website ?? undefined,
      latitude: detailQuery.data.latitude,
      longitude: detailQuery.data.longitude,
      hours: detailQuery.data.hours,
      services: detailQuery.data.services,
    })
  }, [detailQuery.data])

  const saveMutation = useMutation({
    mutationFn: async () => {
      const parsed = adminResourceInputSchema.safeParse(draft)
      if (!parsed.success) {
        throw new Error(getFirstZodIssue(parsed.error))
      }

      if (selectedResourceId) {
        return updateAdminResource(selectedResourceId, parsed.data)
      }

      return createAdminResource(parsed.data)
    },
    onSuccess: async (saved) => {
      setNotice({
        tone: 'success',
        message: selectedResourceId ? 'Resource updated.' : 'Resource created.',
      })
      setSelectedResourceId(saved.id)
      await queryClient.invalidateQueries({ queryKey: ['admin', 'resources'] })
      await queryClient.invalidateQueries({ queryKey: queryKeys.adminResourceById(saved.id) })
    },
    onError: (error) => {
      setNotice({
        tone: 'error',
        message: error instanceof Error ? error.message : 'Unable to save this resource right now.',
      })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: async () => {
      if (!selectedResourceId) {
        return
      }

      const justification = window.prompt('Why are you deleting this resource?')
      if (!justification) {
        throw new Error('Deletion requires a justification.')
      }

      await deleteAdminResource(selectedResourceId, justification)
    },
    onSuccess: async () => {
      setNotice({
        tone: 'success',
        message: 'Resource deleted.',
      })
      setSelectedResourceId('')
      setDraft(emptyDraft)
      await queryClient.invalidateQueries({ queryKey: ['admin', 'resources'] })
    },
    onError: (error) => {
      setNotice({
        tone: 'error',
        message: error instanceof Error ? error.message : 'Unable to delete this resource right now.',
      })
    },
  })

  const totalCount = resourcesQuery.data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / 12))

  return (
    <AdminLayout
      body="Maintain the clinics, pharmacies, vaccination sites, and hospitals the public region pages rely on."
      kicker="Admin resources"
      title="Manage local resources"
      actions={
        <button
          className="dashboard-link admin-action-button"
          onClick={() => {
            setSelectedResourceId('')
            setDraft(emptyDraft)
            setServiceInput('')
            setNotice(null)
          }}
          type="button"
        >
          New resource
        </button>
      }
    >
      <AdminNotice notice={notice} />

      <section className="admin-grid">
        <article className="page-panel admin-list-panel">
          <div className="admin-panel-header">
            <div>
              <span className="section-kicker">Directory</span>
              <strong>Local resources</strong>
            </div>
            <div className="admin-filter-cluster">
              <label className="admin-inline-filter">
                <span>Search</span>
                <input
                  onChange={(event) => {
                    setNameFilter(event.target.value)
                    setPage(1)
                  }}
                  placeholder="Clinic or pharmacy name"
                  type="search"
                  value={nameFilter}
                />
              </label>
              <label className="admin-inline-filter">
                <span>Type</span>
                <select
                  onChange={(event) => {
                    setTypeFilter((event.target.value as ResourceType | '') || '')
                    setPage(1)
                  }}
                  value={typeFilter}
                >
                  <option value="">All</option>
                  <option value="Clinic">Clinic</option>
                  <option value="Pharmacy">Pharmacy</option>
                  <option value="VaccinationSite">Vaccination Site</option>
                  <option value="Hospital">Hospital</option>
                </select>
              </label>
            </div>
          </div>

          <div className="admin-list">
            {resourcesQuery.data?.items.map((resource) => (
              <button
                className={selectedResourceId === resource.id ? 'admin-list-item admin-list-item--active' : 'admin-list-item'}
                key={resource.id}
                onClick={() => setSelectedResourceId(resource.id)}
                type="button"
              >
                <span className="section-kicker">{resource.type}</span>
                <strong>{resource.name}</strong>
                <span className="admin-list-meta">{resource.address}</span>
              </button>
            ))}
            {!resourcesQuery.isLoading && resourcesQuery.data?.items.length === 0 ? (
              <div className="dashboard-empty">No resources match the current filters.</div>
            ) : null}
          </div>

          <div className="alerts-pagination">
            <span className="alerts-pagination__summary">
              Page {page} of {totalPages} · {totalCount} resources
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
              <span className="section-kicker">{selectedResourceId ? 'Edit resource' : 'Create resource'}</span>
              <strong>{selectedResourceId ? 'Update resource details' : 'Add a new local resource'}</strong>
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

            <div className="admin-two-column">
              <label className="admin-field">
                <span>Name</span>
                <input
                  onChange={(event) => setDraft((current) => ({ ...current, name: event.target.value }))}
                  value={draft.name}
                />
              </label>
              <label className="admin-field">
                <span>Type</span>
                <select
                  onChange={(event) => setDraft((current) => ({ ...current, type: event.target.value as ResourceType }))}
                  value={draft.type}
                >
                  <option value="Clinic">Clinic</option>
                  <option value="Pharmacy">Pharmacy</option>
                  <option value="VaccinationSite">Vaccination Site</option>
                  <option value="Hospital">Hospital</option>
                </select>
              </label>
            </div>

            <label className="admin-field">
              <span>Address</span>
              <input
                onChange={(event) => setDraft((current) => ({ ...current, address: event.target.value }))}
                value={draft.address}
              />
            </label>

            <div className="admin-two-column">
              <label className="admin-field">
                <span>Phone</span>
                <input
                  onChange={(event) => setDraft((current) => ({ ...current, phone: event.target.value || undefined }))}
                  value={draft.phone ?? ''}
                />
              </label>
              <label className="admin-field">
                <span>Website</span>
                <input
                  onChange={(event) => setDraft((current) => ({ ...current, website: event.target.value || undefined }))}
                  value={draft.website ?? ''}
                />
              </label>
            </div>

            <div className="admin-two-column">
              <label className="admin-field">
                <span>Latitude</span>
                <input
                  onChange={(event) =>
                    setDraft((current) => ({
                      ...current,
                      latitude: event.target.value ? Number(event.target.value) : null,
                    }))
                  }
                  step="0.000001"
                  type="number"
                  value={draft.latitude ?? ''}
                />
              </label>
              <label className="admin-field">
                <span>Longitude</span>
                <input
                  onChange={(event) =>
                    setDraft((current) => ({
                      ...current,
                      longitude: event.target.value ? Number(event.target.value) : null,
                    }))
                  }
                  step="0.000001"
                  type="number"
                  value={draft.longitude ?? ''}
                />
              </label>
            </div>

            <div className="admin-subpanel">
              <div className="admin-subpanel__header">
                <strong>Hours</strong>
              </div>
              <div className="admin-hours-grid">
                {dayLabels.map((day) => (
                  <label className="admin-field" key={day}>
                    <span>{day.toUpperCase()}</span>
                    <input
                      onChange={(event) =>
                        setDraft((current) => ({
                          ...current,
                          hours: {
                            ...current.hours,
                            [day]: event.target.value || null,
                          },
                        }))
                      }
                      value={draft.hours[day] ?? ''}
                    />
                  </label>
                ))}
              </div>
            </div>

            <div className="admin-subpanel">
              <div className="admin-subpanel__header">
                <strong>Services</strong>
              </div>
              <div className="admin-service-input">
                <input
                  onChange={(event) => setServiceInput(event.target.value)}
                  placeholder="Add a service"
                  value={serviceInput}
                />
                <button
                  className="dashboard-link admin-action-button"
                  onClick={() => {
                    const normalized = serviceInput.trim()
                    if (!normalized) {
                      return
                    }

                    setDraft((current) => ({ ...current, services: [...current.services, normalized] }))
                    setServiceInput('')
                  }}
                  type="button"
                >
                  Add service
                </button>
              </div>
              <div className="admin-chip-row">
                {draft.services.map((service, index) => (
                  <button
                    className="admin-chip"
                    key={`${service}-${index}`}
                    onClick={() =>
                      setDraft((current) => ({
                        ...current,
                        services: current.services.filter((_, itemIndex) => itemIndex !== index),
                      }))
                    }
                    type="button"
                  >
                    {service} ×
                  </button>
                ))}
              </div>
            </div>

            <div className="admin-form-actions">
              <button className="landing-link landing-link--primary" disabled={saveMutation.isPending} type="submit">
                {saveMutation.isPending ? 'Saving…' : selectedResourceId ? 'Save changes' : 'Create resource'}
              </button>
              {selectedResourceId ? (
                <button
                  className="dashboard-link admin-action-button admin-action-button--danger"
                  disabled={deleteMutation.isPending}
                  onClick={() => {
                    if (window.confirm('This will delete the resource record. Continue?')) {
                      setNotice(null)
                      void deleteMutation.mutateAsync()
                    }
                  }}
                  type="button"
                >
                  Delete resource
                </button>
              ) : null}
            </div>
          </form>
        </article>
      </section>
    </AdminLayout>
  )
}
