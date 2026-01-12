using Firebase;
using Firebase.Auth;

public class FirebaseAuthChecker
{
    public static ValidationResult Convert(FirebaseException ex)
    {
        if (ex == null)
            return ValidationResult.Fail("알 수 없는 오류가 발생했습니다.");

        AuthError code = (AuthError)ex.ErrorCode;

        return code switch
        {
            AuthError.InvalidEmail =>
                ValidationResult.Fail("이메일 형식이 올바르지 않습니다."),

            AuthError.EmailAlreadyInUse =>
                ValidationResult.Fail("이미 사용 중인 이메일입니다."),

            AuthError.WeakPassword =>
                ValidationResult.Fail("비밀번호가 너무 약합니다."),

            AuthError.WrongPassword or AuthError.UserNotFound =>
                ValidationResult.Fail("아이디 또는 비밀번호가 틀렸습니다."),

            _ =>
                ValidationResult.Fail("회원가입에 실패했습니다.")
        };
    }
}
