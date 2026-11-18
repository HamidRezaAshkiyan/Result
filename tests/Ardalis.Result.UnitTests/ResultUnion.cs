#if NET6_0_OR_GREATER
using FluentAssertions;
using System.Linq;
using Xunit;

namespace Ardalis.Result.UnitTests;

public class ResultUnion
{
    [Fact]
    public void ResultOfTHasUnionAttribute()
    {
        var attributes = typeof(Result<>).GetCustomAttributes(inherit: false);

        attributes.Select(a => a.GetType().FullName)
            .Should().Contain("System.Runtime.CompilerServices.UnionAttribute");
    }

    [Fact]
    public void NonGenericResultDoesNotInheritUnionAttribute()
    {
        var attributes = typeof(Result).GetCustomAttributes(inherit: true);

        attributes.Select(a => a.GetType().FullName)
            .Should().NotContain("System.Runtime.CompilerServices.UnionAttribute");
    }

    [Fact]
    public void UnionMembersCreateFromValueCreatesSuccessResult()
    {
        var result = Result<string>.IUnionMembers.Create("hello");

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(ResultStatus.Ok);
        result.Value.Should().Be("hello");
        ((Result<string>.IUnionMembers)result).Value.Should().Be("hello");
    }

    [Fact]
    public void UnionMembersCreateFromResultPreservesFailureDetails()
    {
        var failure = Result.Error(new ErrorList(["err1", "err2"], "corr-123"));

        var result = Result<string>.IUnionMembers.Create(failure);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().BeEquivalentTo(["err1", "err2"]);
        result.CorrelationId.Should().Be("corr-123");
    }

    [Fact]
    public void UnionMembersValueReturnsValueWhenCreated()
    {
        var result = Result<int>.Created(42, "/items/42");

        ((Result<int>.IUnionMembers)result).Value.Should().Be(42);
    }

    [Fact]
    public void UnionMembersValueReturnsResultWithFailureStateWhenNotSuccess()
    {
        var validationError = new ValidationError("Name", "Required");
        var result = Result<int>.Invalid(validationError);

        var unionValue = ((Result<int>.IUnionMembers)result).Value;

        var failure = unionValue.Should().BeOfType<Result>().Subject;
        failure.Status.Should().Be(ResultStatus.Invalid);
        failure.ValidationErrors.Should().ContainSingle().Which.Should().Be(validationError);
    }

    [Theory]
    [InlineData(ResultStatus.Error)]
    [InlineData(ResultStatus.Forbidden)]
    [InlineData(ResultStatus.Unauthorized)]
    [InlineData(ResultStatus.Invalid)]
    [InlineData(ResultStatus.NotFound)]
    [InlineData(ResultStatus.Conflict)]
    [InlineData(ResultStatus.CriticalError)]
    [InlineData(ResultStatus.Unavailable)]
    public void UnionMembersValueReturnsResultForAllFailureStatuses(ResultStatus status)
    {
        Result<string> result = status switch
        {
            ResultStatus.Error => Result<string>.Error("error"),
            ResultStatus.Forbidden => Result<string>.Forbidden("forbidden"),
            ResultStatus.Unauthorized => Result<string>.Unauthorized("unauthorized"),
            ResultStatus.Invalid => Result<string>.Invalid(new ValidationError("field", "invalid")),
            ResultStatus.NotFound => Result<string>.NotFound("not found"),
            ResultStatus.Conflict => Result<string>.Conflict("conflict"),
            ResultStatus.CriticalError => Result<string>.CriticalError("critical"),
            ResultStatus.Unavailable => Result<string>.Unavailable("unavailable"),
            _ => throw new System.ArgumentOutOfRangeException(nameof(status))
        };

        var unionValue = ((Result<string>.IUnionMembers)result).Value;

        var failure = unionValue.Should().BeOfType<Result>().Subject;
        failure.Status.Should().Be(status);
        failure.IsSuccess.Should().BeFalse();
    }
}
#endif
