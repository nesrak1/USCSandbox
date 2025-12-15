using AssetsTools.NET;
using System.Globalization;
using USCSandbox.Common;
using USCSandbox.Metadata;
using USCSandbox.ShaderCode.Converters;
using USCSandbox.ShaderCode.UShader;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox.Processor;
public class ShaderTextWriter
{
    private readonly StringBuilderIndented _sb;
    private readonly SerializedShader _shader;
    private readonly UnityVersion _engVer;

    public ShaderTextWriter(AssetTypeValueField shaderBf, UnityVersion engVer)
    {
        _sb = new StringBuilderIndented();
        _shader = new SerializedShader(shaderBf, engVer);
        _engVer = engVer;
    }

    public string LoadAndWrite(GPUPlatform platformId)
    {
        _sb.Clear();

        var blobMan = _shader.MakeBlobManager(platformId)
            ?? throw new Exception($"{nameof(_shader.MakeBlobManager)} returned null. Is this platform used?");

        _sb.AppendLine($"Shader \"{_shader.Name}\" {{");
        _sb.Indent();
        {
            WriteProperties(_shader.PropsField);
            foreach (var subShader in _shader.SubShaders)
            {
                WriteSubShader(subShader, blobMan, platformId);
            }

            if (!string.IsNullOrEmpty(_shader.FallbackName))
                _sb.AppendLine($"Fallback \"{_shader.FallbackName}\"");
        }
        _sb.Unindent();
        _sb.AppendLine("}");

        return _sb.ToString();
    }

    private void WriteProperties(AssetTypeValueField props)
    {
        // todo: convert this to an object (maybe?)
        _sb.AppendLine("Properties {");
        _sb.Indent();
        foreach (var prop in props)
        {
            _sb.Append("");

            var attributes = prop["m_Attributes.Array"];
            foreach (var attribute in attributes)
            {
                _sb.AppendNoIndent($"[{attribute.AsString}] ");
            }

            var flags = (SerializedPropertyFlag)prop["m_Flags"].AsUInt;
            if (flags.HasFlag(SerializedPropertyFlag.HideInInspector))
                _sb.AppendNoIndent("[HideInInspector] ");
            if (flags.HasFlag(SerializedPropertyFlag.PerRendererData))
                _sb.AppendNoIndent("[PerRendererData] ");
            if (flags.HasFlag(SerializedPropertyFlag.NoScaleOffset))
                _sb.AppendNoIndent("[NoScaleOffset] ");
            if (flags.HasFlag(SerializedPropertyFlag.Normal))
                _sb.AppendNoIndent("[Normal] ");
            if (flags.HasFlag(SerializedPropertyFlag.HDR))
                _sb.AppendNoIndent("[HDR] ");
            if (flags.HasFlag(SerializedPropertyFlag.Gamma))
                _sb.AppendNoIndent("[Gamma] ");
            // any more?

            var name = prop["m_Name"].AsString;
            var description = prop["m_Description"].AsString;
            var type = (SerializedPropertyType)prop["m_Type"].AsInt;
            var defValues = new string[]
            {
                    prop["m_DefValue[0]"].AsFloat.ToString(CultureInfo.InvariantCulture),
                    prop["m_DefValue[1]"].AsFloat.ToString(CultureInfo.InvariantCulture),
                    prop["m_DefValue[2]"].AsFloat.ToString(CultureInfo.InvariantCulture),
                    prop["m_DefValue[3]"].AsFloat.ToString(CultureInfo.InvariantCulture)
            };
            var defTextureName = prop["m_DefTexture.m_DefaultName"].AsString;
            var defTextureDim = prop["m_DefTexture.m_TexDim"].AsInt;

            var typeName = type switch
            {
                SerializedPropertyType.Color => "Color",
                SerializedPropertyType.Vector => "Vector",
                SerializedPropertyType.Float => "Float",
                SerializedPropertyType.Range => $"Range({defValues[1]}, {defValues[2]})",
                SerializedPropertyType.Texture => defTextureDim switch
                {
                    1 => "any",
                    2 => "2D",
                    3 => "3D",
                    4 => "Cube",
                    5 => "2DArray",
                    6 => "CubeArray",
                    _ => throw new NotSupportedException("Bad texture dim")
                },
                SerializedPropertyType.Int => "Int",
                _ => throw new NotSupportedException("Bad property type")
            };

            var value = type switch
            {
                SerializedPropertyType.Color or
                SerializedPropertyType.Vector => $"({defValues[0]}, {defValues[1]}, {defValues[2]}, {defValues[3]})",
                SerializedPropertyType.Float or
                SerializedPropertyType.Range or
                SerializedPropertyType.Int => defValues[0],
                SerializedPropertyType.Texture => $"\"{defTextureName}\" {{}}",
                _ => throw new NotSupportedException("Bad property type")
            };

            _sb.AppendNoIndent($"{name} (\"{description}\", {typeName}) = {value}\n");
        }
        _sb.Unindent();
        _sb.AppendLine("}");
    }

