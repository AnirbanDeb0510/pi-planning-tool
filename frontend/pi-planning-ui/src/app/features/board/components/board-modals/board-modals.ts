import { Component, Input, signal, Signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Board } from '../board';
import { BoardResponseDto, FeatureResponseDto } from '../../../../shared/models/board.dto';

@Component({
  selector: 'app-board-modals',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './board-modals.html',
  styleUrls: ['./board-modals.css'],
})
export class BoardModals {
  @Input() board!: Signal<BoardResponseDto | null>;
  @Input() parent!: Board;

  // Feature modal states (moved from board.ts)
  protected showImportFeatureModal = signal(false);
  protected importFeatureId = signal('');
  protected importPat = signal('');
  protected rememberPatForImport = signal(false);
  protected importLoading = signal(false);
  protected importError = signal<string | null>(null);

  protected showRefreshFeatureModal = signal(false);
  protected selectedFeature = signal<FeatureResponseDto | null>(null);
  protected refreshPat = signal('');
  protected rememberPatForRefresh = signal(false);
  protected refreshLoading = signal(false);
  protected refreshError = signal<string | null>(null);

  protected showDeleteFeatureModal = signal(false);
  protected featureToDelete = signal<FeatureResponseDto | null>(null);
  protected deleteLoading = signal(false);
  protected deleteError = signal<string | null>(null);

  protected operationBlockedError = signal<string | null>(null);

  // Import Feature methods
  public openImportFeatureModal(): void {
    this.showImportFeatureModal.set(true);
    this.importFeatureId.set('');
    this.importPat.set('');
    this.importError.set(null);
  }

  protected closeImportFeatureModal(): void {
    this.showImportFeatureModal.set(false);
    this.importLoading.set(false);
    this.importError.set(null);
  }

  protected async importFeatureFromAzure(): Promise<void> {
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
      await this.parent.boardService.importFeature(
        currentBoard.id,
        currentBoard.organization,
        currentBoard.project,
        featureId,
        pat
      );
      if (this.rememberPatForImport()) {
        this.parent.boardService.storePat(pat);
      } else {
        this.parent.boardService.clearPat();
      }
      this.closeImportFeatureModal();
    } catch (error: any) {
      this.importError.set(error.message || 'Failed to import feature');
    } finally {
      this.importLoading.set(false);
    }
  }

  // Refresh Feature methods
  public openRefreshFeatureModal(feature: FeatureResponseDto): void {
    this.selectedFeature.set(feature);
    const storedPat = this.parent.boardService.getStoredPat();
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

  protected closeRefreshFeatureModal(): void {
    this.showRefreshFeatureModal.set(false);
    this.selectedFeature.set(null);
    this.refreshLoading.set(false);
    this.refreshError.set(null);
  }

  protected async refreshFeatureFromAzure(): Promise<void> {
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
      await this.parent.boardService.refreshFeature(
        currentBoard.id,
        feature.id,
        currentBoard.organization,
        currentBoard.project,
        pat
      );
      
      if (this.rememberPatForRefresh()) {
        this.parent.boardService.storePat(pat);
      } else {
        this.parent.boardService.clearPat();
      }
      
      this.closeRefreshFeatureModal();
    } catch (error: any) {
      this.refreshError.set(error.message || 'Failed to refresh feature');
    } finally {
      this.refreshLoading.set(false);
    }
  }

  // Delete Feature methods
  public openDeleteFeatureModal(feature: FeatureResponseDto): void {
    this.featureToDelete.set(feature);
    this.deleteError.set(null);
    this.showDeleteFeatureModal.set(true);
  }

  protected closeDeleteFeatureModal(): void {
    this.showDeleteFeatureModal.set(false);
    this.featureToDelete.set(null);
    this.deleteLoading.set(false);
    this.deleteError.set(null);
  }

  protected async deleteFeature(): Promise<void> {
    const feature = this.featureToDelete();
    const currentBoard = this.board();
    
    if (!feature || !currentBoard) return;

    this.deleteLoading.set(true);
    this.deleteError.set(null);
    
    try {
      await this.parent.boardService.deleteFeature(currentBoard.id, feature.id);
      this.closeDeleteFeatureModal();
    } catch (error: any) {
      this.deleteError.set(error.message || 'Failed to delete feature');
    } finally {
      this.deleteLoading.set(false);
    }
  }

  // Finalization methods (delegates to parent)
  protected openFinalizeConfirmation() {
    return this.parent.openFinalizeConfirmation();
  }

  protected closeFinalizeConfirmation() {
    return this.parent.closeFinalizeConfirmation();
  }

  protected async finalizeBoard() {
    return this.parent.finalizeBoard();
  }

  protected showFinalizeConfirmation(): boolean {
    return this.parent.showFinalizeConfirmation();
  }

  protected finalizationWarnings(): string[] {
    return this.parent.finalizationWarnings();
  }

  protected finalizationLoading(): boolean {
    return this.parent.finalizationLoading();
  }

  protected finalizationError(): string | null {
    return this.parent.finalizationError();
  }
}
