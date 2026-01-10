using System.Text.RegularExpressions;

// 얘는 닉네임 검증만 한다. 순수 C# 클래스
public class NicknameChecker
{
    private int _minLength;
    private int _maxLength;

    public NicknameChecker(int minLength, int maxLength)
    {
        _maxLength = maxLength;
        _minLength = minLength;
    }

    public bool TryConfirmCurrentInput(string nickName, out string resultMsg)
    {
        nickName = nickName.Trim();
        resultMsg = string.Empty;

        if (string.IsNullOrWhiteSpace(nickName))
            resultMsg = "닉네임을 입력해주세요.";

        else if (nickName.Contains(" "))
            resultMsg = "닉네임에는 공백을 사용할 수 없습니다.";

        else if (!Regex.IsMatch(nickName, "^[a-zA-Z0-9가-힣]+$"))
            resultMsg = "닉네임은 한글, 영문, 숫자만 사용할 수 있습니다.";

        else if (nickName.Length < _minLength)
            resultMsg = $"닉네임은 최소 {_minLength}자 이상이어야 합니다.";

        else if (nickName.Length > _maxLength)
            resultMsg = $"닉네임은 최대 {_maxLength}자까지 가능합니다.";

        else // 위 오중나생문을 통과했다면 성공
        {
            resultMsg = $"닉네임 검증 성공 : {nickName}";
            return true;
        }
        
        return false; 
    }
}
