using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.Converter;
using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.UShader.DirectX;
using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.UShader.Function;
using AssetRipper.Primitives;
using AssetsTools.NET;
using AssetsTools.NET.Extra.Decompressors.LZ4;
using System.Globalization;
using System.Text;
using USCSandbox.Extras;

namespace USCSandbox.Processor
{
    internal class ShaderProcessor
    {
        private AssetTypeValueField _shaderBf;
        private GPUPlatform _platformId;
        private UnityVersion _engVer;
        private StringBuilderIndented _sb;

        public ShaderProcessor(AssetTypeValueField shaderBf, UnityVersion engVer, GPUPlatform platformId)
        {
            _engVer = engVer;
            _shaderBf = shaderBf;
            _platformId = platformId;
            _sb = new StringBuilderIndented();
        }

        public string Process()
        {
            _sb.Clear();

            var parsedForm = _shaderBf["m_ParsedForm"];
            var name = parsedForm["m_Name"].AsString;
            var keywordNames = parsedForm["m_KeywordNames.Array"].Select(i => i.AsString).ToList();

            var platforms = _shaderBf["platforms.Array"].Select(i => i.AsInt).ToList();
            var offsets = _shaderBf["offsets.Array"];
            var compressedLengths = _shaderBf["compressedLengths.Array"];
            var decompressedLengths = _shaderBf["decompressedLengths.Array"];
            var compressedBlob = _shaderBf["compressedBlob.Array"].AsByteArray;

            var selectedIndex = platforms.IndexOf((int)_platformId);
            
            uint selectedOffset;
            if (offsets[selectedIndex].Children.Count > 0)
                selectedOffset = offsets[selectedIndex]["Array"][0].AsUInt;
            else
                selectedOffset = offsets[selectedIndex].AsUInt;
            
            uint selectedCompressedLength;
            if (compressedLengths[selectedIndex].Children.Count > 0)
                selectedCompressedLength = compressedLengths[selectedIndex]["Array"][0].AsUInt;
            else
                selectedCompressedLength = compressedLengths[selectedIndex].AsUInt;
            
            uint selectedDecompressedLength;
            if (offsets[selectedIndex].Children.Count > 0)
                selectedDecompressedLength = decompressedLengths[selectedIndex]["Array"][0].AsUInt;
            else
                selectedDecompressedLength = decompressedLengths[selectedIndex].AsUInt;

            var decompressedBlob = new byte[selectedDecompressedLength];
            var lz4Decoder = new Lz4DecoderStream(new MemoryStream(compressedBlob));
            lz4Decoder.Read(decompressedBlob, 0, (int)selectedDecompressedLength);
            lz4Decoder.Dispose();

            var blobManager = new BlobManager(decompressedBlob, _engVer);
            for (var i = 0; i < blobManager.Entries.Count; i++)
            {
                var entryBytes = blobManager.GetRawEntry(i);
                File.WriteAllBytes($"dbg_entry_{i}.bin", entryBytes);
            }

            _sb.AppendLine($"Shader \"{name}\" {{");
            _sb.Indent();
            {
                WriteProperties(parsedForm["m_PropInfo"]);
                WriteSubShaders(blobManager, parsedForm);
                if (!string.IsNullOrEmpty(parsedForm["m_FallbackName"].AsString))
                    _sb.AppendLine($"Fallback \"{parsedForm["m_FallbackName"].AsString}\"");
            }
            _sb.Unindent();
            _sb.AppendLine("}");

            return _sb.ToString();
        }

