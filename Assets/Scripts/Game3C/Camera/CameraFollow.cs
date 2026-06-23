using UnityEngine;

/// <summary>
/// 基础相机跟随器。
/// 
/// 当前第 6 步中，它只负责让 CameraRig 跟随目标位置。
/// 
/// 注意：
/// 1. 它不负责鼠标旋转
/// 2. 它不负责相机遮挡检测
/// 3. 它不负责相机震动
/// 4. 它不负责读取输入
/// 
/// 后续第 7 步会在这个基础上加入鼠标控制相机旋转。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("References")]

    /// <summary>
    /// 相机跟随目标。
    /// 
    /// 当前应该拖 Player。
    /// </summary>
    [SerializeField]
    private Transform target;

    [Header("Follow Settings")]

    /// <summary>
    /// 相机跟随目标偏移。
    /// 
    /// 这个偏移是加在 target.position 上的。
    /// 
    /// 当前基础阶段可以先保持 Vector3.zero，
    /// 表示 CameraRig 跟随 Player 的位置。
    /// 
    /// 如果你希望相机围绕角色胸口附近运动，
    /// 可以设置成：
    /// X = 0
    /// Y = 1
    /// Z = 0
    /// </summary>
    [SerializeField]
    private Vector3 targetOffset = Vector3.zero;

    /// <summary>
    /// 平滑跟随时间。
    /// 
    /// 数值越小，相机跟随越紧；
    /// 数值越大，相机跟随越慢。
    /// 
    /// 推荐初始值：0.08 ~ 0.15。
    /// </summary>
    [SerializeField]
    private float smoothTime = 0.12f;

    /// <summary>
    /// 最大跟随速度。
    /// 
    /// 用于限制 SmoothDamp 的最大移动速度。
    /// 一般设置得比较大即可。
    /// </summary>
    [SerializeField]
    private float maxFollowSpeed = 100f;

    /// <summary>
    /// 游戏开始时是否立即贴到目标位置。
    /// 
    /// 如果为 true，
    /// 运行第一帧时相机会直接到达目标位置，
    /// 避免从场景初始位置慢慢滑过去。
    /// </summary>
    [SerializeField]
    private bool snapOnStart = true;

    /// <summary>
    /// SmoothDamp 内部使用的速度缓存。
    /// </summary>
    private Vector3 followVelocity;

    /// <summary>
    /// Unity 生命周期方法。
    /// 
    /// 初始化时检查引用和参数是否正确。
    /// </summary>
    private void Awake()
    {
        ValidateReferences();
        ValidateSettings();
    }

    /// <summary>
    /// Unity 生命周期方法。
    /// 
    /// 游戏开始时，如果开启 snapOnStart，
    /// 让 CameraRig 直接贴到目标位置。
    /// </summary>
    private void Start()
    {
        if (snapOnStart)
        {
            SnapToTarget();
        }
    }

    /// <summary>
    /// Unity 生命周期方法。
    /// 
    /// 相机跟随放在 LateUpdate 中执行。
    /// 
    /// 原因：
    /// Player 通常在 Update 中移动，
    /// Camera 在 LateUpdate 中跟随，
    /// 可以确保相机读取到的是角色这一帧移动后的最终位置。
    /// </summary>
    private void LateUpdate()
    {
        FollowTarget();
    }

    /// <summary>
    /// 立即把 CameraRig 移动到目标位置。
    /// 
    /// 这个方法适合：
    /// 1. 游戏开始时使用
    /// 2. 角色传送后使用
    /// 3. 切换场景后使用
    /// </summary>
    public void SnapToTarget()
    {
        transform.position = GetDesiredPosition();
        followVelocity = Vector3.zero;
    }

    /// <summary>
    /// 平滑跟随目标位置。
    /// 
    /// 每帧在 LateUpdate 中调用。
    /// </summary>
    private void FollowTarget()
    {
        Vector3 desiredPosition = GetDesiredPosition();

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            smoothTime,
            maxFollowSpeed,
            Time.deltaTime
        );
    }

    /// <summary>
    /// 获取 CameraRig 应该移动到的位置。
    /// 
    /// 当前规则：
    /// 目标位置 = target.position + targetOffset
    /// </summary>
    /// <returns>CameraRig 的目标位置。</returns>
    private Vector3 GetDesiredPosition()
    {
        if (target == null)
        {
            Debug.LogError($"{nameof(CameraFollow)} 缺少 target，无法计算相机跟随位置。", this);
            return transform.position;
        }

        return target.position + targetOffset;
    }

    /// <summary>
    /// 校验引用是否正确。
    /// 
    /// 这里不做静默兜底。
    /// 如果 target 没有配置，就直接报错并禁用脚本。
    /// </summary>
    private void ValidateReferences()
    {
        if (target == null)
        {
            Debug.LogError($"{nameof(CameraFollow)} 缺少 target，请把 Player 拖到 target 上。", this);
            enabled = false;
        }
    }

    /// <summary>
    /// 校验相机跟随参数是否正确。
    /// 
    /// 参数错误时直接报错并禁用脚本。
    /// </summary>
    private void ValidateSettings()
    {
        if (smoothTime <= 0f)
        {
            Debug.LogError($"{nameof(CameraFollow)} 的 smoothTime 必须大于 0。", this);
            enabled = false;
        }

        if (maxFollowSpeed <= 0f)
        {
            Debug.LogError($"{nameof(CameraFollow)} 的 maxFollowSpeed 必须大于 0。", this);
            enabled = false;
        }
    }
}