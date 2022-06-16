using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Mirror;
using UnityEngine.UI;
using TMPro;

public class ThirdPersonController : NetworkBehaviour
{
    [Header("Initialization Agent")]
    private NavMeshAgent navMeshAgent;
    private Animator animator;

    [Header("Game Object Link")]
    [Tooltip("In game Room Player")]
    public GameObject NetworkRoomPlayer = null;
    public GameObject MainCamera = null;

    [Header("Player Settings")]
    [Tooltip("Use gamepad or joystic?")]
    [SerializeField] private bool isGamepad = true;
    [Tooltip("Move speed")]
    [SerializeField] private float moveSpeed = 20;
    [Tooltip("Sprint speed")]
    [SerializeField] private float sprintSpeed = 55;
    [Tooltip("Rotate speed")]
    [Range(0, 0.5f)]
    [SerializeField] private float rotationSmoothTime = 0.05f;
    [Tooltip("Speed change rate")]
    [SerializeField] private float speedChangeRate = 5;

    //  Player Var
    private float speed = 0;
    private float cameraEulerAnglesY;
    private float targetAngle = 0;
    private float turnSmoothVelocity;

    //  Player Animation Var
    private float animationSpeedBlend;
    private int animationPlayerSpeed;
    private int animationSpeeModificator;

