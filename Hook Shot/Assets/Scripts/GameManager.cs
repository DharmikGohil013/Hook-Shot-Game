using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Idle,
    Playing,
    Moving,
    LevelComplete,
    Failed
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Idle;

    // Events
    public event Action OnGameStart;
    public event Action OnBallMoved;
    public event Action OnBallStopped;
    public event Action OnLevelComplete;
    public event Action OnGameFailed;

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
    /// Called by StartButton via UIManager.
    /// </summary>
    public void StartGame()
    {
        if (CurrentState != GameState.Idle) return;
        CurrentState = GameState.Playing;
        OnGameStart?.Invoke();
    }

    /// <summary>
    /// Called by BallController when ball launches.
    /// </summary>
    public void NotifyBallMoved()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.Moving;
        OnBallMoved?.Invoke();
    }

    /// <summary>
    /// Called by BallController when ball stops after hitting a block.
    /// </summary>
    public void NotifyBallStopped()
    {
        if (CurrentState != GameState.Moving) return;
        CurrentState = GameState.Playing;
        OnBallStopped?.Invoke();
    }

    /// <summary>
    /// Called by BallController when ball reaches goal.
    /// </summary>
    public void NotifyLevelComplete()
    {
        CurrentState = GameState.LevelComplete;
        OnLevelComplete?.Invoke();
    }

    /// <summary>
    /// Called by BallController when ball goes out of bounds.
    /// </summary>
    public void NotifyGameFailed()
    {
        CurrentState = GameState.Failed;
        OnGameFailed?.Invoke();
    }

    /// <summary>
    /// Called by RestartButton — reloads current scene.
    /// </summary>
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Called by NextLevelButton — loads next scene or wraps to scene 0.
    /// </summary>
    public void LoadNextLevel()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            // All levels complete — wrap back to first scene
            nextIndex = 0;
        }
        SceneManager.LoadScene(nextIndex);
    }
}
