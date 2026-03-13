import { Injectable, inject } from '@angular/core';
import { FeatureApiService, AzureApiService } from './board-api.service';
import { IFeatureApiService, IAzureApiService } from './board-api.interface';
import { BoardService } from './board.service';
import { firstValueFrom } from 'rxjs';
import { MESSAGES } from '../../../shared/constants';
import { getErrorMessage } from '../../../core/utils/error-handler.util';

/**
 * Feature Service
 * Manages feature-related operations: import, refresh, reorder, delete
 */
@Injectable({ providedIn: 'root' })
export class FeatureService {
  private featureApi: IFeatureApiService = inject(FeatureApiService);
  private azureApi: IAzureApiService = inject(AzureApiService);
  private boardService = inject(BoardService);

  /**
   * Import feature from Azure DevOps
   * First fetches the feature from Azure, then imports it to the board
   */
  public async importFeature(boardId: number, featureId: string, pat: string): Promise<void> {
    try {
      // Step 1: Fetch feature from Azure DevOps
      const featureDto = await firstValueFrom(
        this.azureApi.getFeatureWithChildrenForBoard(boardId, featureId, pat),
      );

      // Step 2: Import the feature to the board
      await firstValueFrom(this.featureApi.importFeature(boardId, featureDto));

      // Step 3: Reload the board to ensure UI matches backend state
      this.boardService.loadBoard(boardId);
    } catch (error: unknown) {
      const message = getErrorMessage(error, MESSAGES.FEATURE.IMPORT_FAILED);
      console.error('Error importing feature:', error);
      throw new Error(message);
    }
  }

  /**
   * Refresh feature from Azure DevOps
   */
  public async refreshFeature(
    boardId: number,
    featureId: number,
    organization: string,
    project: string,
    pat: string,
  ): Promise<void> {
    try {
      await firstValueFrom(
        this.featureApi.refreshFeature(boardId, featureId, organization, project, pat),
      );

      // Reload board to show updated data
      this.boardService.loadBoard(boardId);
    } catch (error: unknown) {
      const message = getErrorMessage(error, MESSAGES.FEATURE.REFRESH_FAILED);
      console.error('Error refreshing feature:', error);
      throw new Error(message);
    }
  }

  /**
   * Reorder features by priority
   */
  public async reorderFeatures(
    boardId: number,
    features: Array<{ featureId: number; newPriority: number }>,
  ): Promise<void> {
    try {
      await firstValueFrom(this.featureApi.reorderFeatures(boardId, features));
      this.boardService.loadBoard(boardId);
    } catch (error: unknown) {
      const message = getErrorMessage(error, MESSAGES.FEATURE.REORDER_FAILED);
      console.error('Error reordering features:', error);
      throw new Error(message);
    }
  }

  /**
   * Delete feature and its user stories
   */
  public async deleteFeature(boardId: number, featureId: number): Promise<void> {
    try {
      await firstValueFrom(this.featureApi.deleteFeature(boardId, featureId));

      // Reload board to show updated data
      this.boardService.loadBoard(boardId);
    } catch (error: unknown) {
      const message = getErrorMessage(error, MESSAGES.FEATURE.DELETE_FAILED);
      console.error('Error deleting feature:', error);
      throw new Error(message);
    }
  }
}
