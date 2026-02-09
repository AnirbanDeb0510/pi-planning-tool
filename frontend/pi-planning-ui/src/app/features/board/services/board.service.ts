import { Injectable, signal } from '@angular/core';
import {
  BoardResponseDto,
  FeatureResponseDto,
  SprintDto,
  UserStoryDto,
  TeamMemberResponseDto,
  TeamMemberSprintDto,
} from '../../../shared/models/board.dto';

@Injectable({ providedIn: 'root' })
export class BoardService {
  /**
   * Mock data - board state
   * In production, this would be replaced with HTTP calls to the backend API
   */
  private mockBoard: BoardResponseDto = {
    id: 1,
    name: 'Q1 2026 Planning',
    organization: 'Acme Corp',
    project: 'Platform',
    isLocked: false,
    isFinalized: false,
    devTestToggle: false,
    startDate: new Date('2026-02-10'),
    sprints: [],
    features: [],
    teamMembers: [],
  };

  // Signal-based state
  private boardSignal = signal<BoardResponseDto>(this.initializeBoardWithMockData());

  // Public read-only signal
  public board = this.boardSignal.asReadonly();

  constructor() {
    console.log('BoardService initialized with mock data');
  }

  /**
   * Initialize board with mock sprints, features, and stories
   */
  private initializeBoardWithMockData(): BoardResponseDto {
    const board = { ...this.mockBoard };

    // Generate sprints (0 = placeholder, 1-6 = actual sprints)
    board.sprints = this.generateMockSprints();

    // Generate features with stories
    board.features = this.generateMockFeatures(board.sprints);

    // Generate team members
    board.teamMembers = this.generateMockTeamMembers(board.sprints);

    return board;
  }

  /**
   * Generate mock sprints matching backend structure
   */
  private generateMockSprints(): SprintDto[] {
    const sprints: SprintDto[] = [];
    const startDate = new Date('2026-02-10');

    // Sprint 0: Placeholder
    sprints.push({
      id: 0,
      name: 'Sprint 0 (Parking Lot)',
      startDate: new Date(startDate),
      endDate: new Date(startDate),
    });

    // Sprints 1-6: Real sprints (14-day duration)
    for (let i = 1; i <= 6; i++) {
      const sprintStart = new Date(startDate);
      sprintStart.setDate(sprintStart.getDate() + (i - 1) * 14);

      const sprintEnd = new Date(sprintStart);
      sprintEnd.setDate(sprintEnd.getDate() + 13);

      sprints.push({
        id: i,
        name: `Sprint ${i}`,
        startDate: sprintStart,
        endDate: sprintEnd,
      });
    }

    return sprints;
  }

  /**
   * Generate mock features with stories
   */
  private generateMockFeatures(sprints: SprintDto[]): FeatureResponseDto[] {
    const featureNames = ['Auth', 'UI', 'Integration'];
    const features: FeatureResponseDto[] = [];

    featureNames.forEach((name, idx) => {
      const featureId = idx + 1;
      const feature: FeatureResponseDto = {
        id: featureId,
        title: name,
        azureId: `FEAT-${featureId}`,
        priority: idx + 1,
        valueArea: idx % 2 === 0 ? 'Architectural' : 'Business',
        userStories: this.generateMockStoriesForFeature(featureId),
      };
      features.push(feature);
    });

    return features;
  }

  /**
   * Generate stories for a feature
   * Stories are placed in Sprint 0 (parking lot) for now
   */
  private generateMockStoriesForFeature(featureId: number): UserStoryDto[] {
    const storyData = [
      { title: 'Implement core logic', dev: 5, test: 3 },
      { title: 'Add validation', dev: 3, test: 2 },
      { title: 'Create UI components', dev: 8, test: 4 },
    ];

    return storyData.map((data, idx) => ({
      id: featureId * 100 + idx + 1,
      title: data.title,
      azureId: `STORY-${featureId}-${idx + 1}`,
      storyPoints: data.dev + data.test,
      devStoryPoints: data.dev,
      testStoryPoints: data.test,
      sprintId: 0, // All start in parking lot (Sprint 0)
      originalSprintId: 0,
      isMoved: false,
    }));
  }

  /**
   * Calculate working days for a sprint based on duration (Mon-Fri).
   */
  private getWorkingDays(sprint: SprintDto): number {
    if (!sprint.startDate || !sprint.endDate) {
      return 0;
    }
    const msPerDay = 24 * 60 * 60 * 1000;
    const totalDays = Math.max(
      1,
      Math.round((sprint.endDate.getTime() - sprint.startDate.getTime()) / msPerDay) + 1,
    );
    return Math.floor((totalDays / 7) * 5);
  }

