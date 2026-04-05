export type AdminNoticeState =
  | {
      tone: 'success' | 'error'
      message: string
    }
  | null

export function toInputDate(isoDateTime: string) {
  return isoDateTime.slice(0, 10)
}

export function getFirstZodIssue(error: {
  flatten: () => { fieldErrors: Record<string, string[] | undefined>; formErrors: string[] }
}) {
  const flattened = error.flatten()

  for (const key of Object.keys(flattened.fieldErrors)) {
    const messages = flattened.fieldErrors[key]
    if (messages?.length) {
      return messages[0]
    }
  }

  return flattened.formErrors[0] ?? 'Please review the form and try again.'
}
