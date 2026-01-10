using System;
using BepInEx.Logging;
using EL2.QuestRecovery.Safety;
using UnityEngine;

namespace EL2.QuestRecovery.UI
{
    public sealed class QuestRecoveryOverlay : MonoBehaviour
    {
        public Func<bool> CanComplete;
        public Func<string> GetTargetLabel;
        public Action CompleteAction;

        // Debug text provider (wire from TargetState)
        public Func<string> GetGoalDebugText;

        private ManualLogSource _log;

        private bool _panelExpanded = false;   // Show/Hide toggle
        private bool _detailsEnabled = false;  // Show Details toggle

        // Fallback gating (only used if signature is missing)
        private bool _completeUsedThisWindowOpen = false;

        // Track target changes to auto re-arm when the quest advances / refreshes
        private string _lastSeenSignature = null;

        // Transient feedback (e.g. "Copied.")
        private string _transientFeedback = null;
        private float _transientUntilRealtime = 0f;

        // Details scroll (only used when needed; renderer will avoid scroll view otherwise)
        private Vector2 _detailsScroll = Vector2.zero;

        // Drag/persistence isolated in helper
        private DraggablePanel _dragger;

        // Renderer
        private QuestRecoveryOverlayRenderer _renderer;

        // Panel sizing
        private const float PanelWidth = 360f;

        // First launch default position (used only when config is unset / negative)
        private static readonly Vector2 FirstLaunchDefaultPos = new Vector2(600f, 300f);

        public void InitLogger(ManualLogSource logSource) => _log = logSource;

        private void OnDestroy()
        {
            try
            {
                _renderer?.Dispose();
                _renderer = null;
            }
            catch { /* ignore */ }
        }

        private void EnsureRenderer()
        {
            if (_renderer != null) return;
            _renderer = new QuestRecoveryOverlayRenderer();
        }

        private void EnsureDragger()
        {
            if (_dragger != null) return;

            _dragger = new DraggablePanel(
                () => QuestRecoveryPlugin.OverlayX != null ? QuestRecoveryPlugin.OverlayX.Value : -1f,
                () => QuestRecoveryPlugin.OverlayY != null ? QuestRecoveryPlugin.OverlayY.Value : -1f,
                x => { if (QuestRecoveryPlugin.OverlayX != null) QuestRecoveryPlugin.OverlayX.Value = x; },
                y => { if (QuestRecoveryPlugin.OverlayY != null) QuestRecoveryPlugin.OverlayY.Value = y; },
                thresholdPx: 4f
            );
        }

        private void EnsureFirstLaunchDefaults()
        {
            try
            {
                if (QuestRecoveryPlugin.OverlayX == null || QuestRecoveryPlugin.OverlayY == null) return;

                // If unset (your convention is -1), set a safe on-screen default.
                if (QuestRecoveryPlugin.OverlayX.Value < 0f || QuestRecoveryPlugin.OverlayY.Value < 0f)
                {
                    QuestRecoveryPlugin.OverlayX.Value = FirstLaunchDefaultPos.x;
                    QuestRecoveryPlugin.OverlayY.Value = FirstLaunchDefaultPos.y;
                }
            }
            catch { /* ignore */ }
        }

        private Vector2 GetDefaultPanelPos()
        {
            // Used only when no saved position exists.
            return FirstLaunchDefaultPos;
        }

        private bool SafeCanComplete()
        {
            try { return CanComplete != null && CanComplete(); }
            catch (Exception e)
            {
                _log?.LogWarning($"[QuestRecoveryOverlay] CanComplete threw: {e.Message}");
                return false;
            }
        }

        private string SafeTargetLabel()
        {
            try
            {
                if (GetTargetLabel == null) return "";
                return GetTargetLabel() ?? "";
            }
            catch (Exception e)
            {
                _log?.LogWarning($"[QuestRecoveryOverlay] GetTargetLabel threw: {e.Message}");
                return "";
            }
        }

        private string SafeGoalDebugText()
        {
            try
            {
                if (GetGoalDebugText == null) return "";
                return GetGoalDebugText() ?? "";
            }
            catch (Exception e)
            {
                _log?.LogWarning($"[QuestRecoveryOverlay] GetGoalDebugText threw: {e.Message}");
                return "";
            }
        }

