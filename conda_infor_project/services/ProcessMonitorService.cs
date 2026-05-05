using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using conda_infor_project.models;

namespace conda_infor_project.services
{
    public class ProcessMonitorService
    {
        private const string CollectorExeFileName = "process_snapshot.exe";
        private const string CollectorPyFileName = "process_snapshot.py";
        private const int CollectorTimeoutSeconds = 4;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly PythonRunner[] PythonRunners =
        {
            new PythonRunner("python", "\"{0}\""),
            new PythonRunner("py", "-3 \"{0}\"")
        };

        public async Task<ActivitySnapshot> CaptureSnapshotAsync()
        {
            CollectorFile? collector = ResolveCollectorFile();
            if (collector == null)
            {
                return CreateFallbackSnapshot(
                    "Collector was not found. Build scripts/process_snapshot.exe or add scripts/process_snapshot.py.",
                    GetExpectedCollectorPath());
            }

            List<string> errors = new List<string>();
            if (collector.Kind == CollectorKind.Exe)
            {
                ScriptRunResult exeResult = await RunProcessAsync(collector.Path, string.Empty, "collector-exe");
                if (exeResult.Success)
                {
                    return ParseSnapshot(exeResult.Stdout, exeResult.Stderr, collector.Path, "collector-exe");
                }

                errors.Add(exeResult.ErrorMessage);
                return CreateFallbackSnapshot(string.Join(" | ", errors), collector.Path);
            }

            foreach (PythonRunner runner in PythonRunners)
            {
                ScriptRunResult result = await RunProcessAsync(
                    runner.FileName,
                    string.Format(runner.ArgumentsFormat, collector.Path),
                    $"python:{runner.FileName}");

                if (result.Success)
                {
                    return ParseSnapshot(result.Stdout, result.Stderr, collector.Path, $"python:{runner.FileName}");
                }

                errors.Add(result.ErrorMessage);
            }

            return CreateFallbackSnapshot(string.Join(" | ", errors), collector.Path);
        }

        private static CollectorFile? ResolveCollectorFile()
        {
            foreach (CollectorFile candidate in GetCollectorCandidates())
            {
                if (File.Exists(candidate.Path))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string GetExpectedCollectorPath()
        {
            return GetCollectorCandidates().First().Path;
        }

        private static IEnumerable<CollectorFile> GetCollectorCandidates()
        {
            string baseDirectory = AppContext.BaseDirectory;
            string currentDirectory = Environment.CurrentDirectory;

            string[] roots =
            {
                Path.Combine(baseDirectory, "scripts"),
                Path.Combine(currentDirectory, "scripts"),
                Path.Combine(currentDirectory, "conda_infor_project", "scripts"),
                Path.Combine(baseDirectory, "..", "..", "..", "scripts")
            };

            foreach (string root in roots.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                yield return new CollectorFile(Path.Combine(root, CollectorExeFileName), CollectorKind.Exe);
            }

            foreach (string root in roots.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                yield return new CollectorFile(Path.Combine(root, CollectorPyFileName), CollectorKind.Python);
            }
        }

        private static async Task<ScriptRunResult> RunProcessAsync(string fileName, string arguments, string runnerName)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    CreateNoWindow = true
                };

                process.Start();

                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                Task<string> stderrTask = process.StandardError.ReadToEndAsync();

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(CollectorTimeoutSeconds));
                try
                {
                    await process.WaitForExitAsync(timeout.Token);
                }
                catch (OperationCanceledException)
                {
                    TryKill(process);
                    return ScriptRunResult.Fail($"{runnerName}: timeout after {CollectorTimeoutSeconds} seconds.");
                }

                string stdout = await stdoutTask;
                string stderr = await stderrTask;

                if (process.ExitCode != 0)
                {
                    string message = string.IsNullOrWhiteSpace(stderr) ? "no stderr" : stderr.Trim();
                    return ScriptRunResult.Fail($"{runnerName}: exit code {process.ExitCode}. {message}");
                }

                return ScriptRunResult.Ok(stdout, stderr);
            }
            catch (Win32Exception ex)
            {
                return ScriptRunResult.Fail($"{runnerName}: cannot start process. {ex.Message}");
            }
            catch (Exception ex)
            {
                return ScriptRunResult.Fail($"{runnerName}: {ex.Message}");
            }
        }

        private static ActivitySnapshot ParseSnapshot(
            string stdout,
            string stderr,
            string collectorPath,
            string runnerName)
        {
            if (string.IsNullOrWhiteSpace(stdout))
            {
                return CreateFallbackSnapshot("Collector returned empty stdout.", collectorPath);
            }

            try
            {
                PythonActivitySnapshot? collectorSnapshot = JsonSerializer.Deserialize<PythonActivitySnapshot>(stdout, JsonOptions);
                if (collectorSnapshot?.Processes == null)
                {
                    return CreateFallbackSnapshot("Collector JSON must contain processes array.", collectorPath);
                }

                List<string> processes = NormalizeProcesses(collectorSnapshot.Processes);
                if (processes.Count == 0)
                {
                    return CreateFallbackSnapshot("Collector JSON processes array is empty.", collectorPath);
                }

                string debug = ExtractDebugMessage(collectorSnapshot.Debug);
                if (string.IsNullOrWhiteSpace(debug))
                {
                    debug = stderr.Trim();
                }

                return new ActivitySnapshot
                {
                    ActiveWindow = collectorSnapshot.ActiveWindow ?? string.Empty,
                    Processes = processes,
                    IsFallback = false,
                    DebugSource = runnerName,
                    ScriptPath = collectorPath,
                    DebugMessage = debug
                };
            }
            catch (Exception ex)
            {
                return CreateFallbackSnapshot($"Invalid collector JSON: {ex.Message}", collectorPath);
            }
        }

        private static ActivitySnapshot CreateFallbackSnapshot(string debugMessage, string collectorPath)
        {
            return new ActivitySnapshot
            {
                ActiveWindow = "sample-window.sample",
                Processes = new List<string>
                {
                    "chrome.sample",
                    "notepad.sample",
                    "calculator.sample"
                },
                IsFallback = true,
                DebugSource = "sample",
                ScriptPath = collectorPath,
                DebugMessage = debugMessage
            };
        }

        private static List<string> NormalizeProcesses(IEnumerable<string> processes)
        {
            return processes
                .Select(process => process.Trim())
                .Where(process => !string.IsNullOrWhiteSpace(process))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(process => process)
                .ToList();
        }

        private static string ExtractDebugMessage(JsonElement? debug)
        {
            if (debug == null)
            {
                return string.Empty;
            }

            JsonElement value = debug.Value;
            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? string.Empty;
            }

            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
            {
                return string.Empty;
            }

            return value.GetRawText();
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch
            {
            }
        }

        private enum CollectorKind
        {
            Exe,
            Python
        }

        private sealed record CollectorFile(string Path, CollectorKind Kind);

        private sealed record PythonRunner(string FileName, string ArgumentsFormat);

        private sealed record ScriptRunResult(bool Success, string Stdout, string Stderr, string ErrorMessage)
        {
            public static ScriptRunResult Ok(string stdout, string stderr)
            {
                return new ScriptRunResult(true, stdout, stderr, string.Empty);
            }

            public static ScriptRunResult Fail(string errorMessage)
            {
                return new ScriptRunResult(false, string.Empty, string.Empty, errorMessage);
            }
        }
    }
}
