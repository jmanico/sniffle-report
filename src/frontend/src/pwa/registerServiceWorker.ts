export function registerServiceWorker() {
  if (
    typeof window === 'undefined'
    || typeof navigator === 'undefined'
    || !('serviceWorker' in navigator)
  ) {
    return
  }

  window.addEventListener('load', () => {
    void navigator.serviceWorker.register('/sw.js', { scope: '/' })
  })
}
