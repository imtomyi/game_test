using UnityEngine;
using System;

public class KartController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float brakeForce = 12f;
    [SerializeField] private float reverseSpeed = 5f;
    [SerializeField] private float friction = 2f;

    [Header("Steering Settings")]
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float driftTurnMultiplier = 1.4f;

    [Header("Drift Settings")]
    [SerializeField] private float driftDrag = 0.98f;
    [SerializeField] private float driftChargeRate = 35f; // Percent per second
    [SerializeField] private float minSpeedToDrift = 5f;

    [Header("Boost Settings")]
    [SerializeField] private float boostSpeedMultiplier = 1.8f;
    [SerializeField] private float boostDuration = 3f;
    [SerializeField] private int maxBoosterSlots = 2;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem driftSparksLeft;
    [SerializeField] private ParticleSystem driftSparksRight;
    [SerializeField] private ParticleSystem boostFlames;
    [SerializeField] private TrailRenderer[] tireTrails;

    [Header("Audio")]
    [SerializeField] private AudioSource engineSound;
    [SerializeField] private AudioSource driftSound;
    [SerializeField] private AudioSource boostSound;

    // Current state
    private float currentSpeed = 0f;
    private float currentTurnAngle = 0f;
    private bool isDrifting = false;
    private float driftDirection = 0f; // -1 left, 1 right

    // Booster system
    private float driftChargePercent = 0f;
    private int storedBoosters = 0;
    private bool isBoosting = false;
    private float boostTimer = 0f;

    // Components
    private Rigidbody rb;

    // Events
    public event Action<float> OnDriftChargeChanged;
    public event Action<int> OnBoosterCountChanged;
    public event Action<bool> OnBoostStateChanged;
    public event Action<float> OnSpeedChanged;

    // Properties
    public float CurrentSpeed => currentSpeed;
    public float MaxSpeed => maxSpeed;
    public bool IsDrifting => isDrifting;
    public bool IsBoosting => isBoosting;
    public float DriftChargePercent => driftChargePercent;
    public int StoredBoosters => storedBoosters;
    public int MaxBoosterSlots => maxBoosterSlots;

    private bool canControl = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Configure rigidbody for kart physics
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.mass = 1000f;
        rb.drag = 0.5f;
        rb.angularDrag = 5f;
    }

    private void Start()
    {
        // Initialize effects as disabled
        SetDriftEffects(false);
        SetBoostEffects(false);
    }

    private void Update()
    {
        if (!canControl) return;

        HandleInput();
        UpdateBoostTimer();
        UpdateEffects();

        // Notify UI of speed changes
        OnSpeedChanged?.Invoke(currentSpeed);
    }

    private void FixedUpdate()
    {
        if (!canControl) return;

        ApplyMovement();
        ApplySteering();
        ApplyFriction();
    }

    private void HandleInput()
    {
        // Acceleration / Brake
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");

        // Drift input (Left Shift)
        bool driftInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Boost activation (/ key)
        if (Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.KeypadDivide))
        {
            TryActivateBoost();
        }

        // Calculate target speed
        float effectiveMaxSpeed = isBoosting ? maxSpeed * boostSpeedMultiplier : maxSpeed;

        if (verticalInput > 0)
        {
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, effectiveMaxSpeed);
        }
        else if (verticalInput < 0)
        {
            if (currentSpeed > 0)
            {
                // Braking
                currentSpeed -= brakeForce * Time.deltaTime;
                currentSpeed = Mathf.Max(currentSpeed, 0);
            }
            else
            {
                // Reversing
                currentSpeed -= acceleration * 0.5f * Time.deltaTime;
                currentSpeed = Mathf.Max(currentSpeed, -reverseSpeed);
            }
        }

        // Handle drifting
        HandleDrift(driftInput, horizontalInput);

        // Store turn input
        currentTurnAngle = horizontalInput;
    }

    private void HandleDrift(bool driftInput, float horizontalInput)
    {
        bool canDrift = currentSpeed >= minSpeedToDrift && Mathf.Abs(horizontalInput) > 0.1f;

        if (driftInput && canDrift)
        {
            if (!isDrifting)
            {
                // Start drifting
                isDrifting = true;
                driftDirection = Mathf.Sign(horizontalInput);
                SetDriftEffects(true);

                if (driftSound != null)
                    driftSound.Play();
            }

            // Charge booster while drifting
            if (storedBoosters < maxBoosterSlots)
            {
                driftChargePercent += driftChargeRate * Time.deltaTime;

                if (driftChargePercent >= 100f)
                {
                    // Store a booster
                    storedBoosters++;
                    driftChargePercent = 0f;
                    OnBoosterCountChanged?.Invoke(storedBoosters);
                }

                OnDriftChargeChanged?.Invoke(driftChargePercent);
            }
        }
        else if (isDrifting)
        {
            // End drift
            EndDrift();
        }
    }

    private void EndDrift()
    {
        isDrifting = false;
        driftDirection = 0f;

        // Don't lose partial charge, keep it for next drift
        SetDriftEffects(false);

        if (driftSound != null)
            driftSound.Stop();
    }

    private void TryActivateBoost()
    {
        if (storedBoosters > 0 && !isBoosting)
        {
            storedBoosters--;
            isBoosting = true;
            boostTimer = boostDuration;

            OnBoosterCountChanged?.Invoke(storedBoosters);
            OnBoostStateChanged?.Invoke(true);
            SetBoostEffects(true);

            if (boostSound != null)
                boostSound.Play();
        }
    }

    private void UpdateBoostTimer()
    {
        if (isBoosting)
        {
            boostTimer -= Time.deltaTime;

            if (boostTimer <= 0)
            {
                isBoosting = false;
                boostTimer = 0f;
                OnBoostStateChanged?.Invoke(false);
                SetBoostEffects(false);
            }
        }
    }

    private void ApplyMovement()
    {
        // Move forward based on current speed
        Vector3 moveDirection = transform.forward * currentSpeed;

        if (isDrifting)
        {
            // Add sideways drift velocity
            Vector3 driftVelocity = transform.right * driftDirection * currentSpeed * 0.3f;
            moveDirection += driftVelocity;
            moveDirection *= driftDrag;
        }

        rb.velocity = new Vector3(moveDirection.x, rb.velocity.y, moveDirection.z);
    }

    private void ApplySteering()
    {
        if (Mathf.Abs(currentSpeed) < 0.5f) return;

        float turnMultiplier = isDrifting ? driftTurnMultiplier : 1f;
        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
        float turnAmount = currentTurnAngle * turnSpeed * turnMultiplier * speedFactor * Time.fixedDeltaTime;

        // Reverse steering direction when going backwards
        if (currentSpeed < 0)
            turnAmount = -turnAmount;

        transform.Rotate(0, turnAmount, 0);
    }

    private void ApplyFriction()
    {
        if (Mathf.Abs(currentSpeed) > 0.1f && currentTurnAngle == 0 && !Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.UpArrow))
        {
            // Apply friction when not accelerating
            float frictionAmount = friction * Time.fixedDeltaTime;

            if (currentSpeed > 0)
                currentSpeed = Mathf.Max(0, currentSpeed - frictionAmount);
            else
                currentSpeed = Mathf.Min(0, currentSpeed + frictionAmount);
        }
    }

    private void SetDriftEffects(bool active)
    {
        if (driftSparksLeft != null)
        {
            if (active && driftDirection < 0)
                driftSparksLeft.Play();
            else
                driftSparksLeft.Stop();
        }

        if (driftSparksRight != null)
        {
            if (active && driftDirection > 0)
                driftSparksRight.Play();
            else
                driftSparksRight.Stop();
        }

        // Tire trails
        if (tireTrails != null)
        {
            foreach (var trail in tireTrails)
            {
                if (trail != null)
                    trail.emitting = active;
            }
        }
    }

    private void SetBoostEffects(bool active)
    {
        if (boostFlames != null)
        {
            if (active)
                boostFlames.Play();
            else
                boostFlames.Stop();
        }
    }

    private void UpdateEffects()
    {
        // Update engine sound pitch based on speed
        if (engineSound != null)
        {
            float pitchRange = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
            engineSound.pitch = 0.8f + pitchRange * 0.8f;
        }
    }

    public void EnableControl(bool enable)
    {
        canControl = enable;

        if (!enable)
        {
            currentSpeed = 0;
            if (isDrifting) EndDrift();
        }
    }

    public void ResetKart(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        currentSpeed = 0;
        driftChargePercent = 0;
        storedBoosters = 0;
        isDrifting = false;
        isBoosting = false;

        if (rb != null)
            rb.velocity = Vector3.zero;

        OnDriftChargeChanged?.Invoke(0);
        OnBoosterCountChanged?.Invoke(0);
        OnBoostStateChanged?.Invoke(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Reduce speed on wall collision
            currentSpeed *= 0.3f;

            if (isBoosting)
            {
                isBoosting = false;
                boostTimer = 0;
                OnBoostStateChanged?.Invoke(false);
                SetBoostEffects(false);
            }
        }
    }
}
