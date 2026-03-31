import { ApplicationConfig, isDevMode, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { routes } from './app.routes';
import { jwtInterceptor } from './core/jwt.interceptor';
import { provideTransloco, TranslocoLoader } from '@jsverse/transloco';
import { Injectable } from '@angular/core';
import { inject } from '@angular/core';

@Injectable({ providedIn: 'root' })
class TranslocoHttpLoader implements TranslocoLoader {
  private http = inject(HttpClient);
  getTranslation(lang: string) {
    return this.http.get<Record<string, unknown>>(`/i18n/${lang}.json`);
  }
}

function detectLanguage(): string {
  const browserLang = navigator.language?.split('-')[0]?.toLowerCase();
  return browserLang === 'sv' ? 'sv' : 'en';
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([jwtInterceptor])),
    provideTransloco({
      config: {
        availableLangs: ['en', 'sv'],
        defaultLang: detectLanguage(),
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader,
    }),
  ],
};
