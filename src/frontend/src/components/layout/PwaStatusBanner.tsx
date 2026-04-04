import { useState } from 'react'

import { useInstallPrompt } from '../../pwa/useInstallPrompt'
import { useOnlineStatus } from '../../pwa/useOnlineStatus'

export function PwaStatusBanner() {
  const isOnline = useOnlineStatus()
  const { canInstall, promptToInstall } = useInstallPrompt()
  const [isInstalling, setIsInstalling] = useState(false)

  if (isOnline && !canInstall) {
    return null
  }

  async function handleInstall() {
    setIsInstalling(true)

    try {
      await promptToInstall()
    } finally {
      setIsInstalling(false)
    }
  }

  return (
    <section className="pwa-banner" aria-live="polite">
      <div>
        <span className="section-kicker">{isOnline ? 'Installable app' : 'Offline mode'}</span>
        <strong>
          {isOnline
            ? 'Install Sniffle Report for faster repeat access.'
            : 'You are offline. Cached screens remain available while the network is down.'}
        </strong>
        <p>
          {isOnline
            ? 'Supported browsers can add this dashboard to the home screen and open it as a standalone app shell.'
            : 'Fresh API data will resume once connectivity returns.'}
        </p>
      </div>

      {canInstall ? (
        <button className="pwa-banner__action" disabled={isInstalling} onClick={() => void handleInstall()} type="button">
          {isInstalling ? 'Opening install prompt…' : 'Install app'}
        </button>
      ) : null}
    </section>
  )
}