    [Header("Cinemachine")]
    [Tooltip("Camera follow target (Special object)")]
    public GameObject CameraFollowTarget;
    [Tooltip("Camera follow target transform")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Camera up in degrees")]
    [SerializeField] private float topClamp = 90;
    [Tooltip("Camera down in degrees")]
    [SerializeField] private float bottomClamp = -40;

    [Header("Info UI")]
    [SerializeField] private GameObject infoCanvas = null;
    [SerializeField] private TMP_Text playerNameText = null;
    [SerializeField] private RawImage avatarImage = null;

    //  Camera rotation
    private float cameraYaw;
    private float cameraPitch;

    private const float threshold = 0.01f;

    //  Input
    private Vector2 lookInput = Vector2.zero;
    private Vector2 moveInput = Vector2.zero;
    private bool isRun = false;

    [Header("Var")]
    private NetworkRoomManager networkRoomManager = null;

    [SyncVar(hook = nameof(OnLobbySlotChanged))]
    [HideInInspector] public int LobbySlot = -1;

    private void Awake()
    {
        //  Assign agent var
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        //  Do not update orientation
        navMeshAgent.updateRotation = false;

        //  Set id for string animation
        animationPlayerSpeed = Animator.StringToHash("PlayerSpeed");
        animationSpeeModificator = Animator.StringToHash("AnimationSpeed");
    }

    [ServerCallback]
    private void Start()
    {
        //  If not hasAuthority or dedicated
        if (!hasAuthority || isServerOnly)
        {
            //  Enable info UI
            infoCanvas.SetActive(true);

            //  Set name
            playerNameText.text = NetworkRoomPlayer.GetComponent<NetworkPlayerInformation>().PlayerName;
            //  Set avatar
            avatarImage.texture = NetworkRoomPlayer.GetComponent<NetworkPlayerInformation>().PlayerAvatar;
        }
    }

    public override void OnStartAuthority()
    {
        //  Wait until LobbySlot sync
        StartCoroutine(waitUntilLobbySlotChanged());
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        Move();
    }

    [ClientCallback]
    private void LateUpdate()
    {
        CameraRotation();
    }

    private void Move()
    {
        //  Set the target speed required to achieve
        float _targetSpeed = isRun ? sprintSpeed : moveSpeed;

        //  If not input (Vector2.zero cheaper than magnitude or sqrMagnitude, may be need to check this)
        if (moveInput == Vector2.zero)
        {
            _targetSpeed = 0;
        }

        //  Velocity - https://docs.unity3d.com/ScriptReference/AI.NavMeshAgent-velocity.html, better description in CharacterController.velocity - https://docs.unity3d.com/ScriptReference/CharacterController-velocity.html
        float _currentMovementSpeed = new Vector3(navMeshAgent.velocity.x, 0, navMeshAgent.velocity.z).magnitude;

        //  Set offset
        float _movementSpeedOffset = 0.1f;

        //  Vector2.magnitude - https://docs.unity3d.com/ScriptReference/Vector2-magnitude.html
        float _inputMagnitude = isGamepad ? moveInput.magnitude : 1;

        //  Up or down speed to _targetSpeed
        if (_currentMovementSpeed < _targetSpeed - _movementSpeedOffset || _currentMovementSpeed > _targetSpeed + _movementSpeedOffset)
        {
            //  The interpolated float result between the two float values, Mathf.Lerp - https://docs.unity3d.com/ScriptReference/Mathf.Lerp.html
            speed = Mathf.Lerp(_currentMovementSpeed, _targetSpeed * _inputMagnitude, Time.fixedDeltaTime * speedChangeRate);

            //  Some smooth for speed Mathf.Round - https://docs.unity3d.com/ScriptReference/Mathf.Round.html
            speed = Mathf.Round(speed * 1000) / 1000;
        }
        else
        {
            speed = _targetSpeed;
        }

        //  Animation to speed
        animationSpeedBlend = Mathf.Lerp(animationSpeedBlend, _targetSpeed, Time.deltaTime * speedChangeRate);

        //  New direction need normalized or not, Vector3.normalized - https://docs.unity3d.com/ScriptReference/Vector3-normalized.html
        Vector3 _moveDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;

        //  If input (Vector2.zero cheaper than magnitude or sqrMagnitude, may be need to check this)
        if (moveInput != Vector2.zero)
        {
            //  If [ServerCallback] and hasAuthority means - is Host
            if (hasAuthority)
            {
                //  Angle calculation, Mathf.Atan2 - https://docs.unity3d.com/ScriptReference/Mathf.Atan2.html, convert to degrees by Mathf.Rad2Deg - https://docs.unity3d.com/ScriptReference/Mathf.Rad2Deg.html and all this by Camera angle
                targetAngle = Mathf.Atan2(_moveDirection.x, _moveDirection.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            }
            else
            {
                //  Angle calculation, Mathf.Atan2 - https://docs.unity3d.com/ScriptReference/Mathf.Atan2.html, convert to degrees by Mathf.Rad2Deg - https://docs.unity3d.com/ScriptReference/Mathf.Rad2Deg.html and all this by Camera angle
                targetAngle = Mathf.Atan2(_moveDirection.x, _moveDirection.z) * Mathf.Rad2Deg + cameraEulerAnglesY;
            }

            //  Angle smooth, Mathf.SmoothDampAngle - https://docs.unity3d.com/ScriptReference/Mathf.SmoothDampAngle.html, ref value
            float _smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);

            //  Rotate to CameraTransform.rotation
            transform.rotation = Quaternion.Euler(0f, _smoothAngle, 0);
        }

        //  Move forward - Camera view
        Vector3 _targetDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

        //  Move apply
        navMeshAgent.Move(_targetDirection.normalized * (speed * Time.fixedDeltaTime));

        //  Animation update        
        animator.SetFloat(animationPlayerSpeed, animationSpeedBlend);
        animator.SetFloat(animationSpeeModificator, _inputMagnitude);
    }

    private void CameraRotation()
    {
        //  Check for input sqrMagnitude - https://docs.unity3d.com/ScriptReference/Vector3-sqrMagnitude.html
        if (lookInput.sqrMagnitude >= threshold)
        {
            cameraYaw += lookInput.x * Time.fixedDeltaTime;
            cameraPitch += lookInput.y * Time.fixedDeltaTime;
        }

        //  Use simple clamp
        cameraYaw = SimpleClamp(cameraYaw, float.MinValue, float.MaxValue);
        cameraPitch = SimpleClamp(cameraPitch, bottomClamp, topClamp);

        //  Camera follow target
        CameraFollowTarget.transform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0);
    }

    #region Input

    public void OnLook(InputAction.CallbackContext _context)
    {
        //  If NetworkClient.active
        if (NetworkClient.active)
        {
            //  If isServer and hasAuthority
            if (isServer && hasAuthority)
            {
                //  Assign input
                lookInput = _context.ReadValue<Vector2>();
            }
            else if (!isServer && isClient)  //  If isClient
            {
                //  Assign input
                lookInput = _context.ReadValue<Vector2>();

                //  Send input to isServer
                CmdSendCameraEulerY(cameraTransform.eulerAngles.y);
            }
        }
    }

    public void OnMove(InputAction.CallbackContext _context)
    {
        //  If NetworkClient.active
        if (NetworkClient.active)
        {
            //  If isServer and hasAuthority
            if (isServer && hasAuthority)
            {
                //  Assign input
                moveInput = _context.ReadValue<Vector2>();
            }
            else if (!isServer && isClient)  //  If isClient
            {
                //  Send input to isServer
                CmdSendMoveInputToServer(_context.ReadValue<Vector2>());
                CmdSendCameraEulerY(cameraTransform.eulerAngles.y);
            }
        }
    }

