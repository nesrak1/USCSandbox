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
            var bundlePath = args[0];
            var assetsFileName = args[1];
            var shaderPathId = long.Parse(args[2]);
            var shaderPlatform = args.Length > 3 ? Enum.Parse<GPUPlatform>(args[3]) : GPUPlatform.d3d11;

            AssetsManager manager = new AssetsManager();
            AssetsFileInstance afileInst;
            AssetsFile afile;
            UnityVersion ver;
            if (bundlePath != "null")
            {
                var bundleFile = manager.LoadBundleFile(bundlePath, true);
                afileInst = manager.LoadAssetsFileFromBundle(bundleFile, assetsFileName);
                afile = afileInst.file;
                manager.LoadClassPackage("classdata.tpk");
                manager.LoadClassDatabaseFromPackage(bundleFile.file.Header.EngineVersion);
                ver = UnityVersion.Parse(bundleFile.file.Header.EngineVersion);
            }
            else
            {
                afileInst = manager.LoadAssetsFile(assetsFileName);
                afile = afileInst.file;
                manager.LoadClassPackage("classdata.tpk");
                manager.LoadClassDatabaseFromPackage(afile.Metadata.UnityVersion);
                ver = UnityVersion.Parse(afile.Metadata.UnityVersion);
            }

            var shaderBf = manager.GetExtAsset(afileInst, 0, shaderPathId).baseField;
            if (shaderBf == null)
            {
                Console.WriteLine("Shader asset not found.");
                return;
            }
            var shaderProcessor = new ShaderProcessor(shaderBf, ver, shaderPlatform);
            Console.WriteLine(shaderProcessor.Process());
        }
    }
}