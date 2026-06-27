import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { ChoreDto } from './chore.service';

export interface ChildSummaryDto {
  id: number;
  username: string;
  totalPoints: number;
  monthlyAllowance: number | null;
  pointsGoal: number | null;
  assignedChores: ChoreDto[];
  links: { href: string; rel: string; method: string }[];
}

export interface ChoreLogDto {
  id: number;
  choreId: number;
  choreName: string;
  childId: number;
  performedBy: number;
  performedByName: string;
  points: number;
  timestamp: string;
}

export interface ChoreLogPeriodDto {
  logs: ChoreLogDto[];
  periodStartInclusive: string;
  periodEndExclusive: string;
}

@Injectable({ providedIn: 'root' })
export class ChildrenService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/children`;

  getAll() { return this.http.get<ChildSummaryDto[]>(this.base); }
  getById(id: number) { return this.http.get<ChildSummaryDto>(`${this.base}/${id}`); }

  getLogs(childId: number, year?: number, month?: number) {
    const query = year !== undefined && month !== undefined
      ? `?year=${year}&month=${month}`
      : '';

    return this.http.get<ChoreLogPeriodDto>(
      `${environment.apiUrl}/chorelog/${childId}${query}`
    );
  }

  deleteLog(logId: number) {
    return this.http.delete(`${environment.apiUrl}/chorelog/item/${logId}`);
  }
}
