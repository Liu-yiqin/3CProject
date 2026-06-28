/// <summary>
/// ÒÆ¶¯×´Ì¬¡£
/// </summary>
public class CharacterMoveState : CharacterState
{
    public CharacterMoveState(PlayerCharacter owner, CharacterStateMachine stateMachine)
        : base(owner, stateMachine)
    {
    }

    public override void Tick()
    {
        if (!owner.CharacterMotor.IsGrounded)
        {
            stateMachine.ChangeState(CharacterStateType.Fall);
            return;
        }

        if (owner.CharacterMotor.VerticalVelocity > 0f)
        {
            stateMachine.ChangeState(CharacterStateType.Jump);
            return;
        }

        if (!owner.CurrentCommand.HasMoveInput)
        {
            stateMachine.ChangeState(CharacterStateType.Idle);
            return;
        }
    }
}