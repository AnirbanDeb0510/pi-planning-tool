import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { UserService } from '../../../core/services/user.service';
import { LABELS, MESSAGES, PLACEHOLDERS } from '../../constants';

@Component({
  selector: 'app-enter-your-name',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './enter-your-name.html',
  styleUrls: ['./enter-your-name.css']
})
export class EnterYourName {
  userName = '';

  protected readonly LABELS = LABELS;
  protected readonly MESSAGES = MESSAGES;
  protected readonly PLACEHOLDERS = PLACEHOLDERS;

  constructor(private router: Router, private userService: UserService) {}

  startBoard() {
    this.userService.setName(this.userName);
    this.router.navigate(['/board']);
  }
}
