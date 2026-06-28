using UnityEngine;

/// <summary>
/// 相机遮挡处理器。
/// 
/// 当前第 12 步中，它负责：
/// 1. 检测 CameraPitchPivot 到 Main Camera 之间是否有障碍物
/// 2. 如果有障碍物，把相机临时拉近
/// 3. 如果障碍物消失，让相机恢复到原始距离
/// 
/// 注意：
/// 它不负责相机跟随。
/// 它不负责鼠标旋转。
/// 它只负责调整 Main Camera 的 localPosition。
/// </summary>
[DefaultExecutionOrder(100)]
public class CameraObstructionHandler : MonoBehaviour
{
    [Header("References")]

    /// <summary>
    /// 需要调整位置的相机 Transform。
    /// 
    /// 当前应该拖 Main Camera。
    /// </summary>
    [SerializeField]
    private Transform cameraTransform;

    [Header("Collision Settings")]

    /// <summary>
    /// 会阻挡相机的 Layer。
    /// 
    /// 这里应该勾选：
    /// Ground、Wall、Environment、Obstacle 等环境层。
    /// 
    /// 不要勾选 Player。
    /// 否则相机会检测到玩家自己的 CharacterController。
    /// </summary>
    [SerializeField]
    private LayerMask obstructionLayerMask;

    /// <summary>
    /// 相机碰撞检测半径。
    /// 
    /// 使用 SphereCast 而不是 Raycast，
    /// 可以减少相机穿过墙角、柱子边缘的问题。
    /// </summary>
    [SerializeField]
    private float cameraRadius = 0.25f;

    /// <summary>
    /// 相机和障碍物之间保留的安全距离。
    /// 
    /// 防止相机正好贴在墙面上导致画面穿模。
    /// </summary>
    [SerializeField]
    private float wallPadding = 0.08f;

    /// <summary>
    /// 相机距离角色最近不能小于这个值。
    /// 
    /// 防止相机被拉到角色身体内部。
    /// </summary>
    [SerializeField]
    private float minDistance = 0.8f;

    [Header("Smooth Settings")]

    /// <summary>
    /// 相机被障碍物推近时的速度。
    /// 
    /// 建议快一点，避免相机穿墙。
    /// </summary>
    [SerializeField]
    private float moveInSpeed = 30f;

    /// <summary>
    /// 障碍物消失后，相机恢复原距离的速度。
    /// 
    /// 建议比 moveInSpeed 慢一点，避免镜头突然弹回。
    /// </summary>
    [SerializeField]
    private float moveOutSpeed = 8f;

    /// <summary>
    /// SphereCastNonAlloc 使用的命中缓存。
    /// 
    /// 这里避免每帧产生 GC。
    /// </summary>
    private readonly RaycastHit[] hitBuffer = new RaycastHit[8];

    /// <summary>
    /// Main Camera 的初始本地位置。
    /// 
    /// 例如：
    /// (0, 0, -6)
    /// </summary>
    private Vector3 defaultLocalPosition;

    /// <summary>
    /// 从 CameraPitchPivot 指向 Main Camera 的本地方向。
    /// 
    /// 通常是：
    /// (0, 0, -1)
    /// </summary>
    private Vector3 cameraLocalDirection;

    /// <summary>
    /// 没有遮挡时，相机应该保持的默认距离。
    /// </summary>
    private float defaultDistance;

    /// <summary>
    /// 当前相机距离。
    /// 
    /// 会在默认距离和遮挡距离之间变化。
    /// </summary>
    private float currentDistance;

    /// <summary>
    /// Unity 生命周期方法。
    /// 
    /// 初始化时校验引用和参数，并记录相机默认位置。
    /// </summary>
    private void Awake()
    {
        ValidateReferences();
        ValidateSettings();
        CacheDefaultCameraPosition();
    }

    /// <summary>
    /// Unity 生命周期方法。
    /// 
    /// 在 LateUpdate 中处理相机遮挡。
    /// 
    /// DefaultExecutionOrder(100) 会让它尽量晚于普通相机跟随和旋转脚本执行。
    /// 这样可以基于本帧相机的最终朝向来检测遮挡。
    /// </summary>
    private void LateUpdate()
    {
        HandleObstruction();
    }

    /// <summary>
    /// 校验引用是否正确。
    /// 
    /// 这里不做静默兜底。
    /// 如果 cameraTransform 没有配置，就直接报错并禁用脚本。
    /// </summary>
    private void ValidateReferences()
    {
        if (cameraTransform == null)
        {
            Debug.LogError($"{nameof(CameraObstructionHandler)} 缺少 cameraTransform，请把 Main Camera 拖到这里。", this);
            enabled = false;
        }
    }

