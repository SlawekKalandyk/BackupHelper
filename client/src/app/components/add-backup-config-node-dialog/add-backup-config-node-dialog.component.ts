import {Component, Inject, OnInit} from '@angular/core';
import {MAT_DIALOG_DATA, MatDialogRef} from "@angular/material/dialog";

export enum AddType {
  file,
  directory
}

export interface AddBackupConfigNodeDialogData {
  type: AddType;
}

@Component({
  selector: 'app-add-backup-config-node-dialog',
  templateUrl: './add-backup-config-node-dialog.component.html',
  styleUrls: ['./add-backup-config-node-dialog.component.scss']
})
export class AddBackupConfigNodeDialogComponent implements OnInit {
  result: string;

  constructor(
    public dialogRef: MatDialogRef<AddBackupConfigNodeDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: AddBackupConfigNodeDialogData,
  ) {
  }

  ngOnInit(): void {
  }

  get displayLabel(): string {
    return this.data.type === AddType.file ? "File path" : "Directory name";
  }

  onCancelClick() {
    this.dialogRef.close();
  }
}