        private void WritePassBody(
            BlobManager blobManager,
            List<ShaderProgramBasket> baskets,
            int depth)
        {
            _sb.AppendLine("CGPROGRAM");

            var defineSb = new StringBuilder();
            var passSb = new StringBuilder();
            
            defineSb.AppendLine(new string(' ', depth * 4));
            passSb.AppendLine(new string(' ', depth * 4));
            var basketsInfo = baskets
                .Select(x => 
                new 
                {
                    progInfo = x.ProgramInfo,
                    subProgInfo = x.SubProgramInfo,
                    index = x.ParameterBlobIndex,
                    subProg = blobManager.GetShaderSubProgram((int)x.SubProgramInfo.BlobIndex)
                })
                .OrderBy(x => x.subProg.GetProgramType(_engVer))
                .ThenByDescending(x => x.subProg.GlobalKeywords.Concat(x.subProg.LocalKeywords).Count())
                .ToList();

            var subPrograms = basketsInfo.Select(x => x.subProg).ToList();
            ShaderGpuProgramType[] vertTypes = [ShaderGpuProgramType.DX11VertexSM40, ShaderGpuProgramType.ConsoleVS];
            ShaderGpuProgramType[] fragTypes = [ShaderGpuProgramType.DX11PixelSM40, ShaderGpuProgramType.ConsoleFS];
            var firstVert = subPrograms.FirstOrDefault(x => vertTypes.Contains(x.GetProgramType(_engVer)));
            var firstFrag = subPrograms.FirstOrDefault(x => fragTypes.Contains(x.GetProgramType(_engVer)));
            
            if (firstVert is not null)
            {
                defineSb.Append(new string(' ', depth * 4));
                defineSb.AppendLine("#pragma vertex vert");
            }

            if (firstFrag is not null)
            {
                defineSb.Append(new string(' ', depth * 4));
                defineSb.AppendLine("#pragma fragment frag");
            }

            defineSb.AppendLine();

            var allKeywordsCombinations = subPrograms.Select(x => x.GlobalKeywords.Concat(x.LocalKeywords).Order()).ToList();
            HashSet<string> allUniqueKeywords = allKeywordsCombinations.SelectMany(x => x).ToHashSet();
            int leastKeywordsAmount = allKeywordsCombinations.Min(x => x.Count());
            List<string> mandatoryKeywords = [];
            
            List<string> optionalKeywords = new List<string>();
            foreach (string keyword in allUniqueKeywords)
            {
                if (allKeywordsCombinations.All(x => x.Contains(keyword)))
                {
                    defineSb.Append(new string(' ', depth * 4));
                    defineSb.AppendLine($"#pragma multi_compile {keyword}");
                    mandatoryKeywords.Add(keyword);
                }
                else
                {
                    optionalKeywords.Add(keyword);
                }
            }

            if (leastKeywordsAmount > mandatoryKeywords.Count)
            {
                var leastAmountCombinations = allKeywordsCombinations
                    .Where(x => x.Count() == leastKeywordsAmount)
                    .Select(x => x.Except(mandatoryKeywords).ToList())
                    .ToList();
                int multiCompileKeywordIndex = 0;
                while (multiCompileKeywordIndex < leastKeywordsAmount - mandatoryKeywords.Count)
                {
                    var multiCompileKeywords = leastAmountCombinations
                        .Select(x => x[multiCompileKeywordIndex])
                        .ToHashSet();
                
                    defineSb.Append(new string(' ', depth * 4));
                    defineSb.AppendLine($"#pragma multi_compile {string.Join(" ", multiCompileKeywords)}");
                    optionalKeywords = optionalKeywords.Except(multiCompileKeywords).ToList();
                    multiCompileKeywordIndex++;
                }
            }

            foreach (string keyword in optionalKeywords)
            {
                defineSb.Append(new string(' ', depth * 4));
                defineSb.AppendLine($"#pragma shader_feature {keyword}");
            }
            
            bool encounterdVert = false;
            bool encounterdFrag = false;
            
            var declaredCBufs = subPrograms
                .Select(x => String.Join("-", x.GlobalKeywords.Concat(x.LocalKeywords).Order()))
                .Distinct()
                .ToDictionary(x => x, _ => new HashSet<string>());
            
            var lastVertext = basketsInfo.LastOrDefault(x => vertTypes.Contains(x.subProg.GetProgramType(_engVer)));
            var lastFragment = basketsInfo.LastOrDefault(x => fragTypes.Contains(x.subProg.GetProgramType(_engVer)));

            bool noVertexVariants = subPrograms.Count(x => vertTypes.Contains(x.GetProgramType(_engVer))) <= 1;
            bool noFragmentVariants = subPrograms.Count(x => fragTypes.Contains(x.GetProgramType(_engVer))) <= 1;
            
            foreach (var basket in basketsInfo)
            {
                // if (basket != basketsInfo.Last())
                //     continue;
                var structSb = new StringBuilder();
                var cbufferSb = new StringBuilder();
                var texSb = new StringBuilder();
                var memeSb = new StringBuilder();
                var codeSb = new StringBuilder();
                
                var progInfo = basket.progInfo;
                var subProgInfo = basket.subProgInfo;
                var index = basket.index;

                var subProg = blobManager.GetShaderSubProgram((int)subProgInfo.BlobIndex);
                File.WriteAllBytes($"dbg_entry_data_{subProgInfo.BlobIndex}.bin", subProg.ProgramData);

                ShaderParams param;
                if (index != -1)
                {
                    param = blobManager.GetShaderParams(index);
                }
                else
                {
                    param = subProg.ShaderParams;
                }

                //Console.WriteLine($"working on {basket.SubProgramInfo.BlobIndex}");

                param.CombineCommon(progInfo);

                var programType = subProg.GetProgramType(_engVer);
                var graphicApi = _platformId;

                // defineSb.Append(new string(' ', depth * 4));
                // defineSb.AppendLine(programType switch
                // {
                //     ShaderGpuProgramType.DX11VertexSM40 => "#pragma vertex vert",
                //     ShaderGpuProgramType.DX11PixelSM40 => "#pragma fragment frag",
                //     ShaderGpuProgramType.ConsoleVS => "#pragma vertex vert",
                //     ShaderGpuProgramType.ConsoleFS => "#pragma fragment frag",
                //     _ => $"// unknown shader type {programType}"
                // });

                var keywords = subProg.GlobalKeywords.Concat(subProg.LocalKeywords).Order().ToArray();

                if ((!noVertexVariants && basket == lastVertext) || (!noFragmentVariants && basket == lastFragment))
                {
                    structSb.AppendLine();
                    structSb.Append(new string(' ', depth * 4));
                    structSb.Append("#else");
                }
                else if ((!noVertexVariants && programType == ShaderGpuProgramType.DX11VertexSM40)
                        || (!noFragmentVariants && programType == ShaderGpuProgramType.DX11PixelSM40))
                {
                    string preprocessorDirective;
                    if ((!encounterdVert && programType == ShaderGpuProgramType.DX11VertexSM40)
                        || (!encounterdFrag && programType == ShaderGpuProgramType.DX11PixelSM40))
                    {
                        preprocessorDirective = "if";
                        if (programType == ShaderGpuProgramType.DX11VertexSM40)
                        {
                            encounterdVert = true;
                        }
                        else
                        {
                            encounterdFrag = true;
                            structSb.Append(new string(' ', depth * 4));
                            structSb.AppendLine("#endif");
                            structSb.AppendLine();
                        }
                    }
                    else
                    {
                        preprocessorDirective = "elif";
                    }
                
                    structSb.AppendLine();
                    structSb.Append(new string(' ', depth * 4));
                    structSb.Append($"#{preprocessorDirective} {string.Join(" && ", keywords)}");
                }
                // if (!keywords.SequenceEqual(["FOG_LINEAR", "POINT", "SHADOWS_CUBE", "SHADOWS_SOFT"]))
                //     continue;
                
                // if ((!encounterdVert && programType == ShaderGpuProgramType.DX11VertexSM40)
                //     || (!encounterdFrag && programType == ShaderGpuProgramType.DX11PixelSM40))
                // {
                    cbufferSb.Append(new string(' ', depth * 4));
                    cbufferSb.AppendLine($"// CBs for {programType}");
                //}
                foreach (ConstantBuffer cbuffer in param.ConstantBuffers)
                {
                    //if (!UnityShaderConstants.BUILTIN_CBUFFER_NAMES.Contains(cbuffer.Name))
                    {
                        cbufferSb.Append(WritePassCBuffer(param, declaredCBufs[string.Join("-", keywords)], cbuffer, depth));
                    }
                }

                //cbufferSb.AppendLine();
                // if ((!encounterdVert && programType == ShaderGpuProgramType.DX11VertexSM40)
                //     || (!encounterdFrag && programType == ShaderGpuProgramType.DX11PixelSM40))
                // {
                    texSb.Append(new string(' ', depth * 4));
                    texSb.AppendLine($"// Textures for {programType}");
                //}

                texSb.Append(WritePassTextures(param, declaredCBufs[string.Join("-", keywords)], depth));
                //texSb.AppendLine();
                
                switch (programType)
                {
                    case ShaderGpuProgramType.DX11VertexSM40:
                    case ShaderGpuProgramType.DX11PixelSM40:
                    {
                        // DBG
                        // int ifDepth = 0;
                        // foreach (var inst in conv.DxShader!.Shdr.shaderInstructions)
                        // {
                        //     memeSb.Append(new string(' ', depth * 4));
                        //     memeSb.AppendLine("// " + DirectXDisassemblyHelper.DisassembleInstruction(inst, ref ifDepth));
                        // }
                        // ///

                        var conv = new USCShaderConverter();
                        conv.LoadDirectXCompiledShader(new MemoryStream(subProg.ProgramData), graphicApi, _engVer);
                        conv.ConvertDxShaderToUShaderProgram();
                        conv.ApplyMetadataToProgram(subProg, param, _engVer);

                        UShaderFunctionToHLSL hlslConverter = new UShaderFunctionToHLSL(conv.ShaderProgram!, depth);
                        

                        //string preprocessorDirective;
                        if ((!encounterdVert && programType == ShaderGpuProgramType.DX11VertexSM40)
                            || (!encounterdFrag && programType == ShaderGpuProgramType.DX11PixelSM40))
                        {
                            

                            //preprocessorDirective = "if";

                            // codeSb.AppendLine();
                            // codeSb.Append(new string(' ', depth * 4));
                            // codeSb.Append("#if " + string.Join(" && ", keywords));
                            // var keywordsSet = programType == ShaderGpuProgramType.DX11VertexSM40
                            //     ? vertextUniqueKeywords
                            //     : fragmentUniqueKeywords;
                            // foreach (string excludedKeyword in keywordsSet.Except(keywords))
                            // {
                            //     codeSb.Append(" && ");
                            //     codeSb.Append("!" + excludedKeyword);
                            // }
                            // codeSb.AppendLine();
                        }
                        else
                        {
                            //preprocessorDirective = "elif";
                        }
                        
                        // structSb.AppendLine();
                        // structSb.Append(new string(' ', depth * 4));
                        // structSb.Append($"#{preprocessorDirective} {string.Join(" && ", keywords)}");
                        // codeSb.AppendLine();
                        // codeSb.Append(new string(' ', depth * 4));
                        // codeSb.Append($"#{preprocessorDirective} {string.Join(" && ", keywords)}");

                        structSb.AppendLine();
                        codeSb.AppendLine();
                        
                        structSb.Append(hlslConverter.WriteStruct());
                        structSb.AppendLine();
                        codeSb.Append(hlslConverter.WriteFunction());

                        break;
                    }
                    case ShaderGpuProgramType.ConsoleVS:
                    case ShaderGpuProgramType.ConsoleFS:
                    {
                        var conv = new USCShaderConverter();
                        conv.LoadUnityNvnShader(new MemoryStream(subProg.ProgramData), graphicApi, _engVer);
                        conv.ConvertNvnShaderToUShaderProgram(programType);
                        conv.ApplyMetadataToProgram(subProg, param, _engVer);

                        UShaderFunctionToHLSL hlslConverter = new UShaderFunctionToHLSL(conv.ShaderProgram!, depth);
                        if ((!encounterdVert && programType == ShaderGpuProgramType.ConsoleVS)
                            || (!encounterdFrag && programType == ShaderGpuProgramType.ConsoleFS))
                        {
                            structSb.Append(hlslConverter.WriteStruct());
                            structSb.AppendLine();
                            if (programType == ShaderGpuProgramType.ConsoleVS)
                                encounterdVert = true;
                            else
                                encounterdFrag = true;
                        }

                        codeSb.Append(new string(' ', depth * 4));
                        codeSb.AppendLine("// Keywords: " + string.Join(", ", keywords));
                        codeSb.Append(hlslConverter.WriteFunction());

                        break;
                    }
                }
                passSb.Append(structSb.ToString());
                passSb.Append(cbufferSb.ToString());
                passSb.Append(texSb.ToString());
                passSb.Append(memeSb.ToString());
                passSb.Append(codeSb.ToString());
            }

            if (!noFragmentVariants)
            {
                passSb.Append(new string(' ', depth * 4));
                passSb.AppendLine("#endif");
            }

            _sb.AppendNoIndent(defineSb.ToString());
            _sb.AppendNoIndent(passSb.ToString());

            _sb.AppendLine("ENDCG");
            _sb.AppendLine("");
        }

