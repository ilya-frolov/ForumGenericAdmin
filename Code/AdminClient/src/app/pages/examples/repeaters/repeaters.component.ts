import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';

interface RepeaterItem {
  title: string;
  description: string;
  dueDate: Date | null;
  priority: string;
  isCompleted: boolean;
}

interface RepeaterState {
  isEditing: boolean;
  originalValue: RepeaterItem;
}

@Component({
    selector: 'app-repeaters',
    templateUrl: './repeaters.component.html',
    standalone: false
})
export class RepeatersComponent implements OnInit {
  formGroup: FormGroup;
  
  // Track active accordion index
  activeAccordionIndex: number | number[] = 0;
  
  // Track expanded panel indexes
  expandedPanelIndexes: Set<number> = new Set<number>();
  
  // State tracking for repeater items
  repeaterStates: RepeaterState[] = [];
  panelRepeaterStates: RepeaterState[] = [];
  
  // Dropdown options
  priorityOptions = [
    { name: 'High', value: 'high' },
    { name: 'Medium', value: 'medium' },
    { name: 'Low', value: 'low' }
  ];
  
  // Mapping for priority severity
  prioritySeverityMap: Record<string, 'danger' | 'warn' | 'info'> = {
    'high': 'danger',
    'medium': 'warn',
    'low': 'info'
  };
  
  constructor(private fb: FormBuilder) {}
  
  ngOnInit() {
    this.initForm();
  }
  
  initForm() {
    this.formGroup = this.fb.group({
      // Repeater Items
      repeaterItems: this.fb.array([]),

      // Panel Repeater Items
      panelRepeaterItems: this.fb.array([])
    });

    // Add example repeater items
    this.addExampleItems();
    
    // Initialize first panel as expanded
    if (this.panelRepeaterItems.length > 0) {
      this.expandedPanelIndexes.add(0);
    }
  }
  
  // Add example items for both repeater types
  addExampleItems(): void {
    // Add example tasks to regular repeater
    this.addExampleRepeaterItems();
    
    // Add example tasks to panel repeater
    this.addExamplePanelRepeaterItems();
  }

  // Getter for repeater items as FormArray
  get repeaterItems(): FormArray {
    return this.formGroup.get('repeaterItems') as FormArray;
  }

  // Getter for panel repeater items as FormArray
  get panelRepeaterItems(): FormArray {
    return this.formGroup.get('panelRepeaterItems') as FormArray;
  }

  // Create a new repeater item form group
  createRepeaterItem(): FormGroup {
    return this.fb.group({
      title: ['', Validators.required],
      description: [''],
      dueDate: [null],
      priority: ['medium'],
      isCompleted: [false]
    });
  }

  // Add example repeater items
  addExampleRepeaterItems(): void {
    const exampleTasks = [
      {
        title: 'Complete project documentation',
        description: 'Finalize all technical documentation for the Q3 release including API references and user guides.',
        dueDate: new Date(new Date().setDate(new Date().getDate() + 2)),
        priority: 'high',
        isCompleted: false
      },
      {
        title: 'Review pull requests',
        description: 'Review and merge outstanding pull requests from the development team.',
        dueDate: new Date(new Date().setDate(new Date().getDate() + 5)),
        priority: 'medium',
        isCompleted: false
      },
      {
        title: 'Update dependencies',
        description: 'Update all npm packages to their latest compatible versions.',
        dueDate: new Date(new Date().setDate(new Date().getDate() - 1)),
        priority: 'low',
        isCompleted: true
      }
    ];
    
    // Add all example tasks to the form array
    exampleTasks.forEach(task => {
      const taskGroup = this.fb.group({
        title: [task.title, Validators.required],
        description: [task.description],
        dueDate: [task.dueDate],
        priority: [task.priority],
        isCompleted: [task.isCompleted]
      });
      
      this.repeaterItems.push(taskGroup);
      this.repeaterStates.push({
        isEditing: false,
        originalValue: {...task}
      });
    });
  }

  // Add a new repeater item
  addRepeaterItem(): void {
    const newItem = this.createRepeaterItem();
    this.repeaterItems.push(newItem);
    
    // Store state for the new item
    this.repeaterStates.push({
      isEditing: true,
      originalValue: newItem.value
    });
    
    // Set the active accordion index to expand the new item
    this.activeAccordionIndex = this.repeaterItems.length - 1;
  }

  // Remove a repeater item
  removeRepeaterItem(index: number): void {
    this.repeaterItems.removeAt(index);
    this.repeaterStates.splice(index, 1);
  }

