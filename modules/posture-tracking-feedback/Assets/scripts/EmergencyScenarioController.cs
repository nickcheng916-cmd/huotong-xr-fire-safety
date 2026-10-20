using System.Collections.Generic;
using UnityEngine;

public class EmergencyScenarioController : MonoBehaviour
{
    [Header("References")]
    public EmergencyMotionBridge motionBridge;

    [Header("Zones")]
    public bool autoFindZonesOnStart = true;
    public List<EmergencyZone> zones = new List<EmergencyZone>();

    [Header("Fire Detection")]
    [Tooltip("把火焰粒子、火焰模型、火焰根物体拖到这里。")]
    public List<Transform> firePoints = new List<Transform>();

    [Tooltip("如果 Fire Points 没有手动拖，会按名字自动找包含 Fire / Flame / huo / 火 的物体。")]
    public bool autoFindFirePointsByName = true;

    [Tooltip("距离火源点多近算进入红色火源危险区。")]
    public float fireInsideDistance = 0.8f;

    [Tooltip("距离火源点多近算进入橙色火源警告区。")]
    public float fireDangerDistance = 3.0f;

    [Header("Score Rules")]
    public int startScore = 100;
    public int currentScore = 100;
    public int orangeFirePenalty = 20;
    public int redFirePenalty = 30;

    [Tooltip("0 = Safe, 1 = Orange Zone, 2 = Red Zone")]
    public int currentFireRiskLevel = 0;

    private int lastFireRiskLevel = 0;

    [Header("Smoke Rules")]
    public bool requireHandNearFaceInSmoke = true;

    [Header("Route Rules")]
    public bool requireStayOnSafeRoute = false;

    [Header("Runtime State")]
    public Vector3 playerWorldGroundPosition;
    public Vector3 playerCalibratedPosition;

    public bool isInSmoke;
    public bool isNearFire;
    public bool isInsideFire;
    public bool isOnSafeRoute;
    public bool isAtExit;

    public bool handNearFace;
    public bool isWaving;

    public bool scenarioCompleted;
    public string runtimeMessage;

    [Header("Runtime Debug")]
    public float closestFireDistance = 999f;
    public string nearestFireName = "None";
    public int firePointCount;
    public int zoneCount;

    private string lastMessage;

    private void Start()
    {
        currentScore = startScore;
        currentFireRiskLevel = 0;
        lastFireRiskLevel = 0;

        if (autoFindZonesOnStart)
        {
            RefreshZones();
        }

        if (autoFindFirePointsByName)
        {
            AutoFindFirePoints();
        }
    }

    private void Update()
    {
        if (motionBridge == null)
        {
            runtimeMessage = "MotionBridge Missing.";
            return;
        }

        ReadMotionData();
        EvaluateScenario();
        ApplyFireScorePenalty();
        BuildRuntimeMessage();
        LogMessageIfChanged();
    }

    public void RefreshZones()
    {
        zones.Clear();

        EmergencyZone[] foundZones = FindObjectsOfType<EmergencyZone>();

        for (int i = 0; i < foundZones.Length; i++)
        {
            if (foundZones[i] != null)
            {
                zones.Add(foundZones[i]);
            }
        }

        zoneCount = zones.Count;

        Debug.Log("[EmergencyScenarioController] Zones found: " + zones.Count);
    }

    public void AutoFindFirePoints()
    {
        if (firePoints == null)
        {
            firePoints = new List<Transform>();
        }

        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject obj = allObjects[i];

            if (obj == null)
            {
                continue;
            }

            string lowerName = obj.name.ToLower();

            bool looksLikeFire =
                lowerName.Contains("fire") ||
                lowerName.Contains("flame") ||
                lowerName.Contains("huo") ||
                lowerName.Contains("火");

            if (!looksLikeFire)
            {
                continue;
            }

            if (!firePoints.Contains(obj.transform))
            {
                firePoints.Add(obj.transform);
            }
        }

        firePointCount = firePoints.Count;

