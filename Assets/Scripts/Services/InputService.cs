using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Services
{
    public enum InputMode
    {
        UI, // The ActiveObject is always whatever the mouse is hovering over
        Direct, // The ActiveObject must be set directly
        Global // all objects that are currently registered fire
    }
    public class InputService : MonoBehaviour, IService
    {
        [SerializeField] private GraphicRaycaster uIRoot;
        private PointerEventData pointerData;
        private PlayerInput input;
        
        private bool holding = false;

        private Dictionary<GameObject, Action> leftTapRegistrar = new();
        private Dictionary<GameObject, Action> rightTapRegistrar = new();
        private Dictionary<GameObject, HoldData> leftHoldRegistrar = new();
        private Dictionary<GameObject, FocusData> focusRegistrar = new();
        private Dictionary<GameObject, ScrollData> scrollRegistrar = new();

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
        public InputMode InputMode = InputMode.UI;
        
        private void Awake()
        {
            ServiceLocator.RegisterService(this);
            input = GetComponent<PlayerInput>();
            
            pointerData = new PointerEventData(EventSystem.current);
            
            input.actions["Point"].performed += OnMouseMove;
            input.actions["Click"].performed += OnLeftTap;
            input.actions["Hold"].performed += ToggleLeftHold;
            input.actions["RightClick"].performed += OnRightTap;
            input.actions["ScrollWheel"].performed += OnScroll;
        }
        

        public void RegisterForLeftTap(InputInteractable target, Action onClick)
        {
            leftTapRegistrar.Add(target.gameObject, onClick);
        }
        
        public void UnregisterForLeftTap(InputInteractable target)
        {
            leftTapRegistrar.Remove(target.gameObject);
        }
        
        public void RegisterForRightTap(InputInteractable target, Action onClick)
        {
            rightTapRegistrar.Add(target.gameObject, onClick);
        }
        
        public void UnregisterForRightTap(InputInteractable target)
        {
            rightTapRegistrar.Remove(target.gameObject);
        }
        
        public void RegisterForFocus(InputInteractable target, Action onFocusStart, Action onFocusEnd)
        {
            focusRegistrar.Add(target.gameObject, new FocusData(onFocusStart, onFocusEnd));
        }
        
        public void UnregisterForFocus(InputInteractable target)
        {
            focusRegistrar.Remove(target.gameObject);
        }
        
        public void RegisterForScroll(InputInteractable target, Action onScrollStart, Action onScrollEnd)
        {
            scrollRegistrar.Add(target.gameObject, new ScrollData(onScrollStart, onScrollEnd));
        }
        
        public void UnregisterForScroll(InputInteractable target)
        {
            focusRegistrar.Remove(target.gameObject);
        }
        
        public void RegisterForLeftHold(InputInteractable target, Action onHold, Action onRelease, Action onFrameHeld, float holdTime = 0.5f)
        {
            leftHoldRegistrar.Add(target.gameObject, new HoldData(onHold, onFrameHeld, onRelease, holdTime));
        }
        
        public void UnregisterForLeftHold(InputInteractable target)
        {
            leftHoldRegistrar.Remove(target.gameObject);
        }
        
        private GameObject RayCastToFindObjectAtMouse()
        {
            List<RaycastResult> results = new();
            InputInteractable castTarget = null;
            uIRoot.Raycast(pointerData, results);
            foreach (RaycastResult result in results)
            {
                // this should be done with a layer, but then I would need a physics raycaster?
                InputInteractable resultObj = result.gameObject.GetComponent<InputInteractable>();
                if (resultObj != null) 
                {
                    castTarget = RaycastResolutionFunction(castTarget, resultObj) ? castTarget : resultObj;
                }
            }
            return castTarget?.gameObject;
        }
        
        private bool RaycastResolutionFunction(InputInteractable a, InputInteractable b)
        {
            if (a == null) return false;
            if (b == null) return true;
            return false;
        }
        
        private void OnMouseMove(InputAction.CallbackContext context)
        {
            pointerData.position = Mouse.current.position.ReadValue();
            if(InputMode == InputMode.UI)
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
                float scrollDirection = context.ReadValue<float>();
                if (scrollDirection > 0)
                    onScroll.OnScrollUp();
                if (scrollDirection < 0)
                    onScroll.OnScrollDown();
            }
        }

        private void OnLeftTap(InputAction.CallbackContext context)
        {
            if (InputMode != InputMode.Global && !ActiveObject) return;
            if (leftTapRegistrar.TryGetValue(ActiveObject, out Action onClick))
            {
                onClick();
            }
        }
        
        private void OnRightTap(InputAction.CallbackContext context)
        {
            if (InputMode != InputMode.Global && !ActiveObject) return;
            if (rightTapRegistrar.TryGetValue(ActiveObject, out Action onClick))
            {
                onClick();
            }
        }

        private void ToggleLeftHold(InputAction.CallbackContext context)
        {
            holding = !holding;
            if (holding)
            {
                if (InputMode != InputMode.Global && !ActiveObject) return;
                if (leftHoldRegistrar.TryGetValue(ActiveObject, out HoldData callbacks))
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