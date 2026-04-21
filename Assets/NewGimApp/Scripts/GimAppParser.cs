using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace NewGimApp
{
    /// <summary>
    /// Standalone parser core for a new GIM software.
    /// This first version parses project/cbm hierarchy and BASEFAMILY properties.
    /// </summary>
    public class GimAppParser
    {
        private string SevenZipExePath
        {
            get { return Path.Combine(Application.streamingAssetsPath, "7-Zip/7z.exe"); }
        }

        public async Task<GimDocument> ParseAsync(string gimPath)
        {
            if (string.IsNullOrEmpty(gimPath) || !File.Exists(gimPath))
            {
                throw new FileNotFoundException("GIM file not found", gimPath);
            }

            var extractedDir = await EnsureExtractedAsync(gimPath);
            var cbmDir = Path.Combine(extractedDir, "CBM");
            var projectPath = Path.Combine(cbmDir, "project.cbm");

            if (!File.Exists(projectPath))
            {
                throw new FileNotFoundException("project.cbm not found", projectPath);
            }

            var projectKv = ParseKeyValueFile(projectPath);
            if (!projectKv.ContainsKey("SUBSYSTEM"))
            {
                throw new InvalidDataException("project.cbm missing SUBSYSTEM field");
            }

            var rootCbm = Path.Combine(cbmDir, projectKv["SUBSYSTEM"]);
            var root = ParseCbmNode(rootCbm, cbmDir);

            return new GimDocument
            {
                GimPath = gimPath,
                ExtractedDirectory = extractedDir,
                Root = root
            };
        }

        private async Task<string> EnsureExtractedAsync(string gimPath)
        {
            var targetDir = Path.Combine(Application.temporaryCachePath, "NewGimApp", Path.GetFileNameWithoutExtension(gimPath));
            var cbmDir = Path.Combine(targetDir, "CBM");
            if (Directory.Exists(cbmDir))
            {
                return targetDir;
            }

            Directory.CreateDirectory(targetDir);
            await Task.Run(() =>
            {
                var args = string.Format("x \"{0}\" -o\"{1}\" -r -y", gimPath, targetDir);
                var process = new Process();
                process.StartInfo = new ProcessStartInfo(SevenZipExePath, args)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                process.Start();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException("7z extraction failed with exit code " + process.ExitCode);
                }
            });

            return targetDir;
        }

        private GimNode ParseCbmNode(string cbmPath, string cbmDir)
        {
            var kv = ParseKeyValueFile(cbmPath);
            var node = new GimNode
            {
                Name = BuildNodeName(kv, Path.GetFileNameWithoutExtension(cbmPath)),
                SourceFile = cbmPath,
            };

            if (kv.ContainsKey("BASEFAMILY"))
            {
                var famPath = Path.Combine(cbmDir, kv["BASEFAMILY"]);
                if (File.Exists(famPath))
                {
                    node.Properties = ParseKeyValueFile(famPath);
                }
            }

            if (kv.ContainsKey("SUBSYSTEMS.NUM"))
            {
                int subsystemCount;
                if (int.TryParse(kv["SUBSYSTEMS.NUM"], out subsystemCount))
                {
                    for (int i = 0; i < subsystemCount; i++)
                    {
                        var key = "SUBSYSTEM" + i;
                        if (!kv.ContainsKey(key))
                        {
                            continue;
                        }

                        var childPath = Path.Combine(cbmDir, kv[key]);
                        if (File.Exists(childPath))
                        {
                            node.Children.Add(ParseCbmNode(childPath, cbmDir));
                        }
                    }
                }
            }

            return node;
        }

        private string BuildNodeName(Dictionary<string, string> kv, string defaultName)
        {
            var names = new List<string>();
            string value;
            if (kv.TryGetValue("ENTITYNAME", out value) && !string.IsNullOrEmpty(value))
            {
                names.Add(value);
            }
            if (kv.TryGetValue("SYSCLASSIFYNAME", out value) && !string.IsNullOrEmpty(value))
            {
                names.Add(value);
            }
            if (names.Count == 0)
            {
                names.Add(defaultName);
            }
            return string.Join("-", names.ToArray());
        }

        private Dictionary<string, string> ParseKeyValueFile(string filePath)
        {
            var map = new Dictionary<string, string>();
            var lines = File.ReadAllLines(filePath);
            foreach (var rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                var segments = rawLine.Split(new[] { '=' }, 2);
                if (segments.Length < 2)
                {
                    continue;
                }

                var key = segments[0].Trim();
                var value = segments[1].Trim();
                if (map.ContainsKey(key))
                {
                    map[key] = value;
                }
                else
                {
                    map.Add(key, value);
                }
            }
            return map;
        }
    }
}
