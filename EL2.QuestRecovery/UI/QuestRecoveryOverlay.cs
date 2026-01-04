using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using EL2.QuestRecovery.UI;

namespace EL2.QuestRecovery
{
    public sealed class QuestRecoveryOverlay : MonoBehaviour
    {
        public Func<bool> CanSkip;
        public Func<string> GetTargetLabel;
        public Action SkipAction;

        private ManualLogSource _log;

        private bool _panelExpanded = false; // default collapsed

        // Fallback gating (only used if signature is missing)
        private bool _skipUsedThisWindowOpen = false;

        // Track target changes to auto re-arm when the quest advances / refreshes
        private string _lastSeenSignature = null;

        private string _lastFeedback = null;
        private float _feedbackUntilRealtime = 0f;

        private bool _stylesReady = false;
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _smallStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _dangerButtonStyle;

        private const float PanelWidth = 300f;

        // Default tuned position (used only when no saved position exists)
        private const float MarginRight = 18f;
        private const float BaseOffsetLeft = 545f;
        private const float BaseOffsetDown = 60f;

        // Panel heights
        private const float HeightCollapsed = 44f;
        private const float HeightExpanded = 150f;

        // Drag/persistence isolated in helper
        private DraggablePanel _dragger;

        public void InitLogger(ManualLogSource logSource) => _log = logSource;

        private void EnsureStyles()
        {
            if (_stylesReady) return;

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            _smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = 32
            };

            _dangerButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = 32,
                fontStyle = FontStyle.Bold
            };

            _stylesReady = true;
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

        private bool SafeCanSkip()
        {
            try { return CanSkip != null && CanSkip(); }
            catch (Exception e)
            {
                _log?.LogWarning($"[QuestRecoveryOverlay] CanSkip threw: {e.Message}");
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

        private void SafeInvokeSkip()
        {
            try { SkipAction?.Invoke(); }
            catch (Exception e)
            {
                _log?.LogError($"[QuestRecoveryOverlay] SkipAction threw: {e}");
            }
        }

        private void SetFeedback(string message, float seconds = 2.0f)
        {
            _lastFeedback = message;
            _feedbackUntilRealtime = Time.realtimeSinceStartup + seconds;
        }

        private static string BuildDisplayBlockWithoutQuestIndex(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";

            string[] lines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            List<string> kept = new List<string>(lines.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) continue;

                if (line.IndexOf("QuestIndex", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                kept.Add(line);
            }

            if (kept.Count == 0) return "";

            int maxLines = Math.Min(3, kept.Count);
            return string.Join("\n", kept.GetRange(0, maxLines));
        }

        private Vector2 GetDefaultPanelPos()
        {
            float defaultX = Screen.width - PanelWidth - MarginRight - BaseOffsetLeft;
            float defaultY = 18f + BaseOffsetDown;
            return new Vector2(defaultX, defaultY);
        }

        private float CurrentPanelHeight()
        {
            return _panelExpanded ? HeightExpanded : HeightCollapsed;
        }

        private void OnGUI()
        {
            if (!UiState.IsQuestWindowOpen)
            {
                _skipUsedThisWindowOpen = false;
                _lastSeenSignature = null;

                _lastFeedback = null;
                _feedbackUntilRealtime = 0f;

                _dragger?.CancelDrag();
                return;
            }

            EnsureStyles();
            EnsureDragger();

            if (!string.IsNullOrEmpty(_lastFeedback) && Time.realtimeSinceStartup > _feedbackUntilRealtime)
                _lastFeedback = null;

            // Auto re-arm if the quest target changed
            string currentSig = QuestRecoveryTargetState.CurrentSignature;
            if (!string.IsNullOrWhiteSpace(currentSig) && currentSig != _lastSeenSignature)
            {
                _lastSeenSignature = currentSig;
                _skipUsedThisWindowOpen = false;
            }

            float panelHeight = CurrentPanelHeight();

            // Initialize + get panel rect from helper (persistent + clamped)
            _dragger.EnsureInitialized(GetDefaultPanelPos(), PanelWidth, panelHeight);
            Rect panelRect = _dragger.GetRect(PanelWidth, panelHeight);

            GUILayout.BeginArea(panelRect, _panelStyle);

            // Drag handle only on LEFT side of header, so it doesn't fight the buttons on the right.
            // Coordinates are panel-local because we're inside BeginArea.
            Rect dragHandleRect = new Rect(0f, 0f, 170f, 26f);
            _dragger.HandleDrag(dragHandleRect, PanelWidth, panelHeight);

            // Header row: title + status + Show/Hide
            GUILayout.BeginHorizontal();
            GUILayout.Label("≡ Quest Recovery", _titleStyle);
            GUILayout.FlexibleSpace();

            bool canSkip = SafeCanSkip();

            bool signatureLockActive = QuestRecoveryTargetState.IsLocked();
            bool fallbackLockActive = _skipUsedThisWindowOpen && string.IsNullOrWhiteSpace(currentSig);
            bool locked = signatureLockActive || fallbackLockActive;

            if (locked) GUILayout.Label("Locked", _smallStyle);
            else if (!canSkip) GUILayout.Label("Not ready", _smallStyle);
            else GUILayout.Label("Ready", _smallStyle);

            GUILayout.Space(8);

            string toggleLabel = _panelExpanded ? "Hide" : "Show";
            if (GUILayout.Button(toggleLabel, GUILayout.Width(56), GUILayout.Height(22)))
                _panelExpanded = !_panelExpanded;

            GUILayout.EndHorizontal();

            if (!_panelExpanded)
            {
                GUILayout.EndArea();
                return;
            }

            GUILayout.Space(6);

            // Details block
            string rawTarget = SafeTargetLabel();
            string displayBlock = BuildDisplayBlockWithoutQuestIndex(rawTarget);

            if (!string.IsNullOrWhiteSpace(displayBlock))
                GUILayout.Label(displayBlock, _smallStyle);
            else
                GUILayout.Label("No recoverable quest detected.", _smallStyle);

            GUILayout.Space(8);

            // Action button
            GUI.enabled = canSkip && !locked;

            string buttonText = locked ? "Skip Quest (locked)" : "Skip Quest";
            GUIStyle buttonStyle = (canSkip && !locked) ? _dangerButtonStyle : _buttonStyle;

            if (GUILayout.Button(buttonText, buttonStyle, GUILayout.ExpandWidth(true)))
            {
                _skipUsedThisWindowOpen = true;
                QuestRecoveryTargetState.MarkApplied();

                SetFeedback("Skip invoked.");

                _log?.LogWarning("[QuestRecoveryOverlay] User clicked Skip Quest.");
                SafeInvokeSkip();
            }

            GUI.enabled = true;

            // Minimal feedback line
            if (!string.IsNullOrEmpty(_lastFeedback))
            {
                GUILayout.Space(4);
                GUILayout.Label(_lastFeedback, _smallStyle);
            }
            else if (!QuestRecoveryTargetState.HasTarget || QuestRecoveryTargetState.QuestIndex < 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("Waiting for quest data...", _smallStyle);
            }
            else if (locked)
            {
                GUILayout.Space(4);
                GUILayout.Label("Locked: progress the quest (end turn / trigger refresh).", _smallStyle);
            }
            else if (!canSkip)
            {
                GUILayout.Space(4);
                GUILayout.Label("Quest cannot be skipped right now.", _smallStyle);
            }

            GUILayout.EndArea();
        }
    }
}
