import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { ChoreService, ChoreDto } from '../../core/chore.service';

@Component({
  selector: 'app-chores-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CommonModule, TranslocoModule],
  templateUrl: './chores-list.component.html',
  styleUrl: './chores-list.component.scss',
})
export class ChoresListComponent implements OnInit {
  private svc = inject(ChoreService);
  private transloco = inject(TranslocoService);
  chores = signal<ChoreDto[]>([]);
  loading = signal(true);

  ngOnInit() {
    this.load();
  }

  load() {
    this.svc.getAll().subscribe({ next: c => { this.chores.set(c); this.loading.set(false); } });
  }

  toggleHidden(chore: ChoreDto) {
    this.svc.update(chore.id, { isHidden: !chore.isHidden }).subscribe(() => this.load());
  }

  delete(id: number) {
    if (!confirm(this.transloco.translate('chores.confirmDelete'))) return;
    this.svc.delete(id).subscribe(() => this.chores.update(c => c.filter(x => x.id !== id)));
  }
}
