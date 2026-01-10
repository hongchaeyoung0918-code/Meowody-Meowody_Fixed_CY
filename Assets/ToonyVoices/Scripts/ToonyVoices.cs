using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

static public class ToonyVoicesResources
{
	//--------------------------------------------------------------------------
	#region Fields

	static private Dictionary<string, AudioClip> _sounds = new Dictionary<string, AudioClip>();

    #endregion

    //--------------------------------------------------------------------------
    #region Class methods

    /// <summary>
    /// Attempts to return an AudioClip for a given letter sound.
    /// <see cref="PopulateSoundDictionary"/> called to ensure dictionary is not empty.
    /// </summary>
    /// <param name="forSound">Character representing the sound</param>
    /// <returns>AudioClip or null if sound not found</returns>
    static public AudioClip GetAudioClip(string forSound)
	{
		PopulateSoundDictionary();
		return _sounds[forSound] ?? null;
	}

	/// <summary>
	/// Checks if the key exists within the dictionary.
	/// <see cref="PopulateSoundDictionary"/> called to ensure dictionary is not empty.
	/// </summary>
	/// <param name="key">Key value as a string to search for</param>
	/// <returns>A boolean value based on the existence of the key</returns>
	static public bool ContainsKey(string key)
    {
		PopulateSoundDictionary();
		return _sounds.ContainsKey(key);
    }

	/// <summary>
	/// Creates a dictionary of letter sounds associated to AudioClips.
	/// Does nothing is the dictionary is already populated.
	/// </summary>
	static private void PopulateSoundDictionary()
	{
		if(_sounds.Count > 0) { return; }

		_sounds.Add("a", Resources.Load("a") as AudioClip);
		_sounds.Add("b", Resources.Load("b") as AudioClip);
		_sounds.Add("c", Resources.Load("c") as AudioClip);
		_sounds.Add("d", Resources.Load("d") as AudioClip);
		_sounds.Add("e", Resources.Load("e") as AudioClip);
		_sounds.Add("f", Resources.Load("f") as AudioClip);
		_sounds.Add("g", Resources.Load("g") as AudioClip);
		_sounds.Add("h", Resources.Load("h") as AudioClip);
		_sounds.Add("i", Resources.Load("i") as AudioClip);
		_sounds.Add("j", Resources.Load("j") as AudioClip);
		_sounds.Add("k", Resources.Load("k") as AudioClip);
		_sounds.Add("l", Resources.Load("l") as AudioClip);
		_sounds.Add("m", Resources.Load("m") as AudioClip);
		_sounds.Add("n", Resources.Load("n") as AudioClip);
		_sounds.Add("o", Resources.Load("o") as AudioClip);
		_sounds.Add("p", Resources.Load("p") as AudioClip);
		_sounds.Add("q", Resources.Load("q") as AudioClip);
		_sounds.Add("r", Resources.Load("r") as AudioClip);
		_sounds.Add("s", Resources.Load("s") as AudioClip);
		_sounds.Add("t", Resources.Load("t") as AudioClip);
		_sounds.Add("u", Resources.Load("u") as AudioClip);
		_sounds.Add("v", Resources.Load("v") as AudioClip);
		_sounds.Add("w", Resources.Load("w") as AudioClip);
		_sounds.Add("x", Resources.Load("x") as AudioClip);
		_sounds.Add("y", Resources.Load("y") as AudioClip);
		_sounds.Add("z", Resources.Load("z") as AudioClip);
		_sounds.Add("th", Resources.Load("th") as AudioClip);
		_sounds.Add("sh", Resources.Load("sh") as AudioClip);
		_sounds.Add(" ", Resources.Load("pause") as AudioClip);
		_sounds.Add(".", Resources.Load("pauselong") as AudioClip);
	}

	#endregion
}

[Serializable]
//public class CharacterSoundedEvent : UnityEvent<string> { }
public class CharacterSoundedEvent : UnityEvent<int> { }

