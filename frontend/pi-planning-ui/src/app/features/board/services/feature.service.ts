import { Injectable, inject } from '@angular/core';
import { FeatureApiService, AzureApiService } from './board-api.service';
import { BoardService } from './board.service';
import { firstValueFrom } from 'rxjs';
import { MESSAGES } from '../../../shared/constants';

/**
 * Feature Service
 * Manages feature-related operations: import, refresh, reorder, delete
 */
@Injectable({ providedIn: 'root' })
export class FeatureService {
  private featureApi = inject(FeatureApiService);
  private azureApi = inject(AzureApiService);
  private boardService = inject(BoardService);

  /**
   * Import feature from Azure DevOps
   * First fetches the feature from Azure, then imports it to the board
   */
  public async importFeature(
    boardId: number,
    organization: string,
    project: string,
    featureId: string,
    pat: string
  ): Promise<void> {
    try {
      // Step 1: Fetch feature from Azure DevOps
      console.log('Fetching feature from Azure:', { organization, project, featureId });
      const featureDto = await firstValueFrom(
        this.azureApi.getFeatureWithChildren(organization, project, featureId, pat)
      );

      // Step 2: Import the feature to the board
      console.log('Importing feature to board:', featureDto);
      const importedFeature = await firstValueFrom(
        this.featureApi.importFeature(boardId, featureDto)
      );

      // Step 3: Reload the board to ensure UI matches backend state
      console.log('Feature imported successfully, reloading board...');
      this.boardService.loadBoard(boardId);
    } catch (error: any) {
      console.error('Error importing feature:', error);
      throw new Error(error.message || MESSAGES.FEATURE.IMPORT_FAILED);
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
    pat: string
  ): Promise<void> {
    try {
      console.log('Refreshing feature from Azure:', { boardId, featureId });
      await firstValueFrom(
        this.featureApi.refreshFeature(boardId, featureId, organization, project, pat)
      );

      // Reload board to show updated data
      console.log('Feature refreshed successfully, reloading board...');
      this.boardService.loadBoard(boardId);
    } catch (error: any) {
      console.error('Error refreshing feature:', error);
      throw new Error(error.message || MESSAGES.FEATURE.REFRESH_FAILED);
    }
  }

  /**
   * Reorder features by priority
   */
  public async reorderFeatures(
    boardId: number,
    features: Array<{ featureId: number; newPriority: number }>
  ): Promise<void> {
    try {
      await firstValueFrom(this.featureApi.reorderFeatures(boardId, features));
      this.boardService.loadBoard(boardId);
    } catch (error: any) {
      console.error('Error reordering features:', error);
      throw new Error(error.message || MESSAGES.FEATURE.REORDER_FAILED);
    }
  }

  /**
   * Delete feature and its user stories
   */
  public async deleteFeature(boardId: number, featureId: number): Promise<void> {
    try {
      console.log('Deleting feature:', { boardId, featureId });
      await firstValueFrom(this.featureApi.deleteFeature(boardId, featureId));

      // Reload board to show updated data
      console.log('Feature deleted successfully, reloading board...');
      this.boardService.loadBoard(boardId);
    } catch (error: any) {
      console.error('Error deleting feature:', error);
      throw new Error(error.message || MESSAGES.FEATURE.DELETE_FAILED);
    }
  }
}
