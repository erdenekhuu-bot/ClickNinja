using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;


[Serializable]
public class MemberRequest
{
    public string name;
}

[Serializable]
public class MemberResponse
{
    public bool success;
    public int id;
    public string name;
    public int score;
}

[Serializable]
public class ScoreData
{
    public int score;
}

[Serializable]
public class MembersResponse
{
    public bool success;
    public ApiMember[] data;
}

[Serializable]
public class ApiMember
{
    public int id;
    public string name;
    public string phone;
    public int score;
}

public class InitCanvas : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject innerpannel;
    [SerializeField] private GameObject FirstBackground;
    [SerializeField] private GameObject SecondBackground;
    [SerializeField] private Text timevalue;
    [SerializeField] private Text requires;
    [SerializeField] private Text men;
    [SerializeField] private GameObject startimg;
    [SerializeField] private GameObject secondLogin;
    [SerializeField] private GameObject secondinnerpannel;
    public Sprite goldMedal;
    public Sprite silverMedal;
    public Sprite bronzeMedal;
    private readonly System.Collections.Generic.List<GameObject> createdRows = new();

    private string apiUrl = "https://massmail.gmobile.mn/game/members/login";
    private string patchUrl = "https://massmail.gmobile.mn/game/members";
    private string resetUrl = "https://massmail.gmobile.mn/game/reset/member";
    private bool isLoading = false;
    public Image echotouch;
    public Sprite pressedSprite;
    public Sprite resets;
    public Text recordvalue;
    private Sprite originalSprite;
    private int count = 0;
    private float remainingTime = 10f;
    private bool scoreSent = false;
    private int memberId;
    public GameObject ratingpanel;
    public AudioSource audioSource;
    public AudioClip clickSound;
    private Sprite originalResetSprite;
    private Vector3 originalResetScale;

    public bool gameStarted
    {
        get
        {
            return PlayerPrefs.GetInt("gameStarted", 0) == 1;
        }
        set
        {
            PlayerPrefs.SetInt("gameStarted", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }


    public void SetMemberData(int id)
    {
        memberId = id;
        PlayerPrefs.SetInt("memberId", id);
        PlayerPrefs.Save();
    }

    public int GetMemberId(){
        if (memberId <= 0)
            memberId = PlayerPrefs.GetInt("memberId", 0);
        return memberId;
    }

    private IEnumerator LoginMember(string name)
    {
        isLoading = true;
        MemberRequest data = new MemberRequest
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
            MemberResponse member = null;
            try
            {
                member = JsonUtility.FromJson<MemberResponse>(responseJson);
            }
            catch (Exception e)
            {
                Debug.LogError("JSON алдаа: " + e.Message);
            }

            if (member == null || !member.success || member.id <= 0)
            {
                isLoading = false;
                yield break;
            }
            men.text = member.name;
            SetMemberData(member.id);
            loginPanel.SetActive(false);
            //secondLogin.SetActive(false);
            FirstBackground.SetActive(false);
            SecondBackground.SetActive(true);
            remainingTime = 10f;
            count = 0;
            scoreSent = false;
            recordvalue.text = "0";
            timevalue.text = "10";
            gameStarted = true;

        }

        isLoading = false;
    }

    void Start()
    {
        recordvalue.text = count.ToString();
        timevalue.text = "10";
        originalSprite = echotouch.sprite;
        StartCoroutine(GetMembers());
    }

    void Update(){
        if (!gameStarted || scoreSent) return;
        remainingTime -= Time.deltaTime;
        remainingTime = Mathf.Max(remainingTime, 0f);

        timevalue.text = Mathf.CeilToInt(remainingTime).ToString();

        if (remainingTime <= 0f){
            gameStarted = false;
            scoreSent = true;
            timevalue.text = "0";
            echotouch.sprite = originalSprite;
            StartCoroutine(SendScore());
        }
    }

    public void NextScreen()
    {
        if (isLoading) return;
        InputField input = innerpannel.GetComponentInChildren<InputField>(true);
        string name = input.text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            requires.text = "Profile required !";
            return;
        }
        StartCoroutine(LoginMember(name));
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (!gameStarted) return;

        GameObject clickedObject = eventData.pointerPressRaycast.gameObject;

        if (clickedObject != echotouch.gameObject)
            return;

        echotouch.sprite = pressedSprite;
        count++;

        recordvalue.text = count.ToString();

        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }


    public void resetScore()
    {
        //StartCoroutine(ResetMember());
        //secondLogin.SetActive(true);

    }

    //public void nextPlayer()
    //{

    //    loginPanel.SetActive(true);
    //    secondLogin.SetActive(false);
    //    FirstBackground.SetActive(true);
    //    SecondBackground.SetActive(false);
    //    count = 0;
    //    recordvalue.text = "0";

    //    remainingTime = 10f;
    //    timevalue.text = "10 сек";
    //}


    public void nextPlayer()
    {

        gameStarted = false;
        loginPanel.SetActive(true);
        secondLogin.SetActive(false);
        FirstBackground.SetActive(true);
        SecondBackground.SetActive(false);
        count = 0;
        recordvalue.text = "0";
        remainingTime = 10f;
        timevalue.text = "10";
        scoreSent = false;
        echotouch.sprite = originalSprite;
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        echotouch.sprite = originalSprite;
    }

    public void callWindow()
    { 
        loginPanel.SetActive(true); 
    }

    IEnumerator SendScore()
    {

        int member = GetMemberId();
        if (member <= 0)
        {
            Debug.Log("Member ID олдсонгүй!");
            yield break;
        }
        string url = $"{patchUrl}/{member}";
        ScoreData data = new ScoreData
        {
            score = count
        };
        string json = JsonUtility.ToJson(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                //Debug.Log("Оноо амжилттай илгээгдлээ!");
                //Debug.Log("Response: " + request.downloadHandler.text);
                StartCoroutine(GetMembers());
            }
            else
            {
                //Debug.LogError("API Error: " + request.error);
                //Debug.LogError("Response: " + request.downloadHandler.text);
            }
        }
    }


    IEnumerator GetMembers()
    {
        if (SecondBackground == null)
        {
            yield break;
        }

        Transform board = SecondBackground.transform.Find("memberrating");

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
            MembersResponse response = null;
            try
            {
                response = JsonUtility.FromJson<MembersResponse>(request.downloadHandler.text);
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
                ApiMember member = response.data[i];

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


    private void ShowStartImg(bool show)
    {
        CanvasGroup cg = startimg.GetComponent<CanvasGroup>();
        if (cg == null) cg = startimg.AddComponent<CanvasGroup>();

        cg.alpha = show ? 1f : 0f;
        cg.blocksRaycasts = show;
        cg.interactable = show;
    }


}
