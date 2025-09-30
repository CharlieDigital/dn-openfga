using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Model;

namespace tests;

public class FormsPermissionsFixture : IDisposable
{
    private readonly Lock _lock = new();
    private OpenFgaClient? _client;
    private string _modelId = string.Empty;

    public FormsPermissionsFixture() { }

    /// <summary>
    /// Constructs a singleton-per-test collection client with the store ID.
    /// </summary>
    public OpenFgaClient GetClient(string storeName)
    {
        if (_client != null && !string.IsNullOrEmpty(_modelId))
        {
            return _client;
        }

        lock (_lock)
        {
            if (_client != null && !string.IsNullOrEmpty(_modelId))
            {
                return _client;
            }

            // Use the initial client to create the store
            using var initialClient = new OpenFgaClient(
                new ClientConfiguration { ApiUrl = "http://localhost:8080" }
            );

            // Create the store
            var storeResponse = initialClient
                .CreateStore(new ClientCreateStoreRequest { Name = storeName })
                .Result;

            _client = new OpenFgaClient(
                new ClientConfiguration
                {
                    ApiUrl = "http://localhost:8080",
                    StoreId = storeResponse.Id,
                }
            );

            // Create a model
            var modelJson = File.ReadAllText(
                Path.Combine(Directory.GetCurrentDirectory(), "fga-model.json")
            );

            var modelRequest = WriteAuthorizationModelRequest.FromJson(modelJson);

            // Writing it using a client initiated with the store ID will link
            // the model to the store.
            var writeResponse = _client
                .WriteAuthorizationModel(
                    new ClientWriteAuthorizationModelRequest
                    {
                        SchemaVersion = modelRequest.SchemaVersion,
                        TypeDefinitions = modelRequest.TypeDefinitions,
                    }
                )
                .Result;

            _modelId = writeResponse.AuthorizationModelId;
        }

        return _client;
    }

    public void Dispose()
    {
        // Cleanup the store.
        if (_client != null)
        {
            _client.DeleteStore().Wait();
            _client.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}

[CollectionDefinition("PermissionsCollection")]
public class FormsPermissionsCollection : ICollectionFixture<FormsPermissionsFixture> { }
