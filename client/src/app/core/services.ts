import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  Announcement, CampaignCard, CampaignDetail, Dashboard, DirectoryCard, DonationListItem,
  DonationOrder, DonationReceipt, EventCard, EventDetail, HomeContent, PagedResult, Profile,
  UpsertCampaign, UpsertProfile, UserAdmin, VerificationQueueItem, VerificationRequest,
} from './models';

@Injectable({ providedIn: 'root' })
export class ContentService {
  private api = inject(ApiService);
  home(): Observable<HomeContent> { return this.api.get<HomeContent>('/content/home'); }
}

@Injectable({ providedIn: 'root' })
export class CampaignService {
  private api = inject(ApiService);
  list(page = 1, pageSize = 12): Observable<PagedResult<CampaignCard>> {
    return this.api.get('/campaigns', { page, pageSize });
  }
  bySlug(slug: string): Observable<CampaignDetail> { return this.api.get(`/campaigns/${slug}`); }
  create(body: UpsertCampaign): Observable<string> { return this.api.post('/campaigns', body); }
  update(id: string, body: UpsertCampaign) { return this.api.put(`/campaigns/${id}`, body); }
  setStatus(id: string, status: number) { return this.api.patch(`/campaigns/${id}/status`, { status }); }
  remove(id: string) { return this.api.delete(`/campaigns/${id}`); }
  postUpdate(id: string, title: string, body: string) { return this.api.post(`/campaigns/${id}/updates`, { title, body }); }
}

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private api = inject(ApiService);
  mine(): Observable<Profile | null> { return this.api.get<Profile | null>('/profiles/me'); }
  upsert(body: UpsertProfile): Observable<Profile> { return this.api.put('/profiles/me', body); }
  byId(id: string) { return this.api.get(`/profiles/${id}`); }
  uploadPhoto(file: File): Observable<{ photoKey: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.api.upload('/profiles/me/photo', form);
  }
}

@Injectable({ providedIn: 'root' })
export class DirectoryService {
  private api = inject(ApiService);
  search(filters: Record<string, string | number | undefined>): Observable<PagedResult<DirectoryCard>> {
    return this.api.get('/directory/search', filters);
  }
}

@Injectable({ providedIn: 'root' })
export class VerificationService {
  private api = inject(ApiService);
  submit(): Observable<VerificationRequest> { return this.api.post('/verification/requests'); }
  mine(): Observable<VerificationRequest | null> { return this.api.get<VerificationRequest | null>('/verification/requests/me'); }
  queue(page = 1, pageSize = 20): Observable<PagedResult<VerificationQueueItem>> {
    return this.api.get('/verification/requests', { page, pageSize });
  }
  approve(id: string, notes?: string) { return this.api.post(`/verification/requests/${id}/approve`, { notes }); }
  reject(id: string, reason: string) { return this.api.post(`/verification/requests/${id}/reject`, { reason }); }
}

@Injectable({ providedIn: 'root' })
export class DonationService {
  private api = inject(ApiService);
  order(campaignId: string, amount: number, donorName: string, donorEmail: string, isAnonymous: boolean): Observable<DonationOrder> {
    return this.api.post('/donations/order', { campaignId, amount, donorName, donorEmail, isAnonymous });
  }
  verify(orderId: string, paymentId: string, signature: string): Observable<DonationReceipt> {
    return this.api.post('/donations/verify', { orderId, paymentId, signature });
  }
  mine(page = 1, pageSize = 20): Observable<PagedResult<DonationListItem>> {
    return this.api.get('/donations/me', { page, pageSize });
  }
  all(campaign?: string, status?: number, page = 1, pageSize = 25): Observable<PagedResult<DonationListItem>> {
    return this.api.get('/donations', { campaign, status, page, pageSize });
  }
}

@Injectable({ providedIn: 'root' })
export class EventService {
  private api = inject(ApiService);
  list(scope?: string): Observable<PagedResult<EventCard>> { return this.api.get('/events', { scope }); }
  get(id: string): Observable<EventDetail> { return this.api.get(`/events/${id}`); }
  rsvp(id: string, status: number) { return this.api.post(`/events/${id}/rsvp`, { status }); }
  create(body: unknown) { return this.api.post('/events', body); }
  setStatus(id: string, status: number) { return this.api.patch(`/events/${id}/status`, { status }); }
}

@Injectable({ providedIn: 'root' })
export class AnnouncementService {
  private api = inject(ApiService);
  list(category?: number): Observable<PagedResult<Announcement>> { return this.api.get('/announcements', { category }); }
  create(body: unknown) { return this.api.post('/announcements', body); }
  remove(id: string) { return this.api.delete(`/announcements/${id}`); }
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private api = inject(ApiService);
  dashboard(): Observable<Dashboard> { return this.api.get('/admin/dashboard'); }
  users(query?: string, role?: string, page = 1, pageSize = 25): Observable<PagedResult<UserAdmin>> {
    return this.api.get('/admin/users', { query, role, page, pageSize });
  }
  setRoles(id: string, roles: string[]) { return this.api.patch(`/admin/users/${id}/roles`, { roles }); }
  setStatus(id: string, status: number) { return this.api.patch(`/admin/users/${id}/status`, { status }); }
  reportUrl(): string { return `${this.api.base}/admin/reports/donations`; }
}
