import { BoardCalculationService } from './board-calculation.service';
import {
  BoardResponseDto,
  FeatureResponseDto,
  SprintDto,
  TeamMemberResponseDto,
  UserStoryDto,
} from '../../../shared/models/board.dto';

describe('BoardCalculationService', () => {
  let service: BoardCalculationService;

  beforeEach(() => {
    service = new BoardCalculationService();
  });

  function createStory(overrides: Partial<UserStoryDto> = {}): UserStoryDto {
    return {
      id: overrides.id ?? 1,
      title: overrides.title ?? 'Story',
      storyPoints: overrides.storyPoints,
      devStoryPoints: overrides.devStoryPoints,
      testStoryPoints: overrides.testStoryPoints,
      sprintId: overrides.sprintId,
      originalSprintId: overrides.originalSprintId,
      isMoved: overrides.isMoved ?? false,
      azureId: overrides.azureId,
    };
  }

  function createFeature(overrides: Partial<FeatureResponseDto> = {}): FeatureResponseDto {
    return {
      id: overrides.id ?? 1,
      title: overrides.title ?? 'Feature',
      azureId: overrides.azureId,
      priority: overrides.priority,
      valueArea: overrides.valueArea,
      userStories: overrides.userStories ?? [],
    };
  }

  function createSprint(overrides: Partial<SprintDto> = {}): SprintDto {
    return {
      id: overrides.id ?? 1,
      name: overrides.name ?? 'Sprint 1',
      startDate: overrides.startDate ?? new Date('2026-01-01'),
      endDate: overrides.endDate ?? new Date('2026-01-14'),
    };
  }

  function createTeamMember(overrides: Partial<TeamMemberResponseDto> = {}): TeamMemberResponseDto {
    return {
      id: overrides.id ?? 1,
      name: overrides.name ?? 'Dev 1',
      isDev: overrides.isDev ?? true,
      isTest: overrides.isTest ?? false,
      sprintCapacities: overrides.sprintCapacities ?? [],
    };
  }

  function createBoard(overrides: Partial<BoardResponseDto> = {}): BoardResponseDto {
    return {
      id: overrides.id ?? 1,
      name: overrides.name ?? 'Board',
      organization: overrides.organization,
      project: overrides.project,
      isLocked: overrides.isLocked ?? false,
      isFinalized: overrides.isFinalized ?? false,
      devTestToggle: overrides.devTestToggle ?? true,
      startDate: overrides.startDate ?? new Date('2026-01-01'),
      sprints: overrides.sprints ?? [],
      features: overrides.features ?? [],
      teamMembers: overrides.teamMembers ?? [],
    };
  }

  it('uses dev and test points instead of base story points when available', () => {
    const story = createStory({ storyPoints: 13, devStoryPoints: 3, testStoryPoints: 2 });

    expect(service.getStoryTotalPoints(story)).toBe(5);
  });

  it('falls back to base story points when dev and test points are not provided', () => {
    const story = createStory({ storyPoints: 8 });

    expect(service.getStoryTotalPoints(story)).toBe(8);
  });

  it('calculates sprint totals across all features', () => {
    const board = createBoard({
      features: [
        createFeature({
          id: 1,
          userStories: [
            createStory({ id: 1, sprintId: 2, devStoryPoints: 3, testStoryPoints: 2 }),
            createStory({ id: 2, sprintId: 1, devStoryPoints: 10, testStoryPoints: 1 }),
          ],
        }),
        createFeature({
          id: 2,
          userStories: [createStory({ id: 3, sprintId: 2, storyPoints: 8 })],
        }),
      ],
    });

    expect(service.getSprintTotals(board, 2)).toEqual({ dev: 3, test: 2, total: 13 });
  });

  it('calculates sprint capacity totals across team members', () => {
    const members = [
      createTeamMember({
        id: 1,
        sprintCapacities: [
          { sprintId: 2, capacityDev: 5, capacityTest: 2 },
          { sprintId: 3, capacityDev: 1, capacityTest: 1 },
        ],
      }),
      createTeamMember({
        id: 2,
        sprintCapacities: [{ sprintId: 2, capacityDev: 4, capacityTest: 3 }],
      }),
    ];

    expect(service.getSprintCapacityTotals(members, 2)).toEqual({ dev: 9, test: 5, total: 14 });
  });

  it('detects over-capacity based on requested type', () => {
    const board = createBoard({
      features: [
        createFeature({
          userStories: [
            createStory({ id: 1, sprintId: 1, devStoryPoints: 7, testStoryPoints: 1 }),
            createStory({ id: 2, sprintId: 1, devStoryPoints: 2, testStoryPoints: 4 }),
          ],
        }),
      ],
      teamMembers: [
        createTeamMember({
          sprintCapacities: [{ sprintId: 1, capacityDev: 8, capacityTest: 10 }],
        }),
      ],
    });

    expect(service.isSprintOverCapacity(board, 1, 'dev')).toBeTrue();
    expect(service.isSprintOverCapacity(board, 1, 'test')).toBeFalse();
    expect(service.isSprintOverCapacity(board, 1, 'total')).toBeFalse();
  });

  it('finds the parking lot sprint id using a case-insensitive trimmed name match', () => {
    const board = createBoard({
      sprints: [
        createSprint({ id: 7, name: ' Sprint 0 ' }),
        createSprint({ id: 9, name: 'Sprint 1' }),
      ],
    });

    expect(service.getParkingLotSprintId(board)).toBe(7);
    expect(service.isParkingLotSprint(board.sprints[0])).toBeTrue();
  });

  it('parses parking lot and sprint ids from drag-drop list ids', () => {
    expect(service.parseSprintIdFromDropListId('feature_10_parkingLot', 99)).toBe(99);
    expect(service.parseSprintIdFromDropListId('feature_10_sprint_4', 99)).toBe(4);
  });

  it('builds connected lists with parking lot first and excludes Sprint 0 from sprint lists', () => {
    const board = createBoard({
      sprints: [
        createSprint({ id: 1, name: 'Sprint 0' }),
        createSprint({ id: 2, name: 'Sprint 1' }),
        createSprint({ id: 3, name: 'Sprint 2' }),
      ],
    });

    expect(service.getConnectedLists(board, 42)).toEqual([
      'feature_42_parkingLot',
      'feature_42_sprint_2',
      'feature_42_sprint_3',
    ]);
  });
});
