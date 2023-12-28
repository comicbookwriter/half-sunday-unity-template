using UnityEngine;
using UnityEngine.UI;

namespace UI.Mission
{
    public class MissionView : UIView<MissionModel>
    {
        [SerializeField] private Image MissionImage;
        [SerializeField] private GameObject DrawArrowRadial;
        [SerializeField] private GameObject AddRequirementRadial;
        [SerializeField] private GameObject AddRewardRadial;
        [SerializeField] private GameObject MissionDataPanel;

        public void Start()
        {
            Priority = UIInteractionPriority.High;
        }
        
        public override void UpdateViewWithModel(MissionModel model)
        {
            transform.position = model.ScreenPos;
            MissionImage.color = model.MissionColor;
            DrawArrowRadial.SetActive(model.RadialElementsActive);
            AddRequirementRadial.SetActive(model.RadialElementsActive);
            AddRewardRadial.SetActive(model.RadialElementsActive);
            MissionDataPanel.SetActive(model.MissionDataActive);
        }
    }
}