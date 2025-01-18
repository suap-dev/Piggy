using System;
using UnityEngine;

public enum CameraType
{
    ThirdPerson = 0,
    Isometric
}

struct InputData
{
    public Vector2 MouseDelta;
    public Vector2 DirectionalInput;
    public bool Jump;
}


public class PlayerCharacterController : MonoBehaviour
{
    public bool getRawInputData;

    [Header("Movement")] public float maxGroundSpeed = 10;
    public float jumpHeight = 0.5f;

    [Header("Mesh")]
    public Transform meshTransform;

    [Header("Camera")] public CameraType cameraType;
    public Transform cameraPivot;
    public Camera mainCamera;

    [Header("Third Person Camera")] public Vector3 thirdPersonCameraPivotOffset = new Vector3(0, 1.2f, 0);
    public float thirdPersonCameraYawSpeed = 5;
    public float thirdPersonCameraPitchSpeed = 2.5f;
    public float thirdPersonCameraMinPitch = -15;
    public float thirdPersonCameraMaxPitch = 45;
    public float thirdPersonCameraFov = 60;
    public float thirdPersonCameraDistance = 8;

    [Header("Isometric Camera")] public Vector3 isometricCameraPivotOffset = new Vector3(0, 0.5f, 0);
    public float isometricCameraForwardAngle = -45;
    public float isometricCameraPitch = 45;
    public float isometricCameraFov = 30;
    public float isometricCameraDistance = 12;

    private InputData _inputData;

    private Vector3 _worldDirection;
    private Vector3 _lastWorldDirectionAboveThreshold;
    private const float DirectionSqrMagnitudeThreshold = 0.01f;

    private Vector3 _velocity;

    private CameraType _currentCameraType;
    private float _currentCameraPitch;
    private float _cameraMouseRayMaxDistance;

    private Transform _characterTransform;
    private CharacterController _characterController;

    private Vector3 _jumpLaunchVelocity;

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _characterTransform = transform;
        _jumpLaunchVelocity = Mathf.Sqrt(-2 * Physics.gravity.y * jumpHeight) * Vector3.up;
        _velocity = Vector3.zero;
        _currentCameraType = cameraType;
        _cameraMouseRayMaxDistance = 2 * (isometricCameraPivotOffset.magnitude + isometricCameraDistance + jumpHeight);
        UpdateCameraType(force: true);
    }

    private void Update()
    {
        UpdateInputData();
        UpdateCamera();
        UpdateDirectionWithInputData();
        UpdateMeshRotation();
        UpdateCurrentVelocity();
        _characterController.Move(_velocity * Time.deltaTime);
    }

    private void UpdateInputData()
    {
        if (getRawInputData)
        {
            _inputData.MouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            _inputData.DirectionalInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }
        else
        {
            _inputData.MouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            _inputData.DirectionalInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }

        _inputData.Jump = Input.GetButton("Jump");
    }

    private void UpdateCameraPitch()
    {
        switch (cameraType)
        {
            case CameraType.ThirdPerson:
                var pitchDelta = _inputData.MouseDelta.y * thirdPersonCameraPitchSpeed;
                _currentCameraPitch =
                    Mathf.Clamp(
                        AngleTo_180_180(_currentCameraPitch) - pitchDelta,
                        thirdPersonCameraMinPitch,
                        thirdPersonCameraMaxPitch);
                cameraPivot.localRotation = Quaternion.Euler(_currentCameraPitch, 0, 0);
                break;
            case CameraType.Isometric:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void UpdateCameraYaw()
    {
        switch (cameraType)
        {
            case CameraType.ThirdPerson:
                var yawDelta = _inputData.MouseDelta.x * thirdPersonCameraYawSpeed;
                _characterTransform.Rotate(Vector3.up, yawDelta);
                break;
            case CameraType.Isometric:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private Vector3 MouseRaycastFromCameraHitPosition()
    {
        return
            Physics.Raycast(
                mainCamera.ScreenPointToRay(Input.mousePosition)
                , out var hit
                , _cameraMouseRayMaxDistance
                , LayerMask.GetMask("Ground")
            )
                ? hit.point
                : _characterTransform.position;
    }

    private void UpdateMeshRotation()
    {
        switch (cameraType)
        {
            case CameraType.ThirdPerson:
                meshTransform.rotation = Quaternion.LookRotation(_lastWorldDirectionAboveThreshold);
                break;
            case CameraType.Isometric:
                var mousePosition = MouseRaycastFromCameraHitPosition();
                mousePosition.y = meshTransform.position.y;
                meshTransform.LookAt(mousePosition);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void UpdateCamera()
    {
        UpdateCameraType(true);
        UpdateCameraPitch();
        UpdateCameraYaw();
    }

    private void UpdateDirectionWithInputData(float cap = 1.0f)
    {
        _worldDirection =
            _characterTransform.TransformVector(Vector3.ClampMagnitude(new Vector3
            {
                x = _inputData.DirectionalInput.x,
                y = 0,
                z = _inputData.DirectionalInput.y,
            }, cap));

        if (_worldDirection.sqrMagnitude > DirectionSqrMagnitudeThreshold)
        {
            _lastWorldDirectionAboveThreshold = _worldDirection;
        }
    }

    private void UpdateCurrentVelocity()
    {
        if (_characterController.isGrounded)
        {
            _velocity = maxGroundSpeed * _worldDirection;
            if (_inputData.Jump)
            {
                _velocity += _jumpLaunchVelocity;
            }
        }

        _velocity += Physics.gravity * Time.deltaTime;
    }

    private void UpdateCameraType(bool force = false)
    {
        if (!force && cameraType == _currentCameraType)
            return;

        _currentCameraType = cameraType;
        switch (cameraType)
        {
            case CameraType.ThirdPerson:
                mainCamera.fieldOfView = thirdPersonCameraFov;
                mainCamera.transform.localPosition = new Vector3(0, 0, -thirdPersonCameraDistance);

                cameraPivot.localPosition = thirdPersonCameraPivotOffset;
                cameraPivot.localRotation =
                    Quaternion.Euler(thirdPersonCameraMaxPitch - thirdPersonCameraMinPitch, 0, 0);

                break;

            case CameraType.Isometric:
                mainCamera.fieldOfView = isometricCameraFov;
                mainCamera.transform.localPosition = new Vector3(0, 0, -isometricCameraDistance);

                cameraPivot.localPosition = isometricCameraPivotOffset;
                cameraPivot.localRotation = Quaternion.Euler(isometricCameraPitch, 0, 0);
                _characterTransform.rotation = Quaternion.Euler(0, isometricCameraForwardAngle, 0);

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(cameraType), cameraType, null);
        }
    }

    /// <summary>
    ///     <para>Translates a given angle in degrees to range (-180;180] </para>
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    private static float AngleTo_180_180(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}