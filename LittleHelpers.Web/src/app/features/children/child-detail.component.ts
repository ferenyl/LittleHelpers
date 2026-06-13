import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { ChildrenService, ChildSummaryDto, ChoreLogDto } from '../../core/children.service';
import { ChoreService } from '../../core/chore.service';
import { AuthService } from '../../core/auth.service';

interface DayPoint { day: number; points: number; cumulative: number; }

@Component({
  selector: 'app-child-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, TranslocoModule],
  templateUrl: './child-detail.component.html',
  styleUrl: './child-detail.component.scss',
})
export class ChildDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private childSvc = inject(ChildrenService);
  private choreSvc = inject(ChoreService);
  private auth = inject(AuthService);
  private transloco = inject(TranslocoService);

  child = signal<ChildSummaryDto | null>(null);
  logs = signal<ChoreLogDto[]>([]);
  loading = signal(true);
  historyExpanded = signal(false);
  completing = signal<number | null>(null);

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
    this.childSvc.getById(this.childId).subscribe(c => { this.child.set(c); this.loading.set(false); });
    this.loadLogs();
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
    this.completing.set(choreId);
    this.choreSvc.complete(choreId, this.childId).subscribe({
      next: () => { this.completing.set(null); this.loadLogs(); },
      error: () => this.completing.set(null),
    });
  }

  hasCompleteLink(chore: { links: { rel: string }[] }) {
    return chore.links.some(link => link.rel === 'complete');
  }
}
