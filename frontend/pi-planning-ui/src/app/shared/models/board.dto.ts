/**
 * Client-side DTOs matching backend API responses
 */

export interface BoardResponseDto {
  id: number;
  name: string;
  organization?: string;
  project?: string;
  isLocked: boolean;
  isFinalized: boolean;
  devTestToggle: boolean;
  startDate: Date;
  sprints: SprintDto[];
  features: FeatureResponseDto[];
  teamMembers: TeamMemberResponseDto[];
}

export interface SprintDto {
  id: number;
  name: string;
  startDate: Date;
  endDate: Date;
}

export interface FeatureResponseDto {
  id: number;
  title: string;
  azureId?: string;
  priority?: number;
  valueArea?: string;
  userStories: UserStoryDto[];
}

export interface UserStoryDto {
  id: number;
  title: string;
  azureId?: string;
  storyPoints?: number;
  devStoryPoints?: number;
  testStoryPoints?: number;
  sprintId?: number;
  originalSprintId?: number;
  isMoved: boolean;
}

export interface TeamMemberResponseDto {
  id: number;
  name: string;
  isDev: boolean;
  isTest: boolean;
  sprintCapacities: TeamMemberSprintDto[];
}

export interface TeamMemberSprintDto {
  sprintId: number;
  capacityDev: number;
  capacityTest: number;
}
