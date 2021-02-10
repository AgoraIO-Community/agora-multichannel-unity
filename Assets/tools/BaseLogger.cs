using UnityEngine;
using UnityEngine.UI;

public class BaseLogger
{
    string text; // 

    public BaseLogger(string text)
    {
        this.text = text;
    }

    public void UpdateLog(string logMessage)
    {
        Debug.Log(logMessage);
        string srcLogMessage = text;
        if (srcLogMessage.Length > 1000)
        {
            srcLogMessage = "";
        }
        srcLogMessage += "\r\n \r\n";
        srcLogMessage += logMessage;
        text = srcLogMessage;
    }

    public bool DebugAssert(bool condition, string message)
    {
        if (!condition)
        {
            UpdateLog(message);
            return false;
        }
        Debug.Assert(condition, message);
        return true;
    }
}