        Debug.Log("[EmergencyScenarioController] Fire points found: " + firePointCount);
    }

    private void ReadMotionData()
    {
        playerCalibratedPosition = motionBridge.GetPlayerPosition();

        if (motionBridge.userPositionProvider != null)
        {
            playerWorldGroundPosition = motionBridge.userPositionProvider.RawUserGroundPosition;
        }
        else
        {
            playerWorldGroundPosition = playerCalibratedPosition;
        }

        playerWorldGroundPosition.y = 0f;

        handNearFace = motionBridge.IsHandNearFace();
        isWaving = motionBridge.IsWaving();
    }

    private void EvaluateScenario()
    {
        isInSmoke = false;
        isNearFire = false;
        isInsideFire = false;
        isAtExit = false;

        closestFireDistance = 999f;
        nearestFireName = "None";

        zoneCount = zones == null ? 0 : zones.Count;
        firePointCount = firePoints == null ? 0 : firePoints.Count;

        if (requireStayOnSafeRoute)
        {
            isOnSafeRoute = false;
        }
        else
        {
            isOnSafeRoute = true;
        }

        EvaluateEmergencyZones();
        EvaluateFirePoints();

        if (isAtExit)
        {
            scenarioCompleted = true;
        }
    }

    private void EvaluateEmergencyZones()
    {
        if (zones == null)
        {
            return;
        }

        for (int i = 0; i < zones.Count; i++)
        {
            EmergencyZone zone = zones[i];

            if (zone == null)
            {
                continue;
            }

            bool inside = zone.ContainsGroundPoint(playerWorldGroundPosition);
            float distance = zone.DistanceToGroundPoint(playerWorldGroundPosition);

            switch (zone.zoneType)
            {
                case EmergencyZoneType.Smoke:
                    if (inside)
                    {
                        isInSmoke = true;
                    }
                    break;

                case EmergencyZoneType.Fire:
                    if (distance < closestFireDistance)
                    {
                        closestFireDistance = distance;
                        nearestFireName = zone.name;
                    }

                    if (inside)
                    {
                        isInsideFire = true;
                    }

                    if (inside || distance <= fireDangerDistance)
                    {
                        isNearFire = true;
                    }
                    break;

                case EmergencyZoneType.SafeRoute:
                    if (inside)
                    {
                        isOnSafeRoute = true;
                    }
                    break;

                case EmergencyZoneType.Exit:
                    if (inside)
                    {
                        isAtExit = true;
                    }
                    break;
            }
        }
    }

    private void EvaluateFirePoints()
    {
        if (firePoints == null)
        {
            return;
        }

        for (int i = 0; i < firePoints.Count; i++)
        {
            Transform firePoint = firePoints[i];

            if (firePoint == null)
            {
                continue;
            }

            Vector3 fireGround = firePoint.position;
            fireGround.y = 0f;

            float distance = Vector3.Distance(playerWorldGroundPosition, fireGround);

            if (distance < closestFireDistance)
            {
                closestFireDistance = distance;
                nearestFireName = firePoint.name;
            }

            if (distance <= fireInsideDistance)
            {
                isInsideFire = true;
            }

            if (distance <= fireDangerDistance)
            {
                isNearFire = true;
            }
        }
    }

    private void ApplyFireScorePenalty()
    {
        if (isInsideFire)
        {
            currentFireRiskLevel = 2;
        }
        else if (isNearFire)
        {
            currentFireRiskLevel = 1;
        }
        else
        {
            currentFireRiskLevel = 0;
        }

        // 离开火焰区域后，允许下次再次进入时重新扣分
        if (currentFireRiskLevel == 0)
        {
            lastFireRiskLevel = 0;
            return;
        }

        // 只有从安全区进入橙色/红色，或者从橙色进入红色时才扣分
        // 避免每一帧都扣分
        if (currentFireRiskLevel > lastFireRiskLevel)
        {
            int penalty = 0;

            if (currentFireRiskLevel == 1)
            {
                penalty = orangeFirePenalty;
            }
            else if (currentFireRiskLevel == 2)
            {
                penalty = redFirePenalty;
            }

            currentScore -= penalty;

            if (currentScore < 0)
            {
                currentScore = 0;
            }

            Debug.Log(
                "[Score] Fire penalty -" + penalty +
                " | Score: " + currentScore +
                " | Risk Level: " + currentFireRiskLevel +
                " | Distance: " + closestFireDistance.ToString("F2") +
                " | Fire: " + nearestFireName
            );
        }

        lastFireRiskLevel = currentFireRiskLevel;
    }

    private void BuildRuntimeMessage()
    {
        if (scenarioCompleted)
        {
            runtimeMessage = "Escape Complete.";
            return;
        }

        if (isInsideFire)
        {
            runtimeMessage = "FIRE DANGER: Move away. Score -" + redFirePenalty;
            return;
        }

        if (isNearFire)
        {
            runtimeMessage = "Near Fire: Move away. Score -" + orangeFirePenalty;
            return;
        }

        if (isInSmoke)
        {
            if (requireHandNearFaceInSmoke)
            {
                if (handNearFace)
                {
                    runtimeMessage = "Smoke: Hand near face.";
                }
                else
                {
                    runtimeMessage = "Smoke: Raise hand to mouth.";
                }
            }
            else
            {
                runtimeMessage = "Smoke Area.";
            }

            return;
        }

        if (requireStayOnSafeRoute && !isOnSafeRoute)
        {
            runtimeMessage = "Route Warning: Off route.";
            return;
        }

        if (isWaving)
        {
            runtimeMessage = "Action: Waving detected.";
            return;
        }

        runtimeMessage = "Status OK: Continue route.";
    }

    private void LogMessageIfChanged()
    {
        if (runtimeMessage != lastMessage)
        {
            lastMessage = runtimeMessage;
            Debug.Log(
                "[EmergencyScenarioController] " +
                runtimeMessage +
                " | Closest Fire: " +
                closestFireDistance.ToString("F2") +
                " | Fire: " +
                nearestFireName +
                " | Score: " +
                currentScore
            );
        }
    }

    public Vector3 GetPlayerPosition()
    {
        return playerWorldGroundPosition;
    }

    public bool IsInSmoke()
    {
        return isInSmoke;
    }

    public bool IsNearFire()
    {
        return isNearFire;
    }

    public bool IsAtExit()
    {
        return isAtExit;
    }

    public string GetRuntimeMessage()
    {
        return runtimeMessage;
    }

    public int GetScore()
    {
        return currentScore;
    }
}