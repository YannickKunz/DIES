using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal; // Required for Light2D
using UnityEngine.UI; // Required for UI Image

public class PlayerHealth : MonoBehaviour
{
    // ... (Keep existing Health, Flashlight Attack, UI, Debug variables) ...
    [Header("Health Settings")]
    public string deathSceneName = "DeathScene";

    [Header("Flashlight Attack Settings")]
    public FlashlightDamager flashlightDamager;
    public Light2D lightVisual; // The Light2D component to rotate
    public string attackInputName = "Fire1";
    public float maxLightCharge = 100f;
    public float chargeConsumptionRate = 20f;
    public float chargeRegenRate = 10f;
    public Color normalColor = Color.white;
    public Color attackColor = Color.yellow;
    [Tooltip("Optional offset for the calculated rotation angle in degrees. Adjust if the light doesn't point correctly.")]
    public float rotationOffset = -90f; // Often needed because Atan2 calculates from positive X-axis

    [Header("UI Settings")]
    public Image lightGaugeFillImage;

    [Header("Debug")]
    [SerializeField] private bool isDead = false;
    [SerializeField] private float currentLightCharge;
    private bool isActivelyDamaging = false;

    // References
    private PlayerController playerController;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera; // Cached reference to the main camera

    void Awake()
    {
        Debug.Log("--- PlayerHealth AWAKE ---");
        playerController = GetComponent<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentLightCharge = maxLightCharge;

        // --- Cache the main camera ---
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("PlayerHealth: Could not find main camera! Mouse rotation will not work.", this);
        }

        // Check other references
        if (lightVisual == null) { Debug.LogError("PH AWAKE: Light2D NULL! Assign in Inspector.", this); }
        else { lightVisual.color = normalColor; }
        if (flashlightDamager == null) { Debug.LogError("PH AWAKE: FlashlightDamager NULL! Assign in Inspector.", this); }
        if (string.IsNullOrEmpty(attackInputName)) { attackInputName = "Fire1"; }
        if (lightGaugeFillImage == null) { Debug.LogWarning("PH AWAKE: Light Gauge Fill Image not assigned.", this); }

        UpdateLightGaugeUI();
    }

    void Update()
    {
        // Exit conditions
        if (isDead || !enabled) return;

        // Keep attack/gauge logic in Update as it affects gameplay state
        HandleFlashlightInputAndGauge();
        UpdateLightGaugeUI();
    }

    // --- Use LateUpdate for visual adjustments like rotation ---
    // This runs after all Update calls, ensuring the player/camera position is final for the frame.
    void LateUpdate()
    {
        // Exit conditions (check again in case state changed mid-frame)
        if (isDead || !enabled) return;

        RotateFlashlightTowardsMouse();
    }

    void HandleFlashlightInputAndGauge()
    {
        // Check required components are assigned
        if (flashlightDamager == null || lightVisual == null) return;

        bool playerIsTryingToAttack = Input.GetButton(attackInputName);
        bool canDamage = playerIsTryingToAttack && currentLightCharge > 0;

        if (canDamage)
        {
            currentLightCharge -= chargeConsumptionRate * Time.deltaTime;
            currentLightCharge = Mathf.Max(currentLightCharge, 0f);
            isActivelyDamaging = true;
            if (lightVisual.color != attackColor) { lightVisual.color = attackColor; }
            flashlightDamager.ProcessDamageTick();
        }
        else
        {
            isActivelyDamaging = false;
            if (currentLightCharge < maxLightCharge)
            {
                currentLightCharge += chargeRegenRate * Time.deltaTime;
                currentLightCharge = Mathf.Min(currentLightCharge, maxLightCharge);
            }
            if (lightVisual.color != normalColor) { lightVisual.color = normalColor; }
        }
    }

    // --- NEW Function for Rotation ---
    void RotateFlashlightTowardsMouse()
    {
        // Ensure we have references needed for rotation
        if (lightVisual == null || mainCamera == null)
        {
            return;
        }

        // 1. Get Mouse Position in Screen Coordinates
        Vector3 mouseScreenPos = Input.mousePosition;

        // 2. Get Flashlight Origin Position in World Coordinates
        Vector3 lightOriginPos = lightVisual.transform.position;

        // 3. Convert Mouse Screen Position to World Position
        // We need to provide a Z-depth. Using the distance from the camera to the light's origin
        // ensures the conversion happens on the same plane as the light.
        mouseScreenPos.z = mainCamera.WorldToScreenPoint(lightOriginPos).z;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);

        // 4. Calculate Direction Vector
        // Direction from the light's origin TO the mouse world position
        Vector2 direction = (Vector2)(mouseWorldPos - lightOriginPos); // Cast to Vector2 to ignore Z

        // 5. Calculate Angle
        // Atan2 gives the angle in radians from the positive X axis to the direction vector
        float angleRadians = Mathf.Atan2(direction.y, direction.x);

        // Convert to degrees
        float angleDegrees = angleRadians * Mathf.Rad2Deg;

        // 6. Apply Rotation
        // We use Quaternion.Euler to create a rotation around the Z axis.
        // Add the offset to align the light's "forward" direction correctly.
        lightVisual.transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees + rotationOffset);
    }

    // --- UI Update Function ---
    void UpdateLightGaugeUI()
    {
        if (lightGaugeFillImage != null)
        {
            float fillAmount = GetCurrentChargeNormalized();
            lightGaugeFillImage.fillAmount = fillAmount;
        }
    }

    // --- Other Functions (Remain the same) ---
    void OnTriggerEnter2D(Collider2D other) { if (isDead) return; if (other.gameObject.CompareTag("Ghost")) { Die(); } }
    private void Die() { /* ... as before ... */ if (isDead) return; isDead = true; Debug.Log("Player Died! Initiating death sequence."); if (playerController != null) { playerController.enabled = false; } isActivelyDamaging = false; if (lightVisual != null) lightVisual.color = normalColor; Rigidbody2D rb = GetComponent<Rigidbody2D>(); if (rb != null) { rb.linearVelocity = Vector2.zero; } LoadDeathScene(); }
    private void LoadDeathScene() { /* ... as before ... */ Debug.Log("Loading Scene: " + deathSceneName); SceneManager.LoadScene(deathSceneName); }
    public float GetCurrentChargeNormalized() { /* ... as before ... */ if (maxLightCharge <= 0f) { return 0f; } return Mathf.Clamp01(currentLightCharge / maxLightCharge); }
}