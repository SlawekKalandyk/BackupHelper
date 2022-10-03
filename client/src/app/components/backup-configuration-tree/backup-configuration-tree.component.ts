import {Component, EventEmitter, Inject, Input, OnInit, Output} from '@angular/core';
import {BackupConfiguration, BackupDirectory, BackupFile} from "../../openapi";
import {Observable} from "rxjs";
import {MatDialog} from "@angular/material/dialog";
import {
  AddBackupConfigNodeDialogComponent,
  AddType
} from "../add-backup-config-node-dialog/add-backup-config-node-dialog.component";
import {DOCUMENT} from "@angular/common";
import {CdkDrag, CdkDragDrop, CdkDragMove, CdkDropList} from "@angular/cdk/drag-drop";

export class BackupConfigurationNode {
  name?: string;
  filePath?: string;
  children: BackupConfigurationNode[] = [];
  parent?: BackupConfigurationNode;
  isExpanded: boolean = false;

  get displayName(): string {
    return (this.name ?? this.filePath)!;
  }

  private get idChunk(): string {
    return (this.name ?? this.filePath)!;
  }

  get id(): string {
    let id = this.idChunk;
    if (this.parent) {
      id = `${this.parent.id}.${id}`;
    }
    return id;
  }

  hasChildren() {
    return this.children && this.children.length > 0;
  }

  isBackupFile(): boolean {
    return this.filePath != null;
  }

  toBackupFile(): BackupFile {
    return {
      filePath: this.filePath
    }
  }

  static fromBackupFile(backupFile: BackupFile, parent?: BackupConfigurationNode): BackupConfigurationNode {
    let backupConfigurationNode = new BackupConfigurationNode();
    backupConfigurationNode.filePath = backupFile.filePath!;
    backupConfigurationNode.parent = parent;
    return backupConfigurationNode;
  }

  isBackupDirectory(): boolean {
    return this.name != null;
  }

  toBackupDirectory(): BackupDirectory {
    return {
      name: this.name,
      directories: this.children.filter(c => c.isBackupDirectory()).map(c => c.toBackupDirectory()),
      files: this.children.filter(c => c.isBackupFile()).map(c => c.toBackupFile())
    }
  }

  static fromBackupDirectory(backupDirectory: BackupDirectory, parent?: BackupConfigurationNode): BackupConfigurationNode {
    let backupConfigurationNode = new BackupConfigurationNode();
    backupConfigurationNode.name = backupDirectory.name!;
    backupConfigurationNode.parent = parent;
    backupConfigurationNode.children = [];
    if (backupDirectory.directories) {
      backupDirectory.directories.forEach(
        d => backupConfigurationNode.children.push(BackupConfigurationNode.fromBackupDirectory(d, backupConfigurationNode))
      );
    }
    if (backupDirectory.files) {
      backupDirectory.files.forEach(
        f => backupConfigurationNode.children.push(BackupConfigurationNode.fromBackupFile(f, backupConfigurationNode))
      );
    }
    return backupConfigurationNode;
  }
}

export interface DropInfo {
  targetId: string;
  action?: string;
}

@Component({
  selector: 'app-backup-configuration-tree',
  templateUrl: './backup-configuration-tree.component.html',
  styleUrls: ['./backup-configuration-tree.component.scss']
})
export class BackupConfigurationTreeComponent implements OnInit {
  @Input() backupConfiguration: Observable<BackupConfiguration>;
  @Output() backupConfigurationChange = new EventEmitter<BackupConfiguration>();

  dataSource: BackupConfigurationNode[] = [];

  dropTargetIds: string[] = [];
  nodeLookup: { [id: string]: BackupConfigurationNode } = {};
  dropActionTodo: DropInfo | null = null;

  constructor(@Inject(DOCUMENT) private document: Document, public dialog: MatDialog) {
  }

  ngOnInit(): void {
    this.backupConfiguration.subscribe(b => {
      let newNodes = this.backupConfigurationToNodes(b);
      this.transferBackupConfigurationNodesStates(this.dataSource, newNodes);
      this.resetDragDropCollections();
      this.prepareDragDrop(newNodes);
      this.dataSource = newNodes;
    });
  }

  resetDragDropCollections() {
    this.dropTargetIds = ['main'];
    this.nodeLookup = {};
  }

  prepareDragDrop(nodes: BackupConfigurationNode[]) {
    nodes.forEach(node => {
      this.dropTargetIds.push(node.id);
      this.nodeLookup[node.id] = node;
      this.prepareDragDrop(node.children);
    });
  }

  identifyNode(index: number, node: BackupConfigurationNode): any {
    return node.id;
  }

  backupConfigurationToNodes(backupConfiguration: BackupConfiguration): BackupConfigurationNode[] {
    let nodes: BackupConfigurationNode[] = [];
    if (backupConfiguration.files) {
      backupConfiguration.files.forEach(
        f => nodes.push(BackupConfigurationNode.fromBackupFile(f))
      );
    }
    if (backupConfiguration.directories) {
      backupConfiguration.directories.forEach(
        d => nodes.push(BackupConfigurationNode.fromBackupDirectory(d))
      );
    }
    return nodes;
  }

  nodesToBackupConfiguration(nodes: BackupConfigurationNode[]): BackupConfiguration {
    return {
      directories: nodes.filter(d => d.isBackupDirectory()).map(d => d.toBackupDirectory()),
      files: nodes.filter(d => d.isBackupFile()).map(d => d.toBackupFile())
    }
  }

  addFile(node?: BackupConfigurationNode) {
    this.openAddDialog(AddType.file, node);
  }

  addDirectory(node?: BackupConfigurationNode) {
    this.openAddDialog(AddType.directory, node);
  }

