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

    [Header("Jump Settings")]

    /// <summary>
    /// 跳跃高度。
    /// 
    /// 单位：米。
    /// 
    /// 这里不是直接设置跳跃速度，
    /// 而是通过 jumpHeight 和 gravity 计算出起跳速度。
    /// 这样调参时更直观：
    /// 你想让角色跳多高，就填多高。
    /// </summary>
    [SerializeField]
    private float jumpHeight = 1.5f;

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
    }

    /// <summary>
    /// 执行角色移动。
    /// 
    /// 当前这个方法每帧由 PlayerCharacter 调用。
    /// 它内部会同时处理：
    /// 1. 水平移动
    /// 2. 地面检测
    /// 3. 重力
    /// 4. 最终位移
    /// </summary>
    /// <param name="moveInput">玩家输入的移动方向，来自 PlayerCommand。</param>
    public void Move(Vector2 moveInput, bool jumpPressed)
    {
        RefreshGroundedState();
        ApplyGravity();
        ApplyJump(jumpPressed);
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
    /// 应用重力。
    /// 
    /// 如果角色在地面上，并且当前垂直速度小于 0，
    /// 就把垂直速度设置成一个小的向下速度。
    /// 
    /// 如果角色不在地面上，
    /// 就持续累加重力，让角色越来越快地下落。
    /// </summary>
    private void ApplyGravity()
    {
        if (IsGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickVelocity;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    /// <summary>
    /// 应用跳跃。
    /// 
    /// 只有满足以下条件才允许起跳：
    /// 1. 当前这一帧按下了跳跃键
    /// 2. 角色当前在地面上
    /// 3. 当前不是正在向上运动
    /// 
    /// 起跳的本质：
    /// 给 verticalVelocity 一个正数，
    /// 让角色获得向上的初速度。
    /// </summary>
    /// <param name="jumpPressed">当前这一帧是否按下跳跃。</param>
    private void ApplyJump(bool jumpPressed)
    {
        if (!jumpPressed)
        {
            return;
        }

        if (!IsGrounded)
        {
            return;
        }

        if (verticalVelocity > 0f)
        {
            return;
        }

        if (gravity >= 0f)
        {
            Debug.LogError($"{nameof(CharacterMotor)} 的 gravity 必须是负数，否则无法根据 jumpHeight 计算跳跃速度。", this);
            return;
        }

        if (jumpHeight <= 0f)
        {
            Debug.LogError($"{nameof(CharacterMotor)} 的 jumpHeight 必须大于 0。", this);
            return;
        }

        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    /// <summary>
    /// 执行最终移动。
    /// 
    /// 这里会把水平移动和垂直移动合并，
    /// 然后统一交给 CharacterController.Move。
    /// </summary>
    /// <param name="moveInput">玩家输入的移动方向。</param>
    private void MoveCharacter(Vector2 moveInput)
    {
        Vector3 horizontalDirection = GetWorldMoveDirection(moveInput);

        Vector3 horizontalMotion = horizontalDirection * moveSpeed;
        Vector3 verticalMotion = Vector3.up * verticalVelocity;

        Vector3 finalVelocity = horizontalMotion + verticalMotion;

        characterController.Move(finalVelocity * Time.deltaTime);
    }

    /// <summary>
    /// 把二维输入转换成三维世界方向。
    /// 
    /// 当前第 2 步仍然使用世界坐标移动：
    /// W：世界 Z 正方向
    /// S：世界 Z 负方向
    /// A：世界 X 负方向
    /// D：世界 X 正方向
    /// 
    /// 后面第 5 步会改成基于相机方向移动。
    /// </summary>
    /// <param name="moveInput">二维移动输入。</param>
    /// <returns>三维世界移动方向。</returns>
    private Vector3 GetWorldMoveDirection(Vector2 moveInput)
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        return moveDirection;
    }
}