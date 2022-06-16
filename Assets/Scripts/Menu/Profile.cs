using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Profile : MonoBehaviour
{

    [Header("Profile Information")]
    public Texture2D ProfileAvatar = null;
    public string ProfileName = null;

    [Header("Profile UI")]
    [SerializeField] private RawImage profileAvatarImage;
    [SerializeField] private TMP_Text profileNameText = null;

#if !UNITY_WEBGL

    private void Awake()
    {
        //  Check profile folder directory exists
        if (Directory.Exists("./Profile"))
        {
            //  Get ProfileName data from JSONUtils
            LoadProfileNameFromJson();

            //  Check custom ProfileAvatar
            if (File.Exists("./Profile/Avatar.png"))
            {
                ProfileAvatar = new Texture2D(2, 2);

                //  Start load ProfileAvatar
                StartCoroutine(loadImageFromfolder());
            }
        }
        else
        {
            //  Try to DirectoryCreate
            DirectoryCreate();
        }
    }

    private void DirectoryCreate()
    {
        try
        {
            // Try to create the directory.
            Directory.CreateDirectory("./Profile");
        }
        finally { }
    }

    private IEnumerator loadImageFromfolder()
    {
        using UnityWebRequest uwr = UnityWebRequest.Get("file:///./Profile/Avatar.png");
        yield return uwr.SendWebRequest();
        if (string.IsNullOrEmpty(uwr.error))
        {
            ProfileAvatar.LoadImage(uwr.downloadHandler.data);
            profileAvatarImage.texture = ProfileAvatar;
        }
        else
        {
            Debug.Log(uwr.error);
        }
    }

    private void LoadProfileNameFromJson()
    {
        if (File.Exists("./Profile/Profile.json"))
        {

            string json = File.ReadAllText("./Profile/Profile.json");
            PlayerProfile data = JsonUtility.FromJson<PlayerProfile>(json);

            profileNameText.text = data.Name;
            ProfileName = data.Name;
        }
    }
}

#endif