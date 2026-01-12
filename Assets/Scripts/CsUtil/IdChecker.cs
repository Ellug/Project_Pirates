using System.Text.RegularExpressions;
using UnityEngine;

public class IdChecker : IValidationRule<string>
{
    public ValidationResult Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Fail("이메일을 입력해주세요.");

        if (!Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return ValidationResult.Fail("이메일 형식이 올바르지 않습니다.");

        return ValidationResult.Success();
    }
}
