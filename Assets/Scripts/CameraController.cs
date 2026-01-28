using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private KartController kart;

    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 8f, -12f);
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Dynamic Camera")]
    [SerializeField] private float boostFOVIncrease = 15f;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float fovChangeSpeed = 5f;

    [Header("Look Ahead")]
    [SerializeField] private float lookAheadDistance = 5f;
    [SerializeField] private float lookAheadSpeed = 3f;

    private Camera cam;
    private Vector3 currentVelocity;
    private float currentLookAhead = 0f;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                kart = player.GetComponent<KartController>();
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        FollowTarget();
        UpdateFOV();
    }

    private void FollowTarget()
    {
        // Calculate look ahead based on speed
        float targetLookAhead = 0f;
        if (kart != null)
        {
            targetLookAhead = (kart.CurrentSpeed / kart.MaxSpeed) * lookAheadDistance;
        }
        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, lookAheadSpeed * Time.deltaTime);

        // Calculate desired position
        Vector3 lookAheadPos = target.position + target.forward * currentLookAhead;
        Quaternion targetRotation = Quaternion.LookRotation(target.forward);
        Vector3 desiredPosition = lookAheadPos + targetRotation * offset;

        // Smooth follow
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / followSpeed);

        // Look at target
        Vector3 lookTarget = target.position + target.forward * (currentLookAhead * 0.5f) + Vector3.up * 2f;
        Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
    }

    private void UpdateFOV()
    {
        if (cam == null || kart == null) return;

        float targetFOV = normalFOV;

        if (kart.IsBoosting)
        {
            targetFOV = normalFOV + boostFOVIncrease;
        }

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovChangeSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        kart = newTarget?.GetComponent<KartController>();
    }
}
