import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddBackupConfigNodeDialogComponent } from './add-backup-config-node-dialog.component';

describe('AddBackupConfigNodeDialogComponent', () => {
  let component: AddBackupConfigNodeDialogComponent;
  let fixture: ComponentFixture<AddBackupConfigNodeDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AddBackupConfigNodeDialogComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AddBackupConfigNodeDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
