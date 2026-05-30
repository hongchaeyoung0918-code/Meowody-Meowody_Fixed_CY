using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    // 1. 싱글톤 패턴 (게임 전체에서 하나만 존재)
    public static SoundManager Instance;

    [System.Serializable]
    public struct Sound
    {
        public string name;
        public AudioClip clip;
    }

    // 2. 모든 효과음을 여기에 등록
    public Sound[] sfxSounds;

    // Dictionary로 변환하여 이름(key)으로 빠르게 찾을 수 있게 함
    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();

    // 3. 효과음을 재생할 AudioSource 컴포넌트 풀
    public AudioSource sfxAudioSourcePrefab; // AudioSource가 붙은 프리팹
    private Queue<AudioSource> sfxSourcePool = new Queue<AudioSource>();
    public int initialPoolSize = 5; // 초기 AudioSource 개수

    void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDictionary();
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 이름으로 AudioClip을 찾기 위한 Dictionary 초기화
    private void InitializeDictionary()
    {
        foreach (Sound s in sfxSounds)
        {
            sfxDictionary.Add(s.name, s.clip);
        }
    }

    // AudioSource 풀 초기화
    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            AudioSource newSource = Instantiate(sfxAudioSourcePrefab, transform);
            newSource.gameObject.SetActive(false);
            sfxSourcePool.Enqueue(newSource);
        }
    }

    // 4. 외부에서 효과음 재생을 요청하는 함수
    public void PlaySFX(string soundName)
    {
        if (sfxDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            AudioSource sourceToUse = GetAudioSourceFromPool();

            if (sourceToUse != null)
            {
                sourceToUse.clip = clip;
                sourceToUse.gameObject.SetActive(true);
                sourceToUse.Play();

                // 재생이 끝나면 자동으로 풀로 돌아가도록 코루틴 사용
                StartCoroutine(ReturnToPoolAfterDelay(sourceToUse, clip.length));
            }
        }
        else
        {
            Debug.LogWarning("Sound: " + soundName + " not found!");
        }
    }

    // 풀에서 AudioSource 가져오기 (없으면 새로 생성)
    private AudioSource GetAudioSourceFromPool()
    {
        if (sfxSourcePool.Count > 0)
        {
            return sfxSourcePool.Dequeue();
        }
        else
        {
            // 풀이 부족하면 새로 생성 (옵션)
            AudioSource newSource = Instantiate(sfxAudioSourcePrefab, transform);
            return newSource;
        }
    }

    // 재생이 끝나면 AudioSource를 풀로 돌려보내는 코루틴
    private System.Collections.IEnumerator ReturnToPoolAfterDelay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 다시 풀로 돌아가기 전에 비활성화
        source.gameObject.SetActive(false);
        source.clip = null; // 클립 초기화
        sfxSourcePool.Enqueue(source);
    }
}