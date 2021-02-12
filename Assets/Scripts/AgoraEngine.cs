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
    private const string partyChannelToken = "006e22a665af29d4904860d3e5f62fb4544IAAG30ZLhxmLy1AYmXFvP+Utv/K34J4jh04MOvjJGFIIfQErAdIAAAAAEABoKgnH3mgnYAEAAQDeaCdg";

    // Broadcast Channel
    private const string broadcastChannelName = "broadcastChannel";
    private const string broadcastChannelToken = "006c36f034a41a5476fae92da698a5f2396IAAJ0uwIVfY/xCYAKWZ8rnA+JD7a46GdKm1RBg//oJYrAVdw9zgAAAAAEABoKgnHJ2cnYAEAAQAnZydg";

    private List<GameObject> playerVideoList;
    private float spaceBetweenUserVideos = 150f;
    public Transform partyChatSpawnPoint;
    public Transform broadcastSpawnPoint;
    public GameObject userVideoPrefab;
    public RectTransform partyChatContentWindow;

    AgoraChannel partyChannel;
    AgoraChannel broadcastChannel;

    public Toggle isBroadcasterToggle;

    public bool isBroadcaster = false;

    void Start()
    {
        playerVideoList = new List<GameObject>();

        mRtcEngine = IRtcEngine.GetEngine(appID);
        mRtcEngine.SetMultiChannelWant(true);

        if (mRtcEngine == null)
        {
            Debug.Log("engine is null");
            return;
        }

        // enable video
        int enableVideo =
        mRtcEngine.EnableVideo();
        // allow camera output callback
        mRtcEngine.EnableVideoObserver();

        Debug.Log("enable Video: " + enableVideo);
    }

    public void Button_JoinPartyChannel()
    {
        Debug.Log("pressing party button");
        // Party Channel
        partyChannel = mRtcEngine.CreateChannel(partyChannelName);

        partyChannel.ChannelOnJoinChannelSuccess = OnPartyJoinChannelSuccessHandler;
        partyChannel.ChannelOnUserJoined = OnUserJoinedPartyHandler;
        partyChannel.ChannelOnLeaveChannel = OnLeavePartyHandler;
        partyChannel.ChannelOnUserOffLine = OnUserLeftPartyHandler;

        int channelJoin =
        partyChannel.JoinChannel(partyChannelToken, null, 0, new ChannelMediaOptions(true, true));
        partyChannel.Publish();

        Debug.Log(channelJoin);
    }

    public void Button_LeavePartyChannel()
    {
        partyChannel.LeaveChannel();
        partyChannel.Unpublish();
    }

    public void Button_JoinBroadcastChannel()
    {
        // Broadcast Channel;
        broadcastChannel = mRtcEngine.CreateChannel(broadcastChannelName);
        broadcastChannel.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_AUDIENCE);

        broadcastChannel.ChannelOnJoinChannelSuccess = OnBroadcastJoinChannelSuccessHandler;
        broadcastChannel.ChannelOnUserJoined = OnUserJoinedBroadcastHandler;
        broadcastChannel.ChannelOnLeaveChannel = OnLeaveBroadcastHandler;
        broadcastChannel.ChannelOnUserOffLine = OnUserLeftBroadcastHandler;

        broadcastChannel.JoinChannel(broadcastChannelToken, null, 0, new ChannelMediaOptions(true, true));
        broadcastChannel.Publish();
    }

    public void Button_LeaveBroadcastChannel()
    {
        broadcastChannel.LeaveChannel();
        broadcastChannel.Unpublish();
    }

    public void Toggle_BroadcasterStateChanged()
    {
        isBroadcaster = isBroadcasterToggle.isOn;
        Debug.Log("user is broadcaster: " + isBroadcaster);
    }

    #region Party Channel Callbacks
    public void OnPartyJoinChannelSuccessHandler(string channelID, uint uid, int elapsed)
    {
        Debug.Log("Join party channel success - channel: " + channelID + " uid: " + uid);
        MakeImageSurface(channelID, uid, partyChatSpawnPoint, true);
    }

    public void OnUserJoinedPartyHandler(string channelID, uint uid, int elapsed)
    {
        Debug.Log("On user joined party - channel: + " + uid);
        MakeImageSurface(channelID, uid, partyChatSpawnPoint);
    }

    private void OnLeavePartyHandler(string channelID, RtcStats stats)
    {
        Debug.Log("You left the party channel.");
        foreach(GameObject player in playerVideoList)
        {
            Destroy(player.gameObject);
        }

        playerVideoList.Clear();
    }

    public void OnUserLeftPartyHandler(string channelID, uint uid, USER_OFFLINE_REASON reason)
    {
        Debug.Log("User: " + uid + " left party - channel: + " + uid + "for reason: " + reason);
        RemoveUserVideoSurface(uid);
    }
    #endregion

    #region Broadcast Channel Callbacks
    public void OnBroadcastJoinChannelSuccessHandler(string channelID, uint uid, int elapsed)
    {
        Debug.Log("Join broadcast channel success - channel: " + channelID + " uid: " + uid);
        MakeImageSurface(channelID, uid, broadcastSpawnPoint, true);
    }

    public void OnUserJoinedBroadcastHandler(string channelID, uint uid, int elapsed)
    {
        Debug.Log("On user joined broadcast - channel: + " + uid);
        MakeImageSurface(channelID, uid, broadcastSpawnPoint);
    }

    public void OnLeaveBroadcastHandler(string channelID, RtcStats stats)
    {
        Debug.Log("You left the broadcast.");
        foreach (GameObject player in playerVideoList)
        {
            Destroy(player.gameObject);
        }

        playerVideoList.Clear();
    }

    public void OnUserLeftBroadcastHandler(string channelID, uint uid, USER_OFFLINE_REASON reason)
    {
        Debug.Log("User: " + uid + " left broadcast - channel: + " + uid + "for reason: " + reason);
        RemoveUserVideoSurface(uid);
    }

    
    #endregion

    #region Agora Cleanup
    private void TerminateAgoraEngine()
    {
        if (mRtcEngine != null)
        {
            if(partyChannel != null)
            {
                Debug.Log("cleaning up party channel");
                partyChannel.LeaveChannel();
                partyChannel.ReleaseChannel();
            }

            if(broadcastChannel != null)
            {
                Debug.Log("cleaning up broadcast");
                broadcastChannel.LeaveChannel();
                broadcastChannel.ReleaseChannel();
            }
            IRtcEngine.Destroy();
        }
    }

    // Cleaning up the Agora engine during OnApplicationQuit() is an essential part of the Agora process with Unity. 
    private void OnApplicationQuit()
    {
        TerminateAgoraEngine();
    }
    #endregion

    void MakeImageSurface(string channelID, uint uid, Transform spawnPoint, bool isLocalUser = false)
    {
        if(GameObject.Find(uid.ToString()) != null)
        {
            Debug.Log("Already a video surface with this uid: " + uid.ToString());
            return;
        }

        GameObject go = new GameObject();
        go.name = uid.ToString();
        go.AddComponent<RawImage>();
        go.transform.localScale = new Vector3(1, -1, 1);

        if (spawnPoint != null)
        {
            go.transform.parent = spawnPoint;
        }

        //if (spawnPoint == partyChatSpawnPoint)
        //{
        //    partyChatContentWindow.sizeDelta = new Vector2(0, playerVideoList.Count * spaceBetweenUserVideos + 140);
        //}
        
        float spawnY = playerVideoList.Count * spaceBetweenUserVideos * -1;
        go.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, spawnY);

        VideoSurface videoSurface = go.AddComponent<VideoSurface>();

        if(isLocalUser == false)
        {
            videoSurface.SetForMultiChannelUser(channelID, uid);
        }

        UpdatePlayerVideoPostions();
        playerVideoList.Add(go);
    }



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
