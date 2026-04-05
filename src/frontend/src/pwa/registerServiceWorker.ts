export function registerServiceWorker() {
  if (
    typeof window === 'undefined'
    || typeof navigator === 'undefined'
    || !('serviceWorker' in navigator)
  ) {
    return
  }

  window.addEventListener('load', () => {
    if (import.meta.env.VITE_ENABLE_PWA !== 'true') {
      void navigator.serviceWorker.getRegistrations().then((registrations) => {
        registrations.forEach((registration) => {
          void registration.unregister()
        })
      })
      return
    }

    void navigator.serviceWorker.register('/sw.js', { scope: '/' })
  })
}
