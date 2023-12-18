using Services;
using UnityEngine;

public class Mission : InputInteractable
{
    public GameObject DrawArrowRadial;
    public GameObject AddRequirementRadial;
    public GameObject AddRewardRadial;
    public GameObject MissionDataPanel;
    
    private InputService InputService;
    private Vector3 DragOffset;

    private bool _radialElementsActive; 
    private bool RadialElementsActive
    {
        get { return _radialElementsActive; }
        set
        {
            _radialElementsActive = value;
            DrawArrowRadial.SetActive(value);
            AddRequirementRadial.SetActive(value);
            AddRewardRadial.SetActive(value);
        }
    }
    
    private void Start()
    {
        if (ServiceLocator.TryGetService(out InputService))
        {
            InputService.RegisterForLeftHold(this, OnMissionDragStart, null, OnMissionDrag, 0f);
            InputService.RegisterForRightTap(this, ToggleMissionRadial);
            InputService.RegisterForFocus(this, ShowMissionDataPanel, HideMissionDataPanel);
        }
        else
        {
            Debug.LogError("InputService not found when instantiating Mission.");
        }
    }

    private void OnMissionDragStart()
    {
        DragOffset = transform.position - InputService.PointerPosition;
    }

    private void OnMissionDrag()
    {
        RadialElementsActive = false;
        gameObject.transform.position = InputService.PointerPosition + DragOffset;
    }

    private void ToggleMissionRadial()
    {
        RadialElementsActive = !RadialElementsActive;
    }

    private void ShowMissionDataPanel()
    {
        MissionDataPanel.SetActive(true);
    }
    
    private void HideMissionDataPanel()
    {
        MissionDataPanel.SetActive(false);
    }
}
