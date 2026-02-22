import { Injectable, inject } from '@angular/core';
import { TeamApiService } from './board-api.service';
import { BoardService } from './board.service';
import { TeamMemberResponseDto } from '../../../shared/models/board.dto';
import { MESSAGES } from '../../../shared/constants';

/**
 * Team Service
 * Manages team member operations: add, update, remove, capacity management
 */
@Injectable({ providedIn: 'root' })
export class TeamService {
  private teamApi = inject(TeamApiService);
  private boardService = inject(BoardService);

  /**
   * Add a new team member with default capacities per sprint
   */
  public addTeamMember(name: string, role: 'dev' | 'test', devTestEnabled: boolean): void {
    const currentBoard = this.boardService.getBoard();
    if (!currentBoard) return;

    const isDev = devTestEnabled ? role === 'dev' : true;
    const isTest = devTestEnabled ? role === 'test' : false;

    // Calculate working days for default capacity
    const getWorkingDays = (startDate: Date | string, endDate: Date | string): number => {
      const start = startDate instanceof Date ? startDate : new Date(startDate);
      const end = endDate instanceof Date ? endDate : new Date(endDate);
      const msPerDay = 24 * 60 * 60 * 1000;
      const totalDays = Math.round((end.getTime() - start.getTime()) / msPerDay) + 1;
      return Math.floor((totalDays / 7) * 5);
    };

    // Create temporary member for optimistic update
    const nextId = Math.max(0, ...currentBoard.teamMembers.map((m) => m.id)) + 1;
    const tempMember: TeamMemberResponseDto = {
      id: nextId,
      name,
      isDev,
      isTest,
      sprintCapacities: currentBoard.sprints
        .filter((s) => s.id > 0)
        .map((sprint) => {
          const workingDays = getWorkingDays(sprint.startDate, sprint.endDate);
          return {
            sprintId: sprint.id,
            capacityDev: isDev ? workingDays : 0,
            capacityTest: isTest ? workingDays : 0,
          };
        }),
    };

    // Optimistic update
    const updatedBoard = {
      ...currentBoard,
      teamMembers: [...currentBoard.teamMembers, tempMember],
    };
    this.boardService.updateBoardState(updatedBoard);

    // Sync with backend
    this.teamApi.addTeamMember(currentBoard.id, name, isDev, isTest).subscribe({
      next: (member) => {
        // Replace temp member with real member from backend
        const finalBoard = {
          ...updatedBoard,
          teamMembers: updatedBoard.teamMembers.map((m) =>
            m.id === nextId ? member : m
          ),
        };
        this.boardService.updateBoardState(finalBoard);
        console.log('Team member added:', member);
      },
      error: (error) => {
        console.error('Error adding team member:', error);
        // Rollback on error
        this.boardService.updateBoardState(currentBoard);
        this.boardService.setError(MESSAGES.TEAM.ADD_FAILED);
      },
    });
  }

  /**
   * Update team member details (name/role)
   */
  public updateTeamMember(
    memberId: number,
    name: string,
    role: 'dev' | 'test',
    devTestEnabled: boolean
  ): void {
    const currentBoard = this.boardService.getBoard();
    if (!currentBoard) return;

    const isDev = devTestEnabled ? role === 'dev' : true;
    const isTest = devTestEnabled ? role === 'test' : false;

    const updatedMembers = currentBoard.teamMembers.map((member) =>
      member.id === memberId
        ? { ...member, name, isDev, isTest }
        : member
    );

    const updatedBoard = { ...currentBoard, teamMembers: updatedMembers };
    this.boardService.updateBoardState(updatedBoard);

    this.teamApi.updateTeamMember(currentBoard.id, memberId, name, isDev, isTest).subscribe({
      next: (member) => {
        const finalBoard = {
          ...updatedBoard,
          teamMembers: updatedBoard.teamMembers.map((m) =>
            m.id === memberId ? member : m
          ),
        };
        this.boardService.updateBoardState(finalBoard);
      },
      error: (error) => {
        console.error('Error updating team member:', error);
        this.boardService.updateBoardState(currentBoard);
        this.boardService.setError(MESSAGES.TEAM.UPDATE_FAILED);
      },
    });
  }

  /**
   * Remove a team member
   */
  public removeTeamMember(memberId: number): void {
    const currentBoard = this.boardService.getBoard();
    if (!currentBoard) return;

    const updatedBoard = {
      ...currentBoard,
      teamMembers: currentBoard.teamMembers.filter((m) => m.id !== memberId),
    };
    this.boardService.updateBoardState(updatedBoard);

    this.teamApi.removeTeamMember(currentBoard.id, memberId).subscribe({
      next: () => {
        console.log(`Team member ${memberId} removed`);
      },
      error: (error) => {
        console.error('Error removing team member:', error);
        this.boardService.updateBoardState(currentBoard);
        this.boardService.setError(MESSAGES.TEAM.REMOVE_FAILED);
      },
    });
  }

  /**
   * Update capacities for a team member in a specific sprint
   */
  public updateTeamMemberCapacity(
    memberId: number,
    sprintId: number,
    capacityDev: number,
    capacityTest: number
  ): void {
    const currentBoard = this.boardService.getBoard();
    if (!currentBoard) return;

    // Optimistic update
    const updatedMembers = currentBoard.teamMembers.map((member) => {
      if (member.id !== memberId) return member;

      const updatedCapacities = member.sprintCapacities.map((cap) => {
        if (cap.sprintId !== sprintId) return cap;
        return { ...cap, capacityDev, capacityTest };
      });

      return { ...member, sprintCapacities: updatedCapacities };
    });

    const updatedBoard = { ...currentBoard, teamMembers: updatedMembers };
    this.boardService.updateBoardState(updatedBoard);

    // Sync with backend
    this.teamApi
      .updateCapacity(currentBoard.id, memberId, sprintId, capacityDev, capacityTest)
      .subscribe({
        next: () => {
          console.log(`Capacity updated for member ${memberId} in sprint ${sprintId}`);
        },
        error: (error) => {
          console.error('Error updating capacity:', error);
          // Rollback on error
          this.boardService.updateBoardState(currentBoard);
          this.boardService.setError(MESSAGES.TEAM.CAPACITY_FAILED);
        },
      });
  }
}
