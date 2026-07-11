import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TopNavbar } from '../top-navbar/top-navbar';

@Component({
  selector: 'app-dashboard-shell',
  standalone: true,
  imports: [RouterOutlet, TopNavbar],
  templateUrl: './dashboard-shell.html',
})
export class DashboardShell {}
