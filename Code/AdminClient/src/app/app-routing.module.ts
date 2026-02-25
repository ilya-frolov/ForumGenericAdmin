import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AppLayoutComponent } from './layouts/app-layout/app-layout.component';
import { ExamplesComponent } from './pages/examples/examples.component';
import { FormElementsComponent } from './pages/examples/form-elements/form-elements.component';
import { RepeatersComponent } from './pages/examples/repeaters/repeaters.component';
import { TableComponent } from './components/table/table.component';
import { DialogsComponent } from './pages/examples/dialogs/dialogs.component';
import { LoginComponent } from './pages/auth/login/login.component';
import { SignupComponent } from './pages/auth/signup/signup.component';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { TabsComponent } from './pages/examples/tabs/tabs.component';
import { UploadFilesComponent } from './pages/examples/upload-files/upload-files.component';
import { ListComponent } from './pages/list/list.component';
import { DynamicFormDialogExampleComponent } from './pages/examples/dynamic-form-dialog-example/dynamic-form-dialog-example.component';
import { AdminSettingsComponent } from './pages/settings/settings.component';
import { AuthGuard } from './pages/auth/auth.guard';
import { AdminHomeComponent } from './pages/home/admin-home.component';

const routes: Routes = [
  // Auth routes with AuthLayout
  {
    path: '',
    component: AuthLayoutComponent,
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      { path: 'login', component: LoginComponent },
      { path: 'signup', component: SignupComponent },
      // Add other auth routes here (register, forgot password, etc.)
    ],
  },

  // App routes with AppLayout (protected by AuthGuard)
  {
    path: '',
    component: AppLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: 'home', component: AdminHomeComponent },
      { path: 'examples', component: ExamplesComponent },
      { path: 'examples/form-elements', component: FormElementsComponent },
      { path: 'examples/table', component: TableComponent },
      { path: 'examples/modals', component: DialogsComponent },
      { path: 'examples/repeaters', component: RepeatersComponent },
      { path: 'examples/tabs', component: TabsComponent },
      { path: 'examples/upload-files', component: UploadFilesComponent },
      { path: 'examples/dynamic-form', component: DynamicFormDialogExampleComponent },
      { path: 'settings/:id', component: AdminSettingsComponent },
      { path: ':id', component: ListComponent },
      { path: ':id/:refId', component: ListComponent }
      // Add other protected routes here
    ],
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
