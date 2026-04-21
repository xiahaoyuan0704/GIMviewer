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
            var root = ParseCbmNode(rootCbm, extractedDir);

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

        private GimNode ParseCbmNode(string cbmPath, string extractedDir)
        {
            var cbmDir = Path.Combine(extractedDir, "CBM");
            var devDir = Path.Combine(extractedDir, "DEV");
            var phmDir = Path.Combine(extractedDir, "PHM");
            var modDir = Path.Combine(extractedDir, "MOD");
            var kv = ParseKeyValueFile(cbmPath);
            var node = new GimNode
            {
                Name = BuildNodeName(kv, Path.GetFileNameWithoutExtension(cbmPath)),
                NodeType = "CBM",
                SourceFile = cbmPath,
            };

            if (kv.ContainsKey("BASEFAMILY"))
            {
                var famPath = Path.Combine(cbmDir, kv["BASEFAMILY"]);
                if (File.Exists(famPath))
                {
                    node.Properties = ParseFamFile(famPath);
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
                            node.Children.Add(ParseCbmNode(childPath, extractedDir));
                        }
                    }
                }
            }

            if (kv.ContainsKey("OBJECTMODELPOINTER"))
            {
                var devPath = Path.Combine(devDir, kv["OBJECTMODELPOINTER"]);
                if (File.Exists(devPath))
                {
                    node.Children.Add(ParseDevNode(devPath, devDir, phmDir, modDir));
                }
            }

            return node;
        }

        private GimNode ParseDevNode(string devPath, string devDir, string phmDir, string modDir)
        {
            var kv = ParseDevKeyValueFile(devPath);
            var node = new GimNode
            {
                Name = BuildDevName(kv, Path.GetFileNameWithoutExtension(devPath)),
                NodeType = "DEV",
                SourceFile = devPath,
                Properties = new Dictionary<string, string>(kv)
            };

            if (kv.ContainsKey("BASEFAMILYPOINTER"))
            {
                var famPath = Path.Combine(devDir, kv["BASEFAMILYPOINTER"]);
                if (File.Exists(famPath))
                {
                    node.Properties = ParseFamFile(famPath);
                }
            }

            int subDeviceCount;
            if (kv.ContainsKey("SUBDEVICES.NUM") && int.TryParse(kv["SUBDEVICES.NUM"], out subDeviceCount))
            {
                for (int i = 0; i < subDeviceCount; i++)
                {
                    var key = "SUBDEVICE" + i;
                    if (!kv.ContainsKey(key))
                    {
                        continue;
                    }

                    var subDevPath = Path.Combine(devDir, kv[key]);
                    if (File.Exists(subDevPath))
                    {
                        node.Children.Add(ParseDevNode(subDevPath, devDir, phmDir, modDir));
                    }
                }
            }

            int solidCount;
            if (kv.ContainsKey("SOLIDMODELS.NUM") && int.TryParse(kv["SOLIDMODELS.NUM"], out solidCount))
            {
                for (int i = 0; i < solidCount; i++)
                {
                    var key = "SOLIDMODEL" + i;
                    if (!kv.ContainsKey(key))
                    {
                        continue;
                    }

                    var solidModel = kv[key];
                    node.ModelFiles.Add(solidModel);
                    var ext = Path.GetExtension(solidModel).ToUpperInvariant();
                    var solidNode = new GimNode
                    {
                        Name = "SolidModel-" + i + ":" + Path.GetFileName(solidModel),
                        NodeType = ext.Trim('.'),
                        SourceFile = solidModel
                    };
                    solidNode.Properties.Add("SOURCE_MODEL", solidModel);

                    if (ext == ".PHM")
                    {
                        var phmPath = Path.Combine(phmDir, solidModel);
                        if (File.Exists(phmPath))
                        {
                            solidNode.Properties["PHM_PATH"] = phmPath;
                        }
                    }
                    else if (ext == ".MOD" || ext == ".STL")
                    {
                        solidNode.Properties["MODEL_PATH"] = Path.Combine(modDir, solidModel);
                    }

                    node.Children.Add(solidNode);
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

                var segments = rawLine.Split('=');
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

        private Dictionary<string, string> ParseFamFile(string famPath)
        {
            var map = new Dictionary<string, string>();
            var lines = File.ReadAllLines(famPath);
            for (int i = 0; i < lines.Length; i++)
            {
                var rawLine = lines[i];
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                var segments = rawLine.Split('=');
                if (segments.Length >= 3)
                {
                    var key = segments[1].Trim();
                    var value = segments[2].Trim();
                    if (!map.ContainsKey(key))
                    {
                        map.Add(key, value);
                    }
                }
                else if (segments.Length >= 2)
                {
                    var key = segments[0].Trim();
                    var value = segments[1].Trim();
                    if (!map.ContainsKey(key))
                    {
                        map.Add(key, value);
                    }
                }
            }
            return map;
        }

        private Dictionary<string, string> ParseDevKeyValueFile(string devPath)
        {
            var map = new Dictionary<string, string>();
            var lines = File.ReadAllLines(devPath);
            for (int i = 0; i < lines.Length; i++)
            {
                var segments = lines[i].Split('=');
                if (segments.Length < 2)
                {
                    continue;
                }

                var key = segments[0].Trim();
                var value = segments[1].Trim();

                if (key == "SUBDEVICES.NUM")
                {
                    map[key] = value;
                    int num;
                    if (int.TryParse(value, out num))
                    {
                        for (int n = 0; n < num; n++)
                        {
                            i += 1;
                            if (i >= lines.Length) break;
                            var subDeviceLine = lines[i].Split('=');
                            if (subDeviceLine.Length >= 2)
                            {
                                map[subDeviceLine[0].Trim()] = subDeviceLine[1].Trim();
                            }

                            i += 1;
                            if (i >= lines.Length) break;
                            var matLine = lines[i].Split('=');
                            if (matLine.Length >= 2)
                            {
                                map["SUBDEVICES." + matLine[0].Trim()] = matLine[1].Trim();
                            }
                        }
                    }
                    continue;
                }

                if (key == "SOLIDMODELS.NUM")
                {
                    map[key] = value;
                    int num;
                    if (int.TryParse(value, out num))
                    {
                        for (int n = 0; n < num; n++)
                        {
                            i += 1;
                            if (i >= lines.Length) break;
                            var modelLine = lines[i].Split('=');
                            if (modelLine.Length >= 2)
                            {
                                map[modelLine[0].Trim()] = modelLine[1].Trim();
                            }

                            i += 1;
                            if (i >= lines.Length) break;
                            var matLine = lines[i].Split('=');
                            if (matLine.Length >= 2)
                            {
                                map["SOLIDMODEL." + matLine[0].Trim()] = matLine[1].Trim();
                            }
                        }
                    }
                    continue;
                }

                map[key] = value;
            }

            return map;
        }

        private string BuildDevName(Dictionary<string, string> kv, string defaultName)
        {
            string symbol;
            string type;
            if (kv.TryGetValue("SYMBOLNAME", out symbol) && !string.IsNullOrEmpty(symbol))
            {
                if (kv.TryGetValue("TYPE", out type) && !string.IsNullOrEmpty(type))
                {
                    return symbol + "-" + type;
                }
                return symbol;
            }

            return defaultName;
        }
    }
}