    private void WriteSubShader(SerializedSubShader subShader, BlobManager blobMan, GPUPlatform platformId)
    {
        _sb.AppendLine("SubShader {");
        _sb.Indent();
        {
            var tags = subShader.Tags;
            if (tags.Count > 0)
            {
                _sb.AppendLine("Tags {");
                _sb.Indent();
                {
                    foreach (var tag in tags)
                    {
                        _sb.AppendLine($"\"{tag.Key}\"=\"{tag.Value}\"");
                    }
                }
                _sb.Unindent();
                _sb.AppendLine("}");
            }

            var lod = subShader.LOD;
            if (lod != 0)
            {
                _sb.AppendLine($"LOD {lod}");
            }

            foreach (var pass in subShader.Passes)
            {
                WritePass(pass, blobMan, platformId);
            }
        }
        _sb.Unindent();
        _sb.AppendLine("}");
    }

    private void WritePass(SerializedPass pass, BlobManager blobMan, GPUPlatform platformId)
    {
        var usePassName = pass.UseName;
        if (!string.IsNullOrEmpty(usePassName))
        {
            _sb.AppendLine($"UsePass \"{usePassName}\"");
            return;
        }

        _sb.AppendLine("Pass {");
        _sb.Indent();
        {
            WritePassState(pass.State);
            _sb.AppendLine("");

            if (platformId == GPUPlatform.d3d11)
            {
                var dx11SubPrograms = Dx11ShaderConverter.Convert(pass, blobMan, _engVer);
                foreach (var subProg in dx11SubPrograms)
                {
                    var hlslConv = new UShaderFunctionToHlsl(subProg.UShaderProg, _sb.GetIndent());
                    _sb.AppendLine($"// Keywords: {string.Join(" && ", subProg.Keywords)}");
                    _sb.AppendNoIndent(hlslConv.WriteStruct());
                    _sb.AppendLine("");
                    _sb.AppendNoIndent(hlslConv.WriteFunction());
                    _sb.AppendLine("");
                }
            }
            else if (platformId == GPUPlatform.Switch)
            {
                var nvnSubprograms = NvnShaderConverter.Convert(pass, blobMan, _engVer);
                foreach (var subProg in nvnSubprograms)
                {
                    var hlslConv = new UShaderFunctionToHlsl(subProg.UShaderProg, _sb.GetIndent());
                    _sb.AppendLine($"// Keywords: {string.Join(" && ", subProg.Keywords)}");
                    _sb.AppendNoIndent(hlslConv.WriteStruct());
                    _sb.AppendLine("");
                    _sb.AppendNoIndent(hlslConv.WriteFunction());
                    _sb.AppendLine("");
                }
            }

            // skipping other programs at this time
            //SerializedProgram vertInfo, fragInfo;
            //foreach (var prog in pass.Programs)
            //{
            //    if (prog.Name == "progVertex")
            //        vertInfo = prog;
            //    else if (prog.Name == "progFragment")
            //        fragInfo = prog;
            //}

            //var vertProgInfos = vertInfo.GetForPlatform((int)GetVertexProgramForPlatform(_platformId));
            //var fragProgInfos = fragInfo.GetForPlatform((int)GetFragmentProgramForPlatform(_platformId));

            //// we should hopefully only have one of each type, but just in case...
            //// todo: cleanup
            //List<ShaderProgramBasket> baskets = [];
            //for (var i = 0; i < vertProgInfos.Count; i++)
            //{
            //    baskets.Add(new ShaderProgramBasket(vertInfo, vertProgInfos[i],
            //        vertInfo.ParameterBlobIndices.Count > 0 ? (int)vertInfo.ParameterBlobIndices[i] : -1));
            //}
            //for (var i = 0; i < fragProgInfos.Count; i++)
            //{
            //    baskets.Add(new ShaderProgramBasket(fragInfo, fragProgInfos[i],
            //        fragInfo.ParameterBlobIndices.Count > 0 ? (int)fragInfo.ParameterBlobIndices[i] : -1));
            //}
            //if (baskets.Count > 0)
            //    WritePassBody(blobManager, baskets, _sb.GetIndent());
        }
        _sb.Unindent();
        _sb.AppendLine("}");
    }

