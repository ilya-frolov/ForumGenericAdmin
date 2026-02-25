import { Component, ElementRef, Input, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { RadioButtonMetadata } from 'src/app/interfaces/field-metadata';
import { RadioButtonModule } from 'primeng/radiobutton';

@Component({
  selector: 'app-radio-button-field',
  standalone: true,
  imports: [CommonModule, FormsModule, RadioButtonModule],
  templateUrl: './radio-button-field.component.html',
})
export class RadioButtonFieldComponent extends BaseFieldComponent<string, RadioButtonMetadata> implements OnInit {

  constructor() {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
  }

  override getEditTemplate(): TemplateRef<ElementRef> {
    return this.editTemplateRef;
  }

  override getListTemplate(): TemplateRef<ElementRef> {
    return this.listTemplateRef;
  }

  override getViewTemplate(): TemplateRef<ElementRef> {
    return this.viewTemplateRef;
  }

  /**
   * Get the display label for the selected value
   */
  getDisplayLabel(value: string): string {
    if (!value || !this.metadata?.options) {
      return '';
    }
    
    const option = this.metadata.options.find(opt => opt.value === value);
    return option ? option.display : value;
  }
} 