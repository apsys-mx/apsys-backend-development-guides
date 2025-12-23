using AutoFixture;
using AutoFixture.AutoMoq;

namespace {ProjectName}.application.tests;

public class ApplicationTestBase
{
    protected IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        // Handle circular references
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }
}
