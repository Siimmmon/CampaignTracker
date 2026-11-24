import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { parseCampaignId } from '../campaign';

@Component({
  selector: 'app-campaign-lookup',
  templateUrl: './campaign-lookup.component.html',
})
export class CampaignLookupComponent {
  private readonly router = inject(Router);
  protected readonly error = signal(false);

  protected find(event: SubmitEvent, value: string): void {
    event.preventDefault();
    const id = parseCampaignId(value.trim());
    this.error.set(id === null);
    if (id !== null) void this.router.navigate(['/campaigns', id]);
  }
}
