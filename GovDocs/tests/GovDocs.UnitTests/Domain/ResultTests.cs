using GovDocs.Domain.Common;
using GovDocs.Domain.Products.Errors;
using FluentAssertions;

namespace GovDocs.UnitTests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_ShouldBeSuccess_AndNotFailure()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Failure_ShouldHaveCorrectErrorCode()
    {
        var error = Error.Failure("Test.Error", "Something went wrong");
        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Test.Error");
        result.Error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void ResultT_Success_ShouldCarryValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ResultT_ImplicitFromError_ShouldProduceFailure()
    {
        Result<int> result = ProductErrors.NotFound;

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void ResultT_Value_OnFailure_ShouldThrow()
    {
        var result = Result<int>.Failure(Error.Failure("x", "y"));

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }
}
