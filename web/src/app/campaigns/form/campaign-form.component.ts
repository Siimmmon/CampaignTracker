import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { CampaignApiService } from '../campaign-api.service';

const datePattern = /^(?!0000)\d{4}-\d{2}-\d{2}$/;

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

  protected readonly name = new FormControl('', {
    nonNullable: true,
    validators: (control) => {
      const name = control.value.trim();
      if (!name) return { required: true };
      if (name.length > 120) return { length: true };
      return /[<>\u0000-\u001f\u007f-\u009f]/u.test(name) ? { plainText: true } : null;
    },
  });
  protected readonly startDate = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.pattern(datePattern)],
  });
  protected readonly endDate = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.pattern(datePattern)],
  });
  protected readonly form = new FormGroup(
    { name: this.name, startDate: this.startDate, endDate: this.endDate },
    {
      validators: () => this.startDate.valid && this.endDate.valid && this.endDate.value <= this.startDate.value
        ? { dateOrder: true }
        : null,
    },
  );

  protected submit(): void {
    if (this.saving()) return;

    this.serverErrors.set([]);
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const { name, startDate, endDate } = this.form.getRawValue();
    this.saving.set(true);
    this.api.create({ name: name.trim(), startDate, endDate })
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
