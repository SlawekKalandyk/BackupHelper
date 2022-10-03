import {ComponentFixture, TestBed} from '@angular/core/testing';

import {BackupConfigurationTreeComponent} from './backup-configuration-tree.component';

describe('BackupConfigurationTreeComponent', () => {
  let component: BackupConfigurationTreeComponent;
  let fixture: ComponentFixture<BackupConfigurationTreeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BackupConfigurationTreeComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BackupConfigurationTreeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