[RequireComponent(typeof(AudioSource))]
public class ToonyVoices : MonoBehaviour
{
    //--------------------------------------------------------------------------
    #region Class structs

    private struct CharacterToken
    {
        //--------------------------------------------------------------------------
        #region Properties

        public string Character { get; private set; }
		public bool Inflective { get; private set; }

        #endregion

        // 새 필드
        public int OriginalCharacterIndex { get; private set; }

        //--------------------------------------------------------------------------
        #region Constructor

        public CharacterToken(string character, bool inflective, int originalIndex)
        {
            Character = character;
            Inflective = inflective;
            OriginalCharacterIndex = originalIndex; // 인덱스 저장
        }

        #endregion
    }

	#endregion

	//--------------------------------------------------------------------------
	#region Fields

	[SerializeField]
	private float _basePitch = 2f;
	[SerializeField]
	private float _pitchRange = 0.35f;
	[SerializeField]
	private float _inflectionPitchModifier = 0.4f;
    [SerializeField]
	private CharacterSoundedEvent _characterSoundedEvent = null;
	[SerializeField]
	private UnityEvent _sentenceFinishedEvent = null;
	private AudioSource _source;
	private Queue<CharacterToken> _queue = new Queue<CharacterToken>();
	private float _previousVolume;

    private int _currentSentenceIndex = 0;
    #endregion

    //--------------------------------------------------------------------------
    #region Properties

    public CharacterSoundedEvent CharacterSounded
    {
		get
        {
			return _characterSoundedEvent;
        }
    }

	public UnityEvent SentenceFinished
    {
		get
        {
			return _sentenceFinishedEvent;
        }
    }

    #endregion

    //--------------------------------------------------------------------------
    #region Unity methods

    private void Awake()
    {
        _source = GetComponent<AudioSource>();

        // 안전을 위한 Null 체크:
        if (_source == null)
        {
            Debug.LogError("ToonyVoices requires an AudioSource component on the same GameObject!");
            enabled = false;
            return;
        }

        _source.playOnAwake = false;
        _source.pitch = _basePitch;
        _previousVolume = _source.volume;
    }

/*    private void Start()
	{
		_source = GetComponent<AudioSource>();

        _source.playOnAwake = false;
		_source.pitch = _basePitch;
		_previousVolume = _source.volume;
    }*/

    #endregion

    //--------------------------------------------------------------------------
    #region Class methods

	/// <summary>
    /// Using <see cref="Process(string)"/>, processes the sentence to be spoken.
    /// Calls <see cref="PlayNextSound"/> for each sound in queue.
    /// </summary>
    /// <param name="sentence">The sentence to be spoken</param>
	public void Speak(string sentence)
    {
		Process(sentence);
		PlayNextSound(_basePitch, 1f);
    }

	/// <summary>
	/// Using <see cref="Process(string)"/>, processes the sentence to be spoken.
	/// Calls <see cref="PlayNextSound"/> for each sound in queue.
	/// </summary>
	/// <param name="sentence">The sentence to be spoken</param>
	/// <param name="pitch">The pitch for this sentence, reverts back to the base pitch when finished</param>
	/// <param name="volume">The volume for this sentence to be spoken</param>
	public void Speak(string sentence, float pitch, float volume = 1f, float speedMultiplier = 1f)
	{
		_previousVolume = _source.volume;
		_source.volume = volume;
		Process(sentence);
		PlayNextSound(pitch, speedMultiplier);
	}

	/// <summary>
	/// Processes the input string, splitting by spaces, and calls <see cref="ParseWord(string)"/> for each word individually.
	/// Clears the queue of any remaining tokens.
	/// </summary>
	/// <param name="input">Input string to be processed</param>
	private void Process(string input)
    {
		_queue.Clear();
        _currentSentenceIndex = 0;

        foreach (string word in input.Split(' '))
        {
            //ParseWord(word);
            //AddToQueue(" ", false);

            ParseWord(word, _currentSentenceIndex);
            _currentSentenceIndex += word.Length + 1;

            AddToQueue(" ", false, -1);
        }

        _currentSentenceIndex--;
    }

