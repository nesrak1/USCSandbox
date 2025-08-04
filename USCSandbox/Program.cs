using AssetsTools.NET.Extra;
using USCSandbox.Processor;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("USCS");
                Console.WriteLine("  [bundle path (or \"null\" for no bundle)]");
                Console.WriteLine("  [assets path (or file name in bundle)]");
                Console.WriteLine("  <shader path id (or don't include for all shaders)>");
                Console.WriteLine("  <shader platform: [d3d11, Switch] (or skip this arg for d3d11)>");
                return;
            }

            var manager = new AssetsManager();
            AssetsFileInstance afileInst;
            UnityVersion ver;

            var bundlePath = args[0];
            if (args.Length == 1)
            {
                var bundleFile = manager.LoadBundleFile(bundlePath, true);
                var dirInfs = bundleFile.file.BlockAndDirInfo.DirectoryInfos;
                Console.WriteLine("Available files in bundle:");
                foreach (var dirInf in dirInfs)
                {
                    if ((dirInf.Flags & 4) == 0)
                        continue;

                    Console.WriteLine($"  {dirInf.Name}");
                }
                return;
            }

            var assetsFileName = args[1];
            if (args.Length == 2)
            {
                if (bundlePath != "null")
                {
                    var bundleFile = manager.LoadBundleFile(bundlePath, true);
                    afileInst = manager.LoadAssetsFileFromBundle(bundleFile, assetsFileName);

                    manager.LoadClassPackage("classdata.tpk");
                    manager.LoadClassDatabaseFromPackage(bundleFile.file.Header.EngineVersion);

                    Console.WriteLine("Available shaders in bundle:");
                }
                else
                {
                    afileInst = manager.LoadAssetsFile(assetsFileName);

                    manager.LoadClassPackage("classdata.tpk");
                    manager.LoadClassDatabaseFromPackage(afileInst.file.Metadata.UnityVersion);

                    Console.WriteLine("Available shaders in assets file:");
                }

                foreach (var shaderInf in afileInst.file.GetAssetsOfType(AssetClassID.Shader))
                {
                    var tmpShaderBf = manager.GetBaseField(afileInst, shaderInf);
                    var tmpShaderName = tmpShaderBf["m_ParsedForm"]["m_Name"].AsString;
                    Console.WriteLine($"  {tmpShaderName} (path id {shaderInf.PathId})");
                }
                return;
            }

            var shaderPathId = -1L;
            if (args.Length > 2)
                shaderPathId = long.Parse(args[2]);

            var shaderPlatform = args.Length > 3
                ? Enum.Parse<GPUPlatform>(args[3])
                : GPUPlatform.d3d11;

            Dictionary<long, string> files = [];
            if (bundlePath != "null")
            {
                var bundleFile = manager.LoadBundleFile(bundlePath, true);
                afileInst = manager.LoadAssetsFileFromBundle(bundleFile, assetsFileName);

                var verStr = bundleFile.file.Header.EngineVersion;
                manager.LoadClassPackage("classdata.tpk");
                manager.LoadClassDatabaseFromPackage(verStr);
                ver = UnityVersion.Parse(verStr);
            }
            else
            {
                afileInst = manager.LoadAssetsFile(assetsFileName);

                var verStr = afileInst.file.Metadata.UnityVersion;
                manager.LoadClassPackage("classdata.tpk");
                manager.LoadClassDatabaseFromPackage(verStr);
                ver = UnityVersion.Parse(verStr);
            }

            var shaderBf = manager.GetBaseField(afileInst, shaderPathId);
            if (shaderBf == null)
            {
                Console.WriteLine("Shader asset not found or couldn't be read.");
                return;
            }

            var shaderName = shaderBf["m_ParsedForm"]["m_Name"].AsString;
            var shaderProcessor = new ShaderProcessor(shaderBf, ver, shaderPlatform);
            string shaderText = shaderProcessor.Process();

            Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "out", Path.GetDirectoryName(shaderName)!));
            File.WriteAllText($"{Path.Combine(Environment.CurrentDirectory, "out", shaderName)}.shader", shaderText);
            Console.WriteLine($"{shaderName} decompiled");
        }
    }
}