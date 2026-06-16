using UnityEngine;

/// <summary>
/// 地面检测器。
/// 
/// 这个脚本只负责判断角色当前是否站在地面上。
/// 它不负责移动角色，也不负责处理重力。
/// 
/// 当前做法：
/// 使用 Physics.CheckSphere 在 GroundCheck 位置检测一个小球范围。
/// 如果这个小球碰到了地面 Layer，就认为角色在地面上。
/// </summary>
public class GroundDetector : MonoBehaviour
{
    [Header("Ground Check Settings")]

    /// <summary>
    /// 地面检测点。
    /// 
    /// 一般是 Player 子物体里的 GroundCheck。
    /// 推荐放在角色脚底附近。
    /// </summary>
    [SerializeField]
    private Transform groundCheck;

    /// <summary>
    /// 地面检测半径。
    /// 
    /// 这个值不要太大，也不要太小。
    /// 太小：容易检测不到地面。
    /// 太大：可能还没真正落地就判断为落地。
    /// 
    /// 当前可以先用 0.2。
    /// </summary>
    [SerializeField]
    private float groundCheckRadius = 0.2f;

    /// <summary>
    /// 哪些 Layer 会被当成地面。
    /// 
    /// 你需要在 Inspector 里把 Ground Layer 勾上。
    /// 如果你希望角色能站在箱子、平台上，也可以把 Obstacle 勾上。
    /// </summary>
    [SerializeField]
    private LayerMask groundLayerMask;

    /// <summary>
    /// 当前是否站在地面上。
    /// 
    /// 外部脚本可以读取这个属性，
    /// 但不能直接修改它。
    /// </summary>
    public bool IsGrounded { get; private set; }

    /// <summary>
    /// 当前地面检测点的位置。
    /// 
    /// 主要给外部 Debug 或 Gizmos 使用。
    /// </summary>
    public Vector3 GroundCheckPosition
    {
        get
        {
            if (groundCheck == null)
            {
                return transform.position;
            }

            return groundCheck.position;
        }
    }

    /// <summary>
    /// 地面检测半径。
    /// 
    /// 主要给 Gizmos 使用。
    /// </summary>
    public float GroundCheckRadius => groundCheckRadius;

    private void Reset()
    {
        TryAutoFindGroundCheck();
    }

    private void Awake()
    {
        TryAutoFindGroundCheck();
    }

    /// <summary>
    /// 刷新地面检测结果。
    /// 
    /// CharacterMotor 每帧移动前会调用这个方法，
    /// 用来判断角色当前是否站在地面上。
    /// </summary>
    public void CheckGrounded()
    {
        if (groundCheck == null)
        {
            IsGrounded = false;
            return;
        }

        IsGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayerMask,
            QueryTriggerInteraction.Ignore
        );
    }

    /// <summary>
    /// 尝试自动查找 GroundCheck 子物体。
    /// 
    /// 这样做的好处是：
    /// 如果你忘了在 Inspector 里拖引用，
    /// 它也会尝试从 Player 子物体中找到名为 GroundCheck 的节点。
    /// </summary>
    private void TryAutoFindGroundCheck()
    {
        if (groundCheck != null)
        {
            return;
        }

        Transform found = transform.Find("GroundCheck");

        if (found != null)
        {
            groundCheck = found;
        }
    }

    /// <summary>
    /// 在 Scene 视图中画出地面检测范围。
    /// 
    /// 这个方法只用于调试，
    /// 可以帮助你看清楚 GroundCheck 的位置和检测半径。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 checkPosition = GroundCheckPosition;

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
    }
}