using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;


[Serializable]
public class LoginRequest
{
    public string name;
}

[Serializable]
public class LoginResponse
{
    public bool success;
    public int id;
    public string name;
    public int score;
}

public class LoginPage : MonoBehaviour
{
    [SerializeField] private GameObject FirstBackground;
    [SerializeField] private GameObject SecondBackground;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private InputField inputs;
    [SerializeField] private Text requires;

    private bool isLoading = false;
    private string apiUrl = "https://massmail.gmobile.mn/game/members/login";
    private PlayBackground playBackground;

    public void callWindow()
    {
        loginPanel.SetActive(true);
    }

    private GameObject FindByName(string objName)
    {
        foreach (Transform t in FindObjectsOfType<Transform>(true))
        {
            if (t.name == objName && t.gameObject.scene.IsValid())
                return t.gameObject;
        }
        return null;
    }

    void Awake()
    {
        FirstBackground = FindByName("LoginCanvas");
        SecondBackground = FindByName("PlayBackgroundCanvas");
        loginPanel = FindByName("askmodal");
        inputs = FindByName("inputs").GetComponent<InputField>();
        requires = FindByName("requiredlabel").GetComponent<Text>();
        playBackground = SecondBackground.GetComponentInChildren<PlayBackground>(true);
    }

    public void NextFrame()
    {
        if (isLoading) return;
        string name = inputs.text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            requires.text = "Нэрээ оруулаарай";
            return;
        }
        StartCoroutine(LoginMember(name));
    }


    private IEnumerator LoginMember(string name)
    {
        isLoading = true;
        LoginRequest data = new LoginRequest
        {
            name = name
        };

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                isLoading = false;
                yield break;
            }
            string responseJson = request.downloadHandler.text;
            LoginResponse member = null;
            try
            {
                member = JsonUtility.FromJson<LoginResponse>(responseJson);
            }
            catch (Exception e)
            {
                Debug.LogError("JSON алдаа: " + e.Message);
            }

            if (!member.success)
            {
                isLoading = false;
                yield break;
            }
            Debug.Log("Амжилттай нэвтэрлээ: " + member.name);
            playBackground.setMemberName(member.name);
            playBackground.setMemberId(member.id);
            FirstBackground.SetActive(false);
            SecondBackground.SetActive(true);
            loginPanel.SetActive(false);

        }
        isLoading = false;
    }
}
