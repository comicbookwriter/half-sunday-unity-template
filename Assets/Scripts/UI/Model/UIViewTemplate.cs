using UnityEngine;

namespace UI.Model
{
    public class UIViewTemplate<TView> : ScriptableObject
    {
        public TView Prefab;
    }
}