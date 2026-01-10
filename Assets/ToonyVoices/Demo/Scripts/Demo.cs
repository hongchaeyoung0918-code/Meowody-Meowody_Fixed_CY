using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ToonyVoices))]
public class Demo : MonoBehaviour
{
    //--------------------------------------------------------------------------
    #region Properties

    [SerializeField]
    private Text[] _textFields;
    [SerializeField]
    private Image[] _textBubbles;
    private ToonyVoices _voices;
    private int _currentIndex = 0;
    private string[] _messages = {
        "Welcome to the ToonyVoices game audio asset! Test Test 한국어 되냐 jebal",
        "This asset is designed to provide cartoon style voices, with any given message, in the style of the Animal Crossing games. 안녕하세요 반갑습니다 최고 사랑해",
        "집에 보내줘 살려줘 잠와 엄마아빠 보고싶어요 술 땡겨"
    };

    private int _characterDisplayedCount = 0;

    #endregion

    //--------------------------------------------------------------------------
    #region Unity methods

    private void Start()
    {
        foreach(Image image in _textBubbles) { image.gameObject.SetActive(false); }
        foreach(Text text in _textFields) { text.gameObject.SetActive(false); }
        _voices = GetComponent<ToonyVoices>();

        StartCoroutine(DelaySpeech(0.5f));
    }

    #endregion

    //--------------------------------------------------------------------------
    #region Class methods

    private IEnumerator DelaySpeech(float delay)
    {
        yield return new WaitForSeconds(delay);
        _textFields[_currentIndex].gameObject.SetActive(true);
        _textBubbles[_currentIndex].gameObject.SetActive(true);
        
        _textFields[_currentIndex].text = "";

        if (_currentIndex == 1)
        {
            _voices.Speak(_messages[1], 3f);
            yield break;
        }
        if(_currentIndex == 2)
        {
            _voices.Speak(_messages[2], 2f, 0.7f);
            yield break;
        }
        _voices.Speak(_messages[_currentIndex]);
    }

    private IEnumerator CloseLastBubble()
    {
        yield return new WaitForSeconds(0.5f);
        _textFields[_currentIndex - 1].gameObject.SetActive(false);
        _textBubbles[_currentIndex - 1].gameObject.SetActive(false);
    }

    #endregion

    //--------------------------------------------------------------------------
    #region Class event handlers

    public void OnCharacterSounded(int originalIndex)
    {
        // 현재 출력해야 할 메시지(_messages[_currentIndex])와 전달받은 인덱스를 사용
        string currentMessage = _messages[_currentIndex];

        if (originalIndex + 1 <= currentMessage.Length)
        {
            _textFields[_currentIndex].text = currentMessage.Substring(0, originalIndex + 1);
        }
    }

    public void OnSentenceFinished()
    {
        // 현재 텍스트 필드의 내용을 비웁니다.
        _textFields[_currentIndex].text = "";

        _currentIndex++;
        if (_currentIndex == _messages.Length)
        {
            StartCoroutine(CloseLastBubble());
            return;
        }

        // 다음 문장으로 넘어가기 전 모든 UI를 잠시 끕니다.
        foreach (Image image in _textBubbles) { image.gameObject.SetActive(false); }
        foreach (Text text in _textFields) { text.gameObject.SetActive(false); }

        // DelaySpeech 코루틴이 시작될 때 다음 텍스트 필드의 내용을 비웁니다.
        // (여기서는 DelaySpeech 내부에서 처리해도 되지만, 안전하게 여기서도 처리 가능)
        // _textFields[_currentIndex].text = ""; // 여기서도 가능

        StartCoroutine(DelaySpeech(0.25f));
    }
    #endregion
}
