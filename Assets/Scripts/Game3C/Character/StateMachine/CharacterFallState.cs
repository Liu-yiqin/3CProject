/// <summary>
/// ÏÂÂä×´Ì¬¡£
/// </summary>
public class CharacterFallState : CharacterState
{
    public CharacterFallState(PlayerCharacter owner, CharacterStateMachine stateMachine)
        : base(owner, stateMachine)
    {
    }

    public override void Tick()
    {
        if (!owner.CharacterMotor.IsGrounded)
        {
            return;
        }

        if (owner.CurrentCommand.HasMoveInput)
        {
            stateMachine.ChangeState(CharacterStateType.Move);
            return;
        }

        stateMachine.ChangeState(CharacterStateType.Idle);
    }
}