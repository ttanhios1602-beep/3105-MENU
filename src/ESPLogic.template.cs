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
        private const int GhostScaleShift = 12;

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

        private const float TwoPi = 6.2831853f;
        private const float Rad2Deg = 57.29578f;

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
                    Camera camera = Camera.main;
                    Transform legacyFov = camera == null ? null : camera.transform.Find("__esp_fov");
                    if (legacyFov != null) UnityEngine.Object.Destroy(legacyFov.gameObject);
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
                        int initialState = StateInitialized | EspMask | DefaultStateBits;
                        menu.{{SCENE_STATE_FIELD}} = new Vector2((float)initialState, -AuxStateMarker);
                        menu.{{SCENE_MENU_FIELD}} = false;
                    }
                }
            }
            catch (Exception) { stealthState = false; }
            return stealthState;
        }

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
                state = StateInitialized | EspMask | DefaultStateBits;
                self.{{SCENE_STATE_FIELD}} = new Vector2((float)(state & 0xFFFFFF), -AuxStateMarker);
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

            if (now - self.{{SCENE_POSITION_FIELD}}.x > 1.0f)
            {
                self.{{SCENE_POSITION_FIELD}} = new Vector2(now, 0f);
                try
                {
                    string cfgPath = Application.persistentDataPath + "/contentcache/Compulsory/ios/gameassetbundles/ingame/.pdata";
                    if (File.Exists(cfgPath))
                    {
                        byte[] cfgBytes = File.ReadAllBytes(cfgPath);
                        int newState = StateInitialized | EspMask | DefaultStateBits;
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
                        int _r8 = cfgBytes.Length >= 9 ? cfgBytes[8] : 0;
                        PlayerPrefs.SetFloat("esp_r8", (float)_r8);
                        int _gsIdx = cfgBytes.Length >= 10 ? (cfgBytes[9] & 7) : 0;
                        PlayerPrefs.SetFloat("esp_gs", (float)_gsIdx);
                        int colorPacked = 0;
                        if (cfgBytes.Length >= 11) colorPacked |= (cfgBytes[10] & 0xFF) << 8;
                        self.{{SCENE_POSITION_FIELD}} = new Vector2(now, (float)colorPacked);
                        if (cfgBytes.Length >= 32)
                        {
                            PlayerPrefs.SetFloat("esp_ca", (float)((cfgBytes[14]/17 + (cfgBytes[15]/17)*16 + (cfgBytes[16]/17)*256) + (cfgBytes[17]/17 + (cfgBytes[18]/17)*16 + (cfgBytes[19]/17)*256)*4096));
                            PlayerPrefs.SetFloat("esp_cb", (float)((cfgBytes[20]/17 + (cfgBytes[21]/17)*16 + (cfgBytes[22]/17)*256) + (cfgBytes[23]/17 + (cfgBytes[24]/17)*16 + (cfgBytes[25]/17)*256)*4096));
                            PlayerPrefs.SetFloat("esp_cc", (float)((cfgBytes[26]/17 + (cfgBytes[27]/17)*16 + (cfgBytes[28]/17)*256) + (cfgBytes[29]/17 + (cfgBytes[30]/17)*16 + (cfgBytes[31]/17)*256)*4096));
                        }
                        if (cfgBytes.Length >= 38)
                        {
                            PlayerPrefs.SetFloat("esp_cd", (float)((cfgBytes[32]/17 + (cfgBytes[33]/17)*16 + (cfgBytes[34]/17)*256) + (cfgBytes[35]/17 + (cfgBytes[36]/17)*16 + (cfgBytes[37]/17)*256)*4096));
                        }
                        if (cfgBytes.Length >= 39)
                        {
                            PlayerPrefs.SetFloat("esp_sk", 0.5f + cfgBytes[38] * 0.2f);
                        }
                        if (cfgBytes.Length >= 14)
                        {
                            PlayerPrefs.SetFloat("esp_lt", 0.5f + cfgBytes[11] * 0.2f);
                            PlayerPrefs.SetFloat("esp_bt", 0.5f + cfgBytes[12] * 0.2f);
                            PlayerPrefs.SetFloat("esp_nt", 1.0f + cfgBytes[13] * 0.02f);
                        }
                        state = newState;
                        int packedAux = newAux;
                        float encodedAux = self.{{SCENE_STATE_FIELD}}.y;
                        if (encodedAux <= -AuxStateMarker)
                        {
                            int oldPacked = (int)(-encodedAux - AuxStateMarker);
                            packedAux |= oldPacked & AuxSpeedRunningApplied;
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

                    if ((state & AimSystemEnabled) != 0 && (state & AimFovHide) == 0)
                    {
                        float fdEnc2 = self.{{SCENE_STATE_FIELD}}.y;
                        int fovPacked = fdEnc2 <= -AuxStateMarker ? (int)(-fdEnc2 - AuxStateMarker) : 0;
                        float fovRadius = (float)((fovPacked >> AuxFovRadiusShift) & 0xFF) * 2f;
                        if (fovRadius > 0f)
                        {
                            Color _cfov = (state & EspColorEnabled) != 0 ? new Color((((int)PlayerPrefs.GetFloat("esp_cd",16777215f)>>12)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_cd",16777215f)>>16)&0xF)/15f,(((int)PlayerPrefs.GetFloat("esp_cd",16777215f)>>20)&0xF)/15f,1f) : new Color(1f,1f,1f,1f);
                            float circleX = (float)screenWidth * 0.5f;
                            float circleY = (float)screenHeight * 0.5f;

                            int fovSamples = 180;
                            float fovStep = TwoPi / fovSamples;
                            float fovThick = 1.5f;

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
                                if (float.IsNaN(segLen) || float.IsInfinity(segLen) || segLen <= 0f) continue;
                                float segAngle = Mathf.Atan2(segY, segX) * Rad2Deg;
                                GUI.matrix = Matrix4x4.TRS(new Vector3(startX, startY, 0f), Quaternion.Euler(0f, 0f, segAngle), Vector3.one);

                                GUI.color = new Color(_cfov.r, _cfov.g, _cfov.b, _cfov.a * 0.3f);
                                GUI.DrawTexture(new Rect(0f, -fovThick * 0.5f - 0.5f, segLen, fovThick + 1f), pixel);
                                GUI.color = _cfov;
                                GUI.DrawTexture(new Rect(0f, -fovThick * 0.5f, segLen, fovThick), pixel);

                                GUI.matrix = Matrix4x4.identity;
                            }
                        }
                    }

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
                            Color _cl  = new Color(0.6f, 1f, 1f, 1f);
                            Color _cb2 = Color.white;
                            Color _cn  = new Color(1f, 1f, 1f, 1f);
                            Color _cd  = new Color(1f, 1f, 1f, 1f);
                            Color _csk = Color.white;
                            float c_skelThick = 1.0f;
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
                                bool knocked = player.IsKnockedDownBleed;
                                bool lyingDown = dying || knocked;
                                int health = player.CurHP;
                                int maximumHealth = player.MaxHP;
                                if (health <= 0 && !dying) continue;
                                Transform head = player.GetHeadTF();
                                if (root == null || head == null) continue;

                                Transform _sLegL = player.LegBoneLeft; Transform _sLegR = player.LegBoneRight;
                                Vector3 _hipWorld;
                                if (_sLegL != null && _sLegR != null)
                                {
                                    Vector3 _lp = _sLegL.position, _rp = _sLegR.position;
                                    _hipWorld = new Vector3((_lp.x + _rp.x) * 0.5f, (_lp.y + _rp.y) * 0.5f, (_lp.z + _rp.z) * 0.5f);
                                }
                                else if (_sLegL != null) _hipWorld = _sLegL.position;
                                else if (_sLegR != null) _hipWorld = _sLegR.position;
                                else _hipWorld = root.position;

                                float distance = Vector3.Distance(localRoot.position, _hipWorld);
                                if (distance > 150f) continue;

                                Vector3 feetWorld, headWorld;
                                if (lyingDown)
                                {
                                    feetWorld = root.position;
                                    headWorld = head.position;
                                }
                                else
                                {
                                    feetWorld = new Vector3(_hipWorld.x, _hipWorld.y - 0.85f, _hipWorld.z);
                                    headWorld = new Vector3(_hipWorld.x, _hipWorld.y + 0.95f, _hipWorld.z);
                                }

                                Vector3 feetScreen = camera.WorldToScreenPoint(feetWorld);
                                Vector3 headScreen = camera.WorldToScreenPoint(headWorld);
                                if (feetScreen.z <= 0f || headScreen.z <= 0f) continue;
                                if (float.IsNaN(feetScreen.x) || float.IsNaN(feetScreen.y)
                                    || float.IsNaN(feetScreen.z) || float.IsNaN(headScreen.x)
                                    || float.IsNaN(headScreen.y) || float.IsNaN(headScreen.z)
                                    || float.IsInfinity(feetScreen.x) || float.IsInfinity(feetScreen.y)
                                    || float.IsInfinity(headScreen.x) || float.IsInfinity(headScreen.y)) continue;

                                float rawHeight = Mathf.Abs(headScreen.y - feetScreen.y);
                                if (rawHeight < 2f) continue;

                                float height, width, left, top;
                                if (lyingDown)
                                {
                                    height = rawHeight * 0.5f;
                                    if (height < 3f) height = 3f;
                                    width = height * 1.2f;
                                    float cx = (headScreen.x + feetScreen.x) * 0.5f;
                                    left = cx - width * 0.5f;
                                    float feetGuiY = (float)screenHeight - feetScreen.y;
                                    top = feetGuiY - height;
                                }
                                else
                                {
                                    height = rawHeight * 0.85f;
                                    width = rawHeight * 0.5f;
                                    left = headScreen.x - width * 0.5f;
                                    top = (float)screenHeight - headScreen.y + (rawHeight * 0.15f);
                                }

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
                                Color lineEspColor = (inFov || dying) ? new Color(1f, 0.15f, 0.15f, 1f) : _cl;
                                Color boxEspColor = (inFov || dying) ? new Color(1f, 0.15f, 0.15f, 1f) : _cb2;

                                if ((mask & EspBox) != 0)
                                {
                                    float tk = 1.0f;
                                    GUI.color = boxEspColor;
                                    GUI.DrawTexture(new Rect(left, top, width, tk), pixel);
                                    GUI.DrawTexture(new Rect(left, top + height - tk, width, tk), pixel);
                                    GUI.DrawTexture(new Rect(left, top, tk, height), pixel);
                                    GUI.DrawTexture(new Rect(left + width - tk, top, tk, height), pixel);
                                }

                                if ((mask & EspTracer) != 0)
                                {
                                    float startX = (float)screenWidth * 0.5f;
                                    float startY = 0f;
                                    float endX = left + width * 0.5f;
                                    float endY = top;
                                    float _tdx = endX - startX;
                                    float _tdy = endY - startY;
                                    float _tlen = Mathf.Sqrt(_tdx * _tdx + _tdy * _tdy);
                                    if (!float.IsNaN(_tlen) && !float.IsInfinity(_tlen) && _tlen > 0.1f)
                                    {
                                        float _tang = Mathf.Atan2(_tdy, _tdx) * Rad2Deg;
                                        GUI.matrix = Matrix4x4.TRS(new Vector3(startX, startY, 0f), Quaternion.Euler(0f, 0f, _tang), Vector3.one);
                                        GUI.color = new Color(lineEspColor.r, lineEspColor.g, lineEspColor.b, 0.3f);
                                        GUI.DrawTexture(new Rect(0f, -1.25f, _tlen, 2.5f), pixel);
                                        GUI.color = lineEspColor;
                                        GUI.DrawTexture(new Rect(0f, -0.75f, _tlen, 1.5f), pixel);
                                        GUI.matrix = Matrix4x4.identity;
                                    }
                                }

                                if ((mask & EspHealth) != 0)
                                {
                                    float healthRatio = maximumHealth > 0 ? Mathf.Clamp01((float)health / (float)maximumHealth) : 0f;
                                    float barX = left - 4f;
                                    float barW = 2f;
                                    GUI.color = new Color(0f, 0f, 0f, 1f);
                                    GUI.DrawTexture(new Rect(barX - 0.5f, top - 0.5f, barW + 1f, height + 1f), pixel);
                                    GUI.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                                    GUI.DrawTexture(new Rect(barX, top, barW, height), pixel);
                                    Color hc;
                                    if (dying) hc = new Color(1f, 0f, 0f, 1f);
                                    else if (healthRatio > 0.5f) hc = new Color(0f, 1f, 0f, 1f);
                                    else if (healthRatio > 0.25f) hc = new Color(1f, 1f, 0f, 1f);
                                    else hc = new Color(1f, 0f, 0f, 1f);
                                    GUI.color = hc;
                                    GUI.DrawTexture(new Rect(barX, top + height - height * healthRatio, barW, height * healthRatio), pixel);
                                }

                                if ((mask & EspName) != 0)
                                {
                                    string nickname = player.NickName;
                                    bool isBot = string.IsNullOrEmpty(nickname);
                                    if (isBot) nickname = "Bot";
                                    else if (nickname.Length > 10) nickname = nickname.Substring(0, 10);

                                    float textScale = 0.9f;
                                    float labelW = 220f / textScale;
                                    float labelH = 30f / textScale;

                                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                    GUI.color = dying ? new Color(1f, 0.35f, 0.35f, 1f) : _cn;
                                    GUI.matrix = Matrix4x4.TRS(new Vector3(left + width + 4f, top, 0f), Quaternion.identity, new Vector3(textScale, textScale, 1f));
                                    GUI.Label(new Rect(0f, 0f, labelW, labelH), nickname);

                                    GUI.matrix = Matrix4x4.identity;
                                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                }

                                if ((mask & EspDistance) != 0)
                                {
                                    int distVal = (int)Mathf.Clamp(distance, 0f, 999f);
                                    string distStr = distVal.ToString() + "M";
                                    float textScale = 0.85f;
                                    float labelW = 100f / textScale;
                                    float labelH = 30f / textScale;
                                    float nameOffsetY = 15f;

                                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                    GUI.color = _cd;
                                    GUI.matrix = Matrix4x4.TRS(new Vector3(left + width + 4f, top + nameOffsetY, 0f), Quaternion.identity, new Vector3(textScale, textScale, 1f));
                                    GUI.Label(new Rect(0f, 0f, labelW, labelH), distStr);

                                    GUI.matrix = Matrix4x4.identity;
                                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
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

                                    Color skelColor = dying ? new Color(1f, 0.2f, 0.2f, 1f) : _csk;

                                    if (spHead.z > 0f && spNeck.z > 0f) {
                                        float _x1 = spHead.x, _y1 = (float)screenHeight - spHead.y, _x2 = spNeck.x, _y2 = (float)screenHeight - spNeck.y;
                                        float _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy);
                                        if (_len >= 0.5f) {
                                            GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * Rad2Deg), Vector3.one);
                                            GUI.color = new Color(skelColor.r, skelColor.g, skelColor.b, 0.3f);
                                            GUI.DrawTexture(new Rect(0f, -1.0f, _len, 2.0f), pixel);
                                            GUI.color = skelColor;
                                            GUI.DrawTexture(new Rect(0f, -c_skelThick * 0.5f, _len, c_skelThick), pixel);
                                            GUI.matrix = Matrix4x4.identity;
                                        }
                                    }
                                    if (spNeck.z > 0f && spArmL.z > 0f) {
                                        float _x1 = spNeck.x, _y1 = (float)screenHeight - spNeck.y, _x2 = spArmL.x, _y2 = (float)screenHeight - spArmL.y;
                                        float _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy);
                                        if (_len >= 0.5f) {
                                            GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * Rad2Deg), Vector3.one);
                                            GUI.color = new Color(skelColor.r, skelColor.g, skelColor.b, 0.3f);
                                            GUI.DrawTexture(new Rect(0f, -1.0f, _len, 2.0f), pixel);
                                            GUI.color = skelColor;
                                            GUI.DrawTexture(new Rect(0f, -c_skelThick * 0.5f, _len, c_skelThick), pixel);
                                            GUI.matrix = Matrix4x4.identity;
                                        }
                                    }
                                    if (spArmL.z > 0f && spElbowL.z > 0f) {
                                        float _x1 = spArmL.x, _y1 = (float)screenHeight - spArmL.y, _x2 = spElbowL.x, _y2 = (float)screenHeight - spElbowL.y;
                                        float _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy);
                                        if (_len >= 0.5f) {
                                            GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * Rad2Deg), Vector3.one);
                                            GUI.color = new Color(skelColor.r, skelColor.g, skelColor.b, 0.3f);
                                            GUI.DrawTexture(new Rect(0f, -1.0f, _len, 2.0f), pixel);
                                            GUI.color = skelColor;
                                            GUI.DrawTexture(new Rect(0f, -c_skelThick * 0.5f, _len, c_skelThick), pixel);
                                            GUI.matrix = Matrix4x4.identity;
                                        }
                                    }
                                    if (spElbowL.z > 0f && spHandL.z > 0f) {
                                        float _x1 = spElbowL.x, _y1 = (float)screenHeight - spElbowL.y, _x2 = spHandL.x, _y2 = (float)screenHeight - spHandL.y;
                                        float _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy);
                                        if (_len >= 0.5f) {
                                            GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * Rad2Deg), Vector3.one);
                                            GUI.color = new Color(skelColor.r, skelColor.g, skelColor.b, 0.3f);
                                            GUI.DrawTexture(new Rect(0f, -1.0f, _len, 2.0f), pixel);
                                            GUI.color = skelColor;
                                            GUI.DrawTexture(new Rect(0f, -c_skelThick * 0.5f, _len, c_skelThick), pixel);
                                            GUI.matrix = Matrix4x4.identity;
                                        }
                                    }
                                    if (spNeck.z > 0f && spArmR.z > 0f) {
                                        float _x1 = spNeck.x, _y1 = (float)screenHeight - spNeck.y, _x2 = spArmR.x, _y2 = (float)screenHeight - spArmR.y;
                                        float _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy);
                                        if (_len >= 0.5f) {
                                            GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * Rad2Deg), Vector3.one);
                                            GUI.color = new Color(skelColor.r, skelColor.g, skelColor.b, 0.3f);
                                            GUI.DrawTexture(new Rect(0f, -1.0f, _len, 2.0f), pixel);
                                            GUI.color = skelColor;
                                            GUI.DrawTexture(new Rect(0f, -c_skelThick * 0.5f, _len, c_skelThick), pixel);
                                            GUI.matrix = Matrix4x4.identity;
                                        }
                                    }
                                    if (spArmR.z > 0f && spElbowR.z > 0f) {
                                        float _x1 = spArmR.x, _y1 = (float)screenHeight - spArmR.y, _x2 = spElbowR.x, _y2 = (float)screenHeight - spElbowR.y;
                                        float _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy);
                                        if (_len >= 0.5f) {
                                            GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * Rad2Deg), Vector3.one);
                                            GUI.color = new Color(skelColor.r, skelColor.g, skelColor.b, 0.3f);
                                            GUI.DrawTexture(new Rect(0f, -1.0f, _len, 2.0f), pixel);
                                            GUI.color = skelColor;
                                            GUI.DrawTexture(new Rect(0f, -c_skelThick * 0.5f, _len, c_skelThick), pixel);
                                            GUI.matrix = Matrix4x4.identity;
                                        }
                                    }
                                    if (spElbowR.z > 0f && spHandR.z > 0f) {
                                        float _x1 = spElbowR.x, _y1 = (float)screenHeight - spElbowR.y, _x2 = spHandR.x, _y2 = (float)screenHeight - spHandR.y;
                                        float _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy);
                                        if (_len >= 0.5f) {
                                            GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * Rad2Deg), Vector3.one);
                                            GUI.color = new Color(skelColor.r, skelColor.g, skelColor.b, 0.3f);
                                            GUI.DrawTexture(new Rect(0f, -1.0f, _len, 2.0f), pixel);
                                            GUI.color = skelColor;
                                            GUI.DrawTexture(new Rect(0f, -c_skelThick * 0.5f, _len, c_skelThick), pixel);
                                            GUI.matrix = Matrix4x4.identity;
                                        }
                                    }
                                    if (spNeck.z > 0f && spHip.z > 0f) {
                                        float _x1 = spNeck.x, _y1 = (float)screenHeight - spNeck.y, _x2 = spHip.x, _y2 = (float)screenHeight - spHip.y;
                                        float _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy);
                                        if (_len >= 0.5f) {
                                            GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * Rad2Deg), Vector3.one);
                                            GUI.color = new Color(skelColor.r, skelColor.g, skelColor.b, 0.3f);
                                            GUI.DrawTexture(new Rect(0f, -1.0f, _len, 2.0f), pixel);
                                            GUI.color = skelColor;
                                            GUI.DrawTexture(new Rect(0f, -c_skelThick * 0.5f, _len, c_skelThick), pixel);
                                            GUI.matrix = Matrix4x4.identity;
                                        }
                                    }
                                    if (spHip.z > 0f && spKneeL.z > 0f) {
                                        float _x1 = spHip.x, _y1 = (float)screenHeight - spHip.y, _x2 = spKneeL.x, _y2 = (float)screenHeight - spKneeL.y;
                                        float _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy);
                                        if (_len >= 0.5f) {
                                            GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * Rad2Deg), Vector3.one);
                                            GUI.color = new Color(skelColor.r, skelColor.g, skelColor.b, 0.3f);
                                            GUI.DrawTexture(new Rect(0f, -1.0f, _len, 2.0f), pixel);
                                            GUI.color = skelColor;
                                            GUI.DrawTexture(new Rect(0f, -c_skelThick * 0.5f, _len, c_skelThick), pixel);
                                            GUI.matrix = Matrix4x4.identity;
                                        }
                                    }
                                    if (spKneeL.z > 0f && spFootL.z > 0f) {
                                        float _x1 = spKneeL.x, _y1 = (float)screenHeight - spKneeL.y, _x2 = spFootL.x, _y2 = (float)screenHeight - spFootL.y;
                                        float _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy);
                                        if (_len >= 0.5f) {
                                            GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * Rad2Deg), Vector3.one);
                                            GUI.color = new Color(skelColor.r, skelColor.g, skelColor.b, 0.3f);
                                            GUI.DrawTexture(new Rect(0f, -1.0f, _len, 2.0f), pixel);
                                            GUI.color = skelColor;
                                            GUI.DrawTexture(new Rect(0f, -c_skelThick * 0.5f, _len, c_skelThick), pixel);
                                            GUI.matrix = Matrix4x4.identity;
                                        }
                                    }
                                    if (spHip.z > 0f && spKneeR.z > 0f) {
                                        float _x1 = spHip.x, _y1 = (float)screenHeight - spHip.y, _x2 = spKneeR.x, _y2 = (float)screenHeight - spKneeR.y;
                                        float _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy);
                                        if (_len >= 0.5f) {
                                            GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * Rad2Deg), Vector3.one);
                                            GUI.color = new Color(skelColor.r, skelColor.g, skelColor.b, 0.3f);
                                            GUI.DrawTexture(new Rect(0f, -1.0f, _len, 2.0f), pixel);
                                            GUI.color = skelColor;
                                            GUI.DrawTexture(new Rect(0f, -c_skelThick * 0.5f, _len, c_skelThick), pixel);
                                            GUI.matrix = Matrix4x4.identity;
                                        }
                                    }
                                    if (spKneeR.z > 0f && spFootR.z > 0f) {
                                        float _x1 = spKneeR.x, _y1 = (float)screenHeight - spKneeR.y, _x2 = spFootR.x, _y2 = (float)screenHeight - spFootR.y;
                                        float _dx = _x2 - _x1, _dy = _y2 - _y1, _len = Mathf.Sqrt(_dx * _dx + _dy * _dy);
                                        if (_len >= 0.5f) {
                                            GUI.matrix = Matrix4x4.TRS(new Vector3(_x1, _y1, 0f), Quaternion.Euler(0f, 0f, Mathf.Atan2(_dy, _dx) * Rad2Deg), Vector3.one);
                                            GUI.color = new Color(skelColor.r, skelColor.g, skelColor.b, 0.3f);
                                            GUI.DrawTexture(new Rect(0f, -1.0f, _len, 2.0f), pixel);
                                            GUI.color = skelColor;
                                            GUI.DrawTexture(new Rect(0f, -c_skelThick * 0.5f, _len, c_skelThick), pixel);
                                            GUI.matrix = Matrix4x4.identity;
                                        }
                                    }
                                }
                                enemyCount++;
                                }
                                catch (Exception) { }
                            }

                            try
                            {
                                string countText = "Người chơi : " + enemyCount;
                                Matrix4x4 _oldCM = GUI.matrix;
                                float _cScale = 2.0f;
                                float _cW = 800f / _cScale;
                                float _cH = 120f / _cScale;
                                float _cCX = (float)screenWidth * 0.5f;
                                float _cCY = 60f;
                                GUI.skin.label.alignment = TextAnchor.UpperCenter;
                                GUI.color = new Color(1f, 1f, 1f, 1f);
                                GUI.matrix = Matrix4x4.TRS(new Vector3(_cCX, _cCY, 0f), Quaternion.identity, new Vector3(_cScale, _cScale, 1f));
                                GUI.Label(new Rect(-_cW * 0.5f, 0f, _cW, _cH), countText);
                                GUI.matrix = _oldCM;
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                            }
                            catch (Exception) { }

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

                    try
                    {
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
                }
            }
            catch (Exception) { }

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
                                            if (_c == null || _c.gameObject == null
                                                || !_c.gameObject.activeInHierarchy
                                                || _c.IsLocalPlayer() || _c.CurHP <= 0
                                                || _c.IsLocalTeammate(false)
                                                || _c.IsLocalTeammate(true)
                                                || ((_c.IsDieing || _c.IsKnockedDownBleed) && (state & AimSkipDowned) != 0)) continue;
                                            Transform _cr = _c.RootTransform;
                                            if (_cr == null) continue;
                                            Vector3 _tp; Collider _tc = null;
                                            if (_mode == 2)
                                            {
                                                Transform _h = _c.GetHeadTF();
                                                if (_h == null) continue;
                                                _tp = _h.position;
                                                _tc = _c.HeadCollider;
                                                if (_tc == null) _tc = (Collider)_h.GetComponent(typeof(Collider));
                                            }
                                            else if (_mode == 1)
                                            {
                                                Transform _n = _c.NeckBone;
                                                if (_n != null) {
                                                    _tp = _n.position;
                                                    _tc = (Collider)_n.GetComponent(typeof(Collider));
                                                } else {
                                                    _tp = _cr.position + new Vector3(0f, 1.2f, 0f);
                                                }
                                                if (_tc == null) _tc = _c.HeadCollider;
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
                                    bool foundGround = Physics.Raycast(origin, new Vector3(0f, -1f, 0f), out groundHit, 2048f, -5, QueryTriggerInteraction.Ignore);
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

            try
            {
                Texture2D _mpx = Texture2D.whiteTexture;
                if (_mpx != null)
                {
                    Vector2 _mPtr = Vector2.zero;
                    bool _mDown = false, _mDrag = false, _mUp = false;
                    if (Input.touchCount > 0)
                    {
                        Touch _mt = Input.GetTouch(0);
                        _mPtr = new Vector2(_mt.position.x, (float)screenHeight - _mt.position.y);
                        _mDown = _mt.phase == TouchPhase.Began;
                        _mDrag = _mt.phase == TouchPhase.Moved || _mt.phase == TouchPhase.Stationary;
                        _mUp   = _mt.phase == TouchPhase.Ended || _mt.phase == TouchPhase.Canceled;
                    }
                    else
                    {
                        Vector2 _mm = Input.mousePosition;
                        _mPtr = new Vector2(_mm.x, (float)screenHeight - _mm.y);
                        _mDown = Input.GetMouseButtonDown(0);
                        _mDrag = Input.GetMouseButton(0);
                        _mUp   = Input.GetMouseButtonUp(0);
                    }

                    GameObject _mFab = GameObject.Find("__esp_menu_fab");
                    if (_mFab == null)
                    {
                        _mFab = new GameObject("__esp_menu_fab");
                        _mFab.transform.position = new Vector3((float)screenWidth - 60f, 70f, 0f);
                        UnityEngine.Object.DontDestroyOnLoad(_mFab);
                    }
                    GameObject _mOpenObj = GameObject.Find("__esp_menu_open");
                    bool _mOpen = _mOpenObj != null;

                    float _fabW = 36f, _fabH = 36f;
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

                    float _fcx = _fabRect.x + _fabRect.width * 0.5f;
                    float _fcy = _fabRect.y + _fabRect.height * 0.5f;
                    float _fr  = _fabRect.width * 0.5f;

                    GUI.color = new Color(0f, 0f, 0f, 0.8f);
                    for (int _si = 0; _si < 64; _si++)
                    {
                        float _a1 = (float)_si * 0.0981748f;
                        float _a2 = (float)(_si + 1) * 0.0981748f;
                        float _sx1 = _fcx + Mathf.Cos(_a1) * (_fr + 1.5f), _sy1 = _fcy + Mathf.Sin(_a1) * (_fr + 1.5f);
                        float _sx2 = _fcx + Mathf.Cos(_a2) * (_fr + 1.5f), _sy2 = _fcy + Mathf.Sin(_a2) * (_fr + 1.5f);
                        float _sdx = _sx2 - _sx1, _sdy = _sy2 - _sy1;
                        float _slen = Mathf.Sqrt(_sdx * _sdx + _sdy * _sdy);
                        float _sang = Mathf.Atan2(_sdy, _sdx) * Rad2Deg;
                        GUI.matrix = Matrix4x4.TRS(new Vector3(_sx1, _sy1, 0f), Quaternion.Euler(0f, 0f, _sang), Vector3.one);
                        GUI.DrawTexture(new Rect(0f, -2f, _slen + 1f, 4f), _mpx);
                        GUI.matrix = Matrix4x4.identity;
                    }

                    Color _fabColor = _mOpen ? new Color(0.88f, 0.10f, 0.10f, 1f) : new Color(0.30f, 0.80f, 0.40f, 1f);
                    GUI.color = _fabColor;
                    for (int _si = 0; _si < 64; _si++)
                    {
                        float _a1 = (float)_si * 0.0981748f;
                        float _a2 = (float)(_si + 1) * 0.0981748f;
                        float _rr = _fr - 2f;
                        float _sx1 = _fcx + Mathf.Cos(_a1) * _rr, _sy1 = _fcy + Mathf.Sin(_a1) * _rr;
                        float _sx2 = _fcx + Mathf.Cos(_a2) * _rr, _sy2 = _fcy + Mathf.Sin(_a2) * _rr;
                        float _sdx = _sx2 - _sx1, _sdy = _sy2 - _sy1;
                        float _slen = Mathf.Sqrt(_sdx * _sdx + _sdy * _sdy);
                        float _sang = Mathf.Atan2(_sdy, _sdx) * Rad2Deg;
                        GUI.matrix = Matrix4x4.TRS(new Vector3(_sx1, _sy1, 0f), Quaternion.Euler(0f, 0f, _sang), Vector3.one);
                        GUI.DrawTexture(new Rect(0f, -1.2f, _slen + 1f, 2.4f), _mpx);
                        GUI.matrix = Matrix4x4.identity;
                    }

                    GUI.color = new Color(0.10f, 0.10f, 0.14f, 1f);
                    for (int _si = 0; _si < 64; _si++)
                    {
                        float _a1 = (float)_si * 0.0981748f;
                        float _a2 = (float)(_si + 1) * 0.0981748f;
                        float _rr = _fr - 4f;
                        float _sx1 = _fcx + Mathf.Cos(_a1) * _rr, _sy1 = _fcy + Mathf.Sin(_a1) * _rr;
                        float _sx2 = _fcx + Mathf.Cos(_a2) * _rr, _sy2 = _fcy + Mathf.Sin(_a2) * _rr;
                        float _sdx = _sx2 - _sx1, _sdy = _sy2 - _sy1;
                        float _slen = Mathf.Sqrt(_sdx * _sdx + _sdy * _sdy);
                        float _sang = Mathf.Atan2(_sdy, _sdx) * Rad2Deg;
                        GUI.matrix = Matrix4x4.TRS(new Vector3(_sx1, _sy1, 0f), Quaternion.Euler(0f, 0f, _sang), Vector3.one);
                        GUI.DrawTexture(new Rect(0f, -1.5f, _slen + 1f, 3f), _mpx);
                        GUI.matrix = Matrix4x4.identity;
                    }

                    Matrix4x4 _oldFM = GUI.matrix;
                    GUI.color = Color.white;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    float _vnS = _fr * 0.14f;
                    float _vnW = (_fr * 1.8f) / _vnS;
                    float _vnH = (_fr * 1.6f) / _vnS;
                    GUI.matrix = Matrix4x4.TRS(new Vector3(_fcx - _fr * 0.9f, _fcy - _fr * 0.8f, 0f), Quaternion.identity, new Vector3(_vnS, _vnS, 1f));
                    GUI.Label(new Rect(0f, 0f, _vnW, _vnH), "VN");
                    GUI.matrix = _oldFM;
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;

                    if (_mOpen)
                    {
                        float _pw = Mathf.Min(580f, (float)screenWidth - 20f);
                        float _ph = Mathf.Min(460f, (float)screenHeight - 40f);

                        GameObject _mPos = GameObject.Find("__esp_menu_pos");
                        if (_mPos == null)
                        {
                            _mPos = new GameObject("__esp_menu_pos");
                            UnityEngine.Object.DontDestroyOnLoad(_mPos);
                        }
                        float _mOffX = _mPos.transform.position.x;
                        float _mOffY = _mPos.transform.position.y;
                        float _centerX = ((float)screenWidth - _pw) * 0.5f;
                        float _centerY = ((float)screenHeight - _ph) * 0.5f;
                        float _px0 = Mathf.Clamp(_centerX + _mOffX, 0f, Mathf.Max(0f, (float)screenWidth - _pw));
                        float _py0 = Mathf.Clamp(_centerY + _mOffY, 0f, Mathf.Max(0f, (float)screenHeight - _ph));

                        GUI.color = new Color(0f, 0f, 0f, 0.55f);
                        GUI.DrawTexture(new Rect(0f, 0f, (float)screenWidth, (float)screenHeight), _mpx);

                        GUI.color = new Color(0.98f, 0.98f, 0.99f, 1f);
                        GUI.DrawTexture(new Rect(_px0, _py0, _pw, _ph), _mpx);

                        GUI.color = new Color(0.10f, 0.10f, 0.14f, 1f);
                        GUI.DrawTexture(new Rect(_px0, _py0, _pw, 44f), _mpx);

                        GUI.color = Color.white;
                        GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                        {
                            Matrix4x4 _hvM = GUI.matrix;
                            float _hvS = 1.4f;
                            GUI.matrix = Matrix4x4.TRS(new Vector3(_px0 + 20f, _py0 + 4f, 0f), Quaternion.identity, new Vector3(_hvS, _hvS, 1f));
                            GUI.Label(new Rect(0f, 0f, (_pw - 80f) / _hvS, 44f / _hvS), "VN TOOL");
                            GUI.matrix = _hvM;
                        }
                        GUI.skin.label.alignment = TextAnchor.UpperLeft;

                        Rect _xRect = new Rect(_px0 + _pw - 50f, _py0 + 7f, 42f, 30f);
                        GUI.color = new Color(0.30f, 0.10f, 0.10f, 1f);
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

                        Rect _headerRect = new Rect(_px0, _py0, _pw - 60f, 44f);
                        GameObject _mDHandle = GameObject.Find("__esp_menu_drag_handle");
                        if (_mDown && _headerRect.Contains(_mPtr) && !_xRect.Contains(_mPtr))
                        {
                            if (_mDHandle != null) { _mDHandle.name = "__esp_menu_drag_handle_old"; UnityEngine.Object.Destroy(_mDHandle); }
                            _mDHandle = new GameObject("__esp_menu_drag_handle");
                            _mDHandle.transform.position = new Vector3(_mPtr.x - _px0, _mPtr.y - _py0, 0f);
                            _mDHandle.transform.localScale = new Vector3(_mPtr.x, _mPtr.y, 0f);
                            UnityEngine.Object.DontDestroyOnLoad(_mDHandle);
                        }
                        if (_mDHandle != null && _mDrag)
                        {
                            Vector3 _dp = _mDHandle.transform.localScale;
                            float _dmx = _mPtr.x - _dp.x, _dmy = _mPtr.y - _dp.y;
                            if (_dp.z < 0.5f && _dmx * _dmx + _dmy * _dmy >= 36f)
                            { _dp.z = 1f; _mDHandle.transform.localScale = _dp; }
                            if (_dp.z >= 0.5f)
                            {
                                float _newPx = _mPtr.x - _mDHandle.transform.position.x;
                                float _newPy = _mPtr.y - _mDHandle.transform.position.y;
                                float _newOffX = _newPx - _centerX;
                                float _newOffY = _newPy - _centerY;
                                _newOffX = Mathf.Clamp(_newOffX, -_centerX, (float)screenWidth - _pw - _centerX);
                                _newOffY = Mathf.Clamp(_newOffY, -_centerY, (float)screenHeight - _ph - _centerY);
                                _mPos.transform.position = new Vector3(_newOffX, _newOffY, 0f);
                                _px0 = _centerX + _newOffX;
                                _py0 = _centerY + _newOffY;
                            }
                        }
                        if (_mDHandle != null && _mUp)
                        {
                            _mDHandle.name = "__esp_menu_drag_handle_done"; UnityEngine.Object.Destroy(_mDHandle);
                        }

                        string[] _tabNames = new string[4];
                        _tabNames[0] = "Ngắm";
                        _tabNames[1] = "Hiển thị";
                        _tabNames[2] = "Cấu hình";
                        _tabNames[3] = "Khác";

                        int _activeTab = 0;
                        GameObject _tabObj = GameObject.Find("__esp_menu_tab");
                        if (_tabObj != null) _activeTab = (int)_tabObj.transform.position.x;
                        if (_activeTab < 0) _activeTab = 0;
                        if (_activeTab > 3) _activeTab = 3;

                        float _tabW = 140f;
                        GUI.color = new Color(0.94f, 0.94f, 0.96f, 1f);
                        GUI.DrawTexture(new Rect(_px0, _py0 + 44f, _tabW, _ph - 44f), _mpx);

                        for (int _ti = 0; _ti < 4; _ti++)
                        {
                            float _ty = _py0 + 44f + 12f + _ti * 60f;
                            Rect _tabRect = new Rect(_px0 + 12f, _ty, _tabW - 24f, 48f);
                            bool _sel = (_activeTab == _ti);
                            GUI.color = _sel ? new Color(0.88f, 0.10f, 0.10f, 1f) : new Color(0.85f, 0.85f, 0.88f, 1f);
                            GUI.DrawTexture(_tabRect, _mpx);

                            GUI.color = _sel ? Color.white : new Color(0.15f, 0.15f, 0.18f, 1f);
                            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                            {
                                Matrix4x4 _tbM = GUI.matrix;
                                float _tbS = 1.2f;
                                GUI.matrix = Matrix4x4.TRS(new Vector3(_tabRect.x, _tabRect.y + _tabRect.height * 0.35f, 0f), Quaternion.identity, new Vector3(_tbS, _tbS, 1f));
                                GUI.Label(new Rect(0f, 0f, _tabRect.width / _tbS, _tabRect.height / _tbS), _tabNames[_ti]);
                                GUI.matrix = _tbM;
                            }
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
                        float _cw  = _pw - _tabW - 32f;
                        float _cy0 = _py0 + 56f;
                        float _rowY = _cy0;
                        float _rowH = 34f;
                        float _lblScale = 1.1f;

                        int _mState = (int)self.{{SCENE_STATE_FIELD}}.x;
                        float _mEncAux = self.{{SCENE_STATE_FIELD}}.y;
                        int _mAux = _mEncAux <= -AuxStateMarker ? (int)(-_mEncAux - AuxStateMarker) : 0;
                        int _mR8 = (int)PlayerPrefs.GetFloat("esp_r8", 0f);

                        if (_activeTab == 0)
                        {
                            bool _v1 = (_mState & AimEnabled) != 0;
                            {
                                Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                                GUI.color = new Color(0.12f, 0.12f, 0.15f, 1f);
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                { Matrix4x4 _lM = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(_row.x + 12f, _row.y + _rowH * 0.5f, 0f), Quaternion.identity, new Vector3(_lblScale, _lblScale, 1f)); GUI.Label(new Rect(0f, -_rowH * 0.5f, (_row.width - 100f) / _lblScale, _rowH / _lblScale), "Tự ngắm (Ẩn)"); GUI.matrix = _lM; }
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect _tk = new Rect(_row.x + _row.width - 64f, _row.y + (_rowH - 26f) * 0.5f, 56f, 26f);
                                GUI.color = _v1 ? new Color(0.88f, 0.10f, 0.10f, 1f) : new Color(0.72f, 0.72f, 0.75f, 1f);
                                GUI.DrawTexture(_tk, _mpx);
                                float _tw = 20f;
                                Rect _th = _v1 ? new Rect(_tk.x + _tk.width - _tw - 2f, _tk.y + 2f, _tw, 22f) : new Rect(_tk.x + 2f, _tk.y + 2f, _tw, 22f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(_th, _mpx);
                                if (_mUp && _tk.Contains(_mPtr)) _mState = _v1 ? (_mState & ~AimEnabled) : (_mState | AimEnabled);
                            }
                            _rowY += _rowH + 4f;

                            bool _v2 = (_mState & AimSystemEnabled) != 0;
                            {
                                Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                                GUI.color = new Color(0.12f, 0.12f, 0.15f, 1f);
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                { Matrix4x4 _lM = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(_row.x + 12f, _row.y + _rowH * 0.5f, 0f), Quaternion.identity, new Vector3(_lblScale, _lblScale, 1f)); GUI.Label(new Rect(0f, -_rowH * 0.5f, (_row.width - 100f) / _lblScale, _rowH / _lblScale), "Khóa mục tiêu (FOV)"); GUI.matrix = _lM; }
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect _tk = new Rect(_row.x + _row.width - 64f, _row.y + (_rowH - 26f) * 0.5f, 56f, 26f);
                                GUI.color = _v2 ? new Color(0.88f, 0.10f, 0.10f, 1f) : new Color(0.72f, 0.72f, 0.75f, 1f);
                                GUI.DrawTexture(_tk, _mpx);
                                float _tw = 20f;
                                Rect _th = _v2 ? new Rect(_tk.x + _tk.width - _tw - 2f, _tk.y + 2f, _tw, 22f) : new Rect(_tk.x + 2f, _tk.y + 2f, _tw, 22f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(_th, _mpx);
                                if (_mUp && _tk.Contains(_mPtr)) _mState = _v2 ? (_mState & ~AimSystemEnabled) : (_mState | AimSystemEnabled);
                            }
                            _rowY += _rowH + 4f;

                            bool _v3 = (_mState & AimSkipDowned) != 0;
                            {
                                Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                                GUI.color = new Color(0.12f, 0.12f, 0.15f, 1f);
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                { Matrix4x4 _lM = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(_row.x + 12f, _row.y + _rowH * 0.5f, 0f), Quaternion.identity, new Vector3(_lblScale, _lblScale, 1f)); GUI.Label(new Rect(0f, -_rowH * 0.5f, (_row.width - 100f) / _lblScale, _rowH / _lblScale), "Bỏ qua địch gục"); GUI.matrix = _lM; }
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect _tk = new Rect(_row.x + _row.width - 64f, _row.y + (_rowH - 26f) * 0.5f, 56f, 26f);
                                GUI.color = _v3 ? new Color(0.88f, 0.10f, 0.10f, 1f) : new Color(0.72f, 0.72f, 0.75f, 1f);
                                GUI.DrawTexture(_tk, _mpx);
                                float _tw = 20f;
                                Rect _th = _v3 ? new Rect(_tk.x + _tk.width - _tw - 2f, _tk.y + 2f, _tw, 22f) : new Rect(_tk.x + 2f, _tk.y + 2f, _tw, 22f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(_th, _mpx);
                                if (_mUp && _tk.Contains(_mPtr)) _mState = _v3 ? (_mState & ~AimSkipDowned) : (_mState | AimSkipDowned);
                            }
                            _rowY += _rowH + 4f;

                            bool _v4 = (_mState & AimFovHide) == 0;
                            {
                                Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                                GUI.color = new Color(0.12f, 0.12f, 0.15f, 1f);
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                { Matrix4x4 _lM = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(_row.x + 12f, _row.y + _rowH * 0.5f, 0f), Quaternion.identity, new Vector3(_lblScale, _lblScale, 1f)); GUI.Label(new Rect(0f, -_rowH * 0.5f, (_row.width - 100f) / _lblScale, _rowH / _lblScale), "Hiện vòng FOV"); GUI.matrix = _lM; }
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect _tk = new Rect(_row.x + _row.width - 64f, _row.y + (_rowH - 26f) * 0.5f, 56f, 26f);
                                GUI.color = _v4 ? new Color(0.88f, 0.10f, 0.10f, 1f) : new Color(0.72f, 0.72f, 0.75f, 1f);
                                GUI.DrawTexture(_tk, _mpx);
                                float _tw = 20f;
                                Rect _th = _v4 ? new Rect(_tk.x + _tk.width - _tw - 2f, _tk.y + 2f, _tw, 22f) : new Rect(_tk.x + 2f, _tk.y + 2f, _tw, 22f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(_th, _mpx);
                                if (_mUp && _tk.Contains(_mPtr)) _mState = _v4 ? (_mState | AimFovHide) : (_mState & ~AimFovHide);
                            }
                            _rowY += _rowH + 4f;

                            {
                                Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                                int _mode = (_mState & AimModeMask) >> AimModeShift;
                                GUI.color = new Color(0.12f, 0.12f, 0.15f, 1f);
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                { Matrix4x4 _lM = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(_row.x + 12f, _row.y + _rowH * 0.5f, 0f), Quaternion.identity, new Vector3(_lblScale, _lblScale, 1f)); GUI.Label(new Rect(0f, -_rowH * 0.5f, (_row.width - 100f) / _lblScale, _rowH / _lblScale), "Kiểu ngắm:"); GUI.matrix = _lM; }
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;

                                string[] _mNames = new string[4];
                                _mNames[0] = "Thân"; _mNames[1] = "Cổ"; _mNames[2] = "Đầu"; _mNames[3] = "Hợp";

                                float _btnW = 50f;
                                float _btnH = 26f;
                                float _btnGap = 4f;
                                float _btnStartX = _row.x + _row.width - (4f * _btnW + 3f * _btnGap);
                                float _btnY = _row.y + (_rowH - _btnH) * 0.5f;

                                for (int _mi = 0; _mi < 4; _mi++)
                                {
                                    Rect _mBtn = new Rect(_btnStartX + _mi * (_btnW + _btnGap), _btnY, _btnW, _btnH);
                                    bool _selMode = (_mode == _mi);
                                    GUI.color = _selMode ? new Color(0.88f, 0.10f, 0.10f, 1f) : new Color(0.72f, 0.72f, 0.75f, 1f);
                                    GUI.DrawTexture(_mBtn, _mpx);
                                    GUI.color = _selMode ? Color.white : new Color(0.10f, 0.10f, 0.12f, 1f);
                                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                                    {
                                        Matrix4x4 _mbM = GUI.matrix;
                                        GUI.matrix = Matrix4x4.TRS(new Vector3(_mBtn.x, _mBtn.y + _mBtn.height * 0.35f, 0f), Quaternion.identity, new Vector3(1.0f, 1.0f, 1f));
                                        GUI.Label(new Rect(0f, 0f, _mBtn.width / 1.0f, _mBtn.height / 1.0f), _mNames[_mi]);
                                        GUI.matrix = _mbM;
                                    }
                                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                    if (_mUp && _mBtn.Contains(_mPtr))
                                    {
                                        _mState = (_mState & ~AimModeMask) | ((_mi & 3) << AimModeShift);
                                    }
                                }
                            }
                            _rowY += _rowH + 4f;

                            {
                                Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                                int _hr = (_mState & HeadRateMask) >> HeadRateShift;
                                GUI.color = new Color(0.12f, 0.12f, 0.15f, 1f);
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                { Matrix4x4 _lM = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(_row.x + 12f, _row.y + _rowH * 0.5f, 0f), Quaternion.identity, new Vector3(_lblScale, _lblScale, 1f)); GUI.Label(new Rect(0f, -_rowH * 0.5f, (_row.width - 240f) / _lblScale, _rowH / _lblScale), "Tỉ lệ đầu: " + (_hr * 25) + "%"); GUI.matrix = _lM; }
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect _tr = new Rect(_row.x + _row.width - 220f, _row.y + (_rowH - 12f) * 0.5f, 200f, 12f);
                                GUI.color = new Color(0.80f, 0.80f, 0.83f, 1f);
                                GUI.DrawTexture(_tr, _mpx);
                                float _ratio = Mathf.Clamp01((float)_hr / 4f);
                                GUI.color = new Color(0.88f, 0.10f, 0.10f, 1f);
                                GUI.DrawTexture(new Rect(_tr.x, _tr.y, _tr.width * _ratio, _tr.height), _mpx);
                                GUI.color = Color.white;
                                GUI.DrawTexture(new Rect(_tr.x + _tr.width * _ratio - 8f, _tr.y - 6f, 16f, 24f), _mpx);
                                if (_mDrag && _mPtr.y >= _row.y && _mPtr.y <= _row.y + _row.height
                                    && _mPtr.x >= _tr.x - 14f && _mPtr.x <= _tr.x + _tr.width + 14f)
                                {
                                    float _nr = Mathf.Clamp01((_mPtr.x - _tr.x) / _tr.width);
                                    int _nh = (int)(_nr * 4f + 0.5f);
                                    if (_nh < 0) _nh = 0; else if (_nh > 4) _nh = 4;
                                    _mState = (_mState & ~HeadRateMask) | ((_nh & 7) << HeadRateShift);
                                }
                            }
                            _rowY += _rowH + 4f;

                            bool _v5 = (_mState & NoRecoil) != 0;
                            {
                                Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                                GUI.color = new Color(0.12f, 0.12f, 0.15f, 1f);
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                { Matrix4x4 _lM = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(_row.x + 12f, _row.y + _rowH * 0.5f, 0f), Quaternion.identity, new Vector3(_lblScale, _lblScale, 1f)); GUI.Label(new Rect(0f, -_rowH * 0.5f, (_row.width - 100f) / _lblScale, _rowH / _lblScale), "Không giật"); GUI.matrix = _lM; }
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect _tk = new Rect(_row.x + _row.width - 64f, _row.y + (_rowH - 26f) * 0.5f, 56f, 26f);
                                GUI.color = _v5 ? new Color(0.88f, 0.10f, 0.10f, 1f) : new Color(0.72f, 0.72f, 0.75f, 1f);
                                GUI.DrawTexture(_tk, _mpx);
                                float _tw = 20f;
                                Rect _th = _v5 ? new Rect(_tk.x + _tk.width - _tw - 2f, _tk.y + 2f, _tw, 22f) : new Rect(_tk.x + 2f, _tk.y + 2f, _tw, 22f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(_th, _mpx);
                                if (_mUp && _tk.Contains(_mPtr)) _mState = _v5 ? (_mState & ~NoRecoil) : (_mState | NoRecoil);
                            }
                            _rowY += _rowH + 4f;

                            self.{{SCENE_STATE_FIELD}} = new Vector2((float)(_mState & 0xFFFFFF), self.{{SCENE_STATE_FIELD}}.y);

                            {
                                Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                                int _fovPx = (_mAux >> AuxFovRadiusShift) & 0xFF;
                                GUI.color = new Color(0.12f, 0.12f, 0.15f, 1f);
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                { Matrix4x4 _lM = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(_row.x + 12f, _row.y + _rowH * 0.5f, 0f), Quaternion.identity, new Vector3(_lblScale, _lblScale, 1f)); GUI.Label(new Rect(0f, -_rowH * 0.5f, (_row.width - 240f) / _lblScale, _rowH / _lblScale), "Bán kính FOV: " + (_fovPx * 2)); GUI.matrix = _lM; }
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect _tr = new Rect(_row.x + _row.width - 220f, _row.y + (_rowH - 12f) * 0.5f, 200f, 12f);
                                GUI.color = new Color(0.80f, 0.80f, 0.83f, 1f);
                                GUI.DrawTexture(_tr, _mpx);
                                float _ratio = Mathf.Clamp01((float)_fovPx / 255f);
                                GUI.color = new Color(0.88f, 0.10f, 0.10f, 1f);
                                GUI.DrawTexture(new Rect(_tr.x, _tr.y, _tr.width * _ratio, _tr.height), _mpx);
                                GUI.color = Color.white;
                                GUI.DrawTexture(new Rect(_tr.x + _tr.width * _ratio - 8f, _tr.y - 6f, 16f, 24f), _mpx);
                                if (_mDrag && _mPtr.y >= _row.y && _mPtr.y <= _row.y + _row.height
                                    && _mPtr.x >= _tr.x - 14f && _mPtr.x <= _tr.x + _tr.width + 14f)
                                {
                                    float _nr = Mathf.Clamp01((_mPtr.x - _tr.x) / _tr.width);
                                    int _nv = (int)(_nr * 255f + 0.5f);
                                    _mAux = (_mAux & ~(0xFF << AuxFovRadiusShift)) | ((_nv & 0xFF) << AuxFovRadiusShift);
                                    self.{{SCENE_STATE_FIELD}} = new Vector2(self.{{SCENE_STATE_FIELD}}.x, -AuxStateMarker - (float)_mAux);
                                }
                            }
                            _rowY += _rowH + 4f;

                            {
                                Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                                int _silPx = (_mAux >> AuxSilentFovShift) & 0xFF;
                                GUI.color = new Color(0.12f, 0.12f, 0.15f, 1f);
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                { Matrix4x4 _lM = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(_row.x + 12f, _row.y + _rowH * 0.5f, 0f), Quaternion.identity, new Vector3(_lblScale, _lblScale, 1f)); GUI.Label(new Rect(0f, -_rowH * 0.5f, (_row.width - 240f) / _lblScale, _rowH / _lblScale), "Bán kính Ẩn: " + (_silPx * 2)); GUI.matrix = _lM; }
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect _tr = new Rect(_row.x + _row.width - 220f, _row.y + (_rowH - 12f) * 0.5f, 200f, 12f);
                                GUI.color = new Color(0.80f, 0.80f, 0.83f, 1f);
                                GUI.DrawTexture(_tr, _mpx);
                                float _ratio = Mathf.Clamp01((float)_silPx / 255f);
                                GUI.color = new Color(0.88f, 0.10f, 0.10f, 1f);
                                GUI.DrawTexture(new Rect(_tr.x, _tr.y, _tr.width * _ratio, _tr.height), _mpx);
                                GUI.color = Color.white;
                                GUI.DrawTexture(new Rect(_tr.x + _tr.width * _ratio - 8f, _tr.y - 6f, 16f, 24f), _mpx);
                                if (_mDrag && _mPtr.y >= _row.y && _mPtr.y <= _row.y + _row.height
                                    && _mPtr.x >= _tr.x - 14f && _mPtr.x <= _tr.x + _tr.width + 14f)
                                {
                                    float _nr = Mathf.Clamp01((_mPtr.x - _tr.x) / _tr.width);
                                    int _nv = (int)(_nr * 255f + 0.5f);
                                    _mAux = (_mAux & ~(0xFF << AuxSilentFovShift)) | ((_nv & 0xFF) << AuxSilentFovShift);
                                    self.{{SCENE_STATE_FIELD}} = new Vector2(self.{{SCENE_STATE_FIELD}}.x, -AuxStateMarker - (float)_mAux);
                                }
                            }
                        }
                        else if (_activeTab == 1)
                        {
                            int[] _bits = new int[9];
                            _bits[0] = EspBox; _bits[1] = EspTracer; _bits[2] = EspHealth;
                            _bits[3] = EspName; _bits[4] = EspDistance; _bits[5] = EspSkeleton;
                            _bits[6] = EspColorEnabled; _bits[7] = EspFov; _bits[8] = EspMaster;

                            string[] _labels = new string[9];
                            _labels[0] = "Khung ESP"; _labels[1] = "Đường kẻ"; _labels[2] = "Thanh máu";
                            _labels[3] = "Tên"; _labels[4] = "Khoảng cách"; _labels[5] = "Xương";
                            _labels[6] = "Màu tùy chỉnh"; _labels[7] = "Vòng FOV"; _labels[8] = "Bật ESP";

                            for (int _bi = 0; _bi < _bits.Length; _bi++)
                            {
                                bool _bv = (_mState & _bits[_bi]) != 0;
                                Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                                GUI.color = new Color(0.12f, 0.12f, 0.15f, 1f);
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                { Matrix4x4 _lM = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(_row.x + 12f, _row.y + _rowH * 0.5f, 0f), Quaternion.identity, new Vector3(_lblScale, _lblScale, 1f)); GUI.Label(new Rect(0f, -_rowH * 0.5f, (_row.width - 100f) / _lblScale, _rowH / _lblScale), _labels[_bi]); GUI.matrix = _lM; }
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect _tk = new Rect(_row.x + _row.width - 64f, _row.y + (_rowH - 26f) * 0.5f, 56f, 26f);
                                GUI.color = _bv ? new Color(0.88f, 0.10f, 0.10f, 1f) : new Color(0.72f, 0.72f, 0.75f, 1f);
                                GUI.DrawTexture(_tk, _mpx);
                                float _tw = 20f;
                                Rect _th = _bv ? new Rect(_tk.x + _tk.width - _tw - 2f, _tk.y + 2f, _tw, 22f) : new Rect(_tk.x + 2f, _tk.y + 2f, _tw, 22f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(_th, _mpx);
                                if (_mUp && _tk.Contains(_mPtr)) _mState = _bv ? (_mState & ~_bits[_bi]) : (_mState | _bits[_bi]);
                                _rowY += _rowH + 4f;
                            }
                            self.{{SCENE_STATE_FIELD}} = new Vector2((float)(_mState & 0xFFFFFF), self.{{SCENE_STATE_FIELD}}.y);
                        }
                        else if (_activeTab == 2)
                        {
                            int[] _abits = new int[8];
                            _abits[0] = AuxFastParachute; _abits[1] = AuxSpeedRunning; _abits[2] = AuxFakeDamage;
                            _abits[3] = AuxFastHeal; _abits[4] = AuxFastFire; _abits[5] = AuxBackJump;
                            _abits[6] = AuxWideCamera; _abits[7] = AuxGhostControl;

                            string[] _alabels = new string[8];
                            _alabels[0] = "Nhảy dù nhanh"; _alabels[1] = "Chạy nhanh"; _alabels[2] = "Sát thương ảo";
                            _alabels[3] = "Hồi máu nhanh"; _alabels[4] = "Bắn nhanh"; _alabels[5] = "Nhảy ngược";
                            _alabels[6] = "Góc rộng"; _alabels[7] = "Điều khiển ma";

                            for (int _bi = 0; _bi < _abits.Length; _bi++)
                            {
                                bool _bv = (_mAux & _abits[_bi]) != 0;
                                Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                                GUI.color = new Color(0.12f, 0.12f, 0.15f, 1f);
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                { Matrix4x4 _lM = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(_row.x + 12f, _row.y + _rowH * 0.5f, 0f), Quaternion.identity, new Vector3(_lblScale, _lblScale, 1f)); GUI.Label(new Rect(0f, -_rowH * 0.5f, (_row.width - 100f) / _lblScale, _rowH / _lblScale), _alabels[_bi]); GUI.matrix = _lM; }
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect _tk = new Rect(_row.x + _row.width - 64f, _row.y + (_rowH - 26f) * 0.5f, 56f, 26f);
                                GUI.color = _bv ? new Color(0.88f, 0.10f, 0.10f, 1f) : new Color(0.72f, 0.72f, 0.75f, 1f);
                                GUI.DrawTexture(_tk, _mpx);
                                float _tw = 20f;
                                Rect _th = _bv ? new Rect(_tk.x + _tk.width - _tw - 2f, _tk.y + 2f, _tw, 22f) : new Rect(_tk.x + 2f, _tk.y + 2f, _tw, 22f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(_th, _mpx);
                                if (_mUp && _tk.Contains(_mPtr)) _mAux = _bv ? (_mAux & ~_abits[_bi]) : (_mAux | _abits[_bi]);
                                _rowY += _rowH + 4f;
                            }
                            self.{{SCENE_STATE_FIELD}} = new Vector2(self.{{SCENE_STATE_FIELD}}.x, -AuxStateMarker - (float)_mAux);
                        }
                        else if (_activeTab == 3)
                        {
                            int[] _rbits = new int[8];
                            _rbits[0] = R8NoFog; _rbits[1] = R8FastCrouch; _rbits[2] = R8FastRevive;
                            _rbits[3] = R8SkillCD; _rbits[4] = R8Chams; _rbits[5] = R8FastLoot;
                            _rbits[6] = R8CamHack; _rbits[7] = R8UnlockFps;

                            string[] _rlabels = new string[8];
                            _rlabels[0] = "Không sương mù"; _rlabels[1] = "Ngồi nhanh"; _rlabels[2] = "Hồi sinh nhanh";
                            _rlabels[3] = "Không hồi chiêu"; _rlabels[4] = "Xuyên tường"; _rlabels[5] = "Nhặt nhanh";
                            _rlabels[6] = "Camera hack"; _rlabels[7] = "Mở FPS (120)";

                            for (int _bi = 0; _bi < _rbits.Length; _bi++)
                            {
                                bool _bv = (_mR8 & _rbits[_bi]) != 0;
                                Rect _row = new Rect(_cx0, _rowY, _cw, _rowH);
                                GUI.color = new Color(0.12f, 0.12f, 0.15f, 1f);
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                { Matrix4x4 _lM = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(_row.x + 12f, _row.y + _rowH * 0.5f, 0f), Quaternion.identity, new Vector3(_lblScale, _lblScale, 1f)); GUI.Label(new Rect(0f, -_rowH * 0.5f, (_row.width - 100f) / _lblScale, _rowH / _lblScale), _rlabels[_bi]); GUI.matrix = _lM; }
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect _tk = new Rect(_row.x + _row.width - 64f, _row.y + (_rowH - 26f) * 0.5f, 56f, 26f);
                                GUI.color = _bv ? new Color(0.88f, 0.10f, 0.10f, 1f) : new Color(0.72f, 0.72f, 0.75f, 1f);
                                GUI.DrawTexture(_tk, _mpx);
                                float _tw = 20f;
                                Rect _th = _bv ? new Rect(_tk.x + _tk.width - _tw - 2f, _tk.y + 2f, _tw, 22f) : new Rect(_tk.x + 2f, _tk.y + 2f, _tw, 22f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(_th, _mpx);
                                if (_mUp && _tk.Contains(_mPtr)) _mR8 = _bv ? (_mR8 & ~_rbits[_bi]) : (_mR8 | _rbits[_bi]);
                                _rowY += _rowH + 4f;
                            }
                            PlayerPrefs.SetFloat("esp_r8", (float)_mR8);
                            PlayerPrefs.Save();
                        }
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

                int aimMode = (state & AimModeMask) >> AimModeShift;
                int headRate = ((state & HeadRateMask) >> HeadRateShift) * 25;
                bool aimAtHead = false, aimAtNeck = false;
                if (silentAimOn) { aimAtHead = true; }
                else
                {
                    if (aimMode == 1) aimAtNeck = true;
                    else if (aimMode == 2) aimAtHead = true;
                    else if (aimMode == 3)
                    {
                        int r = UnityEngine.Random.Range(0, 100);
                        if (r < headRate) aimAtHead = true;
                        else if (r < headRate + (100 - headRate) / 2) aimAtNeck = true;
                    }
                }
                int fovFilterPx = silentAimOn ? silentFovPx : aimFovPixels;

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
                    else if (aimAtNeck)
                    {
                        Transform neck = candidate.NeckBone;
                        if (neck != null) {
                            hitPosition = neck.position;
                            hitCollider = (Collider)neck.GetComponent(typeof(Collider));
                        } else {
                            hitPosition = root.position + new Vector3(0f, 1.2f, 0f);
                            hitCollider = null;
                        }
                        if (hitCollider == null) hitCollider = headCollider;
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

                    bestScore = score; bestCollider = hitCollider; bestPosition = hitPosition;
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

                int aimMode = (state & AimModeMask) >> AimModeShift;
                int headRate = ((state & HeadRateMask) >> HeadRateShift) * 25;
                bool fovAimAtHead = false, fovAimAtNeck = false;
                if (aimMode == 1) fovAimAtNeck = true;
                else if (aimMode == 2) fovAimAtHead = true;
                else if (aimMode == 3)
                {
                    int r = UnityEngine.Random.Range(0, 100);
                    if (r < headRate) fovAimAtHead = true;
                    else if (r < headRate + (100 - headRate) / 2) fovAimAtNeck = true;
                }

                Collider targetCollider = null;
                if (fovAimAtHead) targetCollider = self.HeadCollider;
                else if (fovAimAtNeck)
                {
                    Transform neckN = self.NeckBone;
                    if (neckN != null)
                    {
                        targetCollider = (Collider)neckN.GetComponent(typeof(Collider));
                    }
                    if (targetCollider == null) targetCollider = self.HeadCollider;
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
