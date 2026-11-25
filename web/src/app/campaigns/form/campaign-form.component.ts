import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { CampaignApiService } from '../campaign-api.service';

const datePattern = /^(?!0000)\d{4}-\d{2}-\d{2}$/;
const eventTimestampPattern = /^(?!0000)\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/;

type EventForm = FormGroup<{
  eventType: FormControl<string>;
  timestamp: FormControl<string>;
}>;

function plainTextValidator(maxLength: number): ValidatorFn {
  return (control) => {
    const value = control.value.trim();
    if (!value) return { required: true };
    if (value.length > maxLength) return { length: true };
    return /[<>\u0000-\u001f\u007f-\u009f]/u.test(value) ? { plainText: true } : null;
  };
}

@Component({
  selector: 'app-campaign-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './campaign-form.component.html',
})
export class CampaignFormComponent {
  private readonly api = inject(CampaignApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly saving = signal(false);
  protected readonly serverErrors = signal<string[]>([]);
  protected readonly maxEvents = 20;
  protected readonly maxEventTypeLength = 80;

  protected readonly name = new FormControl('', {
    nonNullable: true,
    validators: plainTextValidator(120),
  });
  protected readonly startDate = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.pattern(datePattern)],
  });
  protected readonly endDate = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.pattern(datePattern)],
  });
  protected readonly events = new FormArray<EventForm>([]);
  protected readonly form = new FormGroup(
    { name: this.name, startDate: this.startDate, endDate: this.endDate, events: this.events },
    {
      validators: () => {
        if (this.startDate.valid && this.endDate.valid && this.endDate.value <= this.startDate.value)
          return { dateOrder: true };

        return this.events.controls.some(({ controls }) => this.isOutsideCampaignDates(controls.timestamp))
          ? { eventOutsideCampaignDates: true }
          : null;
      },
    },
  );

  protected addEvent(): void {
    if (this.events.length >= this.maxEvents) return;

    this.events.push(new FormGroup({
      eventType: new FormControl('', { nonNullable: true, validators: plainTextValidator(this.maxEventTypeLength) }),
      timestamp: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.pattern(eventTimestampPattern)],
      }),
    }));
  }

  protected removeEvent(index: number): void {
    this.events.removeAt(index);
  }

  protected isOutsideCampaignDates(timestamp: FormControl<string>): boolean {
    if (!timestamp.valid || !this.startDate.valid || !this.endDate.valid || this.startDate.value >= this.endDate.value)
      return false;

    const eventDate = timestamp.value.slice(0, 10);
    return eventDate < this.startDate.value || eventDate > this.endDate.value;
  }

  protected submit(): void {
    if (this.saving()) return;

    this.serverErrors.set([]);
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const { name, startDate, endDate, events } = this.form.getRawValue();
    this.saving.set(true);
    this.api.create({
      name: name.trim(),
      startDate,
      endDate,
      events: events.map(({ eventType, timestamp }) => ({ eventType: eventType.trim(), timestamp: `${timestamp}:00Z` })),
    })
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.saving.set(false)))
      .subscribe({
        next: (campaign) => void this.router.navigate(['/campaigns', campaign.id]),
        error: (error: HttpErrorResponse) => {
          const details = error.status === 400 && error.error?.errors
            ? Object.values(error.error.errors as Record<string, string[]>).flat()
            : [];
          this.serverErrors.set(details.length ? details : ['The campaign could not be saved. Try again.']);
        },
      });
  }
}
