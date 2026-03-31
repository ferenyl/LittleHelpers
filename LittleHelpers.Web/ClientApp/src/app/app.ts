import { Component, OnInit, signal, inject, effect, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet, RouterLink, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthService, MenuItemDto } from './core/auth.service';

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
  private router = inject(Router);

  menuItems = signal<MenuItemDto[]>([]);

  constructor() {
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
}

