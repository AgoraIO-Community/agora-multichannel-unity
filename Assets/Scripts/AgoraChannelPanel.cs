using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using agora_gaming_rtc;
using UnityEngine.UI;

public class AgoraChannelPanel : MonoBehaviour
{
    public string channelName;
    public string channelToken;

    private AgoraChannel newChannel;

    public List<GameObject> userVideoList;

    public Transform videoSpawnPoint;
    public RectTransform panelContentWindow;

    public bool isPublishing;

    private float spaceBetweenUserVideos = 150f;

    void Start()
    {
        userVideoList = new List<GameObject>();

    }

    public void Button_JoinChannel()
    {
        newChannel = AgoraEngine.mRtcEngine.CreateChannel(channelName);

        newChannel.ChannelOnJoinChannelSuccess = OnJoinChannelSuccessHandler;
        newChannel.ChannelOnUserJoined = OnUserJoinedHandler;
        newChannel.ChannelOnLeaveChannel = OnLeaveHandler;
        newChannel.ChannelOnUserOffLine = OnUserLeftHandler;

        newChannel.JoinChannel(channelToken, null, 0, new ChannelMediaOptions(true, true));
    }

    public void Button_LeaveChannel()
    {
        newChannel.LeaveChannel();

        Debug.Log("Leaving channel: " + channelName);
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

    public void OnJoinChannelSuccessHandler(string channelID, uint uid, int elapsed)
    {
        Debug.Log("Join party channel success - channel: " + channelID + " uid: " + uid);
        MakeImageSurface(channelID, uid, videoSpawnPoint, true);
    }

    public void OnUserJoinedHandler(string channelID, uint uid, int elapsed)
    {
        Debug.Log("On user joined party - channel: + " + uid);
        MakeImageSurface(channelID, uid, videoSpawnPoint);
    }

    private void OnLeaveHandler(string channelID, RtcStats stats)
    {
        Debug.Log("You left the party channel.");
        foreach (GameObject player in userVideoList)
        {
            Destroy(player.gameObject);
        }

        userVideoList.Clear();
    }

    public void OnUserLeftHandler(string channelID, uint uid, USER_OFFLINE_REASON reason)
    {
        Debug.Log("User: " + uid + " left party - channel: + " + uid + "for reason: " + reason);
        RemoveUserVideoSurface(uid);
    }

    void MakeImageSurface(string channelID, uint uid, Transform spawnPoint, bool isLocalUser = false)
    {
        if (GameObject.Find(uid.ToString()) != null)
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

        panelContentWindow.sizeDelta = new Vector2(0, userVideoList.Count * spaceBetweenUserVideos + 140);
        float spawnY = userVideoList.Count * spaceBetweenUserVideos * -1;
        UpdatePlayerVideoPostions();
        userVideoList.Add(go);        

        go.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, spawnY);

        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        if (isLocalUser == false)
        {
            videoSurface.SetForMultiChannelUser(channelID, uid);
        }
    }

    private void UpdatePlayerVideoPostions()
    {
        for (int i = 0; i < userVideoList.Count; i++)
        {
            userVideoList[i].GetComponent<RectTransform>().anchoredPosition = Vector2.down * spaceBetweenUserVideos * i;
        }
    }

    private void RemoveUserVideoSurface(uint deletedUID)
    {
        foreach (GameObject player in userVideoList)
        {
            if (player.name == deletedUID.ToString())
            {
                userVideoList.Remove(player);
                Destroy(player.gameObject);
                break;
            }
        }

        // update positions of new players
        UpdatePlayerVideoPostions();

        Vector2 oldContent = panelContentWindow.sizeDelta;
        panelContentWindow.sizeDelta = oldContent + Vector2.down * spaceBetweenUserVideos;
        panelContentWindow.anchoredPosition = Vector2.zero;
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