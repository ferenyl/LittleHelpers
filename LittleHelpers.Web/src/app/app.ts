import { Component, signal, inject, effect, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet, RouterLink, Router } from '@angular/router';
import { CommonModule, DOCUMENT } from '@angular/common';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthService, MenuItemDto } from './core/auth.service';
import { ThemeService, Theme } from './core/theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, CommonModule, TranslocoModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  auth = inject(AuthService);
  themeService = inject(ThemeService);
  private document = inject(DOCUMENT);
  private router = inject(Router);

  menuItems = signal<MenuItemDto[]>([]);
  themeMenuOpen = signal(false);
  appTitle = signal('LittleHelpers');

  constructor() {
    this.appTitle.set(this.document.title || 'LittleHelpers');

    effect(() => {
      if (this.auth.isLoggedIn()) {
        this.auth.getMenu().subscribe(items => this.menuItems.set(items));
      } else {
        this.menuItems.set([]);
      }
    });
  }

  logout() {
    this.auth.logout();
  }

  setTheme(theme: Theme) {
    this.themeService.set(theme);
    this.themeMenuOpen.set(false);
  }

  toggleThemeMenu(event: Event) {
    event.stopPropagation();
    this.themeMenuOpen.update(v => !v);
  }

  closeThemeMenu() {
    this.themeMenuOpen.set(false);
  }
}
