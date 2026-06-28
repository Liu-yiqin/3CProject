using System.Collections.Generic;

/// <summary>
/// ½ÇÉ«×´Ì¬»ú¡£
/// 
/// ¸ºÔð±£´æµ±Ç°×´Ì¬£¬
/// ²¢Ö´ÐÐ×´Ì¬ÇÐ»»¡£
/// </summary>
public class CharacterStateMachine
{
    private readonly Dictionary<CharacterStateType, CharacterState> states = new();

    public CharacterState CurrentState { get; private set; }

    public CharacterStateType CurrentStateType { get; private set; }

    /// <summary>
    /// ×¢²á×´Ì¬¡£
    /// </summary>
    public void RegisterState(CharacterStateType type, CharacterState state)
    {
        states.Add(type, state);
    }

    /// <summary>
    /// ÇÐ»»×´Ì¬¡£
    /// </summary>
    public void ChangeState(CharacterStateType type)
    {
        if (!states.TryGetValue(type, out CharacterState nextState))
        {
            throw new System.Exception($"×´Ì¬Î´×¢²á£º{type}");
        }

        if (CurrentStateType == type && CurrentState != null)
        {
            return;
        }

        CurrentState?.Exit();

        CurrentState = nextState;
        CurrentStateType = type;

        CurrentState.Enter();
    }

    /// <summary>
    /// ¸üÐÂµ±Ç°×´Ì¬¡£
    /// </summary>
    public void Tick()
    {
        CurrentState?.Tick();
    }
}