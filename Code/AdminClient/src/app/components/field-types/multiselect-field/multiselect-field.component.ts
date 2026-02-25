import { Component, ElementRef, Input, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { MultiSelectFieldMetadata } from 'src/app/interfaces/field-metadata';
import { MultiSelectModule } from 'primeng/multiselect';



@Component({
  selector: 'app-multiselect-field',
  standalone: true,
  imports: [CommonModule, FormsModule, MultiSelectModule],
  templateUrl: './multiselect-field.component.html',
})
export class MultiSelectFieldComponent extends BaseFieldComponent<any[], MultiSelectFieldMetadata> implements OnInit {

  override ngOnInit(): void {
    super.ngOnInit();

    // Ensure value is an array for multi-select
    if (!Array.isArray(this.value)) {
      this.value = this.value ? [this.value] : [];
    }
  }

  override ngAfterViewInit(): void {
    super.ngAfterViewInit();
    if (!this.metadata.options) {
      this.metadata.options = [];
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

  override getValueForServer(value: any[]): any[] {
    return value || [];
  }

  override parseValueFromServer(value: any[]): any[] {
    // Ensure we return an array - the server should send an array of values
    return Array.isArray(value) ? value : [];
  }

  /**
   * Get the selected options for display
   */
  getSelectedOptions(): Array<{display: string, value: any}> {
    if (!this.value || !Array.isArray(this.value) || !this.metadata?.options) {
      return [];
    }
    return this.metadata.options.filter(option => this.value.includes(option.value));
  }
} 