using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using agora_gaming_rtc;
using UnityEngine.UI;

public class AgoraEngine : MonoBehaviour
{
    public string appID;
    private  IRtcEngine mRtcEngine;

    // Party Channel
    private const string partyChannelName = "partyChannel";
    private const string partyChannelToken = "006e22a665af29d4904860d3e5f62fb4544IAAE+qyFPkEqJUc0b4eAcRxm14Sam1rBNlhK0zbxRJW2hwErAdIAAAAAEAC0V+LrwckRYAEAAQDByRFg";

    // Broadcast Channel
    private const string broadcastChannelName = "broadcastChannel";
    //private const string broadcastChannelToken;

    private List<GameObject> playerVideoList;
    private float spaceBetweenUserVideos = 150f;
    public Transform partyChatSpawnPoint;
    public GameObject userVideoPrefab;


    void Start()
    {
        playerVideoList = new List<GameObject>();

        mRtcEngine = IRtcEngine.GetEngine(appID);


        if(mRtcEngine == null)
        {
            return;
        }

        // set callbacks (optional)
        //mRtcEngine.OnJoinChannelSuccess = OnJoinChannelSuccessHandler;
        //mRtcEngine.OnUserJoined = OnUserJoinedHandler;
        

        // enable video
        mRtcEngine.EnableVideo();
        // allow camera output callback
        mRtcEngine.EnableVideoObserver();

        // join channel
        //mRtcEngine.JoinChannel(partyChannelName, null, 0);



        // Party Channel
        AgoraChannel partyChannel = mRtcEngine.CreateChannel(partyChannelName);
        ChannelMediaOptions partyChannelMediaOptions = new ChannelMediaOptions(true, true);
        partyChannel.ChannelOnJoinChannelSuccess = OnPartyJoinChannelSuccessHandler;
        partyChannel.ChannelOnUserJoined = OnUserJoinedPartyHandler;
        partyChannel.JoinChannel(partyChannelToken, null, 0, partyChannelMediaOptions);

        // Broadcast Channel;
        AgoraChannel broadcastChannel = mRtcEngine.CreateChannel(broadcastChannelName);
        ChannelMediaOptions broadcastChannelMediaOptions = new ChannelMediaOptions(false, false);
        
    }

    #region Party Channel Callbacks
    public void OnPartyJoinChannelSuccessHandler(string channelName, uint uid, int elapsed)
    {
        Debug.Log("Join party channel success - channel: " + channelName + " uid: " + uid);

        // create a GameObject and assign to this new user
        CreateUserVideoSurface(uid, true);
    }

    public void OnUserJoinedPartyHandler(string channelId, uint uid, int elapsed)
    {
        Debug.Log("On user joined party - channel: + " + uid);
        CreateUserVideoSurface(uid, false);
    }
    #endregion

    #region Broadcast Channel Callbacks
    public void OnBroadcastJoinChannelSuccessHandler(string channelName, uint uid, int elapsed)
    {
        Debug.Log("Join broadcast channel success - channel: " + channelName + " uid: " + uid);
    }

    public void OnUserJoinedBroadcastHandler(string channelId, uint uid, int elapsed)
    {
        Debug.Log("On user joined broadcast - channel: + " + uid);
        
    }
    #endregion

    #region Agora Cleanup
    private void TerminateAgoraEngine()
    {
        if (mRtcEngine != null)
        {
            mRtcEngine.LeaveChannel();
            mRtcEngine = null;
            IRtcEngine.Destroy();
        }
    }

    // Cleaning up the Agora engine during OnApplicationQuit() is an essential part of the Agora process with Unity. 
    private void OnApplicationQuit()
    {
        TerminateAgoraEngine();
    }
    #endregion


    // Create new image plane to display users in party.
    private void CreateUserVideoSurface(uint uid, bool isLocalUser)
    {
        // Avoid duplicating Local player VideoSurface image plane.
        for (int i = 0; i < playerVideoList.Count; i++)
        {
            if (playerVideoList[i].name == uid.ToString())
            {
                return;
            }
        }

        // Get the next position for newly created VideoSurface to place inside UI Container.
        float spawnY = playerVideoList.Count * spaceBetweenUserVideos;
        Vector3 spawnPosition = new Vector3(0, -spawnY, 0);

        // Create Gameobject that will serve as our VideoSurface.
        GameObject newUserVideo = Instantiate(userVideoPrefab, spawnPosition, partyChatSpawnPoint.rotation);
        if (newUserVideo == null)
        {
            Debug.LogError("CreateUserVideoSurface() - newUserVideoIsNull");
            return;
        }
        newUserVideo.name = uid.ToString();
        newUserVideo.transform.SetParent(partyChatSpawnPoint, false);
        newUserVideo.transform.rotation = Quaternion.Euler(Vector3.forward * -180);

        playerVideoList.Add(newUserVideo);

        // Update our VideoSurface to reflect new users
        VideoSurface newVideoSurface = newUserVideo.GetComponent<VideoSurface>();
        if (newVideoSurface == null)
        {
            Debug.LogError("CreateUserVideoSurface() - VideoSurface component is null on newly joined user");
            return;
        }

        if (isLocalUser == false)
        {
            newVideoSurface.SetForUser(uid);
        }
        newVideoSurface.SetGameFps(30);

        // Update our "Content" container that holds all the newUserVideo image planes
        //content.sizeDelta = new Vector2(0, playerVideoList.Count * spaceBetweenUserVideos + 140);

        //UpdatePlayerVideoPostions();
        //UpdateLeavePartyButtonState();
    }
}
