using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using agora_gaming_rtc;
using UnityEngine.UI;

public class AgoraChannelPanel : MonoBehaviour
{
    [SerializeField] private string channelName;
    [SerializeField] private string channelToken;

    [SerializeField] private Transform videoSpawnPoint;
    [SerializeField] private RectTransform panelContentWindow;
    [SerializeField] private bool isPublishing;

    private AgoraChannel newChannel;
    private List<GameObject> userVideos;

    private const float SPACE_BETWEEN_USER_VIDEOS = 150f;

    void Start()
    {
        userVideos = new List<GameObject>();
    }

    #region Buttons
    public void Button_JoinChannel()
    {
        if (newChannel == null)
        {
            newChannel = AgoraEngine.mRtcEngine.CreateChannel(channelName);

            newChannel.ChannelOnJoinChannelSuccess = OnJoinChannelSuccessHandler;
            newChannel.ChannelOnUserJoined = OnUserJoinedHandler;
            newChannel.ChannelOnLeaveChannel = OnLeaveHandler;
            newChannel.ChannelOnUserOffLine = OnUserLeftHandler;
            newChannel.ChannelOnRemoteVideoStats = OnRemoteVideoStatsHandler;
        }

        newChannel.JoinChannel(channelToken, null, 0, new ChannelMediaOptions(true, true));
        Debug.Log("Joining channel: " + channelName);
    }

    public void Button_LeaveChannel()
    {
        if(newChannel != null)
        {
            newChannel.LeaveChannel();
            Debug.Log("Leaving channel: " + channelName);
        }
        else
        {
            Debug.LogWarning("Channel: " + channelName + " hasn't been created yet.");
        }   
    }

    public void Button_PublishToPartyChannel()
    {
        if(newChannel == null)
        {
            Debug.LogError("New channel isn't created yet.");
            return;
        }

        if(isPublishing == false)
        {
            int publishResult = newChannel.Publish();
            if(publishResult == 0)
            {
                isPublishing = true;
            }

            Debug.Log("Publishing to channel: " + channelName + " result: " + publishResult);
        }
        else
        {
            Debug.Log("Already publishing to a channel.");
        }
    }

    public void Button_CancelPublishFromChannel()
    {
        if(newChannel == null)
        {
            Debug.Log("New channel isn't created yet.");
            return;
        }

        if(isPublishing == true)
        {
            int unpublishResult = newChannel.Unpublish();
            if(unpublishResult == 0)
            {
                isPublishing = false;
            }

            Debug.Log("Unpublish from channel: " + channelName + " result: " + unpublishResult);
        }
        else
        {
            Debug.Log("Not published to any channel");
        }
    }
    #endregion

    #region Callbacks
    public void OnJoinChannelSuccessHandler(string channelID, uint uid, int elapsed)
    {
        Debug.Log("Join party channel success - channel: " + channelID + " uid: " + uid);
        MakeImageSurface(channelID, uid, true);
    }

    public void OnUserJoinedHandler(string channelID, uint uid, int elapsed)
    {
        Debug.Log("User: " + uid + "joined channel: + " + channelID);
    }

    private void OnLeaveHandler(string channelID, RtcStats stats)
    {
        Debug.Log("You left the party channel.");
        foreach (GameObject player in userVideos)
        {
            Destroy(player);
        }

        userVideos.Clear();
    }

    public void OnUserLeftHandler(string channelID, uint uid, USER_OFFLINE_REASON reason)
    {
        Debug.Log("User: " + uid + " left party - channel: + " + uid + "for reason: " + reason);   
        RemoveUserVideoSurface(uid);
    }
    

    private void OnRemoteVideoStatsHandler(string channelID, RemoteVideoStats remoteStats)
    {
        // If user is publishing...
        if(remoteStats.receivedBitrate > 0)
        {
            bool needsToMakeNewImageSurface = true;
            foreach (GameObject user in userVideos)
            {
                if(remoteStats.uid.ToString() == user.name)
                {
                    needsToMakeNewImageSurface = false;
                    break;
                }
            }
            // ... and their video surface isn't currently displaying ...
            if (needsToMakeNewImageSurface == true)
            {
                // ... display their feed.
                MakeImageSurface(channelID, remoteStats.uid);
            }
        }
        // If user isn't publishing ...
        else if (remoteStats.receivedBitrate == 0)
        {
            bool needsToRemoveUser = false;
            foreach (GameObject user in userVideos)
            {
                // ... but their video stream is currently displaying...
                if(remoteStats.uid.ToString() == user.name)
                {
                    needsToRemoveUser = true;
                }
            }

            if (needsToRemoveUser == true)
            {
                // ... remove their feed.
                RemoveUserVideoSurface(remoteStats.uid);
            }
        }
    }
    #endregion

    void MakeImageSurface(string channelID, uint uid, bool isLocalUser = false)
    {
        if (GameObject.Find(uid.ToString()) != null)
        {
            Debug.Log("A video surface already exists with this uid: " + uid.ToString());
            return;
        }

        // Create my new image surface
        GameObject go = new GameObject();
        go.name = uid.ToString();
        RawImage userVideo = go.AddComponent<RawImage>();
        go.transform.localScale = new Vector3(1, -1, 1);

        // Child it inside the panel scroller
        if (videoSpawnPoint != null)
        {
            go.transform.SetParent(videoSpawnPoint);
        }

        // Update the layout of the panel scrollers
        panelContentWindow.sizeDelta = new Vector2(0, userVideos.Count * SPACE_BETWEEN_USER_VIDEOS);
        float spawnY = userVideos.Count * SPACE_BETWEEN_USER_VIDEOS * -1;

        userVideos.Add(go);

        go.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, spawnY);

        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        if (isLocalUser == false)
        {
            videoSurface.SetForMultiChannelUser(channelID, uid);
        }
    }

    private void UpdatePlayerVideoPostions()
    {
        for (int i = 0; i < userVideos.Count; i++)
        {
            userVideos[i].GetComponent<RectTransform>().anchoredPosition = Vector2.down * SPACE_BETWEEN_USER_VIDEOS * i;
        }
    }

    private void RemoveUserVideoSurface(uint deletedUID)
    {
        foreach (GameObject user in userVideos)
        {
            if (user.name == deletedUID.ToString())
            {
                userVideos.Remove(user);
                Destroy(user);

                UpdatePlayerVideoPostions();

                Vector2 oldContent = panelContentWindow.sizeDelta;
                panelContentWindow.sizeDelta = oldContent + Vector2.down * SPACE_BETWEEN_USER_VIDEOS;
                panelContentWindow.anchoredPosition = Vector2.zero;
                break;
            }
        }        
    }

    private void OnApplicationQuit()
    {
        if(newChannel != null)
        {
            newChannel.LeaveChannel();
            newChannel.ReleaseChannel();
        }
    }
}