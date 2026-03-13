import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { EnterYourName } from './enter-your-name';
import { UserService } from '../../../core/services/user.service';

describe('EnterYourName', () => {
  let navigateByUrlSpy: jasmine.Spy;
  let setNameSpy: jasmine.Spy;

  function configure(returnUrl: string | null = null): void {
    navigateByUrlSpy = jasmine.createSpy('navigateByUrl');
    setNameSpy = jasmine.createSpy('setName');

    TestBed.configureTestingModule({
      imports: [EnterYourName],
      providers: [
        { provide: Router, useValue: { navigateByUrl: navigateByUrlSpy } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap(returnUrl ? { returnUrl } : {}),
            },
          },
        },
        { provide: UserService, useValue: { setName: setNameSpy } },
      ],
    });
  }

  it('shows validation error and does not navigate when name is shorter than 2 characters', () => {
    configure();
    const fixture = TestBed.createComponent(EnterYourName);
    const component = fixture.componentInstance;

    component.userName = ' A ';
    component.startBoard();

    expect(component.errorMessage).toBe('Name must be at least 2 characters');
    expect(setNameSpy).not.toHaveBeenCalled();
    expect(navigateByUrlSpy).not.toHaveBeenCalled();
  });

  it('trims the name, clears previous errors, stores it, and navigates to returnUrl', () => {
    configure('/boards/42');
    const fixture = TestBed.createComponent(EnterYourName);
    const component = fixture.componentInstance;

    component.errorMessage = 'Previous error';
    component.userName = '  Alice  ';
    component.startBoard();

    expect(component.errorMessage).toBe('');
    expect(setNameSpy).toHaveBeenCalledWith('Alice');
    expect(navigateByUrlSpy).toHaveBeenCalledWith('/boards/42');
  });

  it('navigates to /boards when returnUrl is not present', () => {
    configure();
    const fixture = TestBed.createComponent(EnterYourName);
    const component = fixture.componentInstance;

    component.userName = 'Bob';
    component.startBoard();

    expect(setNameSpy).toHaveBeenCalledWith('Bob');
    expect(navigateByUrlSpy).toHaveBeenCalledWith('/boards');
  });
});
