using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Restarts : MonoBehaviour
{
    [SerializeField] private PlayBackground playBackground;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private InputField inputs;
    [SerializeField] private Text requires;

    private bool isLoading = false;

    private string apiUrl = "https://massmail.gmobile.mn/game/members/login";

    private GameObject FindByName(string objName)
    {
        Transform[] objects = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Transform t in objects)
        {
            if (t.name == objName && t.gameObject.scene.IsValid())
            {
                return t.gameObject;
            }
        }

        return null;
    }

    void Awake()
    {
        if (loginPanel == null)
            loginPanel = FindByName("reinaskmodal");

        if (inputs == null)
        {
            GameObject obj = FindByName("inputagain");

            if (obj != null)
                inputs = obj.GetComponent<InputField>();
        }

        if (requires == null)
        {
            GameObject obj = FindByName("requiredlabelagain");

            if (obj != null)
                requires = obj.GetComponent<Text>();
        }

        if (playBackground == null)
        {
            GameObject obj = FindByName("echotouch");

            if (obj != null)
                playBackground =
                    obj.GetComponent<PlayBackground>();
        }

        if (playBackground == null)
            Debug.LogError("PlayBackground олдсонгүй!");

        if (inputs == null)
            Debug.LogError("inputagain олдсонгүй!");
    }

    public void RestartGame()
    {
        if (isLoading) return;

        if (inputs == null || playBackground == null)
        {
            Debug.LogError("UI эсвэл PlayBackground NULL!");
            return;
        }

        string name = inputs.text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            if (requires != null)
                requires.text = "Нэрээ оруулаарай";

            return;
        }

        if (requires != null)
            requires.text = "";

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

        using (UnityWebRequest request =
            new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "Login API Error: " + request.error
                );

                if (requires != null)
                    requires.text = "Сервертэй холбогдож чадсангүй";

                isLoading = false;
                yield break;
            }

            LoginResponse member = null;

            try
            {
                member = JsonUtility.FromJson<LoginResponse>(
                    request.downloadHandler.text
                );
            }
            catch (Exception e)
            {
                Debug.LogError("JSON алдаа: " + e.Message);
            }

            if (member == null || !member.success)
            {
                if (requires != null)
                    requires.text = "Нэвтрэхэд алдаа гарлаа";

                isLoading = false;
                yield break;
            }

            Debug.Log(
                $"Амжилттай нэвтэрлээ: {member.name}, ID: {member.id}"
            );

            // Шинэ тоглогчийн мэдээллийг дамжуулах
            playBackground.setMemberName(member.name);
            playBackground.setMemberId(member.id);

            // Тоглоомыг дахин эхлүүлэх
            playBackground.RestartGame();

            // Нэвтрэх цонхыг хаах
            if (loginPanel != null)
                loginPanel.SetActive(false);

            // Input цэвэрлэх
            inputs.text = "";

            if (requires != null)
                requires.text = "";
        }

        isLoading = false;
    }
}
