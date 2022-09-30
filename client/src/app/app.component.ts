import {Component, OnInit} from '@angular/core';
import {BackupConfiguration, BackupService} from "./openapi";
import {BehaviorSubject, Observable} from "rxjs";

const TREE_DATA = JSON.stringify(
  {
    "directories": [
      {
        "name": "dir11",
        "directories": [
          {
            "name": "dir12",
            "directories": [
            ],
            "files": [
              {
                "filePath": "E:\\Programming\\tests\\dir2"
              }
            ]
          }
        ],
        "files": [
          {
            "filePath": "E:\\Programming\\tests\\dir1"
          }
        ]
      }
    ],
    "files": [
      {
        "filePath": "E:\\Programming\\tests\\file1.txt"
      }
    ]
  }
);

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = 'BackupHelper';
  userInput: string;

  _backupConfiguration: BehaviorSubject<BackupConfiguration> = new BehaviorSubject<BackupConfiguration>({})
  get backupConfiguration(): Observable<BackupConfiguration> {
    return this._backupConfiguration.asObservable();
  }

  constructor(private backupService: BackupService) {
  }

  ngOnInit(): void {
    this.userInput = TREE_DATA;
    this._backupConfiguration.next(JSON.parse(this.userInput));
  }

  input(event: Event) {
    this._backupConfiguration.next(JSON.parse(this.userInput));
  }

  onBackupConfigurationChange(backupConfiguration: BackupConfiguration) {
    this.userInput = JSON.stringify(backupConfiguration);
    this._backupConfiguration.next(backupConfiguration);
  }
}
