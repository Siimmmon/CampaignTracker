import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Campaign, CreateCampaign } from './campaign';

@Injectable({ providedIn: 'root' })
export class CampaignApiService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = '/api/campaigns';

  getById(id: number): Observable<Campaign> {
    return this.http.get<Campaign>(`${this.endpoint}/${id}`);
  }

  create(campaign: CreateCampaign): Observable<Campaign> {
    return this.http.post<Campaign>(this.endpoint, campaign);
  }
}
