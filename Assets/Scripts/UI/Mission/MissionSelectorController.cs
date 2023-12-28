using UI.Model.Templates;
using UnityEngine;

namespace UI.Mission
{
    public class MissionSelectorController : UIController<MissionSelectorView>
    {
        private MissionModel DefaultMissionModel;
        public MissionSelectorController(MissionSelectorView view) : base(view) => Setup();
        public MissionSelectorController(MissionSelectorTemplate template, Transform parent) : base(template, parent) => Setup();

        public void Setup()
        {
            DefaultMissionModel = new MissionModel
            {
                MissionColor = Color.white,
                MissionDataActive = false,
                RadialElementsActive = false,
                ScreenPos = View.transform.position
            };
            
            AddChild(new SimpleButtonController(View.CreateMissionButton, CreateMission));
            AddChild(new SimpleButtonController(View.CloseButton, Close));
        }

        private void CreateMission()
        {
            MissionController newMission = new (View.MissionPrefabTemplate, DefaultMissionModel);
            newMission.Show();
            AddChild(newMission);
        }
    }
}