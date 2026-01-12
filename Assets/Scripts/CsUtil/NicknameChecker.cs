using System.Text.RegularExpressions;

// 얘는 닉네임 검증만 한다. 순수 C# 클래스
public class NicknameChecker : IValidationRule<string>
{
    private readonly int _minNickname;
    private readonly int _maxNickname;

    public NicknameChecker(int minNickname, int maxNickName)
    {
        _minNickname = minNickname;
        _maxNickname = maxNickName;
    }
    public ValidationResult Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Fail("닉네임을 입력해주세요.");

        if (value.Contains(" "))
            return ValidationResult.Fail("닉네임에는 공백을 사용할 수 없습니다.");

        if (!Regex.IsMatch(value, @"^[a-zA-Z0-9가-힣]+$"))
            return ValidationResult.Fail("닉네임은 한글, 영문, 숫자만 사용할 수 있습니다.");

        if (value.Length < _minNickname || value.Length > _maxNickname)
            return ValidationResult.Fail($"닉네임은 최소 {_minNickname}자 이상, {_maxNickname}자 이하로 설정해주세요.");

        return ValidationResult.Success();
    }
}
