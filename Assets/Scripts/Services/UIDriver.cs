using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// This is basically my version of InputSystemUIInputModule, because the existing one has an extremely rigid set
// of actions that I don't particularly care for + raycasting is indiscriminate
namespace Services
{
    public enum UIInputMode
    {
        Pointer, // The ActiveObject is always whatever the mouse is hovering over
        Direct, // The ActiveObject must be set directly; for controllers
        Global // All registered objects are always active; not sure when we would use this, but its easy to code
    }
    
    public class UIDriver : MonoBehaviour, IService
    {
        [SerializeField] private GraphicRaycaster uIRoot;
        [SerializeField] private PlayerInput input;
        private PointerEventData pointerData;

        private bool holding = false;

        public InputActionReference point; 
        public InputActionReference navigate; 
        public InputActionReference tap; 
        public InputActionReference hold; 
        public InputActionReference altTap;
        public InputActionReference altHold;
        public InputActionReference scroll;
        public InputActionReference back;
        
        
        private readonly Dictionary<GameObject, Action> tapRegistrar = new();
        private readonly Dictionary<GameObject, Action> altTapRegistrar = new();
        private readonly Dictionary<GameObject, Action> backRegistrar = new();
        private readonly Dictionary<GameObject, HoldData> holdRegistrar = new();
        private readonly Dictionary<GameObject, HoldData> altHoldRegistrar = new();
        private readonly Dictionary<GameObject, FocusData> focusRegistrar = new();
        private readonly Dictionary<GameObject, ScrollData> scrollRegistrar = new();

        private GameObject _activeObject;
        public GameObject ActiveObject
        {
            get { return _activeObject; }
            set
            {
                // we dont want to change the active object while we are actively in a hold
                if (_activeObject != value && !holding)
                {
                    if (_activeObject != null)
                        OnFocusEnd(_activeObject);
                    if (value != null)
                        OnFocusStart(value);
                    _activeObject = value;
                }
            }
        }
        public Vector3 PointerPosition => pointerData.position;

        private InputDevice currentInputDevice;
        public UIInputMode InputMode;

        private const string GAMEPAD_CONTROL_GROUP = "Gamepad";
        
        private void Awake()
        {
            ServiceLocator.RegisterService(this);
            pointerData = new PointerEventData(EventSystem.current);
            
            point.action.performed += OnPointerMove;
            tap.action.performed += OnTap;
            hold.action.performed += ToggleHold;
            altTap.action.performed += OnAltTap;
            //altHold.action.performed += OnAltHold; //TODO implement right hold later
            scroll.action.performed += OnScroll;
            navigate.action.performed += OnNavigate;
            back.action.performed += OnBack;
            input.onControlsChanged += OnControlsChanged;
        }

        // TODO: Flexible controller support still requires a LOT of work here
        private void OnControlsChanged(PlayerInput playerInput)
        {
            if (playerInput.currentControlScheme == GAMEPAD_CONTROL_GROUP)
            {
                InputMode = UIInputMode.Direct; // if gamepad, use direct navigation
                Cursor.visible = false;
            }
            else
            {
                InputMode = UIInputMode.Pointer; // else use pointer style navigation
                Cursor.visible = true;
            }
        }
        

        public void RegisterForTap(UIInteractable target, Action onClick)
        {
            tapRegistrar.Add(target.gameObject, onClick);
        }

        public void UnregisterForTap(UIInteractable target)
        {
            tapRegistrar.Remove(target.gameObject);
        }
        
        public void RegisterForAltTap(UIInteractable target, Action onClick)
        {
            altTapRegistrar.Add(target.gameObject, onClick);
        }
        
        public void UnregisterForAltTap(UIInteractable target)
        {
            altTapRegistrar.Remove(target.gameObject);
        }
        
        public void RegisterForFocus(UIInteractable target, Action onFocusStart, Action onFocusEnd)
        {
            focusRegistrar.Add(target.gameObject, new FocusData(onFocusStart, onFocusEnd));
        }
        
        public void UnregisterForFocus(UIInteractable target)
        {
            focusRegistrar.Remove(target.gameObject);
        }
        
        public void RegisterForScroll(UIInteractable target, Action onScrollUp, Action onScrollDown)
        {
            scrollRegistrar.Add(target.gameObject, new ScrollData(onScrollUp, onScrollDown));
        }
        
        public void UnregisterForScroll(UIInteractable target)
        {
            focusRegistrar.Remove(target.gameObject);
        }
        
        public void RegisterForHold(UIInteractable target, Action onHold, Action onRelease, Action onFrameHeld, float holdTime = 0.5f)
        {
            holdRegistrar.Add(target.gameObject, new HoldData(onHold, onFrameHeld, onRelease, holdTime));
        }
        
        public void UnregisterForHold(UIInteractable target)
        {
            holdRegistrar.Remove(target.gameObject);
        }
        
        public void RegisterForAltHold(UIInteractable target, Action onHold, Action onRelease, Action onFrameHeld, float holdTime = 0.5f)
        {
            altHoldRegistrar.Add(target.gameObject, new HoldData(onHold, onFrameHeld, onRelease, holdTime));
        }

        public void UnregisterForAltHold(UIInteractable target)
        {
            altHoldRegistrar.Remove(target.gameObject);
        }
        
        public void RegisterForBack(UIInteractable target, Action onBack)
        {
            backRegistrar.Add(target.gameObject, onBack);
        }

        public void UnregisterForBack(UIInteractable target)
        {
            backRegistrar.Remove(target.gameObject);
        }

        public void UnregisterForAll(UIInteractable target)
        {
            UnregisterForTap(target);
            UnregisterForAltTap(target);
            UnregisterForFocus(target);
            UnregisterForBack(target);
            UnregisterForScroll(target);
            UnregisterForHold(target);
            UnregisterForAltHold(target);
        }

