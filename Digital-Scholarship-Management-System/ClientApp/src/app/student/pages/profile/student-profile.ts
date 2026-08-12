import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { merge } from 'rxjs';
import { StudentProfileService } from '../../services/student-profile.service';
import { ToastService } from '../../../shared/services/toast.service';
import { DigitsOnlyDirective } from '../../../shared/directives/digits-only.directive';
import {
  Gender,
  QualificationLevel,
  UpdateStudentProfileRequest,
} from '../../models/student-profile.model';
import { COUNTRIES } from '../../../shared/data/countries';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-student-profile',
  standalone: true,
  imports: [ReactiveFormsModule, DigitsOnlyDirective, RouterLink, LucideAngularModule],
  templateUrl: './student-profile.html',
})
export class StudentProfile {
  private readonly fb = inject(FormBuilder);
  private readonly profileService = inject(StudentProfileService);
  private readonly toast = inject(ToastService);

  protected readonly loading = signal(true);
  protected readonly submitting = signal(false);
  protected readonly countries = COUNTRIES;

  readonly form = this.fb.nonNullable.group({
    icNumber: ['', [Validators.required, Validators.pattern(/^\d{12}$/)]],
    dateOfBirth: ['', [Validators.required]],
    gender: ['', [Validators.required]],
    nationality: ['', [Validators.required]],
    race: [''],
    hasDisability: [false],
    disabilityDetails: [''],

    phoneNumber: ['', [Validators.required]],
    address: ['', [Validators.required]],
    emergencyContactName: ['', [Validators.required]],
    emergencyContactPhone: ['', [Validators.required]],

    fatherName: [''],
    fatherDeceased: [false],
    fatherOccupation: [''],
    motherName: [''],
    motherDeceased: [false],
    motherOccupation: [''],
    parentsDivorced: [false],
    guardianName: [''],
    guardianPhone: [''],
    householdIncome: [0, [Validators.required, Validators.min(0)]],
    numberOfSiblings: [0, [Validators.min(0), Validators.max(20)]],

    highestQualification: ['', [Validators.required]],
    examResults: [''],
    currentInstitution: [''],
    fieldOfStudy: [''],

    bankName: ['', [Validators.required]],
    bankAccNumber: ['', [Validators.required]],
  });

  constructor() {
    merge(
      this.form.controls.fatherDeceased.valueChanges,
      this.form.controls.motherDeceased.valueChanges,
    ).subscribe(() => this.updateGuardianValidator());

    this.loadProfile();
  }

  private updateGuardianValidator() {
    const bothDeceased =
      this.form.controls.fatherDeceased.value && this.form.controls.motherDeceased.value;
    const validators = bothDeceased ? [Validators.required] : [];
    this.form.controls.guardianName.setValidators(validators);
    this.form.controls.guardianName.updateValueAndValidity();
    this.form.controls.guardianPhone.setValidators(validators);
    this.form.controls.guardianPhone.updateValueAndValidity();
  }

  private async loadProfile() {
    try {
      const profile = await this.profileService.getProfile();
      if (profile) {
        this.form.patchValue({
          icNumber: profile.icNumber,
          dateOfBirth: profile.dateOfBirth.substring(0, 10),
          gender: profile.gender,
          nationality: profile.nationality,
          race: profile.race ?? '',
          hasDisability: profile.hasDisability,
          disabilityDetails: profile.disabilityDetails ?? '',
          phoneNumber: profile.phoneNumber,
          address: profile.address,
          emergencyContactName: profile.emergencyContactName,
          emergencyContactPhone: profile.emergencyContactPhone,
          fatherName: profile.fatherName ?? '',
          fatherDeceased: profile.fatherDeceased,
          fatherOccupation: profile.fatherOccupation ?? '',
          motherName: profile.motherName ?? '',
          motherDeceased: profile.motherDeceased,
          motherOccupation: profile.motherOccupation ?? '',
          parentsDivorced: profile.parentsDivorced,
          guardianName: profile.guardianName ?? '',
          guardianPhone: profile.guardianPhone ?? '',
          householdIncome: profile.householdIncome,
          numberOfSiblings: profile.numberOfSiblings,
          highestQualification: profile.highestQualification,
          examResults: profile.examResults ?? '',
          currentInstitution: profile.currentInstitution ?? '',
          fieldOfStudy: profile.fieldOfStudy ?? '',
          bankName: profile.bankName,
          bankAccNumber: profile.bankAccNumber,
        });
      }
    } catch {
      this.toast.error('Could not load your profile.');
    } finally {
      this.loading.set(false);
    }
  }

  async onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const v = this.form.getRawValue();

    // request: as type as ":" colon is saying the compiler to look at the models
    const request: UpdateStudentProfileRequest = {
      icNumber: v.icNumber, // The value is plain primitive.
      dateOfBirth: v.dateOfBirth,
      gender: v.gender as Gender,
      nationality: v.nationality,
      race: v.race || null,
      hasDisability: v.hasDisability,
      disabilityDetails: v.disabilityDetails || null,
      phoneNumber: v.phoneNumber,
      address: v.address,
      emergencyContactName: v.emergencyContactName,
      emergencyContactPhone: v.emergencyContactPhone,
      fatherName: v.fatherName || null,
      fatherDeceased: v.fatherDeceased,
      fatherOccupation: v.fatherOccupation || null,
      motherName: v.motherName || null,
      motherDeceased: v.motherDeceased,
      motherOccupation: v.motherOccupation || null,
      parentsDivorced: v.parentsDivorced,
      guardianName: v.guardianName || null,
      guardianPhone: v.guardianPhone || null,
      householdIncome: v.householdIncome,
      numberOfSiblings: v.numberOfSiblings,
      highestQualification: v.highestQualification as QualificationLevel,
      examResults: v.examResults || null,
      currentInstitution: v.currentInstitution || null,
      fieldOfStudy: v.fieldOfStudy || null,
      bankName: v.bankName,
      bankAccNumber: v.bankAccNumber,
    };

    try {
      await this.profileService.updateProfile(request);
      this.toast.success('Profile saved.');
    } catch (err) {
      if (err instanceof HttpErrorResponse && typeof err.error === 'string') {
        this.toast.error(err.error);
      } else {
        this.toast.error('Could not save your profile.');
      }
    } finally {
      this.submitting.set(false);
    }
  }
}
