public struct SignUpInput
{
    public string Email;
    public string Password;
    public string PasswordConfirm;
    public string Nickname;
    public bool IsNicknameChecked;

    public int MinPw;
    public int MaxPw;
    public int MinNick;
    public int MaxNick;
}

public class SignUpInputChecker : IValidationRule<SignUpInput>
{
    public ValidationResult Validate(SignUpInput input)
    {
        // 이메일
        var email = new ExceptionChecker<string>()
            .AddRule(new IdChecker())
            .Validate(input.Email);

        if (!email.IsValid)
            return email;

        // 비밀번호
        var pw = new ExceptionChecker<string>()
            .AddRule(new PasswordChecker(input.MinPw, input.MaxPw))
            .Validate(input.Password);

        if (!pw.IsValid)
            return pw;

        // 비밀번호 확인
        var confirm = new ExceptionChecker<(string, string)>()
            .AddRule(new PasswordConfirmChecker())
            .Validate((input.Password, input.PasswordConfirm));

        if (!confirm.IsValid)
            return confirm;

        // 닉네임
        var nick = new ExceptionChecker<string>()
            .AddRule(new NicknameChecker(input.MinNick, input.MaxNick))
            .Validate(input.Nickname);

        if (!nick.IsValid)
            return nick;

        // 중복 체크
        if (!input.IsNicknameChecked)
            return ValidationResult.Fail("닉네임 중복 확인을 먼저 해주세요.");

        return ValidationResult.Success();
    }
}