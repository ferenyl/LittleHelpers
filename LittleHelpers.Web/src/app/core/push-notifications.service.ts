import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { FirebaseApp } from 'firebase/app';
import type { MessagePayload, Messaging } from 'firebase/messaging';
import { AuthService, AuthSessionContext, AuthSessionObserver } from './auth.service';
import { environment } from '../../environments/environment';

interface FirebaseWebPushConfigurationDto {
  enabled: boolean;
  apiKey: string;
  authDomain: string;
  projectId: string;
  storageBucket: string;
  messagingSenderId: string;
  appId: string;
  vapidKey: string;
}

@Injectable({ providedIn: 'root' })
export class PushNotificationsService implements AuthSessionObserver {
  private readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);
  private readonly registrationTokenKey = 'lh_push_registration_token';
  private readonly subscriptionPath = `${environment.apiUrl}/notifications/subscriptions`;
  private readonly unsubscriptionPath = `${environment.apiUrl}/notifications/subscriptions/remove`;
  private readonly configPath = `${environment.apiUrl}/notifications/web-config`;

  private setupChain = Promise.resolve();
  private firebaseApp: FirebaseApp | null = null;
  private messaging: Messaging | null = null;
  private foregroundMessagesRegistered = false;
  private currentRegistrationToken = localStorage.getItem(this.registrationTokenKey);

  constructor() {
    this.auth.registerSessionObserver(this);

    if (this.auth.isLoggedIn()) {
      void this.queueEnable();
    }
  }

  afterLogin(_context: AuthSessionContext) {
    return this.queueEnable();
  }

  async beforeLogout(_context: AuthSessionContext) {
    const registrationToken = this.currentRegistrationToken;
    if (!registrationToken) {
      return;
    }

    try {
      await firstValueFrom(
        this.http.post<void>(this.unsubscriptionPath, {
          registrationToken,
        })
      );
    } catch (error) {
      console.error('Failed to remove web push subscription.', error);
    } finally {
      this.clearStoredRegistrationToken();
    }
  }

  private queueEnable() {
    this.setupChain = this.setupChain
      .then(() => this.enable())
      .catch(error => {
        console.error('Failed to enable web push notifications.', error);
      });

    return this.setupChain;
  }

  private async enable() {
    if (!(await this.isSupported())) {
      return;
    }

    const config = await firstValueFrom(
      this.http.get<FirebaseWebPushConfigurationDto>(this.configPath)
    );

    if (!config.enabled || Notification.permission === 'denied') {
      return;
    }

    const permission = Notification.permission === 'granted'
      ? 'granted'
      : await Notification.requestPermission();

    if (permission !== 'granted') {
      return;
    }

    const serviceWorkerRegistration = await navigator.serviceWorker.register('/firebase-messaging-sw.js');
    const [{ getApps, initializeApp }, messagingModule] = await Promise.all([
      import('firebase/app'),
      import('firebase/messaging'),
    ]);

    const supported = await messagingModule.isSupported();
    if (!supported) {
      return;
    }

    this.firebaseApp ??=
      getApps().find(candidate => candidate.name === 'littlehelpers-web-push')
      ?? initializeApp({
        apiKey: config.apiKey,
        authDomain: config.authDomain,
        projectId: config.projectId,
        storageBucket: config.storageBucket,
        messagingSenderId: config.messagingSenderId,
        appId: config.appId,
      }, 'littlehelpers-web-push');

    this.messaging ??= messagingModule.getMessaging(this.firebaseApp);

    if (!this.foregroundMessagesRegistered) {
      messagingModule.onMessage(this.messaging, payload => {
        void this.showForegroundNotification(serviceWorkerRegistration, payload);
      });
      this.foregroundMessagesRegistered = true;
    }

    const registrationToken = await messagingModule.getToken(this.messaging, {
      vapidKey: config.vapidKey,
      serviceWorkerRegistration,
    });

    if (!registrationToken) {
      return;
    }

    if (this.currentRegistrationToken && this.currentRegistrationToken !== registrationToken) {
      await this.unsubscribePreviousToken(this.currentRegistrationToken);
    }

    await firstValueFrom(
      this.http.post<void>(this.subscriptionPath, {
        registrationToken,
      })
    );

    this.currentRegistrationToken = registrationToken;
    localStorage.setItem(this.registrationTokenKey, registrationToken);
  }

  private async unsubscribePreviousToken(registrationToken: string) {
    try {
      await firstValueFrom(
        this.http.post<void>(this.unsubscriptionPath, {
          registrationToken,
        })
      );
    } catch (error) {
      console.error('Failed to replace web push subscription.', error);
    }
  }

  private clearStoredRegistrationToken() {
    this.currentRegistrationToken = null;
    localStorage.removeItem(this.registrationTokenKey);
  }

  private async showForegroundNotification(
    serviceWorkerRegistration: ServiceWorkerRegistration,
    payload: MessagePayload
  ) {
    const title = payload.notification?.title;
    if (!title) {
      return;
    }

    await serviceWorkerRegistration.showNotification(title, {
      body: payload.notification?.body,
      icon: payload.data?.['icon'] ?? '/icons/icon-192.png',
      data: {
        link: payload.fcmOptions?.link ?? payload.data?.['link'] ?? '/',
      },
    });
  }

  private async isSupported() {
    if (typeof window === 'undefined' || typeof Notification === 'undefined') {
      return false;
    }

    return 'serviceWorker' in navigator;
  }
}
