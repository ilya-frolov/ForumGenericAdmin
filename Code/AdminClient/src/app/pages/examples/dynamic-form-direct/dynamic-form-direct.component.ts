import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { DynamicFormComponent } from '../../../components/dynamic-form/dynamic-form.component';
import { FormStructure, NodeStructure, NodeType } from '../../../interfaces/form-structure';
import { FormMode } from '../../../services/form-builder.service';
import { LoggerService } from 'src/app/services/logger.service';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-dynamic-form-direct',
  standalone: true,
  imports: [
    CommonModule,
    CardModule,
    ButtonModule,
    DynamicFormComponent,
    ReactiveFormsModule
  ],
  templateUrl: './dynamic-form-direct.component.html'
})
export class DynamicFormDirectComponent implements OnInit {
  formStructure: FormStructure;
  formModes: FormMode[] = ['edit', 'view', 'list'];
  formMode: FormMode = 'edit';
  formResult: any = null;
  formGroup: FormGroup;

  constructor(private logger: LoggerService, private fb: FormBuilder) {}

  async ngOnInit(): Promise<void> {
    await this.loadFormExample();
  }

  /**
   * Load the form example structure
   */
  private async loadFormExample(): Promise<void> {
    try {
      // We'll load the example form structure directly
      const response = await fetch('/assets/examples/form-example.json');
      this.formStructure = await response.json();
      this.logger.debug('Form structure loaded:', this.formStructure);
    } catch (error) {
      this.logger.error('Error loading form example:', error);
    }
  }

  /**
   * Switch form mode
   */
  setFormMode(mode: FormMode): void {
    this.formMode = mode;
    this.formResult = null;
  }

  /**
   * Handle form submission
   */
  onFormSubmit(event: any): void {
    this.formResult = event.formData;
    this.logger.info('Form submitted:', event);
  }

  /**
   * Handle form cancel
   */
  onFormCancel(): void {
    this.formResult = null;
    this.logger.info('Form cancelled');
  }
} 