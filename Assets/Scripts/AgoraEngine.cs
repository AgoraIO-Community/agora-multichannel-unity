using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using agora_gaming_rtc;
using UnityEngine.UI;



// 1. refrain from creating video frames from users that aren't published, or are audience members

// 2. add a mechanism for grabbing a token from the server

public class AgoraEngine : MonoBehaviour
{
    public string appID;
    public static IRtcEngine mRtcEngine;

    // Party Channel
    private const string partyChannelName = "partyChannel";
    private const string partyChannelToken = "006e22a665af29d4904860d3e5f62fb4544IAAG30ZLhxmLy1AYmXFvP+Utv/K34J4jh04MOvjJGFIIfQErAdIAAAAAEABoKgnH3mgnYAEAAQDeaCdg";

    // Broadcast Channel
    private const string broadcastChannelName = "broadcastChannel";
    private const string broadcastChannelToken = "006c36f034a41a5476fae92da698a5f2396IAAJ0uwIVfY/xCYAKWZ8rnA+JD7a46GdKm1RBg//oJYrAVdw9zgAAAAAEABoKgnHJ2cnYAEAAQAnZydg";

    private AgoraChannel partyChannel;
    private AgoraChannel broadcastChannel;
    private List<GameObject> partyVideoList;
    private List<GameObject> broadcastVideoList;
    private float spaceBetweenUserVideos = 150f;

    [Header("Party Channel")]
    public Transform partyChatSpawnPoint;
    public RectTransform partyChatContentWindow;
    
    [Header("Broadcast Channel")]
    public Transform broadcastSpawnPoint;
    public RectTransform broadcastChatContentWindow;

    [Header("Misc")]
    public Toggle isBroadcasterToggle;
    public bool isPublishing;
    public CLIENT_ROLE_TYPE localClientRole;
    public GameObject userVideoPrefab;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        partyVideoList = new List<GameObject>();
        broadcastVideoList = new List<GameObject>();
        isPublishing = false;
        localClientRole = CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER;

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

        // enable video
        int enableVideo =
        mRtcEngine.EnableVideo();
        // allow camera output callback
        mRtcEngine.EnableVideoObserver();

