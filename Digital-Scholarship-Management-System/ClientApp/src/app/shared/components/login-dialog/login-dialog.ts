import { Component, inject, input, output } from '@angular/core';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-login-dialog',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './login-dialog.html',
})
export class LoginDialog {
  private readonly router = inject(Router);

  readonly open = input.required<boolean>();
  readonly message = input('You need to log in to continue.');
  readonly close = output<void>();

  onLogin() {
    this.close.emit();
    this.router.navigate(['/login'], {
      queryParams: {
        returnUrl: this.router.url,
      },
    });
  }
}