    /// <summary>
    /// 校验参数是否正确。
    /// 
    /// 参数错误时直接报错并禁用脚本。
    /// </summary>
    private void ValidateSettings()
    {
        if (cameraRadius <= 0f)
        {
            Debug.LogError($"{nameof(CameraObstructionHandler)} 的 cameraRadius 必须大于 0。", this);
            enabled = false;
        }

        if (wallPadding < 0f)
        {
            Debug.LogError($"{nameof(CameraObstructionHandler)} 的 wallPadding 不能小于 0。", this);
            enabled = false;
        }

        if (minDistance <= 0f)
        {
            Debug.LogError($"{nameof(CameraObstructionHandler)} 的 minDistance 必须大于 0。", this);
            enabled = false;
        }

        if (moveInSpeed <= 0f)
        {
            Debug.LogError($"{nameof(CameraObstructionHandler)} 的 moveInSpeed 必须大于 0。", this);
            enabled = false;
        }

        if (moveOutSpeed <= 0f)
        {
            Debug.LogError($"{nameof(CameraObstructionHandler)} 的 moveOutSpeed 必须大于 0。", this);
            enabled = false;
        }
    }

    /// <summary>
    /// 缓存相机默认本地位置和默认距离。
    /// 
    /// Main Camera 的初始 localPosition 会被当成“无障碍物时的相机位置”。
    /// </summary>
    private void CacheDefaultCameraPosition()
    {
        if (cameraTransform == null)
        {
            return;
        }

        defaultLocalPosition = cameraTransform.localPosition;
        defaultDistance = defaultLocalPosition.magnitude;

        if (defaultDistance <= 0.0001f)
        {
            Debug.LogError($"{nameof(CameraObstructionHandler)} 的 Main Camera 初始 localPosition 长度过小，无法计算默认相机距离。", this);
            enabled = false;
            return;
        }

        cameraLocalDirection = defaultLocalPosition.normalized;
        currentDistance = defaultDistance;
    }

    /// <summary>
    /// 处理相机遮挡。
    /// 
    /// 如果 CameraPitchPivot 和默认相机位置之间有障碍物，
    /// 相机会被拉近。
    /// 如果没有障碍物，
    /// 相机会恢复到默认距离。
    /// </summary>
    private void HandleObstruction()
    {
        float targetDistance = CalculateTargetDistance();

        float speed = targetDistance < currentDistance
            ? moveInSpeed
            : moveOutSpeed;

        currentDistance = Mathf.MoveTowards(
            currentDistance,
            targetDistance,
            speed * Time.deltaTime
        );

        cameraTransform.localPosition = cameraLocalDirection * currentDistance;
    }

    /// <summary>
    /// 计算相机当前应该使用的目标距离。
    /// 
    /// 没有遮挡时，返回 defaultDistance。
    /// 有遮挡时，返回距离障碍物更近的安全距离。
    /// </summary>
    /// <returns>相机目标距离。</returns>
    private float CalculateTargetDistance()
    {
        Vector3 castOrigin = transform.position;
        Vector3 desiredCameraWorldPosition = transform.TransformPoint(defaultLocalPosition);
        Vector3 castVector = desiredCameraWorldPosition - castOrigin;

        float castDistance = castVector.magnitude;

        if (castDistance <= 0.0001f)
        {
            Debug.LogError($"{nameof(CameraObstructionHandler)} 的检测距离过小，无法进行遮挡检测。", this);
            return currentDistance;
        }

        Vector3 castDirection = castVector / castDistance;

        int hitCount = Physics.SphereCastNonAlloc(
            castOrigin,
            cameraRadius,
            castDirection,
            hitBuffer,
            castDistance,
            obstructionLayerMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitCount <= 0)
        {
            return defaultDistance;
        }

        bool hasValidHit = TryGetNearestHit(hitCount, out RaycastHit nearestHit);

        if (!hasValidHit)
        {
            return defaultDistance;
        }

        float blockedDistance = nearestHit.distance - wallPadding;

        return Mathf.Clamp(
            blockedDistance,
            minDistance,
            defaultDistance
        );
    }

    /// <summary>
    /// 从 SphereCast 的结果中找出最近的有效命中。
    /// 
    /// SphereCastNonAlloc 返回的 hitBuffer 不保证按距离排序，
    /// 所以需要自己遍历一遍。
    /// </summary>
    /// <param name="hitCount">本帧检测到的命中数量。</param>
    /// <param name="nearestHit">最近命中结果。</param>
    /// <returns>是否找到有效命中。</returns>
    private bool TryGetNearestHit(int hitCount, out RaycastHit nearestHit)
    {
        nearestHit = default;

        float nearestDistance = float.MaxValue;
        bool hasHit = false;

        int count = Mathf.Min(hitCount, hitBuffer.Length);

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hitBuffer[i];

            if (hit.collider == null)
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestHit = hit;
                hasHit = true;
            }
        }

        return hasHit;
    }

    /// <summary>
    /// 在 Scene 视图中绘制当前相机遮挡检测范围。
    /// 
    /// 只用于调试。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Vector3 origin = transform.position;
        Vector3 target = cameraTransform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, target);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target, cameraRadius);
    }
}