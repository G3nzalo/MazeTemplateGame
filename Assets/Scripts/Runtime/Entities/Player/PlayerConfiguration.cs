public class PlayerConfiguration
{
    public readonly IInput Input;
    public readonly float Torque;

    public PlayerConfiguration (IInput input, float torque)
    {
        Input = input;
        Torque = torque;
    }
}
