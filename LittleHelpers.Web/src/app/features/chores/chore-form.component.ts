import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { ChoreService } from '../../core/chore.service';
import { UserService, UserDto } from '../../core/user.service';

@Component({
  selector: 'app-chore-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, CommonModule, RouterLink, TranslocoModule],
  templateUrl: './chore-form.component.html',
  styleUrl: './chore-form.component.scss',
})
export class ChoreFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private svc = inject(ChoreService);
  private userSvc = inject(UserService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private transloco = inject(TranslocoService);

  editId = signal<number | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  users = signal<UserDto[]>([]);

  parents = computed(() => this.users().filter(u => u.userLevel === 'Parent'));
  children = computed(() => this.users().filter(u => u.userLevel === 'Child'));
  forParents = signal(false);

  form = this.fb.group({
    name: ['', Validators.required],
    points: [1, Validators.required],
    assignedUserIds: [[] as number[]],
    maxTimesPerDay: [null as number | null],
    minDaysBetween: [null as number | null],
    maxTimesPerWeek: [null as number | null],
  });

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.editId.set(+id);
      forkJoin({ users: this.userSvc.getAll(), chore: this.svc.getById(+id) }).subscribe(({ users, chore }) => {
        this.users.set(users);
        this.form.patchValue({
          name: chore.name,
          points: chore.points,
          assignedUserIds: chore.assignedUserIds,
          maxTimesPerDay: chore.maxTimesPerDay,
          minDaysBetween: chore.minDaysBetween,
          maxTimesPerWeek: chore.maxTimesPerWeek,
        });
        const parentIds = users.filter(u => u.userLevel === 'Parent').map(u => u.id);
        this.forParents.set((chore.assignedUserIds as number[]).some(id => parentIds.includes(id)));
      });
    } else {
      this.userSvc.getAll().subscribe(u => this.users.set(u));
    }
  }

  isAssigned(userId: number): boolean {
    return (this.form.value.assignedUserIds ?? []).includes(userId);
  }

  toggleUser(userId: number) {
    const current = this.form.value.assignedUserIds ?? [];
    const updated = current.includes(userId)
      ? current.filter(id => id !== userId)
      : [...current, userId];
    this.form.patchValue({ assignedUserIds: updated });
  }

  toggleForParents(checked: boolean) {
    this.forParents.set(checked);
    const parentIds = this.parents().map(p => p.id);
    const current = this.form.value.assignedUserIds ?? [];
    const withoutParents = current.filter(id => !parentIds.includes(id));
    this.form.patchValue({ assignedUserIds: checked ? [...withoutParents, ...parentIds] : withoutParents });
  }

  submit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    const { name, points, assignedUserIds, maxTimesPerDay, minDaysBetween, maxTimesPerWeek } = this.form.value;
    const id = this.editId();

    const obs = id
      ? this.svc.update(id, { name: name!, points: points!, assignedUserIds: assignedUserIds!, maxTimesPerDay, minDaysBetween, maxTimesPerWeek })
      : this.svc.create({ name: name!, points: points!, assignedUserIds: assignedUserIds!, maxTimesPerDay, minDaysBetween, maxTimesPerWeek });

    obs.subscribe({
      next: () => this.router.navigate(['/chores']),
      error: () => { this.loading.set(false); this.error.set(this.transloco.translate('common.error')); },
    });
  }
}