    public void OnSprint(InputAction.CallbackContext _context)
    {
        //  If NetworkClient.active
        if (NetworkClient.active)
        {
            //  If isServer and hasAuthority
            if (isServer && hasAuthority)
            {
                if (_context.ReadValue<float>() == 0)
                {
                    RunButtonPressed(false);
                }
                else
                {
                    RunButtonPressed(true);
                }
            }
            else if (!isServer && isClient)  //  If isClient send input to isServer
            {
                if (_context.ReadValue<float>() == 0)
                {
                    CmdSendIsRunInputToServer(false);
                }
                else
                {
                    CmdSendIsRunInputToServer(true);
                }
            }
        }
    }

    private void RunButtonPressed(bool _stateValue)
    {
        //  Change state
        isRun = _stateValue;
    }

    #endregion

    #region Void

    private void LinkPlayerAvatarWithRoomPlayer()
    {
        //  Check for lobby scene
        networkRoomManager = NetworkManager.singleton as NetworkRoomManager;

        //  Find connectionToServer and link
        foreach (var _roomPlayer in networkRoomManager.roomSlots)
        {
            //  If same LobbySlot
            if (_roomPlayer.GetComponent<NetworkPlayerInformation>().LobbySlot == LobbySlot)
            {
                //  Link
                _roomPlayer.GetComponent<NetworkPlayerInformation>().NetworkGamePlayerAvatar = this.gameObject;
                NetworkRoomPlayer = _roomPlayer.gameObject;

                //  If no hasAuthority
                if (!_roomPlayer.hasAuthority)
                {
                    //  Enable info UI
                    infoCanvas.SetActive(true);

                    //  Cache
                    NetworkPlayerInformation _networkPlayerInformation = NetworkRoomPlayer.GetComponent<NetworkPlayerInformation>();

                    //  Set name
                    playerNameText.text = _networkPlayerInformation.PlayerName;
                    //  Set avatar
                    avatarImage.texture = _networkPlayerInformation.PlayerAvatar;
                }

                //  Already found
                break;
            }
        }
    }

    private IEnumerator waitUntilLobbySlotChanged()
    {
        //  Wait for LobbySlot sync from server
        yield return new WaitUntil(() => LobbySlot != -1);

        //  Start initialization
        Initialization();
    }

    private void Initialization()
    {
        //  Assign MainCamera (also is GamePlayer)
        MainCamera = NetworkRoomPlayer.GetComponent<NetworkPlayerInformation>().NetworkGamePlayer;
        //  Link MainCamera transform
        cameraTransform = MainCamera.transform;

        //  Cache NetworkGamePlayer
        NetworkGamePlayer _networkGamePlayer = MainCamera.GetComponent<NetworkGamePlayer>();

        //  Activate Camera stuff
        CameraFollowTarget.SetActive(true);
        //  Start Player initialization
        _networkGamePlayer.Initialization();

        //  Input initialization   
        GetComponent<PlayerInput>().enabled = true;
    }

    #endregion

    #region Hook

    public void OnLobbySlotChanged(int _oldLobbySlot, int _newLobbySlot)
    {
        //  If incorrect value - Return
        if (_newLobbySlot <= -1)
        {
            return;
        }

        //  If not linked
        if (NetworkRoomPlayer == null)
        {
            LinkPlayerAvatarWithRoomPlayer();
        }
    }

    #endregion

    #region Command

    [Command]
    private void CmdSendMoveInputToServer(Vector2 _input)
    {
        //  Assign input value
        moveInput = _input;
    }

    [Command]
    private void CmdSendCameraEulerY(float _cameraEulerY)
    {
        //  Assign cameraEulerAnglesY value
        cameraEulerAnglesY = _cameraEulerY;
    }

    [Command]
    private void CmdSendIsRunInputToServer(bool _isRun)
    {
        //  Set input state
        RunButtonPressed(_isRun);
    }

    #endregion

    #region Utils

    //  Simple clamp angle
    private static float SimpleClamp(float _angle, float _min, float _max)
    {
        if (_angle < -360f)
        {
            _angle += 360f;
        }

        if (_angle > 360f)
        {
            _angle -= 360f;
        }

        return Mathf.Clamp(_angle, _min, _max);
    }

    #endregion

}
