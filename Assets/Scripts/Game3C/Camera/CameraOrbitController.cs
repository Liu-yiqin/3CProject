using UnityEngine;

/// <summary>
/// 第三人称相机旋转控制器。
/// 
/// 当前第 7 步中，它负责：
/// 1. 鼠标左右移动控制 CameraRig 的水平旋转
/// 2. 鼠标上下移动控制 CameraPitchPivot 的俯仰旋转
/// 3. 限制相机俯仰角范围
/// 
/// 注意：
/// 它不负责相机位置跟随，
/// 相机位置跟随仍然由 CameraFollow 负责。
/// </summary>
public class CameraOrbitController : MonoBehaviour
{
    [Header("References")]

    /// <summary>
    /// 相机俯仰轴节点。
    /// 
    /// 它应该是 CameraRig 的子物体。
    /// Main Camera 应该挂在它下面。
    /// 
    /// 鼠标上下移动时，会修改这个节点的本地 X 轴旋转。
    /// </summary>
    [SerializeField]
    private Transform pitchPivot;

    [Header("Mouse Settings")]

    /// <summary>
    /// 鼠标水平灵敏度。
    /// 
    /// 控制鼠标左右移动时，相机水平旋转的速度。
    /// </summary>
    [SerializeField]
    private float mouseSensitivityX = 3f;

    /// <summary>
    /// 鼠标垂直灵敏度。
    /// 
    /// 控制鼠标上下移动时，相机俯仰旋转的速度。
    /// </summary>
    [SerializeField]
    private float mouseSensitivityY = 2f;

    /// <summary>
    /// 是否反转鼠标 Y 轴。
    /// 
    /// false：
    ///     鼠标向上，相机向上看。
    /// 
    /// true：
    ///     鼠标向上，相机向下看。
    /// </summary>
    [SerializeField]
    private bool invertY = false;

    [Header("Pitch Limit Settings")]

    /// <summary>
    /// 最小俯仰角。
    /// 
    /// 数值越小，相机越能往上看。
    /// 
    /// 例如：
    /// -20 表示最多向上抬 20 度。
    /// </summary>
    [SerializeField]
    private float minPitch = -20f;

    /// <summary>
    /// 最大俯仰角。
    /// 
    /// 数值越大，相机越能往下看。
    /// 
    /// 例如：
    /// 60 表示最多向下看 60 度。
    /// </summary>
    [SerializeField]
    private float maxPitch = 60f;

    [Header("Cursor Settings")]

    /// <summary>
    /// 游戏开始时是否锁定鼠标。
    /// 
    /// 锁定后鼠标不会移出 Game 视图，
    /// 更适合测试第三人称相机旋转。
    /// </summary>
    [SerializeField]
    private bool lockCursorOnStart = true;

    /// <summary>
    /// 是否允许按 Escape 解锁鼠标。
    /// </summary>
    [SerializeField]
    private bool unlockCursorByEscape = true;

    /// <summary>
    /// 当前水平旋转角。
    /// 
    /// 对应 CameraRig 的 Y 轴旋转。
    /// </summary>
    private float yaw;

    /// <summary>
    /// 当前俯仰角。
    /// 
    /// 对应 CameraPitchPivot 的本地 X 轴旋转。
    /// </summary>
    private float pitch;

    /// <summary>
    /// 当前这一帧鼠标水平输入。
    /// </summary>
    private float mouseX;

    /// <summary>
    /// 当前这一帧鼠标垂直输入。
    /// </summary>
    private float mouseY;

    /// <summary>
    /// Unity 生命周期方法。
    /// 
    /// 初始化时校验引用和参数。
    /// </summary>
    private void Awake()
    {
        ValidateReferences();
        ValidateSettings();
        InitializeAngles();
    }

    /// <summary>
    /// Unity 生命周期方法。
    /// 
    /// 游戏开始时根据配置锁定鼠标。
    /// </summary>
    private void Start()
    {
        if (lockCursorOnStart)
        {
            SetCursorLocked(true);
        }
    }

    /// <summary>
    /// Unity 生命周期方法。
    /// 
    /// 在 Update 中读取鼠标输入。
    /// </summary>
    private void Update()
    {
        HandleCursorUnlockInput();
        ReadMouseInput();
        UpdateAngles();
    }

