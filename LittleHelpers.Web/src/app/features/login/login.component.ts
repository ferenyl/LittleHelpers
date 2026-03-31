import { Component, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { CommonModule } from '@angular/common';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, CommonModule, TranslocoModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private transloco = inject(TranslocoService);

  form = this.fb.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });

  error = signal<string | null>(null);
  loading = signal(false);

  submit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);

    const { username, password } = this.form.value;
    this.auth.login(username!, password!).subscribe({
      next: res => {
        this.loading.set(false);
        if (res.userLevel === 'Parent') {
          this.router.navigate(['/children']);
        } else {
          this.router.navigate(['/children', this.extractUserId()]);
        }
      },
      error: () => {
        this.loading.set(false);
        this.error.set(this.transloco.translate('login.error'));
      },
    });
  }

  private extractUserId(): string {
    const token = this.auth.token();
    if (!token) return '';
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? '';
    } catch {
      return '';
    }
  }
}
