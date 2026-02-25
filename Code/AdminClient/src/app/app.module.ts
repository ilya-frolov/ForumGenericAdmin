import {NgModule, inject, provideAppInitializer, provideZoneChangeDetection} from '@angular/core';
import {BrowserModule} from '@angular/platform-browser';
import { BrowserAnimationsModule }
    from '@angular/platform-browser/animations';
import {AppRoutingModule} from './app-routing.module';
import {AppComponent} from './app.component';
import {HttpClient, provideHttpClient, withInterceptorsFromDi} from "@angular/common/http";
import {AppConfig} from "./services/app.config";
import {AuthGuard} from "./pages/auth/auth.guard";
import {AppLayoutComponent} from "./layouts/app-layout/app-layout.component";
import {LoaderComponent} from "./components/loader/loader.component";
import {FormsModule, ReactiveFormsModule} from "@angular/forms";

import {TranslateLoader, TranslateModule} from "@ngx-translate/core";

import packageInfo from '../../package.json';
import {ExamplesComponent} from "./pages/examples/examples.component";
import {provideAnimations} from "@angular/platform-browser/animations";
import {TranslateHttpLoader} from "@ngx-translate/http-loader";
import {PrimeModule} from './shared/prime.module';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import  customPrimeNgPreset  from '../customPrimeNgPreset';
import { LoginComponent } from './pages/auth/login/login.component';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { FormElementsComponent } from './pages/examples/form-elements/form-elements.component';
import { TableComponent } from './components/table/table.component';
import { DialogsComponent } from './pages/examples/dialogs/dialogs.component';
import { RepeatersComponent } from './pages/examples/repeaters/repeaters.component';
import { TabsComponent } from './pages/examples/tabs/tabs.component';
import { UploadFilesComponent } from './pages/examples/upload-files/upload-files.component';
import { AdminBreadcrumbComponent } from './components/admin-breadcrumb/admin-breadcrumb.component';
import { AdminBreadcrumbService } from './services/breadcrumb.service';
import { ListComponent } from './pages/list/list.component';
import { ListService } from './services/list.service';
import {IconComponent} from "./components/icon/icon.component";
import { DynamicFormDialogComponent } from './components/dynamic-form-dialog/dynamic-form-dialog.component';
import { DynamicFormComponent } from './components/dynamic-form/dynamic-form.component';
import { LoginService } from './services/login.service';
import {DynamicConfirmModalComponent} from "./modals/dynamic-confirm-modal/dynamic-confirm-modal.component";
import { ImportDialogComponent } from './components/import-dialog/import-dialog.component';
import { DynamicFieldDirective } from './components/field-types/dynamic-field.directive';
import { UploadProgressDialogComponent } from './components/upload-progress-dialog/upload-progress-dialog.component';

// Define translations support constant
export const TRANSLATIONS_SUPPORT: boolean = true;

export function initializeApp(appConfig: AppConfig): any {
  return () => appConfig.load();
}

// Create the translate loader
export function createTranslateLoader(http: HttpClient) {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

@NgModule({
  declarations: [
    LoaderComponent,
    AppComponent,
    AppLayoutComponent,
    ExamplesComponent,
    FormElementsComponent,
    TableComponent,
    DialogsComponent,
    RepeatersComponent,
    TabsComponent,
    UploadFilesComponent,
    ListComponent,
    IconComponent,
    DynamicConfirmModalComponent
  ],
  bootstrap: [AppComponent],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    AppRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    PrimeModule,
    DynamicFieldDirective,
    LoginComponent,
    AuthLayoutComponent,
    AdminBreadcrumbComponent,
    DynamicFormDialogComponent,
    DynamicFormComponent,
    ImportDialogComponent,
    UploadProgressDialogComponent,
    TranslateModule.forRoot({
      loader: TRANSLATIONS_SUPPORT ? {
        provide: TranslateLoader,
        useFactory: createTranslateLoader,
        deps: [HttpClient]
      } : null,
      defaultLanguage: 'en'
    })
  ],
  providers: [
    AppConfig,
    AdminBreadcrumbService,
    ListService,
    provideAnimationsAsync(),
    providePrimeNG({
      theme: {
        preset: customPrimeNgPreset,
        options: {
          darkModeSelector: '.dark-mode',
        }
      }
    }),
    provideAppInitializer(() => {
        const initializerFn = (initializeApp)(inject(AppConfig));
        return initializerFn();
      }),
    AuthGuard,
    provideZoneChangeDetection(),
    provideHttpClient(withInterceptorsFromDi()),
    provideAnimations(),
    LoginService
  ]
})
export class AppModule {
}
