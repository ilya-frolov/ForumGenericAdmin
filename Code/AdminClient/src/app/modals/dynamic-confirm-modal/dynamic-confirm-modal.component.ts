import {Component, inject, Input} from '@angular/core';
import {DialogStructure, ListDef} from "../../interfaces/table-models";
import {DynamicDialogRef} from "primeng/dynamicdialog";

@Component({
  selector: 'app-dynamic-confirm-modal',
  standalone: false,
  providers: [],
  templateUrl: './dynamic-confirm-modal.component.html'
})
export class DynamicConfirmModalComponent {

  private ref: DynamicDialogRef = inject(DynamicDialogRef);

  @Input() dialog: DialogStructure;
  @Input() listDef: ListDef;

  public model: any;

  public close(): void {
    this.ref.close({
      result: false
    });
  }

  public submit(): void {
    this.ref.close({
      result: true,
      data: this.model
    });
  }
}