  /**
   * Generate mock team members with capacity
   */
  private generateMockTeamMembers(sprints: SprintDto[]): TeamMemberResponseDto[] {
    const teamNames = ['Alice', 'Bob', 'Charlie', 'Diana'];

    return teamNames.map((name, idx) => {
      const isDev = idx % 2 === 0;
      const isTest = idx % 2 === 1;

      return {
        id: idx + 1,
        name,
        isDev,
        isTest,
        sprintCapacities: sprints.map((sprint) => {
          const workingDays = sprint.id === 0 ? 0 : this.getWorkingDays(sprint);
          return {
            sprintId: sprint.id,
            capacityDev: isDev ? workingDays : 0,
            capacityTest: isTest ? workingDays : 0,
          };
        }),
      };
    });
  }

  /**
   * Get the current board state
   */
  public getBoard(): BoardResponseDto {
    return this.board();
  }

  /**
   * Move a story from one sprint to another (including parking lot)
   * Updates local state with deep copy to ensure change detection
   */
  public moveStory(storyId: number, fromSprintId: number, toSprintId: number): void {
    const currentBoard = this.boardSignal();

    // Find and update the story in features
    let found = false;
    const updatedFeatures = currentBoard.features.map((feature: FeatureResponseDto) => {
      const updatedStories = feature.userStories.map((s: UserStoryDto) => {
        if (s.id === storyId) {
          found = true;
          return {
            ...s,
            sprintId: toSprintId,
            isMoved: s.originalSprintId !== toSprintId,
          };
        }
        return s;
      });
      return {
        ...feature,
        userStories: updatedStories,
      };
    });

    if (found) {
      // Create a deep copy to trigger change detection
      const updatedBoard = {
        ...currentBoard,
        features: updatedFeatures,
      };
      this.boardSignal.set(updatedBoard);
      console.log(`Story ${storyId} moved from Sprint ${fromSprintId} to Sprint ${toSprintId}`);
    }
  }

  /**
   * Toggle dev/test display mode (for UI purposes)
   */
  public toggleDevTestToggle(): void {
    const currentBoard = this.boardSignal();
    const updatedBoard = {
      ...currentBoard,
      devTestToggle: !currentBoard.devTestToggle,
    };
    this.boardSignal.set(updatedBoard);
  }

  /**
   * Add a new team member with default capacities per sprint.
   */
  public addTeamMember(name: string, role: 'dev' | 'test', devTestEnabled: boolean): void {
    const currentBoard = this.boardSignal();
    const nextId = Math.max(0, ...currentBoard.teamMembers.map((m) => m.id)) + 1;

    const isDev = devTestEnabled ? role === 'dev' : true;
    const isTest = devTestEnabled ? role === 'test' : false;

    const newMember: TeamMemberResponseDto = {
      id: nextId,
      name,
      isDev,
      isTest,
      sprintCapacities: currentBoard.sprints.map((sprint) => {
        const workingDays = sprint.id === 0 ? 0 : this.getWorkingDays(sprint);
        return {
          sprintId: sprint.id,
          capacityDev: isDev ? workingDays : 0,
          capacityTest: isTest ? workingDays : 0,
        };
      }),
    };

    const updatedBoard = {
      ...currentBoard,
      teamMembers: [...currentBoard.teamMembers, newMember],
    };

    this.boardSignal.set(updatedBoard);
  }

  /**
   * Update capacities for a team member in a specific sprint.
   */
  public updateTeamMemberCapacity(
    memberId: number,
    sprintId: number,
    capacityDev: number,
    capacityTest: number,
  ): void {
    const currentBoard = this.boardSignal();

    const updatedMembers = currentBoard.teamMembers.map((member) => {
      if (member.id !== memberId) {
        return member;
      }

      const updatedCapacities = member.sprintCapacities.map((cap) => {
        if (cap.sprintId !== sprintId) {
          return cap;
        }
        return {
          ...cap,
          capacityDev,
          capacityTest,
        };
      });

      return {
        ...member,
        sprintCapacities: updatedCapacities,
      };
    });

    const updatedBoard = {
      ...currentBoard,
      teamMembers: updatedMembers,
    };

    this.boardSignal.set(updatedBoard);
  }

  /**
   * Submit board state (in production, this would persist to backend)
   */
  public submitBoard(): void {
    const currentBoard = this.boardSignal();
    console.log('Board submitted:', currentBoard);
    // In production: this.http.post('/api/boards/1/submit', currentBoard).subscribe(...)
  }
}