        private string WritePassCBuffer(
            ShaderParams shaderParams, HashSet<string> declaredCBufs,
            ConstantBuffer? cbuffer, int depth)
        {
            StringBuilder sb = new StringBuilder();
            if (cbuffer != null)
            {
                bool nonGlobalCbuffer = cbuffer.Name != "$Globals";
                int cbufferIndex = shaderParams.ConstantBuffers.IndexOf(cbuffer);

                // if (nonGlobalCbuffer)
                // {
                //     sb.Append(new string(' ', depth * 4)); // todo: new stringbuilder
                //     sb.AppendLine($"// CBUFFER_START({cbuffer.Name}) // {cbufferIndex}");
                //     depth++;
                // }

                char[] chars = new char[] { 'x', 'y', 'z', 'w' };
                List<ConstantBufferParameter> allParams = cbuffer.CBParams;
                foreach (ConstantBufferParameter param in allParams)
                {
                    string typeName = DXShaderNamingUtils.GetConstantBufferParamTypeName(param);
                    string name = param.ParamName;

                    // skip things like unity_MatrixVP if they show up in $Globals
                    if (UnityShaderConstants.INCLUDED_UNITY_PROP_NAMES.Contains(name))
                    {
                        continue;
                    }

                    if (!declaredCBufs.Contains(name))
                    {
                        if (param.ArraySize > 0)
                        {
                            sb.Append(new string(' ', depth * 4));
                            if (nonGlobalCbuffer)
                                sb.Append("// ");
                            sb.AppendLine($"{typeName} {name}[{param.ArraySize}]; // {param.Index} (starting at cb{cbufferIndex}[{param.Index / 16}].{chars[param.Index % 16 / 4]})");
                        }
                        else
                        {
                            sb.Append(new string(' ', depth * 4));
                            if (nonGlobalCbuffer && !cbuffer.Name.StartsWith("UnityPerDrawSprite"))
                                sb.Append("// ");
                            sb.AppendLine($"{typeName} {name}; // {param.Index} (starting at cb{cbufferIndex}[{param.Index / 16}].{chars[param.Index % 16 / 4]})");
                        }
                        declaredCBufs.Add(name);
                    }
                }

                // if (nonGlobalCbuffer)
                // {
                //     depth--;
                //     sb.Append(new string(' ', depth * 4));
                //     sb.AppendLine("// CBUFFER_END");
                // }
            }
            return sb.ToString();
        }

