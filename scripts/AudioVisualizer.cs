using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using NaughtyAttributes;

public class AudioVisualizer : MonoBehaviour
{
    [SerializeField, Range(0.01f, 10f)] private float updateRate = 0.1f;
    [SerializeField] private float distanceToTrack = 1f;
    [SerializeField] private float heightDifference = 0f;
    [SerializeField] private bool playAtStart = true;
    [SerializeField] private bool useRandomSample;
    [SerializeField, HideIf(nameof(useRandomSample))] private Vector2 minMaxSampleUsage;
    [SerializeField] private bool useMusicPlayerAsSource = true;
    [SerializeField, HideIf(nameof(useMusicPlayerAsSource))] private AudioSource audioSource;
    [SerializeField] private LineRenderer leftLineRenderer;
    [SerializeField] private LineRenderer rightLineRenderer;
    [SerializeField] private int extraPoints = 3;
    [SerializeField] private float effectStrength = 5f;
    [SerializeField] private GameObject scaler;

    private bool visualize;
    public bool Visualize
    {
        get => visualize;
        set
        {
            if (visualize == value) return;

            visualize = value;
        }
    }

    private float[] samples = new float[64];
    private Vector3[] leftLinePositions;
    private Vector3[] rightLinePositions;
    private int currentSampleUsageIndex;

    private LevelCreator levelCreator;

    private void OnDestroy()
    {
        Visualize = false;
        if (levelCreator) levelCreator.OnLevelCreated -= LevelCreator_OnLevelCreated;
    }

    void Start()
    {
        if (useMusicPlayerAsSource)
        {
            audioSource = MusicController.Instance.GetComponent<AudioSource>();
        }

        levelCreator = LevelCreator.Instance;
        if (levelCreator) levelCreator.OnLevelCreated += LevelCreator_OnLevelCreated;

        if (playAtStart) StartVisualize();
    }

    private void LevelCreator_OnLevelCreated(Vector3[] leftLinePoints, Vector3[] rightLinePoints)
    {
        SetLines(leftLinePoints, rightLinePoints);
        StartVisualize(1024);
    }

    private void SetLines(Vector3[] leftPoints, Vector3[] rightPoints)
    {
        for (int i = 0; i < leftPoints.Length; i++)
        {
            leftPoints[i] -= new Vector3(i < 1 || i > leftPoints.Length - 2 ? 0 : distanceToTrack, -heightDifference, i < 2 ? distanceToTrack : i > leftPoints.Length - 3 ? -distanceToTrack : 0);
            rightPoints[i] += new Vector3(i < 1 || i > leftPoints.Length - 2 ? 0 : distanceToTrack, heightDifference, i < 2 ? -distanceToTrack : i > leftPoints.Length - 3 ? distanceToTrack : 0);

            //leftPoints[i] -= new Vector3(i > leftPoints.Length - 1 ? 0 : distanceToTrack, -heightDifference, i < 2 ? distanceToTrack : 0);
            //rightPoints[i] += new Vector3(i < leftPoints.Length - 1 ? 0 : distanceToTrack, heightDifference, i < 2 ? -distanceToTrack : 0);
        }

        List<Vector3> allLeftPoints = new List<Vector3>();
        List<Vector3> allRightPoints = new List<Vector3>();

        allLeftPoints.AddRange(leftPoints);
        allRightPoints.AddRange(rightPoints);

        int currentIndex = 0;

        for (int i = 1; i < leftPoints.Length; i++)
        {
            Vector3 newPos = leftPoints[i];
            currentIndex++;

            for (int j = 1; j < extraPoints; j++)
            {
                newPos = Vector3.Lerp(leftPoints[i - 1], leftPoints[i], 1f / extraPoints * j);
                allLeftPoints.Insert(currentIndex, newPos);
                currentIndex++;
            }
        }

        currentIndex = 0;

        for (int i = 1; i < rightPoints.Length; i++)
        {
            Vector3 newPos = rightPoints[i];
            currentIndex++;

            for (int j = 1; j < extraPoints; j++)
            {
                newPos = Vector3.Lerp(rightPoints[i - 1], rightPoints[i], 1f / extraPoints * j);
                allRightPoints.Insert(currentIndex, newPos);
                currentIndex++;
            }
        }

        leftLinePositions = allLeftPoints.ToArray();
        rightLinePositions = allRightPoints.ToArray();

        leftLineRenderer.positionCount = leftLinePositions.Length;
        rightLineRenderer.positionCount = rightLinePositions.Length;

        leftLineRenderer.SetPositions(leftLinePositions);
        rightLineRenderer.SetPositions(rightLinePositions);
    }

