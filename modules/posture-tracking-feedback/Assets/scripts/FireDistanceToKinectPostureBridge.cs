using UnityEngine;

public class FireDistanceToKinectPostureBridge : MonoBehaviour
{
    [Header("References")]
    public EmergencyScenarioController emergencyController;
    public KinectFirePostureEvaluator postureEvaluator;

    private void Start()
    {
        if (emergencyController == null)
        {
            emergencyController = FindObjectOfType<EmergencyScenarioController>();
        }

        if (postureEvaluator == null)
        {
            postureEvaluator = FindObjectOfType<KinectFirePostureEvaluator>();
        }

        if (postureEvaluator != null)
        {
            postureEvaluator.calculateDistanceHere = false;
        }
    }

    private void Update()
    {
        if (emergencyController == null || postureEvaluator == null)
        {
            return;
        }

        postureEvaluator.externalFireDistance = emergencyController.closestFireDistance;
    }
}