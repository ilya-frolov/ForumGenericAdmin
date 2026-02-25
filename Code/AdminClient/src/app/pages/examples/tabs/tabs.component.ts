import { Component } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { FileUploadEvent } from 'primeng/fileupload';

@Component({
  selector: 'app-tabs',
  standalone: false,
  templateUrl: './tabs.component.html',
  providers: [MessageService]
})

export class TabsComponent {
  formGroup: FormGroup;
  uploadedFiles: any[] = [];


  constructor(private fb: FormBuilder, private messageService: MessageService) {
    this.formGroup = this.fb.group({});
  }

  onUpload(event: FileUploadEvent) {
      for(let file of event.files) {
          this.uploadedFiles.push(file);
      }

      this.messageService.add({severity: 'info', summary: 'File Uploaded', detail: ''});
  }

  onSubmit(): void {
    if (this.formGroup.valid) {
      console.log(this.formGroup.value);
    }
  }

}
