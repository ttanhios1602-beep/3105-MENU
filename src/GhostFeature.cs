using COW.GamePlay;
using UnityEngine;

namespace ProjectEspPatch
{
    public static class GhostFeature
    {
        public const string ControlObjectName = "__esp_ghost_control";
        public const string ActiveObjectName = "__esp_ghost_active";
        public const string AnchorObjectName = "__esp_ghost_anchor";
        public const string UiStateObjectName = "__esp_ghost_ui_state";
        public const string DragObjectName = "__esp_ghost_drag";

        private const float ButtonWidth = 138f;
        private const float ButtonHeight = 42f;
        private const float DragThresholdSq = 36f;

        public static bool ControlEnabled
        {
            get { return GameObject.Find(ControlObjectName) != null; }
        }

        public static bool Active
        {
            get { return GameObject.Find(ActiveObjectName) != null; }
        }

        public static void SetControlEnabled(bool enabled)
        {
            GameObject control = GameObject.Find(ControlObjectName);

            if (enabled)
            {
                if (control == null)
                {
                    control = new GameObject(ControlObjectName);
                    Object.DontDestroyOnLoad(control);
                }
                return;
            }

            DestroyNamed(ControlObjectName, "__esp_ghost_control_off");
            DisableGhost();
            DestroyNamed(DragObjectName, "__esp_ghost_drag_off");
            DestroyNamed(UiStateObjectName, "__esp_ghost_ui_state_off");
        }

        public static void ToggleControl()
        {
            SetControlEnabled(!ControlEnabled);
        }

        public static void ToggleGhost()
        {
            if (!ControlEnabled)
            {
                return;
            }

            GameObject active = GameObject.Find(ActiveObjectName);
            if (active == null)
            {
                DestroyNamed(AnchorObjectName, "__esp_ghost_anchor_stale");

                active = new GameObject(ActiveObjectName);
                Object.DontDestroyOnLoad(active);
            }
            else
            {
                DisableGhost();
            }
        }

        public static void DisableGhost()
        {
            DestroyNamed(ActiveObjectName, "__esp_ghost_active_off");
            DestroyNamed(AnchorObjectName, "__esp_ghost_anchor_off");
        }

        public static void ResetAll()
        {
            DestroyNamed(ControlObjectName, "__esp_ghost_control_off");
            DestroyNamed(ActiveObjectName, "__esp_ghost_active_off");
            DestroyNamed(AnchorObjectName, "__esp_ghost_anchor_off");
            DestroyNamed(DragObjectName, "__esp_ghost_drag_off");
            DestroyNamed(UiStateObjectName, "__esp_ghost_ui_state_off");
        }

        public static Vector3 SyncStatePosition(Player self)
        {
            if (self == null)
            {
                return Vector3.zero;
            }

            Transform root = self.RootTransform;
            Vector3 currentPosition = root != null
                ? root.position
                : (self.transform != null ? self.transform.position : Vector3.zero);

            if (!self.IsLocalPlayer())
            {
                return currentPosition;
            }

            GameObject control = GameObject.Find(ControlObjectName);
            GameObject active = GameObject.Find(ActiveObjectName);

            if (control == null || active == null || self.CurHP <= 0)
            {
                if (self.CurHP <= 0 && active != null)
                {
                    active.name = "__esp_ghost_active_dead";
                    Object.Destroy(active);
                }

                GameObject oldAnchor = GameObject.Find(AnchorObjectName);
                if (oldAnchor != null)
                {
                    oldAnchor.name = "__esp_ghost_anchor_off";
                    Object.Destroy(oldAnchor);
                }

                return currentPosition;
            }

            GameObject anchor = GameObject.Find(AnchorObjectName);
            if (anchor == null)
            {
                anchor = new GameObject(AnchorObjectName);
                anchor.transform.position = currentPosition;
                Object.DontDestroyOnLoad(anchor);
            }

            return anchor.transform.position;
        }

