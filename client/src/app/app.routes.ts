import { Routes } from '@angular/router';
import { adminGuard, authGuard, verifiedGuard } from './core/auth/guards';

export const routes: Routes = [
  // Public site
  {
    path: '',
    loadComponent: () => import('./layout/public-shell.component').then((m) => m.PublicShellComponent),
    children: [
      { path: '', loadComponent: () => import('./features/public/home.component').then((m) => m.HomeComponent) },
      { path: 'about', loadComponent: () => import('./features/public/about.component').then((m) => m.AboutComponent) },
      { path: 'campaigns', loadComponent: () => import('./features/public/campaign-list.component').then((m) => m.CampaignListComponent) },
      { path: 'campaigns/:slug', loadComponent: () => import('./features/public/campaign-detail.component').then((m) => m.CampaignDetailComponent) },
      { path: 'events', loadComponent: () => import('./features/public/event-list.component').then((m) => m.EventListComponent) },
      { path: 'announcements', loadComponent: () => import('./features/public/announcement-list.component').then((m) => m.AnnouncementListComponent) },
      { path: 'contact', loadComponent: () => import('./features/public/about.component').then((m) => m.AboutComponent) },
    ],
  },

  // Auth
  {
    path: 'auth',
    loadComponent: () => import('./layout/auth-shell.component').then((m) => m.AuthShellComponent),
    children: [
      { path: 'login', loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent) },
      { path: 'register', loadComponent: () => import('./features/auth/register.component').then((m) => m.RegisterComponent) },
      { path: 'verify-email', loadComponent: () => import('./features/auth/verify-email.component').then((m) => m.VerifyEmailComponent) },
      { path: '', pathMatch: 'full', redirectTo: 'login' },
    ],
  },

  // Alumni portal
  {
    path: 'app',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/portal-shell.component').then((m) => m.PortalShellComponent),
    children: [
      { path: '', loadComponent: () => import('./features/portal/member-dashboard.component').then((m) => m.MemberDashboardComponent) },
      { path: 'profile', loadComponent: () => import('./features/portal/profile-edit.component').then((m) => m.ProfileEditComponent) },
      { path: 'directory', canActivate: [verifiedGuard], loadComponent: () => import('./features/portal/directory.component').then((m) => m.DirectoryComponent) },
      { path: 'donations', loadComponent: () => import('./features/portal/my-donations.component').then((m) => m.MyDonationsComponent) },
    ],
  },

  // Admin portal
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () => import('./layout/portal-shell.component').then((m) => m.PortalShellComponent),
    data: { admin: true },
    children: [
      { path: '', loadComponent: () => import('./features/admin/admin-dashboard.component').then((m) => m.AdminDashboardComponent) },
      { path: 'verifications', loadComponent: () => import('./features/admin/verification-queue.component').then((m) => m.VerificationQueueComponent) },
      { path: 'campaigns', loadComponent: () => import('./features/admin/admin-campaigns.component').then((m) => m.AdminCampaignsComponent) },
      { path: 'donations', loadComponent: () => import('./features/admin/admin-donations.component').then((m) => m.AdminDonationsComponent) },
      { path: 'users', loadComponent: () => import('./features/admin/admin-users.component').then((m) => m.AdminUsersComponent) },
    ],
  },

  { path: '**', redirectTo: '' },
];
