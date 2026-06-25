self.addEventListener('notificationclick', event => {
  const link = event.notification?.data?.link;
  event.notification.close();

  if (!link) {
    return;
  }

  event.waitUntil((async () => {
    const matchingClients = await self.clients.matchAll({
      type: 'window',
      includeUncontrolled: true,
    });

    for (const client of matchingClients) {
      if ('focus' in client && client.url === new URL(link, self.location.origin).toString()) {
        await client.focus();
        return;
      }
    }

    if (self.clients.openWindow) {
      await self.clients.openWindow(link);
    }
  })());
});

(async () => {
  try {
    const response = await fetch('/api/notifications/web-config', {
      cache: 'no-store',
    });

    if (!response.ok) {
      return;
    }

    const config = await response.json();
    if (!config.enabled) {
      return;
    }

    importScripts('https://www.gstatic.com/firebasejs/12.4.0/firebase-app-compat.js');
    importScripts('https://www.gstatic.com/firebasejs/12.4.0/firebase-messaging-compat.js');

    if (!self.firebase.apps.length) {
      self.firebase.initializeApp({
        apiKey: config.apiKey,
        authDomain: config.authDomain,
        projectId: config.projectId,
        storageBucket: config.storageBucket,
        messagingSenderId: config.messagingSenderId,
        appId: config.appId,
      });
    }

    const messaging = self.firebase.messaging();
    messaging.onBackgroundMessage(payload => {
      const title = payload.notification?.title ?? 'LittleHelpers';
      const body = payload.notification?.body ?? '';
      const link = payload.fcmOptions?.link ?? payload.data?.link ?? '/';
      const icon = payload.data?.icon ?? '/icons/icon-192.png';

      void self.registration.showNotification(title, {
        body,
        icon,
        data: { link },
      });
    });
  } catch (error) {
    console.error('Firebase messaging service worker failed to initialize.', error);
  }
})();
