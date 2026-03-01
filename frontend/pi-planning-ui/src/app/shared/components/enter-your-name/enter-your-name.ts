import { Component, inject } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { UserService } from '../../../core/services/user.service';
import { LABELS, MESSAGES, PLACEHOLDERS } from '../../constants';

@Component({
  selector: 'app-enter-your-name',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './enter-your-name.html',
  styleUrls: ['./enter-your-name.css'],
})
export class EnterYourName {
  userName = '';
  errorMessage = '';

  protected readonly LABELS = LABELS;
  protected readonly MESSAGES = MESSAGES;
  protected readonly PLACEHOLDERS = PLACEHOLDERS;

  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly userService = inject(UserService);

  startBoard() {
    // Validate name
    const trimmedName = this.userName.trim();
    if (trimmedName.length < 2) {
      this.errorMessage = 'Name must be at least 2 characters';
      return;
    }

    // Clear any previous error
    this.errorMessage = '';

    // Set user name in service (persists to sessionStorage)
    this.userService.setName(trimmedName);

    // Get return URL from query params, default to /boards
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/boards';

    // Navigate to return URL or boards list
    this.router.navigateByUrl(returnUrl);
  }
}
