using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class LevelDesigner : MonoBehaviour
{
    [Serializable]
    public class ColorPresets
    {
        [ShowIf(nameof(applyColorToTrack))] public Material trackMaterial;
        [ShowIf(nameof(applyColorToMusic))] public Material musicMaterial;
    }

    [SerializeField] private bool useSeed;
    [SerializeField] private bool useRandomColors;
    [SerializeField, HideIf(nameof(useRandomColors))] private bool useFixedPreset;
    [SerializeField, ShowIf(nameof(showFixedPresetIndex))] private int fixedPresetIndex;
    [SerializeField, HideIf(nameof(useRandomColors))] private ColorPresets[] colorPresets;

    [SerializeField] private bool applyColorToTrack = true;
    [SerializeField, ShowIf(nameof(applyColorToTrack))] private LineRenderer[] trackBorders;
    [SerializeField] private bool applyColorToMusic = true;
    [SerializeField, ShowIf(nameof(applyColorToMusic))] private LineRenderer[] musicLines;

    private bool showFixedPresetIndex => !useRandomColors && useFixedPreset;

    private void OnValidate()
    {
        fixedPresetIndex = Mathf.Clamp(fixedPresetIndex, 0, colorPresets.Length - 1);
    }

    /// <summary>
    /// Sets the level design colors based on the settings in the inspector.
    /// </summary>
    /// <remarks>
    /// If <see cref="useRandomColors"/> is true, the level design colors are randomly generated.
    /// If <see cref="useFixedPreset"/> is true, the level design colors are chosen from the <see cref="colorPresets"/> array at the index specified by <see cref="fixedPresetIndex"/>.
    /// If <see cref="useSeed"/> is true, the random number generator is seeded with the current value of <see cref="Random.seed"/>, otherwise it is seeded with the current value of <see cref="DateTime.Now.Ticks"/>.
    /// </remarks>
    public void SetLevelDesign()
    {
        System.Random random = new System.Random();

        if (useRandomColors)
        {
            float adjustedIntensity = 1.5831F;

            float r;
            float g;
            float b;

            if (applyColorToTrack)
            {
                r = useSeed ? UnityEngine.Random.Range(0f, 1f) : (float)random.NextDouble();
                g = useSeed ? UnityEngine.Random.Range(0f, 1f) : (float)random.NextDouble();
                b = useSeed ? UnityEngine.Random.Range(0f, 1f) : (float)random.NextDouble();

                Color rndTrackColor = new Color(r, g, b);
                rndTrackColor *= Mathf.Pow(2.0F, adjustedIntensity);

                for (int i = 0; i < trackBorders.Length; i++)
                {
                    trackBorders[i].material.SetColor("_EmissionColor", rndTrackColor);
                }
            }

            if (applyColorToMusic)
            {
                r = useSeed ? UnityEngine.Random.Range(0f, 1f) : (float)random.NextDouble();
                g = useSeed ? UnityEngine.Random.Range(0f, 1f) : (float)random.NextDouble();
                b = useSeed ? UnityEngine.Random.Range(0f, 1f) : (float)random.NextDouble();

                Color rndMusicColor = new Color(r, g, b);
                rndMusicColor *= Mathf.Pow(2.0F, adjustedIntensity);

                for (int i = 0; i < musicLines.Length; i++)
                {
                    musicLines[i].material.SetColor("_EmissionColor", rndMusicColor);
                }
            }
        }
        else
        {
            int rndPresetIndex = useSeed ? UnityEngine.Random.Range(0, colorPresets.Length) : random.Next(0,colorPresets.Length);
            if (useFixedPreset) rndPresetIndex = fixedPresetIndex;

            ColorPresets rndPreset = colorPresets[rndPresetIndex];

            if (applyColorToTrack)
            {
                for (int i = 0; i < trackBorders.Length; i++)
                {
                    trackBorders[i].material = rndPreset.trackMaterial;
                }
            }

            if (applyColorToMusic)
            {
                for (int i = 0; i < musicLines.Length; i++)
                {
                    musicLines[i].material = rndPreset.musicMaterial;
                }
            }
        }
    }
}
