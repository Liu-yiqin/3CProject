using UnityEngine;

/// <summary>
/// 玩家角色总入口。
/// 
/// 当前第 9 步中，它负责：
/// 1. 读取输入
/// 2. 驱动角色移动
/// 3. 驱动角色旋转
/// 4. 更新角色状态机
/// </summary>
public class PlayerCharacter : MonoBehaviour
{
    [Header("Control")]

    [SerializeField]
    private PlayerInputReader inputReader;

    [Header("Character")]

    [SerializeField]
    private CharacterMotor characterMotor;

    [SerializeField]
    private CharacterRotator characterRotator;

    private CharacterStateMachine stateMachine;

    public PlayerCommand CurrentCommand { get; private set; }

    public CharacterMotor CharacterMotor => characterMotor;

    public CharacterStateType CurrentStateType => stateMachine.CurrentStateType;

    private void Awake()
    {
        ValidateReferences();
        InitializeStateMachine();
    }

    private void Start()
    {
        stateMachine.ChangeState(CharacterStateType.Idle);
    }

    private void Update()
    {
        inputReader.ReadInput();

        CurrentCommand = inputReader.CurrentCommand;

        characterMotor.Move(
            CurrentCommand.MoveInput,
            CurrentCommand.JumpPressed,
            CurrentCommand.JumpHeld
        );

        characterRotator.RotateTowards(characterMotor.CurrentMoveDirection);

        stateMachine.Tick();
    }

    /// <summary>
    /// 初始化状态机。
    /// </summary>
    private void InitializeStateMachine()
    {
        stateMachine = new CharacterStateMachine();

        stateMachine.RegisterState(
            CharacterStateType.Idle,
            new CharacterIdleState(this, stateMachine)
        );

        stateMachine.RegisterState(
            CharacterStateType.Move,
            new CharacterMoveState(this, stateMachine)
        );

        stateMachine.RegisterState(
            CharacterStateType.Jump,
            new CharacterJumpState(this, stateMachine)
        );

        stateMachine.RegisterState(
            CharacterStateType.Fall,
            new CharacterFallState(this, stateMachine)
        );
    }

    /// <summary>
    /// 校验引用。
    /// </summary>
    private void ValidateReferences()
    {
        bool hasError = false;

        if (inputReader == null)
        {
            Debug.LogError($"{nameof(PlayerCharacter)} 缺少 PlayerInputReader 引用。", this);
            hasError = true;
        }

        if (characterMotor == null)
        {
            Debug.LogError($"{nameof(PlayerCharacter)} 缺少 CharacterMotor 引用。", this);
            hasError = true;
        }

        if (characterRotator == null)
        {
            Debug.LogError($"{nameof(PlayerCharacter)} 缺少 CharacterRotator 引用。", this);
            hasError = true;
        }

        if (hasError)
        {
            enabled = false;
        }
    }

    private void OnGUI()
    {
        GUI.Label(
            new Rect(20, 20, 300, 30),
            $"State: {CurrentStateType}"
        );
    }
}