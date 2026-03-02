import { Routes } from '@angular/router';
import { AppLayoutComponent } from './layouts/app-layout/app-layout.component';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    component: AppLayoutComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'forums' },
      {
        path: 'forums',
        loadComponent: () =>
          import('./pages/forums/forums-page.component').then((m) => m.ForumsPageComponent)
      },
      {
        path: 'forum/:id',
        loadComponent: () =>
          import('./pages/forum/forum-page.component').then((m) => m.ForumPageComponent)
      },
      {
        path: 'post/:id',
        loadComponent: () => import('./pages/post/post-page.component').then((m) => m.PostPageComponent)
      },
      {
        path: 'create-post/:forumId',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./pages/create-post/create-post-page.component').then((m) => m.CreatePostPageComponent)
      }
    ]
  },
  {
    path: '',
    component: AuthLayoutComponent,
    children: [
      {
        path: 'login',
        loadComponent: () => import('./pages/login/login-page.component').then((m) => m.LoginPageComponent)
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./pages/register/register-page.component').then((m) => m.RegisterPageComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'forums' }
];
