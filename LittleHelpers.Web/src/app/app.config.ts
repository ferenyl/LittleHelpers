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

const availableLangs: string[] = ['en', 'sv'];
const fallbackLang = 'en';

function detectLanguage(supportedLangs: readonly string[], defaultLang: string): string {
  const preferred = [
    ...(navigator.languages ?? []),
    navigator.language,
    document.documentElement.lang,
  ]
    .filter((value): value is string => Boolean(value))
    .map(value => value.split('-')[0]?.toLowerCase())
    .filter((value): value is string => Boolean(value));

  return preferred.find(lang => supportedLangs.includes(lang)) ?? defaultLang;
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([jwtInterceptor])),
    provideTransloco({
      config: {
        availableLangs,
        defaultLang: detectLanguage(availableLangs, fallbackLang),
        fallbackLang,
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader,
    }),
  ],
};
