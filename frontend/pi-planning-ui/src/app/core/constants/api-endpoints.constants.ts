/**
 * API Endpoints Constants
 * Centralized location for all API endpoint URLs
 */

const API_PREFIX = '/api';

/**
 * Board API Endpoints
 */
export const BOARD_API = {
  BASE: `${API_PREFIX}/boards`,
  GET_BOARD: (id: number) => `${API_PREFIX}/boards/${id}`,
  CREATE_BOARD: `${API_PREFIX}/boards`,
  GET_BOARD_LIST: `${API_PREFIX}/boards`,
  LOCK_BOARD: (id: number) => `${API_PREFIX}/boards/${id}/lock`,
  UNLOCK_BOARD: (id: number) => `${API_PREFIX}/boards/${id}/unlock`,
  FINALIZE_BOARD: (id: number) => `${API_PREFIX}/boards/${id}/finalize`,
  DELETE_BOARD: (id: number) => `${API_PREFIX}/boards/${id}`,
} as const;

/**
 * Feature API Endpoints
 */
export const FEATURE_API = {
  IMPORT: (boardId: number) => `${API_PREFIX}/v1/boards/${boardId}/features/import`,
  REORDER: (boardId: number) => `${API_PREFIX}/v1/boards/${boardId}/features/reorder`,
  REFRESH: (boardId: number, featureId: number) => `${API_PREFIX}/v1/boards/${boardId}/features/${featureId}/refresh`,
  DELETE: (boardId: number, featureId: number) => `${API_PREFIX}/v1/boards/${boardId}/features/${featureId}`,
} as const;

/**
 * User Story API Endpoints
 */
export const STORY_API = {
  MOVE: (boardId: number, storyId: number) => `${API_PREFIX}/boards/${boardId}/stories/${storyId}/move`,
  REFRESH: (boardId: number, storyId: number) => `${API_PREFIX}/boards/${boardId}/stories/${storyId}/refresh`,
} as const;

/**
 * Team API Endpoints
 */
export const TEAM_API = {
  GET_MEMBERS: (boardId: number) => `${API_PREFIX}/boards/${boardId}/team`,
  ADD_MEMBER: (boardId: number) => `${API_PREFIX}/boards/${boardId}/team`,
  UPDATE_MEMBER: (boardId: number, memberId: number) => `${API_PREFIX}/boards/${boardId}/team/${memberId}`,
  UPDATE_CAPACITY: (boardId: number, memberId: number, sprintId: number) =>
    `${API_PREFIX}/boards/${boardId}/team/${memberId}/sprints/${sprintId}`,
  REMOVE_MEMBER: (boardId: number, memberId: number) => `${API_PREFIX}/boards/${boardId}/team/${memberId}`,
} as const;

/**
 * Azure DevOps Integration API Endpoints
 */
export const AZURE_API = {
  GET_FEATURE: (organization: string, project: string, featureId: string) => 
    `${API_PREFIX}/v1/azure/feature/${organization}/${project}/${featureId}`,
  AUTHENTICATE: `${API_PREFIX}/azure/auth`,
  SEARCH_FEATURES: `${API_PREFIX}/azure/features/search`,
} as const;
