using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public Transform m_PivotTransform;

    [Header("Camera")]
    public Camera m_GameCamera;
    public Vector3 m_ResetRotationVector = new Vector3(0, 0, 0);
    public float m_cameraSpeed = 30;
    public float m_cameraZoomSpeed = .5f;
    public float m_cameraRotateSpeed = 75;
    public float m_maxRayDistance = 100;

    [Header("Boundaries")]
    public float m_boundaryX = 100;
    public float m_boundaryZ = 100;
    public float m_zoomMin = 10;
    public float m_zoomMax = 23;

    private Vector3 m_IsoInput;
    private float m_mouseScroll;
    private Matrix4x4 m_IsoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 0));
    private Collider m_HoveredCollider = null;

    #region SINGLETON DECLARATION

    // Singleton implementation pattern
    private static CameraScript m_instance;
    public static CameraScript Instance
    {
        get { return m_instance; }
    }

    #endregion SINGLETON DECLARATION

    // Awake is called when this script is loaded
    private void Awake ()
    {
        // Singleton declaration
        if (m_instance != null && m_instance != this)
            Destroy(gameObject);
        else
            m_instance = this;
    }

    #region PRIVATE METHODS

    // Update is called once per frame
    private void Update ()
    {
        GetInput();
        MoveCamera();
        ZoomCamera();
    }

    // Capture user input (keyboard and mouse)
    private void GetInput ()
    {
        // Keyboard input for camera movement
        Vector3 rawInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        m_IsoInput = TranslateInputToIso(rawInput);

        // Mouse position
        // Cast a ray from the camera to the point in the 3D world where the mouse is located
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawLine(ray.origin, ray.origin + ray.direction * 100f, Color.green);

        // Check if the ray intersects with a collider that is not the ground (or part of the level)
        if (Physics.Raycast(ray, out hit) && !hit.collider.CompareTag("Level")) {
            // Check if the collider is not the same as the previously hovered collider
            if (hit.collider != m_HoveredCollider)
                m_HoveredCollider = hit.collider;
        } else {
            // If the ray does not intersect with a collider or the mouse has moved away from the previously hovered collider, reset the hovered collider
            if (m_HoveredCollider != null)
                m_HoveredCollider = null;
        }

        // Mouse scroll
        m_mouseScroll = Input.mouseScrollDelta.y;

        // Rotate camera
        if (Input.GetKey(KeyCode.Q))
            RotateCamera(1);

        if (Input.GetKey(KeyCode.E))
            RotateCamera(-1);

        // Reset camera
        if (Input.GetKeyDown(KeyCode.R))
            ResetCameraRotation();
    }

    // Move camera using directional keys or WASD (left/right, up/down)
    private void MoveCamera ()
    {
        // Let's constrain the camera position so that it can't get out of the game board
        float xPos = Mathf.Clamp(m_PivotTransform.position.x + m_IsoInput.x * m_cameraSpeed * Time.deltaTime, -m_boundaryX, m_boundaryX);
        float yPos = m_PivotTransform.position.y + m_IsoInput.y;
        float zPos = Mathf.Clamp(m_PivotTransform.position.z + m_IsoInput.z * m_cameraSpeed * Time.deltaTime, -m_boundaryX, m_boundaryX);

        m_PivotTransform.position = new Vector3(xPos, yPos, zPos);
    }

    // Zoom in or out orthographic camera using mouse wheel
    private void ZoomCamera ()
    {
        float zoom = m_GameCamera.orthographicSize - m_mouseScroll * m_cameraZoomSpeed;   // "-" to reverse the scrolling direction
        m_GameCamera.orthographicSize = Mathf.Clamp(zoom, m_zoomMin, m_zoomMax);
    }

    // Rotate camera left or right using Q and E keys
    private void RotateCamera (int i)   // Increase PivotTransform rotation multiplied by 1 or -1 (depending on the direction we want to go
    {
        Vector3 EurlerRot = m_PivotTransform.transform.rotation.eulerAngles;
        m_PivotTransform.transform.rotation = Quaternion.Euler(EurlerRot.x, EurlerRot.y + i * m_cameraRotateSpeed * Time.deltaTime, EurlerRot.z);

        // Also update the IsoMatrix so that our Horizontal and Vertical axis... axises? Anyway - making sure they are still going up and right relative to the camera
        m_IsoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, m_PivotTransform.transform.rotation.eulerAngles.y, 0));
    }

    // This function returns an updated "UserInput" Vector3, rotated based on an isometric camera orientation
    private Vector3 TranslateInputToIso (Vector3 input) => m_IsoMatrix.MultiplyPoint3x4(input);

    #endregion PRIVATE METHODS

    #region PUBLIC METHODS

    // Reset camera rotation to the default specified rotation
    public void ResetCameraRotation ()
    {
        m_PivotTransform.transform.rotation = Quaternion.Euler(m_ResetRotationVector);
        m_IsoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, m_PivotTransform.transform.rotation.eulerAngles.y, 0));
    }

    // Get currentl mouse position in world coordinates (collision with ground)
    public Vector3 GetMouseWorldPos ()
    {
        // Cast a ray from the camera to the point in the 3D world where the mouse is located
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
            return hit.point;

        return Vector3.zero;
    }


    // Pick up whichever collider is currently being hovered
    public Collider GetHoveredCollider () => m_HoveredCollider;
    //public Vector3 GetMouseWorldPos () => m_MouseWorldPosition;

    #endregion PUBLIC METHODS
}
