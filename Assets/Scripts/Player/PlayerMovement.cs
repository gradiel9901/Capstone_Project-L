using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float turnSpeed = 15f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Interaction Settings")]
    public Transform handContainer;
    public float interactionRange = 2.0f;
    public LayerMask weaponLayer;

    [Header("Weapon Adjustments")]
    public Vector3 weaponRotationOffset;
    public Vector3 weaponPositionOffset;

    [Header("Combat Settings")]
    public float comboResetTime = 2.0f;
    public float attackRate = 0.5f; // Minimum time between clicks

    [Header("References")]
    public Transform playerCamera;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private bool isGrounded;
    private PlayerControls inputActions;
    private Vector2 moveInput;
    private GameObject currentWeapon;

    private int comboCounter = 0;
    private float lastClickedTime = 0;
    private float nextAttackTime = 0;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        inputActions = new PlayerControls();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Update()
    {
        if (currentWeapon != null)
        {
            currentWeapon.transform.localPosition = weaponPositionOffset;
            currentWeapon.transform.localRotation = Quaternion.Euler(weaponRotationOffset);
        }

        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        bool jumpTriggered = inputActions.Player.Jump.triggered;

        bool fKeyPressed = false;
        if (Keyboard.current != null)
        {
            fKeyPressed = Keyboard.current.fKey.wasPressedThisFrame;
        }

        if (fKeyPressed)
        {
            TryEquipWeapon();
        }

        // ATTACK LOGIC
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && currentWeapon != null)
        {
            // Only allow attack if enough time has passed since the last one
            if (Time.time >= nextAttackTime)
            {
                PerformAttack();
            }
        }

        if (Time.time - lastClickedTime > comboResetTime)
        {
            comboCounter = 0;
            if (animator != null) animator.SetInteger("ComboCount", 0);
        }

        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector3 camForward = playerCamera.forward;
        Vector3 camRight = playerCamera.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;

        if (moveDir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", moveInput.magnitude);
            animator.SetBool("IsGrounded", isGrounded);
        }

        controller.Move(moveDir * moveSpeed * Time.deltaTime);

        if (jumpTriggered && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (animator != null) animator.SetTrigger("Jump");
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void PerformAttack()
    {
        lastClickedTime = Time.time;
        nextAttackTime = Time.time + attackRate; // Set the cooldown

        comboCounter++;

        if (comboCounter > 5)
        {
            comboCounter = 1;
        }

        if (animator != null)
        {
            animator.SetInteger("ComboCount", comboCounter);
            animator.SetTrigger("OnAttack");
        }
    }

    private void TryEquipWeapon()
    {
        if (currentWeapon != null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRange, weaponLayer);

        if (hits.Length > 0)
        {
            GameObject weapon = hits[0].gameObject;
            Equip(weapon);
        }
    }

    private void Equip(GameObject weapon)
    {
        currentWeapon = weapon;
        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider col = weapon.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        weapon.transform.SetParent(handContainer);

        weapon.transform.localPosition = weaponPositionOffset;
        weapon.transform.localRotation = Quaternion.Euler(weaponRotationOffset);
    }
}