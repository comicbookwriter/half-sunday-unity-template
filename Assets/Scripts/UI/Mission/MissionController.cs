using System.Collections.Generic;
using UI.Model.Templates;
using UnityEngine;

namespace UI.Mission
{
    public class MissionController : UIController<MissionView, MissionModel>
    {
        private Vector3 DragOffset;

        private int colorIndex = 0;
        private List<Color> Colors = new()
        {
            Color.white,
            Color.blue,
            Color.green,
            Color.red
        };

        public MissionController(MissionView view, MissionModel model) : base(view, model) =>
            Setup();
        
        public MissionController(MissionTemplate template, MissionModel model, Transform parent = null) : base(template, parent, model) =>
            Setup();

        public void Setup()
        {
            Debug.Log(View.name);
            UiDriver.RegisterForHold(View, OnMissionDragStart, null, OnMissionDrag, 0f);
            UiDriver.RegisterForAltTap(View, Close);
            UiDriver.RegisterForFocus(View, ShowMissionDataPanel, HideMissionDataPanel);
            UiDriver.RegisterForScroll(View, OnScrollUp, OnScrollDown);
            UiDriver.RegisterForBack(View, Close);
        }

        private int Mod(int x, int m) => (x%m + m)%m;

        private void OnScrollUp()
        {
            Model.MissionColor = Colors[Mod(--colorIndex, Colors.Count)];
            UpdateViewAtEndOfFrame();
        }
        
        private void OnScrollDown()
        {
            Model.MissionColor = Colors[Mod(++colorIndex, Colors.Count)];
            UpdateViewAtEndOfFrame();
        }

        private void OnMissionDragStart()
        {
            DragOffset = View.transform.position - UiDriver.PointerPosition;
        }

        private void OnMissionDrag()
        {
            Model.RadialElementsActive = false;
            Model.ScreenPos = UiDriver.PointerPosition + DragOffset;
            UpdateViewAtEndOfFrame();
        }

        private void ToggleMissionRadial()
        {
            Model.RadialElementsActive = !Model.RadialElementsActive;
            UpdateViewAtEndOfFrame();
        }

        private void ShowMissionDataPanel()
        {
            Model.MissionDataActive = true;
            UpdateViewAtEndOfFrame();
        }
    
        private void HideMissionDataPanel()
        {
            Model.MissionDataActive = false;
            UpdateViewAtEndOfFrame();
        }
    }
}