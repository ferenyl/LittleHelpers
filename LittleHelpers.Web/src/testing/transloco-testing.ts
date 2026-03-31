import { TranslocoTestingModule, TranslocoTestingOptions } from '@jsverse/transloco';
import en from '../../public/i18n/en.json';

export function translocoTestingModule(options: TranslocoTestingOptions = {}) {
  return TranslocoTestingModule.forRoot({
    langs: { en },
    translocoConfig: { defaultLang: 'en', availableLangs: ['en'] },
    preloadLangs: true,
    ...options,
  });
}
