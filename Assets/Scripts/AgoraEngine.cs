using UnityEngine;
using agora_gaming_rtc;


// 2. add a mechanism for grabbing a token from the server

public class AgoraEngine : MonoBehaviour
{
    public string appID;
    public static IRtcEngine mRtcEngine;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if(mRtcEngine == null)
        {
            mRtcEngine = IRtcEngine.GetEngine(appID);
        }

        mRtcEngine.SetMultiChannelWant(true);

        if (mRtcEngine == null)
        {
            Debug.Log("engine is null");
            return;
        }

        mRtcEngine.EnableVideo();
        mRtcEngine.EnableVideoObserver();
    }

    private void OnApplicationQuit()
    {
        IRtcEngine.Destroy();
    }
}