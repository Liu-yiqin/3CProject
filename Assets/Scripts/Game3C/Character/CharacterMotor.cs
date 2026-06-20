using UnityEngine;

/// <summary>
/// 角色移动执行器。
/// 
/// 它是真正负责让角色移动的模块。
/// 
/// 当前步骤中，它负责：
/// 1. 水平移动
/// 2. 重力
/// 3. 地面检测
/// 4. 跳跃
/// 5. 下落
/// 6. 使用 CharacterController.Move 执行最终位移
/// 
/// 注意：
/// 它不直接读取 Input。
/// 它只接收外部传进来的移动输入。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : MonoBehaviour
{
    [Header("Camera Relative Move Settings")]

    /// <summary>
    /// 移动方向参考物。
    /// 
    /// 当前第 5 步用相机 Transform。
    /// 
    /// 作用：
    /// 把玩家输入的二维方向转换成基于相机的世界移动方向。
    /// 
    /// 例如：
    /// W 不再表示世界 Z+，
    /// 而是表示相机当前水平前方。
    /// </summary>
    [SerializeField]
    private Transform moveReference;

    /// <summary>
    /// 当前这一帧的水平移动方向。
    /// 
    /// 这个方向是世界空间方向。
    /// 
    /// 例如：
    /// W：Vector3.forward
    /// S：Vector3.back
    /// A：Vector3.left
    /// D：Vector3.right
    /// 
    /// 如果当前没有移动输入，值为 Vector3.zero。
    /// </summary>
    private Vector3 currentMoveDirection;

    /// <summary>
    /// 当前这一帧的水平移动方向。
    /// </summary>
    public Vector3 CurrentMoveDirection => currentMoveDirection;

    /// <summary>
    /// 当前这一帧是否有有效移动方向。
    /// 
    /// 注意：
    /// 这里判断的是移动方向，
    /// 不是垂直速度。
    /// 所以角色原地跳跃时，这里仍然是 false。
    /// </summary>
    public bool HasMoveDirection => currentMoveDirection.sqrMagnitude > 0.0001f;

    [Header("References")]

    /// <summary>
    /// 地面检测器。
    /// 
    /// 用于判断角色当前是否站在地面上。
    /// </summary>
    [SerializeField]
    private GroundDetector groundDetector;

    [Header("Move Settings")]

    /// <summary>
    /// 角色水平移动速度。
    /// 单位：米 / 秒。
    /// </summary>
    [SerializeField]
    private float moveSpeed = 5f;

    [Header("Gravity Settings")]

    /// <summary>
    /// 重力加速度。
    /// 
    /// Unity 默认物理重力是 -9.81。
    /// 这里我们自己控制 CharacterController 的重力，
    /// 所以用一个可配置的值。
    /// 
    /// 注意：
    /// 这个值应该是负数。
    /// </summary>
    [SerializeField]
    private float gravity = -20f;

    /// <summary>
    /// 角色站在地面上时，给一个很小的向下速度。
    /// 
    /// 目的：
    /// 让 CharacterController 更稳定地贴在地面上。
    /// 
    /// 如果站在地面时直接把 verticalVelocity 设成 0，
    /// 在一些斜坡、台阶、边缘位置可能会出现轻微离地或抖动。
    /// </summary>
    [SerializeField]
    private float groundedStickVelocity = -2f;

    /// <summary>
    /// 最大下落速度。
    /// 
    /// 防止角色下落速度无限变大。
    /// 注意这个值应该是负数。
    /// </summary>
    [SerializeField]
    private float maxFallSpeed = -30f;

    [Header("Jump Settings")]

    /// <summary>
    /// 起跳初速度对应的基础高度。
    /// 
    /// 这里不是最终最大跳跃高度，
    /// 而是按下跳跃时，用它计算第一下起跳速度。
    /// 
    /// 长按能跳得更高，
    /// 是因为后续上升阶段重力更小。
    /// </summary>
    [SerializeField]
    private float jumpStartHeight = 1.0f;

    /// <summary>
    /// 长按跳跃最多生效多久。
    /// 
    /// 玩家按住跳跃键时，
    /// 只有在这个时间窗口内才会使用较小重力。
    /// 
    /// 时间到后，即使继续按住，
    /// 也会恢复正常上升重力。
    /// </summary>
    [SerializeField]
    private float maxJumpHoldTime = 0.25f;

    /// <summary>
    /// 长按跳跃时的上升重力倍率。
    /// 
    /// 数值越小，按住跳跃时上升速度衰减越慢，跳得越高。
    /// 
    /// 例如：
    /// gravity = -20
    /// jumpHoldGravityMultiplier = 0.45
    /// 实际上升重力 = -9
    /// </summary>
    [SerializeField]
    private float jumpHoldGravityMultiplier = 0.45f;

    /// <summary>
    /// 松开跳跃键后的上升重力倍率。
    /// 
    /// 数值越大，松手后向上速度衰减越快，短按跳得越低。
    /// 
    /// 注意：
    /// 它不会直接修改 verticalVelocity，
    /// 只是让后续速度更快变小。
    /// </summary>
    [SerializeField]
    private float jumpReleaseGravityMultiplier = 2.5f;

    /// <summary>
    /// 下落阶段的重力倍率。
    /// 
    /// 数值越大，下落越快，角色越不飘。
    /// </summary>
    [SerializeField]
    private float fallGravityMultiplier = 1.8f;

    /// <summary>
    /// 重力倍率变化速度。
    /// 
    /// 用于让重力倍率从一个值平滑过渡到另一个值。
    /// 
    /// 这样松开跳跃时，不会连加速度都突然变化得太硬。
    /// </summary>
    [SerializeField]
    private float gravityMultiplierChangeSpeed = 25f;

    /// <summary>
    /// Unity 自带的 CharacterController。
    /// 
    /// 我们通过 CharacterController.Move 来移动角色，
    /// 而不是直接修改 transform.position。
    /// </summary>
    private CharacterController characterController;

    /// <summary>
    /// 当前垂直速度。
    /// 
    /// 小于 0 表示正在向下掉。
    /// 大于 0 表示正在向上运动。
    /// 
    /// 当前第 2 步还没有跳跃，
    /// 所以它主要会是 0 或负数。
    /// 第 3 步加入跳跃后，它会出现正数。
    /// </summary>
    private float verticalVelocity;

    /// <summary>
    /// 当前跳跃长按剩余时间。
    /// </summary>
    private float jumpHoldTimer;

    /// <summary>
    /// 当前是否处于一次跳跃过程中。
    /// 
    /// 用于区分：
    /// 1. 正常地面状态
    /// 2. 起跳后的上升 / 下落状态
    /// </summary>
    private bool isJumping;

    /// <summary>
    /// 当前正在使用的重力倍率。
    /// 
    /// 这个值会向目标倍率平滑变化。
    /// </summary>
    private float currentGravityMultiplier = 1f;


    /// <summary>
    /// 当前是否在地面上。
    /// 
    /// 外部可以读取这个属性，
    /// 后面状态机和动画系统都会用到。
    /// </summary>
    public bool IsGrounded
    {
        get
        {
            if (groundDetector == null)
            {
                Debug.LogError($"{nameof(CharacterMotor)} 缺少 {nameof(GroundDetector)} 引用，请检查 Player 上是否挂载并赋值。", this);
                return false;
            }

            return groundDetector.IsGrounded;
        }
    }

    /// <summary>
    /// 当前垂直速度。
    /// 
    /// 外部可以读取这个属性，
    /// 后面 Animator 可以根据它判断 Jump / Fall。
    /// </summary>
    public float VerticalVelocity => verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (groundDetector == null)
        {
            groundDetector = GetComponent<GroundDetector>();
        }

        ValidateSettings();
    }

    /// <summary>
    /// 校验移动和跳跃参数。
    /// 
    /// 这里不做静默兜底。
    /// 参数错了就直接报错，方便定位配置问题。
    /// </summary>
    private void ValidateSettings()
    {
        if (gravity >= 0f)
        {
            Debug.LogError($"{nameof(CharacterMotor)} 的 gravity 必须是负数。", this);
        }

        if (maxFallSpeed >= 0f)
        {
            Debug.LogError($"{nameof(CharacterMotor)} 的 maxFallSpeed 必须是负数。", this);
        }

        if (jumpStartHeight <= 0f)
        {
            Debug.LogError($"{nameof(CharacterMotor)} 的 jumpStartHeight 必须大于 0。", this);
        }

        if (maxJumpHoldTime < 0f)
        {
            Debug.LogError($"{nameof(CharacterMotor)} 的 maxJumpHoldTime 不能小于 0。", this);
        }

        if (jumpHoldGravityMultiplier <= 0f)
        {
            Debug.LogError($"{nameof(CharacterMotor)} 的 jumpHoldGravityMultiplier 必须大于 0。", this);
        }

        if (jumpReleaseGravityMultiplier <= 0f)
        {
            Debug.LogError($"{nameof(CharacterMotor)} 的 jumpReleaseGravityMultiplier 必须大于 0。", this);
        }

        if (fallGravityMultiplier <= 0f)
        {
            Debug.LogError($"{nameof(CharacterMotor)} 的 fallGravityMultiplier 必须大于 0。", this);
        }

        if (gravityMultiplierChangeSpeed <= 0f)
        {
            Debug.LogError($"{nameof(CharacterMotor)} 的 gravityMultiplierChangeSpeed 必须大于 0。", this);
        }
    }

    /// <summary>
    /// 执行角色移动。
    /// 
    /// 当前每帧由 PlayerCharacter 调用。
    /// </summary>
    /// <param name="moveInput">玩家移动输入。</param>
    /// <param name="jumpPressed">当前这一帧是否按下跳跃。</param>
    /// <param name="jumpHeld">当前是否按住跳跃。</param>
    public void Move(Vector2 moveInput, bool jumpPressed, bool jumpHeld)
    {
        RefreshGroundedState();
        TryStartJump(jumpPressed);
        ApplyVerticalMotion(jumpHeld);
        MoveCharacter(moveInput);
    }


    /// <summary>
    /// 刷新角色是否在地面上的状态。
    /// </summary>
    private void RefreshGroundedState()
    {
        if (groundDetector != null)
        {
            groundDetector.CheckGrounded();
        }
    }

    /// <summary>
    /// 尝试开始跳跃。
    /// 
    /// 只有在地面上，并且当前这一帧按下跳跃键时，才允许起跳。
    /// </summary>
    /// <param name="jumpPressed">当前这一帧是否按下跳跃。</param>
    private void TryStartJump(bool jumpPressed)
    {
        if (!jumpPressed)
        {
            return;
        }

        if (!IsGrounded)
        {
            return;
        }

        // 防止 GroundCheck 还没离开地面时，连续触发起跳。
        if (verticalVelocity > 0f)
        {
            return;
        }

        if (gravity >= 0f || jumpStartHeight <= 0f)
        {
            Debug.LogError($"{nameof(CharacterMotor)} 跳跃参数错误，无法起跳。", this);
            return;
        }

        verticalVelocity = Mathf.Sqrt(jumpStartHeight * -2f * gravity);

        isJumping = true;
        jumpHoldTimer = maxJumpHoldTime;
        currentGravityMultiplier = 1f;
    }

    /// <summary>
    /// 应用垂直方向运动。
    /// 
    /// 这里负责：
    /// 1. 落地时重置垂直速度
    /// 2. 上升时根据是否长按跳跃选择重力倍率
    /// 3. 下落时使用更大的下落重力
    /// 4. 限制最大下落速度
    /// </summary>
    /// <param name="jumpHeld">当前是否按住跳跃。</param>
    private void ApplyVerticalMotion(bool jumpHeld)
    {
        if (IsGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = groundedStickVelocity;
            isJumping = false;
            jumpHoldTimer = 0f;
            currentGravityMultiplier = 1f;
            return;
        }

        float targetGravityMultiplier = GetTargetGravityMultiplier(jumpHeld);

        currentGravityMultiplier = Mathf.MoveTowards(
            currentGravityMultiplier,
            targetGravityMultiplier,
            gravityMultiplierChangeSpeed * Time.deltaTime
        );

        verticalVelocity += gravity * currentGravityMultiplier * Time.deltaTime;

        if (verticalVelocity < maxFallSpeed)
        {
            verticalVelocity = maxFallSpeed;
        }
    }

    /// <summary>
    /// 获取当前应该使用的目标重力倍率。
    /// 
    /// 规则：
    /// 1. 还在上升，并且玩家按住跳跃，并且长按时间还没用完：使用较小重力
    /// 2. 还在上升，但玩家没按住，或者长按时间用完：使用较大上升重力
    /// 3. 正在下落：使用下落重力
    /// </summary>
    /// <param name="jumpHeld">当前是否按住跳跃。</param>
    /// <returns>目标重力倍率。</returns>
    private float GetTargetGravityMultiplier(bool jumpHeld)
    {
        bool isRising = verticalVelocity > 0f;

        if (isRising)
        {
            bool canUseHoldGravity = isJumping && jumpHeld && jumpHoldTimer > 0f;

            if (canUseHoldGravity)
            {
                jumpHoldTimer -= Time.deltaTime;
                return jumpHoldGravityMultiplier;
            }

            return jumpReleaseGravityMultiplier;
        }

        return fallGravityMultiplier;
    }

    /// <summary>
    /// 执行最终移动。
    /// 
    /// 这里会把水平移动和垂直移动合并，
    /// 然后统一交给 CharacterController.Move。
    /// 
    /// 同时，这里会记录当前水平移动方向，
    /// 供 CharacterRotator 进行角色朝向旋转。
    /// </summary>
    /// <param name="moveInput">玩家输入的移动方向。</param>
    private void MoveCharacter(Vector2 moveInput)
    {
        Vector3 horizontalDirection = GetCameraRelativeMoveDirection(moveInput);

        // 保存当前这一帧的移动方向。
        // 有输入时是世界方向；
        // 没输入时是 Vector3.zero。
        currentMoveDirection = horizontalDirection;

        Vector3 horizontalMotion = horizontalDirection * moveSpeed;
        Vector3 verticalMotion = Vector3.up * verticalVelocity;

        Vector3 finalVelocity = horizontalMotion + verticalMotion;

        characterController.Move(finalVelocity * Time.deltaTime);
    }

    /// <summary>
    /// 把二维移动输入转换成基于相机方向的世界移动方向。
    /// 
    /// 当前规则：
    /// W / 上：沿相机水平前方移动
    /// S / 下：沿相机水平后方移动
    /// A / 左：沿相机水平左方移动
    /// D / 右：沿相机水平右方移动
    /// 
    /// 注意：
    /// 相机可能是俯视角色的，
    /// 所以不能直接使用 moveReference.forward，
    /// 必须把 y 分量去掉，只保留水平面方向。
    /// </summary>
    /// <param name="moveInput">二维移动输入。</param>
    /// <returns>基于相机方向得到的世界移动方向。</returns>
    private Vector3 GetCameraRelativeMoveDirection(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        if (moveReference == null)
        {
            Debug.LogError($"{nameof(CharacterMotor)} 缺少 moveReference，无法计算基于相机的移动方向。", this);
            return Vector3.zero;
        }

        Vector3 cameraForward = moveReference.forward;
        Vector3 cameraRight = moveReference.right;

        // 去掉相机方向中的上下分量。
        // 角色移动只应该发生在水平面上。
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        if (cameraForward.sqrMagnitude <= 0.0001f)
        {
            Debug.LogError($"{nameof(CharacterMotor)} 的 moveReference.forward 在水平面上的长度过小，无法作为移动前方。", this);
            return Vector3.zero;
        }

        if (cameraRight.sqrMagnitude <= 0.0001f)
        {
            Debug.LogError($"{nameof(CharacterMotor)} 的 moveReference.right 在水平面上的长度过小，无法作为移动右方。", this);
            return Vector3.zero;
        }

        cameraForward.Normalize();
        cameraRight.Normalize();

        // moveInput.x转换成基于相机的水平方向移动，moveInput.y转换成基于相机的前后方向移动
        Vector3 moveDirection = cameraRight * moveInput.x + cameraForward * moveInput.y;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        return moveDirection;
    }
}