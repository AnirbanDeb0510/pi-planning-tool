import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EnterYourName } from './enter-your-name';

describe('EnterYourName', () => {
  let component: EnterYourName;
  let fixture: ComponentFixture<EnterYourName>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EnterYourName]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EnterYourName);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
