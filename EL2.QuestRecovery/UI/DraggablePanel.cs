using System;
using UnityEngine;

namespace EL2.QuestRecovery.UI
{
    internal sealed class DraggablePanel
    {
        private readonly Func<float> _getSavedX;
        private readonly Func<float> _getSavedY;
        private readonly Action<float> _saveX;
        private readonly Action<float> _saveY;

        private bool _initialized;
        private float _x;
        private float _y;

        // Drag state
        private bool _dragCandidate;
        private bool _dragging;
        private Vector2 _dragAccum;

        private readonly float _thresholdPx;

        internal DraggablePanel(
            Func<float> getSavedX,
            Func<float> getSavedY,
            Action<float> saveX,
            Action<float> saveY,
            float thresholdPx = 4f)
        {
            _getSavedX = getSavedX;
            _getSavedY = getSavedY;
            _saveX = saveX;
            _saveY = saveY;
            _thresholdPx = thresholdPx;
        }

        internal void EnsureInitialized(Vector2 defaultPos, float panelWidth, float panelHeight)
        {
            if (_initialized) return;

            float sx = _getSavedX != null ? _getSavedX() : -1f;
            float sy = _getSavedY != null ? _getSavedY() : -1f;

            _x = sx >= 0f ? sx : defaultPos.x;
            _y = sy >= 0f ? sy : defaultPos.y;

            ClampToScreen(panelWidth, panelHeight);
            _initialized = true;
        }

        internal Rect GetRect(float panelWidth, float panelHeight)
        {
            ClampToScreen(panelWidth, panelHeight);
            return new Rect(_x, _y, panelWidth, panelHeight);
        }

        internal void ClampToScreen(float panelWidth, float panelHeight)
        {
            float maxX = Mathf.Max(0f, Screen.width - panelWidth);
            float maxY = Mathf.Max(0f, Screen.height - panelHeight);

            _x = Mathf.Clamp(_x, 0f, maxX);
            _y = Mathf.Clamp(_y, 0f, maxY);
        }

        /// <summary>
        /// Call this from inside the panel's BeginArea.
        /// handleRect is panel-local coordinates.
        /// Uses Event.delta to avoid flicker/jumps.
        /// </summary>
        internal void HandleDrag(Rect handleRect, float panelWidth, float panelHeight)
        {
            Event e = Event.current;
            if (e == null) return;

            bool inside = handleRect.Contains(e.mousePosition);

            if (e.type == EventType.MouseDown && e.button == 0 && inside)
            {
                _dragCandidate = true;
                _dragging = false;
                _dragAccum = Vector2.zero;
                // do NOT Use(), to keep clicks working
                return;
            }

            if (e.type == EventType.MouseDrag && e.button == 0 && _dragCandidate)
            {
                _dragAccum += e.delta;

                if (!_dragging)
                {
                    if (Mathf.Abs(_dragAccum.x) < _thresholdPx && Mathf.Abs(_dragAccum.y) < _thresholdPx)
                        return;

                    _dragging = true;
                }

                _x += e.delta.x;
                _y += e.delta.y;

                ClampToScreen(panelWidth, panelHeight);

                // once dragging, consume so UI doesn't click
                e.Use();
                return;
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (_dragging)
                {
                    if (_saveX != null) _saveX(_x);
                    if (_saveY != null) _saveY(_y);
                    e.Use();
                }

                _dragCandidate = false;
                _dragging = false;
                return;
            }

            // Optional: do NOT cancel candidate aggressively; it makes drag feel unreliable.
            // (leave it out on purpose)
        }

        internal void CancelDrag()
        {
            _dragCandidate = false;
            _dragging = false;
        }
    }
}