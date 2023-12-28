using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
    
    public class UIDriver : UIController<UIRoot>, IService
    {
        private PointerEventData pointerData;

        private bool holding = false;
        
        private readonly Dictionary<UIInteractable, Action> tapRegistrar = new();
        private readonly Dictionary<UIInteractable, Action> altTapRegistrar = new();
        private readonly Dictionary<UIInteractable, Action> backRegistrar = new();
        private readonly Dictionary<UIInteractable, HoldData> holdRegistrar = new();
        private readonly Dictionary<UIInteractable, HoldData> altHoldRegistrar = new();
        private readonly Dictionary<UIInteractable, FocusData> focusRegistrar = new();
        private readonly Dictionary<UIInteractable, ScrollData> scrollRegistrar = new();

        private UIInteractable _activeObject;
        public UIInteractable ActiveObject
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
        public Transform Root;

        private const string GAMEPAD_CONTROL_GROUP = "Gamepad";
        
        public UIDriver(UIRoot root) : base(root)
        {
            ServiceLocator.RegisterService(this);
            pointerData = new PointerEventData(EventSystem.current);
            Root = root.transform;
            
            root.point.action.performed += OnPointerMove;
            root.tap.action.performed += OnTap;
            root.hold.action.performed += ToggleHold;
            root.altTap.action.performed += OnAltTap;
            //root.altHold.action.performed += OnAltHold; //TODO implement right hold later
            root.scroll.action.performed += OnScroll;
            root.navigate.action.performed += OnNavigate;
            root.back.action.performed += OnBack;
            root.input.onControlsChanged += OnControlsChanged;
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
            tapRegistrar.Add(target, onClick);
        }

        public void UnregisterForTap(UIInteractable target)
        {
            tapRegistrar.Remove(target);
        }
        
        public void RegisterForAltTap(UIInteractable target, Action onClick)
        {
            altTapRegistrar.Add(target, onClick);
        }
        
        public void UnregisterForAltTap(UIInteractable target)
        {
            altTapRegistrar.Remove(target);
        }
        
        public void RegisterForFocus(UIInteractable target, Action onFocusStart, Action onFocusEnd)
        {
            focusRegistrar.Add(target, new FocusData(onFocusStart, onFocusEnd));
        }
        
        public void UnregisterForFocus(UIInteractable target)
        {
            focusRegistrar.Remove(target);
        }
        
        public void RegisterForScroll(UIInteractable target, Action onScrollUp, Action onScrollDown)
        {
            scrollRegistrar.Add(target, new ScrollData(onScrollUp, onScrollDown));
        }
        
        public void UnregisterForScroll(UIInteractable target)
        {
            focusRegistrar.Remove(target);
        }
        
        public void RegisterForHold(UIInteractable target, Action onHold, Action onRelease, Action onFrameHeld, float holdTime = 0.5f)
        {
            holdRegistrar.Add(target, new HoldData(onHold, onFrameHeld, onRelease, holdTime));
        }
        
        public void UnregisterForHold(UIInteractable target)
        {
            holdRegistrar.Remove(target);
        }
        
        public void RegisterForAltHold(UIInteractable target, Action onHold, Action onRelease, Action onFrameHeld, float holdTime = 0.5f)
        {
            altHoldRegistrar.Add(target, new HoldData(onHold, onFrameHeld, onRelease, holdTime));
        }

        public void UnregisterForAltHold(UIInteractable target)
        {
            altHoldRegistrar.Remove(target);
        }
        
        public void RegisterForBack(UIInteractable target, Action onBack)
        {
            backRegistrar.Add(target, onBack);
        }

        public void UnregisterForBack(UIInteractable target)
        {
            backRegistrar.Remove(target);
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

        private UIInteractable RayCastToFindObjectAtMouse()
        {
            List<RaycastResult> results = new();
            UIInteractable castTarget = null;
            View.raycaster.Raycast(pointerData, results);
            foreach (RaycastResult result in results)
            {
                // this should be done with a layer, but then I would need a physics raycaster?
                UIInteractable resultObj = result.gameObject.GetComponent<UIInteractable>();
                if (resultObj != null) 
                {
                    castTarget = RaycastResolutionFunction(castTarget, resultObj) ? castTarget : resultObj;
                }
            }

            if (castTarget != null) return castTarget;
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

        private void OnFocusStart(UIInteractable focusObj)
        {
            if (!focusObj) return;
            if (focusRegistrar.TryGetValue(focusObj, out FocusData onHover))
            {
                onHover.OnFocusStart();
            }
        }
        
        private void OnFocusEnd(UIInteractable focusObj)
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

            UIInteractable nextElement = null;
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
                    View.StartCoroutine(Hold(callbacks));
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

    public struct HoldData
    {
        public Action OnHoldStart;
        public Action OnHoldFrame;
        public Action OnHoldRelease;
        public float ThresholdTime;

        public HoldData(Action onStart, Action onFrame, Action onRelease, float holdTime = 0)
        {
            OnHoldStart = onStart;
            OnHoldFrame = onFrame;
            OnHoldRelease = onRelease;
            ThresholdTime = holdTime;
        }
    }

    public struct FocusData
    {
        public Action OnFocusStart;
        public Action OnFocusEnd;
        public float ThresholdTime;

        public FocusData(Action onStart, Action onEnd, float thresholdTime = 0)
        {
            OnFocusStart = onStart;
            OnFocusEnd = onEnd;
            ThresholdTime = thresholdTime;
        }
    }

    public struct ScrollData
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