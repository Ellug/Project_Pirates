using UnityEngine;

public class PasswordConfirmChecker : IValidationRule<(string pw, string confirm)>
{
    public ValidationResult Validate((string pw, string confirm) value)
    {
        if (value.pw != value.confirm)
            return ValidationResult.Fail("비밀번호가 일치하지 않습니다.");

        return ValidationResult.Success();
    }
}