        private string WritePassTextures(
            ShaderParams shaderParams, HashSet<string> declaredCBufs, int depth)
        {
            StringBuilder sb = new StringBuilder();
            foreach (TextureParameter param in shaderParams.TextureParameters)
            {
                string name = param.Name;
                if (!declaredCBufs.Contains(name) && !UnityShaderConstants.BUILTIN_TEXTURE_NAMES.Contains(name))
                {
                    sb.Append(new string(' ', depth * 4));
                    switch (param.Dim)
                    {
                        case 2:
                            sb.AppendLine($"sampler2D {name}; // {param.Index}");
                            break;
                        case 3:
                            sb.AppendLine($"sampler3D {name}; // {param.Index}");
                            break;
                        case 4:
                            sb.AppendLine($"samplerCUBE {name}; // {param.Index}");
                            break;
                        case 5:
                            sb.AppendLine($"UNITY_DECLARE_TEX2DARRAY({name}); // {param.Index}");
                            break;
                        case 6:
                            sb.AppendLine($"UNITY_DECLARE_TEXCUBEARRAY({name}); // {param.Index}");
                            break;
                        default:
                            sb.AppendLine($"sampler2D {name}; // {param.Index} // Unsure of real type ({param.Dim})");
                            break;
                    }
                    declaredCBufs.Add(name);
                }
            }
            return sb.ToString();
        }