  // Add example panel repeater items
  addExamplePanelRepeaterItems(): void {
    const exampleTasks = [
      {
        title: 'Review design mockups',
        description: 'Review and provide feedback on the new UI design mockups for the dashboard.',
        dueDate: new Date(new Date().setDate(new Date().getDate() + 3)),
        priority: 'high',
        isCompleted: false
      },
      {
        title: 'Update user documentation',
        description: 'Update the user documentation with the latest feature changes.',
        dueDate: new Date(new Date().setDate(new Date().getDate() + 7)),
        priority: 'medium',
        isCompleted: false
      }
    ];
    
    // Add example tasks to the panel form array
    exampleTasks.forEach(task => {
      const taskGroup = this.fb.group({
        title: [task.title, Validators.required],
        description: [task.description],
        dueDate: [task.dueDate],
        priority: [task.priority],
        isCompleted: [task.isCompleted]
      });
      
      this.panelRepeaterItems.push(taskGroup);
      this.panelRepeaterStates.push({
        isEditing: false,
        originalValue: {...task}
      });
    });
  }

  // Add a new panel repeater item
  addPanelRepeaterItem(): void {
    const newItem = this.createRepeaterItem();
    this.panelRepeaterItems.push(newItem);
    
    // Store state for the new item
    this.panelRepeaterStates.push({
      isEditing: true,
      originalValue: newItem.value
    });
    
    // Add the new panel index to expanded panels
    const newIndex = this.panelRepeaterItems.length - 1;
    this.expandedPanelIndexes.add(newIndex);
  }

  // Remove a panel repeater item
  removePanelRepeaterItem(index: number): void {
    // Remove the item from the form array
    this.panelRepeaterItems.removeAt(index);
    
    // Remove the state
    this.panelRepeaterStates.splice(index, 1);
    
    // Remove from expanded panels
    this.expandedPanelIndexes.delete(index);
    
    // Update expanded panel indexes for items after the removed one
    const updatedExpanded = new Set<number>();
    this.expandedPanelIndexes.forEach(i => {
      if (i < index) {
        updatedExpanded.add(i);
      } else if (i > index) {
        updatedExpanded.add(i - 1);
      }
    });
    
    this.expandedPanelIndexes = updatedExpanded;
    
    // If no panels are expanded and we still have items, expand the first one
    if (this.expandedPanelIndexes.size === 0 && this.panelRepeaterItems.length > 0) {
      this.expandedPanelIndexes.add(0);
    }
  }
  
  // Toggle edit mode for a repeater item
  toggleRepeaterEditMode(index: number): void {
    const currentState = this.repeaterStates[index];
    
    if (currentState.isEditing) {
      // If currently in edit mode, save changes before switching to read-only
      this.saveRepeaterItem(index);
    } else {
      // If switching to edit mode, store current values for potential cancellation
      this.repeaterStates[index].originalValue = { ...this.repeaterItems.at(index).value };
      // Set the active accordion index to expand this item
      this.activeAccordionIndex = index;
    }
    
    // Toggle edit state
    this.repeaterStates[index].isEditing = !currentState.isEditing;
  }
  
  // Toggle edit mode for a panel repeater item
  togglePanelRepeaterEditMode(index: number): void {
    const currentState = this.panelRepeaterStates[index];
    
    if (currentState.isEditing) {
      // If currently in edit mode, save changes before switching to read-only
      this.savePanelRepeaterItem(index);
    } else {
      // If switching to edit mode, store current values for potential cancellation
      this.panelRepeaterStates[index].originalValue = { ...this.panelRepeaterItems.at(index).value };
      // Expand the panel when entering edit mode
      this.expandedPanelIndexes.add(index);
    }
    
    // Toggle edit state
    this.panelRepeaterStates[index].isEditing = !currentState.isEditing;
    
    // Force panel expansion when entering edit mode
    if (this.panelRepeaterStates[index].isEditing) {
      this.expandedPanelIndexes.add(index);
    }
  }
  
  // Save a repeater item
  saveRepeaterItem(index: number): void {
    if (this.repeaterItems.at(index).valid) {
      // Update the original value to the current value
      this.repeaterStates[index].originalValue = { ...this.repeaterItems.at(index).value };
      // Switch to read-only mode after saving
      this.repeaterStates[index].isEditing = false;
    } else {
      // Mark fields as touched to show validation errors
      this.markFormGroupTouched(this.repeaterItems.at(index) as FormGroup);
    }
  }
  
  // Save a repeater item and move to the next one
  saveAndNextRepeaterItem(index: number): void {
    if (this.repeaterItems.at(index).valid) {
      // Save the current item
      this.saveRepeaterItem(index);
      
      // If this is not the last item, move to the next one
      if (index < this.repeaterItems.length - 1) {
        // Set the next item to edit mode
        this.repeaterStates[index + 1].isEditing = true;
        // Set the active accordion index to expand the next item
        this.activeAccordionIndex = index + 1;
      } else {
        // If it's the last item, add a new one and move to it
        this.addRepeaterItem();
      }
    } else {
      // Mark fields as touched to show validation errors
      this.markFormGroupTouched(this.repeaterItems.at(index) as FormGroup);
    }
  }
  
