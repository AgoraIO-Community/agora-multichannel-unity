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
    private const string partyChannelToken = "006e22a665af29d4904860d3e5f62fb4544IAAxEYFM02S6687Fi70hOlG7pP+sCZuvxZ6mkecAoYpSjgErAdIAAAAAEABoKgnHt6klYAEAAQC3qSVg";

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

    void Start()
    {
        playerVideoList = new List<GameObject>();

        mRtcEngine = IRtcEngine.GetEngine(appID);


        if (mRtcEngine == null)
        {
            return;
        }

        // enable video
        mRtcEngine.EnableVideo();
        // allow camera output callback
        mRtcEngine.EnableVideoObserver();


        // Party Channel
        AgoraChannel partyChannel = mRtcEngine.CreateChannel(partyChannelName);
        ChannelMediaOptions partyChannelMediaOptions = new ChannelMediaOptions(true, true);
        partyChannel.ChannelOnJoinChannelSuccess = OnPartyJoinChannelSuccessHandler;
        partyChannel.ChannelOnUserJoined = OnUserJoinedPartyHandler;
        partyChannel.ChannelOnUserOffLine = OnUserLeftPartyHandler;
        partyChannel.JoinChannel(partyChannelToken, null, 0, partyChannelMediaOptions);

        // Broadcast Channel;
        mRtcEngine.SetChannelProfile(CHANNEL_PROFILE.CHANNEL_PROFILE_LIVE_BROADCASTING);
        AgoraChannel broadcastChannel = mRtcEngine.CreateChannel(broadcastChannelName);
        ChannelMediaOptions broadcastChannelMediaOptions = new ChannelMediaOptions(true, true);
        broadcastChannel.ChannelOnJoinChannelSuccess = OnBroadcastJoinChannelSuccessHandler;
        broadcastChannel.ChannelOnUserJoined = OnUserJoinedBroadcastHandler;
        broadcastChannel.JoinChannel(broadcastChannelToken, null, 0, broadcastChannelMediaOptions);
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

    public void OnUserLeftPartyHandler(string channelID, uint uid, USER_OFFLINE_REASON reason)
    {
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
