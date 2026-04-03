using UnityEngine;
using Unity.MLAgents.Policies;


[RequireComponent(typeof(BehaviorParameters))]
public class BoidDifficultyController2D : MonoBehaviour
{
    public enum Difficulty { Easy, Medium, Hard }

    [Header("Current Difficulty")]
    public Difficulty currentDifficulty = Difficulty.Medium;

    [Header("Brain Checkpoints (.onnx)")]
    [Tooltip("Assign BoidAgent-50000.onnx here")]
    public Object easyModel;
    
    [Tooltip("Assign BoidAgent-200000.onnx here")]
    public Object mediumModel;
    
    [Tooltip("Assign BoidAgent-500000.onnx here")]
    public Object hardModel;

    private BehaviorParameters behaviorParameters;

    void Awake()
    {
        behaviorParameters = GetComponent<BehaviorParameters>();
        LoadBrain(currentDifficulty);
    }

    public void LoadBrain(Difficulty level)
    {
        currentDifficulty = level;

        if (behaviorParameters == null) behaviorParameters = GetComponent<BehaviorParameters>();
        if (behaviorParameters == null) return;

        switch (level)
        {
            case Difficulty.Easy:
                if (easyModel != null) behaviorParameters.Model = easyModel as Unity.InferenceEngine.ModelAsset;
                break;
            case Difficulty.Medium:
                if (mediumModel != null) behaviorParameters.Model = mediumModel as Unity.InferenceEngine.ModelAsset;
                break;
            case Difficulty.Hard:
                if (hardModel != null) behaviorParameters.Model = hardModel as Unity.InferenceEngine.ModelAsset;
                break;
        }

        Debug.Log($"[BoidDifficultyController2D] Brain loaded for difficulty: {level} on {gameObject.name}");
    }

    void OnValidate()
    {
        if (Application.isPlaying && behaviorParameters != null)
        {
            LoadBrain(currentDifficulty);
        }
    }
}
