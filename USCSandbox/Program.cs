using AssetsTools.NET;
using AssetsTools.NET.Extra;
using USCSandbox.Processor;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // null "C:\Users\nesquack\Documents\GitIssueWs\Data2g\Data2\Resources\unity_builtin_extra" 7 Switch

            var bundlePath = args[0];
            var assetsFileName = args[1];
            var shaderPathId = -1L; 
            if (args.Length > 2)
                shaderPathId = long.Parse(args[2]);
            var shaderPlatform = args.Length > 3 ? Enum.Parse<GPUPlatform>(args[3]) : GPUPlatform.d3d11;

            AssetsManager manager = new AssetsManager();
            AssetsFileInstance afileInst;
            AssetsFile afile;
            UnityVersion ver;
            int shaderTypeId;
            Dictionary<long, string> files = [];
            if (bundlePath != "null")
            {
                var bundleFile = manager.LoadBundleFile(bundlePath, true);
                afileInst = manager.LoadAssetsFileFromBundle(bundleFile, assetsFileName);
                afile = afileInst.file;
                manager.LoadClassPackage("classdata.tpk");
                manager.LoadClassDatabaseFromPackage(bundleFile.file.Header.EngineVersion);
                ver = UnityVersion.Parse(bundleFile.file.Header.EngineVersion);
                shaderTypeId = manager.ClassPackage.GetClassDatabase(ver.ToString()).FindAssetClassByName("Shader").ClassId;
                
                var ggm = manager.LoadAssetsFileFromBundle(bundleFile, "globalgamemanagers");
                //manager.LoadClassDatabaseFromPackage(ggm.file.Metadata.UnityVersion);
                var rsrcInfo = ggm.file.GetAssetsOfType(AssetClassID.ResourceManager)[0];
                var rsrcBf = manager.GetBaseField(ggm, rsrcInfo);
                var m_Container = rsrcBf["m_Container.Array"];
                foreach (var data in m_Container.Children)
                {
                    var name = data[0].AsString;
                    var pathId = data[1]["m_PathID"].AsLong;
                    files[pathId] = name;
                    //Console.WriteLine($"in resources.assets, pathid {pathId} = {name}");
                }
            }
            else
            {
                afileInst = manager.LoadAssetsFile(assetsFileName);
                afile = afileInst.file;
                manager.LoadClassPackage("classdata.tpk");
                manager.LoadClassDatabaseFromPackage(afile.Metadata.UnityVersion);
                ver = UnityVersion.Parse(afile.Metadata.UnityVersion);
                shaderTypeId = manager.ClassPackage.GetClassDatabase(ver.ToString()).FindAssetClassByName("Shader").ClassId;
            }

            var shaders = afileInst.file.GetAssetsOfType(shaderTypeId);
            int unnamedCount = 0;
            foreach (var shader in shaders)
            {
                if (shaderPathId != -1 && shader.PathId != shaderPathId)
                    continue;
                
                var shaderBf = manager.GetExtAsset(afileInst, 0, shader.PathId).baseField;
                if (shaderBf == null)
                {
                    Console.WriteLine("Shader asset not found.");
                    return;
                }
                var shaderProcessor = new ShaderProcessor(shaderBf, ver, shaderPlatform);
                bool fileNameExists = files.TryGetValue(shader.PathId, out string? name);
                string shaderText = shaderProcessor.Process();
                if (fileNameExists)
                {
                    Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "out", Path.GetDirectoryName(name)!));
                    File.WriteAllText($"{Path.Combine(Environment.CurrentDirectory, "out", name)}.shader", shaderText);
                    Console.WriteLine($"{name} decompiled");
                }
                else
                {
                    Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "out", "unnamed"));
                    File.WriteAllText($"{Path.Combine(Environment.CurrentDirectory, "out", "unnamed", $"unnamed {unnamedCount}")}.shader", shaderText);
                    Console.WriteLine($"Unnamed {unnamedCount} decompiled");
                    unnamedCount++;
                }
            }
        }
    }
}