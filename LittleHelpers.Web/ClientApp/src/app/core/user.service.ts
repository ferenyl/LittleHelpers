import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface UserDto {
  id: number;
  username: string;
  userLevel: string;
  monthlyAllowance: number | null;
  pointsGoal: number | null;
  links: { href: string; rel: string; method: string }[];
}

export interface CreateUserRequest { username: string; password: string; userLevel: string; monthlyAllowance?: number; pointsGoal?: number; }
export interface UpdateUserRequest { password?: string; userLevel?: string; monthlyAllowance?: number; pointsGoal?: number; }

@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/users`;

  getAll() { return this.http.get<UserDto[]>(this.base); }
  getById(id: number) { return this.http.get<UserDto>(`${this.base}/${id}`); }
  create(req: CreateUserRequest) { return this.http.post<UserDto>(this.base, req); }
  update(id: number, req: UpdateUserRequest) { return this.http.put<UserDto>(`${this.base}/${id}`, req); }
  delete(id: number) { return this.http.delete(`${this.base}/${id}`); }
}