        private void SafeInvokeComplete()
        {
            try { CompleteAction?.Invoke(); }
            catch (Exception e)
            {
                _log?.LogError($"[QuestRecoveryOverlay] CompleteAction threw: {e}");
            }
        }

        private void SetTransientFeedback(string message, float seconds = 1.6f)
        {
            _transientFeedback = message;
            _transientUntilRealtime = Time.realtimeSinceStartup + seconds;
        }

        private void CopyToClipboard(string text)
        {
            try
            {
                GUIUtility.systemCopyBuffer = text ?? "";
                SetTransientFeedback("Copied.");
            }
            catch (Exception e)
            {
                _log?.LogWarning($"[QuestRecoveryOverlay] Copy failed: {e.Message}");
                SetTransientFeedback("Copy failed.");
            }
        }

        private void OnGUI()
        {
            if (!UiState.IsQuestWindowOpen)
            {
                _completeUsedThisWindowOpen = false;
                _lastSeenSignature = null;

                _transientFeedback = null;
                _transientUntilRealtime = 0f;

                _dragger?.CancelDrag();
                _detailsScroll = Vector2.zero;

                return;
            }

            EnsureRenderer();
            EnsureDragger();
            EnsureFirstLaunchDefaults();

            // fail-closed: only allow when explicitly confirmed single player
            bool spAllowed = SafetyState.IsAllowed();

            // Expire transient feedback (ONLY the toast; guidance is handled in renderer and does not fade)
            if (!string.IsNullOrEmpty(_transientFeedback) && Time.realtimeSinceStartup > _transientUntilRealtime)
                _transientFeedback = null;

            // Auto re-arm if the quest target changed
            string currentSig = QuestRecoveryTargetState.CurrentSignature;
            if (!string.IsNullOrWhiteSpace(currentSig) && currentSig != _lastSeenSignature)
            {
                _lastSeenSignature = currentSig;
                _completeUsedThisWindowOpen = false;
                _detailsScroll = Vector2.zero;
            }

            // Gather current UI inputs
            string rawTarget = SafeTargetLabel();
            string detailsText = SafeGoalDebugText();
            bool rawCanComplete = SafeCanComplete();

            bool signatureLockActive = QuestRecoveryTargetState.IsLocked();
            bool fallbackLockActive = _completeUsedThisWindowOpen && string.IsNullOrWhiteSpace(currentSig);
            bool locked = signatureLockActive || fallbackLockActive;

            float panelHeight = _renderer.ComputePanelHeight(_panelExpanded, _detailsEnabled, detailsText);

            _dragger.EnsureInitialized(GetDefaultPanelPos(), PanelWidth, panelHeight);
            Rect panelRect = _dragger.GetRect(PanelWidth, panelHeight);

            // Drag handle: avoid buttons (same behavior as before)
            Rect dragHandleRect = new Rect(panelRect.x, panelRect.y, 170f, 26f);
            _dragger.HandleDrag(dragHandleRect, PanelWidth, panelHeight);

            // Render
            QuestRecoveryOverlayRenderer.RenderResult rr = _renderer.Draw(
                panelRect: panelRect,
                panelWidth: PanelWidth,
                panelExpanded: _panelExpanded,
                detailsEnabled: _detailsEnabled,
                detailsScroll: _detailsScroll,
                spAllowed: spAllowed,
                canComplete: rawCanComplete,
                locked: locked,
                currentSig: currentSig,
                rawTargetLabel: rawTarget,
                detailsText: detailsText,
                transientFeedback: _transientFeedback
            );

            // Apply UI actions
            if (rr.ToggleExpandedClicked)
            {
                _panelExpanded = !_panelExpanded;
                if (!_panelExpanded)
                    _detailsScroll = Vector2.zero;
            }

            if (rr.ToggleDetailsChanged)
            {
                _detailsEnabled = rr.NewDetailsEnabled;
                _detailsScroll = Vector2.zero;
                // Keep transient toast as-is; "Copied." fading is fine.
            }

            if (rr.CopyClicked)
            {
                CopyToClipboard(detailsText ?? "");
            }

            if (rr.CompleteClicked)
            {
                _completeUsedThisWindowOpen = true;
                QuestRecoveryTargetState.MarkApplied();

                _log?.LogWarning("[QuestRecoveryOverlay] User clicked Complete Quest.");
                SafeInvokeComplete();
            }

            _detailsScroll = rr.NewDetailsScroll;
        }
    }
}