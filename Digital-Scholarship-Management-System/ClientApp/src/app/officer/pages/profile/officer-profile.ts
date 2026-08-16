import { Component, inject } from '@angular/core';
import { AuthService } from '../../../auth/services/auth.service';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-officer-profile',
  standalone: true,
  templateUrl: './officer-profile.html',
  imports: [RouterLink, LucideAngularModule],
})
export class OfficerProfile {

  private readonly auth = inject(AuthService);

  readonly profile = this.auth.profile;


  async ngOnInit() {
    await this.auth.getProfile();
  }

}