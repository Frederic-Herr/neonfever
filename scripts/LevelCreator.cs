using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System.Linq;

public class LevelCreator : MonoBehaviour
{
    public static LevelCreator Instance { get; private set; }

    [BoxGroup("Seed"), SerializeField] private string seed = "Level_1";
    [BoxGroup("Seed"), SerializeField, ReadOnly] private int seedHash;
    [BoxGroup("Seed"), SerializeField] private bool useEditorLevelSeed;
    [BoxGroup("Seed"), SerializeField, Range(1, 500), ShowIf(nameof(useEditorLevelSeed))] private int editorLevelNumber = 1;
    [BoxGroup("Seed"), SerializeField, ReadOnly, ShowIf(nameof(useEditorLevelSeed))] private string editorLevelSeed = "Level_1";
    [BoxGroup("Seed"), SerializeField, ReadOnly, ShowIf(nameof(useEditorLevelSeed))] private int editorLevelSeedHash;

    [SerializeField] private LevelDesigner levelDesigner;

    [BoxGroup("Track Settings"), SerializeField] private int levelLength = 100;
    [BoxGroup("Track Settings"), SerializeField] private LineRenderer leftTrackBorder;
    [BoxGroup("Track Settings"), SerializeField] private LineRenderer rightTrackBorder;
    [BoxGroup("Track Settings"), SerializeField] private Vector2 minMaxTrackX = new Vector2(-3f, 3f);
    [BoxGroup("Track Settings"), SerializeField] private Vector2 minMaxTrackWidth = new Vector2(1f, 3f);
    [BoxGroup("Track Settings"), SerializeField] private float trackWidthReductionPerLevel = 0.05f;
    [BoxGroup("Track Settings"), SerializeField] private float maxTrackWidthReduction = 1.5f;
    [BoxGroup("Track Settings"), SerializeField] private Vector2 minMaxTrackZDif = new Vector2(0.5f, 2f);
    [BoxGroup("Track Settings"), SerializeField] private float zDistance = 3f;

    [BoxGroup("Traps"), SerializeField] private Vector2 minMaxTrapDistance;
    [BoxGroup("Traps"), SerializeField] private GameObject[] traps;

    private Vector3 targetPos;
    public Vector3 TargetPos
    {
        get => targetPos;
        set
        {
            targetPos = value;
        }
    }

    private Vector3 currentPos;
    private List<GameObject> trackColliders = new List<GameObject>();
    private GameManager gameManager;
    private List<GameObject> availableTraps = new List<GameObject>();
    private List<Trap> createdTraps = new List<Trap>();

    public delegate void OnLevelCreatedDelegate(Vector3[] leftLinePoints, Vector3[] rightLinePoints);
    public event OnLevelCreatedDelegate OnLevelCreated;

    private bool buildEditorLevel;

    private void OnValidate()
    {
        SetSeed();
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        gameManager = GameManager.Instance;
        if(gameManager.TutorialMode && leftTrackBorder.positionCount > 0) TargetPos = leftTrackBorder.GetPosition(leftTrackBorder.positionCount - 1);
        if (gameManager.GameStartedFromMainMenu) useEditorLevelSeed = false;
        seed = $"Level_{gameManager.CurrentLevel}";
        BuildLevel();
    }

    public List<Trap> GetTraps()
    {
        return createdTraps;
    }

    public Vector3[] GetLeftLinePositions()
    {
        Vector3[] leftPos = new Vector3[leftTrackBorder.positionCount];
        leftTrackBorder.GetPositions(leftPos);
        return leftPos;
    }

    public Vector3[] GetRightLinePositions()
    {
        Vector3[] rightPos = new Vector3[rightTrackBorder.positionCount];
        rightTrackBorder.GetPositions(rightPos);
        return rightPos;
    }

    public bool UseEditorSeed()
    {
        return useEditorLevelSeed;
    }

    public int GetEditorLevel()
    {
        return editorLevelNumber;
    }

