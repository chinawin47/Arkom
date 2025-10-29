using UnityEngine;
using ARKOM.Scenes.Road;

public class CarCrashTrigger : MonoBehaviour
{
    public RoadSequenceController controller;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(controller.ResetSequenceToStart());
        }
    }
}