        private GameObject RayCastToFindObjectAtMouse()
        {
            List<RaycastResult> results = new();
            UIInteractable castTarget = null;
            uIRoot.Raycast(pointerData, results);
            foreach (RaycastResult result in results)
            {
                // this should be done with a layer, but then I would need a physics raycaster?
                UIInteractable resultObj = result.gameObject.GetComponent<UIInteractable>();
                if (resultObj != null) 
                {
                    castTarget = RaycastResolutionFunction(castTarget, resultObj) ? castTarget : resultObj;
                }
            }

            if (castTarget != null) return castTarget.gameObject;
            return null;
        }
        
        private bool RaycastResolutionFunction(UIInteractable a, UIInteractable b)
        {
            if (a == null) return false;
            if (b == null) return true;
            return (int) a.Priority >= (int) b.Priority;
        }
        
        private void OnPointerMove(InputAction.CallbackContext context)
        {
            pointerData.position = context.ReadValue<Vector2>();
            if(InputMode == UIInputMode.Pointer)
                ActiveObject = RayCastToFindObjectAtMouse();
        }

        private void OnFocusStart(GameObject focusObj)
        {
            if (!focusObj) return;
            if (focusRegistrar.TryGetValue(focusObj, out FocusData onHover))
            {
                onHover.OnFocusStart();
            }
        }
        
        private void OnFocusEnd(GameObject focusObj)
        {
            if (!focusObj) return;
            if (focusRegistrar.TryGetValue(focusObj, out FocusData onHover))
            {
                onHover.OnFocusEnd();
            }
        }
        
        private void OnScroll(InputAction.CallbackContext context)
        {
            if (!ActiveObject) return;
            if (scrollRegistrar.TryGetValue(ActiveObject, out ScrollData onScroll))
            {
                float scrollDirection = context.ReadValue<Vector2>().y;
                if (scrollDirection > 0)
                    onScroll.OnScrollUp();
                if (scrollDirection < 0)
                    onScroll.OnScrollDown();
            }
        }

        // this is horrible, but its a thing
        private void OnNavigate(InputAction.CallbackContext context)
        {
            if (InputMode != UIInputMode.Direct) return;
            Vector2 direction = context.ReadValue<Vector2>().normalized;

            GameObject nextElement = null;
            while (nextElement == null && PointerInBounds(pointerData))
            {
                pointerData.position += direction;
                nextElement = RayCastToFindObjectAtMouse();
            }

            pointerData.position  = Mouse.current.position.ReadValue(); // return the pointer to sanity
            if (nextElement != null)
                ActiveObject = nextElement;
        }

        private bool PointerInBounds(PointerEventData data)
        {
            return data.position.x >= 0 &&
                   data.position.x <= Screen.width &&
                   data.position.y >= 0 &&
                   data.position.y <= Screen.height;
        }

        private void OnTap(InputAction.CallbackContext context)
        {
            if (InputMode != UIInputMode.Global && !ActiveObject) return;
            if (tapRegistrar.TryGetValue(ActiveObject, out Action onClick))
            {
                onClick();
            }
        }
        
        private void OnAltTap(InputAction.CallbackContext context)
        {
            if (InputMode != UIInputMode.Global && !ActiveObject) return;
            if (altTapRegistrar.TryGetValue(ActiveObject, out Action onClick))
            {
                onClick();
            }
        }

        private void ToggleHold(InputAction.CallbackContext context)
        {
            holding = !holding;
            if (holding)
            {
                if (InputMode != UIInputMode.Global && !ActiveObject) return;
                if (holdRegistrar.TryGetValue(ActiveObject, out HoldData callbacks))
                {
                    StartCoroutine(Hold(callbacks));
                }
            }
        }
        
        private IEnumerator Hold(HoldData callbacks)
        {
            float timeElapsed = 0f;
            WaitForEndOfFrame waitUntilNextFrame = new WaitForEndOfFrame();
            bool holdStarted = false;
            while (holding)
            {
                timeElapsed += Time.deltaTime;
                if (timeElapsed > callbacks.ThresholdTime)
                {
                    if (!holdStarted)
                    {
                        callbacks.OnHoldStart?.Invoke();
                        holdStarted = true;
                    }
                    else
                    {
                        callbacks.OnHoldFrame?.Invoke();
                    }
                }
                yield return waitUntilNextFrame;
            }
            callbacks.OnHoldRelease?.Invoke();
            yield return null;
        }
        
        private void OnBack(InputAction.CallbackContext context)
        {
            // the back callback is special, and fires regardless of active status
            // if activity needs to be handled, deal with it in the UI system
            var actions = new List<Action>(backRegistrar.Values);
            foreach(Action onBack in actions)
            {
                onBack();
            }
        }
    }

    public class HoldData
    {
        public Action OnHoldStart;
        public Action OnHoldFrame;
        public Action OnHoldRelease;
        public float ThresholdTime;

        public HoldData(Action onStart, Action onFrame, Action onRelease, float holdTime)
        {
            OnHoldStart = onStart;
            OnHoldFrame = onFrame;
            OnHoldRelease = onRelease;
            ThresholdTime = holdTime;
        }
    }

    public class FocusData
    {
        public Action OnFocusStart;
        public Action OnFocusEnd;
        public float ThresholdTime;

        public FocusData(Action onStart, Action onEnd)
        {
            OnFocusStart = onStart;
            OnFocusEnd = onEnd;
        }
    }

    public class ScrollData
    {
        public Action OnScrollUp;
        public Action OnScrollDown;

        public ScrollData(Action onScrollUp, Action onScrollDown)
        {
            OnScrollUp = onScrollUp;
            OnScrollDown = onScrollDown;
        }
    }
}