using UnityEngine;

public class BeachVolkerballCup : MonoBehaviour
{
    public enum SceneMode
    {
        TrainingQuiet,
        TrainingWithLog
    }
    public SceneMode sceneMode = SceneMode.TrainingQuiet;
    
    void Awake()
    {
        Debug.unityLogger.logEnabled = sceneMode != SceneMode.TrainingQuiet;
    }
}
