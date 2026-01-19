using UnityEngine;

// ToonyVoices 스크립트 내부에 추가하거나 별도 static 클래스로 분리합니다.
public static class KoreanUtil
{
    private const int BASE_CODE = 0xAC00; // '가'

    // 초성, 중성, 종성 배열 (실제 코드에서는 더 긴 배열이 필요함)
    private static readonly char[] CHOSUNG = { 'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ' };
    private static readonly char[] JUNGSUNG = { 'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ', 'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ' };
    private static readonly char[] JONGSUNG = { '\0', 'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ', 'ㄻ', 'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ' };

    // 반환 구조체 (각각의 자모와 매핑된 영어 키를 담음)
    public struct JamoSplitResult
    {
        public char Chosung { get; set; }
        public char Jungsung { get; set; }
        public char Jongsung { get; set; }

        public bool IsValid() => Chosung != '\0';
    }

    public static JamoSplitResult SplitSyllable(char c)
    {
        // 1. 한글 완성형 코드 범위인지 확인
        if (c < BASE_CODE || c > 0xD7A3)
        {
            return new JamoSplitResult(); // 한글이 아님
        }

        // 2. 유니코드 공식에 따라 인덱스 계산
        int uniVal = c - BASE_CODE;
        int choIdx = uniVal / 588;
        int jungIdx = (uniVal % 588) / 28;
        int jongIdx = uniVal % 28; // 0이면 종성 없음 ('\0'에 매핑)

        return new JamoSplitResult
        {
            Chosung = CHOSUNG[choIdx],
            Jungsung = JUNGSUNG[jungIdx],
            Jongsung = JONGSUNG[jongIdx]
        };
    }
}