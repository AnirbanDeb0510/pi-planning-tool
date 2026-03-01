import { Injectable } from '@angular/core';
import {
  FeatureResponseDto,
  UserStoryDto,
  SprintDto,
  BoardResponseDto,
  TeamMemberResponseDto,
} from '../../../shared/models/board.dto';

/**
 * Service for all board-related calculations (sprint totals, capacities, story points)
 * All methods are pure functions for easy testing
 */
@Injectable({
  providedIn: 'root',
})
export class BoardCalculationService {
  /**
   * Get stories for a feature in a specific sprint
   */
  getStoriesInSprint(feature: FeatureResponseDto, sprintId: number): UserStoryDto[] {
    return feature.userStories.filter((story) => story.sprintId === sprintId);
  }

  /**
   * Get stories for a feature in parking lot (Sprint 0)
   */
  getParkingLotStories(feature: FeatureResponseDto, parkingLotSprintId: number): UserStoryDto[] {
    return feature.userStories.filter((story) => story.sprintId === parkingLotSprintId);
  }

  /**
   * Calculate total story points for a story
   * Uses dev+test if available, otherwise uses base story points
   */
  getStoryTotalPoints(story: UserStoryDto): number {
    const hasDevOrTest = (story.devStoryPoints ?? 0) + (story.testStoryPoints ?? 0);
    const dev = story.devStoryPoints ?? 0;
    const test = story.testStoryPoints ?? 0;
    const base = story.storyPoints ?? 0;

    return hasDevOrTest > 0 ? dev + test : base;
  }

  /**
   * Get feature-level total points (sum of all stories)
   */
  getFeatureTotal(feature: FeatureResponseDto): number {
    return feature.userStories.reduce((sum, story) => sum + this.getStoryTotalPoints(story), 0);
  }

  /**
   * Get sprint totals (dev, test, total) across all features for a specific sprint
   */
  getSprintTotals(
    board: BoardResponseDto,
    sprintId: number,
  ): { dev: number; test: number; total: number } {
    let dev = 0;
    let test = 0;
    let total = 0;

    board.features.forEach((feature) => {
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

    let dev = 0;
    let test = 0;
    let total = 0;

    sprintStories.forEach((story) => {
      dev += story.devStoryPoints ?? 0;
      test += story.testStoryPoints ?? 0;
      total += this.getStoryTotalPoints(story);
    });

    return { dev, test, total };
  }

  /**
   * Get sprint capacity totals (dev, test, total) across all team members
   */
  getSprintCapacityTotals(
    teamMembers: TeamMemberResponseDto[],
    sprintId: number,
  ): { dev: number; test: number; total: number } {
    let dev = 0;
    let test = 0;

    teamMembers.forEach((member) => {
      const capacity = member.sprintCapacities.find((sc) => sc.sprintId === sprintId);
      if (capacity) {
        dev += capacity.capacityDev;
        test += capacity.capacityTest;
      }
    });

    return { dev, test, total: dev + test };
  }

  /**
   * Check if a sprint is over capacity
   */
  isSprintOverCapacity(
    board: BoardResponseDto,
    sprintId: number,
    type: 'dev' | 'test' | 'total',
  ): boolean {
    const capacityTotals = this.getSprintCapacityTotals(board.teamMembers, sprintId);
    const sprintTotals = this.getSprintTotals(board, sprintId);

    if (type === 'dev') {
      return sprintTotals.dev > capacityTotals.dev;
    } else if (type === 'test') {
      return sprintTotals.test > capacityTotals.test;
    } else {
      return sprintTotals.total > capacityTotals.total;
    }
  }

  /**
   * Get parking lot sprint ID (Sprint 0)
   */
  getParkingLotSprintId(board: BoardResponseDto): number {
    const parkingSprint = board.sprints.find((s) => this.isParkingLotSprint(s));
    return parkingSprint?.id ?? 0;
  }

  /**
   * Check if a sprint is the parking lot (Sprint 0)
   */
  isParkingLotSprint(sprint: SprintDto): boolean {
    return sprint.name?.trim().toLowerCase() === 'sprint 0';
  }

  /**
   * Parse sprint ID from drop list ID
   * 'feature_1_parkingLot' → Sprint 0 ID
   * 'feature_1_sprint_2' → 2
   */
  parseSprintIdFromDropListId(dropListId: string, parkingLotSprintId: number): number {
    if (dropListId.includes('parkingLot')) {
      return parkingLotSprintId;
    }
    // For sprint IDs: 'feature_X_sprint_Y' → extract Y
    const parts = dropListId.split('_');
    return parseInt(parts[parts.length - 1], 10);
  }

  /**
   * Get connected drop lists for a feature (parking lot + all sprints, excluding Sprint 0)
   */
  getConnectedLists(board: BoardResponseDto, featureId: number): string[] {
    const lists = [`feature_${featureId}_parkingLot`];
    board.sprints
      .filter((sprint) => !this.isParkingLotSprint(sprint))
      .forEach((sprint) => {
        lists.push(`feature_${featureId}_sprint_${sprint.id}`);
      });
    return lists;
  }
}