    /// <summary>
    /// Breaks the word down character by character, looking for inflection, pauses, and compound sounds ('th', 'sh').
    /// <see cref="AddToQueue(string, bool)"/> called for each character found.
    /// </summary>
    /// <param name="word">Word to be parsed</param>
    /*private void ParseWord(string word)
    {
		bool skipNextCharacter = false;
		bool inflective = (word[word.Length - 1] == '?');

		for(int i = 0; i < word.Length; i++)
        {
			string charString = word[i].ToString();
			if (skipNextCharacter == true)
			{
				skipNextCharacter = false;
				continue;
			}

			if(i < word.Length - 1)
            {
				string substring = word.Substring(i, 2);
				if(ToonyVoicesResources.ContainsKey(substring.ToLower()) == true)
                {
					AddToQueue(substring, inflective);
					skipNextCharacter = true;
					continue;
                }
            }

			AddToQueue(charString, inflective);
        }
    }*/

    private void ParseWord(string word, int wordStartIndex)
    {
        bool inflective = (word.Length > 0 && word[word.Length - 1] == '?');

        for (int i = 0; i < word.Length; i++)
        {
            char originalChar = word[i];
            int absoluteIndex = wordStartIndex + i;

            // 1. 한글 음절을 세 부분으로 분리
            KoreanUtil.JamoSplitResult jamo = KoreanUtil.SplitSyllable(originalChar);

            if (jamo.IsValid()) // 한글 완성형 문자인 경우
            {
                // 초성(자음)만 큐에 추가합니다.
                string mappedCho = MapJamoToEnglish(jamo.Chosung);
                if (mappedCho != null) { AddToQueue(mappedCho, inflective, absoluteIndex); }

                /*
                // [기존 코드]: 중성과 종성을 제거합니다.
                string mappedJung = MapJamoToEnglish(jamo.Jungsung);
                if (mappedJung != null) { AddToQueue(mappedJung, inflective, absoluteIndex); }

                if (jamo.Jongsung != '\0')
                {
                    string mappedJong = MapJamoToEnglish(jamo.Jongsung);
                    if (mappedJong != null) { AddToQueue(mappedJong, inflective, absoluteIndex); }
                }
                */
            }
            else // 한글이 아닌 문자 (공백, 기호, 숫자)인 경우
            {
                // 이 토큰은 그대로 유지하여 Pause, 마침표 등의 기능을 살립니다.
                AddToQueue(originalChar.ToString().ToLower(), inflective, absoluteIndex);
            }
        }
    }

