using Novolis.Registry.Abstractions;
using Novolis.Registry.Primitives;

namespace Novolis.Registry.Unit;

internal sealed class TestRegistrySource : IRegistrySource
{
    private readonly RegistryDocument _document;

    public TestRegistrySource(RegistryDocument document)
    {
        _document = document;
    }

    public ValueTask<RegistryDocument> LoadAsync(
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_document);
}
