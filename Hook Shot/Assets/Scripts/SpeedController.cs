using UnityEngine;

public class SpeedController : MonoBehaviour
{
    public static SpeedController Instance { get; private set; }

    // Speed presets: index 0=70%, 1=80%, 2=90%, 3=100%
    private readonly int[] percentages = { 70, 80, 90, 100 };
    private readonly float[] degreesPerSecond = { 70f, 90f, 110f, 130f };

    private int currentIndex = 1; // Default 80%

    public float CurrentDegreesPerSecond => degreesPerSecond[currentIndex];
    public int CurrentPercentage => percentages[currentIndex];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Cycles to the next speed: 70 → 80 → 90 → 100 → 70 ...
    /// </summary>
    public void CycleSpeed()
    {
        currentIndex = (currentIndex + 1) % percentages.Length;
    }
}