    /// <summary>
    /// 한국어 자모(초성, 중성, 종성)를 가장 비슷한 소리의 영어 알파벳 키에 매핑합니다.
    /// 이 키는 ToonyVoicesResources에 등록된 소문자 알파벳 리소스(a, b, c 등)입니다.
    /// </summary>
    private string MapJamoToEnglish(char jamo)
    {
        // C#의 switch 문을 사용하여 자모를 매핑합니다.
        switch (jamo)
        {
            // -------------------------
            // 1. 모음 (중성) 매핑
            // -------------------------
            case 'ㅏ': // A
            case 'ㅑ': return "a";

            case 'ㅓ': // O, U
            case 'ㅕ': return "o"; // '어'는 'o'나 'u' 중 하나를 선택. 여기서는 'o'를 사용합니다.

            case 'ㅗ': // O
            case 'ㅛ': return "o";

            case 'ㅜ': // U
            case 'ㅠ': return "u";

            case 'ㅡ': return "u"; // U (으)

            case 'ㅣ': // I
            case 'ㅐ': // E
            case 'ㅔ': // E
            case 'ㅖ': return "i"; // '이' 소리는 'i'를 사용합니다.

            // 복합 모음은 대표 모음 소리 중 하나를 선택 (예: 'ㅘ'는 'o'나 'a' 중 하나)
            case 'ㅘ':
            case 'ㅙ': return "o";
            case 'ㅚ': return "w"; // 복합 자음 'w' 소리가 있다면 사용합니다. 없다면 'o'나 'i'를 사용합니다. (에셋 리소스에 'w'가 있으므로 사용)
            case 'ㅝ': return "w";
            case 'ㅞ': return "w";
            case 'ㅟ': return "i";
            case 'ㅢ': return "u"; // ㅡ+ㅣ 조합

            // -------------------------
            // 2. 자음 (초성/종성) 매핑
            // -------------------------
            case 'ㄱ': // G, K
            case 'ㅋ': return "k";
            case 'ㄲ': return "k"; // 된소리는 일반 소리 사용

            case 'ㄴ': return "n"; // N
            case 'ㄷ': // D, T
            case 'ㅌ': return "t";
            case 'ㄸ': return "t";

            case 'ㄹ': return "l"; // L (R 소리보다 L 소리가 더 유용할 수 있습니다. 필요에 따라 'r'로 변경 가능)
            case 'ㅁ': return "m"; // M

            case 'ㅂ': // B, P
            case 'ㅍ': return "p";
            case 'ㅃ': return "p";

            case 'ㅅ': // S
            case 'ㅆ': return "s";

            case 'ㅇ': return "a"; // 초성의 'ㅇ'은 음가가 없으므로, 모음 소리 중 하나를 넣어 리듬을 살립니다.

            case 'ㅈ': // J
            case 'ㅊ': return "j";
            case 'ㅉ': return "j";

            case 'ㅎ': return "h"; // H

            // -------------------------
            // 3. 복합 종성 (받침)은 단순화하여 처리
            // -------------------------
            case 'ㄳ': return "k"; // ㄱ으로 끝남
            case 'ㄵ': return "n"; // ㄴ으로 끝남
            case 'ㄶ': return "n"; // ㄴ으로 끝남
            case 'ㄺ': return "k"; // ㄱ으로 끝남
            case 'ㄻ': return "m"; // ㅁ으로 끝남
            case 'ㄼ': return "p"; // ㅂ으로 끝남
            case 'ㄽ': return "l"; // ㄹ으로 끝남
            case 'ㄾ': return "t"; // ㅌ으로 끝남
            case 'ㄿ': return "p"; // ㅍ으로 끝남
            case 'ㅀ': return "l"; // ㄹ으로 끝남
            case 'ㅄ': return "p"; // ㅂ으로 끝남

            // -------------------------
            // 4. 매핑되지 않은 자모 또는 문자
            // -------------------------
            default:
                // 한글 자모 분리 결과가 아닌 경우 (SplitSyllable에서 걸러지지만 안전을 위해)
                return null;
        }
    }

    public void Stop()
    {
        // 1. AudioSource 재생을 즉시 정지합니다.
        if (_source != null && _source.isPlaying)
        {
            _source.Stop();
        }

        // 2. 대기 중인 모든 소리 토큰 큐를 비웁니다.
        // (PlayNextSound가 더 이상 호출되지 않도록 방지)
        _queue.Clear();

        // 3. 현재 실행 중인 대기 코루틴(WaitForAudioCompleted)을 정지합니다.
        // 현재 ToonyVoices 스크립트의 모든 코루틴을 멈춥니다. 
        // 주의: 만약 이 스크립트에서 다른 중요한 코루틴을 사용한다면 해당 코루틴도 멈추게 됩니다.
        StopAllCoroutines();

        // 4. 볼륨을 원상 복구합니다.
        if (_source != null)
        {
            _source.volume = _previousVolume;
        }

        // Stop 후 SentenceFinished 이벤트를 발생시켜 대화 관리자에게 문장이 끝났음을 알릴 수도 있지만,
        // 여기서는 강제 종료이므로 이벤트 호출은 생략합니다.
    }


