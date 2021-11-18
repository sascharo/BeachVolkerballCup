using UnityEngine;

public class BeachVolkerballCup : MonoBehaviour
{
    public enum SceneMode
    {
        TrainingWithLog,
        TrainingQuiet
    }
    public SceneMode sceneMode = SceneMode.TrainingQuiet;
    
    void Awake()
    {
        Debug.unityLogger.logEnabled = sceneMode != SceneMode.TrainingQuiet;
    }
}
