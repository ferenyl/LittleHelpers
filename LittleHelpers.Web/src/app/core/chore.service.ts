import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface ChoreDto {
  id: number;
  name: string;
  points: number;
  isHidden: boolean;
  assignedUserIds: number[];
  links: { href: string; rel: string; method: string }[];
}

export interface CreateChoreRequest { name: string; points: number; assignedUserIds: number[]; }
export interface UpdateChoreRequest { name?: string; points?: number; isHidden?: boolean; assignedUserIds?: number[]; }

@Injectable({ providedIn: 'root' })
export class ChoreService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/chores`;

  getAll() { return this.http.get<ChoreDto[]>(this.base); }
  getById(id: number) { return this.http.get<ChoreDto>(`${this.base}/${id}`); }
  create(req: CreateChoreRequest) { return this.http.post<ChoreDto>(this.base, req); }
  update(id: number, req: UpdateChoreRequest) { return this.http.put<ChoreDto>(`${this.base}/${id}`, req); }
  delete(id: number) { return this.http.delete(`${this.base}/${id}`); }
  complete(id: number, targetChildId?: number) {
    const params = targetChildId != null ? `?targetChildId=${targetChildId}` : '';
    return this.http.post(`${this.base}/${id}/complete${params}`, {});
  }
}
