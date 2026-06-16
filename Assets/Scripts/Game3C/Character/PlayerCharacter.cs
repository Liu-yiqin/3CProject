using UnityEngine;

/// <summary>
/// 玩家角色总入口。
/// 
/// 当前它负责把输入层和角色移动层连接起来。
/// 
/// 当前它做的事情是：
/// 1. 从 PlayerInputReader 获取玩家输入命令
/// 2. 把移动输入交给 CharacterMotor
/// 
/// 后面这里会继续扩展：
/// 状态机、动画控制、跳跃、冲刺、攻击等。
/// </summary>
public class PlayerCharacter : MonoBehaviour
{
    [Header("References")]

    /// <summary>
    /// 输入读取器。
    /// 负责读取键盘、手柄、摇杆等输入。
    /// </summary>
    [SerializeField]
    private PlayerInputReader inputReader;

    /// <summary>
    /// 角色移动执行器。
    /// 负责真正移动角色。
    /// </summary>
    [SerializeField]
    private CharacterMotor characterMotor;

    private void Awake()
    {
        // 如果 Inspector 里没有手动拖引用，
        // 就尝试从当前 Player 物体上自动获取。
        if (inputReader == null)
        {
            inputReader = GetComponent<PlayerInputReader>();
        }

        if (characterMotor == null)
        {
            characterMotor = GetComponent<CharacterMotor>();
        }
    }

    private void Update()
    {
        // 安全检查。
        // 防止组件没挂导致空引用报错。
        if (inputReader == null || characterMotor == null)
        {
            return;
        }

        // 先读取当前这一帧输入。
        inputReader.ReadInput();

        // 再拿到当前这一帧的输入命令。
        PlayerCommand command = inputReader.CurrentCommand;

        // 把移动输入和跳跃输入交给角色移动系统。
        characterMotor.Move(command.MoveInput, command.JumpPressed);
    }
}