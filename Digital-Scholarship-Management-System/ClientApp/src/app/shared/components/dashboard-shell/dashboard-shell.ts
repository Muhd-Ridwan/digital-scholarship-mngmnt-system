import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TopNavbar } from '../top-navbar/top-navbar';
import { ToastContainer } from '../toast-container/toast-container';

@Component({
  selector: 'app-dashboard-shell',
  standalone: true,
  imports: [RouterOutlet, TopNavbar, ToastContainer],
  templateUrl: './dashboard-shell.html',
})
export class DashboardShell {}
