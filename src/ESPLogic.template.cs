using System;
using System.Collections;
using System.IO;
using COW;
using COW.GamePlay;
using UnityEngine;

namespace ProjectEspPatch
{
    public static class Logic
    {
        private const int EspMaster = 1;
        private const int EspBox = 2;
        private const int EspTracer = 4;
        private const int EspHealth = 8;
        private const int EspName = 16;
        private const int EspDistance = 32;
        private const int EspFov = 64;
        private const int EspMask = 127;
        private const int EspColorEnabled = 512;
        private const int EspSkeleton = 1024;

        private const int StateInitialized = 128;
        private const int AimEnabled = 32768;
        private const int AimModeShift = 16;
        private const int AimModeMask = 196608;
        private const int NoRecoil = 262144;
        private const int HeadRateShift = 19;
        private const int HeadRateMask = 3670016;
        private const int AimSystemEnabled = 4194304;
        private const int AimFovHide = 8388608;
        private const int AimSkipDowned = 1 << 11;
        private const int AuxFastParachute = 1;
        private const int AuxSpeedRunning = 2;
        private const int AuxSpeedRunningApplied = 4;
        private const int AuxFakeDamage = 8;
        private const int AuxWideCamera = 1 << 20;
        private const int AuxWideCamFovShift = 21;
        private const int AuxFastHeal = 1 << 27;
        private const int AuxFastFire = 1 << 28;
        private const int AuxBackJump  = 1 << 29;
        private const int AuxGhostControl = 1 << 30;

        private const int R8NoFog        = 1;
        private const int R8FastCrouch   = 2;
        private const int R8FastRevive   = 4;
        private const int R8SkillCD      = 8;
        private const int R8Chams        = 16;
        private const int R8FastLoot     = 32;
        private const int R8CamHack      = 64;
        private const int R8UnlockFps    = 128;
        private const int AuxMask = 4095;
        private const int AuxFovRadiusShift = 4;
        private const int AuxSilentFovShift = 12;
        private const float AuxStateMarker = 1000000f;
        private const ulong SpeedRunningKey = 4995421289296778564UL;

        private const int DefaultStateBits = (2 << AimModeShift) | (3 << HeadRateShift);

