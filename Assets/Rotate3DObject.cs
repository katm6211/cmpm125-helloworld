using UnityEngine;
using UnityEngine.InputSystem;


public class Rotate3DObject : MonoBehaviour
{
    #region InputSystem    
    [SerializeField] private InputActionAsset _actions;
    public InputActionAsset actions
    {
        get => _actions;
        set => _actions = value;
    }
    protected InputAction leftClickPressedInputAction { get; set; }

    protected InputAction mouseLookInputAction { get; set; }

    protected InputAction scrollInputAction { get; set; }
    protected InputAction leftDoubleClickInputAction { get; set; }
    #endregion
    #region Variables 


    private bool _dragAllowed;
    private bool _selected;
    private Camera _camera;
    [Header("Rotation")]
    [SerializeField] private float _rotationSpeed = 1000f;
    [SerializeField] private bool _inverted;

    [Header("Movement")]
    [Tooltip("1 = object follows the mouse 1:1 at its current depth. Lower is slower.")]
    [SerializeField] private float _moveSpeed = 12f;


    [Header("Scaling")]
    [Tooltip("How much the scale multiplier changes per scroll tick.")]
    [SerializeField] private float _scaleStep = 0.1f;
    [SerializeField] private float _minScale = 0.2f;
    [SerializeField] private float _maxScale = 5f;

    private Vector3 _baseScale;
    private float _scaleMultiplier = 1f;
    #endregion


    private void Awake()
    {
        InitializeInputSystem();
    }

    private void InitializeInputSystem()
    {
        leftClickPressedInputAction = actions.FindAction("Left Click");
        if (leftClickPressedInputAction != null)
        {
            Debug.Log("Adding left click hooks");
            leftClickPressedInputAction.started += OnLeftClickPressed;
            leftClickPressedInputAction.canceled += OnLeftClickPressed;
        }

        Debug.Log("retrieving mouse look and enabling actions");
        mouseLookInputAction = actions.FindAction("Mouse Look");
        scrollInputAction = actions.FindAction("Scroll");

        leftDoubleClickInputAction = actions.FindAction("Left Double Click");
        if (leftDoubleClickInputAction != null)
            leftDoubleClickInputAction.performed += OnLeftDoubleClick;
        actions.Enable();
    }

    private void OnDestroy()
    {
        if (leftClickPressedInputAction != null)
        {
            leftClickPressedInputAction.started -= OnLeftClickPressed;
            leftClickPressedInputAction.canceled -= OnLeftClickPressed;
        }
        if (leftDoubleClickInputAction != null)
            leftDoubleClickInputAction.performed -= OnLeftDoubleClick;
    }
    protected virtual void OnLeftClickPressed(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _selected = IsPointerOverThis();
            _dragAllowed = _selected;
            Debug.Log(_selected ? "Selected, dragging allowed" : "Clicked off, deselected");
        }
        else if (context.canceled)
        {
            _dragAllowed = false;
            Debug.Log("Disallowing dragging");
        }
    }
    protected virtual void OnLeftDoubleClick(InputAction.CallbackContext context)
    {
        enabled = !enabled;
        _selected = false;
        _dragAllowed = false;
        Debug.Log(enabled ? "Controls on" : "Controls off");
    }

    private bool IsPointerOverThis()
    {
        if (_camera == null || Mouse.current == null)
            return false;

        // With a locked cursor the pointer position is meaningless,
        // so pick whatever is under the screen center instead.
        Vector2 screenPoint = Cursor.lockState == CursorLockMode.Locked
            ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            : Mouse.current.position.ReadValue();

        Ray ray = _camera.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.transform == transform || hit.transform.IsChildOf(transform);

        return false;
    }

    protected virtual Vector2 GetMouseLookInput()
    {
        if (mouseLookInputAction != null)
            return mouseLookInputAction.ReadValue<Vector2>();
        return Vector2.zero;
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        _camera = Camera.main;
        _baseScale = transform.localScale;

        Debug.Log(_camera != null ? $"Camera found: {_camera.name}" : "Camera.main is NULL");

    }

    // Update is called once per frame
    void Update()
    {
        if (_selected)
            HandleScale();
        if (!_dragAllowed)
            return;
        Vector2 mouseDelta = GetMouseLookInput();

        Rotate(mouseDelta);
        Move(mouseDelta);
    }
    private void HandleScale()
    {
        if (scrollInputAction == null)
            return;

        float scroll = scrollInputAction.ReadValue<Vector2>().y;
        if (Mathf.Approximately(scroll, 0f))
            return;

        // Sign only, because scroll magnitude differs between platforms and devices.
        _scaleMultiplier = Mathf.Clamp(_scaleMultiplier + Mathf.Sign(scroll) * _scaleStep, _minScale, _maxScale);
        transform.localScale = _baseScale * _scaleMultiplier;
    }

    private void Rotate(Vector2 mouseDelta)
    {

        mouseDelta *= _rotationSpeed * Time.deltaTime;

        transform.Rotate(Vector3.up * (_inverted ? 1 : -1), mouseDelta.x, Space.World);
        transform.Rotate(Vector3.right * (_inverted ? 1 : -1), mouseDelta.y, Space.World);
    }
    private void Move(Vector2 mouseDelta)
    {
        if (_camera == null)
            return;

        Transform cam = _camera.transform;

        // World units covered by one screen pixel at the object's depth.
        float worldPerPixel;
        if (_camera.orthographic)
        {
            worldPerPixel = (2f * _camera.orthographicSize) / Screen.height;
        }
        else
        {
            float depth = Mathf.Max(Vector3.Dot(transform.position - cam.position, cam.forward), 0.01f);
            worldPerPixel = (2f * depth * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad)) / Screen.height;
        }

        Vector3 move = (cam.right * mouseDelta.x + cam.up * mouseDelta.y) * (worldPerPixel * _moveSpeed);
        transform.position += move;
    }
}
