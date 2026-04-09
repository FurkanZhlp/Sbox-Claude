using Sandbox;

namespace HorrorGame;

/// <summary>
/// First-person player controller for the horror game example.
///
/// Attach this to the Player GameObject alongside a CameraComponent on a
/// child object. The component reads input every Update() and applies movement
/// through the CharacterController.
///
/// This is a starter template — fill in the TODOs as you build out the game.
/// </summary>
public sealed class PlayerController : Component
{
    // ── Tunable properties ────────────────────────────────────────────────

    [Property, Range(50f, 250f)]
    public float WalkSpeed { get; set; } = 180f;

    [Property, Range(30f, 120f)]
    public float CrouchSpeed { get; set; } = 90f;

    [Property, Range(0.1f, 1f)]
    public float CrouchHeightScale { get; set; } = 0.5f;

    [Property]
    public float MouseSensitivity { get; set; } = 0.15f;

    /// <summary>Camera child object whose angles control look direction.</summary>
    [Property]
    public GameObject? CameraObject { get; set; }

    // ── Runtime state ─────────────────────────────────────────────────────

    private CharacterController? _cc;
    private bool _isCrouching;
    private bool _frozen;

    private Angles _eyeAngles;

    // ── Component lifecycle ───────────────────────────────────────────────

    protected override void OnStart()
    {
        _cc = Components.Get<CharacterController>();
        if (_cc is null)
            Log.Warning($"[PlayerController] No CharacterController found on {GameObject.Name}");

        // Lock cursor for first-person look
        Mouse.Visible = false;
        Mouse.LockMode = MouseLockMode.Locked;
    }

    protected override void OnUpdate()
    {
        if (_frozen) return;

        HandleLook();
        HandleMovement();
        HandleCrouch();
    }

    // ── Input handlers ────────────────────────────────────────────────────

    private void HandleLook()
    {
        _eyeAngles.pitch = MathX.Clamp(_eyeAngles.pitch - Input.MouseDelta.y * MouseSensitivity, -80f, 80f);
        _eyeAngles.yaw  -= Input.MouseDelta.x * MouseSensitivity;

        WorldRotation = Rotation.From(0f, _eyeAngles.yaw, 0f);

        if (CameraObject is not null)
            CameraObject.LocalRotation = Rotation.From(_eyeAngles.pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        if (_cc is null) return;

        var speed = _isCrouching ? CrouchSpeed : WalkSpeed;
        var wishDir = BuildWishDir();

        _cc.Velocity = _cc.Velocity.WithZ(0f) * 0.85f + wishDir * speed;
        _cc.Move();
    }

    private Vector3 BuildWishDir()
    {
        var fwd   = Input.Down("Forward")  ? 1f : 0f;
        var back  = Input.Down("Backward") ? 1f : 0f;
        var left  = Input.Down("Left")     ? 1f : 0f;
        var right = Input.Down("Right")    ? 1f : 0f;

        var dir = new Vector3(fwd - back, right - left, 0f).Normal;
        return WorldRotation * dir;
    }

    private void HandleCrouch()
    {
        if (Input.Pressed("Crouch"))
            SetCrouch(true);
        else if (Input.Released("Crouch"))
            SetCrouch(false);
    }

    private void SetCrouch(bool crouch)
    {
        _isCrouching = crouch;

        // Scale the capsule so the player fits under low geometry
        var scale = WorldScale;
        scale.z = crouch ? CrouchHeightScale : 1f;
        WorldScale = scale;
    }

    // ── Public API used by scripted events / GameManager ─────────────────

    /// <summary>
    /// Freeze the player in place (e.g. during a cutscene or jumpscare).
    /// </summary>
    public void Freeze() => _frozen = true;

    /// <summary>
    /// Release a previously applied freeze.
    /// </summary>
    public void Unfreeze() => _frozen = false;

    /// <summary>
    /// Force the player into crouch-and-freeze state used by hide-in-closet
    /// scripted sequences.
    /// </summary>
    public void Hide()
    {
        SetCrouch(true);
        Freeze();
    }
}
