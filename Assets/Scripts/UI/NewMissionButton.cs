using Services;
using UnityEngine;

public class NewMissionButton : InputInteractable
{
    [SerializeField]
    private GameObject MissionPrefab;

    private InputService InputService;
    public void Start()
    {
        if (ServiceLocator.TryGetService(out InputService))
        {
            InputService.RegisterForLeftTap(this, GenerateNewMission);
        }
        else
        {
            Debug.LogError("InputService not found when instantiating NewMissionButton.");
        }
    }

    public void GenerateNewMission()
    {
        Instantiate(MissionPrefab, transform);
    }
}
