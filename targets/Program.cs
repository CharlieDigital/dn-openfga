using DotNet.Testcontainers.Builders;
using static Bullseye.Targets;

// docker run -v $(pwd):/workspace -w /workspace openfga/cli model transform --file ./tests/fga-model.fga --output-format json > ./tests/fga-model.json
Target(
    "default",
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

await RunTargetsAndExitAsync(args);
