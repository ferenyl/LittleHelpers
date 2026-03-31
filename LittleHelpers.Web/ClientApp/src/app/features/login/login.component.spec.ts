import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { LoginComponent } from './login.component';
import { AuthService } from '../../core/auth.service';
import { translocoTestingModule } from '../../../testing/transloco-testing';
import { of, throwError } from 'rxjs';

describe('LoginComponent', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [LoginComponent, translocoTestingModule()],
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
    const fixture = TestBed.createComponent(LoginComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('submit button should be present', async () => {
    const fixture = TestBed.createComponent(LoginComponent);
    await fixture.whenStable();
    const btn = fixture.debugElement.query(By.css('button[type=submit]'));
    expect(btn).toBeTruthy();
  });

  it('should not call login when form is empty', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const auth = TestBed.inject(AuthService);
    const spy = vi.spyOn(auth, 'login');

    fixture.componentInstance.submit();

    expect(spy).not.toHaveBeenCalled();
  });

  it('should show error signal on failed login', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const auth = TestBed.inject(AuthService);
    vi.spyOn(auth, 'login').mockReturnValue(throwError(() => ({ status: 401 })));

    fixture.componentInstance.form.setValue({ username: 'bad', password: 'wrong' });
    fixture.componentInstance.submit();

    expect(fixture.componentInstance.error()).toBeTruthy();
  });

  it('should navigate to /children on successful Parent login', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const auth = TestBed.inject(AuthService);
    vi.spyOn(auth, 'login').mockReturnValue(of({
      token: 'head.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEifQ==.sig',
      username: 'admin',
      userLevel: 'Parent',
    }));

    const router = TestBed.inject(Router);
    const navSpy = vi.spyOn(router, 'navigate');

    fixture.componentInstance.form.setValue({ username: 'admin', password: 'pass' });
    fixture.componentInstance.submit();

    expect(navSpy).toHaveBeenCalledWith(['/children']);
  });
});
