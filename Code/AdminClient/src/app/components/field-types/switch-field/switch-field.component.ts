import { Component, ElementRef, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { SwitchMetadata } from 'src/app/interfaces/field-metadata';
import { InputSwitchModule } from 'primeng/inputswitch';

@Component({
  selector: 'app-switch-field',
  standalone: true,
  imports: [CommonModule, FormsModule, InputSwitchModule],
  templateUrl: './switch-field.component.html',
})
export class SwitchFieldComponent extends BaseFieldComponent<boolean, SwitchMetadata> implements OnInit {

  constructor() {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
    
    // Initialize value if not set
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

  override get value(): boolean {
    return !!this._value;
  }

  override set value(value: boolean) {
    this._value = !!value;
  }
} 