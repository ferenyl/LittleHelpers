import { Injectable, signal, effect } from '@angular/core';

export type Theme = 'light' | 'dark' | 'system';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly storageKey = 'lh_theme';
  private _theme = signal<Theme>(this.loadTheme());

  readonly theme = this._theme.asReadonly();

  constructor() {
    effect(() => this.applyTheme(this._theme()));

    // Re-apply when OS preference changes (only relevant in system mode)
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
      if (this._theme() === 'system') this.applyTheme('system');
    });
  }

  set(theme: Theme): void {
    localStorage.setItem(this.storageKey, theme);
    this._theme.set(theme);
  }

  private loadTheme(): Theme {
    const stored = localStorage.getItem(this.storageKey);
    if (stored === 'light' || stored === 'dark' || stored === 'system') return stored;
    return 'system';
  }

  private applyTheme(theme: Theme): void {
    const resolved = theme === 'system' ? this.getSystemPreference() : theme;
    document.documentElement.setAttribute('data-bs-theme', resolved);
  }

  private getSystemPreference(): 'light' | 'dark' {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
}
