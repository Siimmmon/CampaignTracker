import { Routes } from '@angular/router';
import { CampaignDetailComponent } from './campaigns/detail/campaign-detail.component';
import { CampaignFormComponent } from './campaigns/form/campaign-form.component';
import { CampaignLookupComponent } from './campaigns/lookup/campaign-lookup.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'campaigns/new' },
  { path: 'campaigns', pathMatch: 'full', redirectTo: 'campaigns/find' },
  { path: 'campaigns/new', component: CampaignFormComponent, title: 'New campaign | Campaign Tracker' },
  { path: 'campaigns/find', component: CampaignLookupComponent, title: 'Find a campaign | Campaign Tracker' },
  { path: 'campaigns/:id', component: CampaignDetailComponent, title: 'Campaign details | Campaign Tracker' },
  { path: '**', redirectTo: 'campaigns/new' },
];
