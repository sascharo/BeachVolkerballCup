using System;
using System.Collections;
using System.IO;
using UnityEngine;

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
    public enum LogMode
    {
        Log,
        Error,
        Quiet
    }

    public static BeachVolkerballCupLogHandler Log = new BeachVolkerballCupLogHandler();
    public LogMode logMode = LogMode.Error;

    private void Awake()
    {
        Log = new BeachVolkerballCupLogHandler();

        if (logMode == LogMode.Quiet)
        {
            Debug.unityLogger.logEnabled = false;
        }
    }
}
