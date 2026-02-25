import { Component, ElementRef, TemplateRef, ViewChild, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { EditorModule } from 'primeng/editor';
import { RichTextMetadata } from 'src/app/interfaces/field-metadata';
import { TabViewModule } from 'primeng/tabview';

@Component({
  selector: 'app-richtext-field',
  standalone: true,
  imports: [CommonModule, FormsModule, EditorModule, TabViewModule],
  templateUrl: './richtext-field.component.html',
})
export class RichTextFieldComponent extends BaseFieldComponent<any, RichTextMetadata> implements OnInit {

  // For multilingual content support
  isMultilingual = false;
  currentLanguage = 'en';
  languages: string[] = [];
  multilingualValues: Record<string, string> = {};

  override ngOnInit(): void {
    super.ngOnInit();
    this.checkIfMultilingual();
  }

  /**
   * Check if the value is multilingual (object with language keys)
   */
  private checkIfMultilingual(): void {
    // Handle case where value is an object with language keys
    if (this.value && typeof this.value === 'object' && !Array.isArray(this.value)) {
      this.isMultilingual = true;
      
      // Extract languages and values
      this.languages = Object.keys(this.value);
      
      // Initialize the multilingual values
      this.multilingualValues = { ...this.value };
      
      // Set default language
      if (this.languages.includes('en')) {
        this.currentLanguage = 'en';
      } else if (this.languages.length > 0) {
        this.currentLanguage = this.languages[0];
      }
    }
  }

  /**
   * Handle language tab change
   */
  onLanguageChange(langCode: string): void {
    this.currentLanguage = langCode;
  }

  /**
   * Handle multilingual value change
   */
  onMultilingualValueChange(langCode: string, htmlValue: string): void {
    this.multilingualValues[langCode] = htmlValue;
    this.onValueChange(this.multilingualValues);
  }

  /**
   * Add a new language
   */
  addLanguage(langCode: string): void {
    if (!this.languages.includes(langCode)) {
      this.languages.push(langCode);
      this.multilingualValues[langCode] = '';
      this.currentLanguage = langCode;
      this.onValueChange(this.multilingualValues);
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

  override getValueForServer(value: any): any {
    if (this.isMultilingual) {
      return this.multilingualValues;
    }
    return value || '';
  }

  override parseValueFromServer(value: any): any {
    if (value && typeof value === 'object' && !Array.isArray(value)) {
      // This is a multilingual value
      this.isMultilingual = true;
      return value;
    }
    return value || '';
  }

  /**
   * Get the current displayed content for the template
   */
  get displayValue(): string {
    if (this.isMultilingual) {
      return this.multilingualValues[this.currentLanguage] || '';
    }
    return this.value || '';
  }

  /**
   * Format multilingual content for display in list view
   */
  getMultilingualDisplayValue(): string {
    if (!this.isMultilingual || !this.multilingualValues) {
      return this.value || '-';
    }
    
    // Just show the default language or first available language
    if (this.multilingualValues['en']) {
      return this.multilingualValues['en'];
    }
    
    const keys = Object.keys(this.multilingualValues);
    if (keys.length > 0) {
      return this.multilingualValues[keys[0]];
    }
    
    return '-';
  }
} 