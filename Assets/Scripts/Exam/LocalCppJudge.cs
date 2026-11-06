using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NOIChaos
{
    public sealed class ExamJudgeResult
    {
        public bool CompileSuccess;
        public int Score;
        public bool[] Passed;
        public string Message;
        public string Output;
    }

    public static class LocalCppJudge
    {
        private static bool compilerResolved;
        private static string compilerPath;

        public static bool IsCompilerAvailable
        {
            get
            {
                ResolveCompiler();
                return !string.IsNullOrEmpty(compilerPath);
            }
        }

        public static string CompilerDescription
        {
            get
            {
                ResolveCompiler();
                return string.IsNullOrEmpty(compilerPath)
                    ? "未找到 C++14 编译器"
                    : compilerPath;
            }
        }

        public static Task<ExamJudgeResult> JudgeAsync(
            ExamProblemDefinition problem,
            ProblemRunState runState,
            string code,
            bool sampleOnly)
        {
            return Task.Run(() => Judge(problem, runState, code, sampleOnly));
        }

        private static ExamJudgeResult Judge(
            ExamProblemDefinition problem,
            ProblemRunState runState,
            string code,
            bool sampleOnly)
        {
            ExamJudgeResult result = new ExamJudgeResult
            {
                Passed = Array.Empty<bool>(),
                Score = 0,
                CompileSuccess = false,
                Message = string.Empty,
                Output = string.Empty
            };

            ResolveCompiler();
            if (string.IsNullOrEmpty(compilerPath))
            {
                result.Message = "未找到 C++14 编译器。请安装 Xcode Command Line Tools、clang++ 或 MinGW g++。";
                return result;
            }

            string workingDirectory = Path.Combine(Path.GetTempPath(), "NOIChaosJudge", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workingDirectory);
            string sourcePath = Path.Combine(workingDirectory, "main.cpp");
            string executablePath = Path.Combine(
                workingDirectory,
                Application.platform == RuntimePlatform.WindowsPlayer ? "main.exe" : "main");
            string includeDirectory = Path.Combine(workingDirectory, "include");
            Directory.CreateDirectory(Path.Combine(includeDirectory, "bits"));
            TextAsset compatibilityHeader = Resources.Load<TextAsset>("NOI/Exam/Toolchain/stdcpp");
            string headerText = compatibilityHeader != null
                ? compatibilityHeader.text
                : "#include <iostream>\n#include <vector>\n#include <algorithm>\n#include <queue>\n#include <stack>\n#include <map>\n#include <set>\n#include <string>\n#include <cstdio>\n#include <cstring>\n#include <cmath>\n";
            File.WriteAllText(Path.Combine(includeDirectory, "bits", "stdc++.h"), headerText);
            File.WriteAllText(sourcePath, code, new UTF8Encoding(false));

            try
            {
                ProcessResult compile = RunProcess(
                    compilerPath,
                    $"-std=c++14 -O2 -pipe -I {Quote(includeDirectory)} {Quote(sourcePath)} -o {Quote(executablePath)}",
                    string.Empty,
                    15000);
                if (compile.TimedOut)
                {
                    result.Message = "编译超时。";
                    return result;
                }
                if (compile.ExitCode != 0)
                {
                    result.Message = "Compile Error\n" + Trim(compile.Error + "\n" + compile.Output, 5000);
                    return result;
                }

                result.CompileSuccess = true;
                if (sampleOnly)
                {
                    ProcessResult sample = RunProcess(executablePath, string.Empty, problem.sampleInput, 3000);
                    result.Output = sample.TimedOut ? "运行超时。" : Trim(sample.Output, 5000);
                    result.Message = sample.TimedOut
                        ? "Sample Timeout"
                        : (Normalize(sample.Output) == Normalize(problem.sampleOutput)
                            ? "Sample Passed"
                            : "Sample Failed");
                    return result;
                }

                ExamTestPack testPack = LoadTests(problem.id);
                if (testPack == null || testPack.tests == null)
                {
                    result.Message = "离线测试数据缺失。";
                    return result;
                }

                List<bool> passed = new List<bool>();
                StringBuilder details = new StringBuilder();
                for (int i = 0; i < runState.TestIndices.Length; i++)
                {
                    int testIndex = runState.TestIndices[i];
                    ExamTestCase testCase = FindTest(testPack, testIndex);
                    if (testCase == null)
                    {
                        passed.Add(false);
                        details.AppendLine($"#{testIndex + 1} MISSING");
                        continue;
                    }

                    ProcessResult run = RunProcess(executablePath, string.Empty, testCase.input, 3000);
                    bool ok = !run.TimedOut && run.ExitCode == 0
                        && Normalize(run.Output) == Normalize(testCase.output);
                    passed.Add(ok);
                    details.AppendLine($"#{testIndex + 1} {(ok ? "AC" : (run.TimedOut ? "TLE" : "WA"))}");
                }

                result.Passed = passed.ToArray();
                for (int i = 0; i < result.Passed.Length; i++)
                {
                    if (result.Passed[i])
                    {
                        result.Score += OfflineJudge.PointsPerTest;
                    }
                }
                result.Message = result.Score >= 100 ? "Accepted" : "Partial Score";
                result.Output = details.ToString().TrimEnd();
                return result;
            }
            catch (Exception exception)
            {
                result.Message = "Judge Error\n" + exception.Message;
                return result;
            }
            finally
            {
                try
                {
                    Directory.Delete(workingDirectory, true);
                }
                catch
                {
                    // Temporary files can be cleaned by the OS.
                }
            }
        }

        private static ExamTestPack LoadTests(string problemId)
        {
            TextAsset asset = Resources.Load<TextAsset>("NOI/Exam/Tests/" + problemId);
            return asset == null ? null : JsonUtility.FromJson<ExamTestPack>(asset.text);
        }

        private static ExamTestCase FindTest(ExamTestPack pack, int index)
        {
            for (int i = 0; i < pack.tests.Length; i++)
            {
                if (pack.tests[i].index == index)
                {
                    return pack.tests[i];
                }
            }
            return null;
        }

        private static ProcessResult RunProcess(string executable, string arguments, string input, int timeoutMs)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                if (!string.IsNullOrEmpty(input))
                {
                    process.StandardInput.Write(input);
                }
                process.StandardInput.Close();
                bool exited = process.WaitForExit(timeoutMs);
                if (!exited)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // The process may have exited between the timeout and kill call.
                    }
                }
                string output = outputTask.GetAwaiter().GetResult();
                string error = errorTask.GetAwaiter().GetResult();
                return new ProcessResult
                {
                    TimedOut = !exited,
                    ExitCode = exited ? process.ExitCode : -1,
                    Output = output,
                    Error = error
                };
            }
        }

        private static void ResolveCompiler()
        {
            if (compilerResolved)
            {
                return;
            }
            compilerResolved = true;
            string[] candidates;
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            candidates = new[]
            {
                "/usr/bin/clang++",
                "/Library/Developer/CommandLineTools/usr/bin/clang++"
            };
#else
            List<string> found = new List<string>();
            string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            string[] names = Application.platform == RuntimePlatform.WindowsPlayer
                ? new[] { "g++.exe", "clang++.exe" }
                : new[] { "g++", "clang++" };
            string[] directories = path.Split(Path.PathSeparator);
            for (int i = 0; i < directories.Length; i++)
            {
                for (int j = 0; j < names.Length; j++)
                {
                    string candidate = Path.Combine(directories[i], names[j]);
                    if (File.Exists(candidate))
                    {
                        found.Add(candidate);
                    }
                }
            }
            candidates = found.ToArray();
#endif
            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    compilerPath = candidates[i];
                    return;
                }
            }
            compilerPath = string.Empty;
        }

        private static string Normalize(string text)
        {
            string[] lines = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            List<string> normalized = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                normalized.Add(lines[i].TrimEnd());
            }
            while (normalized.Count > 0 && normalized[normalized.Count - 1].Length == 0)
            {
                normalized.RemoveAt(normalized.Count - 1);
            }
            return string.Join("\n", normalized);
        }

        private static string Trim(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text ?? string.Empty;
            }
            return text.Substring(0, maxLength) + "\n...";
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private sealed class ProcessResult
        {
            public bool TimedOut;
            public int ExitCode;
            public string Output;
            public string Error;
        }
    }
}