    /// <summary>
    /// Unity 生命周期方法。
    /// 
    /// 在 LateUpdate 中应用相机旋转。
    /// 
    /// 相机位置跟随通常也在 LateUpdate 中执行，
    /// 旋转放在这里可以让相机本帧的最终姿态更稳定。
    /// </summary>
    private void LateUpdate()
    {
        ApplyRotation();
    }

    /// <summary>
    /// 校验引用是否正确。
    /// 
    /// 这里不做静默兜底。
    /// 如果 pitchPivot 没有配置，就直接报错并禁用脚本。
    /// </summary>
    private void ValidateReferences()
    {
        if (pitchPivot == null)
        {
            Debug.LogError($"{nameof(CameraOrbitController)} 缺少 pitchPivot，请把 CameraPitchPivot 拖到这里。", this);
            enabled = false;
        }
    }

    /// <summary>
    /// 校验相机旋转参数是否正确。
    /// 
    /// 参数错误时直接报错并禁用脚本。
    /// </summary>
    private void ValidateSettings()
    {
        if (mouseSensitivityX <= 0f)
        {
            Debug.LogError($"{nameof(CameraOrbitController)} 的 mouseSensitivityX 必须大于 0。", this);
            enabled = false;
        }

        if (mouseSensitivityY <= 0f)
        {
            Debug.LogError($"{nameof(CameraOrbitController)} 的 mouseSensitivityY 必须大于 0。", this);
            enabled = false;
        }

        if (minPitch > maxPitch)
        {
            Debug.LogError($"{nameof(CameraOrbitController)} 的 minPitch 不能大于 maxPitch。", this);
            enabled = false;
        }
    }

    /// <summary>
    /// 初始化相机角度。
    /// 
    /// 从当前 Transform 读取已有角度，
    /// 避免运行时相机突然跳到 0 度。
    /// </summary>
    private void InitializeAngles()
    {
        yaw = transform.eulerAngles.y;

        if (pitchPivot == null)
        {
            return;
        }

        pitch = NormalizeAngle(pitchPivot.localEulerAngles.x);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    /// <summary>
    /// 处理鼠标解锁输入。
    /// 
    /// 当前规则：
    /// 按 Escape 解锁鼠标。
    /// 左键点击 Game 视图后重新锁定鼠标。
    /// </summary>
    private void HandleCursorUnlockInput()
    {
        if (!unlockCursorByEscape)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLocked(false);
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            SetCursorLocked(true);
        }
    }

    /// <summary>
    /// 读取鼠标输入。
    /// 
    /// 旧输入系统里，Mouse X / Mouse Y 表示这一帧鼠标移动量。
    /// 
    /// 注意：
    /// 这里不乘 Time.deltaTime。
    /// 因为 Mouse X / Mouse Y 本身已经是鼠标这一帧的移动增量。
    /// </summary>
    private void ReadMouseInput()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            mouseX = 0f;
            mouseY = 0f;
            return;
        }

        mouseX = Input.GetAxisRaw("Mouse X");
        mouseY = Input.GetAxisRaw("Mouse Y");
    }

    /// <summary>
    /// 根据鼠标输入更新 yaw 和 pitch。
    /// 
    /// yaw 控制水平旋转。
    /// pitch 控制上下俯仰。
    /// </summary>
    private void UpdateAngles()
    {
        yaw += mouseX * mouseSensitivityX;

        float pitchInput = invertY ? mouseY : -mouseY;
        pitch += pitchInput * mouseSensitivityY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    /// <summary>
    /// 应用相机旋转。
    /// 
    /// CameraRig 只负责 Y 轴水平旋转。
    /// CameraPitchPivot 只负责 X 轴俯仰旋转。
    /// </summary>
    private void ApplyRotation()
    {
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (pitchPivot == null)
        {
            Debug.LogError($"{nameof(CameraOrbitController)} 缺少 pitchPivot，无法应用俯仰旋转。", this);
            return;
        }

        pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    /// <summary>
    /// 设置鼠标锁定状态。
    /// 
    /// locked 为 true 时：
    ///     鼠标锁定到 Game 视图中心，并隐藏光标。
    /// 
    /// locked 为 false 时：
    ///     鼠标解锁，并显示光标。
    /// </summary>
    /// <param name="locked">是否锁定鼠标。</param>
    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    /// <summary>
    /// 把 Unity 的 0~360 欧拉角转换成 -180~180。
    /// 
    /// 这样更方便做俯仰角限制。
    /// </summary>
    /// <param name="angle">原始角度。</param>
    /// <returns>转换后的角度。</returns>
    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}