    private void WritePassState(SerializedShaderState state)
    {
        var name = state.Name;
        _sb.AppendLine($"Name \"{name}\"");

        var lod = state.LOD;
        if (lod != 0)
        {
            _sb.AppendLine($"LOD {lod}");
        }

        for (var i = 0; i < state.RtBlendState.Count; i++)
        {
            var index = state.RtBlendState.Count == 1 ? -1 : i;
            WritePassRtBlend(state.RtBlendState[i], index);
        }

        var alphaToMask = state.AlphaToMask.Value;
        var zClip = state.ZClip.Value;
        var zTest = state.ZTest.Value;
        var zWrite = state.ZWrite.Value;
        var culling = state.Culling.Value;
        var offsetFactor = state.OffsetFactor.Value;
        var offsetUnits = state.OffsetUnits.Value;
        var stencilRef = state.StencilRef.Value;
        var stencilReadMask = state.StencilReadMask.Value;
        var stencilWriteMask = state.StencilWriteMask.Value;
        var stencilOpPass = state.StencilOp.Pass.Value;
        var stencilOpFail = state.StencilOp.Fail.Value;
        var stencilOpZfail = state.StencilOp.ZFail.Value;
        var stencilOpComp = state.StencilOp.Comp.Value;
        var stencilOpFrontPass = state.StencilOpFront.Pass.Value;
        var stencilOpFrontFail = state.StencilOpFront.Fail.Value;
        var stencilOpFrontZfail = state.StencilOpFront.ZFail.Value;
        var stencilOpFrontComp = state.StencilOpFront.Comp.Value;
        var stencilOpBackPass = state.StencilOpBack.Pass.Value;
        var stencilOpBackFail = state.StencilOpBack.Fail.Value;
        var stencilOpBackZfail = state.StencilOpBack.ZFail.Value;
        var stencilOpBackComp = state.StencilOpBack.Comp.Value;
        var fogMode = state.FogMode;
        var fogColorX = state.FogColor.X.Value;
        var fogColorY = state.FogColor.Y.Value;
        var fogColorZ = state.FogColor.Z.Value;
        var fogColorW = state.FogColor.W.Value;
        var fogDensity = state.FogDensity.Value;
        var fogStart = state.FogStart.Value;
        var fogEnd = state.FogEnd.Value;
        var lighting = state.Lighting;

        if (alphaToMask > 0f)
        {
            _sb.AppendLine("AlphaToMask On");
        }
        if (zClip == ZClip.On)
        {
            _sb.AppendLine("ZClip On");
        }
        if (zTest != ZTest.None && zTest != ZTest.LEqual)
        {
            _sb.AppendLine($"ZTest {zTest}");
        }
        if (zWrite != ZWrite.On)
        {
            _sb.AppendLine($"ZWrite {zWrite}");
        }
        if (culling != CullMode.Back)
        {
            _sb.AppendLine($"Cull {culling}");
        }
        if (offsetFactor != 0f || offsetUnits != 0f)
        {
            _sb.AppendLine($"Offset {offsetFactor}, {offsetUnits}");
        }

        if (stencilRef != 0.0 || stencilReadMask != 255.0 || stencilWriteMask != 255.0
            || !(stencilOpPass == StencilOp.Keep && stencilOpFail == StencilOp.Keep && stencilOpZfail == StencilOp.Keep && stencilOpComp == StencilComp.Always)
            || !(stencilOpFrontPass == StencilOp.Keep && stencilOpFrontFail == StencilOp.Keep && stencilOpFrontZfail == StencilOp.Keep && stencilOpFrontComp == StencilComp.Always)
            || !(stencilOpBackPass == StencilOp.Keep && stencilOpBackFail == StencilOp.Keep && stencilOpBackZfail == StencilOp.Keep && stencilOpBackComp == StencilComp.Always))
        {
            _sb.AppendLine("Stencil {");
            _sb.Indent();
            if (stencilRef != 0.0)
            {
                _sb.AppendLine($"Ref {stencilRef}");
            }
            if (stencilReadMask != 255.0)
            {
                _sb.AppendLine($"ReadMask {stencilReadMask}");
            }
            if (stencilWriteMask != 255.0)
            {
                _sb.AppendLine($"WriteMask {stencilWriteMask}");
            }
            if (stencilOpPass != StencilOp.Keep
                || stencilOpFail != StencilOp.Keep
                || stencilOpZfail != StencilOp.Keep
                || (stencilOpComp != StencilComp.Always && stencilOpComp != StencilComp.Disabled))
            {
                _sb.AppendLine($"Comp {stencilOpComp}");
                _sb.AppendLine($"Pass {stencilOpPass}");
                _sb.AppendLine($"Fail {stencilOpFail}");
                _sb.AppendLine($"ZFail {stencilOpZfail}");
            }
            if (stencilOpFrontPass != StencilOp.Keep
                || stencilOpFrontFail != StencilOp.Keep
                || stencilOpFrontZfail != StencilOp.Keep
                || (stencilOpFrontComp != StencilComp.Always && stencilOpFrontComp != StencilComp.Disabled))
            {
                _sb.AppendLine($"CompFront {stencilOpFrontComp}");
                _sb.AppendLine($"PassFront {stencilOpFrontPass}");
                _sb.AppendLine($"FailFront {stencilOpFrontFail}");
                _sb.AppendLine($"ZFailFront {stencilOpFrontZfail}");
            }
            if (stencilOpBackPass != StencilOp.Keep
                || stencilOpBackFail != StencilOp.Keep
                || stencilOpBackZfail != StencilOp.Keep
                || (stencilOpBackComp != StencilComp.Always && stencilOpBackComp != StencilComp.Disabled))
            {
                _sb.AppendLine($"CompBack {stencilOpBackComp}");
                _sb.AppendLine($"PassBack {stencilOpBackPass}");
                _sb.AppendLine($"FailBack {stencilOpBackFail}");
                _sb.AppendLine($"ZFailBack {stencilOpBackZfail}");
            }
            _sb.Unindent();
            _sb.AppendLine("}");
        }

        if (fogMode != FogMode.Unknown || fogDensity != 0.0 || fogStart != 0.0 || fogEnd != 0.0
            || !(fogColorX == 0.0 && fogColorY == 0.0 && fogColorZ == 0.0 && fogColorW == 0.0))
        {
            _sb.AppendLine("Fog {");
            _sb.Indent();
            if (fogMode != FogMode.Unknown)
            {
                _sb.AppendLine($"Mode {fogMode}");
            }
            if (fogColorX != 0.0 || fogColorY != 0.0 || fogColorZ != 0.0 || fogColorW != 0.0)
            {
                _sb.AppendLine($"Color ({fogColorX.ToString(CultureInfo.InvariantCulture)}," +
                               $"{fogColorY.ToString(CultureInfo.InvariantCulture)}," +
                               $"{fogColorZ.ToString(CultureInfo.InvariantCulture)}," +
                               $"{fogColorW.ToString(CultureInfo.InvariantCulture)})");
            }
            if (fogDensity != 0.0)
            {
                _sb.AppendLine($"Density {fogDensity.ToString(CultureInfo.InvariantCulture)}");
            }
            if (fogStart != 0.0 || fogEnd != 0.0)
            {
                _sb.AppendLine($"Range {fogStart.ToString(CultureInfo.InvariantCulture)}, " +
                               $"{fogEnd.ToString(CultureInfo.InvariantCulture)}");
            }
            _sb.Unindent();
            _sb.AppendLine("}");
        }

        if (lighting)
        {
            _sb.AppendLine("Lighting On");
        }

        var tags = state.Tags;
        if (tags.Count > 0)
        {
            _sb.AppendLine("Tags {");
            _sb.Indent();
            {
                foreach (var tag in tags)
                {
                    _sb.AppendLine($"\"{tag.Key}\"=\"{tag.Value}\"");
                }
            }
            _sb.Unindent();
            _sb.AppendLine("}");
        }
    }