  addAfterDialog(filePath?: string, name?: string, node?: BackupConfigurationNode) {
    let newNode = new BackupConfigurationNode();
    newNode.filePath = filePath;
    newNode.name = name;
    newNode.parent = node;
    if (node) {
      node.children.push(newNode);
    } else {
      this.dataSource.push(newNode);
    }
    this.updateBackupConfiguration();
  }

  remove(node: BackupConfigurationNode) {
    if (node.parent) {
      this.removeNode(node.parent.children, node);
    } else {
      this.removeNode(this.dataSource, node);
    }
    this.updateBackupConfiguration();
  }

  removeNode(array: BackupConfigurationNode[], node: BackupConfigurationNode) {
    const index = array.indexOf(node, 0);
    if (index > -1) {
      array.splice(index, 1);
    }
  }

  openAddDialog(type: AddType, node?: BackupConfigurationNode) {
    const dialogRef = this.dialog.open(AddBackupConfigNodeDialogComponent, {
      width: '270px',
      data: {
        type: type
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (type === AddType.file) {
        this.addAfterDialog(result, undefined, node);
      } else if (type === AddType.directory) {
        this.addAfterDialog(undefined, result, node);
      }
    });
  }

  displayAdd(node: BackupConfigurationNode): string {
    if (node.isBackupDirectory()) {
      return 'inline';
    }
    return 'none';
  }

  updateBackupConfiguration() {
    this.backupConfigurationChange.emit(this.nodesToBackupConfiguration(this.dataSource));
  }

  transferBackupConfigurationNodesStates(oldNodes: BackupConfigurationNode[], newNodes: BackupConfigurationNode[]) {
    oldNodes.forEach(oldNode => {
      let newNode = newNodes.find(n => n.id === oldNode.id);
      if (newNode) {
        newNode.isExpanded = oldNode.isExpanded;
        if (oldNode.hasChildren() && newNode.hasChildren()) {
          this.transferBackupConfigurationNodesStates(oldNode.children, newNode.children);
        }
      }
    });
  }

  /* drag & drop */

  dragMoved(event: CdkDragMove) {
    let e = this.document.elementFromPoint(event.pointerPosition.x, event.pointerPosition.y);

    if (!e) {
      this.clearDragInfo();
      return;
    }
    let container = e.classList.contains("node-item") ? e : e.closest(".node-item");
    if (!container) {
      this.clearDragInfo();
      return;
    }
    this.dropActionTodo = {
      targetId: container.getAttribute("data-id")!
    };
    const targetRect = container.getBoundingClientRect();
    const oneThird = targetRect.height / 3;

    if (event.pointerPosition.y - targetRect.top < oneThird) {
      // before
      this.dropActionTodo["action"] = "before";
    } else if (event.pointerPosition.y - targetRect.top > 2 * oneThird) {
      // after
      this.dropActionTodo["action"] = "after";
    } else {
      // inside
      this.dropActionTodo["action"] = "inside";
    }
    this.showDragInfo();
  }


  drop(event: CdkDragDrop<any, any, BackupConfigurationNode>) {
    if (!this.dropActionTodo) return;

    // if target is a file and action is to drop item inside - don't do it
    if (this.nodeLookup[this.dropActionTodo.targetId].isBackupFile() && this.dropActionTodo.action === 'inside') {
      this.clearDragInfo(true);
      return;
    }

    const draggedItemId = event.item.data.id;
    const parentItemId = event.previousContainer.id;
    const targetListId = this.getParentNodeId(this.dropActionTodo.targetId, this.dataSource, 'main');

    const draggedItem = this.nodeLookup[draggedItemId];

    const oldItemContainer = parentItemId != 'main' ? this.nodeLookup[parentItemId].children : this.dataSource;
    const newContainer = targetListId != 'main' ? this.nodeLookup[targetListId!].children : this.dataSource;

    let i = oldItemContainer.findIndex(c => c.id === draggedItemId);
    oldItemContainer.splice(i, 1);

    switch (this.dropActionTodo.action) {
      case 'before':
      case 'after':
        const targetIndex = newContainer.findIndex(c => c.id === this.dropActionTodo!.targetId);
        if (this.dropActionTodo.action == 'before') {
          newContainer.splice(targetIndex, 0, draggedItem);
        } else {
          newContainer.splice(targetIndex + 1, 0, draggedItem);
        }
        break;

      case 'inside':
        this.nodeLookup[this.dropActionTodo.targetId].children.push(draggedItem)
        this.nodeLookup[this.dropActionTodo.targetId].isExpanded = true;
        break;
    }

    this.clearDragInfo(true)
    this.updateBackupConfiguration();
  }

  getParentNodeId(id: string, nodesToSearch: BackupConfigurationNode[], parentId: string): string | null {
    for (let node of nodesToSearch) {
      if (node.id == id) return parentId;
      let ret = this.getParentNodeId(id, node.children, node.id);
      if (ret) return ret;
    }
    return null;
  }

  showDragInfo() {
    this.clearDragInfo();
    if (this.dropActionTodo) {
      this.document.getElementById("node-" + this.dropActionTodo.targetId)!.classList.add("drop-" + this.dropActionTodo.action);
    }
  }

  clearDragInfo(dropped = false) {
    if (dropped) {
      this.dropActionTodo = null;
    }
    this.document
      .querySelectorAll(".drop-before")
      .forEach(element => element.classList.remove("drop-before"));
    this.document
      .querySelectorAll(".drop-after")
      .forEach(element => element.classList.remove("drop-after"));
    this.document
      .querySelectorAll(".drop-inside")
      .forEach(element => element.classList.remove("drop-inside"));
  }
}
