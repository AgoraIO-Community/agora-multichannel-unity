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
    private const string partyChannelToken = "006e22a665af29d4904860d3e5f62fb4544IABDjIMO5OxhEXxwhThw5YmaNso4Rn7JY4b9Klxqzv9bqwErAdIAAAAAEABoKgnHNBsnYAEAAQA0Gydg";

    // Broadcast Channel
    private const string broadcastChannelName = "broadcastChannel";
    private const string broadcastChannelToken = "006c36f034a41a5476fae92da698a5f2396IABFK1PbrofipKKEp7tEj1emhTV8CzijtzhhDFrW9iQMf1dw9zgAAAAAEABoKgnH12MjYAEAAQDXYyNg";

    private List<GameObject> playerVideoList;
    private float spaceBetweenUserVideos = 150f;
    public Transform partyChatSpawnPoint;
    public GameObject userVideoPrefab;

    public Transform partySpawnPoint;
    public Transform broadcastSpawnPoint;

    public RectTransform partyChatContentWindow;

    AgoraChannel partyChannel;

    void Start()
    {
        playerVideoList = new List<GameObject>();

        mRtcEngine = IRtcEngine.GetEngine(appID);
        mRtcEngine.SetMultiChannelWant(true);


        if (mRtcEngine == null)
        {
            return;
        }

        // enable video
        mRtcEngine.EnableVideo();
        // allow camera output callback
        mRtcEngine.EnableVideoObserver();
        //mRtcEngine.OnJoinChannelSuccess = OnJoinChannelSuccessHandler;
        //mRtcEngine.OnUserJoined = OnUserJoinedHandler;
        //mRtcEngine.JoinChannel("testChannel", null, 0);

        // Party Channel
        partyChannel = mRtcEngine.CreateChannel(partyChannelName);
        partyChannel.ChannelOnJoinChannelSuccess = OnPartyJoinChannelSuccessHandler;
        partyChannel.ChannelOnUserJoined = OnUserJoinedPartyHandler;
        partyChannel.ChannelOnLeaveChannel = OnLeavePartyHandler;
        partyChannel.ChannelOnUserOffLine = OnUserLeftPartyHandler;
        partyChannel.JoinChannel(partyChannelToken, null, 0, new ChannelMediaOptions(true, true));
        partyChannel.Publish();

        

        // Broadcast Channel;
        //mRtcEngine.SetChannelProfile(CHANNEL_PROFILE.CHANNEL_PROFILE_LIVE_BROADCASTING);
        //AgoraChannel broadcastChannel = mRtcEngine.CreateChannel(broadcastChannelName);
        //ChannelMediaOptions broadcastChannelMediaOptions = new ChannelMediaOptions(true, true);
        //broadcastChannel.ChannelOnJoinChannelSuccess = OnBroadcastJoinChannelSuccessHandler;
        //broadcastChannel.ChannelOnUserJoined = OnUserJoinedBroadcastHandler;
        //broadcastChannel.JoinChannel(broadcastChannelToken, null, 0, broadcastChannelMediaOptions);
    }

    public void OnJoinChannelSuccessHandler(string channelName, uint uid, int elapsed)
    {
        Debug.Log("Join party channel success - channel: " + channelName + " uid: " + uid);

        CreateUserVideoSurface(uid, true, partyChatSpawnPoint);
    }

    public void OnUserJoinedHandler(uint uid, int elapsed)
    {
        Debug.Log("On user joined party - channel: + " + uid);

        CreateUserVideoSurface(uid, false, partyChatSpawnPoint);
    }

    #region Party Channel Callbacks
    public void OnPartyJoinChannelSuccessHandler(string channelName, uint uid, int elapsed)
    {
        Debug.Log("Join party channel success - channel: " + channelName + " uid: " + uid);

        CreateUserVideoSurface(uid, true, partyChatSpawnPoint);
    }

    public void OnUserJoinedPartyHandler(string channelID, uint uid, int elapsed)
    {
        Debug.Log("On user joined party - channel: + " + uid);

        CreateUserVideoSurface(uid, false, partyChatSpawnPoint);
    }

    private void OnLeavePartyHandler(string channelID, RtcStats stats)
    {
        Debug.Log("You left the party channel.");
    }

    public void OnUserLeftPartyHandler(string channelID, uint uid, USER_OFFLINE_REASON reason)
    {
        Debug.Log("User left party - channel: + " + uid);

        RemoveUserVideoSurface(uid);
    }
    #endregion

    #region Broadcast Channel Callbacks
    public void OnBroadcastJoinChannelSuccessHandler(string channelName, uint uid, int elapsed)
    {
        Debug.Log("Join broadcast channel success - channel: " + channelName + " uid: " + uid);
        CreateUserVideoSurface(uid, true, broadcastSpawnPoint);
    }

    public void OnUserJoinedBroadcastHandler(string channelId, uint uid, int elapsed)
    {
        Debug.Log("On user joined broadcast - channel: + " + uid);
        CreateUserVideoSurface(uid, false, broadcastSpawnPoint);
    }

    
    #endregion

    #region Agora Cleanup
    private void TerminateAgoraEngine()
    {
        if (mRtcEngine != null)
        {
            partyChannel.LeaveChannel();
            partyChannel.ReleaseChannel();
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
    private void CreateUserVideoSurface(uint uid, bool isLocalUser, Transform spawnPoint)
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
        GameObject newUserVideo = Instantiate(userVideoPrefab, spawnPoint.position, partyChatSpawnPoint.rotation, spawnPoint);
        if (newUserVideo == null)
        {
            Debug.LogError("CreateUserVideoSurface() - newUserVideoIsNull");
            return;
        }
        newUserVideo.name = uid.ToString();
        newUserVideo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, spawnY);
        // newUserVideo.transform.SetParent(spawnPoint, false);
        newUserVideo.transform.rotation = Quaternion.Euler(Vector3.forward * -180);

        playerVideoList.Add(newUserVideo);

        // Update our VideoSurface to reflect new users
        VideoSurface newVideoSurface = newUserVideo.GetComponent<VideoSurface>();
        //VideoSurface newVideoSurface = newUserVideo.AddComponent<VideoSurface>();
        //newVideoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.Renderer);
        newVideoSurface.SetForMultiChannelUser(partyChannelName, uid);

        if (newVideoSurface == null)
        {
            Debug.LogError("CreateUserVideoSurface() - VideoSurface component is null on newly joined user");
            return;
        }

        if (isLocalUser == false)
        {
            //newVideoSurface.SetForMultiChannelUser(partyChannelName, uid);
            //newVideoSurface.SetForUser(uid);
        }
        //newVideoSurface.SetGameFps(30);

        // Update our "Content" container that holds all the newUserVideo image planes

        if(spawnPoint == partyChatSpawnPoint)
        {
            partyChatContentWindow.sizeDelta = new Vector2(0, playerVideoList.Count * spaceBetweenUserVideos + 140);
        }
        
        UpdatePlayerVideoPostions();
    }

    private void UpdatePlayerVideoPostions()
    {
        for (int i = 0; i < playerVideoList.Count; i++)
        {
            playerVideoList[i].GetComponent<RectTransform>().anchoredPosition = Vector2.down * spaceBetweenUserVideos * i;
        }
    }

    private void RemoveUserVideoSurface(uint deletedUID)
    {
        foreach (GameObject player in playerVideoList)
        {
            if (player.name == deletedUID.ToString())
            {
                playerVideoList.Remove(player);
                Destroy(player.gameObject);
                break;
            }
        }

        // update positions of new players
        UpdatePlayerVideoPostions();

        Vector2 oldContent = partyChatContentWindow.sizeDelta;
        partyChatContentWindow.sizeDelta = oldContent + Vector2.down * spaceBetweenUserVideos;
        partyChatContentWindow.anchoredPosition = Vector2.zero;
    }
}
