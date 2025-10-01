using System.Text;
using DotNet.Testcontainers.Builders;
using Humanizer;
using OpenFga.Sdk.Model;
using Org.BouncyCastle.Crypto.Modes;
using static Bullseye.Targets;

// docker run -v $(pwd):/workspace -w /workspace openfga/cli model transform --file ./tests/fga-model.fga --output-format json > ./tests/fga-model.json
Target(
    "transform-fga",
    static async () =>
    {
        Console.WriteLine(
            "Building FGA JSON from FGA model; working directory: "
                + Directory.GetCurrentDirectory()
        );

        using var stdoutStream = new MemoryStream();
        using var stderrStream = new MemoryStream();

        var container = new ContainerBuilder()
            .WithImage("openfga/cli")
            .WithWorkingDirectory("/workspace")
            .WithBindMount(Directory.GetCurrentDirectory(), "/workspace")
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(stdoutStream, stderrStream))
            .WithCommand(
                "model",
                "transform",
                "--file",
                "./tests/fga-model.fga",
                "--output-format",
                "json"
            )
            .Build();

        try
        {
            await container.StartAsync();

            // Add a delay to ensure the container has time to flush its output
            await Task.Delay(100);

            stdoutStream.Position = 0;
            stderrStream.Position = 0;
            using var stdoutReader = new StreamReader(stdoutStream);
            using var stderrReader = new StreamReader(stderrStream);
            var stdout = await stdoutReader.ReadToEndAsync();
            var stderr = await stderrReader.ReadToEndAsync();

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                Console.WriteLine("[ERROR] Container stderr: " + stderr);
                return;
            }
            else if (string.IsNullOrWhiteSpace(stdout))
            {
                Console.WriteLine("[ERROR] No output from container");
                return;
            }

            var outputFilePath = Path.Combine("tests", "fga-model.json");
            await File.WriteAllTextAsync(outputFilePath, stdout);

            Console.WriteLine("Container output written to: " + outputFilePath);
        }
        finally
        {
            await container.StopAsync();
            await container.DisposeAsync();
        }
    }
);

