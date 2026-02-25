import {
  Component,
  OnInit,
  Input,
  Output,
  EventEmitter,
  AfterViewInit,
  AfterContentInit,
  ChangeDetectorRef,
  OnDestroy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { FormStructure, NodeStructure, NodeType } from '../../interfaces/form-structure';
import { DynamicNodeComponent } from '../dynamic-node/dynamic-node.component';
import {
  FormBuilderService,
  FormMode,
} from '../../services/form-builder.service';
import { TabViewModule } from 'primeng/tabview';
import { ButtonModule } from 'primeng/button';
import { LoggerService } from 'src/app/services/logger.service';
import { formAnimations } from '../dynamic-node/dynamic-node.animations';
import { AppService } from 'src/app/services/app.service';
import { Subscription } from 'rxjs';
import { UploadProgressDialogComponent } from '../upload-progress-dialog/upload-progress-dialog.component';

@Component({
  selector: 'app-dynamic-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    DynamicNodeComponent,
    TabViewModule,
    ButtonModule,
    UploadProgressDialogComponent
  ],
  templateUrl: './dynamic-form.component.html',
  animations: formAnimations,
})
export class DynamicFormComponent implements OnInit, AfterContentInit, AfterViewInit, OnDestroy {
  @Input() formMode: FormMode = 'edit';
  @Input() isShowCancelButton: boolean = true;
  @Input() formStructure: FormStructure;

  @Output() formSubmit = new EventEmitter<any>();
  @Output() formCancel = new EventEmitter<void>();

  // Track initialization of fields
  private initializedFields: Set<string> = new Set();
  private totalFields: number = 0;
  private uploadingSubscription: Subscription;

  nodeStructure: NodeStructure;
  tabs: any[] = [];
  activeTabIndex = 0;
  hasTabs = false;
  loading = false;
  nonTabContent: any[] = [];
  isSubmitting = false;
  submitSuccess = false;
  submitError: string | null = null;
  modelData: any = null;
  refId: string;
  formGroup: FormGroup;

  constructor(
    private formBuilderService: FormBuilderService,
    private logger: LoggerService,
    private cdr: ChangeDetectorRef,
    private appService: AppService
  ) {}

  ngOnDestroy(): void {
    if (this.uploadingSubscription) {
      this.uploadingSubscription.unsubscribe();
    }
  }

  ngAfterContentInit(): void {
    // Register for field initialization events from DynamicNodeComponent
    // This will be done through communication with form builder service
  }

  ngAfterViewInit(): void {
    // At this point, all child components and directives have been initialized
    // Update the form with any changes from child components
    this.formGroup = this.formBuilderService.getFormGroup();
    
  }

  async ngOnInit(): Promise<void> {
    // Initialize the form
    this.formGroup = this.formBuilderService.getFormGroup();
    if (this.formStructure) {
      this.initForm(this.formStructure);
    }
    
    // Monitor file upload progress
    this.uploadingSubscription = this.appService.uploadingInProgress$.subscribe(
      isUploading => {
        this.isSubmitting = isUploading;
      }
    );
  }

  /**
   * Handle field initialized event from a child field
   */
  onFieldInitialized(fieldPath: string): void {
    this.initializedFields.add(fieldPath);
    this.logger.debug(`Field initialized: ${fieldPath}, Total: ${this.initializedFields.size}/${this.totalFields}`);
    
    // Check if all fields have been initialized
    if (this.totalFields > 0 && this.initializedFields.size >= this.totalFields) {
      this.logger.debug('All fields initialized, updating form');
      this.updateFormAfterAllFieldsInitialized();
    }
  }

  /**
   * Update form after all fields have been initialized
   */
  private updateFormAfterAllFieldsInitialized(): void {
    // Refresh form from the form builder service
    this.formGroup = this.formBuilderService.getFormGroup();
    this.cdr.detectChanges();
    this.logger.debug('Form updated after all fields initialized:', this.formGroup.value);
  }

  /**
   * Initialize the reactive form
   */
  private initForm(formStructure: FormStructure): void {
    if (!formStructure) {
      return;
    }
    this.nodeStructure = formStructure.structure;

    // Extract tabs if any
    this.extractTabs();

    // Check for content outside tabs
    this.checkForContentOutsideTabs();

    
    // Count total fields for initialization tracking
    this.countTotalFields(this.nodeStructure);
  }

  /**
   * Count total fields in the form structure
   */
  private countTotalFields(node: any): void {
    if (!node) return;
    
    if (node.nodeType === 'field') {
      this.totalFields++;
    }
    
    if (node.children && Array.isArray(node.children)) {
      node.children.forEach((child: any) => {
        this.countTotalFields(child);
      });
    }
  }

  /**
   * Extract tabs from the structure
   */
  private extractTabs(): void {
    if (this.nodeStructure.children) {
      // Filter out the tab nodes
      this.tabs = this.nodeStructure.children.filter(
        (child) => child.nodeType === NodeType.TAB
      );

      this.hasTabs = this.tabs.length > 0;

      if (this.hasTabs) {
        // Default to first tab
        this.activeTabIndex = 0;
      }
    }
  }


  /**
   * Check if there is content outside of tabs and log a warning
   */
  private checkForContentOutsideTabs(): void {
    if (this.hasTabs && this.nodeStructure.children) {
      // Find any nodes that are not tabs
      this.nonTabContent = this.nodeStructure.children.filter(
        (child) => child.nodeType !== NodeType.TAB
      );
    }
  }

  /**
   * Set the active tab
   */
  setActiveTab(index: number): void {
    if (index >= 0 && index < this.tabs.length) {
      this.activeTabIndex = index;
    }
  }

  /**
   * Handle form submission
   */
  onSubmit(): void {
    if (this.formMode === 'view' || this.formMode === 'list' || this.isSubmitting) {
      return;
    }

    // Reset previous submission state
    this.submitSuccess = false;
    this.submitError = null;

    if (this.formGroup.valid) {
      // Set submitting state
      this.isSubmitting = true;
      
      // Emit form data for submission
      // The app.service will handle file uploads and show progress
      this.formSubmit.emit(
        this.formGroup.value
      );
      this.isSubmitting = false;
    } else {
      this.markFormGroupTouched(this.formGroup);
    }
  }

  /**
   * Handle cancel action
   */
  onCancel(): void {
    // const formGroup = this.formBuilderService.getFormGroup();
    // const value = formGroup.value;
    // const errors = Object.values(formGroup.controls).filter(
    //   (control) => control.invalid
    // );
    this.formCancel.emit();
  }

  /**
   * Mark all form controls as touched to trigger validation
   */
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.values(formGroup.controls).forEach((control) => {
      control.markAsTouched();

      if ((control as any).controls) {
        this.markFormGroupTouched(control as FormGroup);
      }
    });
  }
}
