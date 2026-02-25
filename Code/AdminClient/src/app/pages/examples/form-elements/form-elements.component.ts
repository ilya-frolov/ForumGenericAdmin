import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

@Component({
    selector: 'app-form-elements',
    templateUrl: './form-elements.component.html',
    standalone: false
})
export class FormElementsComponent implements OnInit {
  formGroup: FormGroup;
  
  // Dropdown options
  genderOptions = [
    { name: 'Male', value: 'male' },
    { name: 'Female', value: 'female' },
    { name: 'Other', value: 'other' },
    { name: 'Prefer not to say', value: 'not_specified' }
  ];
  
  stateOptions = [
    { name: 'California', value: 'CA' },
    { name: 'New York', value: 'NY' },
    { name: 'Texas', value: 'TX' },
    { name: 'Florida', value: 'FL' },
    { name: 'Illinois', value: 'IL' }
  ];
  
  countryOptions = [
    { name: 'United States', value: 'US' },
    { name: 'Canada', value: 'CA' },
    { name: 'United Kingdom', value: 'UK' },
    { name: 'Australia', value: 'AU' },
    { name: 'Germany', value: 'DE' },
    { name: 'France', value: 'FR' },
    { name: 'Japan', value: 'JP' }
  ];

  // Advanced components options
  fruitOptions = [
    { name: 'Apple', value: 'apple' },
    { name: 'Orange', value: 'orange' },
    { name: 'Banana', value: 'banana' },
    { name: 'Strawberry', value: 'strawberry' },
    { name: 'Mango', value: 'mango' },
    { name: 'Pineapple', value: 'pineapple' },
    { name: 'Grapes', value: 'grapes' }
  ];

  languageOptions = [
    { name: 'JavaScript', value: 'js' },
    { name: 'TypeScript', value: 'ts' },
    { name: 'Python', value: 'py' },
    { name: 'Java', value: 'java' },
    { name: 'C#', value: 'csharp' },
    { name: 'PHP', value: 'php' },
    { name: 'Ruby', value: 'ruby' },
    { name: 'Go', value: 'go' }
  ];

  // Priority options for repeater items
  priorityOptions = [
    { name: 'High', value: 'high' },
    { name: 'Medium', value: 'medium' },
    { name: 'Low', value: 'low' }
  ];
  
  constructor(private fb: FormBuilder) {}
  
  ngOnInit() {
    this.initForm();
  }
  
  initForm() {
    this.formGroup = this.fb.group({
      // Personal Information
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: [null],
      birthdate: [null],
      gender: [null],
      
      // Address Information
      address: [''],
      city: [''],
      state: [null],
      zipCode: [''],
      country: [null],
      
      // Preferences
      email_pref: [false],
      sms_pref: [false],
      phone_pref: [false],
      newsletter: [true],
      twoFactor: [false],
      darkMode: [false],
      
      // Interests & Hobbies
      primaryInterest: ['technology'],
      reading: [false],
      traveling: [false],
      cooking: [false],
      gaming: [false],
      photography: [false],
      
      // Additional Information
      comments: [''],
      termsAccepted: [false, Validators.requiredTrue],

      // Advanced Components
      favoriteColor: ['#1976D2'],
      themeColor: ['rgb(103, 58, 183)'],
      brightness: [50],
      favoriteFruits: [[]],
      programmingLanguages: [[]],
      appointmentTime: [null],
      dateRange: [null],
      workHours: [null],
      rating: [3],
      satisfaction: [75],
      urlInput: [''],
    });
  }

  isImageUrl(url: string): boolean {
    return url?.match(/\.(jpeg|jpg|gif|png|webp)$/i) !== null;
  }

  onSubmit() {
    if (this.formGroup.valid) {
      console.log('Form submitted:', this.formGroup.value);
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
    this.formGroup.reset();
    
    // Re-initialize the form with default values
    this.initForm();
  }
} 