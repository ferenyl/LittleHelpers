import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';
import { translocoTestingModule } from '../testing/transloco-testing';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App, translocoTestingModule()],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should not show navbar when not logged in', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const nav = fixture.nativeElement.querySelector('nav');
    expect(nav).toBeNull();
  });
});

