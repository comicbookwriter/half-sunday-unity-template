using UnityEngine;

[DisallowMultipleComponent]
public abstract class InputInteractable : MonoBehaviour
{
    public enum InteractionPriority
    {
        VeryLow,
        Low,
        Default,
        High,
        VeryHigh
    }

    public InteractionPriority Priority { get; } = InteractionPriority.Default;
}
