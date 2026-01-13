using System.Collections.Generic;

public struct ValidationResult
{
    public bool IsValid;
    public string Message;

    public static ValidationResult Success(string msg = "")
    => new ValidationResult { IsValid = true, Message = msg };

    public static ValidationResult Fail(string msg)
    => new ValidationResult { IsValid = false, Message = msg };
}

public interface IValidationRule<T>
{
    ValidationResult Validate(T value);
}

public class ExceptionChecker<T>
{
    private readonly List<IValidationRule<T>> _rules = new();

    public ExceptionChecker<T> AddRule(IValidationRule<T> rule)
    {
        _rules.Add(rule);
        return this;
    }

    public ValidationResult Validate(T value)
    {
        foreach (var rule in _rules)
        {
            var result = rule.Validate(value);
            if (!result.IsValid)
                return result;
        }
        return ValidationResult.Success();
    }
}
