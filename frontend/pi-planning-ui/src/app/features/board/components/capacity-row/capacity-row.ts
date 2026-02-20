import { Component, Input, signal, Signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Board } from '../board';
import { BoardResponseDto, TeamMemberResponseDto } from '../../../../shared/models/board.dto';
import { TeamService } from '../../services/team.service';

@Component({
  selector: 'app-capacity-row',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './capacity-row.html',
  styleUrls: ['./capacity-row.css'],
})
export class CapacityRow {
  @Input() board!: Signal<BoardResponseDto | null>;
  @Input() parent!: Board;
  @Input() showDevTest!: Signal<boolean>;
  private teamService = inject(TeamService);

  protected showCapacityModal = signal(false);
  protected selectedSprintId = signal<number | null>(null);
  protected capacityEdits = signal<Record<number, { dev: number; test: number }>>({});
  protected capacityFormError = signal('');

  protected openCapacityEditor(sprintId: number): void {
    this.selectedSprintId.set(sprintId);
    const edits: Record<number, { dev: number; test: number }> = {};
    this.getTeamMembers().forEach((member) => {
      const current = this.parent.getMemberSprintCapacity(member, sprintId);
      edits[member.id] = { dev: current.dev, test: current.test };
    });
    this.capacityEdits.set(edits);
    this.showCapacityModal.set(true);
  }

  protected closeCapacityEditor(): void {
    this.showCapacityModal.set(false);
    this.selectedSprintId.set(null);
    this.capacityEdits.set({});
  }

  protected updateCapacityEdit(memberId: number, field: 'dev' | 'test', value: number): void {
    const edits = { ...this.capacityEdits() };
    const existing = edits[memberId] ?? { dev: 0, test: 0 };
    
    // When toggle is OFF, preserve role-based capacity field
    if (!this.showDevTest()) {
      const member = this.getTeamMembers().find(m => m.id === memberId);
      if (member) {
        // Preserve which role's capacity field we're editing
        if (member.isDev) {
          edits[memberId] = { dev: value, test: 0 };
        } else if (member.isTest) {
          edits[memberId] = { dev: 0, test: value };
        }
      }
    } else {
      edits[memberId] = { ...existing, [field]: value };
    }
    
    this.capacityEdits.set(edits);
  }

  protected saveCapacityEdits(): void {
    this.capacityFormError.set('');
    
    const sprintId = this.selectedSprintId();
    if (sprintId === null) {
      return;
    }

    // Find the sprint to get max capacity
    const sprint = this.board()?.sprints.find(s => s.id === sprintId);
    if (!sprint) return;

    // Calculate sprint duration in working days
    const startDate = new Date(sprint.startDate);
    const endDate = new Date(sprint.endDate);
    const msPerDay = 24 * 60 * 60 * 1000;
    const totalDays = Math.round((endDate.getTime() - startDate.getTime()) / msPerDay) + 1;
    const maxCapacity = Math.floor((totalDays / 7) * 5);

    // Validate capacities
    const edits = this.capacityEdits();
    for (const [id, values] of Object.entries(edits)) {
      // Check for integer values
      if (!Number.isInteger(values.dev) || !Number.isInteger(values.test)) {
        this.capacityFormError.set('Capacity must be a positive integer');
        return;
      }
      if (values.dev < 0 || values.test < 0) {
        this.capacityFormError.set('Capacity cannot be negative');
        return;
      }
      if (values.dev > maxCapacity || values.test > maxCapacity) {
        this.capacityFormError.set(`Capacity cannot exceed sprint duration (${maxCapacity} working days)`);
        return;
      }
    }

    // Save if validation passes
    Object.entries(edits).forEach(([id, values]) => {
      this.teamService.updateTeamMemberCapacity(Number(id), sprintId, values.dev, values.test);
    });
    this.closeCapacityEditor();
  }

  protected getTeamMembers(): TeamMemberResponseDto[] {
    return this.parent.getTeamMembers();
  }

  protected getMemberRoleLabel(member: TeamMemberResponseDto): string {
    if (member.isDev && member.isTest) {
      return 'Dev/Test';
    } else if (member.isDev) {
      return 'Dev';
    } else if (member.isTest) {
      return 'Test';
    }
    return '';
  }

  protected toNumber(value: string): number {
    return Number(value);
  }
}
