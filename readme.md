# uscsandbox

A rework of the version of USC from [AssetRipper](https://github.com/AssetRipper/AssetRipper) that can be run headlessly.

The version I wrote for AssetRipper got a bit of a fix-up, but I was getting annoyed when I needed to batch test files. So this exists as a way to run the tool outside of AssetRipper. I'm not doing much work to this repo, so I am unsure of if and when it will get any updates. Despite that, it will remain here free to use (I probably won't archive it). Interested in getting problems fixed? Please contact me!

The license is GPL v3. USC was originally MIT, but I am changing it because:

- Even if most of the base Unity shader parsing code is from [uTinyRipper](https://github.com/mafaca/UtinyRipper), some of it was added by ds5678 to AssetRipper under GPL.
- I'm pretty sure the DirectX shader disassembler (which has existed since the original UnityShaderEdit) is based on [3dmigoto](https://github.com/bo3b/3Dmigoto) which is GPL licensed.

Unfortunately it is not an easy task to figure out what parts of the code are GPL and to replace them, so the repo will remain GPL.

Note: AssetRipper Premium now comes with its own shader decompiler. Supposedly, it is not based on any of the USC work. Do not ask for support in this repo about AssetRipper's new shader decompiler, I am not involved with it at all.

## supported architectures

- DirectX (decently well supported)
- Switch NVN (WIP and not functional at this time)

## how to use

```
USCS [bundle path] [assets path] [shader path id] <--platform> <--version> <--all>
  [bundle path (or \"null\" for no bundle)]
  [assets path (or file name in bundle)]
  [shader path id (or --all to load all shaders)]
  --platform <[d3d11, Switch] (or skip this arg for d3d11)>
  --version <unity version override>
```

- List assets files in bundle
    - `uscsandbox file.bundle`
- List shader assets in bundle
    - `uscsandbox file.bundle CAB-abcdef0123456789abcdef`
- Decompile single shader in bundle
    - `uscsandbox file.bundle CAB-abcdef0123456789abcdef 123456789123456789`
- Decompile single shader in bundle with stripped version
    - `uscsandbox file.bundle CAB-abcdef0123456789abcdef 123456789123456789 --version 6000.0.50f1`
- Decompile all shaders in bundle
    - `uscsandbox file.bundle CAB-abcdef0123456789abcdef 123456789123456789 --all`

- List shader assets in .assets file
    - `uscsandbox null resources.assets`
- Decompile single shader in .assets file
    - `uscsandbox null resources.assets 123456789123456789`
- Decompile all shaders in .assets file
    - `uscsandbox null resources.assets 123456789123456789 --all`

- Decompile switch shader from bundle (unsupported)
    - `uscsandbox file.bundle CAB-abcdef0123456789abcdef 123456789123456789 --platform Switch`

And of course, if your bundle name is "null", please rename it to use this tool :)

## how does it work

The workings are not well documented, but here's a quick summary. A Unity shader is split into two major parts: the serialized (asset field) metadata and the shader data (in the `compressedBlob` field). Metadata in the shader data exists both before the platform specific data, and either after the platform specific shader data or in a "parameter blob". See `ShaderSubProgramData` for more details. Where certain data is located depends on the engine version and the platform, but usually metadata for both is combined to get a better understanding of the shader's inputs/outputs/CBs. The actual code part of the shader is converted to USIL using a `<Platform>ToUSIL` class, then USIL is converted to an hlsl function body with `UShaderFunctionToHLSL`. The rest of the shaderlab code is handled by the metadata parsers. USIL has three postprocessing categories: Fixers, Metadders, and Optimizers (due to bad naming on my part, the class that processes all three is called `USILOptimizerApplier`). Fixers are postprocessing tasks that are _required_ for the shader to correctly decompiled. Metadders, short for "Metadata Adders" insert metadata from outside of the native shader format and add information like constant buffer names, inputs/outputs, etc. If you see fields like `cb[0]` in the finally result, this means a Metadder failed to match a constant buffer operand to the metadata. Finally, Optimizers clean the code up and make it a little more readable. Optimizers are usually optional and can probably be disabled.

<!-- todo: edit/remove below section -->

On the metadata side, a "shader basket" is a made up term given to a pairing of metadata and shader data (i.e. DXBC data) together. On a similar note, one of the goals of the decompiler is to find all combinations of shader variants and match up the vertex and fragment shaders together. I think it is possible for, as an example, a single vertex shader could be matched up with multiple different fragment shaders due to the way keywords work, but don't quote me on that.