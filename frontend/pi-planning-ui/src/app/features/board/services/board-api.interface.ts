import { Observable } from 'rxjs';
import {
  BoardResponseDto,
  FeatureResponseDto,
  UserStoryDto,
  TeamMemberResponseDto,
} from '../../../shared/models/board.dto';
import {
  BoardCreateDto,
  BoardCreatedDto,
  BoardSummaryDto,
  BoardFilters,
} from '../../../shared/models/board-api.dto';

/**
 * Board API Service Interface
 * Defines contract for board-related HTTP operations
 */
export interface IBoardApiService {
  /**
   * Get full board with hierarchy (sprints, features, stories, team)
   */
  getBoard(id: number): Observable<BoardResponseDto>;

  /**
   * Create a new board
   */
  createBoard(dto: BoardCreateDto): Observable<BoardCreatedDto>;

  /**
   * Get list of boards with optional filters
   */
  getBoardList(filters?: BoardFilters): Observable<BoardSummaryDto[]>;

  /**
   * Lock a board (prevent modifications)
   */
  lockBoard(id: number): Observable<void>;

  /**
   * Unlock a board
   */
  unlockBoard(id: number): Observable<void>;

  /**
   * Finalize a board (mark as complete)
   */
  finalizeBoard(id: number): Observable<void>;

  /**
   * Delete a board
   */
  deleteBoard(id: number): Observable<void>;
}

/**
 * Feature API Service Interface
 */
export interface IFeatureApiService {
  /**
   * Import feature from Azure DevOps
   */
  importFeature(boardId: number, azureFeatureId: string): Observable<FeatureResponseDto>;

  /**
   * Reorder feature priority
   */
  reorderFeatures(
    boardId: number,
    features: Array<{ featureId: number; newPriority: number }>
  ): Observable<void>;

  /**
   * Refresh feature from Azure
   */
  refreshFeature(
    boardId: number,
    featureId: number,
    organization: string,
    project: string,
    pat: string
  ): Observable<FeatureResponseDto>;

  /**
   * Delete feature and its user stories
   */
  deleteFeature(boardId: number, featureId: number): Observable<void>;
}

/**
 * User Story API Service Interface
 */
export interface IStoryApiService {
  /**
   * Move story to different sprint
   */
  moveStory(boardId: number, storyId: number, targetSprintId: number): Observable<void>;

  /**
   * Refresh story from Azure
   */
  refreshStory(boardId: number, storyId: number): Observable<UserStoryDto>;
}

/**
 * Team API Service Interface
 */
export interface ITeamApiService {
  /**
   * Get team members for a board
   */
  getTeamMembers(boardId: number): Observable<TeamMemberResponseDto[]>;

  /**
   * Add team member to board
   */
  addTeamMember(boardId: number, name: string, isDev: boolean, isTest: boolean): Observable<TeamMemberResponseDto>;

  /**
   * Update team member details
   */
  updateTeamMember(
    boardId: number,
    memberId: number,
    name: string,
    isDev: boolean,
    isTest: boolean
  ): Observable<TeamMemberResponseDto>;

  /**
   * Update team member capacity for a sprint
   */
  updateCapacity(
    boardId: number,
    memberId: number,
    sprintId: number,
    capacityDev: number,
    capacityTest: number
  ): Observable<void>;

  /**
   * Remove team member from board
   */
  removeTeamMember(boardId: number, memberId: number): Observable<void>;
}
