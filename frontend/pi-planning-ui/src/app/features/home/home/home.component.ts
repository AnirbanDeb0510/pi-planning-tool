import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

/**
 * Home Component - Landing Page
 * Entry point for the application with navigation to create/load boards
 */
@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'],
})
export class HomeComponent {
  private readonly router = inject(Router);

  navigateToCreate(): void {
    this.router.navigate(['/boards/new']);
  }

  navigateToBoardList(): void {
    this.router.navigate(['/boards']);
  }
}
