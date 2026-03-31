import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { authGuard, parentGuard } from './auth.guard';
import { AuthService } from './auth.service';

function runGuard(guard: typeof authGuard) {
  return TestBed.runInInjectionContext(() => guard({} as any, {} as any));
}

describe('authGuard', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
  });

  it('returns true when logged in', () => {
    localStorage.setItem('lh_token', 'some.jwt');
    const result = runGuard(authGuard);
    expect(result).toBe(true);
  });

  it('returns UrlTree to /login when not logged in', () => {
    const result = runGuard(authGuard);
    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/login');
  });
});

describe('parentGuard', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
  });

  it('returns true when logged in as Parent', () => {
    localStorage.setItem('lh_token', 'some.jwt');
    localStorage.setItem('lh_userlevel', 'Parent');
    const result = runGuard(parentGuard);
    expect(result).toBe(true);
  });

  it('returns UrlTree to /login when logged in as Child', () => {
    localStorage.setItem('lh_token', 'some.jwt');
    localStorage.setItem('lh_userlevel', 'Child');
    const result = runGuard(parentGuard);
    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/login');
  });

  it('returns UrlTree to /login when not logged in', () => {
    const result = runGuard(parentGuard);
    expect(result).toBeInstanceOf(UrlTree);
  });
});
