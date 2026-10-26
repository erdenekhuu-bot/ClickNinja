using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[Serializable]
public class AllMembersResponse
{
    public bool success;
    public ApiMemberModel[] data;
}

[Serializable]
public class ApiMemberModel
{
    public int id;
    public string name;
    public string phone;
    public int score;
}

public class PlayBackground : MonoBehaviour,IPointerDownHandler,IPointerUpHandler
{
    [Header("UI")]
    [SerializeField] private Text member;
    [SerializeField] private Text timevalue;
    [SerializeField] private Text recordvalue;
    [SerializeField] private GameObject reinaskmodal;
    [SerializeField] private GameObject PlayBackgroundCanvas;

    [Header("Sprite")]
    [SerializeField] private Sprite pressedSprite;

    [Header("Sound")]
    [SerializeField] private AudioClip clickSound;

    private Image echotouch;
    private Sprite originalSprite;
    private AudioSource audioSource;
    public Sprite goldMedal;
    public Sprite silverMedal;
    public Sprite bronzeMedal;
    public GameObject ratingpanel;
    private int memberId;
    private readonly System.Collections.Generic.List<GameObject> createdRows = new();
    private string patchUrl = "https://massmail.gmobile.mn/game/members";
    private string apiUrl = "https://massmail.gmobile.mn/game/members/login";
    private string resetUrl = "https://massmail.gmobile.mn/game/reset/member";

    private int count = 0;
    private float remainingTime = 20f;
    private bool scoreSent = false;
    private bool gameStarted = false;
    private const string MEMBER_ID_KEY = "memberId";

    public void setMemberId(int id)
    {
        if (id <= 0) return;

        PlayerPrefs.SetInt(MEMBER_ID_KEY, id);
        PlayerPrefs.Save();

        Debug.Log("Member ID saved: " + id);
    }

    public int getMemberId()
    {
        int id = PlayerPrefs.GetInt(MEMBER_ID_KEY, 0);

        Debug.Log("Member ID loaded: " + id);

        return id;
    }

    public void RestartGame()
    {
        remainingTime = 20f;
        count = 0;
        scoreSent = false;
        gameStarted = true;

        timevalue.text = "20";

        if (recordvalue != null)
            recordvalue.text = "0";

        if (echotouch != null)
            echotouch.sprite = originalSprite;

        reinaskmodal.SetActive(false);
    }
    void Awake()
    {
        echotouch = GetComponent<Image>();
        if (echotouch != null) originalSprite = echotouch.sprite;

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    void OnEnable()
    {
        RestartGame();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!gameStarted) return;

        if (pressedSprite != null) echotouch.sprite = pressedSprite;

        count++;

        if (recordvalue != null) recordvalue.text = count.ToString();

        if (clickSound != null) audioSource.PlayOneShot(clickSound);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (echotouch != null)
            echotouch.sprite = originalSprite;
    }

    public void setMemberName(string name)
    {
        if (member != null)
            member.text = name;
    }

    void Update()
    {
      
        if (!gameStarted) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            gameStarted = false;

            timevalue.text = "0";
            echotouch.sprite = originalSprite;

            if (!scoreSent)
            {
                scoreSent = true;
                StartCoroutine(FinishGame());
            }

            return;
        }

