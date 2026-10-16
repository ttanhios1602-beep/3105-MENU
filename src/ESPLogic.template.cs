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
        private const int AuxBackJump = 1 << 29;
        private const int AuxGhostControl = 1 << 30;

        private const int R8NoFog = 1;
        private const int R8FastCrouch = 2;
        private const int R8FastRevive = 4;
        private const int R8SkillCD = 8;
        private const int R8Chams = 16;
        private const int R8FastLoot = 32;
        private const int R8CamHack = 64;
        private const int R8UnlockFps = 128;

        private const int AuxMask = 4095;
        private const int AuxFovRadiusShift = 4;
        private const int AuxSilentFovShift = 12;
        private const float AuxStateMarker = 1000000f;
        private const ulong SpeedRunningKey = 4995421289296778564UL;

        private const int DefaultStateBits = (2 << AimModeShift) | (3 << HeadRateShift);

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
            if (currentEvent == null || currentEvent.type != EventType.Repaint) return;

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
                            newState = (int)cfgBytes[0] | ((int)cfgBytes[1] << 8) | ((int)cfgBytes[2] << 16) | ((int)cfgBytes[3] << 24);
                            newState |= StateInitialized;
                        }
                        if (cfgBytes.Length >= 8)
                        {
                            newAux = (int)cfgBytes[4] | ((int)cfgBytes[5] << 8) | ((int)cfgBytes[6] << 16) | ((int)cfgBytes[7] << 24);
                        }
                        int _r8 = cfgBytes.Length >= 9 ? cfgBytes[8] : 0;
                        PlayerPrefs.SetFloat("esp_r8", (float)_r8);

                        state = newState;
                        int packedAux = newAux;
                        float encodedAux = self.{{SCENE_STATE_FIELD}}.y;
                        if (encodedAux <= -AuxStateMarker)
                        {
                            int oldPacked = (int)(-encodedAux - AuxStateMarker);
                            packedAux |= oldPacked & AuxSpeedRunningApplied;
                        }
                        self.{{SCENE_STATE_FIELD}} = new Vector2((float)(state & 0xFFFFFF), -AuxStateMarker - (float)packedAux);
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
                            GUI.color = new Color(0.1f, 0.9f, 0.4f, 0.8f);
                            float circleX = (float)screenWidth * 0.5f;
                            float circleY = (float)screenHeight * 0.5f;
                            const int fovSamples = 64;
                            const float fovStep = (Mathf.PI * 2f) / fovSamples;
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
                                if (segLen <= 0f) continue;
                                GUI.matrix = Matrix4x4.TRS(new Vector3(startX, startY, 0f), Quaternion.Euler(0f, 0f, segAngle), Vector3.one);
                                GUI.DrawTexture(new Rect(0f, -0.8f, segLen + 0.5f, 1.6f), pixel);
                                GUI.matrix = Matrix4x4.identity;
                            }
                        }
                    }

                    int enemyCount = 0;
                    if ((mask & EspMaster) != 0)
                    {
                        {{MATCH_TYPE}} match = GameFacade.CurrentMatch();
                        IList players = match == null ? null : match.{{MATCH_PLAYERS_METHOD}}();
                        GameObject localObject = localPlayer == null ? null : localPlayer.gameObject;

                        if (camera != null && localPlayer != null && localObject != null && localObject.activeInHierarchy && localRoot != null && players != null)
                        {
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
                                    if (player.IsLocalPlayer() || player.IsLocalTeammate(false) || !player.IsVisible()) continue;

                                    bool dying = player.IsDieing;
                                    int health = player.CurHP;
                                    int maximumHealth = player.MaxHP;
                                    if (health <= 0 && !dying) continue;
                                    Transform head = player.GetHeadTF();
                                    if (root == null || head == null) continue;

                                    Vector3 _hipWorld = root.position + new Vector3(0f, 0.9f, 0f);
                                    float distance = Vector3.Distance(localRoot.position, _hipWorld);
                                    if (distance > 250f) continue;

                                    Vector3 feetScreen = camera.WorldToScreenPoint(new Vector3(_hipWorld.x, _hipWorld.y - 0.9f, _hipWorld.z));
                                    Vector3 headScreen = camera.WorldToScreenPoint(new Vector3(_hipWorld.x, _hipWorld.y + 0.9f, _hipWorld.z));
                                    if (feetScreen.z <= 0f || headScreen.z <= 0f) continue;

                                    float height = Mathf.Abs(headScreen.y - feetScreen.y) * 1.1f;
                                    if (height < 3f) continue;
                                    float width = height * 0.52f;
                                    float left = headScreen.x - width * 0.5f;
                                    float top = (float)screenHeight - headScreen.y;

                                    if (left > (float)screenWidth || left + width < 0f || top > (float)screenHeight || top + height < 0f) continue;

                                    enemyCount++;

                                    Color mainColor = dying ? new Color(1f, 0.2f, 0.2f, 1f) : new Color(0.95f, 0.15f, 0.15f, 1f);

                                    if ((mask & EspBox) != 0)
                                    {
                                        GUI.color = Color.black;
                                        GUI.DrawTexture(new Rect(left - 1, top - 1, width + 2, 3), pixel);
                                        GUI.DrawTexture(new Rect(left - 1, top + height - 2, width + 2, 3), pixel);
                                        GUI.DrawTexture(new Rect(left - 1, top - 1, 3, height + 2), pixel);
                                        GUI.DrawTexture(new Rect(left + width - 2, top - 1, 3, height + 2), pixel);

                                        GUI.color = mainColor;
                                        GUI.DrawTexture(new Rect(left, top, width, 1.5f), pixel);
                                        GUI.DrawTexture(new Rect(left, top + height - 1.5f, width, 1.5f), pixel);
                                        GUI.DrawTexture(new Rect(left, top, 1.5f, height), pixel);
                                        GUI.DrawTexture(new Rect(left + width - 1.5f, top, 1.5f, height), pixel);
                                    }

                                    if ((mask & EspTracer) != 0)
                                    {
                                        float startX = (float)screenWidth * 0.5f;
                                        float startY = 0f;
                                        float endX = left + width * 0.5f;
                                        float endY = top;
                                        float tracerX = endX - startX;
                                        float tracerY = endY - startY;
                                        float tracerLen = Mathf.Sqrt(tracerX * tracerX + tracerY * tracerY);
                                        float tracerAng = Mathf.Atan2(tracerY, tracerX) * 57.29578f;

                                        GUI.matrix = Matrix4x4.TRS(new Vector3(startX, startY, 0f), Quaternion.Euler(0f, 0f, tracerAng), Vector3.one);
                                        GUI.color = new Color(0f, 0f, 0f, 0.6f);
                                        GUI.DrawTexture(new Rect(0f, -1.5f, tracerLen, 3f), pixel);
                                        GUI.color = mainColor;
                                        GUI.DrawTexture(new Rect(0f, -0.75f, tracerLen, 1.5f), pixel);
                                        GUI.matrix = Matrix4x4.identity;
                                    }

                                    if ((mask & EspHealth) != 0)
                                    {
                                        float healthRatio = maximumHealth > 0 ? Mathf.Clamp01((float)health / (float)maximumHealth) : 0f;
                                        float healthX = left - 6f;
                                        GUI.color = new Color(0f, 0f, 0f, 0.8f);
                                        GUI.DrawTexture(new Rect(healthX - 1, top - 1, 5, height + 2), pixel);
                                        GUI.color = healthRatio > 0.5f ? new Color(0.2f, 0.9f, 0.2f, 1f) : (healthRatio > 0.2f ? Color.yellow : Color.red);
                                        GUI.DrawTexture(new Rect(healthX, top + height - height * healthRatio, 3, height * healthRatio), pixel);
                                    }

                                    if ((mask & EspName) != 0)
                                    {
                                        string nickname = player.NickName;
                                        if (string.IsNullOrEmpty(nickname)) nickname = "Bot";
                                        else if (nickname.Length > 8) nickname = nickname.Substring(0, 8);

                                        GUI.color = new Color(0.95f, 0.15f, 0.15f, 1f);
                                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                                        GUI.Label(new Rect(left - 20f, top - 20f, width + 40f, 20f), nickname);
                                        GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                    }

                                    if ((mask & EspDistance) != 0)
                                    {
                                        string distStr = ((int)distance).ToString() + "m";
                                        GUI.color = Color.white;
                                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                                        GUI.Label(new Rect(left - 20f, top + height + 2f, width + 40f, 20f), distStr);
                                        GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                    }
                                }
                                catch (Exception) { }
                            }
                        }
                    }

                    GUI.color = new Color(0f, 0f, 0f, 0.7f);
                    GUI.DrawTexture(new Rect(20f, 20f, 160f, 32f), pixel);
                    GUI.color = Color.white;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(new Rect(20f, 20f, 160f, 32f), "Người chơi: " + enemyCount);
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;

                    Vector2 mPtr = Vector2.zero;
                    bool mDown = false, mDrag = false, mUp = false;
                    if (Input.touchCount > 0)
                    {
                        Touch mt = Input.GetTouch(0);
                        mPtr = new Vector2(mt.position.x, (float)screenHeight - mt.position.y);
                        mDown = mt.phase == TouchPhase.Began;
                        mDrag = mt.phase == TouchPhase.Moved || mt.phase == TouchPhase.Stationary;
                        mUp = mt.phase == TouchPhase.Ended || mt.phase == TouchPhase.Canceled;
                    }
                    else
                    {
                        Vector2 mm = Input.mousePosition;
                        mPtr = new Vector2(mm.x, (float)screenHeight - mm.y);
                        mDown = Input.GetMouseButtonDown(0);
                        mDrag = Input.GetMouseButton(0);
                        mUp = Input.GetMouseButtonUp(0);
                    }

                    GameObject mFab = GameObject.Find("__esp_menu_fab");
                    if (mFab == null)
                    {
                        mFab = new GameObject("__esp_menu_fab");
                        mFab.transform.position = new Vector3((float)screenWidth - 80f, 100f, 0f);
                        UnityEngine.Object.DontDestroyOnLoad(mFab);
                    }
                    GameObject mOpenObj = GameObject.Find("__esp_menu_open");
                    bool mOpen = mOpenObj != null;

                    float fabW = 56f, fabH = 56f;
                    float fabX = Mathf.Clamp(mFab.transform.position.x, 0f, Mathf.Max(0f, (float)screenWidth - fabW));
                    float fabY = Mathf.Clamp(mFab.transform.position.y, 0f, Mathf.Max(0f, (float)screenHeight - fabH));
                    Rect fabRect = new Rect(fabX, fabY, fabW, fabH);

                    GameObject mFabDrag = GameObject.Find("__esp_menu_fab_drag");
                    if (mDown && fabRect.Contains(mPtr) && !mOpen)
                    {
                        if (mFabDrag != null) { mFabDrag.name = "__esp_menu_fab_drag_old"; UnityEngine.Object.Destroy(mFabDrag); }
                        mFabDrag = new GameObject("__esp_menu_fab_drag");
                        mFabDrag.transform.position = new Vector3(mPtr.x - fabRect.x, mPtr.y - fabRect.y, 0f);
                        mFabDrag.transform.localScale = new Vector3(mPtr.x, mPtr.y, 0f);
                        UnityEngine.Object.DontDestroyOnLoad(mFabDrag);
                    }
                    if (mFabDrag != null && mDrag)
                    {
                        Vector3 fp = mFabDrag.transform.localScale;
                        float fmx = mPtr.x - fp.x, fmy = mPtr.y - fp.y;
                        if (fp.z < 0.5f && fmx * fmx + fmy * fmy >= 36f)
                        { fp.z = 1f; mFabDrag.transform.localScale = fp; }
                        if (fp.z >= 0.5f)
                        {
                            fabX = Mathf.Clamp(mPtr.x - mFabDrag.transform.position.x, 0f, Mathf.Max(0f, (float)screenWidth - fabW));
                            fabY = Mathf.Clamp(mPtr.y - mFabDrag.transform.position.y, 0f, Mathf.Max(0f, (float)screenHeight - fabH));
                            mFab.transform.position = new Vector3(fabX, fabY, 0f);
                            fabRect = new Rect(fabX, fabY, fabW, fabH);
                        }
                    }
                    if (mFabDrag != null && mUp)
                    {
                        bool fWasDragged = mFabDrag.transform.localScale.z >= 0.5f;
                        mFabDrag.name = "__esp_menu_fab_drag_done"; UnityEngine.Object.Destroy(mFabDrag);
                        if (!fWasDragged && fabRect.Contains(mPtr))
                        {
                            if (mOpen)
                            {
                                GameObject mo = GameObject.Find("__esp_menu_open");
                                if (mo != null) { mo.name = "__esp_menu_open_off"; UnityEngine.Object.Destroy(mo); }
                                mOpen = false;
                            }
                            else
                            {
                                GameObject mo = new GameObject("__esp_menu_open");
                                UnityEngine.Object.DontDestroyOnLoad(mo);
                                mOpen = true;
                            }
                        }
                    }

                    float fcx = fabRect.x + fabRect.width * 0.5f;
                    float fcy = fabRect.y + fabRect.height * 0.5f;
                    float fr = fabRect.width * 0.5f;

                    GUI.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
                    for (int si = 0; si < 48; si++)
                    {
                        float a1 = (float)si * 0.1308997f;
                        float a2 = (float)(si + 1) * 0.1308997f;
                        float sx1 = fcx + Mathf.Cos(a1) * fr, sy1 = fcy + Mathf.Sin(a1) * fr;
                        float sx2 = fcx + Mathf.Cos(a2) * fr, sy2 = fcy + Mathf.Sin(a2) * fr;
                        float sdx = sx2 - sx1, sdy = sy2 - sy1;
                        float slen = Mathf.Sqrt(sdx * sdx + sdy * sdy);
                        float sang = Mathf.Atan2(sdy, sdx) * 57.29578f;
                        GUI.matrix = Matrix4x4.TRS(new Vector3(sx1, sy1, 0f), Quaternion.Euler(0f, 0f, sang), Vector3.one);
                        GUI.DrawTexture(new Rect(0f, -fr, slen + 1f, fr * 2f), pixel);
                        GUI.matrix = Matrix4x4.identity;
                    }

                    GUI.color = mOpen ? new Color(0.9f, 0.15f, 0.15f, 1f) : new Color(0.15f, 0.75f, 0.35f, 1f);
                    for (int si = 0; si < 48; si++)
                    {
                        float a1 = (float)si * 0.1308997f;
                        float a2 = (float)(si + 1) * 0.1308997f;
                        float rr = fr - 3f;
                        float sx1 = fcx + Mathf.Cos(a1) * rr, sy1 = fcy + Mathf.Sin(a1) * rr;
                        float sx2 = fcx + Mathf.Cos(a2) * rr, sy2 = fcy + Mathf.Sin(a2) * rr;
                        float sdx = sx2 - sx1, sdy = sy2 - sy1;
                        float slen = Mathf.Sqrt(sdx * sdx + sdy * sdy);
                        float sang = Mathf.Atan2(sdy, sdx) * 57.29578f;
                        GUI.matrix = Matrix4x4.TRS(new Vector3(sx1, sy1, 0f), Quaternion.Euler(0f, 0f, sang), Vector3.one);
                        GUI.DrawTexture(new Rect(0f, -rr, slen + 1f, rr * 2f), pixel);
                        GUI.matrix = Matrix4x4.identity;
                    }

                    GUI.color = Color.white;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(new Rect(fabRect.x, fabRect.y, fabRect.width, fabRect.height), "VN");
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;

                    if (mOpen)
                    {
                        float pw = Mathf.Min(760f, (float)screenWidth - 30f);
                        float ph = Mathf.Min(520f, (float)screenHeight - 40f);
                        float px0 = ((float)screenWidth - pw) * 0.5f;
                        float py0 = ((float)screenHeight - ph) * 0.5f;

                        GUI.color = new Color(0f, 0f, 0f, 0.6f);
                        GUI.DrawTexture(new Rect(0f, 0f, (float)screenWidth, (float)screenHeight), pixel);

                        GUI.color = new Color(0.12f, 0.12f, 0.16f, 1f);
                        GUI.DrawTexture(new Rect(px0, py0, pw, ph), pixel);

                        GUI.color = new Color(0.16f, 0.16f, 0.22f, 1f);
                        GUI.DrawTexture(new Rect(px0, py0, pw, 48f), pixel);
                        GUI.color = Color.white;
                        GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                        GUI.Label(new Rect(px0 + 20f, py0, pw - 60f, 48f), "  VN TOOL");
                        GUI.skin.label.alignment = TextAnchor.UpperLeft;

                        Rect xRect = new Rect(px0 + pw - 45f, py0 + 10f, 34f, 28f);
                        GUI.color = new Color(0.85f, 0.2f, 0.2f, 1f);
                        GUI.DrawTexture(xRect, pixel);
                        GUI.color = Color.white;
                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                        GUI.Label(xRect, "X");
                        GUI.skin.label.alignment = TextAnchor.UpperLeft;
                        if (mUp && xRect.Contains(mPtr))
                        {
                            GameObject mo = GameObject.Find("__esp_menu_open");
                            if (mo != null) { mo.name = "__esp_menu_open_off"; UnityEngine.Object.Destroy(mo); }
                            mOpen = false;
                        }

                        string[] tabNames = new string[5];
                        tabNames[0] = "Tự Động Ngắm";
                        tabNames[1] = "Hiển Thị ESP";
                        tabNames[2] = "Tính Năng Phụ";
                        tabNames[3] = "Chức Năng Khác";
                        tabNames[4] = "Cài Đặt Chung";

                        int activeTab = 0;
                        GameObject tabObj = GameObject.Find("__esp_menu_tab");
                        if (tabObj != null) activeTab = (int)tabObj.transform.position.x;
                        if (activeTab < 0) activeTab = 0;
                        if (activeTab > 4) activeTab = 4;

                        float tabW = 160f;
                        GUI.color = new Color(0.10f, 0.10f, 0.14f, 1f);
                        GUI.DrawTexture(new Rect(px0, py0 + 48f, tabW, ph - 48f), pixel);
                        for (int ti = 0; ti < 5; ti++)
                        {
                            float ty = py0 + 56f + ti * 52f;
                            Rect tabRect = new Rect(px0 + 10f, ty, tabW - 20f, 42f);
                            bool sel = (activeTab == ti);
                            GUI.color = sel ? new Color(0.9f, 0.15f, 0.15f, 1f) : new Color(0.18f, 0.18f, 0.24f, 1f);
                            GUI.DrawTexture(tabRect, pixel);
                            GUI.color = Color.white;
                            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                            GUI.Label(tabRect, tabNames[ti]);
                            GUI.skin.label.alignment = TextAnchor.UpperLeft;
                            if (mUp && tabRect.Contains(mPtr))
                            {
                                if (tabObj == null)
                                {
                                    tabObj = new GameObject("__esp_menu_tab");
                                    UnityEngine.Object.DontDestroyOnLoad(tabObj);
                                }
                                tabObj.transform.position = new Vector3((float)ti, 0f, 0f);
                                activeTab = ti;
                            }
                        }

                        float cx0 = px0 + tabW + 18f;
                        float cw = pw - tabW - 36f;
                        float rowY = py0 + 64f;
                        float rowH = 42f;

                        int mState = (int)self.{{SCENE_STATE_FIELD}}.x;
                        float mEncAux = self.{{SCENE_STATE_FIELD}}.y;
                        int mAux = mEncAux <= -AuxStateMarker ? (int)(-mEncAux - AuxStateMarker) : 0;
                        int mR8 = (int)PlayerPrefs.GetFloat("esp_r8", 0f);

                        if (activeTab == 0)
                        {
                            bool v1 = (mState & AimEnabled) != 0;
                            {
                                Rect row = new Rect(cx0, rowY, cw, rowH);
                                GUI.color = Color.white;
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                GUI.Label(new Rect(row.x + 8f, row.y, row.width - 90f, row.height), "Ngắm Khóa Mục Tiêu (Silent)");
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect tk = new Rect(row.x + row.width - 75f, row.y + 7f, 64f, 28f);
                                GUI.color = v1 ? new Color(0.9f, 0.15f, 0.15f, 1f) : new Color(0.3f, 0.3f, 0.38f, 1f);
                                GUI.DrawTexture(tk, pixel);
                                float tw = 26f;
                                Rect th = v1 ? new Rect(tk.x + tk.width - tw - 2f, tk.y + 2f, tw, 24f) : new Rect(tk.x + 2f, tk.y + 2f, tw, 24f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(th, pixel);
                                if (mUp && tk.Contains(mPtr)) mState = v1 ? (mState & ~AimEnabled) : (mState | AimEnabled);
                            }
                            rowY += rowH + 10f;

                            bool v2 = (mState & AimSystemEnabled) != 0;
                            {
                                Rect row = new Rect(cx0, rowY, cw, rowH);
                                GUI.color = Color.white;
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                GUI.Label(new Rect(row.x + 8f, row.y, row.width - 90f, row.height), "Hệ Thống Ngắm Tự Động (FOV)");
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect tk = new Rect(row.x + row.width - 75f, row.y + 7f, 64f, 28f);
                                GUI.color = v2 ? new Color(0.9f, 0.15f, 0.15f, 1f) : new Color(0.3f, 0.3f, 0.38f, 1f);
                                GUI.DrawTexture(tk, pixel);
                                float tw = 26f;
                                Rect th = v2 ? new Rect(tk.x + tk.width - tw - 2f, tk.y + 2f, tw, 24f) : new Rect(tk.x + 2f, tk.y + 2f, tw, 24f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(th, pixel);
                                if (mUp && tk.Contains(mPtr)) mState = v2 ? (mState & ~AimSystemEnabled) : (mState | AimSystemEnabled);
                            }
                            rowY += rowH + 10f;

                            bool v3 = (mState & AimSkipDowned) != 0;
                            {
                                Rect row = new Rect(cx0, rowY, cw, rowH);
                                GUI.color = Color.white;
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                GUI.Label(new Rect(row.x + 8f, row.y, row.width - 90f, row.height), "Bỏ Qua Kẻ Địch Đã Gục");
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect tk = new Rect(row.x + row.width - 75f, row.y + 7f, 64f, 28f);
                                GUI.color = v3 ? new Color(0.9f, 0.15f, 0.15f, 1f) : new Color(0.3f, 0.3f, 0.38f, 1f);
                                GUI.DrawTexture(tk, pixel);
                                float tw = 26f;
                                Rect th = v3 ? new Rect(tk.x + tk.width - tw - 2f, tk.y + 2f, tw, 24f) : new Rect(tk.x + 2f, tk.y + 2f, tw, 24f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(th, pixel);
                                if (mUp && tk.Contains(mPtr)) mState = v3 ? (mState & ~AimSkipDowned) : (mState | AimSkipDowned);
                            }
                            rowY += rowH + 10f;

                            bool v4 = (mState & AimFovHide) == 0;
                            {
                                Rect row = new Rect(cx0, rowY, cw, rowH);
                                GUI.color = Color.white;
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                GUI.Label(new Rect(row.x + 8f, row.y, row.width - 90f, row.height), "Hiển Thị Vòng Tròn FOV");
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect tk = new Rect(row.x + row.width - 75f, row.y + 7f, 64f, 28f);
                                GUI.color = v4 ? new Color(0.9f, 0.15f, 0.15f, 1f) : new Color(0.3f, 0.3f, 0.38f, 1f);
                                GUI.DrawTexture(tk, pixel);
                                float tw = 26f;
                                Rect th = v4 ? new Rect(tk.x + tk.width - tw - 2f, tk.y + 2f, tw, 24f) : new Rect(tk.x + 2f, tk.y + 2f, tw, 24f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(th, pixel);
                                if (mUp && tk.Contains(mPtr)) mState = v4 ? (mState | AimFovHide) : (mState & ~AimFovHide);
                            }
                            rowY += rowH + 10f;

                            bool v5 = (mState & NoRecoil) != 0;
                            {
                                Rect row = new Rect(cx0, rowY, cw, rowH);
                                GUI.color = Color.white;
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                GUI.Label(new Rect(row.x + 8f, row.y, row.width - 90f, row.height), "Triệt Tiêu Giật Súng (No Recoil)");
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect tk = new Rect(row.x + row.width - 75f, row.y + 7f, 64f, 28f);
                                GUI.color = v5 ? new Color(0.9f, 0.15f, 0.15f, 1f) : new Color(0.3f, 0.3f, 0.38f, 1f);
                                GUI.DrawTexture(tk, pixel);
                                float tw = 26f;
                                Rect th = v5 ? new Rect(tk.x + tk.width - tw - 2f, tk.y + 2f, tw, 24f) : new Rect(tk.x + 2f, tk.y + 2f, tw, 24f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(th, pixel);
                                if (mUp && tk.Contains(mPtr)) mState = v5 ? (mState & ~NoRecoil) : (mState | NoRecoil);
                            }

                            self.{{SCENE_STATE_FIELD}} = new Vector2((float)(mState & 0xFFFFFF), self.{{SCENE_STATE_FIELD}}.y);
                        }
                        else if (activeTab == 1)
                        {
                            int[] bits = new int[9];
                            bits[0] = EspBox;
                            bits[1] = EspTracer;
                            bits[2] = EspHealth;
                            bits[3] = EspName;
                            bits[4] = EspDistance;
                            bits[5] = EspSkeleton;
                            bits[6] = EspColorEnabled;
                            bits[7] = EspFov;
                            bits[8] = EspMaster;

                            string[] labels = new string[9];
                            labels[0] = "Khung ESP (Box)";
                            labels[1] = "Đường Kẻ (Tracer)";
                            labels[2] = "Thanh Máu (Health)";
                            labels[3] = "Tên Kẻ Địch (Name)";
                            labels[4] = "Khoảng Cách (Distance)";
                            labels[5] = "Hệ Thống Xương (Skeleton)";
                            labels[6] = "Màu Sắc Tùy Chỉnh";
                            labels[7] = "Vòng FOV Màn Hình";
                            labels[8] = "Bật/Tắt Tổng ESP";

                            for (int bi = 0; bi < bits.Length; bi++)
                            {
                                bool bv = (mState & bits[bi]) != 0;
                                Rect row = new Rect(cx0, rowY, cw, rowH);
                                GUI.color = Color.white;
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                GUI.Label(new Rect(row.x + 8f, row.y, row.width - 90f, row.height), labels[bi]);
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect tk = new Rect(row.x + row.width - 75f, row.y + 7f, 64f, 28f);
                                GUI.color = bv ? new Color(0.9f, 0.15f, 0.15f, 1f) : new Color(0.3f, 0.3f, 0.38f, 1f);
                                GUI.DrawTexture(tk, pixel);
                                float tw = 26f;
                                Rect th = bv ? new Rect(tk.x + tk.width - tw - 2f, tk.y + 2f, tw, 24f) : new Rect(tk.x + 2f, tk.y + 2f, tw, 24f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(th, pixel);
                                if (mUp && tk.Contains(mPtr)) mState = bv ? (mState & ~bits[bi]) : (mState | bits[bi]);
                                rowY += rowH + 6f;
                            }
                            self.{{SCENE_STATE_FIELD}} = new Vector2((float)(mState & 0xFFFFFF), self.{{SCENE_STATE_FIELD}}.y);
                        }
                        else if (activeTab == 2)
                        {
                            int[] abits = new int[8];
                            abits[0] = AuxFastParachute;
                            abits[1] = AuxSpeedRunning;
                            abits[2] = AuxFakeDamage;
                            abits[3] = AuxFastHeal;
                            abits[4] = AuxFastFire;
                            abits[5] = AuxBackJump;
                            abits[6] = AuxWideCamera;
                            abits[7] = AuxGhostControl;

                            string[] alabels = new string[8];
                            alabels[0] = "Nhảy Dù Nhanh";
                            alabels[1] = "Chạy Nước Rút Tốc Độ";
                            alabels[2] = "Sát Thương Ảo Lực";
                            alabels[3] = "Hồi Máu Tức Thì";
                            alabels[4] = "Bắn Tốc Độ Cao";
                            alabels[5] = "Nhảy Lùi Linh Hoạt";
                            alabels[6] = "Mở Rộng Góc Nhìn Camera";
                            alabels[7] = "Điều Khiển Ẩn (Ghost)";

                            for (int bi = 0; bi < abits.Length; bi++)
                            {
                                bool bv = (mAux & abits[bi]) != 0;
                                Rect row = new Rect(cx0, rowY, cw, rowH);
                                GUI.color = Color.white;
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                GUI.Label(new Rect(row.x + 8f, row.y, row.width - 90f, row.height), alabels[bi]);
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect tk = new Rect(row.x + row.width - 75f, row.y + 7f, 64f, 28f);
                                GUI.color = bv ? new Color(0.9f, 0.15f, 0.15f, 1f) : new Color(0.3f, 0.3f, 0.38f, 1f);
                                GUI.DrawTexture(tk, pixel);
                                float tw = 26f;
                                Rect th = bv ? new Rect(tk.x + tk.width - tw - 2f, tk.y + 2f, tw, 24f) : new Rect(tk.x + 2f, tk.y + 2f, tw, 24f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(th, pixel);
                                if (mUp && tk.Contains(mPtr)) mAux = bv ? (mAux & ~abits[bi]) : (mAux | abits[bi]);
                                rowY += rowH + 6f;
                            }
                            self.{{SCENE_STATE_FIELD}} = new Vector2(self.{{SCENE_STATE_FIELD}}.x, -AuxStateMarker - (float)mAux);
                        }
                        else if (activeTab == 3)
                        {
                            int[] rbits = new int[8];
                            rbits[0] = R8NoFog;
                            rbits[1] = R8FastCrouch;
                            rbits[2] = R8FastRevive;
                            rbits[3] = R8SkillCD;
                            rbits[4] = R8Chams;
                            rbits[5] = R8FastLoot;
                            rbits[6] = R8CamHack;
                            rbits[7] = R8UnlockFps;

                            string[] rlabels = new string[8];
                            rlabels[0] = "Xóa Sương Mù Bản Đồ";
                            rlabels[1] = "Ngồi Trượt Siêu Tốc";
                            rlabels[2] = "Cứu Đồng Đội Tức Thì";
                            rlabels[3] = "Giảm Hồi Chiêu Kỹ Năng";
                            rlabels[4] = "Hiệu Ứng Xuyên Tường (Chams)";
                            rlabels[5] = "Tự Động Nhặt Đồ Nhanh";
                            rlabels[6] = "Góc Nhìn Camera Hack";
                            rlabels[7] = "Mở Khóa FPS Cao (120)";

                            for (int bi = 0; bi < rbits.Length; bi++)
                            {
                                bool bv = (mR8 & rbits[bi]) != 0;
                                Rect row = new Rect(cx0, rowY, cw, rowH);
                                GUI.color = Color.white;
                                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                                GUI.Label(new Rect(row.x + 8f, row.y, row.width - 90f, row.height), rlabels[bi]);
                                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                                Rect tk = new Rect(row.x + row.width - 75f, row.y + 7f, 64f, 28f);
                                GUI.color = bv ? new Color(0.9f, 0.15f, 0.15f, 1f) : new Color(0.3f, 0.3f, 0.38f, 1f);
                                GUI.DrawTexture(tk, pixel);
                                float tw = 26f;
                                Rect th = bv ? new Rect(tk.x + tk.width - tw - 2f, tk.y + 2f, tw, 24f) : new Rect(tk.x + 2f, tk.y + 2f, tw, 24f);
                                GUI.color = Color.white;
                                GUI.DrawTexture(th, pixel);
                                if (mUp && tk.Contains(mPtr)) mR8 = bv ? (mR8 & ~rbits[bi]) : (mR8 | rbits[bi]);
                                rowY += rowH + 6f;
                            }
                            PlayerPrefs.SetFloat("esp_r8", (float)mR8);
                            PlayerPrefs.Save();
                        }
                        else if (activeTab == 4)
                        {
                            GUI.color = Color.white;
                            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                            GUI.Label(new Rect(cx0, rowY + 40f, cw, 40f), "VN TOOL - Hệ thống hỗ trợ tối ưu hóa game mượt mà.");
                            GUI.Label(new Rect(cx0, rowY + 80f, cw, 40f), "Mọi cài đặt đã được tối ưu hóa tiêu chuẩn cao nhất.");
                            GUI.skin.label.alignment = TextAnchor.UpperLeft;
                        }
                    }
                }
            }
            catch (Exception) { }

            GUI.matrix = savedMatrix;
            GUI.color = savedColor;
        }

        public static void UpdateSpeed(PlayerAttributes attributes, bool speedRunning, ref int auxState)
        {
            if (speedRunning)
            {
                attributes.SetSpecialRunSpeedScaleByKeyAndValue(
                    PlayerAttributes.{{SPEED_TYPE}}.BuffSystem,
                    SpeedRunningKey, 3f);
                auxState |= AuxSpeedRunningApplied;
            }
            else
            {
                attributes.RemoveSpecialRunSpeedScaleByKey(
                    PlayerAttributes.{{SPEED_TYPE}}.BuffSystem,
                    SpeedRunningKey);
                auxState &= ~AuxSpeedRunningApplied;
            }
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
                if (silentAimOn) { aimAtHead = true; fovFilterPx = silentFovPx; }
                else
                {
                    int aimModeFov  = (state & AimModeMask) >> AimModeShift;
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