    private void GetSpectrumAudioSource()
    {
        if (audioSource.isPlaying)
        {
            audioSource.GetSpectrumData(samples, 0, FFTWindow.Blackman);
        }
    }

    public void SlowMusic(bool slow)
    {
        audioSource.pitch = slow ? 0.5f : 1f;
    }

    public void StartVisualize(int sampleCount = 64)
    {
        if (Visualize) return;

        Visualize = true;
        samples = new float[sampleCount];
        SetVisualEffect();
    }

    private void SetRandomSamples()
    {
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = UnityEngine.Random.Range(0f, 0.2f);
        }
    }

    async private void SetVisualEffect()
    {
        if (leftLineRenderer && rightLineRenderer)
        {
            leftLinePositions = new Vector3[leftLineRenderer.positionCount];
            leftLineRenderer.GetPositions(leftLinePositions);
        }

        while (true)
        {
            if (!Visualize) break;

            if (audioSource.mute)
            {
                SetRandomSamples();
            }
            else
            {
                GetSpectrumAudioSource();
            }

            if (samples == null || samples.Length == 0) return;

            if (scaler)
            {
                Vector3 scale = Vector3.Lerp(Vector3.one, Vector3.one * 1.1f, samples.Max() * 100f * effectStrength);
                scaler.transform.localScale = scale;
            }

            if (leftLineRenderer && rightLineRenderer)
            {

                for (int i = extraPoints + 1; i < leftLineRenderer.positionCount; i++)
                {
                    int rndSample = 0;
                    if (useRandomSample)
                    {
                        rndSample = UnityEngine.Random.Range(0, samples.Length - 1);
                    }
                    int sampleIndex = useRandomSample ? rndSample : currentSampleUsageIndex;

                    //for (int i = extraPoints + 1; i < leftLineRenderer.positionCount; i++)
                    {
                        Vector3 newPos = leftLinePositions[i];
                        Vector3 direction = leftLinePositions[i] - leftLinePositions[i - 1];
                        Vector3 normal = new Vector3(-direction.z, 0f, direction.x);
                        newPos = newPos + (normal * samples[sampleIndex] * effectStrength * Time.deltaTime);
                        newPos.x = Mathf.Clamp(newPos.x, leftLinePositions[i].x - 0.5f, leftLinePositions[i].x + 0.5f);
                        newPos.y = 1f + heightDifference;
                        newPos.z = Mathf.Clamp(newPos.z, leftLinePositions[i].z - 0.5f, leftLinePositions[i].z + 0.5f);
                        leftLineRenderer.SetPosition(i, newPos);
                    }

                    //for (int i = extraPoints + 1; i < rightLineRenderer.positionCount; i++)
                    {
                        Vector3 newPos = rightLinePositions[i];
                        Vector3 direction = rightLinePositions[i] - rightLinePositions[i - 1];
                        Vector3 normal = new Vector3(-direction.z, 0f, direction.x);
                        newPos = newPos + (normal * -samples[sampleIndex] * effectStrength * Time.deltaTime);
                        newPos.x = Mathf.Clamp(newPos.x, rightLinePositions[i].x - 0.5f, rightLinePositions[i].x + 0.5f);
                        newPos.y = 1f + heightDifference;
                        newPos.z = Mathf.Clamp(newPos.z, rightLinePositions[i].z - 0.5f, rightLinePositions[i].z + 0.5f);
                        rightLineRenderer.SetPosition(i, newPos);
                    }

                    if (!useRandomSample)
                    {
                        currentSampleUsageIndex++;
                        if (currentSampleUsageIndex >= minMaxSampleUsage.y) currentSampleUsageIndex = 0;
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(updateRate));

            if (!Visualize) break;
        }
    }
}
