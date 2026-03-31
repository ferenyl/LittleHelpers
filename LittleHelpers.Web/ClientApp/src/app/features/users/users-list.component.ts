import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { UserService, UserDto } from '../../core/user.service';

@Component({
  selector: 'app-users-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CommonModule],
  templateUrl: './users-list.component.html',
  styleUrl: './users-list.component.scss',
})
export class UsersListComponent implements OnInit {
  private svc = inject(UserService);
  users = signal<UserDto[]>([]);
  loading = signal(true);

  ngOnInit() {
    this.svc.getAll().subscribe({ next: u => { this.users.set(u); this.loading.set(false); } });
  }

  delete(id: number) {
    if (!confirm('Ta bort användaren?')) return;
    this.svc.delete(id).subscribe(() => this.users.update(u => u.filter(x => x.id !== id)));
  }
}
