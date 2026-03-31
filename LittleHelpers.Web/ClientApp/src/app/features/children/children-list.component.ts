import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ChildrenService, ChildSummaryDto, ChoreLogDto } from '../../core/children.service';
import { ChoreService, ChoreDto } from '../../core/chore.service';

interface DayPoint { day: number; cumulative: number; }
interface ChartAxis { yTicks: { value: number; y: number }[]; xTicks: { day: number; x: number }[] }
interface ExpandedData {
  logs: ChoreLogDto[];
  parentChores: ChoreDto[];
  svgPath: string;
  chartAxis: ChartAxis;
  completing: number | null;
  loading: boolean;
}

@Component({
  selector: 'app-children-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink],
  templateUrl: './children-list.component.html',
  styleUrl: './children-list.component.scss',
})
export class ChildrenListComponent implements OnInit {
  private svc = inject(ChildrenService);
  private choreSvc = inject(ChoreService);

  children = signal<ChildSummaryDto[]>([]);
  loading = signal(true);
  expandedId = signal<number | null>(null);
  expandedData = signal<Map<number, ExpandedData>>(new Map());

  ngOnInit() {
    this.svc.getAll().subscribe({ next: c => { this.children.set(c); this.loading.set(false); } });
  }

  toggle(child: ChildSummaryDto) {
    if (this.expandedId() === child.id) { this.expandedId.set(null); return; }
    this.expandedId.set(child.id);
    if (this.expandedData().has(child.id)) return;

    this.patchExpanded(child.id, { logs: [], parentChores: [], svgPath: '', chartAxis: { yTicks: [], xTicks: [] }, completing: null, loading: true });

    const now = new Date();
    forkJoin({
      detail: this.svc.getById(child.id),
      logs: this.svc.getLogs(child.id, now.getFullYear(), now.getMonth() + 1),
    }).subscribe(({ detail, logs }) => {
      const parentChores = detail.assignedChores.filter(c => !c.assignedUserIds.includes(child.id));
      this.patchExpanded(child.id, { logs, parentChores, completing: null, loading: false, ...this.buildChart(logs, now) });
    });
  }

  completeParentChore(choreId: number, child: ChildSummaryDto) {
    this.patchExpanded(child.id, { completing: choreId });
    this.choreSvc.complete(choreId, child.id).subscribe({
      next: () => {
        const now = new Date();
        this.svc.getLogs(child.id, now.getFullYear(), now.getMonth() + 1).subscribe(logs => {
          const totalPoints = logs.reduce((s, l) => s + l.points, 0);
          this.children.update(list => list.map(c => c.id === child.id ? { ...c, totalPoints } : c));
          this.patchExpanded(child.id, { logs, completing: null, ...this.buildChart(logs, now) });
        });
      },
      error: () => this.patchExpanded(child.id, { completing: null }),
    });
  }

  getExpanded(childId: number): ExpandedData | undefined {
    return this.expandedData().get(childId);
  }

  private patchExpanded(childId: number, patch: Partial<ExpandedData>) {
    const map = new Map(this.expandedData());
    map.set(childId, { ...(map.get(childId) ?? { logs: [], parentChores: [], svgPath: '', chartAxis: { yTicks: [], xTicks: [] }, completing: null, loading: false }), ...patch });
    this.expandedData.set(map);
  }

  private buildChart(logs: ChoreLogDto[], now: Date): { svgPath: string; chartAxis: ChartAxis } {
    const year = now.getFullYear(), month = now.getMonth() + 1;
    const daysInMonth = new Date(year, month, 0).getDate();
    const byDay: Record<number, number> = {};
    for (const log of logs) { const d = new Date(log.timestamp).getDate(); byDay[d] = (byDay[d] ?? 0) + log.points; }
    let cum = 0;
    const pts: DayPoint[] = Array.from({ length: daysInMonth }, (_, i) => { cum += byDay[i + 1] ?? 0; return { day: i + 1, cumulative: cum }; });

    const w = 600, h = 120, pad = 40;
    const maxVal = Math.max(...pts.map(p => p.cumulative), 1);
    const minVal = Math.min(...pts.map(p => p.cumulative), 0);
    const range = maxVal - minVal || 1;
    const toX = (i: number) => pad + (i / (pts.length - 1)) * (w - pad - 20);
    const toY = (v: number) => pad + (1 - (v - minVal) / range) * (h - pad * 2);

    const svgPath = pts.map((p, i) => `${i === 0 ? 'M' : 'L'}${toX(i)},${toY(p.cumulative)}`).join(' ');
    const mid = Math.round((minVal + maxVal) / 2);
    const yTicks = [{ value: maxVal, y: toY(maxVal) }, { value: mid, y: toY(mid) }, { value: minVal, y: toY(minVal) }];
    const xTicks = pts.filter((_, i) => i === 0 || pts[i].day % 5 === 0 || i === pts.length - 1).map(p => ({ day: p.day, x: toX(pts.indexOf(p)) }));

    return { svgPath, chartAxis: { yTicks, xTicks } };
  }

  calcPct(child: ChildSummaryDto): number {
    if (!child.pointsGoal) return 0;
    return Math.min(Math.round((Math.max(child.totalPoints, 0) / child.pointsGoal) * 100), 100);
  }

  calcPayout(child: ChildSummaryDto): number {
    if (!child.monthlyAllowance || !child.pointsGoal) return 0;
    return Math.ceil((this.calcPct(child) / 100) * child.monthlyAllowance);
  }
}
