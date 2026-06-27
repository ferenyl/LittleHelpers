import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, Subject } from 'rxjs';
import { ChildDetailComponent } from './child-detail.component';
import { ChildrenService, ChildSummaryDto } from '../../core/children.service';
import { ChoreService } from '../../core/chore.service';
import { AuthService } from '../../core/auth.service';
import { RealtimeService } from '../../core/realtime.service';
import { translocoTestingModule } from '../../../testing/transloco-testing';

const makeChild = (overrides: Partial<ChildSummaryDto> = {}): ChildSummaryDto => ({
  id: 1,
  username: 'Alice',
  totalPoints: 10,
  monthlyAllowance: 200,
  pointsGoal: 25,
  assignedChores: [],
  links: [],
  ...overrides,
});

describe('ChildDetailComponent', () => {
  it('loads the current cycle on first render and syncs the displayed month to the cycle start', async () => {
    const childUpdates$ = new Subject<{ childId: number }>();
    const childrenService = {
      getById: vi.fn().mockReturnValue(of(makeChild())),
      getLogs: vi.fn().mockReturnValue(of({
        logs: [],
        periodStartInclusive: '2026-06-27T00:00:00.000Z',
        periodEndExclusive: '2026-07-27T00:00:00.000Z',
      })),
      deleteLog: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [ChildDetailComponent, translocoTestingModule()],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '1' }) } } },
        { provide: ChildrenService, useValue: childrenService },
        { provide: ChoreService, useValue: { complete: vi.fn() } },
        { provide: AuthService, useValue: { userLevel: signal<'Parent' | 'Child'>('Child') } },
        {
          provide: RealtimeService,
          useValue: {
            childUpdates$,
            trackChild: vi.fn(),
            untrackChild: vi.fn(),
          },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ChildDetailComponent);
    fixture.detectChanges();

    expect(childrenService.getLogs).toHaveBeenCalledWith(1);
    expect(fixture.componentInstance.year()).toBe(2026);
    expect(fixture.componentInstance.month()).toBe(6);
  });
});
