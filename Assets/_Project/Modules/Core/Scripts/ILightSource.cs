namespace Simulador.Core
{
    public enum LightState { Red, Amber, Green }

    public interface ILightSource
    {
        LightState CurrentState { get; }
    }
}