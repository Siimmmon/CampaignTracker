import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, combineLatest, map, of, startWith, Subject, switchMap } from 'rxjs';
import { Campaign, parseCampaignId } from '../campaign';
import { CampaignApiService } from '../campaign-api.service';

type CampaignState =
  | { status: 'loading' }
  | { status: 'ready'; campaign: Campaign }
  | { status: 'invalid' | 'missing' | 'error' };

@Component({
  selector: 'app-campaign-detail',
  imports: [DatePipe, RouterLink],
  templateUrl: './campaign-detail.component.html',
})
export class CampaignDetailComponent {
  private readonly api = inject(CampaignApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly retry = new Subject<void>();

  protected readonly state = toSignal<CampaignState, CampaignState>(
    combineLatest([this.route.paramMap, this.retry.pipe(startWith(undefined))]).pipe(
      switchMap(([parameters]) => {
        const id = parseCampaignId(parameters.get('id') ?? '');
        if (id === null) return of<CampaignState>({ status: 'invalid' });

        return this.api.getById(id).pipe(
          map((campaign): CampaignState => ({ status: 'ready', campaign })),
          startWith<CampaignState>({ status: 'loading' }),
          catchError((error: HttpErrorResponse) => of<CampaignState>({
            status: error.status === 404 ? 'missing' : 'error',
          })),
        );
      }),
    ),
    { initialValue: { status: 'loading' } },
  );

  protected reload(): void {
    this.retry.next();
  }
}
