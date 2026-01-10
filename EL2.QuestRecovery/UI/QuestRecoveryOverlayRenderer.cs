using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EL2.QuestRecovery.UI
{
    internal sealed class QuestRecoveryOverlayRenderer
    {
        internal struct RenderResult
        {
            public bool ToggleExpandedClicked;

            public bool ToggleDetailsChanged;
            public bool NewDetailsEnabled;

            public bool CopyClicked;
            public bool CompleteClicked;

            public Vector2 NewDetailsScroll;
        }

        // Panel visuals
        private bool _stylesReady;
        private Texture2D _panelBgTex;
        private Texture2D _detailsBgTex;

        private GUIStyle _panelStyle;

        private GUIStyle _titleStyle;
        private GUIStyle _headerStatusStyle;
        private GUIStyle _smallStyle;

        private GUIStyle _buttonStyle;
        private GUIStyle _dangerButtonStyle;

        private GUIStyle _detailsBoxStyle;
        private GUIStyle _detailsTextStyle;

        private GUIStyle _toggleStyle;

        // Theme knobs
        private const float PanelBgAlpha = 0.82f;
        private const float DetailsBgAlpha = 0.75f;

        // Layout constants
        private const float HeightCollapsed = 44f;
        private const float HeightExpandedBase = 154f;

        private const float DetailsMinHeight = 200f;
        private const float DetailsMaxHeight = 520f;

        private const float DetailsOuterPad = 6f;
        private const float DetailsInnerPad = 8f;

        private const float DetailsLineHeightApprox = 18f;
        private const int DetailsMaxCharsMeasured = 12000;

        private const float BottomBreathingRoom = 2f;

        private const float HeaderRowHeight = 22f;

        // Small header button width (for Copy)
        private const float HeaderSmallButtonW = 46f;

        internal void Dispose()
        {
            try
            {
                if (_panelBgTex != null) Object.Destroy(_panelBgTex);
                if (_detailsBgTex != null) Object.Destroy(_detailsBgTex);
            }
            catch { /* ignore */ }
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;

            _panelBgTex = MakeSolidTex(new Color(0f, 0f, 0f, PanelBgAlpha));
            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            _panelStyle.normal.background = _panelBgTex;
            _panelStyle.hover.background = _panelBgTex;
            _panelStyle.active.background = _panelBgTex;
            _panelStyle.focused.background = _panelBgTex;
            _panelStyle.onNormal.background = _panelBgTex;
            _panelStyle.onHover.background = _panelBgTex;
            _panelStyle.onActive.background = _panelBgTex;
            _panelStyle.onFocused.background = _panelBgTex;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                normal = { textColor = Color.white }
            };
            _titleStyle.fixedHeight = HeaderRowHeight;

            _smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _headerStatusStyle = new GUIStyle(_smallStyle)
            {
                alignment = TextAnchor.MiddleRight,
                wordWrap = false
            };
            _headerStatusStyle.fixedHeight = HeaderRowHeight;

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

            _toggleStyle = new GUIStyle(GUI.skin.toggle);

            _detailsBgTex = MakeSolidTex(new Color(0f, 0f, 0f, DetailsBgAlpha));
            _detailsBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(
                    (int)DetailsInnerPad,
                    (int)DetailsInnerPad,
                    (int)DetailsInnerPad,
                    (int)DetailsInnerPad
                ),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _detailsBoxStyle.normal.background = _detailsBgTex;
            _detailsBoxStyle.hover.background = _detailsBgTex;
            _detailsBoxStyle.active.background = _detailsBgTex;
            _detailsBoxStyle.focused.background = _detailsBgTex;
            _detailsBoxStyle.onNormal.background = _detailsBgTex;
            _detailsBoxStyle.onHover.background = _detailsBgTex;
            _detailsBoxStyle.onActive.background = _detailsBgTex;
            _detailsBoxStyle.onFocused.background = _detailsBgTex;

            _detailsTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                richText = false,
                normal = { textColor = Color.white }
            };

            _stylesReady = true;
        }

        internal float ComputePanelHeight(bool panelExpanded, bool detailsEnabled, string detailsText)
        {
            EnsureStyles();

            if (!panelExpanded) return HeightCollapsed;

            float h = HeightExpandedBase;

            if (detailsEnabled)
            {
                h += 10f;
                h += ComputeDetailsExtraHeight(detailsText);
            }

            h += BottomBreathingRoom;
            return h;
        }

        private float ComputeDetailsExtraHeight(string detailsText)
        {
            if (string.IsNullOrWhiteSpace(detailsText))
                return DetailsOuterPad + DetailsMinHeight;

            string s = detailsText;
            if (s.Length > DetailsMaxCharsMeasured)
                s = s.Substring(0, DetailsMaxCharsMeasured);

            int lines = 1;
            for (int i = 0; i < s.Length; i++)
                if (s[i] == '\n') lines++;

            int wrapBump = Mathf.Clamp(s.Length / 260, 0, 10);
            lines += wrapBump;

            float estimated =
                DetailsOuterPad +
                DetailsInnerPad +
                (lines * DetailsLineHeightApprox) +
                DetailsInnerPad;

            return Mathf.Clamp(
                estimated,
                DetailsOuterPad + DetailsMinHeight,
                DetailsOuterPad + DetailsMaxHeight
            );
        }

        internal RenderResult Draw(
            Rect panelRect,
            float panelWidth,
            bool panelExpanded,
            bool detailsEnabled,
            Vector2 detailsScroll,
            bool spAllowed,
            bool canComplete,
            bool locked,
            string currentSig,
            string rawTargetLabel,
            string detailsText,
            string transientFeedback
        )
        {
            EnsureStyles();

            RenderResult rr = default(RenderResult);
            rr.NewDetailsEnabled = detailsEnabled;
            rr.NewDetailsScroll = detailsScroll;

            GUILayout.BeginArea(panelRect, GUIContent.none, _panelStyle);

            // Header
            GUILayout.BeginHorizontal(GUILayout.Height(HeaderRowHeight));

            GUILayout.Label("≡ Quest Recovery", _titleStyle, GUILayout.Height(HeaderRowHeight));

            if (panelExpanded && spAllowed)
            {
                GUILayout.Space(6);

                bool newDetails = GUILayout.Toggle(
                    rr.NewDetailsEnabled,
                    GUIContent.none,
                    _toggleStyle,
                    GUILayout.Width(18),
                    GUILayout.Height(18)
                );

                if (newDetails != rr.NewDetailsEnabled)
                {
                    rr.ToggleDetailsChanged = true;
                    rr.NewDetailsEnabled = newDetails;
                }

                if (rr.NewDetailsEnabled && !string.IsNullOrWhiteSpace(detailsText))
                {
                    GUILayout.Space(2);
                    if (GUILayout.Button("Copy", GUILayout.Width(HeaderSmallButtonW), GUILayout.Height(HeaderRowHeight)))
                        rr.CopyClicked = true;
                }
            }

            GUILayout.FlexibleSpace();

            string statusText =
                !spAllowed ? "SP-only" :
                locked ? "Locked" :
                !canComplete ? "Not ready" :
                "Ready";

            GUILayout.Label(statusText, _headerStatusStyle, GUILayout.Height(HeaderRowHeight));

            GUILayout.Space(4);

            if (GUILayout.Button(panelExpanded ? "Hide" : "Show", GUILayout.Width(56), GUILayout.Height(HeaderRowHeight)))
                rr.ToggleExpandedClicked = true;

            GUILayout.EndHorizontal();

            if (!spAllowed)
            {
                if (panelExpanded)
                {
                    GUILayout.Space(6);
                    GUILayout.Label("Quest Recovery is disabled in multiplayer games.", _smallStyle);
                }

                GUILayout.EndArea();
                return rr;
            }

            if (!panelExpanded)
            {
                GUILayout.EndArea();
                return rr;
            }

            GUILayout.Space(6);

            string displayBlock = BuildDisplayBlockWithoutQuestIndex(rawTargetLabel);
            GUILayout.Label(
                string.IsNullOrWhiteSpace(displayBlock)
                    ? "No recoverable quest detected."
                    : displayBlock,
                _smallStyle
            );

            GUILayout.Space(8);

            bool enabled = canComplete && !locked;
            GUI.enabled = enabled;

            string buttonText =
                locked ? "Progress quest (trigger action)" :
                !canComplete ? "Complete Quest (not ready)" :
                "Complete Quest";

            if (GUILayout.Button(buttonText, enabled ? _dangerButtonStyle : _buttonStyle, GUILayout.ExpandWidth(true)))
                rr.CompleteClicked = true;

            GUI.enabled = true;

            if (!string.IsNullOrEmpty(transientFeedback))
            {
                GUILayout.Space(2);
                GUILayout.Label(transientFeedback, _smallStyle);
            }

            if (rr.NewDetailsEnabled)
            {
                GUILayout.Space(10);

                string safe = string.IsNullOrWhiteSpace(detailsText)
                    ? "No additional debug info available yet."
                    : detailsText;

                float detailsBoxHeight = Mathf.Clamp(
                    ComputeDetailsExtraHeight(detailsText) - DetailsOuterPad,
                    DetailsMinHeight,
                    DetailsMaxHeight
                );

                GUILayout.BeginVertical(_detailsBoxStyle);

                float innerWidth = Mathf.Max(20f, panelWidth - 20f - (DetailsInnerPad * 2f));
                float contentHeight = _detailsTextStyle.CalcHeight(new GUIContent(safe), innerWidth);

                if (contentHeight > detailsBoxHeight)
                {
                    rr.NewDetailsScroll = GUILayout.BeginScrollView(
                        rr.NewDetailsScroll,
                        false,
                        true,
                        GUILayout.Height(detailsBoxHeight)
                    );
                    GUILayout.Label(safe, _detailsTextStyle);
                    GUILayout.EndScrollView();
                }
                else
                {
                    GUILayout.Label(safe, _detailsTextStyle);
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndArea();
            return rr;
        }

        private static string BuildDisplayBlockWithoutQuestIndex(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";

            string[] lines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> kept = new List<string>();

            foreach (string line in lines)
            {
                string t = line.Trim();
                if (t.Length == 0) continue;
                if (t.IndexOf("QuestIndex", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                kept.Add(t);
            }

            if (kept.Count == 0) return "";
            return string.Join("\n", kept.GetRange(0, Math.Min(3, kept.Count)));
        }

        private static Texture2D MakeSolidTex(Color c)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, c);
            tex.Apply();
            return tex;
        }
    }
}