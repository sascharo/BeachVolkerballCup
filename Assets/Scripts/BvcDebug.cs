using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

public class BeachVolkerballCupLogHandler : ILogHandler
{
    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        Debug.unityLogger.logHandler.LogFormat(logType, context, format, args);
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        Debug.unityLogger.LogException(exception, context);
    }
}

public class BvcDebug : MonoBehaviour
{
    [Serializable]
    public enum LogMode
    {
        Log,
        Error,
        Quiet
    }

    private static ILogger s_Logger = Debug.unityLogger;
    private BeachVolkerballCupLogHandler _logHandler;
    public LogMode logMode = LogMode.Error;

    void Awake()
    {
        _logHandler = new BeachVolkerballCupLogHandler();

        if (logMode == LogMode.Quiet)
        {
            Debug.unityLogger.logEnabled = false;
        }
    }
}
