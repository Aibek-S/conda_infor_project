using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using conda_infor_project.models;

namespace conda_infor_project.services
{
    public class ProcessMonitorService
    {
        private const string ScriptFileName = "process_snapshot.py";
        private const int ScriptTimeoutSeconds = 4;

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
            string? scriptPath = ResolveScriptPath();
            if (scriptPath == null)
            {
                return CreateFallbackSnapshot(
                    "Python script was not found. Put it into conda_infor_project/scripts/process_snapshot.py.",
                    GetExpectedScriptPath());
            }

            var errors = new List<string>();
            foreach (PythonRunner runner in PythonRunners)
            {
                ScriptRunResult result = await RunPythonScriptAsync(runner, scriptPath);
                if (result.Success)
                {
                    return ParsePythonSnapshot(result.Stdout, result.Stderr, scriptPath, runner.FileName);
                }

                errors.Add(result.ErrorMessage);
            }

            return CreateFallbackSnapshot(string.Join(" | ", errors), scriptPath);
        }

        private static string? ResolveScriptPath()
        {
            foreach (string candidate in GetScriptCandidates())
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string GetExpectedScriptPath()
        {
            return GetScriptCandidates().First();
        }

        private static IEnumerable<string> GetScriptCandidates()
        {
            string baseDirectory = AppContext.BaseDirectory;
            string currentDirectory = Environment.CurrentDirectory;

            string[] candidates =
            {
                Path.Combine(baseDirectory, "scripts", ScriptFileName),
                Path.Combine(currentDirectory, "scripts", ScriptFileName),
                Path.Combine(currentDirectory, "conda_infor_project", "scripts", ScriptFileName),
                Path.Combine(baseDirectory, "..", "..", "..", "scripts", ScriptFileName)
            };

            return candidates
                .Select(path => Path.GetFullPath(path))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static async Task<ScriptRunResult> RunPythonScriptAsync(PythonRunner runner, string scriptPath)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = runner.FileName,
                    Arguments = string.Format(runner.ArgumentsFormat, scriptPath),
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

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(ScriptTimeoutSeconds));
                try
                {
                    await process.WaitForExitAsync(timeout.Token);
                }
                catch (OperationCanceledException)
                {
                    TryKill(process);
                    return ScriptRunResult.Fail($"{runner.FileName}: timeout after {ScriptTimeoutSeconds} seconds.");
                }

                string stdout = await stdoutTask;
                string stderr = await stderrTask;

                if (process.ExitCode != 0)
                {
                    string message = string.IsNullOrWhiteSpace(stderr) ? "no stderr" : stderr.Trim();
                    return ScriptRunResult.Fail($"{runner.FileName}: exit code {process.ExitCode}. {message}");
                }

                return ScriptRunResult.Ok(stdout, stderr);
            }
            catch (Win32Exception ex)
            {
                return ScriptRunResult.Fail($"{runner.FileName}: cannot start Python runner. {ex.Message}");
            }
            catch (Exception ex)
            {
                return ScriptRunResult.Fail($"{runner.FileName}: {ex.Message}");
            }
        }

        private static ActivitySnapshot ParsePythonSnapshot(
            string stdout,
            string stderr,
            string scriptPath,
            string runnerName)
        {
            if (string.IsNullOrWhiteSpace(stdout))
            {
                return CreateFallbackSnapshot("Python script returned empty stdout.", scriptPath);
            }

            try
            {
                PythonActivitySnapshot? pythonSnapshot = JsonSerializer.Deserialize<PythonActivitySnapshot>(stdout, JsonOptions);
                if (pythonSnapshot?.Processes == null)
                {
                    return CreateFallbackSnapshot("Python JSON must contain processes array.", scriptPath);
                }

                List<string> processes = NormalizeProcesses(pythonSnapshot.Processes);
                if (processes.Count == 0)
                {
                    return CreateFallbackSnapshot("Python JSON processes array is empty.", scriptPath);
                }

                string debug = ExtractDebugMessage(pythonSnapshot.Debug);
                if (string.IsNullOrWhiteSpace(debug))
                {
                    debug = stderr.Trim();
                }

                return new ActivitySnapshot
                {
                    ActiveWindow = pythonSnapshot.ActiveWindow ?? string.Empty,
                    Processes = processes,
                    IsFallback = false,
                    DebugSource = $"python:{runnerName}",
                    ScriptPath = scriptPath,
                    DebugMessage = debug
                };
            }
            catch (Exception ex)
            {
                return CreateFallbackSnapshot($"Invalid Python JSON: {ex.Message}", scriptPath);
            }
        }

        private static ActivitySnapshot CreateFallbackSnapshot(string debugMessage, string scriptPath)
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
                ScriptPath = scriptPath,
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
