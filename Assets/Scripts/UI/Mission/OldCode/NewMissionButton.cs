using Services;
using UnityEngine;

public class NewMissionButton : UIInteractable
{
    [SerializeField]
    private GameObject MissionPrefab;

    private UIDriver InputService;
    public void Start()
    {
        if (ServiceLocator.TryGetService(out InputService))
        {
            InputService.RegisterForTap(this, GenerateNewMission);
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
