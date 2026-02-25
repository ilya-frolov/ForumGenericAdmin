import { Component, ElementRef, Input, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { TextMetadata } from 'src/app/interfaces/field-metadata';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';

@Component({
  selector: 'app-password-field',
  standalone: true,
  imports: [CommonModule, FormsModule, InputTextModule, TooltipModule],
  templateUrl: './password-field.component.html',
})
export class PasswordFieldComponent extends BaseFieldComponent<string, TextMetadata> implements OnInit {
  
  showPassword = false;

  constructor() {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
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