        // ═══════════════ THEME COLORS (YADAV white + red) ═══════════════
        private static readonly Color UiBg        = new Color(0.96f, 0.96f, 0.96f, 1f);
        private static readonly Color UiText      = new Color(0.15f, 0.15f, 0.15f, 1f);
        private static readonly Color UiSubText   = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static readonly Color UiAccent    = new Color(0.898f, 0.224f, 0.208f, 1f);
        private static readonly Color UiBorder    = new Color(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color UiToggleOff = new Color(0.78f, 0.78f, 0.78f, 1f);
        private static readonly Color UiTrack     = new Color(0.90f, 0.90f, 0.90f, 1f);

        public static bool Bootstrap(Player self)
        {
            if (self == null)
            {
                return false;
            }
            bool stealthState;
            try
            {
                stealthState = self.IsInStealth();
                GameObject driver = GameObject.Find("__esp_driver");
                bool createdDriver = false;
                if (driver == null)
                {
                    Camera camera = Camera.main;
                    Transform legacyFov = camera == null
                        ? null
                        : camera.transform.Find("__esp_fov");
                    if (legacyFov != null)
                    {
                        UnityEngine.Object.Destroy(legacyFov.gameObject);
                    }
                    driver = new GameObject("__esp_driver");
                    createdDriver = true;
                }

                Transform oldLogHolder = driver.transform.Find("__esp_log_holder");
                if (oldLogHolder != null) UnityEngine.Object.Destroy(oldLogHolder.gameObject);

                Component component = driver.GetComponent(typeof(SceneEditBoxSelectTool));
                if (component == null)
                {
                    component = driver.AddComponent(typeof(SceneEditBoxSelectTool));
                }
                if (component == null)
                {
                    if (createdDriver)
                    {
                        UnityEngine.Object.Destroy(driver);
                    }
                }
                else
                {
                    if (createdDriver)
                    {
                        UnityEngine.Object.DontDestroyOnLoad(driver);
                        SceneEditBoxSelectTool menu = (SceneEditBoxSelectTool)component;
                        menu.{{SCENE_POSITION_FIELD}} = new Vector2(-999f, 0f);
                        int initialState = StateInitialized | EspMask | DefaultStateBits;
                        menu.{{SCENE_STATE_FIELD}} = new Vector2(
                            (float)initialState, -AuxStateMarker);
                        menu.{{SCENE_MENU_FIELD}} = false;
                        PlayerPrefs.SetFloat("esp_m", 0f);
                        PlayerPrefs.SetFloat("esp_tab", 0f);
                        PlayerPrefs.SetFloat("esp_fabx", -1f);
                        PlayerPrefs.SetFloat("esp_faby", -1f);
                    }
                }
            }
            catch (Exception)
            {
                stealthState = false;
            }
            return stealthState;
        }

        public static void Draw(SceneEditBoxSelectTool self)
        {
            if (self == null)
            {
                return;
            }
            Event currentEvent = Event.current;
            if (currentEvent == null)
            {
                return;
            }
            if (currentEvent.type != EventType.Repaint)
            {
                return;
            }

            GameObject driverObject = self.gameObject;
            if (driverObject == null || driverObject.name != "__esp_driver")
            {
                return;
            }

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            int state = (int)self.{{SCENE_STATE_FIELD}}.x;

            if ((state & StateInitialized) == 0)
            {
                state = StateInitialized | EspMask | DefaultStateBits;
                self.{{SCENE_STATE_FIELD}} = new Vector2(
                    (float)(state & 0xFFFFFF), -AuxStateMarker);
            }

            float now = Time.unscaledTime;
            int curFrame = Time.frameCount;
            if (curFrame % 1200 == 1)
            {
                System.GC.Collect();
                System.GC.Collect();
            }
            Player _localSelf = GameFacade.CurrentLocalPlayer();
            if (_localSelf != null && _localSelf.CurHP <= 0 && curFrame % 1200 == 1)
            {
                System.GC.Collect();
                System.GC.Collect();
            }

            int mask = state & EspMask;
            int aimMode = (state & AimModeMask) >> AimModeShift;

            Matrix4x4 savedMatrix = GUI.matrix;
            Color savedColor = GUI.color;
            try
            {
                GUI.matrix = Matrix4x4.identity;
                Texture2D pixel = Texture2D.whiteTexture;
                if (pixel != null
                    && screenWidth > 0 && screenHeight > 0)
                {
                    Camera camera = Camera.main;
                    Player localPlayer = GameFacade.CurrentLocalPlayer();
                    Transform localRoot = localPlayer == null ? null : localPlayer.RootTransform;

                    // FOV circle
                    if ((state & AimSystemEnabled) != 0 && (state & AimFovHide) == 0)
                    {
                        float fdEnc2 = self.{{SCENE_STATE_FIELD}}.y;
                        int fovPacked = fdEnc2 <= -AuxStateMarker
                            ? (int)(-fdEnc2 - AuxStateMarker) : 0;
                        float fovRadius = (float)((fovPacked >> AuxFovRadiusShift) & 0xFF) * 2f;
                        if (fovRadius > 0f)
                        {
                            Color _cfov = (state & EspColorEnabled) != 0 ? new Color((((int)PlayerPrefs.GetFloat("esp_cd",16777215f)>>12)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_cd",16777215f)>>16)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_cd",16777215f)>>20)&0xF)/15f,0.85f) : new Color(1f,1f,1f,0.85f);
                            GUI.color = _cfov;
                            float circleX = (float)screenWidth * 0.5f;
                            float circleY = (float)screenHeight * 0.5f;
                            const int fovSamples = 72;
                            const float fovStep = 0.08726646f;
                            const float fovThickness = 2f;
                            for (int sample = 0; sample < fovSamples; sample++)
                            {
                                float angleA = (float)sample * fovStep;
                                float angleB = (float)(sample + 1) * fovStep;
                                float startX = circleX + Mathf.Cos(angleA) * fovRadius;
                                float startY = circleY + Mathf.Sin(angleA) * fovRadius;
                                float endX   = circleX + Mathf.Cos(angleB) * fovRadius;
                                float endY   = circleY + Mathf.Sin(angleB) * fovRadius;
                                float segX = endX - startX, segY = endY - startY;
                                float segLen = Mathf.Sqrt(segX * segX + segY * segY);
                                float segAngle = Mathf.Atan2(segY, segX) * 57.29578f;
                                if (float.IsNaN(segLen) || float.IsInfinity(segLen) || segLen <= 0f) continue;
                                GUI.matrix = Matrix4x4.TRS(new Vector3(startX, startY, 0f), Quaternion.Euler(0f, 0f, segAngle), Vector3.one);
                                GUI.DrawTexture(new Rect(0f, -fovThickness * 0.5f, segLen + 1f, fovThickness), pixel);
                                GUI.matrix = Matrix4x4.identity;
                            }
                        }
                    }

                    if ((mask & EspMaster) != 0)
                    {
                        {{MATCH_TYPE}} match = GameFacade.CurrentMatch();
                        IList players = match == null ? null : match.{{MATCH_PLAYERS_METHOD}}();
                        GameObject localObject = localPlayer == null
                            ? null : localPlayer.gameObject;
                        if (camera != null && localPlayer != null
                            && localObject != null && localObject.activeInHierarchy
                            && localRoot != null && players != null)
                        {
                            int enemyCount = 0;
                            float espFovRadius = 0f;
                            if ((state & AimSystemEnabled) != 0)
                            {
                                float fdEncFov = self.{{SCENE_STATE_FIELD}}.y;
                                if (fdEncFov <= -AuxStateMarker)
                                {
                                    int fovPackedEsp = (int)(-fdEncFov - AuxStateMarker);
                                    espFovRadius = (float)((fovPackedEsp >> AuxFovRadiusShift) & 0xFF) * 2f;
                                }
                            }
                            bool espOn = (state & EspColorEnabled) != 0;
                            float c_lineThick  = PlayerPrefs.GetFloat("esp_lt", 1.5f);
                            float c_boxThick   = PlayerPrefs.GetFloat("esp_bt", 1.5f);
                            float c_nameScale  = 1.9f;
                            Color _cl  = espOn ? new Color(((int)PlayerPrefs.GetFloat("esp_ca",1175839f)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_ca",1175839f)>>4)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_ca",1175839f)>>8)&0xF)/15f,1f) : Color.white;
                            Color _cb2 = espOn ? new Color((((int)PlayerPrefs.GetFloat("esp_ca",1175839f)>>12)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_ca",1175839f)>>16)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_ca",1175839f)>>20)&0xF)/15f,1f) : Color.white;
                            Color _ch  = espOn ? new Color(((int)PlayerPrefs.GetFloat("esp_cb",2093553f)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_cb",2093553f)>>4)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_cb",2093553f)>>8)&0xF)/15f,1f) : new Color(0.1f,0.95f,0.1f,1f);
                            Color _cn  = espOn ? new Color((((int)PlayerPrefs.GetFloat("esp_cb",2093553f)>>12)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_cb",2093553f)>>16)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_cb",2093553f)>>20)&0xF)/15f,1f) : new Color(1f,1f,0.35f,1f);
                            Color _cd  = espOn ? new Color(((int)PlayerPrefs.GetFloat("esp_cc",1179647f)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_cc",1179647f)>>4)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_cc",1179647f)>>8)&0xF)/15f,1f) : Color.white;
                            Color _cc2 = espOn ? new Color((((int)PlayerPrefs.GetFloat("esp_cc",1179647f)>>12)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_cc",1179647f)>>16)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_cc",1179647f)>>20)&0xF)/15f,1f) : new Color(1f,0.08f,0.08f,1f);
                            Color _csk = espOn ? new Color(((int)PlayerPrefs.GetFloat("esp_cd",16777215f)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_cd",16777215f)>>4)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_cd",16777215f)>>8)&0xF)/15f,0.9f) : Color.white;
                            float c_skelThick = PlayerPrefs.GetFloat("esp_sk", 1.5f);
                            float _sh = c_skelThick * 0.5f;
                            int playerCount = players.Count;
                            if (playerCount > 64) playerCount = 64;
                            for (int index = 0; index < playerCount; index++)
                            {
                                try
                                {
                                Player player = players[index] as Player;
                                if (player == null) continue;
                                GameObject playerObject = player.gameObject;
                                if (playerObject == null || !playerObject.activeInHierarchy) continue;
                                Transform root = player.RootTransform;
                                if (player.IsLocalPlayer()
                                    || player.IsLocalTeammate(false) || !player.IsVisible()) continue;

                                bool dying = player.IsDieing;
                                int health = player.CurHP;
                                int maximumHealth = player.MaxHP;
                                if (health <= 0 && !dying) continue;
                                Transform head = player.GetHeadTF();
                                if (root == null || head == null) continue;

                                Transform _sLegL = player.LegBoneLeft; Transform _sLegR = player.LegBoneRight;
                                Vector3 _hipWorld; if (_sLegL != null && _sLegR != null) { Vector3 _lp = _sLegL.position, _rp = _sLegR.position; _hipWorld = new Vector3((_lp.x + _rp.x) * 0.5f, (_lp.y + _rp.y) * 0.5f, (_lp.z + _rp.z) * 0.5f); } else if (_sLegL != null) { _hipWorld = _sLegL.position; } else if (_sLegR != null) { _hipWorld = _sLegR.position; } else { _hipWorld = root.position; }
                                float distance = Vector3.Distance(localRoot.position, _hipWorld);
                                if (distance > 150f) continue;
                                Vector3 feetScreen = camera.WorldToScreenPoint(new Vector3(_hipWorld.x, _hipWorld.y - 0.9f, _hipWorld.z));
                                Vector3 headScreen = camera.WorldToScreenPoint(new Vector3(_hipWorld.x, _hipWorld.y + 0.9f, _hipWorld.z));
                                if (feetScreen.z <= 0f || headScreen.z <= 0f) continue;
                                if (float.IsNaN(feetScreen.x) || float.IsNaN(feetScreen.y)
                                    || float.IsNaN(feetScreen.z) || float.IsNaN(headScreen.x)
                                    || float.IsNaN(headScreen.y) || float.IsNaN(headScreen.z)
                                    || float.IsInfinity(feetScreen.x) || float.IsInfinity(feetScreen.y)
                                    || float.IsInfinity(headScreen.x) || float.IsInfinity(headScreen.y)) continue;

                                float height = Mathf.Abs(headScreen.y - feetScreen.y) * 1.0909091f;
                                if (height < 2f) continue;
                                float width = height * 0.6f;
                                float left = headScreen.x - width * 0.5f;
                                float top = (float)screenHeight - headScreen.y;
                                float thickness = c_boxThick;
                                float lineThick = c_lineThick;
                                if (float.IsNaN(left) || float.IsNaN(top)
                                    || float.IsInfinity(left) || float.IsInfinity(top)
                                    || left > (float)screenWidth || left + width < 0f
                                    || top > (float)screenHeight || top + height < 0f) continue;
                                bool inFov = false;
                                if (espFovRadius > 0f)
                                {
                                    float dxF = left + width * 0.5f - (float)screenWidth * 0.5f;
                                    float dyF = top + height * 0.5f - (float)screenHeight * 0.5f;
                                    inFov = (dxF * dxF + dyF * dyF) <= (espFovRadius * espFovRadius);
                                }
                                Color lineEspColor = (inFov || dying) ? Color.red : _cl;
                                Color boxEspColor = (inFov || dying) ? Color.red : _cb2;

                                if ((mask & EspBox) != 0)
                                {
                                    GUI.color = boxEspColor;
                                    GUI.DrawTexture(new Rect(left, top, width, thickness), pixel);
                                    GUI.DrawTexture(new Rect(left, top + height - thickness, width, thickness), pixel);
                                    GUI.DrawTexture(new Rect(left, top, thickness, height), pixel);
                                    GUI.DrawTexture(new Rect(left + width - thickness, top, thickness, height), pixel);
                                }

                                if ((mask & EspTracer) != 0)
                                {
                                    float startX = (float)screenWidth * 0.5f;
                                    float startY = 0f;
                                    float endX = left + width * 0.5f;
                                    float endY = top;
                                    float tracerX = endX - startX;
                                    float tracerY = endY - startY;
                                    float tracerLength = Mathf.Sqrt(tracerX * tracerX + tracerY * tracerY);
                                    float tracerAngle = Mathf.Atan2(tracerY, tracerX) * 57.29578f;
                                    GUI.matrix = Matrix4x4.TRS(new Vector3(startX, startY, 0f), Quaternion.Euler(0f, 0f, tracerAngle), Vector3.one);
                                    GUI.color = lineEspColor;
                                    GUI.DrawTexture(new Rect(0f, -lineThick * 0.5f, tracerLength, lineThick), pixel);
                                    GUI.matrix = Matrix4x4.identity;
                                }

                                if ((mask & EspHealth) != 0)
                                {
                                    float healthRatio = maximumHealth > 0
                                        ? Mathf.Clamp01((float)health / (float)maximumHealth) : 0f;
                                    float healthX = left + width + 2f;
                                    GUI.color = new Color(0f, 0f, 0f, 0.9f);
                                    GUI.DrawTexture(new Rect(healthX, top, c_boxThick + 2f, height), pixel);
                                    GUI.color = dying ? new Color(1f, 0f, 0f, 1f)
                                        : (espOn ? _ch : (healthRatio < 0.3f ? new Color(1f, 0f, 0f, 1f)
                                            : (healthRatio <= 0.6f ? new Color(1f, 1f, 0f, 1f)
                                                : new Color(0f, 1f, 0f, 1f))));
                                    GUI.DrawTexture(new Rect(healthX + 1f, top + height - height * healthRatio, c_boxThick, height * healthRatio), pixel);
                                }

                                if ((mask & EspName) != 0)
                                {
                                    string nickname = player.NickName;
                                    bool isBot = string.IsNullOrEmpty(nickname);
                                    if (isBot) nickname = "Bot";
                                    else if (nickname.Length > 10) nickname = nickname.Substring(0, 10);
                                    float badgeH = 14f * c_nameScale;
                                    float textScale = 0.65f * c_nameScale;
                                    float nameBadgeW = (isBot ? 130f : 90f) * c_nameScale;
                                    float nameBadgeX = left + width * 0.5f + 8.5f - nameBadgeW * 0.5f;
                                    float badgeY = top - badgeH - 2f;
                                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                                    GUI.color = dying ? new Color(1f, 0.3f, 0.3f, 1f) : _cn;
                                    GUI.matrix = Matrix4x4.TRS(new Vector3(nameBadgeX, badgeY, 0f), Quaternion.identity, new Vector3(textScale, textScale, 1f));
                                    GUI.Label(new Rect(0f, 0f, nameBadgeW / textScale, badgeH / textScale), nickname);
                                    GUI.matrix = Matrix4x4.identity;
                                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                }

                                if ((mask & EspDistance) != 0)
                                {
                                    int distVal = (int)Mathf.Clamp(distance, 0f, 999f);
                                    string distStr = distVal.ToString() + "m";
                                    float dW = (float)distStr.Length * 9f;
                                    float dX = left + width * 0.5f - dW * 0.5f;
                                    float dY = top + height + 4f;
                                    GUI.color = _cd;
                                    GUI.Label(new Rect(dX, dY, dW, 18f), distStr);
                                }

                                if ((state & EspSkeleton) != 0)
                                {
                                    Transform skelNeck  = player.NeckBone;
                                    Transform skelArmL  = player.ArmBoneLeft;
                                    Transform skelArmR  = player.ArmBoneRight;
                                    Transform skelHandL = player.HandBoneLeft;
                                    Transform skelHandR = player.HandBoneRight;
                                    Transform skelFootL = player.FootBoneLeft;
                                    Transform skelFootR = player.FootBoneRight;
                                    Vector3 spHead  = camera.WorldToScreenPoint(head.position);
                                    Vector3 spNeck  = skelNeck  != null ? camera.WorldToScreenPoint(skelNeck.position)  : new Vector3(0f, 0f, -1f);
                                    Vector3 spArmL  = skelArmL  != null ? camera.WorldToScreenPoint(skelArmL.position)  : new Vector3(0f, 0f, -1f);
                                    Vector3 spArmR  = skelArmR  != null ? camera.WorldToScreenPoint(skelArmR.position)  : new Vector3(0f, 0f, -1f);
                                    Vector3 spHandL = skelHandL != null ? camera.WorldToScreenPoint(skelHandL.position) : new Vector3(0f, 0f, -1f);
                                    Vector3 spHandR = skelHandR != null ? camera.WorldToScreenPoint(skelHandR.position) : new Vector3(0f, 0f, -1f);
                                    Vector3 spFootL = skelFootL != null ? camera.WorldToScreenPoint(skelFootL.position) : new Vector3(0f, 0f, -1f);
                                    Vector3 spFootR = skelFootR != null ? camera.WorldToScreenPoint(skelFootR.position) : new Vector3(0f, 0f, -1f);
                                    Vector3 spHip   = camera.WorldToScreenPoint(_hipWorld);
                                    Vector3 spElbowL = (spArmL.z > 0f && spHandL.z > 0f) ? new Vector3(spArmL.x + (spHandL.x - spArmL.x) * 0.5f, spArmL.y + (spHandL.y - spArmL.y) * 0.5f, 1f) : new Vector3(0f, 0f, -1f);
                                    Vector3 spElbowR = (spArmR.z > 0f && spHandR.z > 0f) ? new Vector3(spArmR.x + (spHandR.x - spArmR.x) * 0.5f, spArmR.y + (spHandR.y - spArmR.y) * 0.5f, 1f) : new Vector3(0f, 0f, -1f);
                                    Vector3 spKneeL  = (spHip.z  > 0f && spFootL.z > 0f) ? new Vector3(spHip.x + (spFootL.x - spHip.x) * 0.45f, spHip.y + (spFootL.y - spHip.y) * 0.45f, 1f) : new Vector3(0f, 0f, -1f);
                                    Vector3 spKneeR  = (spHip.z  > 0f && spFootR.z > 0f) ? new Vector3(spHip.x + (spFootR.x - spHip.x) * 0.45f, spHip.y + (spFootR.y - spHip.y) * 0.45f, 1f) : new Vector3(0f, 0f, -1f);
                                    GUI.color = dying ? Color.red : _csk;
                                    if (spHead.z > 0f && spNeck.z > 0f) { float _x1 = spHead.x, _y1 = (float)screenHeight - spHead.y, _x2 = spNeck.x, _y2 = (float)screenHeight - spNeck.y, _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy); if (_len >= 0.5f) { GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * 57.29578f), Vector3.one); GUI.DrawTexture(new Rect(0f, -_sh, _len, c_skelThick), pixel); GUI.matrix = Matrix4x4.identity; } }
                                    if (spNeck.z > 0f && spArmL.z > 0f) { float _x1 = spNeck.x, _y1 = (float)screenHeight - spNeck.y, _x2 = spArmL.x, _y2 = (float)screenHeight - spArmL.y, _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy); if (_len >= 0.5f) { GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * 57.29578f), Vector3.one); GUI.DrawTexture(new Rect(0f, -_sh, _len, c_skelThick), pixel); GUI.matrix = Matrix4x4.identity; } }
                                    if (spArmL.z > 0f && spElbowL.z > 0f) { float _x1 = spArmL.x, _y1 = (float)screenHeight - spArmL.y, _x2 = spElbowL.x, _y2 = (float)screenHeight - spElbowL.y, _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy); if (_len >= 0.5f) { GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * 57.29578f), Vector3.one); GUI.DrawTexture(new Rect(0f, -_sh, _len, c_skelThick), pixel); GUI.matrix = Matrix4x4.identity; } }
                                    if (spElbowL.z > 0f && spHandL.z > 0f) { float _x1 = spElbowL.x, _y1 = (float)screenHeight - spElbowL.y, _x2 = spHandL.x, _y2 = (float)screenHeight - spHandL.y, _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy); if (_len >= 0.5f) { GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * 57.29578f), Vector3.one); GUI.DrawTexture(new Rect(0f, -_sh, _len, c_skelThick), pixel); GUI.matrix = Matrix4x4.identity; } }
                                    if (spNeck.z > 0f && spArmR.z > 0f) { float _x1 = spNeck.x, _y1 = (float)screenHeight - spNeck.y, _x2 = spArmR.x, _y2 = (float)screenHeight - spArmR.y, _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy); if (_len >= 0.5f) { GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * 57.29578f), Vector3.one); GUI.DrawTexture(new Rect(0f, -_sh, _len, c_skelThick), pixel); GUI.matrix = Matrix4x4.identity; } }
                                    if (spArmR.z > 0f && spElbowR.z > 0f) { float _x1 = spArmR.x, _y1 = (float)screenHeight - spArmR.y, _x2 = spElbowR.x, _y2 = (float)screenHeight - spElbowR.y, _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy); if (_len >= 0.5f) { GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * 57.29578f), Vector3.one); GUI.DrawTexture(new Rect(0f, -_sh, _len, c_skelThick), pixel); GUI.matrix = Matrix4x4.identity; } }
                                    if (spElbowR.z > 0f && spHandR.z > 0f) { float _x1 = spElbowR.x, _y1 = (float)screenHeight - spElbowR.y, _x2 = spHandR.x, _y2 = (float)screenHeight - spHandR.y, _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy); if (_len >= 0.5f) { GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * 57.29578f), Vector3.one); GUI.DrawTexture(new Rect(0f, -_sh, _len, c_skelThick), pixel); GUI.matrix = Matrix4x4.identity; } }
                                    if (spNeck.z > 0f && spHip.z > 0f) { float _x1 = spNeck.x, _y1 = (float)screenHeight - spNeck.y, _x2 = spHip.x, _y2 = (float)screenHeight - spHip.y, _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy); if (_len >= 0.5f) { GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * 57.29578f), Vector3.one); GUI.DrawTexture(new Rect(0f, -_sh, _len, c_skelThick), pixel); GUI.matrix = Matrix4x4.identity; } }
                                    if (spHip.z > 0f && spKneeL.z > 0f) { float _x1 = spHip.x, _y1 = (float)screenHeight - spHip.y, _x2 = spKneeL.x, _y2 = (float)screenHeight - spKneeL.y, _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy); if (_len >= 0.5f) { GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * 57.29578f), Vector3.one); GUI.DrawTexture(new Rect(0f, -_sh, _len, c_skelThick), pixel); GUI.matrix = Matrix4x4.identity; } }
                                    if (spKneeL.z > 0f && spFootL.z > 0f) { float _x1 = spKneeL.x, _y1 = (float)screenHeight - spKneeL.y, _x2 = spFootL.x, _y2 = (float)screenHeight - spFootL.y, _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy); if (_len >= 0.5f) { GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * 57.29578f), Vector3.one); GUI.DrawTexture(new Rect(0f, -_sh, _len, c_skelThick), pixel); GUI.matrix = Matrix4x4.identity; } }
                                    if (spHip.z > 0f && spKneeR.z > 0f) { float _x1 = spHip.x, _y1 = (float)screenHeight - spHip.y, _x2 = spKneeR.x, _y2 = (float)screenHeight - spKneeR.y, _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy); if (_len >= 0.5f) { GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * 57.29578f), Vector3.one); GUI.DrawTexture(new Rect(0f, -_sh, _len, c_skelThick), pixel); GUI.matrix = Matrix4x4.identity; } }
                                    if (spKneeR.z > 0f && spFootR.z > 0f) { float _x1 = spKneeR.x, _y1 = (float)screenHeight - spKneeR.y, _x2 = spFootR.x, _y2 = (float)screenHeight - spFootR.y, _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy); if (_len >= 0.5f) { GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * 57.29578f), Vector3.one); GUI.DrawTexture(new Rect(0f, -_sh, _len, c_skelThick), pixel); GUI.matrix = Matrix4x4.identity; } }
                                }
                                enemyCount++;
                                }
                                catch (Exception) { }
                            }

                            if (enemyCount == 0)
                            {
                                float _lastNeg = PlayerPrefs.GetFloat("esp_neg", 0f);
                                if (now - _lastNeg > 20f)
                                {
                                    PlayerPrefs.SetFloat("esp_neg", now);
                                    System.GC.Collect();
                                    System.GC.Collect();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
            
            // ═══════════════ GHOST BUTTON ═══════════════
            try
            {
                Texture2D pixel = Texture2D.whiteTexture;
                if (GameObject.Find("__esp_ghost_control") != null)
                {
                    bool _gActive = GameObject.Find("__esp_ghost_active") != null;
                    int _gsi = (int)PlayerPrefs.GetFloat("esp_gs", 0f);
                    float _gsc = _gsi == 1 ? 0.5f : _gsi == 2 ? 0.75f : _gsi == 4 ? 1.25f : _gsi == 5 ? 1.5f : _gsi == 6 ? 1.75f : _gsi == 7 ? 2.0f : 1.0f;
                    float _gBtnW = 70f * _gsc, _gBtnH = 50f * _gsc;
                    GameObject _gUi = GameObject.Find("__esp_ghost_ui_state");
                    if (_gUi == null)
                    {
                        _gUi = new GameObject("__esp_ghost_ui_state");
                        _gUi.transform.position = new Vector3(
                            (float)screenWidth * 0.5f - _gBtnW * 0.5f,
                            (float)screenHeight * 0.5f - _gBtnH * 0.5f, 0f);
                        UnityEngine.Object.DontDestroyOnLoad(_gUi);
                    }
                    float _gBx = Mathf.Clamp(_gUi.transform.position.x, 0f, Mathf.Max(0f, (float)screenWidth - _gBtnW));
                    float _gBy = Mathf.Clamp(_gUi.transform.position.y, 0f, Mathf.Max(0f, (float)screenHeight - _gBtnH));
                    Rect _gRect = new Rect(_gBx, _gBy, _gBtnW, _gBtnH);
                    Vector2 _gPtr = Vector2.zero;
                    bool _gDown = false, _gDrag = false, _gUp = false;
                    if (Input.touchCount > 0)
                    {
                        Touch _gt = Input.GetTouch(0);
                        _gPtr = new Vector2(_gt.position.x, (float)screenHeight - _gt.position.y);
                        _gDown = _gt.phase == TouchPhase.Began;
                        _gDrag = _gt.phase == TouchPhase.Moved || _gt.phase == TouchPhase.Stationary;
                        _gUp   = _gt.phase == TouchPhase.Ended || _gt.phase == TouchPhase.Canceled;
                    }
                    GameObject _gDragObj = GameObject.Find("__esp_ghost_drag");
                    if (_gDown && _gRect.Contains(_gPtr))
                    {
                        if (_gDragObj != null) { _gDragObj.name = "__esp_ghost_drag_old"; UnityEngine.Object.Destroy(_gDragObj); }
                        _gDragObj = new GameObject("__esp_ghost_drag");
                        _gDragObj.transform.position = new Vector3(_gPtr.x - _gRect.x, _gPtr.y - _gRect.y, 0f);
                        _gDragObj.transform.localScale = new Vector3(_gPtr.x, _gPtr.y, 0f);
                        UnityEngine.Object.DontDestroyOnLoad(_gDragObj);
                    }
                    if (_gDragObj != null && _gDrag)
                    {
                        Vector3 _gPress = _gDragObj.transform.localScale;
                        float _gmx = _gPtr.x - _gPress.x, _gmy = _gPtr.y - _gPress.y;
                        if (_gPress.z < 0.5f && _gmx * _gmx + _gmy * _gmy >= 36f)
                        { _gPress.z = 1f; _gDragObj.transform.localScale = _gPress; }
                        if (_gPress.z >= 0.5f)
                        {
                            _gBx = Mathf.Clamp(_gPtr.x - _gDragObj.transform.position.x, 0f, Mathf.Max(0f, (float)screenWidth - _gBtnW));
                            _gBy = Mathf.Clamp(_gPtr.y - _gDragObj.transform.position.y, 0f, Mathf.Max(0f, (float)screenHeight - _gBtnH));
                            _gUi.transform.position = new Vector3(_gBx, _gBy, 0f);
                            _gRect = new Rect(_gBx, _gBy, _gBtnW, _gBtnH);
                        }
                    }
                    if (_gDragObj != null && _gUp)
                    {
                        bool _gWasDragged = _gDragObj.transform.localScale.z >= 0.5f;
                        _gDragObj.name = "__esp_ghost_drag_done"; UnityEngine.Object.Destroy(_gDragObj);
                        if (!_gWasDragged && _gRect.Contains(_gPtr))
                        {
                            GameObject _gAct = GameObject.Find("__esp_ghost_active");
                            if (_gAct == null)
                            {
                                GameObject _gAnch = GameObject.Find("__esp_ghost_anchor");
                                if (_gAnch != null) { _gAnch.name = "__esp_ghost_anchor_stale"; UnityEngine.Object.Destroy(_gAnch); }
                                _gAct = new GameObject("__esp_ghost_active");
                                UnityEngine.Object.DontDestroyOnLoad(_gAct);
                            }
                            else
                            {
                                _gAct.name = "__esp_ghost_active_off"; UnityEngine.Object.Destroy(_gAct);
                                GameObject _gAnch2 = GameObject.Find("__esp_ghost_anchor");
                                if (_gAnch2 != null) { _gAnch2.name = "__esp_ghost_anchor_off"; UnityEngine.Object.Destroy(_gAnch2); }
                            }
                            _gActive = GameObject.Find("__esp_ghost_active") != null;
                        }
                    }
                    GUI.matrix = Matrix4x4.identity;
                    GUI.color = Color.white;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(new Rect(_gRect.x, _gRect.y, _gRect.width, 18f * _gsc), "GHOST");
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    float _tkX = _gRect.x + 5f * _gsc, _tkY = _gRect.y + 22f * _gsc;
                    float _tkW = 60f * _gsc, _tkH = 26f * _gsc;
                    float _tkR = 4f * _gsc;
                    GUI.color = _gActive ? new Color(0.88f, 0.10f, 0.10f, 0.96f) : new Color(0.24f, 0.24f, 0.28f, 0.92f);
                    GUI.DrawTexture(new Rect(_tkX + _tkR, _tkY, _tkW - _tkR * 2f, _tkH), pixel);
                    GUI.DrawTexture(new Rect(_tkX, _tkY + _tkR, _tkW, _tkH - _tkR * 2f), pixel);
                    GUI.DrawTexture(new Rect(_tkX + _tkR * 0.5f, _tkY + _tkR * 0.5f, _tkW - _tkR, _tkH - _tkR), pixel);
                    float _thD = 20f * _gsc, _thR = 3f * _gsc;
                    float _thX = _gActive ? (_tkX + _tkW - _thR - _thD) : (_tkX + _thR);
                    float _thY = _tkY + _thR;
                    GUI.color = new Color(0f, 0f, 0f, 0.18f);
                    GUI.DrawTexture(new Rect(_thX - _gsc, _thY + _gsc * 2f, _thD + _gsc * 2f, _thD), pixel);
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect(_thX + _thR, _thY, _thD - _thR * 2f, _thD), pixel);
                    GUI.DrawTexture(new Rect(_thX, _thY + _thR, _thD, _thD - _thR * 2f), pixel);
                    GUI.DrawTexture(new Rect(_thX + _gsc, _thY + _gsc, _thD - _gsc * 2f, _thD - _gsc * 2f), pixel);
                }
            }
            catch (Exception) { }

            // ═══════════════ FAB "VN" + MENU YADAV ═══════════════
            try
            {
                GUI.matrix = Matrix4x4.identity;
                Texture2D px = Texture2D.whiteTexture;
                if (px != null)
                {
                    float fabR = 28f;
                    float fabx = PlayerPrefs.GetFloat("esp_fabx", -1f);
                    float faby = PlayerPrefs.GetFloat("esp_faby", -1f);
                    if (fabx < 0f) { fabx = (float)screenWidth - 60f; PlayerPrefs.SetFloat("esp_fabx", fabx); }
                    if (faby < 0f) { faby = (float)screenHeight * 0.5f - fabR; PlayerPrefs.SetFloat("esp_faby", faby); }
                    if (fabx < 0f) fabx = 0f;
                    if (faby < 0f) faby = 0f;
                    if (fabx > screenWidth - fabR * 2f) fabx = screenWidth - fabR * 2f;
                    if (faby > screenHeight - fabR * 2f) faby = screenHeight - fabR * 2f;
                    Rect fabRect = new Rect(fabx, faby, fabR * 2f, fabR * 2f);

                    Vector2 tp = Vector2.zero;
                    bool tDown = false, tDrag = false, tUp = false;
                    if (Input.touchCount > 0)
                    {
                        Touch t = Input.GetTouch(0);
                        tp = new Vector2(t.position.x, (float)screenHeight - t.position.y);
                        tDown = t.phase == TouchPhase.Began;
                        tDrag = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
                        tUp = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                    }

                    GameObject drag = GameObject.Find("__esp_fab_drag");
                    if (tDown && fabRect.Contains(tp))
                    {
                        if (drag != null) { drag.name = "__esp_fab_drag_old"; UnityEngine.Object.Destroy(drag); }
                        drag = new GameObject("__esp_fab_drag");
                        drag.transform.position = new Vector3(tp.x - fabRect.x, tp.y - fabRect.y, 0f);
                        drag.transform.localScale = new Vector3(tp.x, tp.y, 0f);
                        UnityEngine.Object.DontDestroyOnLoad(drag);
                    }
                    if (drag != null && tDrag)
                    {
                        Vector3 dv = drag.transform.localScale;
                        float fdx = tp.x - dv.x;
                        float fdy = tp.y - dv.y;
                        if (dv.z < 0.5f && fdx * fdx + fdy * fdy >= 36f) { dv.z = 1f; drag.transform.localScale = dv; }
                        if (dv.z >= 0.5f)
                        {
                            fabx = Mathf.Clamp(tp.x - drag.transform.position.x, 0f, screenWidth - fabR * 2f);
                            faby = Mathf.Clamp(tp.y - drag.transform.position.y, 0f, screenHeight - fabR * 2f);
                            PlayerPrefs.SetFloat("esp_fabx", fabx);
                            PlayerPrefs.SetFloat("esp_faby", faby);
                            fabRect = new Rect(fabx, faby, fabR * 2f, fabR * 2f);
                        }
                    }
                    if (drag != null && tUp)
                    {
                        bool wasDragged = drag.transform.localScale.z >= 0.5f;
                        drag.name = "__esp_fab_drag_done";
                        UnityEngine.Object.Destroy(drag);
                        if (!wasDragged && fabRect.Contains(tp))
                        {
                            float curOpen = PlayerPrefs.GetFloat("esp_m", 0f);
                            PlayerPrefs.SetFloat("esp_m", curOpen >= 0.5f ? 0f : 1f);
                        }
                    }

                    GUI.color = UiAccent;
                    float fabCx = fabRect.x + fabR;
                    float fabCy = fabRect.y + fabR;
                    for (int fi = 0; fi < 40; fi++)
                    {
                        float a = (float)fi / 40f * 6.2831855f;
                        float px2 = fabCx + Mathf.Cos(a) * fabR;
                        float py2 = fabCy + Mathf.Sin(a) * fabR;
                        GUI.DrawTexture(new Rect(px2 - 8f, py2 - 8f, 16f, 16f), px);
                    }
                    GUI.DrawTexture(new Rect(fabRect.x + 4f, fabRect.y + 4f, fabR * 2f - 8f, fabR * 2f - 8f), px);
                    GUI.color = Color.white;
                    GUI.skin.label.fontSize = 20;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(fabRect, "VN");
                    GUI.skin.label.fontSize = 12;
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;

                    bool openMenu = PlayerPrefs.GetFloat("esp_m", 0f) >= 0.5f;
                    if (openMenu)
                    {
                        float mw = Mathf.Min(720f, screenWidth - 40f);
                        float mh = Mathf.Min(480f, screenHeight - 80f);
                        float mxp = (screenWidth - mw) * 0.5f;
                        float myp = (screenHeight - mh) * 0.5f;

                        GUI.color = new Color(0f, 0f, 0f, 0.15f);
                        GUI.DrawTexture(new Rect(mxp + 4f, myp + 6f, mw, mh), px);
                        GUI.color = UiBg;
                        GUI.DrawTexture(new Rect(mxp, myp, mw, mh), px);
                        GUI.color = UiBorder;
                        GUI.DrawTexture(new Rect(mxp, myp, mw, 2f), px);
                        GUI.DrawTexture(new Rect(mxp, myp + mh - 2f, mw, 2f), px);
                        GUI.DrawTexture(new Rect(mxp, myp, 2f, mh), px);
                        GUI.DrawTexture(new Rect(mxp + mw - 2f, myp, 2f, mh), px);

                        float closeS = 28f;
                        Rect closeRect = new Rect(mxp + mw - closeS - 8f, myp + 8f, closeS, closeS);
                        GUI.color = UiBorder;
                        GUI.DrawTexture(closeRect, px);
                        GUI.color = UiText;
                        GUI.skin.label.fontSize = 18;
                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                        GUI.Label(closeRect, "X");
                        if (tUp && closeRect.Contains(tp)) PlayerPrefs.SetFloat("esp_m", 0f);

                        GUI.color = UiAccent;
                        GUI.skin.label.fontSize = 20;
                        GUI.skin.label.alignment = TextAnchor.UpperLeft;
                        GUI.Label(new Rect(mxp + 20f, myp + 12f, 300f, 30f), "VN TOOL");

                        float sideW = 170f;
                        float sideTop = myp + 60f;
                        float sideItemH = 46f;
                        int tabIdx = (int)PlayerPrefs.GetFloat("esp_tab", 0f);
                        if (tabIdx < 0) tabIdx = 0;
                        if (tabIdx > 2) tabIdx = 2;

                        Rect tR0 = new Rect(mxp + 12f, sideTop, sideW - 24f, sideItemH - 6f);
                        Rect tR1 = new Rect(mxp + 12f, sideTop + sideItemH, sideW - 24f, sideItemH - 6f);
                        Rect tR2 = new Rect(mxp + 12f, sideTop + 2 * sideItemH, sideW - 24f, sideItemH - 6f);

                        GUI.color = (tabIdx == 0) ? UiAccent : UiBg;
                        GUI.DrawTexture(tR0, px);
                        GUI.color = (tabIdx == 0) ? Color.white : UiText;
                        GUI.skin.label.fontSize = 16;
                        GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                        GUI.Label(new Rect(tR0.x + 16f, tR0.y, tR0.width, tR0.height), "ESP");
                        if (tUp && tR0.Contains(tp)) PlayerPrefs.SetFloat("esp_tab", 0f);

                        GUI.color = (tabIdx == 1) ? UiAccent : UiBg;
                        GUI.DrawTexture(tR1, px);
                        GUI.color = (tabIdx == 1) ? Color.white : UiText;
                        GUI.Label(new Rect(tR1.x + 16f, tR1.y, tR1.width, tR1.height), "AIM");
                        if (tUp && tR1.Contains(tp)) PlayerPrefs.SetFloat("esp_tab", 1f);

                        GUI.color = (tabIdx == 2) ? UiAccent : UiBg;
                        GUI.DrawTexture(tR2, px);
                        GUI.color = (tabIdx == 2) ? Color.white : UiText;
                        GUI.Label(new Rect(tR2.x + 16f, tR2.y, tR2.width, tR2.height), "MISC");
                        if (tUp && tR2.Contains(tp)) PlayerPrefs.SetFloat("esp_tab", 2f);

                        GUI.color = UiBorder;
                        GUI.DrawTexture(new Rect(mxp + sideW, myp + 8f, 2f, mh - 16f), px);

                        float cxL = mxp + sideW + 20f;
                        float cyT = myp + 24f;
                        float cw = (mw - sideW - 60f) * 0.5f;

                        // ═══ TAB 0: ESP ═══
                        if (tabIdx == 0)
                        {
                            GUI.color = UiAccent;
                            GUI.skin.label.fontSize = 14;
                            GUI.Label(new Rect(cxL, cyT - 4f, cw, 20f), "VISUALS");

                            for (int ei = 0; ei < 6; ei++)
                            {
                                float rowY = cyT + 24f + ei * 44f;
                                string lbl = "";
                                int bit = 0;
                                if (ei == 0) { lbl = "ESP Box"; bit = EspBox; }
                                else if (ei == 1) { lbl = "ESP Line"; bit = EspTracer; }
                                else if (ei == 2) { lbl = "ESP Name"; bit = EspName; }
                                else if (ei == 3) { lbl = "ESP Health"; bit = EspHealth; }
                                else if (ei == 4) { lbl = "ESP Distance"; bit = EspDistance; }
                                else { lbl = "ESP Skeleton"; bit = EspSkeleton; }

                                bool onR = (state & bit) != 0;

                                GUI.color = UiText;
                                GUI.skin.label.fontSize = 14;
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                GUI.Label(new Rect(cxL, rowY, cw - 60f, 30f), lbl);

                                float tx = cxL + cw - 54f;
                                float ty = rowY + 2f;
                                GUI.color = onR ? UiAccent : UiToggleOff;
                                GUI.DrawTexture(new Rect(tx, ty, 50f, 28f), px);
                                GUI.color = Color.white;
                                GUI.DrawTexture(new Rect(onR ? (tx + 24f) : (tx + 2f), ty + 2f, 24f, 24f), px);

                                if (tUp && new Rect(cxL, rowY, cw, 30f).Contains(tp))
                                {
                                    int nS = state ^ bit;
                                    nS |= EspMaster;
                                    state = nS;
                                    self.{{SCENE_STATE_FIELD}} = new Vector2((float)nS, self.{{SCENE_STATE_FIELD}}.y);
                                }
                            }
                        }
                        // ═══ TAB 1: AIM ═══
                        else if (tabIdx == 1)
                        {
                            int auxCur = 0;
                            float auxE2 = self.{{SCENE_STATE_FIELD}}.y;
                            if (auxE2 <= -AuxStateMarker) auxCur = (int)(-auxE2 - AuxStateMarker);

                            GUI.color = UiAccent;
                            GUI.skin.label.fontSize = 14;
                            GUI.Label(new Rect(cxL, cyT - 4f, cw * 2f + 20f, 20f), "AIM ASSIST");

                            for (int ai = 0; ai < 4; ai++)
                            {
                                int rrow = ai / 2;
                                int rcol = ai % 2;
                                float xR = cxL + rcol * (cw + 20f);
                                float yR = cyT + 24f + rrow * 44f;

                                string rLbl = "";
                                int rBit = 0;
                                if (ai == 0) { rLbl = "Silent Aim"; rBit = AimEnabled; }
                                else if (ai == 1) { rLbl = "Aim System"; rBit = AimSystemEnabled; }
                                else if (ai == 2) { rLbl = "Ignore Knocked"; rBit = AimSkipDowned; }
                                else { rLbl = "Đạn thẳng"; rBit = NoRecoil; }

                                bool onR = (state & rBit) != 0;

                                GUI.color = UiText;
                                GUI.skin.label.fontSize = 14;
                                GUI.Label(new Rect(xR, yR, cw - 60f, 30f), rLbl);

                                float tx = xR + cw - 54f;
                                float ty = yR + 2f;
                                GUI.color = onR ? UiAccent : UiToggleOff;
                                GUI.DrawTexture(new Rect(tx, ty, 50f, 28f), px);
                                GUI.color = Color.white;
                                GUI.DrawTexture(new Rect(onR ? (tx + 24f) : (tx + 2f), ty + 2f, 24f, 24f), px);

                                if (tUp && new Rect(xR, yR, cw, 30f).Contains(tp))
                                {
                                    state ^= rBit;
                                    self.{{SCENE_STATE_FIELD}} = new Vector2((float)state, self.{{SCENE_STATE_FIELD}}.y);
                                }
                            }

                            float baseY = cyT + 24f + 2 * 44f + 16f;

                            int boneCur = (state & AimModeMask) >> AimModeShift;
                            string boneLbl = boneCur == 0 ? "Body" : (boneCur == 1 ? "Head" : "Mixed");

                            GUI.color = UiText;
                            GUI.skin.label.fontSize = 14;
                            GUI.Label(new Rect(cxL, baseY, cw, 20f), "Aim Bone");
                            Rect boneRect = new Rect(cxL, baseY + 22f, cw, 34f);
                            GUI.color = UiTrack;
                            GUI.DrawTexture(boneRect, px);
                            GUI.color = UiText;
                            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                            GUI.Label(new Rect(boneRect.x + 12f, boneRect.y, boneRect.width, boneRect.height), boneLbl);
                            if (tUp && boneRect.Contains(tp))
                            {
                                int nb = (boneCur + 1) % 3;
                                state = (state & ~AimModeMask) | (nb << AimModeShift);
                                self.{{SCENE_STATE_FIELD}} = new Vector2((float)state, self.{{SCENE_STATE_FIELD}}.y);
                            }

                            GUI.color = UiText;
                            GUI.skin.label.fontSize = 14;
                            GUI.Label(new Rect(cxL + cw + 20f, baseY, cw, 20f), "FOV Size");
                            int fovByte = (auxCur >> AuxFovRadiusShift) & 0xFF;
                            float fovV = fovByte * 2f;
                            if (fovV < 40f) fovV = 200f;
                            Rect sliderR = new Rect(cxL + cw + 20f, baseY + 28f, cw, 22f);

                            float pct = Mathf.Clamp01((fovV - 40f) / 460f);
                            GUI.color = UiTrack;
                            GUI.DrawTexture(new Rect(sliderR.x, sliderR.y + sliderR.height * 0.5f - 3f, sliderR.width, 6f), px);
                            GUI.color = UiAccent;
                            GUI.DrawTexture(new Rect(sliderR.x, sliderR.y + sliderR.height * 0.5f - 3f, sliderR.width * pct, 6f), px);
                            float thX = sliderR.x + sliderR.width * pct - 10f;
                            GUI.color = Color.white;
                            GUI.DrawTexture(new Rect(thX, sliderR.y + sliderR.height * 0.5f - 10f, 20f, 20f), px);
                            GUI.color = UiAccent;
                            GUI.DrawTexture(new Rect(thX + 3f, sliderR.y + sliderR.height * 0.5f - 7f, 14f, 14f), px);

                            GUI.color = UiSubText;
                            GUI.skin.label.fontSize = 12;
                            GUI.skin.label.alignment = TextAnchor.MiddleRight;
                            GUI.Label(new Rect(sliderR.x, sliderR.y - 18f, sliderR.width, 16f), ((int)fovV) + "px");
                            GUI.skin.label.alignment = TextAnchor.UpperLeft;

                            Rect hitA = new Rect(sliderR.x - 10f, sliderR.y - 8f, sliderR.width + 20f, sliderR.height + 16f);
                            GameObject slDrag = GameObject.Find("__esp_slider_drag");
                            if (tUp && hitA.Contains(tp))
                            {
                                float np = Mathf.Clamp01((tp.x - sliderR.x) / sliderR.width);
                                float nv = 40f + np * 460f;
                                int nb2 = (int)(nv / 2f);
                                if (nb2 < 1) nb2 = 1;
                                if (nb2 > 255) nb2 = 255;
                                auxCur = (auxCur & ~(0xFF << AuxFovRadiusShift)) | (nb2 << AuxFovRadiusShift);
                                self.{{SCENE_STATE_FIELD}} = new Vector2(self.{{SCENE_STATE_FIELD}}.x, -AuxStateMarker - (float)auxCur);
                            }
                            if (slDrag != null && tDrag)
                            {
                                float np = Mathf.Clamp01((tp.x - sliderR.x) / sliderR.width);
                                float nv = 40f + np * 460f;
                                int nb2 = (int)(nv / 2f);
                                if (nb2 < 1) nb2 = 1;
                                if (nb2 > 255) nb2 = 255;
                                auxCur = (auxCur & ~(0xFF << AuxFovRadiusShift)) | (nb2 << AuxFovRadiusShift);
                                self.{{SCENE_STATE_FIELD}} = new Vector2(self.{{SCENE_STATE_FIELD}}.x, -AuxStateMarker - (float)auxCur);
                            }
                            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && hitA.Contains(tp))
                            {
                                if (slDrag != null) { slDrag.name = "slider_old"; UnityEngine.Object.Destroy(slDrag); }
                                slDrag = new GameObject("__esp_slider_drag");
                                UnityEngine.Object.DontDestroyOnLoad(slDrag);
                            }
                            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended && slDrag != null)
                            {
                                slDrag.name = "slider_done";
                                UnityEngine.Object.Destroy(slDrag);
                            }
                        }
                        // ═══ TAB 2: MISC ═══
                        else
                        {
                            int auxM = 0;
                            float auxE3 = self.{{SCENE_STATE_FIELD}}.y;
                            if (auxE3 <= -AuxStateMarker) auxM = (int)(-auxE3 - AuxStateMarker);
                            int r8m = (int)PlayerPrefs.GetFloat("esp_r8", 0f);

                            GUI.color = UiAccent;
                            GUI.skin.label.fontSize = 14;
                            GUI.Label(new Rect(cxL, cyT - 4f, cw * 2f + 20f, 20f), "MISC");

                            for (int mi = 0; mi < 8; mi++)
                            {
                                int row = mi / 2;
                                int col = mi % 2;
                                float xR = cxL + col * (cw + 20f);
                                float yR = cyT + 24f + row * 44f;

                                string mL = "";
                                int group = 0;
                                int mB = 0;
                                if (mi == 0) { mL = "Speed x3"; group = 0; mB = AuxSpeedRunning; }
                                else if (mi == 1) { mL = "Fast Para"; group = 0; mB = AuxFastParachute; }
                                else if (mi == 2) { mL = "Fast Heal"; group = 0; mB = AuxFastHeal; }
                                else if (mi == 3) { mL = "Fast Fire"; group = 0; mB = AuxFastFire; }
                                else if (mi == 4) { mL = "Back Jump"; group = 0; mB = AuxBackJump; }
                                else if (mi == 5) { mL = "Ghost"; group = 0; mB = AuxGhostControl; }
                                else if (mi == 6) { mL = "Fast Loot"; group = 1; mB = R8FastLoot; }
                                else { mL = "No Fog"; group = 1; mB = R8NoFog; }

                                bool onR = (group == 0) ? ((auxM & mB) != 0) : ((r8m & mB) != 0);

                                GUI.color = UiText;
                                GUI.skin.label.fontSize = 14;
                                GUI.Label(new Rect(xR, yR, cw - 60f, 30f), mL);

                                float tx = xR + cw - 54f;
                                float ty = yR + 2f;
                                GUI.color = onR ? UiAccent : UiToggleOff;
                                GUI.DrawTexture(new Rect(tx, ty, 50f, 28f), px);
                                GUI.color = Color.white;
                                GUI.DrawTexture(new Rect(onR ? (tx + 24f) : (tx + 2f), ty + 2f, 24f, 24f), px);

                                if (tUp && new Rect(xR, yR, cw, 30f).Contains(tp))
                                {
                                    if (group == 0)
                                    {
                                        auxM ^= mB;
                                        self.{{SCENE_STATE_FIELD}} = new Vector2(self.{{SCENE_STATE_FIELD}}.x, -AuxStateMarker - (float)auxM);
                                    }
                                    else
                                    {
                                        r8m ^= mB;
                                        PlayerPrefs.SetFloat("esp_r8", (float)r8m);
                                    }
                                }
                            }

                            GUI.color = UiSubText;
                            GUI.skin.label.fontSize = 12;
                            GUI.Label(new Rect(cxL, cyT + 24f + 4 * 44f + 16f, cw * 2f + 20f, 20f),
                                "Nếu không hoạt động, thử bật trong trận.");
                        }

                        GUI.skin.label.fontSize = 12;
                        GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    }
                }
            }
            catch (Exception) { }

            // ═══════════════ AUX APPLY ═══════════════
            try
            {
                float encodedAuxState = self.{{SCENE_STATE_FIELD}}.y;
                if (encodedAuxState <= -AuxStateMarker)
                {
                    int packedAuxState = (int)(-encodedAuxState - AuxStateMarker);
                    int auxState = packedAuxState & AuxMask;
                    Player _lp = GameFacade.CurrentLocalPlayer();
                    if (_lp != null && _lp.gameObject != null && _lp.gameObject.activeInHierarchy)
                    {
                        float _dt = Time.deltaTime;
                        PlayerAttributes attributes = _lp.Attributes;
                        if (attributes != null)
                        {
                            bool speedRunning = (auxState & AuxSpeedRunning) != 0;
                            bool speedRunningApplied = (auxState & AuxSpeedRunningApplied) != 0;
                            if (speedRunning)
                            {
                                attributes.SetSpecialRunSpeedScaleByKeyAndValue(
                                    PlayerAttributes.{{SPEED_TYPE}}.BuffSystem,
                                    SpeedRunningKey, 3f);
                                auxState |= AuxSpeedRunningApplied;
                            }
                            else if (speedRunningApplied)
                            {
                                attributes.RemoveSpecialRunSpeedScaleByKey(
                                    PlayerAttributes.{{SPEED_TYPE}}.BuffSystem,
                                    SpeedRunningKey);
                                auxState &= ~AuxSpeedRunningApplied;
                            }

                            if ((auxState & AuxFakeDamage) != 0)
                            {
                                attributes.BuffWeaponDamageScale = 10000.0f;
                                attributes.DamageAdditionScale   = 10000.0f;
                                attributes.ExecuteDamageScale    = 10000.0f;
                            }
                            else if (attributes.BuffWeaponDamageScale > 10.0f)
                            {
                                attributes.BuffWeaponDamageScale = 0f;
                                attributes.DamageAdditionScale   = 0f;
                                attributes.ExecuteDamageScale    = 0f;
                            }
                        }

                        {
                            int fullAux = packedAuxState;
                            bool isWide = (fullAux & AuxWideCamera) != 0;
                            if (isWide)
                            {
                                int wcRaw = (fullAux >> AuxWideCamFovShift) & 0x3F;
                                float wFov = wcRaw > 0 ? 60f + (float)wcRaw : 88f;
                                CameraControllerManager camMgr = GameFacade.CurrentCameraControllerManager();
                                if (camMgr != null) camMgr.SetFov(wFov);
                            }
                        }

                        {
                            int fullAux = packedAuxState;
                            bool fastHealOn = (fullAux & AuxFastHeal) != 0;
                            PlayerAttributes healAttrs = _lp.Attributes;
                            if (healAttrs != null)
                            {
                                if (fastHealOn)
                                {
                                    healAttrs.EatSpeedScale = 10.0f;
                                    healAttrs.FSModeUseMedikitFasterRate = 10.0f;
                                    healAttrs.BeHealingIncreaseRatio = 5.0f;
                                }
                                else if (healAttrs.EatSpeedScale > 1.5f)
                                {
                                    healAttrs.EatSpeedScale = 1.0f;
                                    healAttrs.FSModeUseMedikitFasterRate = 0.0f;
                                    healAttrs.BeHealingIncreaseRatio = 0.0f;
                                }
                            }
                        }

                        {
                            int fullAux = packedAuxState;
                            bool fastFireOn = (fullAux & AuxFastFire) != 0;
                            PlayerAttributes fireAttrs = _lp.Attributes;
                            if (fireAttrs != null)
                            {
                                if (fastFireOn) fireAttrs.FireIntervalScale = 0.5f;
                                else if (fireAttrs.FireIntervalScale < 0.9f) fireAttrs.FireIntervalScale = 1.0f;
                            }
                        }

                        {
                            bool backJumpOn = (packedAuxState & AuxBackJump) != 0;
                            try
                            {
                                if (backJumpOn)
                                {
                                    COW.GameVarDef.EnableAccelerationOnFalling = false;
                                    COW.GameVarDef.EnableLowFallingSwapWeapon = true;
                                }
                                else
                                {
                                    COW.GameVarDef.EnableAccelerationOnFalling = true;
                                    COW.GameVarDef.EnableLowFallingSwapWeapon = false;
                                }
                            }
                            catch (Exception) { }
                        }

                        {
                            bool ghostControlOn = (packedAuxState & AuxGhostControl) != 0;
                            if (ghostControlOn)
                            {
                                if (GameObject.Find("__esp_ghost_control") == null)
                                {
                                    GameObject _gc = new GameObject("__esp_ghost_control");
                                    UnityEngine.Object.DontDestroyOnLoad(_gc);
                                }
                            }
                            else
                            {
                                GameObject _gcOff = GameObject.Find("__esp_ghost_control");
                                if (_gcOff != null) { _gcOff.name = "__esp_ghost_control_off"; UnityEngine.Object.Destroy(_gcOff); }
                                GameObject _gaOff = GameObject.Find("__esp_ghost_active");
                                if (_gaOff != null) { _gaOff.name = "__esp_ghost_active_off"; UnityEngine.Object.Destroy(_gaOff); }
                                GameObject _gnOff = GameObject.Find("__esp_ghost_anchor");
                                if (_gnOff != null) { _gnOff.name = "__esp_ghost_anchor_off"; UnityEngine.Object.Destroy(_gnOff); }
                                GameObject _gdOff = GameObject.Find("__esp_ghost_drag");
                                if (_gdOff != null) { _gdOff.name = "__esp_ghost_drag_off"; UnityEngine.Object.Destroy(_gdOff); }
                                GameObject _guOff = GameObject.Find("__esp_ghost_ui_state");
                                if (_guOff != null) { _guOff.name = "__esp_ghost_ui_state_off"; UnityEngine.Object.Destroy(_guOff); }
                            }
                        }

                        {
                            int r8 = (int)PlayerPrefs.GetFloat("esp_r8", 0f);
                            PlayerAttributes rAttrs = _lp.Attributes;
                            if (rAttrs != null)
                            {
                                bool fastReviveOn = (r8 & R8FastRevive) != 0;
                                if (fastReviveOn) { rAttrs.RescureRate = 5.0f; }
                                else if (rAttrs.RescureRate > 2.0f) { rAttrs.RescureRate = 1.0f; }
                                bool skillCDOn = (r8 & R8SkillCD) != 0;
                                rAttrs.ActiveSkillCdReduction = skillCDOn ? 0.9f : 0f;
                                rAttrs.PetSkillCDReduction = skillCDOn ? 0.9f : 0f;
                            }
                            {
                                bool chamsOn = (r8 & R8Chams) != 0;
                                try
                                {
                                    if (chamsOn)
                                    {
                                        COW.GameVarDef.EnableShowPlayerOutline = true;
                                        COW.GameVarDef.ShowPlayerOutlineMaxDistance = 500u;
                                        COW.GameVarDef.ShowPlayerOutlineColor = 0xFFFF0000u;
                                        COW.GameVarDef.ShowPlayerOutlineWidth = 3f;
                                        COW.GameVarDef.PCOBOutlineSolid = true;
                                    }
                                    else
                                    {
                                        COW.GameVarDef.EnableShowPlayerOutline = false;
                                        COW.GameVarDef.PCOBOutlineSolid = false;
                                    }
                                }
                                catch (Exception) { }
                            }
                            {
                                bool fastLootOn = (r8 & R8FastLoot) != 0;
                                try
                                {
                                    if (fastLootOn)
                                    {
                                        COW.GameVarDef.AutoPickupPoolOptEnabled = true;
                                        COW.GameVarDef.AutoPickupInvokeOptEnabled = true;
                                        COW.GameVarDef.AutoPickUpSortAttachmentEnable = true;
                                        COW.GameVarDef.EnableBackgroundCacheAutoPickupWeapon = true;
                                        COW.GameVarDef.EnableBackgroundCacheAutoPickupFppWeapon = true;
                                    }
                                }
                                catch (Exception) { }
                            }
                            {
                                bool camHackOn = (r8 & R8CamHack) != 0;
                                try
                                {
                                    if (camHackOn)
                                    {
                                        COW.GameVarDef.CameraShakeCabinFactor = 1.0f;
                                        COW.GameVarDef.CameraBackOffectLowerCutAngle = 1.0f;
                                        COW.GameVarDef.CameraBackOffectLowerAngleDist = 1.0f;
                                        COW.GameVarDef.CameraUpOffectLowerAngleDist = 1.0f;
                                        COW.GameVarDef.CameraUpOffectLowerHitDrawBackDist = 1.0f;
                                    }
                                }
                                catch (Exception) { }
                            }
                            {
                                bool unlockFpsOn = (r8 & R8UnlockFps) != 0;
                                try
                                {
                                    if (unlockFpsOn)
                                    {
                                        if (Application.targetFrameRate != 120)
                                        {
                                            Application.targetFrameRate = 120;
                                            GameFacade.SetFrameRate(COW.EHighFPS.HighFPS120);
                                        }
                                    }
                                    else if (Application.targetFrameRate > 60)
                                    {
                                        Application.targetFrameRate = 60;
                                        GameFacade.SetFrameRate(COW.EHighFPS.HighFPS);
                                    }
                                }
                                catch (Exception) { }
                            }
                            {
                                bool noFogOn = (r8 & R8NoFog) != 0;
                                try
                                {
                                    if (noFogOn)
                                    {
                                        RenderSettings.fog = false;
                                        CameraControllerManager noFogCam = GameFacade.CurrentCameraControllerManager();
                                        if (noFogCam != null) noFogCam.SetFarClipPlane(1000f);
                                    }
                                    else if (!RenderSettings.fog)
                                    {
                                        RenderSettings.fog = true;
                                        CameraControllerManager noFogCam = GameFacade.CurrentCameraControllerManager();
                                        if (noFogCam != null) noFogCam.ReSetFarClipPlane();
                                    }
                                }
                                catch (Exception) { }
                            }
                            {
                                bool fastCrouchOn = (r8 & R8FastCrouch) != 0;
                                try { COW.GameVarDef.CanCrouchingRunFast = fastCrouchOn; }
                                catch (Exception) { }
                            }
                        }

                        {
                            if ((state & AimSystemEnabled) != 0 && (state & AimEnabled) == 0
                                && Input.touchCount >= 2)
                            {
                                Camera _cam = Camera.main;
                                {{MATCH_TYPE}} _match = GameFacade.CurrentMatch();
                                IList _pl = _match == null ? null : _match.{{MATCH_PLAYERS_METHOD}}();
                                if (_cam != null && _pl != null)
                                {
                                    int _mode = (state & AimModeMask) >> AimModeShift;
                                    int _fovRaw = (packedAuxState >> AuxFovRadiusShift) & 0xFF;
                                    float _fovR = _fovRaw >= 10 ? (float)_fovRaw * 2f : 200f;
                                    float _bestSc = _fovR * _fovR;
                                    Player _bt = null; Collider _bc = null; Vector3 _bp = Vector3.zero;
                                    int _pcnt = _pl.Count; if (_pcnt > 64) _pcnt = 64;
                                    for (int _i = 0; _i < _pcnt; _i++)
                                    {
                                        try
                                        {
                                            Player _c = _pl[_i] as Player;
                                            if (_c == null || _c.gameObject == null || !_c.gameObject.activeInHierarchy
                                                || _c.IsLocalPlayer() || _c.CurHP <= 0
                                                || _c.IsLocalTeammate(false) || _c.IsLocalTeammate(true)
                                                || ((_c.IsDieing || _c.IsKnockedDownBleed) && (state & AimSkipDowned) != 0)) continue;
                                            Transform _cr = _c.RootTransform;
                                            if (_cr == null) continue;
                                            Vector3 _tp; Collider _tc = null;
                                            if (_mode == 1)
                                            {
                                                Transform _h = _c.GetHeadTF();
                                                if (_h == null) continue;
                                                _tp = _h.position;
                                                _tc = _c.HeadCollider;
                                                if (_tc == null) _tc = (Collider)_h.GetComponent(typeof(Collider));
                                            }
                                            else
                                            {
                                                _tp = _cr.position + new Vector3(0f, 0.9f, 0f);
                                                _tc = (Collider)_cr.GetComponent("CapsuleCollider");
                                                if (_tc == null) _tc = _c.HeadCollider;
                                            }
                                            if (_tc == null) continue;
                                            Vector3 _sv = _cam.WorldToScreenPoint(_tp);
                                            if (_sv.z <= 1f || float.IsNaN(_sv.x) || float.IsNaN(_sv.y)
                                                || float.IsInfinity(_sv.x) || float.IsInfinity(_sv.y)) continue;
                                            float _dx = _sv.x - (float)Screen.width * 0.5f;
                                            float _dy = _sv.y - (float)Screen.height * 0.5f;
                                            float _sc = _dx * _dx + _dy * _dy;
                                            if (float.IsNaN(_sc) || float.IsInfinity(_sc) || _sc >= _bestSc) continue;
                                            _bestSc = _sc; _bt = _c; _bc = _tc; _bp = _tp;
                                        }
                                        catch (Exception) { }
                                    }
                                    if (_bt != null && _bc != null)
                                    {
                                        try
                                        {
                                            _bt.EspLockedAimingCollider = _bc;
                                            Vector3 _dir = _bp - _lp.AimStartPostion;
                                            float _dlen = _dir.sqrMagnitude;
                                            if (_dlen > 0.0001f && !float.IsNaN(_dlen) && !float.IsInfinity(_dlen))
                                            {
                                                Quaternion _tgt = Quaternion.LookRotation(_dir);
                                                Quaternion _cur = _lp.GetAimRotation();
                                                float _fd = _dt > 0f && _dt < 0.1f ? _dt : 0.016f;
                                                float _bl = 1f - Mathf.Exp(-64f * _fd);
                                                _bl = Mathf.Clamp01(_bl);
                                                _lp.SetAimRotation(Quaternion.Slerp(_cur, _tgt, _bl), false);
                                            }
                                        }
                                        catch (Exception) { }
                                    }
                                }
                            }
                        }

                        if ((auxState & AuxFastParachute) != 0)
                        {
                            bool skySurfing = _lp.IsSkySurfing;
                            bool skyDiving = _lp.IsSkyDiving;
                            bool parachuting = _lp.IsParachuting;
                            if (skySurfing)
                            {
                                _lp.RequestSkyDiving();
                            }
                            else if (skyDiving || parachuting)
                            {
                                CharacterController controller = _lp.CharacterController;
                                if (controller != null && controller.enabled)
                                {
                                    RaycastHit groundHit;
                                    Vector3 origin = controller.transform.position;
                                    bool foundGround = Physics.Raycast(origin, new Vector3(0f, -1f, 0f),
                                        out groundHit, 2048f, -5, QueryTriggerInteraction.Ignore);
                                    if (foundGround)
                                    {
                                        bool landed = controller.isGrounded;
                                        float groundDistance = groundHit.distance;
                                        if (!landed && groundDistance > 0f)
                                        {
                                            float contactPadding = controller.skinWidth;
                                            if (contactPadding < 0.05f) contactPadding = 0.05f;
                                            float moveDistance = groundDistance + contactPadding;
                                            float frameStep = _dt * 768f;
                                            if (frameStep < 0.5f) frameStep = 0.5f;
                                            else if (frameStep > 32f) frameStep = 32f;
                                            if (moveDistance > frameStep) moveDistance = frameStep;
                                            CollisionFlags collision = controller.Move(new Vector3(0f, -moveDistance, 0f));
                                            landed = (collision & CollisionFlags.Below) != 0 || controller.isGrounded;
                                        }
                                        if (landed)
                                        {
                                            _lp.StopParachuting(true);
                                            _lp.OnLandFinsish();
                                        }
                                    }
                                }
                            }
                        }

                        packedAuxState = (packedAuxState & ~AuxMask) | (auxState & AuxMask);
                        self.{{SCENE_STATE_FIELD}} = new Vector2(
                            self.{{SCENE_STATE_FIELD}}.x,
                            -AuxStateMarker - (float)packedAuxState);
                    }
                }
            }
            catch (Exception) { }

            GUI.matrix = savedMatrix;
            GUI.color = savedColor;
        }

        public static {{AIM_INFO_TYPE}} SilentAim(Player self)
        {
            if (self == null) return null;
            {{AIM_INFO_TYPE}} info = self.{{PLAYER_AIM_INFO_FIELD}};
            if (info == null) return info;
            try
            {
                GameObject driver = GameObject.Find("__esp_driver");
                SceneEditBoxSelectTool menu = driver == null ? null
                    : (SceneEditBoxSelectTool)driver.GetComponent(typeof(SceneEditBoxSelectTool));
                int state = menu == null ? 0 : (int)menu.{{SCENE_STATE_FIELD}}.x;

                float fdEnc = menu == null ? 0f : menu.{{SCENE_STATE_FIELD}}.y;
                if (fdEnc <= -AuxStateMarker)
                {
                    int fdAux = (int)(-fdEnc - AuxStateMarker) & AuxMask;
                    PlayerAttributes fdAttrs = self.Attributes;
                    if (fdAttrs != null)
                    {
                        if ((fdAux & AuxFakeDamage) != 0)
                        {
                            fdAttrs.BuffWeaponDamageScale = 10000.0f;
                            fdAttrs.DamageAdditionScale   = 10000.0f;
                            fdAttrs.ExecuteDamageScale    = 10000.0f;
                        }
                        else if (fdAttrs.BuffWeaponDamageScale > 10.0f)
                        {
                            fdAttrs.BuffWeaponDamageScale = 0f;
                            fdAttrs.DamageAdditionScale   = 0f;
                            fdAttrs.ExecuteDamageScale    = 0f;
                        }
                    }
                }

                bool silentAimOn = (state & AimEnabled) != 0;
                bool aimFovOn    = (state & AimSystemEnabled) != 0;
                if (!silentAimOn && !aimFovOn) return info;

                int silentFovPx  = 0;
                int aimFovPixels = 0;
                if (fdEnc <= -AuxStateMarker)
                {
                    int fullAux  = (int)(-fdEnc - AuxStateMarker);
                    silentFovPx  = ((fullAux >> AuxSilentFovShift) & 0xFF) * 2;
                    aimFovPixels = ((fullAux >> AuxFovRadiusShift) & 0xFF) * 2;
                }

                bool aimAtHead;
                int fovFilterPx;
                if (silentAimOn)
                {
                    aimAtHead   = true;
                    fovFilterPx = silentFovPx;
                }
                else
                {
                    int aimModeFov  = (state & AimModeMask) >> AimModeShift;
                    int headRateFov = ((state & HeadRateMask) >> HeadRateShift) * 25;
                    aimAtHead   = aimModeFov == 1
                        || (aimModeFov == 2 && UnityEngine.Random.Range(0, 100) < headRateFov);
                    fovFilterPx = aimFovPixels;
                }

                {{MATCH_TYPE}} match = GameFacade.CurrentMatch();
                IList players = match == null ? null : match.{{MATCH_PLAYERS_METHOD}}();
                Camera camera = Camera.main;
                if (players == null || camera == null) return info;

                Vector3 aimStart = self.AimStartPostion;
                float bestScore = float.MaxValue;
                Collider bestCollider = null;
                Vector3 bestPosition = Vector3.zero;

                for (int index = 0; index < players.Count; index++)
                {
                    Player candidate = players[index] as Player;
                    GameObject candidateObject = candidate == null ? null : candidate.gameObject;
                    if (candidateObject == null || !candidateObject.activeInHierarchy
                        || candidate.IsLocalPlayer() || candidate.CurHP <= 0
                        || candidate.IsLocalTeammate(false) || candidate.IsLocalTeammate(true)
                        || ((candidate.IsDieing || candidate.IsKnockedDownBleed) && (state & AimSkipDowned) != 0))
                        continue;

                    Transform root = candidate.RootTransform;
                    Transform head = candidate.GetHeadTF();
                    Collider headCollider = candidate.HeadCollider;
                    if (root == null) continue;

                    Vector3 hitPosition;
                    Collider hitCollider;
                    if (aimAtHead)
                    {
                        if (head == null || headCollider == null) continue;
                        hitPosition = head.position;
                        hitCollider = headCollider;
                    }
                    else
                    {
                        hitPosition = root.position + new Vector3(0f, 0.9f, 0f);
                        hitCollider = (Collider)root.GetComponent("CapsuleCollider");
                        if (hitCollider == null)
                        {
                            if (head == null || headCollider == null) continue;
                            hitCollider = headCollider;
                        }
                    }

                    if (Vector3.Distance(hitPosition, aimStart) > 150f) continue;
                    Vector3 screen = camera.WorldToScreenPoint(hitPosition);
                    if (screen.z <= 1f || float.IsNaN(screen.x) || float.IsNaN(screen.y)
                        || float.IsNaN(screen.z) || float.IsInfinity(screen.x)
                        || float.IsInfinity(screen.y) || float.IsInfinity(screen.z)) continue;

                    float dx = screen.x - (float)Screen.width * 0.5f;
                    float dy = screen.y - (float)Screen.height * 0.5f;
                    float score = dx * dx + dy * dy;
                    if (float.IsNaN(score) || float.IsInfinity(score) || score >= bestScore) continue;
                    if (fovFilterPx > 0 && score > (float)(fovFilterPx * fovFilterPx)) continue;

                    bestScore = score;
                    bestCollider = hitCollider;
                    bestPosition = hitPosition;
                }

                if (bestCollider != null)
                {
                    if (silentAimOn)
                    {
                        Vector3 rayDirection = Vector3.Normalize(bestPosition - aimStart);
                        info.{{AIM_GAME_OBJECT_FIELD}} = bestCollider.gameObject;
                        info.{{AIM_COLLIDER_FIELD}} = bestCollider;
                        info.{{AIM_HIT_POSITION_FIELD}} = bestPosition;
                        info.{{AIM_TRACE_POSITION_FIELD}} = bestPosition;
                        info.{{AIM_DIRECTION_FIELD}} = rayDirection;
                        info.{{AIM_ORIGIN_FIELD}} = aimStart;
                        info.{{AIM_SECONDARY_ORIGIN_FIELD}} = aimStart;
                        info.{{AIM_HIT_TYPE_FIELD}} = ({{AIM_HIT_TYPE}})1;
                        info.{{AIM_FLAG_A_FIELD}} = false;
                        info.{{AIM_FLAG_B_FIELD}} = false;
                        info.{{AIM_SHORT_FIELD}} = (short)0;
                    }
                    else
                    {
                        self.EspLockedAimingCollider = bestCollider;
                    }
                }
                return info;
            }
            catch (Exception) { return info; }
        }

        public static bool AimSystem(Player self)
        {
            if (self == null) return false;
            bool original = false;
            try
            {
                original = self.EspBaseIsMovableEntity();
                GameObject driver = GameObject.Find("__esp_driver");
                SceneEditBoxSelectTool menu = driver == null ? null
                    : (SceneEditBoxSelectTool)driver.GetComponent(typeof(SceneEditBoxSelectTool));
                int state = menu == null ? 0 : (int)menu.{{SCENE_STATE_FIELD}}.x;
                if ((state & AimSystemEnabled) == 0 || (state & AimEnabled) != 0
                    || self.gameObject == null || !self.gameObject.activeInHierarchy
                    || self.IsLocalPlayer() || self.CurHP <= 0
                    || self.IsLocalTeammate(false) || self.IsLocalTeammate(true)
                    || ((self.IsDieing || self.IsKnockedDownBleed) && (state & AimSkipDowned) != 0))
                    return original;

                int aimModeFov = (state & AimModeMask) >> AimModeShift;
                int headRateFov = ((state & HeadRateMask) >> HeadRateShift) * 25;
                bool fovAimAtHead = aimModeFov == 1
                    || (aimModeFov == 2 && UnityEngine.Random.Range(0, 100) < headRateFov);
                Collider targetCollider = null;
                if (fovAimAtHead)
                {
                    targetCollider = self.HeadCollider;
                }
                else
                {
                    Transform neck = self.NeckBone;
                    IList fireColliders = self.FireColliders;
                    float nearestDistance = 10000f;
                    if (neck != null && fireColliders != null)
                    {
                        for (int index = 0; index < fireColliders.Count; index++)
                        {
                            Collider candidateCollider = fireColliders[index] as Collider;
                            if (candidateCollider == null) continue;
                            float distance = Vector3.Distance(candidateCollider.transform.position, neck.position);
                            if (distance < nearestDistance) { nearestDistance = distance; targetCollider = candidateCollider; }
                        }
                    }
                    if (targetCollider == null && neck != null)
                        targetCollider = (Collider)neck.GetComponent(typeof(Collider));
                    if (targetCollider == null)
                    {
                        Transform root = self.RootTransform;
                        targetCollider = root == null ? null : (Collider)root.GetComponent("CapsuleCollider");
                    }
                    if (targetCollider == null) targetCollider = self.HeadCollider;
                }
                if (targetCollider == null) return original;

                float fovEnc = menu == null ? 0f : menu.{{SCENE_STATE_FIELD}}.y;
                int fovPackedAim = fovEnc <= -AuxStateMarker ? (int)(-fovEnc - AuxStateMarker) : 0;
                float fovRadiusAim = (float)((fovPackedAim >> AuxFovRadiusShift) & 0xFF) * 2f;
                if (fovRadiusAim > 0f)
                {
                    Camera camAim = Camera.main;
                    if (camAim != null)
                    {
                        Vector3 screenPosAim = camAim.WorldToScreenPoint(targetCollider.transform.position);
                        if (screenPosAim.z > 0f)
                        {
                            float dxAim = screenPosAim.x - (float)Screen.width * 0.5f;
                            float dyAim = screenPosAim.y - (float)Screen.height * 0.5f;
                            if (dxAim * dxAim + dyAim * dyAim > fovRadiusAim * fovRadiusAim) return original;
                        }
                    }
                }
                self.EspLockedAimingCollider = targetCollider;
                return original;
            }
            catch (Exception) { return original; }
        }

        public static float ScatterRate(PlayerAttributes self)
        {
            if (self == null) return 0f;
            try
            {
                GameObject driver = GameObject.Find("__esp_driver");
                SceneEditBoxSelectTool menu = driver == null ? null
                    : (SceneEditBoxSelectTool)driver.GetComponent(typeof(SceneEditBoxSelectTool));
                int state = menu == null ? 0 : (int)menu.{{SCENE_STATE_FIELD}}.x;
                if ((state & NoRecoil) != 0) return 0f;
                return 1f;
            }
            catch (Exception) { return 1f; }
        }
    }
}
