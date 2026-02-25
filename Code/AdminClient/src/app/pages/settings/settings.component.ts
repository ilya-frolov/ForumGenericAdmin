import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { DynamicFormComponent } from '../../components/dynamic-form/dynamic-form.component';
import { FormBuilderService, FormMode } from '../../services/form-builder.service';
import { LoggerService } from 'src/app/services/logger.service';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { Subscription, switchMap, Observable, takeUntil } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { AppService } from 'src/app/services/app.service';
import { AdminSegment, AdminSettingsSegment } from 'src/app/interfaces/models';
import { FormStructure } from 'src/app/interfaces/form-structure';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import {LangService} from '../../services/lang.service';
import { TranslateService, TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-admin-settings',
  standalone: true,
  imports: [
    CommonModule,
    CardModule,
    DynamicFormComponent,
    ToastModule,
    ProgressSpinnerModule,
    TranslateModule
  ],
  providers: [MessageService],
  templateUrl: './settings.component.html',
})
export class AdminSettingsComponent implements OnInit, OnDestroy {
  formMode: FormMode = 'edit';
  refId: string = null;
  formId: string = null;
  controllerName: string = null;
  loading = true;
  formResult: any = null;
  private subsCleanup: Subscription[] = [];
  segmentSegment: AdminSettingsSegment;
  structure: FormStructure;
  

  constructor(
    private logger: LoggerService,
    private messageService: MessageService,
    private route: ActivatedRoute,
    private appService: AppService,
    private formBuilderService: FormBuilderService,
    private langService: LangService,
    private translateService: TranslateService
  ) {}

  ngOnInit(): void {
    this.loading = true;
    this.routeInit();
    this.logger.info('Settings page initialized');
  }

  ngOnDestroy(): void {
    // Clean up subscriptions to prevent memory leaks
    this.subsCleanup.forEach(sub => sub.unsubscribe());
    this.subsCleanup = [];
  }

  routeInit(): void {
      const subscription = this.route.params.pipe(
        switchMap((params) => {
          this.loading = true;
          this.refId = params['refId'];
          this.segmentSegment = this.appService.getSettingsSegment(params['id']);
          this.controllerName = this.segmentSegment?.controllerName;
          this.formId = this.segmentSegment?.id;
          return this.formBuilderService.buildFormAndStructure(
            this.segmentSegment.controllerName,
            this.refId,
            this.formId,
          );
        })
      ).subscribe(structure => {
        this.structure = structure;
        this.loading = false;
      });
      
      this.subsCleanup.push(subscription);
  }

  // Handle form submission
  onFormSubmit(formData: any): void {
    this.formResult = formData;
    this.logger.info('Settings form submitted:', formData);


    this.appService
    .submitForm(this.segmentSegment.controllerName, formData, this.segmentSegment.id)
    .subscribe((response) => {
      if (response.result) {
        this.messageService.add({
          severity: 'success',
          summary: this.translateService.instant('SHARED.Success'),
          detail: this.translateService.instant('SETTINGS_PAGE.SettingsSavedSuccessfully'),
          life: 3000
        });
      } else {
        this.messageService.add({
          severity: 'error',
          summary: this.translateService.instant('SHARED.Error'),
          detail: this.translateService.instant('SETTINGS_PAGE.FailedToSaveSettings'),
          life: 5000
        });
      }
    });
  }
} 
