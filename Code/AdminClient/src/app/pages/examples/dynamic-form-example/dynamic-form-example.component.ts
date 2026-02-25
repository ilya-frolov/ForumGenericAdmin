import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DynamicFormComponent } from '../../../components/dynamic-form/dynamic-form.component';
import { FormStructure } from '../../../interfaces/form-structure';
import {FormMode } from '../../../services/form-builder.service';
import { LoggerService } from 'src/app/services/logger.service';

@Component({
  selector: 'app-dynamic-form-example',
  standalone: true,
  imports: [
    CommonModule,
    DialogModule,
    ButtonModule,
    CardModule,
    DynamicFormComponent
  ],
  templateUrl: './dynamic-form-example.component.html',
})
export class DynamicFormExampleComponent {
  displayFormDialog = false;
  formStructure: FormStructure | null = null;
  formMode: FormMode = 'edit';
  loading = false;
  formResult: any = null;
  refId: string = null;
  formId: string = null;

  constructor(
    private logger: LoggerService,
  ) {}

  // Function to open the form dialog
  openFormDialog(): void {
    this.displayFormDialog = true;
    this.formResult = null;
  }


  // Handle form submission
  onFormSubmit(formData: any): void {
    this.formResult = formData;
    this.logger.info('Form submitted:', formData);
  }

  // Handle form cancel
  onFormCancel(): void {
    this.displayFormDialog = false;
    this.logger.info('Form cancelled');
  }

  // Switch form mode
  setFormMode(mode: FormMode): void {
    this.formMode = mode;
    this.formResult = null;
  }

} 