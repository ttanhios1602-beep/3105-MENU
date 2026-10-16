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
