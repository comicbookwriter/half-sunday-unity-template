using UnityEngine;

[DisallowMultipleComponent]
public abstract class UIInteractable : MonoBehaviour
{
    public enum UIInteractionPriority
    {
        VeryLow,
        Low,
        Default,
        High,
        VeryHigh
    }

    public UIInteractionPriority Priority = UIInteractionPriority.Default;
}
