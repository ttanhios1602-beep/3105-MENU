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
        // ═══════════════════════════════════════════════════════════
        //  ESP BITS
        // ═══════════════════════════════════════════════════════════
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
        private const int EspEnemyCount = 2048;

        // ═══════════════════════════════════════════════════════════
        //  STATE BITS
        // ═══════════════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════════════
        //  FEATURE BITS
        // ═══════════════════════════════════════════════════════════
        private const int AuxFastFire = 1 << 28;
        private const int AuxBackJump = 1 << 29;
        private const int AuxSpeedRun = 1 << 27;
        private const int AuxSpeedRunApplied = 1 << 26;
        private const int AuxMask = 4095;
        private const int AuxFovRadiusShift = 4;
        private const int AuxSilentFovShift = 12;
        private const float AuxStateMarker = 1000000f;
        private const ulong SpeedRunningKey = 4995421289296778564UL;

        private const int DefaultStateBits = (2 << AimModeShift) | (3 << HeadRateShift);

        // ═══════════════════════════════════════════════════════════
        //  BOOTSTRAP
        // ═══════════════════════════════════════════════════════════
        public static bool Bootstrap(Player self)
        {
            if (self == null) return false;
            bool stealthState;
            try
            {
                stealthState = self.IsInStealth();
                GameObject driver = GameObject.Find("__esp_driver");
                bool createdDriver = false;
                if (driver == null)
                {
                    driver = new GameObject("__esp_driver");
                    createdDriver = true;
                }

                Transform oldLogHolder = driver.transform.Find("__esp_log_holder");
                if (oldLogHolder != null) UnityEngine.Object.Destroy(oldLogHolder.gameObject);

                Component component = driver.GetComponent(typeof(SceneEditBoxSelectTool));
                if (component == null) component = driver.AddComponent(typeof(SceneEditBoxSelectTool));
                if (component == null)
                {
                    if (createdDriver) UnityEngine.Object.Destroy(driver);
                }
                else
                {
                    if (createdDriver)
                    {
                        UnityEngine.Object.DontDestroyOnLoad(driver);
                        SceneEditBoxSelectTool menu = (SceneEditBoxSelectTool)component;
                        menu.{{SCENE_POSITION_FIELD}} = new Vector2(-999f, 0f);
                        int initialState = StateInitialized | DefaultStateBits;
                        menu.{{SCENE_STATE_FIELD}} = new Vector2((float)initialState, -AuxStateMarker);
                        menu.{{SCENE_MENU_FIELD}} = false;
                    }
                }
            }
            catch (Exception) { stealthState = false; }
            return stealthState;
        }

        // ═══════════════════════════════════════════════════════════
        //  DRAW — ESP + FOV + Features + MENU (gọi cuối)
        // ═══════════════════════════════════════════════════════════
        public static void Draw(SceneEditBoxSelectTool self)
        {
            if (self == null) return;
            Event currentEvent = Event.current;
            if (currentEvent == null) return;
            if (currentEvent.type != EventType.Repaint) return;

            GameObject driverObject = self.gameObject;
            if (driverObject == null || driverObject.name != "__esp_driver") return;

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            int state = (int)self.{{SCENE_STATE_FIELD}}.x;

            if ((state & StateInitialized) == 0)
            {
                self.{{SCENE_POSITION_FIELD}} = new Vector2(-999f, 0f);
                state = StateInitialized | DefaultStateBits;
                self.{{SCENE_STATE_FIELD}} = new Vector2((float)(state & 0xFFFFFF), -AuxStateMarker);
            }

            float now = Time.unscaledTime;
            int curFrame = Time.frameCount;
            if (curFrame % 1200 == 1)
            {
                System.GC.Collect();
                System.GC.Collect();
            }

            // Đọc config .pdata mỗi 1 giây
            if (now - self.{{SCENE_POSITION_FIELD}}.x > 1.0f)
            {
                self.{{SCENE_POSITION_FIELD}} = new Vector2(now, 0f);
                try
                {
                    string cfgPath = Application.persistentDataPath + "/contentcache/Compulsory/ios/gameassetbundles/ingame/.pdata";
                    if (File.Exists(cfgPath))
                    {
                        byte[] cfgBytes = File.ReadAllBytes(cfgPath);
                        int newState = StateInitialized | DefaultStateBits;
                        int newAux = 0;
                        if (cfgBytes.Length >= 4)
                        {
                            newState = (int)cfgBytes[0]
                                | ((int)cfgBytes[1] << 8)
                                | ((int)cfgBytes[2] << 16)
                                | ((int)cfgBytes[3] << 24);
                            newState |= StateInitialized;
                        }
                        if (cfgBytes.Length >= 8)
                        {
                            newAux = (int)cfgBytes[4]
                                | ((int)cfgBytes[5] << 8)
                                | ((int)cfgBytes[6] << 16)
                                | ((int)cfgBytes[7] << 24);
                        }
                        state = newState;
                        int packedAux = newAux;
                        float encodedAux = self.{{SCENE_STATE_FIELD}}.y;
                        if (encodedAux <= -AuxStateMarker)
                        {
                            int oldPacked = (int)(-encodedAux - AuxStateMarker);
                            packedAux |= oldPacked & AuxSpeedRunApplied;
                        }
                        self.{{SCENE_STATE_FIELD}} = new Vector2(
                            (float)(state & 0xFFFFFF),
                            -AuxStateMarker - (float)packedAux);
                    }
                }
                catch (Exception) { }
            }

            int mask = state & EspMask;

            Matrix4x4 savedMatrix = GUI.matrix;
            Color savedColor = GUI.color;
            try
            {
                GUI.matrix = Matrix4x4.identity;
                Texture2D pixel = Texture2D.whiteTexture;
                if (pixel != null && screenWidth > 0 && screenHeight > 0)
                {
                    Camera camera = Camera.main;
                    Player localPlayer = GameFacade.CurrentLocalPlayer();
                    Transform localRoot = localPlayer == null ? null : localPlayer.RootTransform;

                    // ═══════════════════════════════════════════════
                    //  VÒNG FOV — 128 đoạn tròn đều
                    // ═══════════════════════════════════════════════
                    if ((state & AimSystemEnabled) != 0 && (state & AimFovHide) == 0)
                    {
                        float fdEnc2 = self.{{SCENE_STATE_FIELD}}.y;
                        int fovPacked = fdEnc2 <= -AuxStateMarker ? (int)(-fdEnc2 - AuxStateMarker) : 0;
                        float fovRadius = (float)((fovPacked >> AuxFovRadiusShift) & 0xFF) * 2f;
                        if (fovRadius > 0f)
                        {
                            bool espOn = (state & EspColorEnabled) != 0;
                            Color _cfov = espOn
                                ? new Color(
                                    ((int)PlayerPrefs.GetFloat("esp_cd",16777215f) & 0xF) / 15f,
                                    (((int)PlayerPrefs.GetFloat("esp_cd",16777215f) >> 4) & 0xF) / 15f,
                                    (((int)PlayerPrefs.GetFloat("esp_cd",16777215f) >> 8) & 0xF) / 15f,
                                    0.95f)
                                : new Color(0.1f, 1f, 0.2f, 0.95f);
                            GUI.color = _cfov;
                            float circleX = (float)screenWidth * 0.5f;
                            float circleY = (float)screenHeight * 0.5f;
                            const int fovSamples = 128;
                            const float fovStep = 0.049087385f;
                            const float fovThickness = 2.5f;
                            for (int sample = 0; sample < fovSamples; sample++)
                            {
                                float angleA = (float)sample * fovStep;
                                float angleB = (float)(sample + 1) * fovStep;
                                float startX = circleX + Mathf.Cos(angleA) * fovRadius;
                                float startY = circleY + Mathf.Sin(angleA) * fovRadius;
                                float endX = circleX + Mathf.Cos(angleB) * fovRadius;
                                float endY = circleY + Mathf.Sin(angleB) * fovRadius;
                                float segX = endX - startX, segY = endY - startY;
                                float segLen = Mathf.Sqrt(segX * segX + segY * segY);
                                float segAngle = Mathf.Atan2(segY, segX) * 57.29578f;
                                if (float.IsNaN(segLen) || float.IsInfinity(segLen) || segLen <= 0f) continue;
                                GUI.matrix = Matrix4x4.TRS(new Vector3(startX, startY, 0f),
                                    Quaternion.Euler(0f, 0f, segAngle), Vector3.one);
                                GUI.DrawTexture(new Rect(0f, -fovThickness * 0.5f, segLen + 1.2f, fovThickness), pixel);
                                GUI.matrix = Matrix4x4.identity;
                            }
                        }
                    }

                    // ═══════════════════════════════════════════════
                    //  ESP CHÍNH
                    // ═══════════════════════════════════════════════
                    if ((mask & EspMaster) != 0)
                    {
                        {{MATCH_TYPE}} match = GameFacade.CurrentMatch();
                        IList players = match == null ? null : match.{{MATCH_PLAYERS_METHOD}}();
                        GameObject localObject = localPlayer == null ? null : localPlayer.gameObject;
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
                            Color _cl = new Color(0f, 1f, 0.4f, 1f);
                            Color _cb2 = new Color(0f, 1f, 0.2f, 1f);
                            Color _ch = new Color(0.1f, 0.95f, 0.1f, 1f);
                            Color _cn = new Color(0.3f, 1f, 0.9f, 1f);
                            Color _cd = new Color(1f, 1f, 1f, 1f);
                            Color _csk = new Color(0.2f, 1f, 0.3f, 0.95f);

                            if (espOn)
                            {
                                _cl = new Color(
                                    ((int)PlayerPrefs.GetFloat("esp_ca", 65280f) & 0xF) / 15f,
                                    (((int)PlayerPrefs.GetFloat("esp_ca", 65280f) >> 4) & 0xF) / 15f,
                                    (((int)PlayerPrefs.GetFloat("esp_ca", 65280f) >> 8) & 0xF) / 15f,
                                    1f);
                                _cb2 = new Color(
                                    (((int)PlayerPrefs.GetFloat("esp_ca", 65280f) >> 12) & 0xF) / 15f,
                                    (((int)PlayerPrefs.GetFloat("esp_ca", 65280f) >> 16) & 0xF) / 15f,
                                    (((int)PlayerPrefs.GetFloat("esp_ca", 65280f) >> 20) & 0xF) / 15f,
                                    1f);
                            }

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

                                    Transform _sLegL = player.LegBoneLeft;
                                    Transform _sLegR = player.LegBoneRight;
                                    Vector3 _hipWorld;
                                    if (_sLegL != null && _sLegR != null)
                                    {
                                        Vector3 _lp = _sLegL.position, _rp = _sLegR.position;
                                        _hipWorld = new Vector3((_lp.x + _rp.x) * 0.5f,
                                            (_lp.y + _rp.y) * 0.5f, (_lp.z + _rp.z) * 0.5f);
                                    }
                                    else if (_sLegL != null) _hipWorld = _sLegL.position;
                                    else if (_sLegR != null) _hipWorld = _sLegR.position;
                                    else _hipWorld = root.position;

                                    float distance = Vector3.Distance(localRoot.position, _hipWorld);
                                    if (distance > 150f) continue;
                                    Vector3 feetScreen = camera.WorldToScreenPoint(
                                        new Vector3(_hipWorld.x, _hipWorld.y - 0.9f, _hipWorld.z));
                                    Vector3 headScreen = camera.WorldToScreenPoint(
                                        new Vector3(_hipWorld.x, _hipWorld.y + 0.9f, _hipWorld.z));
                                    if (feetScreen.z <= 0f || headScreen.z <= 0f) continue;
                                    if (float.IsNaN(feetScreen.x) || float.IsNaN(feetScreen.y)
                                        || float.IsNaN(headScreen.x) || float.IsNaN(headScreen.y)
                                        || float.IsInfinity(feetScreen.x) || float.IsInfinity(feetScreen.y)
                                        || float.IsInfinity(headScreen.x) || float.IsInfinity(headScreen.y)) continue;

                                    float height = Mathf.Abs(headScreen.y - feetScreen.y) * 1.0909091f;
                                    if (height < 4f) continue;
                                    float width = height * 0.55f;
                                    float left = headScreen.x - width * 0.5f;
                                    float top = (float)screenHeight - headScreen.y;
                                    if (float.IsNaN(left) || float.IsNaN(top)
                                        || left > (float)screenWidth || left + width < 0f
                                        || top > (float)screenHeight || top + height < 0f) continue;

                                    bool inFov = false;
                                    if (espFovRadius > 0f)
                                    {
                                        float dxF = left + width * 0.5f - (float)screenWidth * 0.5f;
                                        float dyF = top + height * 0.5f - (float)screenHeight * 0.5f;
                                        inFov = (dxF * dxF + dyF * dyF) <= (espFovRadius * espFovRadius);
                                    }
                                    Color lineEspColor = (inFov || dying) ? new Color(1f, 0.1f, 0.1f, 1f) : _cl;
                                    Color boxEspColor = (inFov || dying) ? new Color(1f, 0.1f, 0.1f, 1f) : _cb2;

                                    // ── BOX góc bo ──
                                    if ((mask & EspBox) != 0)
                                    {
                                        GUI.color = boxEspColor;
                                        float thick = 2f;
                                        float cornerLen = width * 0.25f;
                                        if (cornerLen > height * 0.15f) cornerLen = height * 0.15f;
                                        if (cornerLen < 6f) cornerLen = 6f;
                                        GUI.DrawTexture(new Rect(left, top, cornerLen, thick), pixel);
                                        GUI.DrawTexture(new Rect(left, top, thick, cornerLen), pixel);
                                        GUI.DrawTexture(new Rect(left + width - cornerLen, top, cornerLen, thick), pixel);
                                        GUI.DrawTexture(new Rect(left + width - thick, top, thick, cornerLen), pixel);
                                        GUI.DrawTexture(new Rect(left, top + height - thick, cornerLen, thick), pixel);
                                        GUI.DrawTexture(new Rect(left, top + height - cornerLen, thick, cornerLen), pixel);
                                        GUI.DrawTexture(new Rect(left + width - cornerLen, top + height - thick, cornerLen, thick), pixel);
                                        GUI.DrawTexture(new Rect(left + width - thick, top + height - cornerLen, thick, cornerLen), pixel);
                                    }

                                    // ── TRACER ──
                                    if ((mask & EspTracer) != 0)
                                    {
                                        float startX = (float)screenWidth * 0.5f;
                                        float startY = (float)screenHeight;
                                        float endX = left + width * 0.5f;
                                        float endY = top;
                                        float tracerX = endX - startX;
                                        float tracerY = endY - startY;
                                        float tracerLength = Mathf.Sqrt(tracerX * tracerX + tracerY * tracerY);
                                        float tracerAngle = Mathf.Atan2(tracerY, tracerX) * 57.29578f;
                                        GUI.matrix = Matrix4x4.TRS(new Vector3(startX, startY, 0f),
                                            Quaternion.Euler(0f, 0f, tracerAngle), Vector3.one);
                                        GUI.color = lineEspColor;
                                        GUI.DrawTexture(new Rect(0f, -1.2f, tracerLength, 2.4f), pixel);
                                        GUI.matrix = Matrix4x4.identity;
                                    }

                                    // ── MÁU ──
                                    if ((mask & EspHealth) != 0)
                                    {
                                        float healthRatio = maximumHealth > 0
                                            ? Mathf.Clamp01((float)health / (float)maximumHealth) : 0f;
                                        float healthX = left - 5f;
                                        GUI.color = new Color(0f, 0f, 0f, 0.85f);
                                        GUI.DrawTexture(new Rect(healthX - 1f, top - 1f, 5f, height + 2f), pixel);
                                        GUI.color = dying ? new Color(1f, 0f, 0f, 1f)
                                            : (espOn ? _ch
                                                : (healthRatio < 0.3f ? new Color(1f, 0f, 0f, 1f)
                                                    : (healthRatio <= 0.6f ? new Color(1f, 1f, 0f, 1f)
                                                        : new Color(0f, 1f, 0f, 1f))));
                                        GUI.DrawTexture(new Rect(healthX, top + height - height * healthRatio,
                                            3f, height * healthRatio), pixel);
                                    }

                                    // ── TÊN ──
                                    if ((mask & EspName) != 0)
                                    {
                                        string nickname = player.NickName;
                                        bool isBot = string.IsNullOrEmpty(nickname);
                                        if (isBot) nickname = "Bot";
                                        else if (nickname.Length > 12) nickname = nickname.Substring(0, 12);
                                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                                        GUI.color = dying ? new Color(1f, 0.4f, 0.4f, 1f) : _cn;
                                        GUI.Label(new Rect(left - 4f, top - 20f, width + 8f, 18f), nickname);
                                        GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                    }

                                    // ── KHOẢNG CÁCH ──
                                    if ((mask & EspDistance) != 0)
                                    {
                                        int distVal = (int)Mathf.Clamp(distance, 0f, 999f);
                                        string distStr = distVal.ToString() + "m";
                                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                                        GUI.color = _cd;
                                        GUI.Label(new Rect(left - 4f, top + height + 2f, width + 8f, 16f), distStr);
                                        GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                    }

                                    // ── XƯƠNG ──
                                    if ((state & EspSkeleton) != 0)
                                    {
                                        Transform skelNeck = player.NeckBone;
                                        Transform skelArmL = player.ArmBoneLeft;
                                        Transform skelArmR = player.ArmBoneRight;
                                        Transform skelHandL = player.HandBoneLeft;
                                        Transform skelHandR = player.HandBoneRight;
                                        Transform skelFootL = player.FootBoneLeft;
                                        Transform skelFootR = player.FootBoneRight;
                                        Vector3 spHead = camera.WorldToScreenPoint(head.position);
                                        Vector3 spNeck = skelNeck != null ? camera.WorldToScreenPoint(skelNeck.position) : new Vector3(0f, 0f, -1f);
                                        Vector3 spArmL = skelArmL != null ? camera.WorldToScreenPoint(skelArmL.position) : new Vector3(0f, 0f, -1f);
                                        Vector3 spArmR = skelArmR != null ? camera.WorldToScreenPoint(skelArmR.position) : new Vector3(0f, 0f, -1f);
                                        Vector3 spHandL = skelHandL != null ? camera.WorldToScreenPoint(skelHandL.position) : new Vector3(0f, 0f, -1f);
                                        Vector3 spHandR = skelHandR != null ? camera.WorldToScreenPoint(skelHandR.position) : new Vector3(0f, 0f, -1f);
                                        Vector3 spFootL = skelFootL != null ? camera.WorldToScreenPoint(skelFootL.position) : new Vector3(0f, 0f, -1f);
                                        Vector3 spFootR = skelFootR != null ? camera.WorldToScreenPoint(skelFootR.position) : new Vector3(0f, 0f, -1f);
                                        Vector3 spHip = camera.WorldToScreenPoint(_hipWorld);
                                        GUI.color = dying ? new Color(1f, 0f, 0f, 1f) : _csk;
                                        DrawBoneLine(spHead, spNeck, pixel, 1.6f);
                                        DrawBoneLine(spNeck, spArmL, pixel, 1.6f);
                                        DrawBoneLine(spNeck, spArmR, pixel, 1.6f);
                                        DrawBoneLine(spArmL, spHandL, pixel, 1.6f);
                                        DrawBoneLine(spArmR, spHandR, pixel, 1.6f);
                                        DrawBoneLine(spNeck, spHip, pixel, 1.6f);
                                        DrawBoneLine(spHip, spFootL, pixel, 1.6f);
                                        DrawBoneLine(spHip, spFootR, pixel, 1.6f);
                                    }

                                    enemyCount++;
                                }
                                catch (Exception) { }
                            }

                            // ── ĐẾM ĐỊCH ──
                            if ((state & EspEnemyCount) != 0)
                            {
                                string cntStr = "Người chơi : " + enemyCount;
                                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                                GUI.color = new Color(0f, 0f, 0f, 0.85f);
                                GUI.Label(new Rect((float)screenWidth * 0.5f - 202f, 38f, 404f, 40f), cntStr);
                                GUI.Label(new Rect((float)screenWidth * 0.5f - 198f, 42f, 404f, 40f), cntStr);
                                GUI.Label(new Rect((float)screenWidth * 0.5f - 200f, 40f, 404f, 40f), cntStr);
                                GUI.color = Color.white;
                                GUI.Label(new Rect((float)screenWidth * 0.5f - 200f, 40f, 400f, 40f), cntStr);
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                            }
                        }
                    }
                }
            }
            catch (Exception) { }

            // ═══════════════════════════════════════════════════════════
            //  HIỆU ỨNG FEATURES — Bắn nhanh / Chạy nhanh / Nhảy lùi
            // ═══════════════════════════════════════════════════════════
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
                        PlayerAttributes attrs = _lp.Attributes;

                        if (attrs != null)
                        {
                            bool speedRunOn = (auxState & AuxSpeedRun) != 0;
                            bool speedRunApplied = (auxState & AuxSpeedRunApplied) != 0;
                            if (speedRunOn)
                            {
                                attrs.SetSpecialRunSpeedScaleByKeyAndValue(
                                    PlayerAttributes.{{SPEED_TYPE}}.BuffSystem,
                                    SpeedRunningKey, 3f);
                                auxState |= AuxSpeedRunApplied;
                            }
                            else if (speedRunApplied)
                            {
                                attrs.RemoveSpecialRunSpeedScaleByKey(
                                    PlayerAttributes.{{SPEED_TYPE}}.BuffSystem,
                                    SpeedRunningKey);
                                auxState &= ~AuxSpeedRunApplied;
                            }

                            bool fastFireOn = (auxState & AuxFastFire) != 0;
                            if (fastFireOn) attrs.FireIntervalScale = 0.5f;
                            else if (attrs.FireIntervalScale < 0.9f) attrs.FireIntervalScale = 1.0f;
                        }

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

            // ✅ GỌI MENU NGAY TRONG DRAW — không cần hook ngoài
            DrawMenu(self);
        }

                // ═══════════════════════════════════════════════════════════
        //  MENU — 3 tab tiếng Việt
        // ═══════════════════════════════════════════════════════════
        public static void DrawMenu(SceneEditBoxSelectTool self)
        {
            if (self == null) return;
            GameObject driverObject = self.gameObject;
            if (driverObject == null || driverObject.name != "__esp_driver") return;

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            int state = (int)self.{{SCENE_STATE_FIELD}}.x;
            float encodedAux = self.{{SCENE_STATE_FIELD}}.y;
            int mAux = encodedAux <= -AuxStateMarker ? (int)(-encodedAux - AuxStateMarker) : 0;

            try
            {
                Texture2D _mpx = Texture2D.whiteTexture;
                if (_mpx == null) return;

                Vector2 _mPtr = Vector2.zero;
                bool _mDown = false, _mDrag = false, _mUp = false;
                if (Input.touchCount > 0)
                {
                    Touch _mt = Input.GetTouch(0);
                    _mPtr = new Vector2(_mt.position.x, (float)screenHeight - _mt.position.y);
                    _mDown = _mt.phase == TouchPhase.Began;
                    _mDrag = _mt.phase == TouchPhase.Moved || _mt.phase == TouchPhase.Stationary;
                    _mUp = _mt.phase == TouchPhase.Ended || _mt.phase == TouchPhase.Canceled;
                }
                else
                {
                    Vector2 _mm = Input.mousePosition;
                    _mPtr = new Vector2(_mm.x, (float)screenHeight - _mm.y);
                    _mDown = Input.GetMouseButtonDown(0);
                    _mDrag = Input.GetMouseButton(0);
                    _mUp = Input.GetMouseButtonUp(0);
                }

                // ═══════════════════════════════════════════════════
                //  NÚT TRÒN VN — góc phải
                // ═══════════════════════════════════════════════════
                GameObject _mFab = GameObject.Find("__esp_menu_fab");
                if (_mFab == null)
                {
                    _mFab = new GameObject("__esp_menu_fab");
                    _mFab.transform.position = new Vector3((float)screenWidth - 88f, 100f, 0f);
                    UnityEngine.Object.DontDestroyOnLoad(_mFab);
                }
                GameObject _mOpenObj = GameObject.Find("__esp_menu_open");
                bool _mOpen = _mOpenObj != null;

                float _fabW = 68f, _fabH = 68f;
                float _fabX = Mathf.Clamp(_mFab.transform.position.x, 0f, Mathf.Max(0f, (float)screenWidth - _fabW));
                float _fabY = Mathf.Clamp(_mFab.transform.position.y, 0f, Mathf.Max(0f, (float)screenHeight - _fabH));
                Rect _fabRect = new Rect(_fabX, _fabY, _fabW, _fabH);

                GameObject _mFabDrag = GameObject.Find("__esp_menu_fab_drag");
                if (_mDown && _fabRect.Contains(_mPtr) && !_mOpen)
                {
                    if (_mFabDrag != null) { _mFabDrag.name = "__esp_menu_fab_drag_old"; UnityEngine.Object.Destroy(_mFabDrag); }
                    _mFabDrag = new GameObject("__esp_menu_fab_drag");
                    _mFabDrag.transform.position = new Vector3(_mPtr.x - _fabRect.x, _mPtr.y - _fabRect.y, 0f);
                    _mFabDrag.transform.localScale = new Vector3(_mPtr.x, _mPtr.y, 0f);
                    UnityEngine.Object.DontDestroyOnLoad(_mFabDrag);
                }
                if (_mFabDrag != null && _mDrag)
                {
                    Vector3 _fp = _mFabDrag.transform.localScale;
                    float _fmx = _mPtr.x - _fp.x, _fmy = _mPtr.y - _fp.y;
                    if (_fp.z < 0.5f && _fmx * _fmx + _fmy * _fmy >= 36f)
                    { _fp.z = 1f; _mFabDrag.transform.localScale = _fp; }
                    if (_fp.z >= 0.5f)
                    {
                        _fabX = Mathf.Clamp(_mPtr.x - _mFabDrag.transform.position.x, 0f, Mathf.Max(0f, (float)screenWidth - _fabW));
                        _fabY = Mathf.Clamp(_mPtr.y - _mFabDrag.transform.position.y, 0f, Mathf.Max(0f, (float)screenHeight - _fabH));
                        _mFab.transform.position = new Vector3(_fabX, _fabY, 0f);
                        _fabRect = new Rect(_fabX, _fabY, _fabW, _fabH);
                    }
                }
                if (_mFabDrag != null && _mUp)
                {
                    bool _fWasDragged = _mFabDrag.transform.localScale.z >= 0.5f;
                    _mFabDrag.name = "__esp_menu_fab_drag_done"; UnityEngine.Object.Destroy(_mFabDrag);
                    if (!_fWasDragged && _fabRect.Contains(_mPtr))
                    {
                        if (_mOpen)
                        {
                            GameObject _mo = GameObject.Find("__esp_menu_open");
                            if (_mo != null) { _mo.name = "__esp_menu_open_off"; UnityEngine.Object.Destroy(_mo); }
                            _mOpen = false;
                        }
                        else
                        {
                            GameObject _mo = new GameObject("__esp_menu_open");
                            UnityEngine.Object.DontDestroyOnLoad(_mo);
                            _mOpen = true;
                        }
                    }
                }

                // ── Vẽ nút tròn VN ──
                float _fcx = _fabRect.x + _fabRect.width * 0.5f;
                float _fcy = _fabRect.y + _fabRect.height * 0.5f;
                float _fr = _fabRect.width * 0.5f;

                // Viền ngoài
                GUI.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
                for (int _si = 0; _si < 48; _si++)
                {
                    float _a1 = (float)_si * 0.1308997f;
                    float _a2 = (float)(_si + 1) * 0.1308997f;
                    float _sx1 = _fcx + Mathf.Cos(_a1) * _fr, _sy1 = _fcy + Mathf.Sin(_a1) * _fr;
                    float _sx2 = _fcx + Mathf.Cos(_a2) * _fr, _sy2 = _fcy + Mathf.Sin(_a2) * _fr;
                    float _sdx = _sx2 - _sx1, _sdy = _sy2 - _sy1;
                    float _slen = Mathf.Sqrt(_sdx * _sdx + _sdy * _sdy);
                    float _sang = Mathf.Atan2(_sdy, _sdx) * 57.29578f;
                    GUI.matrix = Matrix4x4.TRS(new Vector3(_sx1, _sy1, 0f),
                        Quaternion.Euler(0f, 0f, _sang), Vector3.one);
                    GUI.DrawTexture(new Rect(0f, -3.5f, _slen + 1f, 7f), _mpx);
                    GUI.matrix = Matrix4x4.identity;
                }
                // Vòng đỏ
                GUI.color = _mOpen ? new Color(0.95f, 0.1f, 0.15f, 1f) : new Color(0.88f, 0.10f, 0.10f, 1f);
                for (int _si = 0; _si < 48; _si++)
                {
                    float _a1 = (float)_si * 0.1308997f;
                    float _a2 = (float)(_si + 1) * 0.1308997f;
                    float _rr = _fr - 3.5f;
                    float _sx1 = _fcx + Mathf.Cos(_a1) * _rr, _sy1 = _fcy + Mathf.Sin(_a1) * _rr;
                    float _sx2 = _fcx + Mathf.Cos(_a2) * _rr, _sy2 = _fcy + Mathf.Sin(_a2) * _rr;
                    float _sdx = _sx2 - _sx1, _sdy = _sy2 - _sy1;
                    float _slen = Mathf.Sqrt(_sdx * _sdx + _sdy * _sdy);
                    float _sang = Mathf.Atan2(_sdy, _sdx) * 57.29578f;
                    GUI.matrix = Matrix4x4.TRS(new Vector3(_sx1, _sy1, 0f),
                        Quaternion.Euler(0f, 0f, _sang), Vector3.one);
                    GUI.DrawTexture(new Rect(0f, -2f, _slen + 1f, 4f), _mpx);
                    GUI.matrix = Matrix4x4.identity;
                }
                GUI.color = Color.white;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(_fabRect.x, _fabRect.y + _fr - 12f, _fabRect.width, 24f), "VN");
                GUI.skin.label.alignment = TextAnchor.UpperLeft;

                // ═══════════════════════════════════════════════════
                //  PANEL MENU
                // ═══════════════════════════════════════════════════
                if (_mOpen)
                {
                    float _pw = Mathf.Min(760f, (float)screenWidth - 30f);
                    float _ph = Mathf.Min(520f, (float)screenHeight - 60f);
                    float _px0 = ((float)screenWidth - _pw) * 0.5f;
                    float _py0 = ((float)screenHeight - _ph) * 0.5f;

                    GUI.color = new Color(0f, 0f, 0f, 0.55f);
                    GUI.DrawTexture(new Rect(0f, 0f, (float)screenWidth, (float)screenHeight), _mpx);

                    GUI.color = new Color(0.98f, 0.98f, 0.99f, 1f);
                    GUI.DrawTexture(new Rect(_px0, _py0, _pw, _ph), _mpx);

                    // Header đỏ + chữ VN TOOL
                    GUI.color = new Color(0.88f, 0.10f, 0.10f, 1f);
                    GUI.DrawTexture(new Rect(_px0, _py0, _pw, 46f), _mpx);
                    GUI.color = Color.white;
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUI.Label(new Rect(_px0 + 16f, _py0, _pw - 60f, 46f), "   VN TOOL");
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;

                    // Nút X
                    Rect _xRect = new Rect(_px0 + _pw - 48f, _py0 + 8f, 36f, 30f);
                    GUI.color = new Color(0.75f, 0.08f, 0.08f, 1f);
                    GUI.DrawTexture(_xRect, _mpx);
                    GUI.color = Color.white;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(_xRect, "X");
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    if (_mUp && _xRect.Contains(_mPtr))
                    {
                        GameObject _mo = GameObject.Find("__esp_menu_open");
                        if (_mo != null) { _mo.name = "__esp_menu_open_off"; UnityEngine.Object.Destroy(_mo); }
                        _mOpen = false;
                    }

                    // ═══════════════════════════════════════════
                    //  3 TAB tiếng Việt
                    // ═══════════════════════════════════════════
                    string[] _tabNames = new string[3];
                    _tabNames[0] = "NGẮM";
                    _tabNames[1] = "HIỂN THỊ";
                    _tabNames[2] = "CHỨC NĂNG";

                    int _activeTab = 0;
                    GameObject _tabObj = GameObject.Find("__esp_menu_tab");
                    if (_tabObj != null) _activeTab = (int)_tabObj.transform.position.x;
                    if (_activeTab < 0) _activeTab = 0;
                    if (_activeTab > 2) _activeTab = 2;

                    float _tabW = 170f;
                    GUI.color = new Color(0.94f, 0.94f, 0.96f, 1f);
                    GUI.DrawTexture(new Rect(_px0, _py0 + 46f, _tabW, _ph - 46f), _mpx);

                    for (int _ti = 0; _ti < 3; _ti++)
                    {
                        float _ty = _py0 + 46f + 14f + _ti * 64f;
                        Rect _tabRect = new Rect(_px0 + 12f, _ty, _tabW - 24f, 52f);
                        bool _sel = (_activeTab == _ti);
                        GUI.color = _sel ? new Color(0.88f, 0.10f, 0.10f, 1f) : new Color(0.86f, 0.86f, 0.90f, 1f);
                        GUI.DrawTexture(_tabRect, _mpx);
                        GUI.color = _sel ? Color.white : new Color(0.15f, 0.15f, 0.18f, 1f);
                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                        GUI.Label(_tabRect, _tabNames[_ti]);
                        GUI.skin.label.alignment = TextAnchor.UpperLeft;
                        if (_mUp && _tabRect.Contains(_mPtr))
                        {
                            if (_tabObj == null)
                            {
                                _tabObj = new GameObject("__esp_menu_tab");
                                UnityEngine.Object.DontDestroyOnLoad(_tabObj);
                            }
                            _tabObj.transform.position = new Vector3((float)_ti, 0f, 0f);
                            _activeTab = _ti;
                        }
                    }

                    float _cx0 = _px0 + _tabW + 16f;
                    float _cw = _pw - _tabW - 32f;
                    float _rowY = _py0 + 60f;
                    float _rowH = 50f;

                    // ═══════════════════════════════════════════
                    //  TAB 0: NGẮM
                    // ═══════════════════════════════════════════
                    if (_activeTab == 0)
                    {
                        // Aim bot
                        {
                            bool _v = (state & AimEnabled) != 0;
                            Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                            DrawRowBg(_mpx, _row);
                            DrawRowText(_mpx, _row, "Bật Ngắm Tự Động");
                            if (DrawToggle(_mpx, _row, _v, _mUp, _mPtr))
                                state = _v ? (state & ~AimEnabled) : (state | AimEnabled);
                            _rowY += _rowH + 4f;
                        }
                        // Aim Collider
                        {
                            bool _v = (state & AimSystemEnabled) != 0;
                            Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                            DrawRowBg(_mpx, _row);
                            DrawRowText(_mpx, _row, "Vòng FOV Ngắm");
                            if (DrawToggle(_mpx, _row, _v, _mUp, _mPtr))
                                state = _v ? (state & ~AimSystemEnabled) : (state | AimSystemEnabled);
                            _rowY += _rowH + 4f;
                        }
                        // Bỏ qua địch gục
                        {
                            bool _v = (state & AimSkipDowned) != 0;
                            Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                            DrawRowBg(_mpx, _row);
                            DrawRowText(_mpx, _row, "Bỏ Qua Địch Gục");
                            if (DrawToggle(_mpx, _row, _v, _mUp, _mPtr))
                                state = _v ? (state & ~AimSkipDowned) : (state | AimSkipDowned);
                            _rowY += _rowH + 4f;
                        }
                        // No Recoil
                        {
                            bool _v = (state & NoRecoil) != 0;
                            Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                            DrawRowBg(_mpx, _row);
                            DrawRowText(_mpx, _row, "Đạn Thẳng (No Recoil)");
                            if (DrawToggle(_mpx, _row, _v, _mUp, _mPtr))
                                state = _v ? (state & ~NoRecoil) : (state | NoRecoil);
                            _rowY += _rowH + 4f;
                        }
                        // Chế độ ngắm
                        {
                            Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                            DrawRowBg(_mpx, _row);
                            int _mode = (state & AimModeMask) >> AimModeShift;
                            DrawRowText(_mpx, _row, "Vị Trí Ngắm");
                            float _bw = (_row.width * 0.5f - 20f) / 3f;
                            string[] _opts = new string[3];
                            _opts[0] = "Người"; _opts[1] = "Đầu"; _opts[2] = "Hỗn Hợp";
                            for (int _i = 0; _i < 3; _i++)
                            {
                                Rect _btn = new Rect(_row.x + _row.width - 14f - (3 - _i) * _bw,
                                    _row.y + (_rowH - 30f) * 0.5f, _bw - 4f, 30f);
                                DrawRoundRect(_mpx, _btn, 6f,
                                    _i == _mode ? new Color(0.88f, 0.10f, 0.10f, 1f) : new Color(0.86f, 0.86f, 0.90f, 1f));
                                GUI.color = _i == _mode ? Color.white : new Color(0.15f, 0.15f, 0.18f, 1f);
                                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                                GUI.Label(_btn, _opts[_i]);
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                if (_mUp && _btn.Contains(_mPtr))
                                    state = (state & ~AimModeMask) | ((_i & 3) << AimModeShift);
                            }
                            _rowY += _rowH + 4f;
                        }
                        // Tỉ lệ aim đầu
                        {
                            Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                            DrawRowBg(_mpx, _row);
                            int _hr = (state & HeadRateMask) >> HeadRateShift;
                            DrawRowText(_mpx, _row, "Tỉ Lệ Aim Đầu: " + (_hr * 25) + "%");
                            Rect _tr = new Rect(_row.x + _row.width - 220f, _row.y + (_rowH - 12f) * 0.5f, 200f, 12f);
                            DrawSlider(_mpx, _tr, (float)_hr / 4f);
                            if (_mDrag && _mPtr.y >= _row.y && _mPtr.y <= _row.y + _row.height
                                && _mPtr.x >= _tr.x - 12f && _mPtr.x <= _tr.x + _tr.width + 12f)
                            {
                                float _nr = Mathf.Clamp01((_mPtr.x - _tr.x) / _tr.width);
                                int _nh = (int)(_nr * 4f + 0.5f);
                                if (_nh < 0) _nh = 0; if (_nh > 4) _nh = 4;
                                state = (state & ~HeadRateMask) | ((_nh & 7) << HeadRateShift);
                            }
                            _rowY += _rowH + 4f;
                        }
                        // FOV px
                        {
                            Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                            DrawRowBg(_mpx, _row);
                            int _fovPx = (mAux >> AuxFovRadiusShift) & 0xFF;
                            DrawRowText(_mpx, _row, "Bán Kính FOV: " + (_fovPx * 2));
                            Rect _tr = new Rect(_row.x + _row.width - 220f, _row.y + (_rowH - 12f) * 0.5f, 200f, 12f);
                            DrawSlider(_mpx, _tr, (float)_fovPx / 255f);
                            if (_mDrag && _mPtr.y >= _row.y && _mPtr.y <= _row.y + _row.height
                                && _mPtr.x >= _tr.x - 12f && _mPtr.x <= _tr.x + _tr.width + 12f)
                            {
                                float _nr = Mathf.Clamp01((_mPtr.x - _tr.x) / _tr.width);
                                int _nv = (int)(_nr * 255f + 0.5f);
                                mAux = (mAux & ~(0xFF << AuxFovRadiusShift)) | ((_nv & 0xFF) << AuxFovRadiusShift);
                            }
                            _rowY += _rowH + 4f;
                        }
                        // Silent FOV
                        {
                            Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                            DrawRowBg(_mpx, _row);
                            int _silPx = (mAux >> AuxSilentFovShift) & 0xFF;
                            DrawRowText(_mpx, _row, "Bán Kính Đạn Đuổi: " + (_silPx * 2));
                            Rect _tr = new Rect(_row.x + _row.width - 220f, _row.y + (_rowH - 12f) * 0.5f, 200f, 12f);
                            DrawSlider(_mpx, _tr, (float)_silPx / 255f);
                            if (_mDrag && _mPtr.y >= _row.y && _mPtr.y <= _row.y + _row.height
                                && _mPtr.x >= _tr.x - 12f && _mPtr.x <= _tr.x + _tr.width + 12f)
                            {
                                float _nr = Mathf.Clamp01((_mPtr.x - _tr.x) / _tr.width);
                                int _nv = (int)(_nr * 255f + 0.5f);
                                mAux = (mAux & ~(0xFF << AuxSilentFovShift)) | ((_nv & 0xFF) << AuxSilentFovShift);
                            }
                            _rowY += _rowH + 4f;
                        }
                    }

                    // ═══════════════════════════════════════════
                    //  TAB 1: HIỂN THỊ
                    // ═══════════════════════════════════════════
                    else if (_activeTab == 1)
                    {
                        int[] _espBits = new int[10];
                        _espBits[0] = EspMaster;
                        _espBits[1] = EspBox;
                        _espBits[2] = EspTracer;
                        _espBits[3] = EspHealth;
                        _espBits[4] = EspName;
                        _espBits[5] = EspDistance;
                        _espBits[6] = EspSkeleton;
                        _espBits[7] = EspFov;
                        _espBits[8] = EspEnemyCount;
                        _espBits[9] = EspColorEnabled;

                        string[] _espNames = new string[10];
                        _espNames[0] = "Bật Hiển Thị";
                        _espNames[1] = "Khung Hộp";
                        _espNames[2] = "Đường Kẻ";
                        _espNames[3] = "Thanh Máu";
                        _espNames[4] = "Tên Địch";
                        _espNames[5] = "Khoảng Cách";
                        _espNames[6] = "Khung Xương";
                        _espNames[7] = "Vòng FOV";
                        _espNames[8] = "Đếm Địch";
                        _espNames[9] = "Màu Tùy Chỉnh";

                        for (int _i = 0; _i < 10; _i++)
                        {
                            bool _v = (state & _espBits[_i]) != 0;
                            Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                            DrawRowBg(_mpx, _row);
                            DrawRowText(_mpx, _row, _espNames[_i]);
                            if (DrawToggle(_mpx, _row, _v, _mUp, _mPtr))
                                state = _v ? (state & ~_espBits[_i]) : (state | _espBits[_i]);
                            _rowY += _rowH + 4f;
                        }
                    }

                    // ═══════════════════════════════════════════
                    //  TAB 2: CHỨC NĂNG
                    // ═══════════════════════════════════════════
                    else if (_activeTab == 2)
                    {
                        int[] _featBits = new int[3];
                        _featBits[0] = AuxFastFire;
                        _featBits[1] = AuxSpeedRun;
                        _featBits[2] = AuxBackJump;

                        string[] _featNames = new string[3];
                        _featNames[0] = "Bắn Nhanh";
                        _featNames[1] = "Chạy Nhanh";
                        _featNames[2] = "Nhảy Lùi";

                        for (int _i = 0; _i < 3; _i++)
                        {
                            bool _v = (mAux & _featBits[_i]) != 0;
                            Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                            DrawRowBg(_mpx, _row);
                            DrawRowText(_mpx, _row, _featNames[_i]);
                            if (DrawToggle(_mpx, _row, _v, _mUp, _mPtr))
                                mAux = _v ? (mAux & ~_featBits[_i]) : (mAux | _featBits[_i]);
                            _rowY += _rowH + 4f;
                        }
                    }

                    // Lưu state
                    self.{{SCENE_STATE_FIELD}} = new Vector2(
                        (float)(state & 0xFFFFFF),
                        -AuxStateMarker - (float)mAux);
                }
            }
            catch (Exception) { }
        }

                // ═══════════════════════════════════════════════════════════
        //  HELPERS UI
        // ═══════════════════════════════════════════════════════════
        private static void DrawRowBg(Texture2D px, Rect r)
        {
            GUI.color = new Color(0.97f, 0.97f, 0.99f, 1f);
            GUI.DrawTexture(r, px);
            GUI.color = new Color(0.85f, 0.85f, 0.88f, 1f);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - 1f, r.width, 1f), px);
        }

        private static void DrawRowText(Texture2D px, Rect r, string text)
        {
            GUI.color = new Color(0.12f, 0.12f, 0.15f, 1f);
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(r.x + 14f, r.y, r.width - 100f, r.height), text);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }

        private static bool DrawToggle(Texture2D px, Rect row, bool value, bool mUp, Vector2 mPtr)
        {
            float _tkW = 64f, _tkH = 34f;
            Rect _tk = new Rect(row.x + row.width - _tkW - 14f, row.y + (row.height - _tkH) * 0.5f, _tkW, _tkH);
            DrawRoundRect(px, _tk, _tkH * 0.5f,
                value ? new Color(0.20f, 0.80f, 0.35f, 1f) : new Color(0.75f, 0.75f, 0.78f, 1f));
            float _thumbR = _tkH - 6f;
            float _thumbX = value ? (_tk.x + _tkW - _thumbR - 3f) : (_tk.x + 3f);
            Rect _thumb = new Rect(_thumbX, _tk.y + 3f, _thumbR, _thumbR);
            DrawRoundRect(px, _thumb, _thumbR * 0.5f, Color.white);
            return (mUp && _tk.Contains(mPtr));
        }

        private static void DrawSlider(Texture2D px, Rect tr, float ratio)
        {
            DrawRoundRect(px, tr, 6f, new Color(0.80f, 0.80f, 0.83f, 1f));
            ratio = Mathf.Clamp01(ratio);
            DrawRoundRect(px, new Rect(tr.x, tr.y, tr.width * ratio, tr.height), 6f,
                new Color(0.88f, 0.10f, 0.10f, 1f));
            float _thumbR = 22f;
            Rect _thumb = new Rect(tr.x + tr.width * ratio - _thumbR * 0.5f,
                tr.y + tr.height * 0.5f - _thumbR * 0.5f, _thumbR, _thumbR);
            DrawRoundRect(px, _thumb, _thumbR * 0.5f, Color.white);
        }

        private static void DrawRoundRect(Texture2D px, Rect r, float radius, Color color)
        {
            GUI.color = color;
            float _r = Mathf.Min(radius, Mathf.Min(r.width, r.height) * 0.5f);
            GUI.DrawTexture(new Rect(r.x + _r, r.y, r.width - _r * 2f, r.height), px);
            GUI.DrawTexture(new Rect(r.x, r.y + _r, _r, r.height - _r * 2f), px);
            GUI.DrawTexture(new Rect(r.x + r.width - _r, r.y + _r, _r, r.height - _r * 2f), px);
            int arcSteps = 8;
            float arcStep = 1.570796f / arcSteps;
            for (int i = 0; i < arcSteps; i++)
            {
                float a1 = (float)i * arcStep;
                float a2 = (float)(i + 1) * arcStep;
                DrawArcSeg(px, r.x + _r, r.y + _r, _r, a1 + 3.14159f, a2 + 3.14159f);
                DrawArcSeg(px, r.x + r.width - _r, r.y + _r, _r, a1 + 4.71238f, a2 + 4.71238f);
                DrawArcSeg(px, r.x + _r, r.y + r.height - _r, _r, a1 + 1.570796f, a2 + 1.570796f);
                DrawArcSeg(px, r.x + r.width - _r, r.y + r.height - _r, _r, a1, a2);
            }
        }

        private static void DrawArcSeg(Texture2D px, float cx, float cy, float r, float a1, float a2)
        {
            float x1 = cx + Mathf.Cos(a1) * r;
            float y1 = cy + Mathf.Sin(a1) * r;
            float x2 = cx + Mathf.Cos(a2) * r;
            float y2 = cy + Mathf.Sin(a2) * r;
            float dx = x2 - x1, dy = y2 - y1;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 0.5f) return;
            float ang = Mathf.Atan2(dy, dx) * 57.29578f;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(x1, y1, 0f), Quaternion.Euler(0f, 0f, ang), Vector3.one);
            GUI.DrawTexture(new Rect(0f, -1.2f, len + 0.5f, 2.4f), px);
            GUI.matrix = old;
        }

        private static void DrawBoneLine(Vector3 a, Vector3 b, Texture2D pixel, float thick)
        {
            if (a.z <= 0f || b.z <= 0f) return;
            float screenHeight = (float)Screen.height;
            float x1 = a.x, y1 = screenHeight - a.y;
            float x2 = b.x, y2 = screenHeight - b.y;
            float dx = x2 - x1, dy = y2 - y1;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 1f || float.IsNaN(len) || float.IsInfinity(len)) return;
            float ang = Mathf.Atan2(dy, dx) * 57.29578f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(x1, y1, 0f), Quaternion.Euler(0f, 0f, ang), Vector3.one);
            GUI.DrawTexture(new Rect(0f, -thick * 0.5f, len, thick), pixel);
            GUI.matrix = Matrix4x4.identity;
        }

        // ═══════════════════════════════════════════════════════════
        //  SILENT AIM
        // ═══════════════════════════════════════════════════════════
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

                bool silentAimOn = (state & AimEnabled) != 0;
                bool aimFovOn = (state & AimSystemEnabled) != 0;
                if (!silentAimOn && !aimFovOn) return info;

                int silentFovPx = 0;
                int aimFovPixels = 0;
                float fdEnc = menu == null ? 0f : menu.{{SCENE_STATE_FIELD}}.y;
                if (fdEnc <= -AuxStateMarker)
                {
                    int fullAux = (int)(-fdEnc - AuxStateMarker);
                    silentFovPx = ((fullAux >> AuxSilentFovShift) & 0xFF) * 2;
                    aimFovPixels = ((fullAux >> AuxFovRadiusShift) & 0xFF) * 2;
                }

                bool aimAtHead;
                int fovFilterPx;
                if (silentAimOn) { aimAtHead = true; fovFilterPx = silentFovPx; }
                else
                {
                    int aimModeFov = (state & AimModeMask) >> AimModeShift;
                    int headRateFov = ((state & HeadRateMask) >> HeadRateShift) * 25;
                    aimAtHead = aimModeFov == 1 || (aimModeFov == 2 && UnityEngine.Random.Range(0, 100) < headRateFov);
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
                        || ((candidate.IsDieing || candidate.IsKnockedDownBleed) && (state & AimSkipDowned) != 0)) continue;

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
                        || float.IsInfinity(screen.x) || float.IsInfinity(screen.y)) continue;
                    float dx = screen.x - (float)Screen.width * 0.5f;
                    float dy = screen.y - (float)Screen.height * 0.5f;
                    float score = dx * dx + dy * dy;
                    if (float.IsNaN(score) || float.IsInfinity(score) || score >= bestScore) continue;
                    if (fovFilterPx > 0 && score > (float)(fovFilterPx * fovFilterPx)) continue;

                    bestScore = score;
                    bestCollider = hitCollider;
                    bestPosition = hitPosition;
                }

                if (bestCollider != null && silentAimOn)
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
                return info;
            }
            catch (Exception) { return info; }
        }

        // ═══════════════════════════════════════════════════════════
        //  AIM SYSTEM
        // ═══════════════════════════════════════════════════════════
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
                bool fovAimAtHead = aimModeFov == 1 || (aimModeFov == 2 && UnityEngine.Random.Range(0, 100) < headRateFov);
                Collider targetCollider = null;
                if (fovAimAtHead) targetCollider = self.HeadCollider;
                else
                {
                    Transform neck = self.NeckBone;
                    IList fireColliders = self.FireColliders;
                    float nearestDistance = 10000f;
                    if (neck != null && fireColliders != null)
                    {
                        for (int index = 0; index < fireColliders.Count; index++)
                        {
                            Collider cc = fireColliders[index] as Collider;
                            if (cc == null) continue;
                            float d = Vector3.Distance(cc.transform.position, neck.position);
                            if (d < nearestDistance) { nearestDistance = d; targetCollider = cc; }
                        }
                    }
                    if (targetCollider == null && neck != null) targetCollider = (Collider)neck.GetComponent(typeof(Collider));
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
                        Vector3 sp = camAim.WorldToScreenPoint(targetCollider.transform.position);
                        if (sp.z > 0f)
                        {
                            float dxAim = sp.x - (float)Screen.width * 0.5f;
                            float dyAim = sp.y - (float)Screen.height * 0.5f;
                            if (dxAim * dxAim + dyAim * dyAim > fovRadiusAim * fovRadiusAim) return original;
                        }
                    }
                }
                self.EspLockedAimingCollider = targetCollider;
                return original;
            }
            catch (Exception) { return original; }
        }

        // ═══════════════════════════════════════════════════════════
        //  SCATTER RATE
        // ═══════════════════════════════════════════════════════════
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
