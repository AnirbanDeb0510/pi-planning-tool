import { TestBed } from '@angular/core/testing';
import { Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { userNameGuard } from './user-name.guard';
import { UserService } from '../services/user.service';

describe('userNameGuard', () => {
  const route = {} as any;

  function runGuard(url: string): unknown {
    const state = { url } as RouterStateSnapshot;
    return TestBed.runInInjectionContext(() => userNameGuard(route, state));
  }

  it('returns true when user has name', () => {
    const userServiceMock = {
      hasName: jasmine.createSpy().and.returnValue(true),
    };

    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: UserService, useValue: userServiceMock }],
    });

    const result = runGuard('/board/1');

    expect(result).toBeTrue();
  });

  it('redirects to /name with returnUrl when user has no name', () => {
    const userServiceMock = {
      hasName: jasmine.createSpy().and.returnValue(false),
    };

    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: UserService, useValue: userServiceMock }],
    });

    const result = runGuard('/boards/42');

    expect(result instanceof UrlTree).toBeTrue();
    const urlTree = result as UrlTree;

    expect(urlTree.root.children['primary']?.segments.map((segment) => segment.path)).toEqual([
      'name',
    ]);
    expect(urlTree.queryParams['returnUrl']).toBe('/boards/42');
  });
});
