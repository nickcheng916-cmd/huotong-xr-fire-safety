using UnityEngine;

public enum EmergencyZoneType
{
    Smoke,
    Fire,
    SafeRoute,
    Exit
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class EmergencyZone : MonoBehaviour
{
    [Header("Zone")]
    public EmergencyZoneType zoneType = EmergencyZoneType.Smoke;
    public string zoneName = "Emergency Zone";

    [Header("Options")]
    public bool autoSetTrigger = true;

    [Tooltip("地面点判断容差。数值越大，越容易判定进入区域。")]
    public float groundCheckMargin = 0.05f;

    private Collider zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();

        if (zoneCollider != null && autoSetTrigger)
        {
            zoneCollider.isTrigger = true;
        }
    }

    public bool ContainsGroundPoint(Vector3 groundPoint)
    {
        Collider col = GetCollider();

        if (col == null || !col.enabled)
        {
            return false;
        }

        Vector3 checkPoint = groundPoint;
        checkPoint.y = col.bounds.center.y;

        Bounds expandedBounds = col.bounds;
        expandedBounds.Expand(groundCheckMargin * 2f);

        if (!expandedBounds.Contains(checkPoint))
        {
            return false;
        }

        Vector3 closest = col.ClosestPoint(checkPoint);
        float distance = Vector3.Distance(closest, checkPoint);

        return distance <= Mathf.Max(0.02f, groundCheckMargin);
    }

    public float DistanceToGroundPoint(Vector3 groundPoint)
    {
        Collider col = GetCollider();

        if (col == null || !col.enabled)
        {
            return float.PositiveInfinity;
        }

        Vector3 checkPoint = groundPoint;
        checkPoint.y = col.bounds.center.y;

        Vector3 closest = col.ClosestPoint(checkPoint);
        closest.y = checkPoint.y;

        return Vector3.Distance(checkPoint, closest);
    }

    private Collider GetCollider()
    {
        if (zoneCollider == null)
        {
            zoneCollider = GetComponent<Collider>();
        }

        return zoneCollider;
    }

    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();

        if (col == null)
        {
            return;
        }

        switch (zoneType)
        {
            case EmergencyZoneType.Smoke:
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.35f);
                break;

            case EmergencyZoneType.Fire:
                Gizmos.color = new Color(1f, 0.2f, 0.05f, 0.35f);
                break;

            case EmergencyZoneType.SafeRoute:
                Gizmos.color = new Color(0.1f, 0.7f, 1f, 0.35f);
                break;

            case EmergencyZoneType.Exit:
                Gizmos.color = new Color(0.1f, 1f, 0.2f, 0.35f);
                break;
        }

        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}