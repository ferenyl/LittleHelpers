import { Routes } from '@angular/router';
import { authGuard, loginGuard, parentGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  {
    path: 'login',
    canActivate: [loginGuard],
    loadComponent: () => import('./features/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'users',
    canActivate: [parentGuard],
    children: [
      { path: '', loadComponent: () => import('./features/users/users-list.component').then(m => m.UsersListComponent) },
      { path: 'new', loadComponent: () => import('./features/users/user-form.component').then(m => m.UserFormComponent) },
      { path: ':id/edit', loadComponent: () => import('./features/users/user-form.component').then(m => m.UserFormComponent) },
    ],
  },
  {
    path: 'chores',
    canActivate: [parentGuard],
    children: [
      { path: '', loadComponent: () => import('./features/chores/chores-list.component').then(m => m.ChoresListComponent) },
      { path: 'new', loadComponent: () => import('./features/chores/chore-form.component').then(m => m.ChoreFormComponent) },
      { path: ':id/edit', loadComponent: () => import('./features/chores/chore-form.component').then(m => m.ChoreFormComponent) },
    ],
  },
  {
    path: 'children',
    canActivate: [authGuard],
    children: [
      { path: '', canActivate: [parentGuard], loadComponent: () => import('./features/children/children-list.component').then(m => m.ChildrenListComponent) },
      { path: ':id', loadComponent: () => import('./features/children/child-detail.component').then(m => m.ChildDetailComponent) },
    ],
  },
  { path: '**', redirectTo: '/login' },
];
