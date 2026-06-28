/// <summary>
/// ÌøÔ¾ÉÏÉı×´Ì¬¡£
/// </summary>
public class CharacterJumpState : CharacterState
{
    public CharacterJumpState(PlayerCharacter owner, CharacterStateMachine stateMachine)
        : base(owner, stateMachine)
    {
    }

    public override void Tick()
    {
        if (owner.CharacterMotor.VerticalVelocity <= 0f)
        {
            stateMachine.ChangeState(CharacterStateType.Fall);
            return;
        }
    }
}