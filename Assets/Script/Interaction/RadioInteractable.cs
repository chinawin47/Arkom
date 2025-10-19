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
        if (!source || !chantClip) return;
        on = !on;
        if (on)
        {
            source.clip = chantClip;
            source.loop = loop;
            source.Play();
        }
        else
        {
            source.Stop();
        }
    }
}
