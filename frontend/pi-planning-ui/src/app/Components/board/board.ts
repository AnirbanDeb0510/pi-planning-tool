import { Component, inject, signal, ChangeDetectorRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import {
  CdkDragDrop,
  DragDropModule,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { FormsModule } from '@angular/forms';
import { StoryCard } from '../story-card/story-card';
import { UserService } from '../../Services/user.service';
import { BoardService } from '../../features/board/services/board.service';
import {
  SprintDto,
  FeatureResponseDto,
  UserStoryDto,
  TeamMemberResponseDto,
} from '../../shared/models/board.dto';

@Component({
  selector: 'app-board',
  standalone: true,
  imports: [CommonModule, DragDropModule, StoryCard, FormsModule],
  templateUrl: './board.html',
  styleUrls: ['./board.css'],
})
export class Board implements OnInit {
  protected boardService = inject(BoardService);
  private userService = inject(UserService);
  private route = inject(ActivatedRoute);
  protected router = inject(Router); // Make public for template
  private cdr = inject(ChangeDetectorRef);

  // Board state from service
  protected board = this.boardService.board;
  protected loading = this.boardService.loading;
  protected error = this.boardService.error;

  // Local UI state
  protected cursorName = signal(this.userService.getName() || 'Guest');
  protected cursorX = signal(0);
  protected cursorY = signal(0);
  public showDevTest = signal(false);
  protected showAddMemberModal = signal(false);
  protected showCapacityModal = signal(false);
  protected newMemberName = signal('');
  protected newMemberRole = signal<'dev' | 'test'>('dev');
  protected selectedSprintId = signal<number | null>(null);
  protected capacityEdits = signal<Record<number, { dev: number; test: number }>>({});

  ngOnInit(): void {
    // Load board from route parameter
    this.route.params.subscribe((params) => {
      const boardId = Number(params['id']);
      if (boardId) {
        this.boardService.loadBoard(boardId);
      } else {
        console.error('No board ID provided');
        this.router.navigate(['/']);
      }
    });
  }

  /**
   * Get all sprints from the board (skip Sprint 0 for main display)
   */
  protected getDisplayedSprints(): SprintDto[] {
    const currentBoard = this.board();
    if (!currentBoard) return [];
    return currentBoard.sprints.filter((s) => this.isParkingLotSprint(s) === false);
  }

  private isParkingLotSprint(sprint: SprintDto): boolean {
    return sprint.name?.trim().toLowerCase() === 'sprint 0';
  }

  protected getTeamMembers(): TeamMemberResponseDto[] {
    const currentBoard = this.board();
    return currentBoard ? currentBoard.teamMembers : [];
  }

  protected getMemberRoleLabel(member: TeamMemberResponseDto): string {
    if (member.isDev && member.isTest) {
      return 'Dev/Test';
    }
    if (member.isDev) {
      return 'Dev';
    }
    if (member.isTest) {
      return 'Test';
    }
    return 'Member';
  }

  protected getMemberSprintCapacity(
    member: TeamMemberResponseDto,
    sprintId: number,
  ): { dev: number; test: number } {
    const entry = member.sprintCapacities.find((cap) => cap.sprintId === sprintId);
    return {
      dev: entry?.capacityDev ?? 0,
      test: entry?.capacityTest ?? 0,
    };
  }

  protected getSprintCapacityTotals(sprintId: number): {
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

  protected isSprintOverCapacity(sprintId: number, type: 'dev' | 'test' | 'total'): boolean {
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
  protected getGridTemplateColumns(): string {
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
    this.boardService.moveStory(storyId, previousSprintId, targetSprintId);

    // Force change detection
    this.cdr.detectChanges();
  }

  /**
   * Helper: parse sprint ID from drop list ID
   * 'feature_1_parkingLot' → 0
   * 'feature_1_sprint_2' → 2
   */
  private parseSprintIdFromDropListId(id: string): number {
    if (id.includes('parkingLot')) {
      return 0; // Parking lot is Sprint 0
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
    return feature.userStories.filter((story) => story.sprintId === 0);
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

  protected openAddMember(): void {
    this.newMemberName.set('');
    this.newMemberRole.set('dev');
    this.showAddMemberModal.set(true);
  }

  protected closeAddMember(): void {
    this.showAddMemberModal.set(false);
  }

  protected saveNewMember(): void {
    const name = this.newMemberName().trim();
    if (!name) {
      return;
    }
    this.boardService.addTeamMember(name, this.newMemberRole(), this.showDevTest());
    this.showAddMemberModal.set(false);
  }

  protected openCapacityEditor(sprintId: number): void {
    this.selectedSprintId.set(sprintId);
    const edits: Record<number, { dev: number; test: number }> = {};
    this.getTeamMembers().forEach((member) => {
      const current = this.getMemberSprintCapacity(member, sprintId);
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
    edits[memberId] = { ...existing, [field]: value };
    this.capacityEdits.set(edits);
  }

  protected saveCapacityEdits(): void {
    const sprintId = this.selectedSprintId();
    if (sprintId === null) {
      return;
    }
    const edits = this.capacityEdits();
    Object.entries(edits).forEach(([id, values]) => {
      this.boardService.updateTeamMemberCapacity(Number(id), sprintId, values.dev, values.test);
    });
    this.closeCapacityEditor();
  }

  /**
   * Submit board result
   */
  sendBoardResult(): void {
    this.boardService.submitBoard();
  }

  /**
   * Mouse move handler for cursor display
   */
  onMouseMove(event: MouseEvent): void {
    this.cursorX.set(event.clientX + 20);
    this.cursorY.set(event.clientY + 20);
  }
}