        private void WriteProperties(AssetTypeValueField propInfo)
        {
            _sb.AppendLine("Properties {");
            _sb.Indent();
            var props = propInfo["m_Props.Array"];
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
                // more?

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

        private void WriteSubShaders(BlobManager blobManager, AssetTypeValueField parsedForm)
        {
            var subshaders = parsedForm["m_SubShaders.Array"];
            foreach (var subshader in subshaders)
            {
                _sb.AppendLine("SubShader {");
                _sb.Indent();
                {
                    var tags = subshader["m_Tags"]["tags.Array"];
                    if (tags.Children.Count > 0)
                    {
                        _sb.AppendLine("Tags {");
                        _sb.Indent();
                        {
                            foreach (var tag in tags)
                            {
                                _sb.AppendLine($"\"{tag["first"].AsString}\"=\"{tag["second"].AsString}\"");
                            }
                        }
                        _sb.Unindent();
                        _sb.AppendLine("}");
                    }

                    var lod = subshader["m_LOD"].AsInt;
                    if (lod != 0)
                    {
                        _sb.AppendLine($"LOD {lod}");
                    }

                    WritePasses(blobManager, subshader);
                }
                _sb.Unindent();
                _sb.AppendLine("}");
            }
        }

        private void WritePasses(BlobManager blobManager, AssetTypeValueField subshader)
        {
            var passes = subshader["m_Passes.Array"];
            foreach (var pass in passes)
            {
                var usePassName = pass["m_UseName"].AsString;
                if (!string.IsNullOrEmpty(usePassName))
                {
                    _sb.AppendLine($"UsePass \"{usePassName}\"");
                    continue;
                }
                // if (pass != passes.Last())
                //     continue;
                _sb.AppendLine("Pass {");
                _sb.Indent();
                {
                    WritePassState(pass["m_State"]);

                    var nameTable = pass["m_NameIndices.Array"]
                        .ToDictionary(ni => ni["second"].AsInt, ni => ni["first"].AsString);

                    var vertInfo = new SerializedProgramInfo(pass["progVertex"], nameTable);
                    var fragInfo = new SerializedProgramInfo(pass["progFragment"], nameTable);

                    var vertProgInfos = vertInfo.GetForPlatform((int)GetVertexProgramForPlatform(_platformId));
                    var fragProgInfos = fragInfo.GetForPlatform((int)GetFragmentProgramForPlatform(_platformId));

                    // if (vertProgInfos.Count != fragProgInfos.Count && fragProgInfos.Count > 0)
                    // {
                    //     throw new Exception("Vert and frag program count should be the same");
                    // }

                    // we should hopefully only have one of each type, but just in case...
                    // todo: cleanup
                    List<ShaderProgramBasket> baskets = [];
                    for (var i = 0; i < vertProgInfos.Count; i++)
                    {
                        baskets.Add(new ShaderProgramBasket(vertInfo, vertProgInfos[i],
                            vertInfo.ParameterBlobIndices.Count > 0 ? (int)vertInfo.ParameterBlobIndices[i] : -1));
                    }
                    for (var i = 0; i < fragProgInfos.Count; i++)
                    {
                        baskets.Add(new ShaderProgramBasket(fragInfo, fragProgInfos[i],
                            fragInfo.ParameterBlobIndices.Count > 0 ? (int)fragInfo.ParameterBlobIndices[i] : -1));
                    }
                    if (baskets.Count > 0)
                        WritePassBody(blobManager, baskets, _sb.GetIndent());

                    /*for (var i = 0; i < vertProgInfos.Count; i++)
                    {
                        var test = blobManager.GetShaderSubProgram((int)vertProgInfos[i].BlobIndex);
                        var keys = test.GlobalKeywords.Concat(test.LocalKeywords).ToArray();
                        if (!(keys.SequenceEqual(["DUMMY"]) || keys.SequenceEqual(["DIRECTIONAL", "DUMMY"])))
                        {
                            continue;
                        }

                        int fragId = -1;
                        for (var j = 0; j < fragProgInfos.Count; j++)
                        {
                            test = blobManager.GetShaderSubProgram((int)fragProgInfos[j].BlobIndex);
                            keys = test.GlobalKeywords.Concat(test.LocalKeywords).ToArray();
                            if (!(keys.SequenceEqual(["DUMMY"]) || keys.SequenceEqual(["DIRECTIONAL", "DUMMY"])))
                            {
                                continue;
                            }

                            fragId = j;
                            break;
                        }

                        List<ShaderProgramBasket> baskets;
                        if (vertInfo.ParameterBlobIndices.Count > 0) //&& fragInfo.ParameterBlobIndices.Count > 0)
                        {
                            baskets = new List<ShaderProgramBasket>
                            {
                                new ShaderProgramBasket(vertInfo, vertProgInfos[i], (int)vertInfo.ParameterBlobIndices[i]),
                                new ShaderProgramBasket(fragInfo, fragProgInfos[fragId], (int)fragInfo.ParameterBlobIndices[fragId])
                            };
                        }
                        else
                        {
                            baskets = new List<ShaderProgramBasket>
                            {
                                new ShaderProgramBasket(vertInfo, vertProgInfos[i], -1),
                                new ShaderProgramBasket(fragInfo, fragProgInfos[fragId], -1)
                            };
                        }
                        WritePassBody(blobManager, baskets, _sb.GetIndent());
                    }*/
                    /*if (vertProgInfos.Count > 0 && fragProgInfos.Count > 0)
                    {
                        for (var i = 0; i < vertProgInfos.Count; i++)
                        {
                            List<ShaderProgramBasket> baskets;
                            if (vertInfo.ParameterBlobIndices.Count > 0 && vertInfo.ParameterBlobIndices.Count > 0)
                            {
                                baskets = new List<ShaderProgramBasket>
                                {
                                    new ShaderProgramBasket(vertInfo, vertProgInfos[i], (int)vertInfo.ParameterBlobIndices[i]),
                                    //new ShaderProgramBasket(fragInfo, fragProgInfos[i], (int)fragInfo.ParameterBlobIndices[i])
                                };
                            }
                            else
                            {
                                baskets = new List<ShaderProgramBasket>
                                {
                                    new ShaderProgramBasket(vertInfo, vertProgInfos[i], -1),
                                    //new ShaderProgramBasket(fragInfo, fragProgInfos[i], -1)
                                };
                            }
                            WritePassBody(blobManager, baskets, _sb.GetIndent());
                        }
                        for (var i = 0; i < fragProgInfos.Count; i++)
                        {
                            List<ShaderProgramBasket> baskets;
                            if (vertInfo.ParameterBlobIndices.Count > 0 && fragInfo.ParameterBlobIndices.Count > 0)
                            {
                                baskets = new List<ShaderProgramBasket>
                                {
                                    new ShaderProgramBasket(fragInfo, fragProgInfos[i], (int)fragInfo.ParameterBlobIndices[i]),
                                    //new ShaderProgramBasket(fragInfo, fragProgInfos[i], (int)fragInfo.ParameterBlobIndices[i])
                                };
                            }
                            else
                            {
                                baskets = new List<ShaderProgramBasket>
                                {
                                    new ShaderProgramBasket(fragInfo, fragProgInfos[i], -1),
                                    //new ShaderProgramBasket(fragInfo, fragProgInfos[i], -1)
                                };
                            }
                            WritePassBody(blobManager, baskets, _sb.GetIndent());
                        }
                    }
                    else if (vertProgInfos.Count > 0 && fragProgInfos.Count == 0)
                    {
                        for (var i = 0; i < vertProgInfos.Count; i++)
                        {
                            List<ShaderProgramBasket> baskets;
                            if (vertInfo.ParameterBlobIndices.Count > 0)
                            {
                                baskets = new List<ShaderProgramBasket>
                                {
                                    new ShaderProgramBasket(vertInfo, vertProgInfos[i], (int)vertInfo.ParameterBlobIndices[i]),
                                };
                            }
                            else
                            {
                                baskets = new List<ShaderProgramBasket>
                                {
                                    new ShaderProgramBasket(vertInfo, vertProgInfos[i], -1),
                                };
                            }
                            WritePassBody(blobManager, baskets, _sb.GetIndent());
                        }
                    }*/
                }
                _sb.Unindent();
                _sb.AppendLine("}");
            }
        }

        private void WritePassState(AssetTypeValueField state)
        {
            var name = state["m_Name"].AsString;
            _sb.AppendLine($"Name \"{name}\"");

            var lod = state["m_LOD"].AsInt;
            if (lod != 0)
            {
                _sb.AppendLine($"LOD {lod}");
            }

            var rtSeparateBlend = state["rtSeparateBlend"].AsBool;
            if (rtSeparateBlend)
            {
                for (var i = 0; i < 8; i++)
                {
                    WritePassRtBlend(state[$"rtBlend{i}"], i);
                }
            }
            else
            {
                WritePassRtBlend(state["rtBlend0"], -1);
            }

            var alphaToMask = state["alphaToMask.val"].AsFloat;
            var zClip = (ZClip)(int)state["zClip.val"].AsFloat;
            var zTest = (ZTest)(int)state["zTest.val"].AsFloat;
            var zWrite = (ZWrite)(int)state["zWrite.val"].AsFloat;
            var culling = (CullMode)(int)state["culling.val"].AsFloat;
            var offsetFactor = state["offsetFactor.val"].AsFloat;
            var offsetUnits = state["offsetUnits.val"].AsFloat;
            var stencilRef = state["stencilRef.val"].AsFloat;
            var stencilReadMask = state["stencilReadMask.val"].AsFloat;
            var stencilWriteMask = state["stencilWriteMask.val"].AsFloat;
            var stencilOpPass = (StencilOp)(int)state["stencilOp.pass.val"].AsFloat;
            var stencilOpFail = (StencilOp)(int)state["stencilOp.fail.val"].AsFloat;
            var stencilOpZfail = (StencilOp)(int)state["stencilOp.zFail.val"].AsFloat;
            var stencilOpComp = (StencilComp)(int)state["stencilOp.comp.val"].AsFloat;
            var stencilOpFrontPass = (StencilOp)(int)state["stencilOpFront.pass.val"].AsFloat;
            var stencilOpFrontFail = (StencilOp)(int)state["stencilOpFront.fail.val"].AsFloat;
            var stencilOpFrontZfail = (StencilOp)(int)state["stencilOpFront.zFail.val"].AsFloat;
            var stencilOpFrontComp = (StencilComp)(int)state["stencilOpFront.comp.val"].AsFloat;
            var stencilOpBackPass = (StencilOp)(int)state["stencilOpBack.pass.val"].AsFloat;
            var stencilOpBackFail = (StencilOp)(int)state["stencilOpBack.fail.val"].AsFloat;
            var stencilOpBackZfail = (StencilOp)(int)state["stencilOpBack.zFail.val"].AsFloat;
            var stencilOpBackComp = (StencilComp)(int)state["stencilOpBack.comp.val"].AsFloat;
            var fogMode = (FogMode)(int)state["fogMode"].AsFloat;
            var fogColorX = state["fogColor.x.val"].AsFloat;
            var fogColorY = state["fogColor.y.val"].AsFloat;
            var fogColorZ = state["fogColor.z.val"].AsFloat;
            var fogColorW = state["fogColor.w.val"].AsFloat;
            var fogDensity = state["fogDensity.val"].AsFloat;
            var fogStart = state["fogStart.val"].AsFloat;
            var fogEnd = state["fogEnd.val"].AsFloat;

            
            var lighting = state["lighting"].AsBool;

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

            var tags = state["m_Tags"]["tags.Array"];
            if (tags.Children.Count > 0)
            {
                _sb.AppendLine("Tags {");
                _sb.Indent();
                {
                    foreach (var tag in tags)
                    {
                        _sb.AppendLine($"\"{tag["first"].AsString}\"=\"{tag["second"].AsString}\"");
                    }
                }
                _sb.Unindent();
                _sb.AppendLine("}");
            }
        }

        private void WritePassRtBlend(AssetTypeValueField rtBlend, int index)
        {
            var srcBlend = (BlendMode)(int)rtBlend["srcBlend.val"].AsFloat;
            var destBlend = (BlendMode)(int)rtBlend["destBlend.val"].AsFloat;
            var srcBlendAlpha = (BlendMode)(int)rtBlend["srcBlendAlpha.val"].AsFloat;
            var destBlendAlpha = (BlendMode)(int)rtBlend["destBlendAlpha.val"].AsFloat;
            var blendOp = (BlendOp)(int)rtBlend["blendOp.val"].AsFloat;
            var blendOpAlpha = (BlendOp)(int)rtBlend["blendOpAlpha.val"].AsFloat;
            var colMask = (ColorWriteMask)(int)rtBlend["colMask.val"].AsFloat;

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

        // todo: move
        private ShaderGpuProgramType GetVertexProgramForPlatform(GPUPlatform gpuPlatform)
        {
            return gpuPlatform switch
            {
                GPUPlatform.d3d11 => ShaderGpuProgramType.DX11VertexSM40,
                GPUPlatform.Switch => ShaderGpuProgramType.Console,
            };
        }

        private ShaderGpuProgramType GetFragmentProgramForPlatform(GPUPlatform gpuPlatform)
        {
            return gpuPlatform switch
            {
                GPUPlatform.d3d11 => ShaderGpuProgramType.DX11PixelSM40,
                GPUPlatform.Switch => ShaderGpuProgramType.Console,
            };
        }
    }
}