    private void SetSeed()
    {
#if UNITY_EDITOR
        if (useEditorLevelSeed)
        {
            editorLevelSeed = $"Level_{editorLevelNumber}";
            editorLevelSeedHash = editorLevelSeed.GetHashCode();
            Random.InitState(editorLevelSeedHash);
        }
        else
        {
            seedHash = seed.GetHashCode();
            Random.InitState(seedHash);
        }
#else
            seedHash = seed.GetHashCode();
            Random.InitState(seedHash);
#endif
    }

    [Button]
    public void BuildLevel()
    {
        buildEditorLevel = !Application.isPlaying;

        SetSeed();

        ClearLevel();

        BuildTrackBorder();

        BuildTraps();

        Vector3[] leftPositions = new Vector3[leftTrackBorder.positionCount];
        leftTrackBorder.GetPositions(leftPositions);

        Vector3[] rightPositions = new Vector3[rightTrackBorder.positionCount];
        rightTrackBorder.GetPositions(rightPositions);

        OnLevelCreated?.Invoke(leftPositions, rightPositions);

        levelDesigner.SetLevelDesign();
    }

    private void BuildTraps()
    {
        int currentPosIndex = 3;
        int level = buildEditorLevel ? editorLevelNumber : gameManager.CurrentLevel;
        availableTraps = traps.Where(x => x.GetComponent<Trap>().GetRequiredLevel() <= level).ToList();

        while (currentPosIndex < levelLength - minMaxTrapDistance.x)
        {
            int rndTrapDistance = (int)Random.Range(minMaxTrapDistance.x, minMaxTrapDistance.y + 0.99f);
            int rndBorderIndex = Random.Range(0, 2);
            LineRenderer rndTrackBorder = rndBorderIndex == 0 ? leftTrackBorder : rightTrackBorder;
            currentPosIndex += rndTrapDistance;
            currentPosIndex = Mathf.Clamp(currentPosIndex, 0, levelLength - 1);

            GameObject trapObj = Instantiate(availableTraps[Random.Range(0, availableTraps.Count)], rndTrackBorder.GetPosition(currentPosIndex), Quaternion.identity, rndTrackBorder.transform);
            Trap trap = trapObj.GetComponent<Trap>();
            trapObj.transform.localEulerAngles = new Vector3(0f, rndBorderIndex == 0 ? 90f : -90f, 0f);
            trap.SetUpTrap(leftTrackBorder.GetPosition(currentPosIndex), rightTrackBorder.GetPosition(currentPosIndex), rndBorderIndex == 0);

            createdTraps.Add(trap);
        }
    }

    private void BuildTrackBorder()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

        leftTrackBorder.positionCount = levelLength + 2;
        rightTrackBorder.positionCount = levelLength + 2;
        currentPos = leftTrackBorder.GetPosition(2);
        int level = buildEditorLevel ? editorLevelNumber : gameManager.CurrentLevel;

        for (int i = 1; i < 3; i++)
        {
            AddColliderToLine(leftTrackBorder.transform, leftTrackBorder.GetPosition(i - 1), leftTrackBorder.GetPosition(i));
            AddColliderToLine(rightTrackBorder.transform, rightTrackBorder.GetPosition(i - 1), rightTrackBorder.GetPosition(i));
        }

        for (int i = 3; i < levelLength; i++)
        {
            currentPos += new Vector3(Random.Range(minMaxTrackX.x, minMaxTrackX.y), 0f, zDistance);// + Random.Range(minMaxTrackZDif.x, minMaxTrackZDif.y));
            leftTrackBorder.SetPosition(i, currentPos);
            float trackWidthReduction = Mathf.Clamp(trackWidthReductionPerLevel * level, 0f, maxTrackWidthReduction);
            float randomMaxWidth = minMaxTrackWidth.y - trackWidthReduction;
            float trackWidth = Mathf.Clamp(Random.Range(minMaxTrackWidth.x, randomMaxWidth), minMaxTrackWidth.x, minMaxTrackWidth.y);
            rightTrackBorder.SetPosition(i, currentPos + new Vector3(trackWidth, 0f, Random.Range(minMaxTrackZDif.x, minMaxTrackZDif.y)));
            AddColliderToLine(leftTrackBorder.transform, leftTrackBorder.GetPosition(i - 1), leftTrackBorder.GetPosition(i));
            AddColliderToLine(rightTrackBorder.transform, rightTrackBorder.GetPosition(i - 1), rightTrackBorder.GetPosition(i));
        }

