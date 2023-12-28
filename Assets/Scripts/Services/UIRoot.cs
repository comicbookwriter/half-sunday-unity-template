using UI.Mission;
using UI.Model.Templates;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Services
{
    public class UIRoot : UIInteractable
    {
        public  GraphicRaycaster raycaster;
        public PlayerInput input;
        
        public InputActionReference point; 
        public InputActionReference navigate; 
        public InputActionReference tap; 
        public InputActionReference hold; 
        public InputActionReference altTap;
        public InputActionReference altHold;
        public InputActionReference scroll;
        public InputActionReference back;

        public MissionSelectorTemplate MissionSelectorPrefabDEBUG;

        private UIDriver Controller;

        // The UIRoot inverts the normal controller/view power balance because it needs to bootstrap the UI system
        public void Awake()
        { 
            Controller = new UIDriver(this);
            
            Controller.AddChild(new MissionSelectorController(MissionSelectorPrefabDEBUG, transform));
        }
    }
}