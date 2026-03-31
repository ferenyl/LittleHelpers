import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { UserService } from '../../core/user.service';

@Component({
  selector: 'app-user-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, CommonModule, RouterLink],
  templateUrl: './user-form.component.html',
  styleUrl: './user-form.component.scss',
})
export class UserFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private svc = inject(UserService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  editId = signal<number | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  form = this.fb.group({
    username: ['', Validators.required],
    password: [''],
    userLevel: ['Child', Validators.required],
    monthlyAllowance: this.fb.control<number | null>(null),
    pointsGoal: this.fb.control<number | null>(null),
  });

  get isChild() { return this.form.get('userLevel')!.value === 'Child'; }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.editId.set(+id);
      this.form.get('username')!.disable();
      this.svc.getById(+id).subscribe(u => {
        this.form.patchValue({
          userLevel: u.userLevel,
          monthlyAllowance: u.monthlyAllowance,
          pointsGoal: u.pointsGoal,
        });
      });
    } else {
      this.form.get('password')!.addValidators(Validators.required);
      this.form.get('password')!.updateValueAndValidity();
    }
  }

  submit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    const id = this.editId();
    const { username, password, userLevel, monthlyAllowance, pointsGoal } = this.form.getRawValue();
    const allowance = monthlyAllowance != null ? monthlyAllowance : undefined;
    const goal = pointsGoal != null ? pointsGoal : undefined;

    const obs = id
      ? this.svc.update(id, { password: password || undefined, userLevel: userLevel!, monthlyAllowance: allowance, pointsGoal: goal })
      : this.svc.create({ username: username!, password: password!, userLevel: userLevel!, monthlyAllowance: allowance, pointsGoal: goal });

    obs.subscribe({
      next: () => this.router.navigate(['/users']),
      error: () => { this.loading.set(false); this.error.set('Något gick fel.'); },
    });
  }
}
