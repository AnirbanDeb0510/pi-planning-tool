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
  IMPORT: `${API_PREFIX}/features/import`,
  REORDER: (featureId: number) => `${API_PREFIX}/features/${featureId}/reorder`,
  REFRESH: (featureId: number) => `${API_PREFIX}/features/${featureId}/refresh`,
} as const;

/**
 * User Story API Endpoints
 */
export const STORY_API = {
  MOVE: (storyId: number) => `${API_PREFIX}/userstories/${storyId}/move`,
  REFRESH: (storyId: number) => `${API_PREFIX}/userstories/${storyId}/refresh`,
} as const;

/**
 * Team API Endpoints
 */
export const TEAM_API = {
  GET_MEMBERS: (boardId: number) => `${API_PREFIX}/team/${boardId}`,
  ADD_MEMBER: (boardId: number) => `${API_PREFIX}/team/${boardId}`,
  UPDATE_CAPACITY: (boardId: number) => `${API_PREFIX}/team/${boardId}/capacity`,
  REMOVE_MEMBER: (boardId: number, memberId: number) => `${API_PREFIX}/team/${boardId}/${memberId}`,
} as const;

/**
 * Azure DevOps Integration API Endpoints (if needed in future)
 */
export const AZURE_API = {
  AUTHENTICATE: `${API_PREFIX}/azure/auth`,
  SEARCH_FEATURES: `${API_PREFIX}/azure/features/search`,
} as const;
