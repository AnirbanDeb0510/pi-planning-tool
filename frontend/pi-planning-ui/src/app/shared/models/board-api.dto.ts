/**
 * DTOs for board creation and management
 */

export interface BoardCreateDto {
  name: string;
  organization?: string;
  project?: string;
  azureStoryPointField?: string;
  azureDevStoryPointField?: string;
  azureTestStoryPointField?: string;
  numSprints: number;
  sprintDuration: number;
  startDate: Date | string;
  devTestToggle?: boolean;
}

export interface BoardCreatedDto {
  id: number;
  name: string;
  organization?: string;
  project?: string;
  numSprints: number;
  sprintDuration: number;
  startDate: Date;
  isLocked: boolean;
  isFinalized: boolean;
  devTestToggle: boolean;
  createdAt: Date;
}

export interface BoardFilters {
  search?: string;
  organization?: string;
  project?: string;
  isLocked?: boolean;
  isFinalized?: boolean;
}

export interface BoardSummaryDto {
  id: number;
  name: string;
  organization?: string;
  project?: string;
  createdAt: Date;
  isLocked: boolean;
  isFinalized: boolean;
  sprintCount: number;
  featureCount: number;
}
