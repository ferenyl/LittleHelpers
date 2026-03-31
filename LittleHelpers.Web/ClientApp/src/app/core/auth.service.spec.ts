import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'login', component: {} as any }]),
      ],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('isLoggedIn should be false when no token in localStorage', () => {
    expect(service.isLoggedIn()).toBe(false);
  });

  it('login stores token and sets isLoggedIn to true', () => {
    service.login('admin', 'pass').subscribe();
    http.expectOne(req => req.url.includes('/auth/login')).flush({
      token: 'fake.jwt.token',
      username: 'admin',
      userLevel: 'Parent',
    });

    expect(service.isLoggedIn()).toBe(true);
    expect(service.username()).toBe('admin');
    expect(service.userLevel()).toBe('Parent');
    expect(localStorage.getItem('lh_token')).toBe('fake.jwt.token');
  });

  it('logout clears token and sets isLoggedIn to false', () => {
    localStorage.setItem('lh_token', 'fake.jwt.token');
    localStorage.setItem('lh_username', 'admin');
    localStorage.setItem('lh_userlevel', 'Parent');

    service.logout();

    expect(service.isLoggedIn()).toBe(false);
    expect(service.username()).toBeNull();
    expect(localStorage.getItem('lh_token')).toBeNull();
  });

  it('getMenu calls correct endpoint', () => {
    service.getMenu().subscribe();
    const req = http.expectOne(r => r.url.includes('/menu'));
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
