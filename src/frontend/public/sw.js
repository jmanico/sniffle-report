const CacheName = 'sniffle-report-v1';
const AppShellUrls = ['/', '/manifest.webmanifest', '/favicon.svg'];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CacheName).then((cache) => cache.addAll(AppShellUrls)),
  );
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(
        keys
          .filter((key) => key !== CacheName)
          .map((key) => caches.delete(key)),
      ),
    ),
  );
  self.clients.claim();
});

self.addEventListener('fetch', (event) => {
  const { request } = event;

  if (request.method !== 'GET') {
    return;
  }

  if (request.mode === 'navigate') {
    event.respondWith(
      fetch(request).catch(async () => {
        const cache = await caches.open(CacheName);
        return cache.match('/') || Response.error();
      }),
    );
    return;
  }

  const requestUrl = new URL(request.url);
  if (requestUrl.origin !== self.location.origin) {
    return;
  }

  event.respondWith(
    caches.match(request).then(async (cachedResponse) => {
      if (cachedResponse) {
        return cachedResponse;
      }

      const networkResponse = await fetch(request);
      const cache = await caches.open(CacheName);
      cache.put(request, networkResponse.clone());
      return networkResponse;
    }),
  );
});
