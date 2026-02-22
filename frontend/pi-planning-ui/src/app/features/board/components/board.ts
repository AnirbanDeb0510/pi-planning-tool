import { Component, inject, signal, ChangeDetectorRef, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import {
  CdkDragDrop,
  DragDropModule,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { FormsModule } from '@angular/forms';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { UserService } from '../../../core/services/user.service';
import { BoardService } from '../services/board.service';
import { StoryService } from '../services/story.service';
import { FeatureService } from '../services/feature.service';
import { TeamService } from '../services/team.service';
import {
  SprintDto,
  FeatureResponseDto,
  UserStoryDto,
  TeamMemberResponseDto,
} from '../../../shared/models/board.dto';
import { LABELS, MESSAGES, VALIDATIONS, TOOLTIPS, PLACEHOLDERS } from '../../../shared/constants';
import { BoardHeader } from './board-header/board-header';
import { TeamBar } from './team-bar/team-bar';
import { CapacityRow } from './capacity-row/capacity-row';
import { SprintHeader } from './sprint-header/sprint-header';
import { FeatureRow } from './feature-row/feature-row';
import { BoardModals } from './board-modals/board-modals';

@Component({
  selector: 'app-board',
  standalone: true,
  imports: [
    CommonModule,
    DragDropModule,
    FormsModule,
    MatMenuModule,
    MatIconModule,
    MatButtonModule,
    BoardHeader,
    TeamBar,
    CapacityRow,
    SprintHeader,
    FeatureRow,
    BoardModals,
  ],
  templateUrl: './board.html',
  styleUrls: ['./board.css'],
})
export class Board implements OnInit {
  public boardService = inject(BoardService);
  public storyService = inject(StoryService);
  public featureService = inject(FeatureService);
  public teamService = inject(TeamService);
  private userService = inject(UserService);
  private route = inject(ActivatedRoute);
  protected router = inject(Router); // Make public for template
  private cdr = inject(ChangeDetectorRef);

  // Board state from service
  protected board = this.boardService.board;
  protected loading = this.boardService.loading;
  protected error = this.boardService.error;

  protected readonly LABELS = LABELS;
  protected readonly MESSAGES = MESSAGES;
  protected readonly VALIDATIONS = VALIDATIONS;
  protected readonly TOOLTIPS = TOOLTIPS;
  protected readonly PLACEHOLDERS = PLACEHOLDERS;

  // Local UI state - needed by sub components
  protected cursorName = signal(this.userService.getName() || LABELS.APP.GUEST);
  protected cursorX = signal(0);
  protected cursorY = signal(0);
  public showDevTest = signal(false);

  // Board finalization state - managed here, used by board-header and board-modals
  public showFinalizeConfirmation = signal(false);
  public finalizationWarnings = signal<string[]>([]);
  public finalizationLoading = signal(false);
  public finalizationError = signal<string | null>(null);

  @ViewChild(BoardModals)
  private boardModals?: BoardModals;

  // PAT modal state
  protected showPatModal = signal(false);
  protected patModalInput = signal('');
  protected patValidationError = signal<string | null>(null);
  protected patValidationLoading = signal(false);
  protected currentBoardId = signal<number | null>(null);
  protected patValidated = signal(false);
  protected boardPreview = signal<any>(null);

  ngOnInit(): void {
    // Load board from route parameter
    this.route.params.subscribe(async (params) => {
      const boardId = Number(params['id']);
      if (boardId) {
        this.currentBoardId.set(boardId);
        this.patValidated.set(false);
        this.patModalInput.set('');
        this.patValidationError.set(null);

        // First, check if board requires PAT validation using lightweight preview endpoint
        const preview = await this.boardService.getBoardPreview(boardId);

        if (preview && preview.featureCount > 0 && preview.sampleFeatureAzureId) {
          // Board has features - store preview data and show PAT modal before loading board data
          this.boardPreview.set(preview);
          this.showPatModal.set(true);
        } else {
          // No PAT needed, load board immediately
          this.patValidated.set(true);
          this.boardService.loadBoard(boardId);
        }
      } else {
        console.error('No board ID provided');
        this.router.navigate(['/']);
      }
    });
  }

  /**
   * Validate PAT and proceed to load board if valid
   */
  protected async validatePat(): Promise<void> {
    const boardId = this.currentBoardId();
    const pat = this.patModalInput();
    const preview = this.boardPreview();

    if (!boardId || !pat) {
      this.patValidationError.set(VALIDATIONS.PAT.REQUIRED);
      return;
    }

    if (!preview || !preview.organization || !preview.project || !preview.sampleFeatureAzureId) {
      this.patValidationError.set(VALIDATIONS.BOARD.MISSING_PREVIEW);
      return;
    }

    this.patValidationLoading.set(true);
    this.patValidationError.set(null);

    try {
      const isValid = await this.boardService.validatePatForBoard(
        preview.organization,
        preview.project,
        preview.sampleFeatureAzureId,
        pat
      );

      if (isValid) {
        this.patValidated.set(true);
        this.showPatModal.set(false);
        this.patModalInput.set(''); // Clear input after successful validation

        // Now load the board with validated PAT
        this.boardService.loadBoard(boardId);
      } else {
        this.patValidationError.set(VALIDATIONS.PAT.INVALID);
      }
    } catch (error) {
      this.patValidationError.set(VALIDATIONS.PAT.ERROR);
      console.error('PAT validation error:', error);
    } finally {
      this.patValidationLoading.set(false);
    }
  }

  /**
   * Close PAT modal (cancel)
   */
  protected closePatModal(): void {
    this.showPatModal.set(false);
    this.patModalInput.set('');
    this.patValidationError.set(null);
    // Navigate away if PAT validation is required but user cancelled
    this.router.navigate(['/']);
  }

  public openImportFeatureModal(): void {
    this.boardModals?.openImportFeatureModal();
  }

  public openRefreshFeatureModal(feature: FeatureResponseDto): void {
    this.boardModals?.openRefreshFeatureModal(feature);
  }

  public openDeleteFeatureModal(feature: FeatureResponseDto): void {
    this.boardModals?.openDeleteFeatureModal(feature);
  }

  /**
   * Get all sprints from the board (skip Sprint 0 for main display)
   */
  public getDisplayedSprints(): SprintDto[] {
    const currentBoard = this.board();
    if (!currentBoard) return [];
    return currentBoard.sprints.filter((s) => this.isParkingLotSprint(s) === false);
  }

  /**
   * Public method to get sprint name by ID (used by child components)
   */
  getSprintNameById(sprintId: number | undefined): string {
    if (!sprintId) return LABELS.FIELDS.PARKING_LOT;
    const currentBoard = this.board();
    if (!currentBoard) return `${LABELS.FIELDS.SPRINT} ${sprintId}`;
    const sprint = currentBoard.sprints.find(s => s.id === sprintId);
    return sprint?.name || `${LABELS.FIELDS.SPRINT} ${sprintId}`;
  }

  private isParkingLotSprint(sprint: SprintDto): boolean {
    return sprint.name?.trim().toLowerCase() === 'sprint 0';
  }

  /**
   * Get the Sprint 0 (parking lot) ID from the board
   */
  private getParkingLotSprintId(): number {
    const currentBoard = this.board();
    if (!currentBoard) return 0;
    const sprint0 = currentBoard.sprints.find((s) => this.isParkingLotSprint(s));
    return sprint0?.id ?? 0;
  }

  public getTeamMembers(): TeamMemberResponseDto[] {
    const currentBoard = this.board();
    return currentBoard ? currentBoard.teamMembers : [];
  }

  public getMemberRoleLabel(member: TeamMemberResponseDto): string {
    if (member.isDev && member.isTest) {
      return LABELS.ROLES.DEV_TEST;
    }
    if (member.isDev) {
      return LABELS.ROLES.DEV;
    }
    if (member.isTest) {
      return LABELS.ROLES.TEST;
    }
    return LABELS.ROLES.MEMBER;
  }

  public getMemberSprintCapacity(
    member: TeamMemberResponseDto,
    sprintId: number,
  ): { dev: number; test: number } {
    const entry = member.sprintCapacities.find((cap) => cap.sprintId === sprintId);
    return {
      dev: entry?.capacityDev ?? 0,
      test: entry?.capacityTest ?? 0,
    };
  }

  public getSprintCapacityTotals(sprintId: number): {
    dev: number;
    test: number;
    total: number;
  } {
    let dev = 0;
    let test = 0;

    this.getTeamMembers().forEach((member) => {
      const cap = this.getMemberSprintCapacity(member, sprintId);
      dev += cap.dev;
      test += cap.test;
    });

    return { dev, test, total: dev + test };
  }

  public isSprintOverCapacity(sprintId: number, type: 'dev' | 'test' | 'total'): boolean {
    const load = this.getSprintTotals(sprintId);
    const cap = this.getSprintCapacityTotals(sprintId);

    if (type === 'dev') {
      return load.dev > cap.dev;
    }
    if (type === 'test') {
      return load.test > cap.test;
    }
    return load.total > cap.total;
  }

  /**
   * Construct a grid-template-columns string for header and rows so columns align.
   * First column: feature (fixed), second: parking lot (fixed), rest: sprints (flex)
   */
  public getGridTemplateColumns(): string {
    const currentBoard = this.board();
    if (!currentBoard) return '';
    const sprints = this.getDisplayedSprints();
    const featureCol = '140px';
    const parkingCol = '240px';
    const sprintCols = sprints.length > 0 ? sprints.map(() => 'minmax(220px, 1fr)').join(' ') : '';
    return `${featureCol} ${parkingCol}${sprintCols ? ' ' + sprintCols : ''}`;
  }

  /**
   * Drag-drop handler - move story between sprints or parking lot
   */
  drop(event: CdkDragDrop<UserStoryDto[]>): void {
    // Get story from the previous container's data array (not from event.item.data which is unstable)
    const story = event.previousContainer.data[event.previousIndex];
    if (!story || !story.id) {
      console.error('Invalid story in drop event', story);
      return;
    }

    const storyId = story.id;
    const previousId = event.previousContainer.id;
    const targetId = event.container.id;

    console.log('Drop handler called', {
      previousContainerId: previousId,
      targetContainerId: targetId,
      storyId,
      previousIndex: event.previousIndex,
      currentIndex: event.currentIndex,
    });

    // If same container and same index, no-op
    if (event.previousContainer === event.container && event.previousIndex === event.currentIndex) {
      console.log('Same container, same index, no move needed');
      return;
    }

    const previousSprintId = this.parseSprintIdFromDropListId(previousId);
    const targetSprintId = this.parseSprintIdFromDropListId(targetId);

    console.log('Moving story', { storyId, previousSprintId, targetSprintId });

    // Use CDK utilities to immediately move the item in the arrays
    if (event.previousContainer !== event.container) {
      // Moving between different drop lists
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex,
      );
    } else {
      // Reordering within same list
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    }

    // Update service state
    this.storyService.moveStory(storyId, previousSprintId, targetSprintId);

    // Force change detection
    this.cdr.detectChanges();
  }

  /**
   * Drag-drop handler - reorder features
   */
  dropFeature(event: CdkDragDrop<FeatureResponseDto[]>): void {
    const currentBoard = this.board();
    if (!currentBoard) return;

    if (event.previousIndex === event.currentIndex) return;

    moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);

    const updates: Array<{ featureId: number; newPriority: number }> = [];
    event.container.data.forEach((feature, index) => {
      const newPriority = index + 1;
      if (feature.priority !== newPriority) {
        feature.priority = newPriority;
        updates.push({ featureId: feature.id, newPriority });
      }
    });

    if (updates.length === 0) return;

    this.featureService
      .reorderFeatures(currentBoard.id, updates)
      .catch((error: any) => {
        console.error('Error reordering features:', error);
        this.boardService.loadBoard(currentBoard.id);
      });

    this.cdr.detectChanges();
  }

  /**
   * Helper: parse sprint ID from drop list ID
   * 'feature_1_parkingLot' → Sprint 0 ID
   * 'feature_1_sprint_2' → 2
   */
  private parseSprintIdFromDropListId(id: string): number {
    if (id.includes('parkingLot')) {
      return this.getParkingLotSprintId(); // Return actual Sprint 0 ID
    }
    // For sprint IDs: 'feature_X_sprint_Y' → extract Y
    const parts = id.split('_');
    return parseInt(parts[parts.length - 1], 10);
  }

  /**
   * Get connected drop lists for a feature (parking lot + all sprints, excluding Sprint 0)
   */
  getConnectedLists(featureId: number): string[] {
    const currentBoard = this.board();
    if (!currentBoard) return [];
    const lists = [`feature_${featureId}_parkingLot`];
    currentBoard.sprints
      .filter((sprint) => this.isParkingLotSprint(sprint) === false)
      .forEach((sprint) => {
        lists.push(`feature_${featureId}_sprint_${sprint.id}`);
      });
    return lists;
  }

  /**
   * Get stories for a feature in a specific sprint
   */
  getStoriesInSprint(feature: FeatureResponseDto, sprintId: number): UserStoryDto[] {
    return feature.userStories.filter((story) => story.sprintId === sprintId);
  }

  /**
   * Get stories for a feature in parking lot (Sprint 0)
   */
  getParkingLotStories(feature: FeatureResponseDto): UserStoryDto[] {
    const parkingLotId = this.getParkingLotSprintId();
    return feature.userStories.filter((story) => story.sprintId === parkingLotId);
  }

  /**
   * Calculate total story points for a story
   */
  private getStoryTotalPoints(story: UserStoryDto): number {
    const hasDevOrTest = (story.devStoryPoints ?? 0) + (story.testStoryPoints ?? 0);
    const dev = story.devStoryPoints ?? 0;
    const test = story.testStoryPoints ?? 0;
    const base = story.storyPoints ?? 0;

    return hasDevOrTest > 0 ? dev + test : base;
  }

  /**
   * Get feature-level total points
   */
  getFeatureTotal(feature: FeatureResponseDto): number {
    return feature.userStories.reduce((sum, s) => sum + this.getStoryTotalPoints(s), 0);
  }

  /**
   * Get sprint totals (dev, test, total) across all features
   */
  getSprintTotals(sprintId: number): { dev: number; test: number; total: number } {
    const currentBoard = this.board();
    if (!currentBoard) return { dev: 0, test: 0, total: 0 };
    
    let dev = 0,
      test = 0,
      total = 0;

    currentBoard.features.forEach((feature) => {
      const sprintStories = this.getStoriesInSprint(feature, sprintId);
      sprintStories.forEach((story) => {
        dev += story.devStoryPoints ?? 0;
        test += story.testStoryPoints ?? 0;
        total += this.getStoryTotalPoints(story);
      });
    });

    return { dev, test, total };
  }

  /**
   * Get feature-level totals for a specific sprint
   */
  getFeatureSprintDevTestTotals(
    feature: FeatureResponseDto,
    sprintId: number,
  ): { dev: number; test: number; total: number } {
    const sprintStories = this.getStoriesInSprint(feature, sprintId);

    let dev = 0,
      test = 0,
      total = 0;

    sprintStories.forEach((story) => {
      dev += story.devStoryPoints ?? 0;
      test += story.testStoryPoints ?? 0;
      total += this.getStoryTotalPoints(story);
    });

    return { dev, test, total };
  }

  /**
   * Toggle dev/test display
   */
  toggleDevTest(): void {
    this.showDevTest.update((val) => !val);
    this.boardService.toggleDevTestToggle();
  }

  /**
   * Mouse move handler for cursor display
   */
  onMouseMove(event: MouseEvent): void {
    this.cursorX.set(event.clientX + 20);
    this.cursorY.set(event.clientY + 20);
  }

  async openFinalizeConfirmation(): Promise<void> {
    const currentBoard = this.board();
    if (!currentBoard || currentBoard.isFinalized) return;

    this.finalizationError.set(null);
    this.finalizationLoading.set(true);

    try {
      // Fetch warnings from board service
      const warnings = await this.boardService.getFinalizationWarnings(currentBoard.id);
      this.finalizationWarnings.set(warnings);
      this.showFinalizeConfirmation.set(true);
    } catch (error: any) {
      this.finalizationError.set(error.message || MESSAGES.BOARD.FINALIZE_WARNINGS_FAILED);
    } finally {
      this.finalizationLoading.set(false);
    }
  }

  /**
   * Close finalize confirmation dialog
   */
  closeFinalizeConfirmation(): void {
    this.showFinalizeConfirmation.set(false);
    this.finalizationWarnings.set([]);
    this.finalizationLoading.set(false);
    this.finalizationError.set(null);
  }

  /**
   * Finalize board
   */
  async finalizeBoard(): Promise<void> {
    const currentBoard = this.board();
    if (!currentBoard) return;

    this.finalizationLoading.set(true);
    this.finalizationError.set(null);

    try {
      await this.boardService.finalizeBoard(currentBoard.id);
      this.closeFinalizeConfirmation();
    } catch (error: any) {
      this.finalizationError.set(error.message || MESSAGES.BOARD.FINALIZE_FAILED);
    } finally {
      this.finalizationLoading.set(false);
    }
  }

  /**
   * Restore board (allow further editing)
   */
  async restoreBoard(): Promise<void> {
    const currentBoard = this.board();
    if (!currentBoard || !currentBoard.isFinalized) return;

    this.finalizationLoading.set(true);
    this.finalizationError.set(null);

    try {
      await this.boardService.restoreBoard(currentBoard.id);
    } catch (error: any) {
      this.finalizationError.set(error.message || MESSAGES.BOARD.RESTORE_FAILED);
    } finally {
      this.finalizationLoading.set(false);
    }
  }

  /**
   * Check if operation is blocked by finalization
   */
  isOperationBlocked(): boolean {
    const board = this.board();
    return board?.isFinalized ?? false;
  }

  /**
   * Get operation blocked error message
   */
  getOperationBlockedMessage(operation: string): string {
    return MESSAGES.BOARD.OPERATION_BLOCKED(operation);
  }

  /**
   * Check if board is finalized (for child components)
   */
  isBoardFinalized(): boolean {
    return this.board()?.isFinalized ?? false;
  }
}