        public static bool DrawFloatingButton(
            Texture2D pixel,
            Vector2 pointer,
            bool pointerDown,
            bool pointerDrag,
            bool pointerUp,
            int screenWidth,
            int screenHeight)
        {
            if (!ControlEnabled || pixel == null)
            {
                if (!ControlEnabled)
                {
                    DestroyNamed(DragObjectName, "__esp_ghost_drag_off");
                }
                return false;
            }

            bool consumed = false;
            bool active = Active;

            float buttonX = (float)screenWidth * 0.5f - ButtonWidth * 0.5f;
            float buttonY = (float)screenHeight * 0.5f - ButtonHeight * 0.5f;

            GameObject uiState = GameObject.Find(UiStateObjectName);
            if (uiState == null)
            {
                uiState = new GameObject(UiStateObjectName);
                uiState.transform.position = new Vector3(buttonX, buttonY, 0f);
                Object.DontDestroyOnLoad(uiState);
            }

            Vector3 savedPosition = uiState.transform.position;
            buttonX = Mathf.Clamp(savedPosition.x, 0f, Mathf.Max(0f, (float)screenWidth - ButtonWidth));
            buttonY = Mathf.Clamp(savedPosition.y, 0f, Mathf.Max(0f, (float)screenHeight - ButtonHeight));
            if (savedPosition.x != buttonX || savedPosition.y != buttonY)
            {
                uiState.transform.position = new Vector3(buttonX, buttonY, 0f);
            }

            Rect buttonRect = new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight);
            GameObject dragState = GameObject.Find(DragObjectName);

            if (pointerDown && buttonRect.Contains(pointer))
            {
                if (dragState != null)
                {
                    dragState.name = "__esp_ghost_drag_old";
                    Object.Destroy(dragState);
                }

                dragState = new GameObject(DragObjectName);
                dragState.transform.position = new Vector3(pointer.x - buttonRect.x, pointer.y - buttonRect.y, 0f);
                dragState.transform.localScale = new Vector3(pointer.x, pointer.y, 0f);
                consumed = true;
            }

            if (dragState != null && pointerDrag)
            {
                Vector3 press = dragState.transform.localScale;
                float moveX = pointer.x - press.x;
                float moveY = pointer.y - press.y;

                if (press.z < 0.5f && moveX * moveX + moveY * moveY >= DragThresholdSq)
                {
                    press.z = 1f;
                    dragState.transform.localScale = press;
                }

                if (press.z >= 0.5f)
                {
                    Vector3 grab = dragState.transform.position;
                    buttonX = Mathf.Clamp(pointer.x - grab.x, 0f, Mathf.Max(0f, (float)screenWidth - ButtonWidth));
                    buttonY = Mathf.Clamp(pointer.y - grab.y, 0f, Mathf.Max(0f, (float)screenHeight - ButtonHeight));
                    uiState.transform.position = new Vector3(buttonX, buttonY, 0f);
                    buttonRect = new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight);
                }

                consumed = true;
            }

            if (dragState != null && pointerUp)
            {
                bool wasDragged = dragState.transform.localScale.z >= 0.5f;
                dragState.name = "__esp_ghost_drag_done";
                Object.Destroy(dragState);

                if (!wasDragged && buttonRect.Contains(pointer))
                {
                    ToggleGhost();
                    active = Active;
                }

                consumed = true;
            }

            GUI.matrix = Matrix4x4.identity;
            GUI.color = active
                ? new Color(0.16f, 0.05f, 0.24f, 0.96f)
                : new Color(0.035f, 0.040f, 0.058f, 0.94f);
            GUI.DrawTexture(buttonRect, pixel);

            GUI.color = active
                ? new Color(0.78f, 0.26f, 1.0f, 1f)
                : new Color(0.42f, 0.50f, 0.62f, 0.95f);
            GUI.DrawTexture(new Rect(buttonRect.x, buttonRect.y, buttonRect.width, 2f), pixel);
            GUI.DrawTexture(new Rect(buttonRect.x, buttonRect.y + buttonRect.height - 2f, buttonRect.width, 2f), pixel);
            GUI.DrawTexture(new Rect(buttonRect.x, buttonRect.y, 2f, buttonRect.height), pixel);
            GUI.DrawTexture(new Rect(buttonRect.x + buttonRect.width - 2f, buttonRect.y, 2f, buttonRect.height), pixel);

            GUI.color = Color.white;
            GUI.Label(
                new Rect(buttonRect.x + 13f, buttonRect.y + 10f, buttonRect.width - 20f, 24f),
                active ? "GHOST: ON" : "GHOST: OFF");

            return consumed;
        }

        private static void DestroyNamed(string objectName, string temporaryName)
        {
            GameObject obj = GameObject.Find(objectName);
            if (obj != null)
            {
                obj.name = temporaryName;
                Object.Destroy(obj);
            }
        }
    }
}
