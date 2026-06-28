/// <summary>
/// 角色状态基类。
/// 
/// 每个具体状态都继承它。
/// </summary>
public abstract class CharacterState
{
    protected readonly PlayerCharacter owner;
    protected readonly CharacterStateMachine stateMachine;

    /// <summary>
    /// 初始化状态。
    /// </summary>
    protected CharacterState(PlayerCharacter owner, CharacterStateMachine stateMachine)
    {
        this.owner = owner;
        this.stateMachine = stateMachine;
    }

    /// <summary>
    /// 进入状态时调用一次。
    /// </summary>
    public virtual void Enter() { }

    /// <summary>
    /// 每帧更新状态逻辑。
    /// </summary>
    public virtual void Tick() { }

    /// <summary>
    /// 离开状态时调用一次。
    /// </summary>
    public virtual void Exit() { }
}