import {
  Component,
  ElementRef,
  OnInit,
  TemplateRef,
  ViewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { TooltipModule } from 'primeng/tooltip';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { Menu } from 'primeng/menu';
import { BaseFieldComponent } from '../base-field.component';
import { UrlMetadata } from 'src/app/interfaces/field-metadata';

interface UrlFieldValue {
  baseUrl?: string;
  urlName?: string;
}

@Component({
  selector: 'app-url-field',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    InputTextModule,
    ButtonModule,
    RippleModule,
    TooltipModule,
    MenuModule,
  ],
  templateUrl: './url-field.component.html',
})
export class UrlFieldComponent
  extends BaseFieldComponent<UrlFieldValue, UrlMetadata>
  implements OnInit
{
  menuItems: MenuItem[] = [];
  type: string = 'url';

  @ViewChild('menuBtn') menuButton: ElementRef;
  @ViewChild('menuBtn2') menuButton2: ElementRef;
  @ViewChild('menu') menu: Menu;
  @ViewChild('menu2') menu2: Menu;

  // Override the value property to trigger extractUrlParts when value changes
  override get value(): UrlFieldValue {
    return super.value;
  }

  override set value(val: UrlFieldValue) {
    super.value = val;
  }

  constructor() {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
    this.initMenuItems();
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
   * Initialize dropdown menu items
   */
  private initMenuItems(): void {
    this.menuItems = [
      {
        label: 'Copy URL',
        icon: 'pi pi-copy',
        command: (event) => this.copyToClipboard(event.originalEvent),
      },
      {
        label: 'Open in new tab',
        icon: 'pi pi-external-link',
        url: this.fullUrl,
        target: '_blank',
      },
    ];
  }



  /**
   * Handle URL path changes
   */
  onUrlPathChange(event: Event): void {
    const path = (event.target as HTMLInputElement).value;

    // Update the value object
    const newValue: UrlFieldValue = {
      baseUrl: this.value?.baseUrl,
      urlName: path,
    };

    this.value = newValue;
    this.onValueChange(newValue);

    // Update menu items with new URL
    this.initMenuItems();
  }

  /**
   * Handle base URL changes
   */
  onBaseUrlChange(event: Event): void {
    const baseUrl = (event.target as HTMLInputElement).value;

    // Update the value object
    const newValue: UrlFieldValue = {
      baseUrl: baseUrl,
      urlName: this.value?.urlName,
    };

    this.value = newValue;
    this.onValueChange(newValue);

    // Update menu items with new URL
    this.initMenuItems();
  }

  /**
   * Get the full URL including base URL
   */
  get fullUrl(): string {
    if (!this.value?.baseUrl && !this.value?.urlName) return '';


    const fullUrl = this.value.baseUrl + "/" + this.value.urlName;

    // Return the URL as is if it already has a protocol
    if (fullUrl.match(/^https?:\/\//)) {
      return fullUrl;
    }

    // Add http:// as default protocol if no protocol is present
    return fullUrl.includes('://') ? fullUrl : `http://${fullUrl}`;
  }

  /**
   * Calculate dynamic width for the URL path input
   */
  get urlPathWidth(): number {
    return Math.max((this.value?.urlName?.length || 0) + 5, 15);
  }

  /**
   * Calculate dynamic width for the base URL input
   */
  get baseUrlWidth(): number {
    return (this.value?.baseUrl?.length || 10) + 2;
  }

  /**
   * Copy URL to clipboard
   */
  copyToClipboard(event: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }

    if (!this.value?.baseUrl && !this.value?.urlName) return;

    const url = this.fullUrl;
    navigator.clipboard
      .writeText(url)
      .then(() => {
        // Could show a toast message here
        console.log('URL copied to clipboard:', url);
      })
      .catch((err) => {
        console.error('Could not copy URL to clipboard:', err);
      });
  }
}
