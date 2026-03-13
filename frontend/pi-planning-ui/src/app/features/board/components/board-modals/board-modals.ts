import { Component, Input, signal, Signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Board } from '../board';
import { BoardResponseDto, FeatureResponseDto } from '../../../../shared/models/board.dto';
import { FeatureService } from '../../services/feature.service';
import { RuntimeConfig } from '../../../../core/config/runtime-config';
import { LABELS, MESSAGES, PLACEHOLDERS, VALIDATIONS } from '../../../../shared/constants';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-board-modals',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule],
  templateUrl: './board-modals.html',
  styleUrls: ['./board-modals.css'],
})
export class BoardModals {
  @Input() board!: Signal<BoardResponseDto | null>;
  @Input() parent!: Board;
  protected featureService = inject(FeatureService);

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

  protected showLockBoardModal = signal(false);
  protected lockPassword = signal('');
  protected lockPasswordConfirm = signal('');
  protected hasExistingPassword = signal(false);
  protected lockLoading = signal(false);
  protected lockError = signal<string | null>(null);

  protected showUnlockBoardModal = signal(false);
  protected unlockPassword = signal('');
  protected unlockLoading = signal(false);
  protected unlockError = signal<string | null>(null);

  protected operationBlockedError = signal<string | null>(null);
  protected patTtlMinutes = RuntimeConfig.patTtlMinutes;

  protected readonly LABELS = LABELS;
  protected readonly MESSAGES = MESSAGES;
  protected readonly PLACEHOLDERS = PLACEHOLDERS;
  protected readonly VALIDATIONS = VALIDATIONS;

  // Import Feature methods
  public openImportFeatureModal(): void {
    this.showImportFeatureModal.set(true);
    this.importFeatureId.set('');
    const storedPat = this.parent.boardService.getStoredPat();
    if (storedPat) {
      this.importPat.set(storedPat);
      this.rememberPatForImport.set(true);
    } else {
      this.importPat.set('');
      this.rememberPatForImport.set(false);
    }
    this.importError.set(null);
  }

  protected onRememberPatForImportChange(remember: boolean): void {
    if (!remember) {
      const storedPat = this.parent.boardService.getStoredPat();
      if (storedPat && this.importPat() === storedPat) {
        this.importPat.set('');
      }
      return;
    }

    const storedPat = this.parent.boardService.getStoredPat();
    if (storedPat && !this.importPat().trim()) {
      this.importPat.set(storedPat);
    }
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
      this.importError.set(VALIDATIONS.FEATURE.REQUIRED_ID_AND_PAT);
      return;
    }

    this.importLoading.set(true);
    this.importError.set(null);

    try {
      await this.featureService.importFeature(currentBoard.id, featureId, pat);
      if (this.rememberPatForImport()) {
        this.parent.boardService.storePat(pat);
      } else {
        this.parent.boardService.clearPat();
      }
      this.closeImportFeatureModal();
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : String(error);
      this.importError.set(message || MESSAGES.FEATURE.IMPORT_FAILED);
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

  protected onRememberPatForRefreshChange(remember: boolean): void {
    if (!remember) {
      const storedPat = this.parent.boardService.getStoredPat();
      if (storedPat && this.refreshPat() === storedPat) {
        this.refreshPat.set('');
      }
      return;
    }

    const storedPat = this.parent.boardService.getStoredPat();
    if (storedPat && !this.refreshPat().trim()) {
      this.refreshPat.set(storedPat);
    }
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
      this.refreshError.set(VALIDATIONS.BOARD.MISSING_INFO);
      return;
    }

    this.refreshLoading.set(true);
    this.refreshError.set(null);

    try {
      await this.featureService.refreshFeature(
        currentBoard.id,
        feature.id,
        currentBoard.organization,
        currentBoard.project,
        pat,
      );

      if (this.rememberPatForRefresh()) {
        this.parent.boardService.storePat(pat);
      } else {
        this.parent.boardService.clearPat();
      }

      this.closeRefreshFeatureModal();
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : String(error);
      this.refreshError.set(message || MESSAGES.FEATURE.REFRESH_FAILED);
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
      await this.featureService.deleteFeature(currentBoard.id, feature.id);
      this.closeDeleteFeatureModal();
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : String(error);
      this.deleteError.set(message || MESSAGES.FEATURE.DELETE_FAILED);
    } finally {
      this.deleteLoading.set(false);
    }
  }

  // Lock Board methods
  public openLockBoardModal(): void {
    const currentBoard = this.board();
    if (!currentBoard) return;

    // Check if board already has a password (Scenario B) or not (Scenario A)
    // We can't directly check from frontend, so we'll show single password field
    // Backend will handle both scenarios
    this.hasExistingPassword.set(false); // For now, always show password + confirm
    this.lockPassword.set('');
    this.lockPasswordConfirm.set('');
    this.lockError.set(null);
    this.showLockBoardModal.set(true);
  }

  protected closeLockBoardModal(): void {
    this.showLockBoardModal.set(false);
    this.lockPassword.set('');
    this.lockPasswordConfirm.set('');
    this.lockLoading.set(false);
    this.lockError.set(null);
  }

  protected async lockBoard(): Promise<void> {
    const currentBoard = this.board();
    if (!currentBoard) return;

    const password = this.lockPassword().trim();
    const passwordConfirm = this.lockPasswordConfirm().trim();

    if (!password) {
      this.lockError.set('Password is required');
      return;
    }

    // Only validate confirmation if we're setting a new password
    if (!this.hasExistingPassword() && password !== passwordConfirm) {
      this.lockError.set('Passwords do not match');
      return;
    }

    this.lockLoading.set(true);
    this.lockError.set(null);

    try {
      await this.parent.boardService.lockBoard(currentBoard.id, password);
      this.closeLockBoardModal();
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : String(error);
      this.lockError.set(message || MESSAGES.BOARD.LOCK_FAILED);
    } finally {
      this.lockLoading.set(false);
    }
  }

  // Unlock Board methods
  public openUnlockBoardModal(): void {
    this.unlockPassword.set('');
    this.unlockError.set(null);
    this.showUnlockBoardModal.set(true);
  }

  protected closeUnlockBoardModal(): void {
    this.showUnlockBoardModal.set(false);
    this.unlockPassword.set('');
    this.unlockLoading.set(false);
    this.unlockError.set(null);
  }

  protected async unlockBoard(): Promise<void> {
    const currentBoard = this.board();
    if (!currentBoard) return;

    const password = this.unlockPassword().trim();

    if (!password) {
      this.unlockError.set('Password is required');
      return;
    }

    this.unlockLoading.set(true);
    this.unlockError.set(null);

    try {
      await this.parent.boardService.unlockBoard(currentBoard.id, password);
      this.closeUnlockBoardModal();
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : String(error);
      this.unlockError.set(message || MESSAGES.BOARD.UNLOCK_FAILED);
    } finally {
      this.unlockLoading.set(false);
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
