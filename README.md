# .NET + OpenFGA

This sandbox contains examples of using OpenFGA with the .NET SDK.

The raw API is string based and error prone.

This sample has a fluent API built on top of it to make it a bit easier to use (maybe?).

## Running

```shell
docker compose up
dotnet test

# If you edit the model:
dotnet run --project targets
```

The last command will rebuild the model JSON which is what the runtime actually loads.