  // Cancel edits to a repeater item
  cancelRepeaterItemEdit(index: number): void {
    // Reset the form group to its original values
    this.repeaterItems.at(index).patchValue(this.repeaterStates[index].originalValue);
    // Switch to read-only mode after cancelling
    this.repeaterStates[index].isEditing = false;
  }
  
  // Save a panel repeater item
  savePanelRepeaterItem(index: number): void {
    if (this.panelRepeaterItems.at(index).valid) {
      // Update the original value to the current value
      this.panelRepeaterStates[index].originalValue = { ...this.panelRepeaterItems.at(index).value };
      // Switch to read-only mode after saving
      this.panelRepeaterStates[index].isEditing = false;
    } else {
      // Mark fields as touched to show validation errors
      this.markFormGroupTouched(this.panelRepeaterItems.at(index) as FormGroup);
    }
  }
  
  // Save a panel repeater item and move to the next one
  saveAndNextPanelRepeaterItem(index: number): void {
    if (this.panelRepeaterItems.at(index).valid) {
      // Save the current item
      this.savePanelRepeaterItem(index);
      
      // If this is not the last item, move to the next one
      if (index < this.panelRepeaterItems.length - 1) {
        // Set the next item to edit mode
        this.panelRepeaterStates[index + 1].isEditing = true;
        
        // Update expanded panels
        this.expandedPanelIndexes.add(index + 1);
        this.expandedPanelIndexes.delete(index);
      } else {
        // If it's the last item, add a new one and move to it
        this.addPanelRepeaterItem();
        // Collapse the current panel
        this.expandedPanelIndexes.delete(index);
      }
    } else {
      // Mark fields as touched to show validation errors
      this.markFormGroupTouched(this.panelRepeaterItems.at(index) as FormGroup);
    }
  }
  
  // Cancel edits to a panel repeater item
  cancelPanelRepeaterItemEdit(index: number): void {
    // Reset the form group to its original values
    this.panelRepeaterItems.at(index).patchValue(this.panelRepeaterStates[index].originalValue);
    // Switch to read-only mode after cancelling
    this.panelRepeaterStates[index].isEditing = false;
  }
  
  onSubmit() {
    if (this.formGroup.valid) {
      // Handle form submission
    } else {
      // Mark all fields as touched to trigger validation
      this.markFormGroupTouched(this.formGroup);
    }
  }
  
  // Helper method to mark all form controls as touched
  markFormGroupTouched(formGroup: FormGroup) {
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }
  
  // Reset form
  resetForm() {
    // Clear form arrays efficiently
    (this.repeaterItems as FormArray).clear();
    (this.panelRepeaterItems as FormArray).clear();
    
    // Reset state arrays
    this.repeaterStates = [];
    this.panelRepeaterStates = [];
    
    // Reset active accordion index
    this.activeAccordionIndex = 0;
    
    // Reset expanded panel indexes
    this.expandedPanelIndexes.clear();
    
    // Re-initialize the form with default values
    this.initForm();
  }

  // Handle accordion tab change event
  onAccordionTabChange(event: number | number[]): void {
    this.activeAccordionIndex = event;
  }

  // Toggle panel expansion
  togglePanelExpansion(index: number): void {
    if (this.expandedPanelIndexes.has(index)) {
      this.expandedPanelIndexes.delete(index);
    } else {
      this.expandedPanelIndexes.add(index);
    }
  }
  
  // Check if a panel is expanded
  isPanelExpanded(index: number): boolean {
    return this.expandedPanelIndexes.has(index);
  }
  
  // Get priority severity based on priority value
  getPrioritySeverity(priority: string): 'danger' | 'warn' | 'info' {
    return this.prioritySeverityMap[priority] || 'info';
  }
  
  // Check if a repeater item is in edit mode
  isRepeaterItemEditing(index: number): boolean {
    return this.repeaterStates[index]?.isEditing || false;
  }
  
  // Check if a panel repeater item is in edit mode
  isPanelRepeaterItemEditing(index: number): boolean {
    return this.panelRepeaterStates[index]?.isEditing || false;
  }
  
  // Check if a repeater item is blocked (read-only)
  isRepeaterItemBlocked(index: number): boolean {
    return !this.isRepeaterItemEditing(index);
  }
  
  // Check if a panel repeater item is blocked (read-only)
  isPanelRepeaterItemBlocked(index: number): boolean {
    return !this.isPanelRepeaterItemEditing(index);
  }
}