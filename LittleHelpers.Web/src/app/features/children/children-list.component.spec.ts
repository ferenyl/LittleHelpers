import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ChildrenListComponent } from './children-list.component';
import { ChildrenService, ChildSummaryDto } from '../../core/children.service';
import { translocoTestingModule } from '../../../testing/transloco-testing';
import { of } from 'rxjs';

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

describe('ChildrenListComponent', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChildrenListComponent, translocoTestingModule()],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should create', () => {
    const fixture = TestBed.createComponent(ChildrenListComponent);
    fixture.detectChanges();
    http.expectOne(r => r.url.includes('/children')).flush([]);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('calcPct returns correct percentage capped at 100', () => {
    const fixture = TestBed.createComponent(ChildrenListComponent);
    fixture.detectChanges();
    http.expectOne(r => r.url.includes('/children')).flush([]);
    const cmp = fixture.componentInstance;

    expect(cmp.calcPct(makeChild({ totalPoints: 10, pointsGoal: 25 }))).toBe(40);
    expect(cmp.calcPct(makeChild({ totalPoints: 30, pointsGoal: 25 }))).toBe(100);
    expect(cmp.calcPct(makeChild({ pointsGoal: null }))).toBe(0);
  });

  it('calcPayout rounds up to nearest krona', () => {
    const fixture = TestBed.createComponent(ChildrenListComponent);
    fixture.detectChanges();
    http.expectOne(r => r.url.includes('/children')).flush([]);
    const cmp = fixture.componentInstance;

    // 10/25 = 40% of 200 = 80
    expect(cmp.calcPayout(makeChild({ totalPoints: 10, pointsGoal: 25, monthlyAllowance: 200 }))).toBe(80);
    // 1/3 = 33% (calcPct rounds) of 100 = 33
    expect(cmp.calcPayout(makeChild({ totalPoints: 1, pointsGoal: 3, monthlyAllowance: 100 }))).toBe(33);
    // no allowance
    expect(cmp.calcPayout(makeChild({ monthlyAllowance: null }))).toBe(0);
  });

  it('calcPct returns 0 for negative points', () => {
    const fixture = TestBed.createComponent(ChildrenListComponent);
    fixture.detectChanges();
    http.expectOne(r => r.url.includes('/children')).flush([]);
    const cmp = fixture.componentInstance;

    expect(cmp.calcPct(makeChild({ totalPoints: -5, pointsGoal: 25 }))).toBe(0);
  });

  it('toggle sets expandedId', () => {
    const fixture = TestBed.createComponent(ChildrenListComponent);
    fixture.detectChanges();
    http.expectOne(r => r.url.includes('/children')).flush([]);
    const cmp = fixture.componentInstance;
    const svc = TestBed.inject(ChildrenService);
    vi.spyOn(svc, 'getById').mockReturnValue(of({ ...makeChild(), assignedChores: [] }));
    vi.spyOn(svc, 'getLogs').mockReturnValue(of({
      logs: [],
      periodStartInclusive: new Date('2026-05-27T00:00:00.000Z').toISOString(),
      periodEndExclusive: new Date('2026-06-27T00:00:00.000Z').toISOString(),
    }));

    const child = makeChild();
    cmp.toggle(child);
    expect(cmp.expandedId()).toBe(1);

    cmp.toggle(child);
    expect(cmp.expandedId()).toBeNull();
  });
});
