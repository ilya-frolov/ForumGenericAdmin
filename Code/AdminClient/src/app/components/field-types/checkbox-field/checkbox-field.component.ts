import { Component, ElementRef, Input, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { CheckboxMetadata } from 'src/app/interfaces/field-metadata';
import { CheckboxModule } from 'primeng/checkbox';

@Component({
  selector: 'app-checkbox-field',
  standalone: true,
  imports: [CommonModule, FormsModule, CheckboxModule],
  templateUrl: './checkbox-field.component.html',
})
export class CheckboxFieldComponent extends BaseFieldComponent<boolean, CheckboxMetadata> implements OnInit {

  constructor() {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
    // Default value to false if undefined
    if (this.value === undefined || this.value === null) {
      this.value = false;
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

} 