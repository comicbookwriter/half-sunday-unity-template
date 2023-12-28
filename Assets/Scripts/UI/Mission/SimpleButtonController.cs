using System;
using UnityEngine;

namespace UI.Mission
{
    public class SimpleButtonController : UIController<SimpleButtonView, SimpleButtonModel>
    {
        public Action OnClicked;

        public SimpleButtonController(SimpleButtonView view, Action onClick) : base(view) => 
            Setup(onClick);

        public SimpleButtonController(SimpleButtonView view, Action onClick, Transform parent) : base(view, parent) =>
            Setup(onClick);

        public void Setup(Action onClick)
        {
            OnClicked = onClick;
            UiDriver.RegisterForHold(View, OnClickStart, OnClickRelease, null, 0f);
        }

        public void OnClickStart()
        {
            Model.IsHeld = true;
            UpdateViewAtEndOfFrame();
        }

        public void OnClickRelease()
        {
            Model.IsHeld = false;
            OnClicked();
            UpdateViewAtEndOfFrame();
        }
    }
}