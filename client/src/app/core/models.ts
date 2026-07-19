// TypeScript mirrors of the backend DTOs (server Phase 2 §6). Kept deliberately close
// to the C# records so the API contract is visible in one place.

export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message: string | null;
  errors: ApiError[] | null;
}
export interface ApiError { field?: string; code: string; message: string; }

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AuthUser { id: string; fullName: string; email: string; roles: string[]; emailVerified: boolean; }
export interface AuthResult { accessToken: string; accessTokenExpiresAt: string; refreshToken: string; user: AuthUser; }

export type SectionVisibility = 0 | 1 | 2; // Public | Members | Private
export interface ProfilePrivacy { contact: SectionVisibility; professional: SectionVisibility; academic: SectionVisibility; }

export interface Profile {
  id: string; userId: string; fullName: string; email: string;
  batch: number; house?: string; rollNumber?: string; dateOfBirth?: string;
  mobile?: string; address?: string; currentCity?: string; currentCountry?: string;
  company?: string; designation?: string; industry?: string; education?: string;
  bio?: string; linkedInUrl?: string; gitHubUrl?: string; photoKey?: string;
  skills: string[]; privacy: ProfilePrivacy; completionPct: number;
  isVerified: boolean; directoryVisible: boolean;
}
export interface UpsertProfile {
  batch: number; house?: string; rollNumber?: string; dateOfBirth?: string;
  mobile?: string; address?: string; currentCity?: string; currentCountry?: string;
  company?: string; designation?: string; industry?: string; education?: string;
  bio?: string; linkedInUrl?: string; gitHubUrl?: string;
  skills?: string[]; privacy?: ProfilePrivacy; directoryVisible: boolean;
}

export interface DirectoryCard {
  profileId: string; userId: string; fullName: string; batch?: number; house?: string;
  company?: string; designation?: string; industry?: string; currentCity?: string;
  currentCountry?: string; photoKey?: string; skills: string[];
}

export type VerificationStatus = 0 | 1 | 2; // Pending | Approved | Rejected
export interface VerificationRequest {
  id: string; status: VerificationStatus; submittedAt: string; reviewedAt?: string; rejectionReason?: string;
}
export interface VerificationQueueItem {
  requestId: string; userId: string; profileId: string; fullName: string; email: string;
  batch: number; house?: string; currentCity?: string; company?: string; designation?: string;
  completionPct: number; submittedAt: string;
}

export type CampaignStatus = 0 | 1 | 2 | 3 | 4; // Draft | Active | Paused | Completed | Closed
export interface CampaignCard {
  id: string; title: string; slug: string; coverImageKey?: string;
  goalAmount: number; raisedAmount: number; currency: string; status: CampaignStatus;
  startDate: string; endDate?: string; progressPct: number;
}
export interface Donor { name: string; amount: number; at: string; }
export interface CampaignUpdate { id: string; title: string; body: string; createdAt: string; }
export interface CampaignDetail extends CampaignCard {
  description?: string; organizerName?: string; donorCount: number;
  recentDonors: Donor[]; topDonors: Donor[]; updates: CampaignUpdate[];
}
export interface UpsertCampaign {
  title: string; description?: string; goalAmount: number;
  startDate: string; endDate?: string; organizerName?: string;
}

export type DonationStatus = 0 | 1 | 2 | 3; // Created | Captured | Failed | Refunded
export interface DonationOrder { donationId: string; orderId: string; keyId: string; amountMinor: number; currency: string; campaignTitle: string; }
export interface DonationReceipt { donationId: string; receiptNumber?: string; donorName: string; campaignTitle: string; amount: number; currency: string; status: DonationStatus; capturedAt?: string; }
export interface DonationListItem { id: string; campaignTitle: string; donorName: string; isAnonymous: boolean; amount: number; currency: string; status: DonationStatus; receiptNumber?: string; createdAt: string; capturedAt?: string; }

export type EventStatus = 0 | 1 | 2 | 3;
export type RsvpStatus = 0 | 1 | 2; // Going | Maybe | NotGoing
export interface EventCard { id: string; title: string; eventDate: string; endDate?: string; location?: string; coverImageKey?: string; status: EventStatus; goingCount: number; }
export interface EventDetail extends EventCard { description?: string; maybeCount: number; myRsvp?: RsvpStatus; galleryKeys: string[]; }

export type AnnouncementCategory = 0 | 1 | 2 | 3 | 4;
export type AnnouncementAudience = 0 | 1 | 2;
export interface Announcement { id: string; title: string; body: string; category: AnnouncementCategory; audience: AnnouncementAudience; publishedAt?: string; createdAt: string; }

export interface HomeStats { verifiedAlumni: number; totalRaised: number; activeCampaigns: number; upcomingEvents: number; }
export interface HomeContent { stats: HomeStats; latestCampaigns: CampaignCard[]; upcomingEvents: EventCard[]; recentAnnouncements: Announcement[]; }

export interface DashboardCards { registeredAlumni: number; verifiedAlumni: number; pendingVerifications: number; activeCampaigns: number; fundsRaised: number; upcomingEvents: number; totalDonations: number; }
export interface TimeSeriesPoint { label: string; value: number; }
export interface Dashboard {
  cards: DashboardCards;
  monthlyDonations: TimeSeriesPoint[];
  registrationTrend: TimeSeriesPoint[];
  verification: { pending: number; approved: number; rejected: number };
}

export type UserStatus = 0 | 1 | 2;
export interface UserAdmin { id: string; fullName: string; email: string; roles: string[]; status: UserStatus; emailConfirmed: boolean; createdAt: string; }