        Debug.Log("enable Video: " + enableVideo);
    }

    public void Button_JoinPartyChannel()
    {
        // Party Channel
        partyChannel = mRtcEngine.CreateChannel(partyChannelName);

        partyChannel.ChannelOnJoinChannelSuccess = OnPartyJoinChannelSuccessHandler;
        partyChannel.ChannelOnUserJoined = OnUserJoinedPartyHandler;
        partyChannel.ChannelOnLeaveChannel = OnLeavePartyHandler;
        partyChannel.ChannelOnUserOffLine = OnUserLeftPartyHandler;

        partyChannel.JoinChannel(partyChannelToken, null, 0, new ChannelMediaOptions(true, true));
    }

    public void Button_LeavePartyChannel()
    {
        partyChannel.LeaveChannel();
        //partyChannel.ReleaseChannel();

        Debug.Log("Leaving party channel");
    }

    public void Button_PublishToPartyChannel()
    {
        if(partyChannel == null)
        {
            Debug.LogError("Party channel isn't created yet");
            return;
        }

        if(isPublishing == false)
        {
            int publishResult = partyChannel.Publish();
            if (publishResult == 0)
            {
                isPublishing = true;
            }

            Debug.Log("Publishing to party channel result: " + publishResult);
        }
        else
        {
            Debug.Log("Already published to a channel");
        }   
    }

    public void Button_CancelPublishFromPartyChannel()
    {
        if (partyChannel == null)
        {
            Debug.LogError("Party channel isn't created yet");
            return;
        }

        if (isPublishing == true)
        {
            int unpublishResult = partyChannel.Unpublish();
            if(unpublishResult == 0)
            {
                isPublishing = false;
            }

            Debug.Log("Unpublish from party channel result: " + unpublishResult);
        }
        else
        {
            Debug.Log("Not published to any channel");
        }
    }

    public void Button_JoinBroadcastChannel()
    {
        broadcastChannel = mRtcEngine.CreateChannel(broadcastChannelName);

        if(isBroadcasterToggle.isOn)
        {
            broadcastChannel.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
            localClientRole = CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER;
            
        }
        else
        {
            broadcastChannel.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_AUDIENCE);
            localClientRole = CLIENT_ROLE_TYPE.CLIENT_ROLE_AUDIENCE;
        }

        broadcastChannel.ChannelOnJoinChannelSuccess = OnBroadcastJoinChannelSuccessHandler;
        broadcastChannel.ChannelOnUserJoined = OnUserJoinedBroadcastHandler;
        broadcastChannel.ChannelOnLeaveChannel = OnLeaveBroadcastHandler;
        broadcastChannel.ChannelOnUserOffLine = OnUserLeftBroadcastHandler;


        broadcastChannel.JoinChannel(broadcastChannelToken, null, 0, new ChannelMediaOptions(true, true));

        Debug.Log("Joined broadcast as " + localClientRole);
    }

    public void Button_LeaveBroadcastChannel()
    {
        broadcastChannel.LeaveChannel();

        Debug.Log("leaving broadcast channel");
    }

    public void Button_PublishToBroadcastChannel()
    {
        if(broadcastChannel == null)
        {
            Debug.LogError("broadcast channel isn't created yet");
            return;
        }

        int publishResult = -5;
        if (isPublishing == false && localClientRole == CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER)
        {
            publishResult = broadcastChannel.Publish();
            if (publishResult == 0)
            {
                isPublishing = true;
                Debug.Log("Publishing to broadcast channel");
            }
        }
        else
        {
            Debug.LogWarning("You are already publishing, and cannot publish to more than one stream.\nClient role is: " + localClientRole);
        }

        Debug.Log("Publishing to broadcast channel result: " + publishResult);
    }

    public void Button_CancelPublishToBroadcastChannel()
    {
        if (broadcastChannel == null)
        {
            Debug.LogError("broadcast channel isn't created yet");
            return;
        }

        if (isPublishing == true)
        {
            int cancelPublishResult = broadcastChannel.Unpublish();
            if(cancelPublishResult == 0)
            {
                isPublishing = false;
                Debug.Log("Unpublish from broadcast result: " + cancelPublishResult);
            }
        }
        else
        {
            Debug.Log("Not publishing anything");
        }
    }

    public void Toggle_BroadcasterStateChanged()
    {
        if(isBroadcasterToggle.isOn)
        {
            localClientRole = CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER;
        }
        else
        {
            localClientRole = CLIENT_ROLE_TYPE.CLIENT_ROLE_AUDIENCE;
        }

        Debug.Log("user is broadcaster: " + isBroadcasterToggle.isOn);
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
        foreach(GameObject player in partyVideoList)
        {
            Destroy(player.gameObject);
        }

        partyVideoList.Clear();
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
        foreach (GameObject player in broadcastVideoList)
        {
            Destroy(player.gameObject);
        }

        partyVideoList.Clear();
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
                partyChannel.LeaveChannel();
                partyChannel.ReleaseChannel();
            }

            if(broadcastChannel != null)
            {
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
            go.transform.SetParent(spawnPoint);
        }

        float spawnY = 0;
        if (spawnPoint == partyChatSpawnPoint)
        {
            partyChatContentWindow.sizeDelta = new Vector2(0, partyVideoList.Count * spaceBetweenUserVideos + 140);
            spawnY = partyVideoList.Count * spaceBetweenUserVideos * -1;
            UpdatePlayerVideoPostions();
            partyVideoList.Add(go);
        }
        else if(spawnPoint == broadcastSpawnPoint)
        {
            broadcastChatContentWindow.sizeDelta = new Vector2(0, broadcastVideoList.Count * spaceBetweenUserVideos + 140);
            spawnY = broadcastVideoList.Count * spaceBetweenUserVideos * -1;
            UpdatePlayerVideoPostions();
            broadcastVideoList.Add(go);
        }
        go.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, spawnY);

        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        if (isLocalUser == false)
        {
            videoSurface.SetForMultiChannelUser(channelID, uid);
        }
    }

    private void UpdatePlayerVideoPostions()
    {
        for (int i = 0; i < partyVideoList.Count; i++)
        {
            partyVideoList[i].GetComponent<RectTransform>().anchoredPosition = Vector2.down * spaceBetweenUserVideos * i;
        }
    }

    private void RemoveUserVideoSurface(uint deletedUID)
    {
        foreach (GameObject player in partyVideoList)
        {
            if (player.name == deletedUID.ToString())
            {
                partyVideoList.Remove(player);
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
