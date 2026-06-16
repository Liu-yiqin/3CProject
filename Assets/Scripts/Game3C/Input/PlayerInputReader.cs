using UnityEngine;

/// <summary>
/// 玩家输入读取器。(主要用CurrentCommand来记录玩家当前帧的输入行为)
/// 
/// 这个脚本只负责读取输入，
/// 不负责移动角色，
/// 不负责修改 Transform，
/// 不负责判断角色状态。
/// 
/// 也就是说，它只做 Control 层的事情：
/// “玩家现在想做什么？”：把玩家的抽象输入转换成具体的游戏想法
/// </summary>
public class PlayerInputReader : MonoBehaviour
{
    /// <summary>
    /// 当前这一帧的玩家输入命令。
    /// 
    /// 外部角色系统可以通过这个属性读取输入结果。
    /// </summary>
    public PlayerCommand CurrentCommand { get; private set; }

    /// <summary>
    /// 读取当前这一帧的所有玩家输入。
    /// 
    /// 当前第 3 步包含：
    /// 1. 移动输入
    /// 2. 跳跃输入
    /// 
    /// 后续如果加入冲刺、攻击、镜头输入，
    /// 也可以继续在这里扩展。
    /// </summary>
    public void ReadInput()
    {
        Vector2 moveInput = ReadMoveInput();
        bool jumpPressed = ReadJumpInput();

        CurrentCommand = new PlayerCommand
        {
            MoveInput = moveInput,
            JumpPressed = jumpPressed
        };
    }

    /// <summary>
    /// 读取移动输入。
    /// 
    /// 当前使用 Unity 旧输入系统里的默认轴：
    /// Horizontal:
    ///     A / D
    ///     LeftArrow / RightArrow
    /// 
    /// Vertical:
    ///     W / S
    ///     UpArrow / DownArrow
    /// </summary>
    /// <returns>当前这一帧的二维移动输入。</returns>
    private Vector2 ReadMoveInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector2 moveInput = new Vector2(horizontal, vertical);

        // 防止斜向移动速度变快。
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        return moveInput;
    }

    /// <summary>
    /// 读取跳跃输入。
    /// 
    /// 当前先使用 Space 键作为跳跃键。
    /// 
    /// 这里使用 GetKeyDown，
    /// 表示只在按下 Space 的这一帧返回 true。
    /// </summary>
    /// <returns>当前这一帧是否按下跳跃键。</returns>
    private bool ReadJumpInput()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }
}