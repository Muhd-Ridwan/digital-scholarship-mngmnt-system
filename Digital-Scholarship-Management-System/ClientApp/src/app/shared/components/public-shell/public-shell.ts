import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PublicTopbar } from '../public-topbar/public-topbar';
import { ToastContainer } from '../toast-container/toast-container';

@Component({
  selector: 'app-public-shell',
  standalone: true,
  imports: [RouterOutlet, PublicTopbar, ToastContainer],
  templateUrl: './public-shell.html',
})
export class PublicShell {}
