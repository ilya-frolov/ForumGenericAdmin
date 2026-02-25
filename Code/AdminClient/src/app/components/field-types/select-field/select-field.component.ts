import { Component, ElementRef, Input, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { SelectMetadata } from 'src/app/interfaces/field-metadata';
import { DropdownModule } from 'primeng/dropdown';
import { MultiSelectModule } from 'primeng/multiselect';

@Component({
  selector: 'app-select-field',
  standalone: true,
  imports: [CommonModule, FormsModule, DropdownModule, MultiSelectModule],
  templateUrl: './select-field.component.html',
})
export class SelectFieldComponent extends BaseFieldComponent<any, SelectMetadata> implements OnInit {

  constructor() {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
    
    // Initialize multi-select value as array if needed
    if (this.metadata?.multiSelect && !Array.isArray(this.value)) {
      this.value = this.value ? [this.value] : [];
    }
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
   * Get the display label for a value
   */
  getDisplayLabel(value: any): string {
    if (value === null || value === undefined) {
      return '';
    }
    
    const option = (this.metadata?.options || []).find(opt => opt.value === value);
    return option ? option.display : value.toString();
  }
} 