        currentPos += new Vector3(0f, 0f, zDistance);
        leftTrackBorder.SetPosition(levelLength, currentPos);
        currentPos.x = rightTrackBorder.GetPosition(levelLength - 1).x;
        rightTrackBorder.SetPosition(levelLength, currentPos);
        currentPos.x -= Mathf.Abs(Vector3.Distance(rightTrackBorder.GetPosition(levelLength), leftTrackBorder.GetPosition(levelLength)) * 0.5f);
        rightTrackBorder.SetPosition(levelLength + 1, currentPos);
        currentPos.x = leftTrackBorder.GetPosition(levelLength - 1).x;
        currentPos.x += Mathf.Abs(Vector3.Distance(rightTrackBorder.GetPosition(levelLength), leftTrackBorder.GetPosition(levelLength)) * 0.5f);
        leftTrackBorder.SetPosition(levelLength + 1, currentPos);
        AddColliderToLine(leftTrackBorder.transform, leftTrackBorder.GetPosition(levelLength - 1), leftTrackBorder.GetPosition(levelLength));
        AddColliderToLine(rightTrackBorder.transform, rightTrackBorder.GetPosition(levelLength - 1), rightTrackBorder.GetPosition(levelLength));
        AddColliderToLine(leftTrackBorder.transform, leftTrackBorder.GetPosition(levelLength), leftTrackBorder.GetPosition(levelLength + 1));
        AddColliderToLine(rightTrackBorder.transform, rightTrackBorder.GetPosition(levelLength), rightTrackBorder.GetPosition(levelLength + 1));

        TargetPos = leftTrackBorder.GetPosition(levelLength - 1);
    }

    [Button]
    private void ClearLevel()
    {
        leftTrackBorder.positionCount = 3;
        rightTrackBorder.positionCount = 3;

        while (leftTrackBorder.transform.childCount > 0)
        {
            if (Application.isEditor)
            {
                DestroyImmediate(leftTrackBorder.transform.GetChild(0).gameObject);
            }
            else
            {
                Destroy(leftTrackBorder.transform.GetChild(0).gameObject);
            }
        }

        while (rightTrackBorder.transform.childCount > 0)
        {
            if (Application.isEditor)
            {
                DestroyImmediate(rightTrackBorder.transform.GetChild(0).gameObject);
            }
            else
            {
                Destroy(rightTrackBorder.transform.GetChild(0).gameObject);
            }
        }

        trackColliders.Clear();
    }

    private void AddColliderToLine(Transform lineTransform, Vector3 startPos, Vector3 endPos)
    {
        BoxCollider col = new GameObject("TrackBorder").AddComponent<BoxCollider>();
        trackColliders.Add(col.gameObject);
        col.tag = "TrackBorder";
        col.transform.parent = lineTransform.transform;
        float lineLength = Vector3.Distance(startPos, endPos);
        col.size = new Vector3(lineLength, 1f, 0.1f);
        Vector3 midPoint = (startPos + endPos) / 2;
        col.transform.position = midPoint;
        float angle = (Mathf.Abs(startPos.z - endPos.z) / Mathf.Abs(startPos.x - endPos.x));
        if (startPos.x < endPos.x)
        {
            angle *= -1;
        }
        angle = Mathf.Rad2Deg * Mathf.Atan(angle);
        col.transform.Rotate(0, angle, 0);
    }

    public void AddCollidersToTrack()
    {
        for (int i = 0; i < leftTrackBorder.positionCount; i++)
        {
            AddColliderToLine(leftTrackBorder.transform, leftTrackBorder.GetPosition(i - 1), leftTrackBorder.GetPosition(i));
        }

        for (int i = 0; i < rightTrackBorder.positionCount; i++)
        {
            AddColliderToLine(rightTrackBorder.transform, rightTrackBorder.GetPosition(i - 1), rightTrackBorder.GetPosition(i));
        }
    }
}
