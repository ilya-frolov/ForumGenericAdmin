import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DynamicFormDialogComponent } from '../../../components/dynamic-form-dialog/dynamic-form-dialog.component';
import { FormMode } from '../../../services/form-builder.service';
import { LoggerService } from 'src/app/services/logger.service';

@Component({
  selector: 'app-dynamic-form-dialog-example',
  standalone: true,
  imports: [
    CommonModule,
    ButtonModule,
    CardModule,
    DynamicFormDialogComponent
  ],
  templateUrl: './dynamic-form-dialog-example.component.html',
})
export class DynamicFormDialogExampleComponent {
  formDialogVisible = false;
  formMode: FormMode = 'edit';
  refId: string = null;
  formId: string = null;
  formResult: any = null;

  constructor(private logger: LoggerService) {}

  openFormDialog(): void {
    this.formDialogVisible = true;
  }

  onFormSubmit(formData: any): void {
    this.formResult = formData;
    this.logger.info('Form submitted:', formData);
  }

  onFormCancel(): void {
    this.logger.info('Form cancelled');
  }

  setFormMode(mode: FormMode): void {
    this.formMode = mode;
  }
}
