using UnityEngine;

namespace UI.Model
{
    public class UIViewTemplate<TView> : ScriptableObject where TView : UIInteractable
    {
        public TView Prefab;
    }
}