import { Component, DestroyRef, OnDestroy, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChildrenService, ChildSummaryDto, ChoreLogDto } from '../../core/children.service';
import { ChoreService } from '../../core/chore.service';
import { AuthService } from '../../core/auth.service';
import { RealtimeService } from '../../core/realtime.service';

interface DayPoint { day: number; points: number; cumulative: number; }
interface ChoreConfirmation { id: number; name: string; }
interface HistoryDeleteConfirmation { id: number; choreName: string; }

@Component({
  selector: 'app-child-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, TranslocoModule],
  templateUrl: './child-detail.component.html',
  styleUrl: './child-detail.component.scss',
})
export class ChildDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private childSvc = inject(ChildrenService);
  private choreSvc = inject(ChoreService);
  private auth = inject(AuthService);
  private transloco = inject(TranslocoService);
  private realtime = inject(RealtimeService);
  private destroyRef = inject(DestroyRef);

  child = signal<ChildSummaryDto | null>(null);
  logs = signal<ChoreLogDto[]>([]);
  loading = signal(true);
  historyExpanded = signal(false);
  completing = signal<number | null>(null);
  deletingHistoryId = signal<number | null>(null);
  pendingConfirmation = signal<ChoreConfirmation | null>(null);
  pendingHistoryDelete = signal<HistoryDeleteConfirmation | null>(null);

  now = new Date();
  year = signal(this.now.getFullYear());
  month = signal(this.now.getMonth() + 1);

  translatedMonth = computed(() => this.transloco.translate(`months.${this.month()}`));
  monthLabel = computed(() => `${this.translatedMonth()} ${this.year()}`);

  visibleHistory = computed(() =>
    this.historyExpanded() ? this.logs() : this.logs().slice(0, 5)
  );

  isParent = computed(() => this.auth.userLevel() === 'Parent');

  availableChores = computed(() => {
    const child = this.child();
    if (!child) {
      return [];
    }

    return child.assignedChores.filter(chore => {
      if (!chore.assignedUserIds.includes(this.childId)) {
        return false;
      }

      return this.isParent() || this.hasCompleteLink(chore);
    });
  });

  totalPoints = computed(() => this.logs().reduce((s, l) => s + l.points, 0));

  allowancePct = computed(() => {
    const c = this.child();
    if (!c?.pointsGoal) return null;
    return Math.min(Math.round((Math.max(this.totalPoints(), 0) / c.pointsGoal) * 100), 100);
  });

  allowancePayout = computed(() => {
    const c = this.child();
    const pct = this.allowancePct();
    if (pct === null || !c?.monthlyAllowance) return null;
    return Math.ceil((pct / 100) * c.monthlyAllowance);
  });

  chartPoints = computed<DayPoint[]>(() => {
    const daysInMonth = new Date(this.year(), this.month(), 0).getDate();
    const byDay: Record<number, number> = {};
    for (const log of this.logs()) {
      const d = new Date(log.timestamp).getDate();
      byDay[d] = (byDay[d] ?? 0) + log.points;
    }
    let cum = 0;
    return Array.from({ length: daysInMonth }, (_, i) => {
      cum += byDay[i + 1] ?? 0;
      return { day: i + 1, points: byDay[i + 1] ?? 0, cumulative: cum };
    });
  });

  svgPath = computed(() => {
    const pts = this.chartPoints();
    if (!pts.length) return '';
    const w = 600, h = 160, pad = 40;
    const maxVal = Math.max(...pts.map(p => p.cumulative), 1);
    const minVal = Math.min(...pts.map(p => p.cumulative), 0);
    const range = maxVal - minVal || 1;
    const x = (i: number) => pad + (i / (pts.length - 1)) * (w - pad - 20);
    const y = (v: number) => pad + (1 - (v - minVal) / range) * (h - pad * 2);
    return pts.map((p, i) => `${i === 0 ? 'M' : 'L'}${x(i)},${y(p.cumulative)}`).join(' ');
  });

  chartAxis = computed(() => {
    const pts = this.chartPoints();
    if (!pts.length) return { yTicks: [], xTicks: [] };
    const w = 600, h = 160, pad = 40;
    const maxVal = Math.max(...pts.map(p => p.cumulative), 1);
    const minVal = Math.min(...pts.map(p => p.cumulative), 0);
    const range = maxVal - minVal || 1;
    const toX = (i: number) => pad + (i / (pts.length - 1)) * (w - pad - 20);
    const toY = (v: number) => pad + (1 - (v - minVal) / range) * (h - pad * 2);

    const mid = Math.round((minVal + maxVal) / 2);
    const yTicks = [
      { value: maxVal, y: toY(maxVal) },
      { value: mid, y: toY(mid) },
      { value: minVal, y: toY(minVal) },
    ];

    const xTicks = pts
      .filter((_, i) => i === 0 || (pts[i].day % 5 === 0) || i === pts.length - 1)
      .map(p => ({ day: p.day, x: toX(pts.indexOf(p)) }));

    return { yTicks, xTicks };
  });

  get childId() { return +this.route.snapshot.paramMap.get('id')!; }

  ngOnInit() {
    this.loadChild(true);
    this.loadLogs();
    this.realtime.trackChild(this.childId);
    this.realtime.childUpdates$
      .pipe(
        filter(update => update.childId === this.childId),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.refreshFromServer());
  }

  ngOnDestroy() {
    this.realtime.untrackChild(this.childId);
  }

  loadChild(setLoading = false) {
    if (setLoading) {
      this.loading.set(true);
    }

    this.childSvc.getById(this.childId).subscribe({
      next: child => {
        this.child.set(child);
        if (setLoading) {
          this.loading.set(false);
        }
      },
      error: () => {
        if (setLoading) {
          this.loading.set(false);
        }
      },
    });
  }

  loadLogs() {
    this.childSvc.getLogs(this.childId, this.year(), this.month()).subscribe(l => this.logs.set(l));
  }

  prevMonth() {
    if (this.month() === 1) { this.month.set(12); this.year.update(y => y - 1); }
    else this.month.update(m => m - 1);
    this.loadLogs();
  }

  nextMonth() {
    if (this.month() === 12) { this.month.set(1); this.year.update(y => y + 1); }
    else this.month.update(m => m + 1);
    this.loadLogs();
  }

  complete(choreId: number) {
    const chore = this.availableChores().find(candidate => candidate.id === choreId);
    if (!chore) {
      return;
    }

    if (this.requiresTouchConfirmation()) {
      this.pendingConfirmation.set({ id: chore.id, name: chore.name });
      return;
    }

    this.completeNow(chore.id);
  }

  confirmMobileComplete() {
    const pending = this.pendingConfirmation();
    if (!pending) {
      return;
    }

    this.pendingConfirmation.set(null);
    this.completeNow(pending.id);
  }

  cancelMobileComplete() {
    this.pendingConfirmation.set(null);
  }

  deleteHistoryItem(log: ChoreLogDto) {
    if (!this.isParent()) {
      return;
    }

    if (this.deletingHistoryId() === log.id) {
      return;
    }

    this.pendingHistoryDelete.set({ id: log.id, choreName: log.choreName });
  }

  confirmHistoryDelete() {
    const pending = this.pendingHistoryDelete();
    if (!pending) {
      return;
    }

    this.pendingHistoryDelete.set(null);
    this.deletingHistoryId.set(pending.id);
    this.childSvc.deleteLog(pending.id).subscribe({
      next: () => {
        this.refreshFromServer();
        this.deletingHistoryId.set(null);
      },
      error: () => this.deletingHistoryId.set(null),
    });
  }

  cancelHistoryDelete() {
    this.pendingHistoryDelete.set(null);
  }

  private completeNow(choreId: number) {
    this.completing.set(choreId);
    this.choreSvc.complete(choreId, this.childId).subscribe({
      next: () => {
        this.completing.set(null);
        this.refreshFromServer();
      },
      error: () => this.completing.set(null),
    });
  }

  private refreshFromServer() {
    this.loadChild();
    this.loadLogs();
  }

  private requiresTouchConfirmation() {
    return window.matchMedia('(max-width: 768px)').matches;
  }

  hasCompleteLink(chore: { links: { rel: string }[] }) {
    return chore.links.some(link => link.rel === 'complete');
  }
}
