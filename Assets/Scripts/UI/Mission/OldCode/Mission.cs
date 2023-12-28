using System.Collections.Generic;
using Services;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Mission : UIInteractable
{
    public GameObject DrawArrowRadial;
    public GameObject AddRequirementRadial;
    public GameObject AddRewardRadial;
    public GameObject MissionDataPanel;
    public Image Image;
    
    private UIDriver InputService;
    private Vector3 DragOffset;

    private int colorIndex = 0;
    private List<Color> Colors = new()
    {
        Color.white,
        Color.blue,
        Color.green,
        Color.red
    };

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
            InputService.RegisterForHold(this, OnMissionDragStart, null, OnMissionDrag, 0f);
            InputService.RegisterForAltTap(this, ToggleMissionRadial);
            InputService.RegisterForFocus(this, ShowMissionDataPanel, HideMissionDataPanel);
            InputService.RegisterForScroll(this, OnScrollUp, OnScrollDown);
            InputService.RegisterForBack(this, Clear);
        }
        else
        {
            Debug.LogError("InputService not found when instantiating Mission.");
        }
    }
    
    private int Mod(int x, int m) => (x%m + m)%m;

    private void OnScrollUp()
    {
        Image.color = Colors[Mod(--colorIndex, Colors.Count)];
    }
        
    private void OnScrollDown()
    {
        Image.color = Colors[Mod(++colorIndex, Colors.Count)];
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

    private void Clear()
    {
        InputService.UnregisterForAll(this);
        Destroy(gameObject);
    }
}