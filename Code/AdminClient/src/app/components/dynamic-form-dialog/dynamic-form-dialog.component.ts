import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { DynamicFormComponent } from '../dynamic-form/dynamic-form.component';
import { FormMode } from '../../services/form-builder.service';
import { LoggerService } from 'src/app/services/logger.service';
import { FormStructure } from 'src/app/interfaces/form-structure';

@Component({
  selector: 'app-dynamic-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    DialogModule,
    ButtonModule,
    DynamicFormComponent
  ],
  templateUrl: './dynamic-form-dialog.component.html',
})
export class DynamicFormDialogComponent {
  @Input() visible = false;
  @Input() dialogHeader = 'Dynamic Form';
  @Input() formId: string = null;
  @Input() formMode: FormMode = 'edit';
  @Input() width = '90vw';
  @Input() maxWidth = '1200px';
  @Input() isBackgroundShouldBeBlockedWhenDialogIsDisplayed = true;
  @Input() isCloseOnEscape = true;
  @Input() isDismissableMask = true;
  @Input() isDraggable = true;
  @Input() isResizable = true;
  @Input() structure: FormStructure;

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() formSubmit = new EventEmitter<any>();
  @Output() formCancel = new EventEmitter<void>();

  loading = false;
  formResult: any = null;

  constructor(private logger: LoggerService) {}


  // Handle dialog visibility
  onDialogHide(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }

  // Handle form submission
  onFormSubmit(formData: any): void {
    this.formResult = formData;
    this.formSubmit.emit(formData);
    this.logger.info('Form submitted:', formData);
  }

  // Handle form cancel
  onFormCancel(): void {
    this.formCancel.emit();
    this.visible = false;
    this.visibleChange.emit(false);
    this.logger.info('Form cancelled');
  }

  // Public method to open the dialog
  open(): void {
    this.visible = true;
    this.visibleChange.emit(true);
    this.formResult = null;
  }

  // Public method to close the dialog
  close(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }
}
