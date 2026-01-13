using UnityEngine;

public class PasswordChecker : IValidationRule<string>
{
    private readonly int _minLength;
    private readonly int _maxLength;

    public PasswordChecker(int minLength, int maxLength)
    {
        _minLength = minLength;
        _maxLength = maxLength;
    }

    public ValidationResult Validate(string value)
    {
        if (string.IsNullOrEmpty(value))
            return ValidationResult.Fail("비밀번호를 입력해주세요.");

        if (value.Length < _minLength || value.Length > _maxLength)
            return ValidationResult.Fail($"비밀번호는 {_minLength}자 이상, {_maxLength}자 이하로 설정해주세요.");

        return ValidationResult.Success();
    }
}