    /// <summary>
    /// Adds the specified character into the queue to be spoken.
    /// </summary>
    /// <param name="character">Character to enqueue</param>
    /// <param name="inflective">Is the sentence inflective</param>
    /*    private void AddToQueue(string character, bool inflective)
        {
            CharacterToken symbol = new CharacterToken(character, inflective);
            _queue.Enqueue(symbol);
        }*/
    private void AddToQueue(string character, bool inflective, int originalIndex)
    {
        CharacterToken symbol = new CharacterToken(character, inflective, originalIndex);
        _queue.Enqueue(symbol);
    }

    /// <summary>
    /// Plays the next sound in queue, fires sounded and finished events, calls <see cref="PlayNextSound"/> until queue is empty.
    /// </summary>
    private void PlayNextSound(float tonePitch, float speedMultiplier) // 시그니처 수정 완료
    {
        if (_queue.Count == 0)
        {
            _source.volume = _previousVolume;
            if (_sentenceFinishedEvent != null)
            {
                _sentenceFinishedEvent.Invoke();
            }
            return;
        }

        CharacterToken token = _queue.Dequeue();
        if (token.OriginalCharacterIndex >= 0 && _characterSoundedEvent != null)
        {
            // Debug.Log($"Index: {token.OriginalCharacterIndex}, Char: {token.Character}"); // 디버그 로그는 이제 제거하는 것이 좋습니다.

            _characterSoundedEvent.Invoke(token.OriginalCharacterIndex);
        }

        if (ToonyVoicesResources.ContainsKey(token.Character) == false)
        {
            PlayNextSound(tonePitch, speedMultiplier);
            return;
        }

        AudioClip clip = ToonyVoicesResources.GetAudioClip(token.Character);
        if (clip == null)
        {
            PlayNextSound(tonePitch, speedMultiplier);
            return;
        }

        _source.clip = clip;
        // 톤 피치만 AudioSource의 pitch 속성에 적용 (속도에 영향 주지 않음)
        _source.pitch = tonePitch +
                        UnityEngine.Random.Range(-_pitchRange, _pitchRange) +
                        ((token.Inflective == true) ? _inflectionPitchModifier : 0f);
        _source.Play();

        // 코루틴 호출 시, speedMultiplier를 전달
        StartCoroutine(WaitForAudioCompleted(tonePitch, speedMultiplier));
    }

    /// <summary>
    /// Waits for <see cref="AudioSource.isPlaying"/> to return false before playing next sound
    /// </summary>
    /// <param name="pitch">The pitch for the next sound to be played at</param>
    /// <returns></returns>
    private IEnumerator WaitForAudioCompleted(float tonePitch, float speedMultiplier)
    {
        // 1. 현재 재생된 클립의 순수 길이
        float clipDuration = _source.clip.length;

        // 2. 톤 피치는 높낮이만 제어하므로, 속도 계산 시에는 톤 피치 보정을 1.0f로 고정합니다.
        const float BaseTone = 1.0f;
        float pitchAdjustedDuration = clipDuration / BaseTone;

        // 3. 사용자가 원하는 속도 승수 적용
        // speedMultiplier가 0.7f이면 30% 빠르게 재생됩니다.
        float actualWaitTime = pitchAdjustedDuration * speedMultiplier;

        // 계산된 시간만큼만 기다립니다. (속도 제어)
        yield return new WaitForSeconds(actualWaitTime);

        // 대기 시간이 끝났는데도 소리가 남아있다면 강제로 멈춥니다.
        if (_source.isPlaying)
        {
            _source.Stop();
        }

        // 다음 사운드 재생 시에도 톤 피치와 속도 승수를 전달합니다.
        PlayNextSound(tonePitch, speedMultiplier);
    }

    #endregion
}
