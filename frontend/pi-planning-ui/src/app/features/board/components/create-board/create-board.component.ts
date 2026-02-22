import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { BoardApiService } from '../../services/board-api.service';
import { BoardCreateDto, BoardCreatedDto } from '../../../../shared/models/board-api.dto';
import { LABELS, MESSAGES, PLACEHOLDERS, VALIDATIONS } from '../../../../shared/constants';

/**
 * Create Board Component
 * Form to create a new PI Planning board
 */
@Component({
  selector: 'app-create-board',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-board.component.html',
  styleUrls: ['./create-board.component.css']
})
export class CreateBoardComponent {
  private boardApi = inject(BoardApiService);
  private router = inject(Router);

  protected loading = signal(false);
  protected error = signal<string | null>(null);

  protected readonly LABELS = LABELS;
  protected readonly MESSAGES = MESSAGES;
  protected readonly PLACEHOLDERS = PLACEHOLDERS;
  protected readonly VALIDATIONS = VALIDATIONS;

  protected formData = {
    name: '',
    organization: '',
    project: '',
    azureStoryPointField: 'Microsoft.VSTS.Scheduling.StoryPoints',
    azureDevStoryPointField: 'Custom.DevStoryPoints',
    azureTestStoryPointField: 'Custom.TestStoryPoints',
    numSprints: 6,
    sprintDuration: 14,
    startDate: this.getDefaultStartDate(),
    devTestToggle: false,
  };

  private getDefaultStartDate(): string {
    const today = new Date();
    return today.toISOString().split('T')[0];
  }

  goBack(): void {
    this.router.navigate(['/']);
  }

  onSubmit(): void {
    if (!this.formData.name || !this.formData.startDate) {
      this.error.set(VALIDATIONS.CREATE_BOARD.REQUIRED_FIELDS);
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const dto: BoardCreateDto = {
      name: this.formData.name,
      organization: this.formData.organization || undefined,
      project: this.formData.project || undefined,
      azureStoryPointField: this.formData.azureStoryPointField || undefined,
      azureDevStoryPointField: this.formData.devTestToggle
        ? this.formData.azureDevStoryPointField || undefined
        : undefined,
      azureTestStoryPointField: this.formData.devTestToggle
        ? this.formData.azureTestStoryPointField || undefined
        : undefined,
      numSprints: this.formData.numSprints,
      sprintDuration: this.formData.sprintDuration,
      startDate: this.formData.startDate,
      devTestToggle: this.formData.devTestToggle,
    };

    this.boardApi.createBoard(dto).subscribe({
      next: (response: BoardCreatedDto) => {
        console.log('Board created:', response);
        this.router.navigate(['/boards', response.id]);
      },
      error: (error: any) => {
        this.loading.set(false);
        this.error.set(error.message || MESSAGES.CREATE_BOARD.CREATE_FAILED);
        console.error('Error creating board:', error);
      },
    });
  }
}
