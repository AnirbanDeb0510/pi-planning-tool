import { Component, Input, signal, Signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { Board } from '../board';
import { BoardResponseDto, TeamMemberResponseDto } from '../../../../shared/models/board.dto';
import { TeamService } from '../../services/team.service';
import { LABELS, MESSAGES, PLACEHOLDERS, TOOLTIPS, VALIDATIONS } from '../../../../shared/constants';

@Component({
  selector: 'app-team-bar',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, MatButtonModule, MatMenuModule],
  templateUrl: './team-bar.html',
  styleUrls: ['./team-bar.css'],
})
export class TeamBar {
  @Input() board!: Signal<BoardResponseDto | null>;
  @Input() parent!: Board;
  private teamService = inject(TeamService);

  // Team member modal state
  protected showAddMemberModal = signal(false);
  protected editingMember = signal<TeamMemberResponseDto | null>(null);
  protected newMemberName = signal('');
  protected newMemberRole = signal<'dev' | 'test'>('dev');
  protected memberFormError = signal('');

  protected showDeleteMemberModal = signal(false);
  protected memberToDelete = signal<TeamMemberResponseDto | null>(null);

  protected readonly LABELS = LABELS;
  protected readonly MESSAGES = MESSAGES;
  protected readonly PLACEHOLDERS = PLACEHOLDERS;
  protected readonly TOOLTIPS = TOOLTIPS;
  protected readonly VALIDATIONS = VALIDATIONS;

  protected openAddMember(): void {
    this.newMemberName.set('');
    this.newMemberRole.set('dev');
    this.editingMember.set(null);
    this.showAddMemberModal.set(true);
  }

  protected openEditMember(member: TeamMemberResponseDto): void {
    this.newMemberName.set(member.name);
    if (member.isDev && !member.isTest) {
      this.newMemberRole.set('dev');
    } else if (member.isTest && !member.isDev) {
      this.newMemberRole.set('test');
    } else {
      this.newMemberRole.set('dev');
    }
    this.editingMember.set(member);
    this.showAddMemberModal.set(true);
  }

  protected closeAddMember(): void {
    this.editingMember.set(null);
    this.showAddMemberModal.set(false);
  }

  protected saveNewMember(): void {
    this.memberFormError.set('');
    
    const name = this.newMemberName().trim();
    if (!name) {
      this.memberFormError.set(VALIDATIONS.TEAM_MEMBER.NAME_REQUIRED);
      return;
    }

    if (name.length > 100) {
      this.memberFormError.set(VALIDATIONS.TEAM_MEMBER.NAME_TOO_LONG);
      return;
    }

    const editing = this.editingMember();
    if (editing) {
      this.teamService.updateTeamMember(editing.id, name, this.newMemberRole(), this.parent.showDevTest());
    } else {
      this.teamService.addTeamMember(name, this.newMemberRole(), this.parent.showDevTest());
    }
    this.showAddMemberModal.set(false);
    this.editingMember.set(null);
    this.memberFormError.set('');
  }

  protected openDeleteMember(member: TeamMemberResponseDto): void {
    this.memberToDelete.set(member);
    this.showDeleteMemberModal.set(true);
  }

  protected closeDeleteMember(): void {
    this.memberToDelete.set(null);
    this.showDeleteMemberModal.set(false);
  }

  protected confirmDeleteMember(): void {
    const member = this.memberToDelete();
    if (!member) return;
    this.parent.boardService.clearError();
    this.teamService.removeTeamMember(member.id);
    this.closeDeleteMember();
  }

  protected getMemberRoleLabel(member: TeamMemberResponseDto): string {
    return this.parent.getMemberRoleLabel(member);
  }

  protected getTeamMembers(): TeamMemberResponseDto[] {
    return this.parent.getTeamMembers();
  }

  protected onRoleChange(value: string): void {
    this.newMemberRole.set(value === 'test' ? 'test' : 'dev');
  }
}
