import {
  Component,
  Directive,
  Input,
  OnInit,
  ViewContainerRef,
  ComponentRef,
  OnDestroy,
  ElementRef,
  TemplateRef,
  inject,
  Renderer2,
  AfterViewInit,
  EventEmitter,
  Output,
  OnChanges,
  SimpleChanges,
} from '@angular/core';
import { DynamicFieldService } from '../../services/dynamic-field.service';
import { LoggerService } from '../../services/logger.service';
import { Subscription } from 'rxjs';
import { FieldData, FieldType } from 'src/app/interfaces/field-data';
import { FieldMetadata } from 'src/app/interfaces/field-metadata';
import { FieldTypeComponent } from 'src/app/interfaces/field-type.interface';
import { FormGroup, FormControl, AbstractControl } from '@angular/forms';
import { FormBuilderService } from '../../services/form-builder.service';
import { ValidatorFn } from '@angular/forms';
import { Validators } from '@angular/forms';

// Simple placeholder component to use instead of direct DOM manipulation
@Component({
  selector: 'app-placeholder',
  standalone: true,
  template: `
    <div
      class="alert alert-info mt-2"
      style="padding: 0.5rem; font-size: 0.8rem;"
    >
      {{ message }}
    </div>
  `,
})
export class PlaceholderComponent {
  @Input() message: string = '';
}

@Directive({
  selector: '[appDynamicField]',
  standalone: true,
})
export class DynamicFieldDirective<
  InPutType,
  Template,
  FD extends FieldData<InPutType, FieldMetadata>
> implements OnInit, OnDestroy, AfterViewInit
{
  @Input() fieldData!: FD;
  @Input() mode: 'edit' | 'list' | 'view' = 'edit';
  @Input() formGroup?: FormGroup;
  @Input() path: string = '';
  @Input() isHidden: boolean = false;
  @Output() fieldInitialized = new EventEmitter<string>();

  private componentRef!: ComponentRef<
    FieldTypeComponent<InPutType, FieldMetadata>
  >;
  private valueChangeSubscription?: Subscription;

  // Using inject for better standalone compatibility
  private viewContainerRef = inject(ViewContainerRef);
  private dynamicFieldService = inject(DynamicFieldService);
  private logger = inject(LoggerService);
  private renderer = inject(Renderer2);
  private formBuilderService = inject(FormBuilderService);

  constructor() {}

  ngOnInit(): void {
    if (!this.fieldData) {
      this.logger.error(
        'No field data provided to the dynamic field directive'
      );
      return;
    }

    // Initialize the field in the form
    this.initializeFormControl();
  }

  ngAfterViewInit(): void {
    // Load the component after view init to ensure proper rendering
    this.loadComponent();
    // Notify parent that this field has been initialized
    this.fieldInitialized.emit(this.path);

  }

  ngOnDestroy(): void {
    if (this.valueChangeSubscription) {
      this.valueChangeSubscription.unsubscribe();
    }

    if (this.componentRef) {
      this.componentRef.destroy();
    }
  }

  /**
   * Initialize the form control for this field
   */
  private initializeFormControl(): void {
    const existingControl = this.formGroup?.get(this.path);
      // Modified to perform case-insensitive property lookup
      const defaultValue = this.formBuilderService.getModelFieldValueFromPath(this.path)
       ?? this.formBuilderService.getDefaultValueForFieldType(this.fieldData.type);
      
      this.formBuilderService.getOrCreateControlByPath(
        this.path,
        defaultValue,
        false,
        this.formBuilderService.getValidatorsFromFieldData(this.fieldData, this.isHidden)
      );

      if (existingControl && defaultValue != undefined && existingControl.value !== defaultValue) {
        existingControl.setValue(defaultValue);
        existingControl.markAsDirty();
        existingControl.updateValueAndValidity();
      }

  }

  private loadComponent(): void {
    // Clear previous component
    this.viewContainerRef.clear();

    this.logger.debug(
      'DynamicFieldDirective loading component for field:',
      this.fieldData.id,
      'type:',
      this.fieldData.type
    );

    // Get component class from service
    const componentType = this.dynamicFieldService.getFieldComponent(
      this.fieldData.type as FieldType
    );

    if (!componentType) {
      this.logger.error(
        `Component for field type ${this.fieldData.type} not found`
      );

      // Insert placeholder content
      this.renderPlaceholder(
        `Field type ${this.fieldData.type} not registered`
      );
      return;
    }

    this.logger.debug('Found component type:', componentType.name);

    // Create component instance directly using ViewContainerRef
    this.componentRef = this.viewContainerRef.createComponent(componentType);

    // Set component properties
    const instance = this.componentRef.instance;
    instance.metadata = this.fieldData.metadata || {};
    instance.label = this.fieldData.label;
    instance.id = this.fieldData.id;
    
    // Get the current value from the form control provided via @Input() formGroup
    const control = this.formGroup ? this.formGroup.get(this.path) : null;
    if (control) {
      instance.value = instance.parseValueFromServer(control.value);
    } else {
      instance.value = instance.parseValueFromServer(this.fieldData.value);
    }

    this.logger.debug(
      'Component instance created with metadata:',
      instance.metadata
    );

    // Subscribe to value changes
    this.valueChangeSubscription = instance.valueChange.subscribe(
      (newValue: InPutType) => {
        this.logger.debug(
          'Value changed for field:',
          this.fieldData.id,
          'new value:',
          newValue
        );

        // Convert value for server using component's method
        const serverValue = instance.getValueForServer(newValue);

        // Update the form control if we have a form group
        if (this.formGroup) {
          const control = this.formGroup?.get(this.path);
          if (control) {
            try {
              control.setValue(serverValue);
              control.markAsDirty();
              control.updateValueAndValidity();
            } catch (error) {
              //missmatch control type error - remove and add new control
              this.formGroup.removeControl(this.path);
              this.formBuilderService.getOrCreateControlByPath(
                this.path,
                serverValue,
                false,
                this.formBuilderService.getValidatorsFromFieldData(this.fieldData, this.isHidden)
              );
            }
          }
        }

        // Update the field data value
        this.fieldData.value = serverValue; // TODO: check if this is needed

        // Get model data and update it
        const modelData = this.formBuilderService.getModelData();
        if (modelData) {
          modelData[this.fieldData.id] = serverValue;
        }
      }
    );

    // Get the appropriate template based on mode
    const template =
      this.mode === 'edit'
        ? instance.getEditTemplate()
        : this.mode === 'list'
          ? instance.getListTemplate()
          : instance.getViewTemplate();

    this.logger.debug(
      'Template received:',
      template ? 'Yes' : 'No',
      'Mode:',
      this.mode
    );

    // Handle the template based on its type
    if (template instanceof ElementRef) {
      // If it's an ElementRef, append the native element
      this.viewContainerRef.element.nativeElement.appendChild(
        template.nativeElement
      );
      this.logger.debug('Appended ElementRef');
    } else if (template instanceof TemplateRef) {
      // If it's a TemplateRef, create an embedded view
      this.viewContainerRef.createEmbeddedView(template);
      this.logger.debug('Created embedded view from TemplateRef');
    } else {
      this.logger.error(
        'Template is neither ElementRef nor TemplateRef:',
        template
      );
    }
  }

  /**
   * Render a placeholder for fields without valid component types
   * Uses proper Angular methods to create and insert an element
   */
  private renderPlaceholder(message: string): void {
    // Create a placeholder element using Angular's component creation
    const componentFactory =
      this.viewContainerRef.createComponent(PlaceholderComponent);
    const component = componentFactory.instance;
    component.message = message;
  }

  
}
