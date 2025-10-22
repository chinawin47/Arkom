using UnityEngine;
using ARKOM.Core;

[AddComponentMenu("Interactable/Radio (Toggle Chant)")]
public class RadioInteractable : Interactable
{
    [Header("Audio")] public AudioSource source;
    public AudioClip chantClip;
    public bool loop = true;

    private bool on;

    void Awake()
    {
        if (!source) source = GetComponent<AudioSource>();
        if (source)
        {
            source.playOnAwake = false; source.loop = loop; source.spatialBlend = 1f;
        }
    }

    protected override void OnInteract(object interactor)
    {
        Toggle();
    }

    public void StartRadio()
    {
        if (on) return;
        on = true;
        if (source && chantClip)
        {
            source.clip = chantClip;
            source.loop = loop;
            source.Play();
        }
        EventBus.Publish(new ARKOM.Story.RadioToggledEvent(on));
    }

    public void StopRadio()
    {
        if (!on) return;
        on = false;
        if (source) source.Stop();
        EventBus.Publish(new ARKOM.Story.RadioToggledEvent(on));
    }

    public void Toggle()
    {
        if (on) StopRadio(); else StartRadio();
    }
}
