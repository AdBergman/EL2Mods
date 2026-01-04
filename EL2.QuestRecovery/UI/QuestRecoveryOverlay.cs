using System;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using EL2.QuestRecovery.UI; // your UiState.IsQuestWindowOpen lives here

namespace EL2.QuestRecovery
{
    /// <summary>
    /// Minimal, friendly IMGUI overlay for the Quest Recovery Mod.
    ///
    /// Goals:
    /// - Only appears while the QuestWindow is open (UiState flag set by your visibility patch)
    /// - One obvious action button
    /// - Two-step confirmation to avoid misclicks
    /// - No auto-repeat (one-shot lock until the window closes, or you reset it)
    ///
    /// Wiring:
    /// - Set CanSkip / GetTargetLabel / SkipAction from your existing quest detection code.
    /// </summary>
    public sealed class QuestRecoveryOverlay : MonoBehaviour
    {
        // --- External wiring (set these from your plugin / snapshot patch) ---
        public Func<bool> CanSkip;                 // return true only when you have a valid, player-owned target
        public Func<string> GetTargetLabel;        // e.g. "Main Quest: Chapter 2 – Step 3"
        public Action SkipAction;                  // call your already-working recovery action (CompleteQuestStep once)

        // --- Internal state ---
        private ManualLogSource _log;

        private bool _panelExpanded = true;
        private bool _armedConfirm = false;
        private float _armedUntilRealtime = 0f;

        // One-shot safety: don’t allow repeated trigger while the quest window remains open
        private bool _skipUsedThisWindowOpen = false;

        // Styles cached to avoid per-frame allocations
        private bool _stylesReady = false;
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _textStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _dangerButtonStyle;
        private GUIStyle _buttonStyle;

        // Layout constants
        private const float PanelWidth = 360f;
        private const float PanelPadding = 12f;

        public void InitLogger(ManualLogSource logSource)
        {
            _log = logSource;
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            _textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true
            };

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = 34
            };

            // “Danger-ish” button without going wild: same button, bold text.
            _dangerButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = 36,
                fontStyle = FontStyle.Bold
            };

            _stylesReady = true;
        }

        private Rect ComputePanelRect()
        {
            // Top-right anchor, then fine-tuned offsets:
            float x = Screen.width - PanelWidth - 18f - 545f;
            float y = 18f + 60f;
            float height = _panelExpanded ? 220f : 54f;

            return new Rect(x, y, PanelWidth, height);
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
                if (GetTargetLabel == null) return "No target";
                string label = GetTargetLabel();
                return string.IsNullOrWhiteSpace(label) ? "No target" : label;
            }
            catch (Exception e)
            {
                _log?.LogWarning($"[QuestRecoveryOverlay] GetTargetLabel threw: {e.Message}");
                return "No target";
            }
        }

        private void SafeInvokeSkip()
        {
            try
            {
                SkipAction?.Invoke();
            }
            catch (Exception e)
            {
                _log?.LogError($"[QuestRecoveryOverlay] SkipAction threw: {e}");
            }
        }

        private void ResetArming()
        {
            _armedConfirm = false;
            _armedUntilRealtime = 0f;
        }

        private void OnGUI()
        {
            // Only show when the actual Quest window is open.
            if (!UiState.IsQuestWindowOpen)
            {
                // Reset one-shot state when leaving quest window.
                _skipUsedThisWindowOpen = false;
                ResetArming();
                return;
            }

            EnsureStyles();

            Rect panelRect = ComputePanelRect();
            GUILayout.BeginArea(panelRect, _panelStyle);

            // Header row
            GUILayout.BeginHorizontal();
            GUILayout.Label("Quest Recovery", _titleStyle);

            GUILayout.FlexibleSpace();

            string expandLabel = _panelExpanded ? "Hide" : "Show";
            if (GUILayout.Button(expandLabel, GUILayout.Width(60), GUILayout.Height(22)))
            {
                _panelExpanded = !_panelExpanded;
                ResetArming();
            }
            GUILayout.EndHorizontal();

            if (!_panelExpanded)
            {
                GUILayout.Label("Opens only in the quest window.", _hintStyle);
                GUILayout.EndArea();
                return;
            }

            GUILayout.Space(6);

            // Current target info
            bool canSkip = SafeCanSkip();
            string target = SafeTargetLabel();

            GUILayout.Label("Target", _textStyle);
            GUILayout.Label(target, _hintStyle);

            GUILayout.Space(8);

            // Safety hints
            GUILayout.Label(
                "This tool is single-player recovery only.\n" +
                "It performs a one-shot step completion when you confirm.",
                _hintStyle
            );

            GUILayout.Space(10);

            // If already used during this window open, lock it.
            if (_skipUsedThisWindowOpen)
            {
                GUILayout.Label("✅ Used once for this quest window open.", _textStyle);
                GUILayout.Label("Close/reopen the quest window to re-arm.", _hintStyle);
                GUILayout.EndArea();
                return;
            }

            // Confirmation logic: click once to arm, click again within a short time to execute.
            float now = Time.realtimeSinceStartup;
            if (_armedConfirm && now > _armedUntilRealtime)
            {
                // Auto-disarm if time window elapsed.
                ResetArming();
            }

            // Disable the button if we have no valid target.
            GUI.enabled = canSkip;

            if (!_armedConfirm)
            {
                if (GUILayout.Button("Skip current quest step…", _dangerButtonStyle))
                {
                    _armedConfirm = true;
                    _armedUntilRealtime = now + 3.0f; // 3 seconds to confirm
                    _log?.LogInfo("[QuestRecoveryOverlay] Armed skip confirmation (3s).");
                }

                GUI.enabled = true;

                if (!canSkip)
                {
                    GUILayout.Space(4);
                    GUILayout.Label("Waiting for a valid player quest target…", _hintStyle);
                }
                else
                {
                    GUILayout.Space(4);
                    GUILayout.Label("Click once to arm, then click again to confirm.", _hintStyle);
                }

                GUILayout.EndArea();
                return;
            }

            // Armed state
            GUILayout.Label("⚠ Confirm skip now", _textStyle);
            GUILayout.Label("Click confirm within a few seconds.", _hintStyle);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Cancel", _buttonStyle))
            {
                ResetArming();
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                return;
            }

            if (GUILayout.Button("CONFIRM: Skip Step", _dangerButtonStyle))
            {
                // One-shot guard: lock immediately
                _skipUsedThisWindowOpen = true;
                ResetArming();

                GUI.enabled = true;

                _log?.LogWarning("[QuestRecoveryOverlay] User confirmed skip step.");
                SafeInvokeSkip();
            }

            GUILayout.EndHorizontal();

            GUI.enabled = true;

            GUILayout.Space(6);
            GUILayout.Label("Tip: If nothing happens, the quest might be choice-gated or not step-completable.", _hintStyle);

            GUILayout.EndArea();
        }
    }
}
