using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityStandardAssets.CrossPlatformInput;
using DG.Tweening;

public class FreeCameraController : MonoBehaviour
{
    public UnityEvent onResetFinished = new UnityEvent();
    public float scale = 1;
    public float zoomSpeed = 2000;
    public float panSpeed = 60;
    public float moveSpeed = 3000;
    public float shakeSpeed = 10;
    public float panDamp = 10;
    //旋转最大角度
    public int yRotationMinLimit = -20;
    public int yRotationMaxLimit = 80;
    //旋转速度
    public float xRotationSpeed = 250.0f;
    public float yRotationSpeed = 120.0f;
    public GraphicRaycaster canvas;
    public static bool panning;
    public static bool shaking;
    private Vector3 shakePoint;
    private bool sliding2;
    private Vector3 endSlideAngle;
    private Vector3 currSlideAngle;

    private Camera freeCamera;

    private Rigidbody body;
    private PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
    private List<RaycastResult> results = new List<RaycastResult>(10);

    private bool active = true;
    //旋转角度
    private float xRotation = 0.0f;
    private float yRotation = 0.0f;

    private Vector3 originalPos;
    private Quaternion originalRot;

    // Start is called before the first frame update
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }
    }

    void Start()
    {
        freeCamera = GetComponentInChildren<Camera>();
        originalPos = transform.position;
        originalRot = transform.rotation;
    }

    private void OnEnable()
    {
        body.useGravity = false;
        body.isKinematic = false;
        body.drag = panDamp;
        body.constraints = RigidbodyConstraints.FreezeRotation;
    }


    public void Reset(bool anim = true)
    {
        if (anim)
        {
            transform.DORotateQuaternion(originalRot, 1);
            StartCoroutine(StartTime());
        }
        else
        {
            transform.position = originalPos;
            transform.rotation = originalRot;
            xRotation = originalRot.eulerAngles.y;
            yRotation = originalRot.eulerAngles.x;
            onResetFinished.Invoke();
        }
    }

    private IEnumerator StartTime()
    {
        yield return new WaitForSeconds(1);
        SyncState();
        onResetFinished.Invoke();
    }

    private void Update()
    {
        if (!active)
        {
            return;
        }
        if (Input.GetMouseButtonUp(0))
        {
            panning = false;
            //Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
        {
            var x = CrossPlatformInputManager.GetAxis("Mouse X");
            var y = CrossPlatformInputManager.GetAxis("Mouse Y");
            shaking = false;
            sliding2 = true;
            //currSlideAngle = delta;
            currSlideAngle.x = x;
            currSlideAngle.y = y;
            endSlideAngle = Vector3.zero;
            //Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            //Debug.Log("finish shaking.");
        }
        if (!shaking && (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) && !hasOverUI())
        {
            panning = false;
            if (Input.GetMouseButtonDown(2))
            {
                Ray ray = freeCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo, 10000, (1 << 11) | (1 << 12)))
                {
                    shakePoint = hitInfo.point;
                }
            }
            else
            {
                shakePoint = freeCamera.transform.position;
            }
            body.velocity = Vector3.zero;
            shaking = true;
            sliding2 = false;
            //shakePoint = freeCamera.position;
            Debug.Log("start shaking");
        }
        if (Input.GetMouseButton(0) && !panning && !hasOverUI())
        {
            var x = CrossPlatformInputManager.GetAxis("Mouse X");
            var y = CrossPlatformInputManager.GetAxis("Mouse Y");
            //Debug.LogFormat("mouse button 0 down x={0}, y={1}", x, y);
            if (x != 0 || y != 0)
            {
                panning = true;
                sliding2 = false;
                shaking = false;
                //Debug.Log("start panning");
                body.velocity = Vector3.zero;
                Cursor.visible = false;
            }
        }
    }

    public void SyncState()
    {
        xRotation = transform.rotation.eulerAngles.y;
        yRotation = transform.rotation.eulerAngles.x;
        yRotation = ClampValue(yRotation, yRotationMinLimit, yRotationMaxLimit);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!active)
        {
            return;
        }
        if (panning)
        {
            var x = CrossPlatformInputManager.GetAxis("Mouse X");
            var y = CrossPlatformInputManager.GetAxis("Mouse Y");
            //Debug.LogFormat("mouse x: {0}， mouse y: {1}", x, y);
            //Vector3 delta = Input.mousePosition - startPos;
            Vector3 forward = Quaternion.AngleAxis(-freeCamera.transform.eulerAngles.x, freeCamera.transform.right) * freeCamera.transform.forward;
            Debug.DrawLine(freeCamera.transform.position, freeCamera.transform.position + forward * 100, Color.blue);

            var force = (freeCamera.transform.right * -x) + (forward * -y);
            body.AddForce(force * panSpeed * scale);
        }
        if (!hasOverUI())
        {
            float wheelDelta = Input.GetAxis("Mouse ScrollWheel");
            if (wheelDelta != 0)
            {
                body.AddForce(freeCamera.transform.forward * wheelDelta * zoomSpeed * scale);
            }
        }
        if (Input.GetKey(KeyCode.Q))
        {
            body.AddForce(-freeCamera.transform.up * moveSpeed * scale * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.E))
        {
            body.AddForce(freeCamera.transform.up * moveSpeed * scale * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.W))
        {
            body.AddForce(freeCamera.transform.forward * moveSpeed * scale * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S))
        {
            body.AddForce(-freeCamera.transform.forward * moveSpeed * scale * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.A))
        {
            body.AddForce(-freeCamera.transform.right * moveSpeed * scale * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            body.AddForce(freeCamera.transform.right * moveSpeed * scale * Time.deltaTime);
        }

        if (shaking)
        {
            if (Input.GetMouseButton(1))
            {
                shakePoint = freeCamera.transform.position;
            }
            var x = CrossPlatformInputManager.GetAxis("Mouse X");
            var y = -CrossPlatformInputManager.GetAxis("Mouse Y");
            if (x == 0 && y == 0)
            {
                return;
            }
            if (Cursor.visible && ((x != 0 || y != 0)))
            {
                //Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            /*freeCamera.transform.RotateAround(shakePoint, Vector3.up, x * Time.deltaTime * shakeSpeed);

            float rotatedAngle = transform.eulerAngles.x + y;
            if (rotatedAngle > 85)
            {
                freeCamera.transform.RotateAround(shakePoint, freeCamera.transform.right, y + (85 - rotatedAngle));
            }
            else if (rotatedAngle < 0)
            {
                freeCamera.transform.RotateAround(shakePoint, freeCamera.transform.right, y - (rotatedAngle - 0));
            }
            else
            {
                freeCamera.transform.RotateAround(shakePoint, freeCamera.transform.right, y);
            }*/
            xRotation += x * xRotationSpeed * 0.02f;
            yRotation += y * yRotationSpeed * 0.02f;
            yRotation = ClampValue(yRotation, yRotationMinLimit, yRotationMaxLimit);
            //欧拉角转化为四元数
            Quaternion rotation = Quaternion.Euler(yRotation, xRotation, 0);
            transform.localRotation = rotation;
        }
        /*if (sliding2)
        {
            currSlideAngle = Vector3.Slerp(currSlideAngle, endSlideAngle, Time.deltaTime * 15);
            if (currSlideAngle == Vector3.zero)
            {
                sliding2 = false;
                return;
            }
            freeCamera.transform.RotateAround(shakePoint, Vector3.up, currSlideAngle.x);

            float rotatedAngle = transform.eulerAngles.x + currSlideAngle.y;
            if (rotatedAngle > 85)
            {
                freeCamera.transform.RotateAround(shakePoint, freeCamera.transform.right, currSlideAngle.y + (85 - rotatedAngle));
            }
            else if (rotatedAngle < 0)
            {
                freeCamera.transform.RotateAround(shakePoint, freeCamera.transform.right, currSlideAngle.y - (rotatedAngle - 0));
            }
            else
            {
                freeCamera.transform.RotateAround(shakePoint, freeCamera.transform.right, currSlideAngle.y);
            }
            if ((currSlideAngle - endSlideAngle).magnitude <= 0.01)
            {
                Debug.Log("sliding2 completed");
                sliding2 = false;
            }
        }*/

        Debug.DrawLine(shakePoint, shakePoint + Vector3.up * 100, Color.black);
    }

    float ClampValue(float value, float min, float max)//控制旋转的角度
    {
        if (value < -360)
            value += 360;
        if (value > 360)
            value -= 360;
        return Mathf.Clamp(value, min, max);//限制value的值在min和max之间， 如果value小于min，返回min。 如果value大于max，返回max，否则返回value
    }

    /// <summary>
    /// 获取鼠标停留处UI
    /// </summary>
    /// <param name="canvas"></param>
    /// <returns></returns>
    public bool hasOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
        /*if (canvas == null)
        {
            return false;
        }
        results.Clear();
        pointerEventData.position = Input.mousePosition;
        canvas.Raycast(pointerEventData, results);
        return results.Count > 0;*/
    }

}
