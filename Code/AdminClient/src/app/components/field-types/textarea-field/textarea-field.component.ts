import { Component, ElementRef, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { TextMetadata } from 'src/app/interfaces/field-metadata';
import { TextareaModule } from 'primeng/textarea';

@Component({
  selector: 'app-textarea-field',
  standalone: true,
  imports: [CommonModule, FormsModule, TextareaModule],
  templateUrl: './textarea-field.component.html',
})
export class TextAreaFieldComponent extends BaseFieldComponent<string, TextMetadata> implements OnInit {

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
} 