        timevalue.text = Mathf.CeilToInt(remainingTime).ToString();
    }

    private IEnumerator FinishGame()
    {

        yield return StartCoroutine(SendScore());
        yield return StartCoroutine(GetMembers());

        if (reinaskmodal != null)
            reinaskmodal.SetActive(true);
    }


    void Start() {
        StartCoroutine(GetMembers());
    }

   

    private static void PlaceLeft(RectTransform r, float w, float h, float x)
    {
        LayoutElement le = r.GetComponent<LayoutElement>();
        if (le == null) le = r.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        r.anchorMin = r.anchorMax = new Vector2(0, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(w, h);
        r.anchoredPosition = new Vector2(x, 0);
    }

    
IEnumerator SendScore()
{
    int memberId = getMemberId();

    if (memberId <= 0)
    {
            Debug.LogError("Storage дотор Member ID байхгүй!");
            yield break;
    }
    Debug.Log("SendScore Member ID: " + memberId);
    string url = $"{patchUrl}/{memberId}";
    

        ScoreData data = new ScoreData
    {
        score = count
    };

    string json = JsonUtility.ToJson(data);

    using (UnityWebRequest request =
        new UnityWebRequest(url, "PATCH"))
    {
        request.uploadHandler = new UploadHandlerRaw(
            Encoding.UTF8.GetBytes(json)
        );

        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type", "application/json"
        );

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Оноо амжилттай илгээгдлээ!");
        }
        else
        {
            Debug.LogError("PATCH ERROR: " + request.error);
        }
    }
}

    public void clearMemberId()
    {
        PlayerPrefs.DeleteKey(MEMBER_ID_KEY);
        PlayerPrefs.Save();
    }


    private IEnumerator LoginMember(string name)
    {
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
                
                yield break;
            }
            Debug.Log("Амжилттай нэвтэрлээ: " + member.name);
            this.setMemberName(member.name);
            this.setMemberId(member.id);
        }
    }

    public void ResetMemberScore()
    {
        StartCoroutine(ResetMember());
    }


    IEnumerator ResetMember()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(resetUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Reset error: " + request.error);
                yield break;
            }

            Debug.Log("Бүх хэрэглэгчийн оноо амжилттай reset хийгдлээ!");
        }

        yield return StartCoroutine(GetMembers());
    }

    IEnumerator GetMembers()
    {
        if (PlayBackgroundCanvas == null)
        {
            yield break;
        }

        Transform board = PlayBackgroundCanvas.transform.Find("memberrating");

        if (board == null)
        {
            yield break;
        }

        Transform template = board.Find("ratingpanel");

        if (template == null)
        {
            yield break;
        }

        ratingpanel = template.gameObject;
        RectTransform parentRect = board.GetComponent<RectTransform>();
        RectTransform templateRect = template.GetComponent<RectTransform>();

        if (parentRect == null || templateRect == null)
        {
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(patchUrl))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                yield break;
            }
            AllMembersResponse response = null;
            try
            {
                response = JsonUtility.FromJson<AllMembersResponse>(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                Debug.LogError("JSON Error: " + e.Message);
                yield break;
            }

            if (response == null || !response.success || response.data == null)
            {
                Debug.LogError("Invalid members response!");
                yield break;
            }



            // Remove all previously generated ranking rows
            for (int i = board.childCount - 1; i >= 0; i--)
            {
                Transform child = board.GetChild(i);

                if (child.name.StartsWith("Rank_") ||
                    child.name.StartsWith("ratingpanel(Clone)"))
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }

            createdRows.Clear();
            yield return null;
            Canvas.ForceUpdateCanvases();

            Vector3[] corners = new Vector3[4];
            templateRect.GetWorldCorners(corners);

            Vector3 topLeft = parentRect.InverseTransformPoint(corners[1]);

            Vector3 topRight = parentRect.InverseTransformPoint(corners[2]);

            float rowWidth = topRight.x - topLeft.x;
            float rowHeight = rowWidth * 0.12f;
            float rowSpacing = rowHeight * 0.18f;
            float medalSize = rowHeight * 1.1f;
            float slotWidth = medalSize;
            float slotCenterX = slotWidth * 0.5f;
            int fontSize = Mathf.RoundToInt(rowHeight * 0.48f);
            float scoreWidth = rowHeight * 1.8f;

            float startX = topLeft.x - parentRect.rect.xMin;
            float startY = topLeft.y - parentRect.rect.yMax;

            ratingpanel.SetActive(false);

            // 6. Generate maximum 10 players
            int total = Mathf.Min(10, response.data.Length);

            for (int i = 0; i < total; i++)
            {
                ApiMemberModel member = response.data[i];

                GameObject newRow = Instantiate(ratingpanel, board, false);
                newRow.name = $"Rank_{i + 1}";
                newRow.SetActive(false);

                // ---- Row size / position ----
                RectTransform rect = newRow.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.sizeDelta = new Vector2(rowWidth, rowHeight);
                rect.anchoredPosition = new Vector2(
                    startX,
                    startY - i * (rowHeight + rowSpacing)
                );
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;

                // Row ignores the parent's auto-layout
                LayoutElement rowLayout = newRow.GetComponent<LayoutElement>();
                if (rowLayout == null) rowLayout = newRow.AddComponent<LayoutElement>();
                rowLayout.ignoreLayout = true;

                // ---- Find children ----
                Text nameText = null;
                Text scoreText = null;
                Image medalImage = null;

                foreach (Transform child in newRow.GetComponentsInChildren<Transform>(true))
                {
                    if (child == newRow.transform) continue;

                    if (child.name == "membersrate") nameText = child.GetComponent<Text>();
                    if (child.name == "scoreText") scoreText = child.GetComponent<Text>();
                    if (child.name == "Image") medalImage = child.GetComponent<Image>();
                }

                if (nameText == null || scoreText == null)
                {
                    Destroy(newRow);
                    continue;
                }

                // ---- Horizontal layout (name + score only) ----
                HorizontalLayoutGroup flex = newRow.GetComponent<HorizontalLayoutGroup>();
                if (flex == null) flex = newRow.AddComponent<HorizontalLayoutGroup>();

                flex.padding = new RectOffset(
                    Mathf.RoundToInt(slotWidth + 8),     // left: leave room for medal/rank
                    Mathf.RoundToInt(rowHeight * 0.4f),  // right
                    0, 0);
                flex.spacing = 8;
                flex.childAlignment = TextAnchor.MiddleLeft;
                flex.childControlWidth = true;
                flex.childControlHeight = true;
                flex.childForceExpandWidth = false;
                flex.childForceExpandHeight = false;

                // ---- Background by rank ----
                Image background = newRow.GetComponent<Image>();
                Outline outline = newRow.GetComponent<Outline>();
                if (outline == null) outline = newRow.AddComponent<Outline>();

                if (background != null)
                {
                    Color32 panelColor;
                    Color32 borderColor;

                    if (i == 0)
                    {
                        panelColor = new Color32(175, 115, 0, 255);
                        borderColor = new Color32(255, 215, 0, 255);
                    }
                    else if (i == 1)
                    {
                        panelColor = new Color32(55, 60, 70, 255);
                        borderColor = new Color32(200, 210, 220, 255);
                    }
                    else if (i == 2)
                    {
                        panelColor = new Color32(90, 45, 20, 255);
                        borderColor = new Color32(255, 140, 30, 255);
                    }
                    else
                    {
                        // flat rows 4-10, like the mockup
                        panelColor = new Color32(15, 15, 20, 0);
                        borderColor = new Color32(60, 60, 60, 255);
                    }

                    background.color = panelColor;
                    background.raycastTarget = false;

                    outline.effectColor = borderColor;
                    outline.effectDistance = new Vector2(1, -1);
                    outline.enabled = i < 3;   // no border on flat rows
                }

                // ---- Player name ----
                nameText.text = member.name;
                nameText.fontSize = fontSize;
                nameText.fontStyle = FontStyle.Bold;
                nameText.color = Color.white;
                nameText.alignment = TextAnchor.MiddleLeft;
                nameText.resizeTextForBestFit = true;
                nameText.resizeTextMinSize = Mathf.RoundToInt(fontSize * 0.6f);
                nameText.resizeTextMaxSize = fontSize;
                nameText.raycastTarget = false;

                LayoutElement nameLayout = nameText.GetComponent<LayoutElement>();
                if (nameLayout == null) nameLayout = nameText.gameObject.AddComponent<LayoutElement>();
                nameLayout.ignoreLayout = false;
                nameLayout.minWidth = 0;
                nameLayout.preferredWidth = -1;
                nameLayout.flexibleWidth = 1;
                nameLayout.preferredHeight = rowHeight;

                // ---- Player score ----
                scoreText.text = member.score.ToString("N0");
                scoreText.fontSize = fontSize;
                scoreText.fontStyle = FontStyle.Bold;
                scoreText.color = Color.white;
                scoreText.alignment = TextAnchor.MiddleRight;
                scoreText.raycastTarget = false;

                LayoutElement scoreLayout = scoreText.GetComponent<LayoutElement>();
                if (scoreLayout == null) scoreLayout = scoreText.gameObject.AddComponent<LayoutElement>();
                scoreLayout.ignoreLayout = false;
                scoreLayout.minWidth = scoreWidth;
                scoreLayout.preferredWidth = scoreWidth;
                scoreLayout.flexibleWidth = 0;
                scoreLayout.preferredHeight = rowHeight;

                // ---- Medal (top 3) or rank number ----
                if (medalImage != null)
                {
                    if (i < 3)
                    {
                        medalImage.sprite = i == 0 ? goldMedal : i == 1 ? silverMedal : bronzeMedal;
                        medalImage.preserveAspect = true;
                        medalImage.color = Color.white;
                        medalImage.raycastTarget = false;
                        medalImage.gameObject.SetActive(true);

                        PlaceLeft(medalImage.rectTransform, medalSize, medalSize, slotCenterX);
                    }
                    else
                    {
                        medalImage.gameObject.SetActive(false);

                        Text rankNumber = Instantiate(nameText, newRow.transform, false);
                        rankNumber.name = "rankNumber";
                        rankNumber.text = (i + 1).ToString();
                        rankNumber.alignment = TextAnchor.MiddleCenter;
                        rankNumber.fontSize = fontSize;
                        rankNumber.resizeTextForBestFit = false;

                        PlaceLeft(rankNumber.rectTransform, slotWidth, rowHeight, slotCenterX);
                    }
                }

                // ---- Order: only name and score take part in the layout group ----
                nameText.transform.SetSiblingIndex(0);
                scoreText.transform.SetAsLastSibling();

                // ---- Show ----
                newRow.SetActive(true);
                newRow.transform.SetAsLastSibling();

                createdRows.Add(newRow);
            }


        }
    }
}