    private void WritePassRtBlend(SerializedShaderRTBlendState rtBlendState, int index)
    {
        var srcBlend = (BlendMode)(int)rtBlendState.SrcBlend.Value;
        var destBlend = (BlendMode)(int)rtBlendState.DestBlend.Value;
        var srcBlendAlpha = (BlendMode)(int)rtBlendState.SrcBlendAlpha.Value;
        var destBlendAlpha = (BlendMode)(int)rtBlendState.DestBlendAlpha.Value;
        var blendOp = (BlendOp)(int)rtBlendState.BlendOp.Value;
        var blendOpAlpha = (BlendOp)(int)rtBlendState.BlendOpAlpha.Value;
        var colMask = (ColorWriteMask)(int)rtBlendState.ColMask.Value;

        if (srcBlend != BlendMode.One || destBlend != BlendMode.Zero || srcBlendAlpha != BlendMode.One || destBlendAlpha != BlendMode.Zero)
        {
            _sb.Append("");
            _sb.AppendNoIndent("Blend ");
            if (index != -1)
            {
                _sb.AppendNoIndent($"{index} ");
            }
            _sb.AppendNoIndent($"{srcBlend} {destBlend}");
            if (srcBlendAlpha != BlendMode.One || destBlendAlpha != BlendMode.Zero)
            {
                _sb.AppendNoIndent($", {srcBlendAlpha} {destBlendAlpha}");
            }
            _sb.AppendNoIndent("\n");
        }

        if (blendOp != BlendOp.Add || blendOpAlpha != BlendOp.Add)
        {
            _sb.Append("");
            _sb.AppendNoIndent("BlendOp ");
            if (index != -1)
            {
                _sb.AppendNoIndent($"{index} ");
            }
            _sb.AppendNoIndent($"{blendOp}");
            if (blendOpAlpha != BlendOp.Add)
            {
                _sb.AppendNoIndent($", {blendOpAlpha}");
            }
            _sb.AppendNoIndent("\n");
        }

        if (colMask != ColorWriteMask.All)
        {
            _sb.Append("");
            _sb.AppendNoIndent("ColorMask ");
            if (colMask == ColorWriteMask.None)
            {
                _sb.AppendNoIndent("0");
            }
            else
            {
                if ((colMask & ColorWriteMask.Red) == ColorWriteMask.Red)
                {
                    _sb.AppendNoIndent("R");
                }
                if ((colMask & ColorWriteMask.Green) == ColorWriteMask.Green)
                {
                    _sb.AppendNoIndent("G");
                }
                if ((colMask & ColorWriteMask.Blue) == ColorWriteMask.Blue)
                {
                    _sb.AppendNoIndent("B");
                }
                if ((colMask & ColorWriteMask.Alpha) == ColorWriteMask.Alpha)
                {
                    _sb.AppendNoIndent("A");
                }
            }
            if (index != -1)
            {
                _sb.AppendNoIndent($" {index}"); // -1 check needed?
            }
            _sb.AppendNoIndent("\n");
        }
    }
}