// Perform the transform of the model to C# by reading the JSON file.
Target(
    "create-model",
    static () =>
    {
        var json = File.ReadAllText(Path.Combine("tests", "fga-model.json"));

        // Read the type definitions from the JSON
        var model = WriteAuthorizationModelRequest.FromJson(json);
        var types = model.TypeDefinitions;

        var buffer = new StringBuilder();
        buffer.AppendLine("// This file is auto-generated from fga-model.fga");
        buffer.AppendLine("#pragma warning disable SA1600 // Auto-generated code");

        // For each type, we generate a snippet of C# code by reading the relations
        // and creating properties for each relation.
        foreach (var type in types)
        {
            if (type.Relations == null)
            {
                buffer.AppendLine($"public sealed record {type.Type.Dehumanize()} : IAccessor;\n");
                continue;
            }

            buffer.AppendLine($"public sealed record {type.Type.Dehumanize()}(");

            var actionBuffer = new StringBuilder();
            var actionCount = 0;

            foreach (var relation in type.Relations ?? [])
            {
                if (relation.Value.This != null)
                {
                    // This is a direct assignment relation
                    buffer.AppendLine($"    string {relation.Key.Dehumanize()},");
                }
                else
                {
                    actionCount++;

                    if (actionBuffer.Length == 0)
                    {
                        actionBuffer.Append("    (");
                    }

                    actionBuffer.Append($"string {relation.Key.Dehumanize()}, ");
                }
            }

            // Remove the last comma
            if (type.Relations?.Count > 0)
            {
                buffer.Remove(buffer.Length - 1, 1);
            }

            if (actionBuffer.Length > 0)
            {
                if (actionCount > 1)
                {
                    // We can build a tuple here for multiple `Perform`
                    actionBuffer.Remove(actionBuffer.Length - 2, 2);
                    actionBuffer.Append(") Perform");
                }
                else
                {
                    // If there's only one action, remove the opening parenthesis and space
                    actionBuffer.Remove(0, 5);
                    actionBuffer.Insert(0, "    ");
                    actionBuffer.Remove(actionBuffer.Length - 2, 2);
                }

                buffer.AppendLine();
                buffer.Append(actionBuffer.ToString());
            }
            else
            {
                buffer.Remove(buffer.Length - 1, 1);
            }

            var objectType = type.Type switch
            {
                // Use convention to determine type of object
                var t when t.EndsWith("users") => "IResource, IAccessor",
                var t when t.EndsWith("org") => "IResource, IAccessor",
                var t when t.EndsWith("group") => "IResource, IAccessor",
                var t when t.EndsWith("team") => "IResource, IAccessor",
                _ => "IResource",
            };

            buffer.AppendLine(
                $"""

                ) : {objectType};

                """
            );
        }

        buffer.AppendLine();
        buffer.AppendLine("// Conditions which can be used in the model.");
        buffer.AppendLine("public sealed record Conditions");
        buffer.AppendLine("{");

        var contextBuffer = new StringBuilder();

        // Now build conditions
        foreach (var (name, condition) in model.Conditions ?? [])
        {
            AddConditionToBuffer(condition, name, buffer, contextBuffer);
        }

        // Add the context section
        buffer.AppendLine("    public sealed record ReadContext");
        buffer.AppendLine("    {");
        buffer.Append(contextBuffer.ToString());
        buffer.AppendLine("    }");

        buffer.AppendLine("}");

        buffer.AppendLine("#pragma warning restore SA1600 // Auto-generated code");

        var code = buffer.ToString();

        // Visual confirmation
        Console.WriteLine(code);

        // Write to the tests/Models/PermissionScopes.g.cs file
        File.WriteAllText(Path.Combine("tests", "Models", "PermissionScopes.g.cs"), code);

        // Function used above to build a standalone condition.
        void AddConditionToBuffer(
            Condition condition,
            string name,
            StringBuilder buffer,
            StringBuilder contextBuffer
        )
        {
            var parameterDeclarations = new StringBuilder();
            var parameterConversions = new StringBuilder();

            var contextDeclarations = new StringBuilder();
            var contextAssignments = new StringBuilder();

            var index = 0;
            foreach (var param in condition.Parameters ?? [])
            {
                var variableName = param.Key.Replace("_init", string.Empty).Camelize();
                var contextVariableName = param.Key.Replace("_provided", string.Empty).Camelize();

                var conversionTarget = param.Key.EndsWith("_provided")
                    ? contextVariableName
                    : variableName;

                var (type, conversion) = param.Value.TypeName switch
                {
                    TypeName.TIMESTAMP => (
                        "DateTime",
                        $"{conversionTarget}.ToString(\"yyyy-MM-dd'T'HH:mm:ss.fffZ\")"
                    ),
                    TypeName.INT => ("int", conversionTarget),
                    TypeName.TypeName_BOOL => (
                        "bool",
                        $"Convert.ToString({conversionTarget}).ToLowerInvariant()"
                    ),
                    TypeName.DURATION => ("TimeSpan", $"$\"{{{conversionTarget}.TotalSeconds}}s\""),
                    _ => ("string", $"Convert.ToString({conversionTarget})"),
                };

                if (param.Key.EndsWith("_provided"))
                {
                    contextDeclarations.AppendLine($"{type} {contextVariableName},");
                    contextAssignments.AppendLine($"{param.Key} = {conversion},");

                    continue; // We only model the _init parameters
                }

                var declarationPadding = index == 0 ? string.Empty : new string(' ', 8);
                var conversionPadding = index == 0 ? string.Empty : new string(' ', 16);

                parameterDeclarations.AppendLine($"{declarationPadding}{type} {variableName},");
                parameterConversions.AppendLine($"{conversionPadding}{param.Key} = {conversion},");
                index++;
            }

            var codeBlock = $$"""
                public OpenFga.Sdk.Model.RelationshipCondition For{{name.Pascalize()}}(
                    {{parameterDeclarations.Remove(parameterDeclarations.Length - 2, 2)}}
                )
                    => new() {
                        Name = "{{name}}",
                        Context = new
                        {
                            {{parameterConversions.Remove(parameterConversions.Length - 2, 2)}}
                        }
                    };
            """;

            buffer.AppendLine(codeBlock);

            contextBuffer.AppendLine(
                $$"""
                        public object {{name.Pascalize()}}Context(
                            {{contextDeclarations.Remove(contextDeclarations.Length - 2, 2)}}
                        )
                            => new
                            {
                                {{contextAssignments.Remove(contextAssignments.Length - 2, 2)}}
                            };
                """
            );
        }
    }
);

Target("default", dependsOn: ["transform-fga", "create-model"]);

await RunTargetsAndExitAsync(args);

/* Example condition
public sealed record Conditions
{
    public static RelationshipCondition ForActiveTrial(
        DateTime trialStart,
        TimeSpan trialDuration
    )
        => new() {
            Name = "active_trial",
            Context = new
            {
                trial_start = trialStart.ToString("o"),
                trial_duration = $"{trialDuration.TotalSeconds}s",
            }
        };
}
*/
