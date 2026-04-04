import { useEffect, useState } from 'react'

import type { BeforeInstallPromptEvent } from './installPrompt'

export function useInstallPrompt() {
  const [installEvent, setInstallEvent] = useState<BeforeInstallPromptEvent | null>(null)

  useEffect(() => {
    if (typeof window === 'undefined') {
      return
    }

    const handleBeforeInstallPrompt = (event: Event) => {
      event.preventDefault()
      setInstallEvent(event as BeforeInstallPromptEvent)
    }

    const handleAppInstalled = () => {
      setInstallEvent(null)
    }

    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt)
    window.addEventListener('appinstalled', handleAppInstalled)

    return () => {
      window.removeEventListener('beforeinstallprompt', handleBeforeInstallPrompt)
      window.removeEventListener('appinstalled', handleAppInstalled)
    }
  }, [])

  async function promptToInstall() {
    if (!installEvent) {
      return false
    }

    await installEvent.prompt()
    const choice = await installEvent.userChoice
    setInstallEvent(null)
    return choice.outcome === 'accepted'
  }

  return {
    canInstall: installEvent !== null,
    promptToInstall,
  }
}
