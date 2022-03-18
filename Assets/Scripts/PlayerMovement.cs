using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;
using Unity.RenderStreaming;

[RequireComponent(typeof(InputChannelReceiverBase))]
public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform playerCamera;

    public float speed = 12f;
    public float runRatio = 2.5f;
    public float gravity = -9.81f;

    Vector3 velocity;
    bool isGrounded;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    public float jumpHeight = 3f;

    // Inputs
    public float x;
    public float z;
    public bool jump;
    public float run;


    [SerializeField] private InputChannelReceiverBase receiver;

    private List<Gamepad> listGamepad = new List<Gamepad>();
    private List<Keyboard> listKeyboard = new List<Keyboard>();
    private List<Mouse> listMouse = new List<Mouse>();
    private List<Gyroscope> listGyroscpe = new List<Gyroscope>();
    private List<TrackedDevice> listTracker = new List<TrackedDevice>();
    private List<Touchscreen> listScreen = new List<Touchscreen>();

    [Header("Rotation Settings")]
    [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
    [SerializeField]
    private AnimationCurve mouseSensitivityCurve = new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (receiver == null)
            receiver = GetComponent<InputChannelReceiverBase>();
        receiver.onDeviceChange += OnDeviceChange;

        EnhancedTouchSupport.Enable();
    }
    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        Debug.Log("DEVICE CHANGED");
        switch (change)
        {
            case InputDeviceChange.Added:
                SetDevice(device);
                return;
            case InputDeviceChange.Removed:
                SetDevice(device, false);
                return;
        }
    }

    void SetDevice(InputDevice device, bool add = true)
    {

        switch (device)
        {
            case Mouse mouse:
                if (add)
                    listMouse.Add(mouse);
                else
                    listMouse.Remove(mouse);
                return;
            case Keyboard keyboard:
                if (add)
                    listKeyboard.Add(keyboard);
                else
                    listKeyboard.Remove(keyboard);
                return;
            case Touchscreen screen:
                if (add)
                    listScreen.Add(screen);
                else
                    listScreen.Remove(screen);
                return;
            case Gamepad pad:
                if (add)
                    listGamepad.Add(pad);
                else
                    listGamepad.Remove(pad);
                return;
            case Gyroscope gyroscope:
                if (add)
                    listGyroscpe.Add(gyroscope);
                else
                    listGyroscpe.Remove(gyroscope);
                return;
            case TrackedDevice tracker:
                if (add)
                    listTracker.Add(tracker);
                else
                    listTracker.Remove(tracker);
                return;
#if URS_USE_AR_SUBSYSTEMS
                case HandheldARInputDevice handheld:
                    if (add)
                        listHandheld.Add(handheld);
                    else
                        listHandheld.Remove(handheld);
                    return;
#endif
        }
    }

    Vector3 GetInputTranslationDirection()
    {
        Vector3 direction = new Vector3();

        // keyboard control
        foreach (var keyboard in listKeyboard)
        {
            if (keyboard.wKey.isPressed)
            {
                direction += Vector3.forward;
            }
            if (keyboard.sKey.isPressed)
            {
                direction += Vector3.back;
            }
            if (keyboard.aKey.isPressed)
            {
                direction += Vector3.left;
            }
            if (keyboard.dKey.isPressed)
            {
                direction += Vector3.right;
            }

            direction = direction.normalized;

            if (keyboard.spaceKey.isPressed)
            {
                direction += Vector3.up;
            }
        }

        return direction;
    }


    private void Start()
    {

    }

    float GetXInput()
    {
        float value = Input.GetAxis("Horizontal");
        return value;
    }

    float GetYInput()
    {
        float value = Input.GetAxis("Vertical");
        return value;
    }

    bool GetJumpInput()
    {
        bool value = Input.GetButtonDown("Jump");
        return value;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(groundCheck.position, groundDistance);
    }

    void UpdateTargetCameraStateFromInput(Vector2 input)
    {
        float mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(input.magnitude);

        //transform.rotation.eulerAngles += input.x * mouseSensitivityFactor;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + input.x * mouseSensitivityFactor, transform.eulerAngles.z);

        float i = input.y;

        if (Mathf.Abs(i) > 40)
        {
            i = 40 * Mathf.Sign(i);
        }

        float t = playerCamera.localEulerAngles.x + (-i) * mouseSensitivityFactor;

        Debug.Log(t);

        if (t < 320 && t > 180)
        {
            t = 320;
        }

        if(t > 40 && t < 180)
        {
            t = 40;
        }

        if(t > 45 && t < 315)
        {
            t = 0;
        }

        playerCamera.localEulerAngles = new Vector3(t, playerCamera.localEulerAngles.y, playerCamera.localEulerAngles.z);

        //m_TargetCameraState.yaw += input.x * mouseSensitivityFactor;
        //m_TargetCameraState.pitch += input.y * mouseSensitivityFactor;
    }

    // Update is called once per frame
    void Update()
    {

        // Rotation
        foreach (var mouse in listMouse)
        {
            //if (IsMouseDragged(mouse, false))
            {
                UpdateTargetCameraStateFromInput(mouse.delta.ReadValue());
            }
        }

        Vector3 moveVector = new Vector3();

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        Vector3 getMovementInput = GetInputTranslationDirection();

        x = getMovementInput.x;
        z = getMovementInput.z;
        jump = getMovementInput.y > 0;

        // X, Z movement
        Vector3 move = transform.right * x + transform.forward * z;

        if (move.magnitude > 1)
            move = move.normalized;

        move = move * speed;
     
        moveVector += move * Time.deltaTime;

        // Y velocity reset
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Jump
        if (jump && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        moveVector += velocity * Time.deltaTime;

        // Update movement
        controller.Move(moveVector);
    }

    static bool IsMouseDragged(Mouse m, bool useLeftButton)
    {
        if (null == m)
            return false;

        if (Screen.safeArea.Contains(m.position.ReadValue()))
        {
            //check left/right click
            if ((useLeftButton && m.leftButton.isPressed) || (!useLeftButton && m.rightButton.isPressed))
            {
                return true;
            }
        }

        return false;
    }
}
