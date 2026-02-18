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
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
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
  imports: [CommonModule, DragDropModule, StoryCard, FormsModule, MatMenuModule, MatIconModule, MatButtonModule],
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
  protected editingMember = signal<TeamMemberResponseDto | null>(null);
  protected showDeleteMemberModal = signal(false);
  protected memberToDelete = signal<TeamMemberResponseDto | null>(null);
  protected showCapacityModal = signal(false);
  protected showImportFeatureModal = signal(false);
  protected newMemberName = signal('');
  protected newMemberRole = signal<'dev' | 'test'>('dev');
  protected memberFormError = signal('');
  protected selectedSprintId = signal<number | null>(null);
  protected capacityEdits = signal<Record<number, { dev: number; test: number }>>({});
  protected capacityFormError = signal('');
  
  // Import feature form state
  protected importFeatureId = signal('');
  protected importPat = signal('');
  protected rememberPatForImport = signal(false);
  protected importLoading = signal(false);
  protected importError = signal<string | null>(null);

  // Refresh feature form state
  protected showRefreshFeatureModal = signal(false);
  protected selectedFeature = signal<FeatureResponseDto | null>(null);
  protected refreshPat = signal('');
  protected rememberPatForRefresh = signal(false);
  protected refreshLoading = signal(false);
  protected refreshError = signal<string | null>(null);

  // Delete feature confirmation state
  protected showDeleteFeatureModal = signal(false);
  protected featureToDelete = signal<FeatureResponseDto | null>(null);
  protected deleteLoading = signal(false);
  protected deleteError = signal<string | null>(null);

  // Board finalization state
  protected showFinalizeConfirmation = signal(false);
  protected finalizationWarnings = signal<string[]>([]);
  protected finalizationLoading = signal(false);
  protected finalizationError = signal<string | null>(null);
  protected operationBlockedError = signal<string | null>(null);

  // PAT validation state
  protected showPatModal = signal(false);
  protected patModalInput = signal('');
  protected patValidationError = signal<string | null>(null);
  protected patValidationLoading = signal(false);
  protected currentBoardId = signal<number | null>(null);
  protected patValidated = signal(false);
  protected boardPreview = signal<any>(null);  // Store board preview for PAT validation

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
      this.patValidationError.set('Please enter a PAT');
      return;
    }

    if (!preview || !preview.organization || !preview.project || !preview.sampleFeatureAzureId) {
      this.patValidationError.set('Missing organization, project, or feature information');
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
        this.patValidationError.set('Invalid PAT or no permission to access this board');
      }
    } catch (error) {
      this.patValidationError.set('Error validating PAT. Please check your credentials.');
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

  /**
   * Open import feature modal
   */
  openImportFeatureModal(): void {
    this.importFeatureId.set('');
    const storedPat = this.boardService.getStoredPat();
    if (storedPat) {
      this.importPat.set(storedPat);
      this.rememberPatForImport.set(true);
    } else {
      this.importPat.set('');
      this.rememberPatForImport.set(false);
    }
    this.importError.set(null);
    this.showImportFeatureModal.set(true);
  }

  /**
   * Close import feature modal
   */
  closeImportFeatureModal(): void {
    this.showImportFeatureModal.set(false);
    this.importLoading.set(false);
    this.importError.set(null);
  }

  /**
   * Import feature from Azure DevOps
   */
  async importFeatureFromAzure(): Promise<void> {
    const currentBoard = this.board();
    if (!currentBoard) return;

    const featureId = this.importFeatureId().trim();
    const pat = this.importPat().trim();

    if (!featureId || !pat) {
      this.importError.set('Please provide Feature ID and PAT');
      return;
    }

    if (!currentBoard.organization || !currentBoard.project) {
      this.importError.set('Board is missing organization or project information');
      return;
    }

    this.importLoading.set(true);
    this.importError.set(null);

    try {
      await this.boardService.importFeature(
        currentBoard.id,
        currentBoard.organization,
        currentBoard.project,
        featureId,
        pat
      );
      if (this.rememberPatForImport()) {
        this.boardService.storePat(pat);
      } else {
        this.boardService.clearPat();
      }
      this.closeImportFeatureModal();
    } catch (error: any) {
      this.importError.set(error.message || 'Failed to import feature');
    } finally {
      this.importLoading.set(false);
    }
  }

  /**
   * Open refresh feature modal
   */
  openRefreshFeatureModal(feature: FeatureResponseDto): void {
    this.selectedFeature.set(feature);
    
    // Prepopulate PAT if stored and valid
    const storedPat = this.boardService.getStoredPat();
    if (storedPat) {
      this.refreshPat.set(storedPat);
      this.rememberPatForRefresh.set(true);
    } else {
      this.refreshPat.set('');
      this.rememberPatForRefresh.set(false);
    }
    
    this.refreshError.set(null);
    this.showRefreshFeatureModal.set(true);
  }

  /**
   * Close refresh feature modal
   */
  closeRefreshFeatureModal(): void {
    this.showRefreshFeatureModal.set(false);
    this.selectedFeature.set(null);
    this.refreshLoading.set(false);
    this.refreshError.set(null);
  }

  /**
   * Refresh feature from Azure DevOps
   */
  async refreshFeatureFromAzure(): Promise<void> {
    const feature = this.selectedFeature();
    const currentBoard = this.board();
    const pat = this.refreshPat().trim();
    
    if (!feature || !currentBoard || !pat) return;

    if (!currentBoard.organization || !currentBoard.project) {
      this.refreshError.set('Board is missing organization or project information');
      return;
    }

    this.refreshLoading.set(true);
    this.refreshError.set(null);
    
    try {
      await this.boardService.refreshFeature(
        currentBoard.id,
        feature.id,
        currentBoard.organization,
        currentBoard.project,
        pat
      );
      
      // Store PAT if checkbox is checked
      if (this.rememberPatForRefresh()) {
        this.boardService.storePat(pat);
      } else {
        this.boardService.clearPat();
      }
      
      this.closeRefreshFeatureModal();
    } catch (error: any) {
      this.refreshError.set(error.message || 'Failed to refresh feature');
    } finally {
      this.refreshLoading.set(false);
    }
  }

  /**
   * Open delete feature confirmation modal
   */
  openDeleteFeatureModal(feature: FeatureResponseDto): void {
    this.featureToDelete.set(feature);
    this.deleteError.set(null);
    this.showDeleteFeatureModal.set(true);
  }

  /**
   * Close delete feature confirmation modal
   */
  closeDeleteFeatureModal(): void {
    this.showDeleteFeatureModal.set(false);
    this.featureToDelete.set(null);
    this.deleteLoading.set(false);
    this.deleteError.set(null);
  }

  /**
   * Delete feature and its user stories
   */
  async deleteFeature(): Promise<void> {
    const feature = this.featureToDelete();
    const currentBoard = this.board();
    
    if (!feature || !currentBoard) return;

    this.deleteLoading.set(true);
    this.deleteError.set(null);
    
    try {
      await this.boardService.deleteFeature(currentBoard.id, feature.id);
      this.closeDeleteFeatureModal();
    } catch (error: any) {
      this.deleteError.set(error.message || 'Failed to delete feature');
    } finally {
      this.deleteLoading.set(false);
    }
  }

  /**
   * Get all sprints from the board (skip Sprint 0 for main display)
   */
  protected getDisplayedSprints(): SprintDto[] {
    const currentBoard = this.board();
    if (!currentBoard) return [];
    return currentBoard.sprints.filter((s) => this.isParkingLotSprint(s) === false);
  }

  /**
   * Public method to get sprint name by ID (used by child components)
   */
  getSprintNameById(sprintId: number | undefined): string {
    if (!sprintId) return 'Parking Lot';
    const currentBoard = this.board();
    if (!currentBoard) return `Sprint ${sprintId}`;
    const sprint = currentBoard.sprints.find(s => s.id === sprintId);
    return sprint?.name || `Sprint ${sprintId}`;
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

    this.boardService
      .reorderFeatures(currentBoard.id, updates)
      .catch((error) => {
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
      this.memberFormError.set('Team member name cannot be empty');
      return;
    }

    if (name.length > 100) {
      this.memberFormError.set('Team member name must be 100 characters or less');
      return;
    }

    const editing = this.editingMember();
    if (editing) {
      this.boardService.updateTeamMember(editing.id, name, this.newMemberRole(), this.showDevTest());
    } else {
      this.boardService.addTeamMember(name, this.newMemberRole(), this.showDevTest());
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
    this.boardService.removeTeamMember(member.id);
    this.closeDeleteMember();
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
      this.boardService.updateTeamMemberCapacity(Number(id), sprintId, values.dev, values.test);
    });
    this.closeCapacityEditor();
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
      this.finalizationError.set(error.message || 'Failed to fetch finalization warnings');
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
    this.operationBlockedError.set(null);

    try {
      await this.boardService.finalizeBoard(currentBoard.id);
      this.closeFinalizeConfirmation();
    } catch (error: any) {
      this.finalizationError.set(error.message || 'Failed to finalize board');
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
      this.finalizationError.set(error.message || 'Failed to restore board');
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
    return `Cannot ${operation} on a finalized board. Restore the board first.`;
  }

  /**
   * Check if board is finalized (for child components)
   */
  isBoardFinalized(): boolean {
    return this.board()?.isFinalized ?? false;
  }
}

