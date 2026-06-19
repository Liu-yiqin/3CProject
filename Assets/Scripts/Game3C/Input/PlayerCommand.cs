using UnityEngine;

/// <summary>
/// 玩家输入命令。(玩家所有输入形式的列举)
/// 
/// 它不是直接控制角色移动的脚本，
/// 而是用来保存“这一帧玩家想做什么”。
/// 
/// 当前包含：
/// 1. 移动输入
/// 2. 跳跃按下输入
/// 
/// 后面可以继续扩展：
/// SprintHeld、AttackPressed、LookInput 等。
/// </summary>
public struct PlayerCommand
{
    /// <summary>
    /// 移动输入。
    /// 
    /// X:
    /// -1 表示向左
    ///  1 表示向右
    /// 
    /// Y:
    /// -1 表示向后
    ///  1 表示向前
    /// </summary>
    public Vector2 MoveInput;

    /// <summary>
    /// 当前这一帧是否按下跳跃键。
    /// 
    /// 只在按下 Space 的这一帧为 true。
    /// 用于触发起跳。
    /// </summary>
    public bool JumpPressed;

    /// <summary>
    /// 当前是否按住跳跃键。
    /// 
    /// 按住 Space 期间，每一帧都是 true。
    /// 用于控制长按跳得更高。
    /// </summary>
    public bool JumpHeld;

    /// <summary>
    /// 是否有移动输入。
    /// 
    /// 用 sqrMagnitude 是因为它比 magnitude 更省一点性能，
    /// 这里不需要开平方，只要判断是否接近 0。
    /// </summary>
    public bool HasMoveInput => MoveInput.sqrMagnitude > 0.0001f;
}