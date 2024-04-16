using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.DirectXDisassembler;
using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.DirectXDisassembler.Blocks;
using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.USIL;
using USCSandbox.Processor;

namespace AssetRipper.Export.Modules.Shaders.UltraShaderConverter.UShader.DirectX
{
    public static class DXShaderNamingUtils
    {
        public static string GetConstantBufferParamTypeName(ConstantBufferParameter param) => GetConstantBufferParamTypeName(param.Rows, param.Columns, param.ParamType, true);

        public static string GetComponentTypeName(VertexComponent component, bool isVert)
        {
            return component switch
            {
                VertexComponent.None => "NONE",
                VertexComponent.Vertex => isVert ? "POSITION" : "SV_POSITION",
                VertexComponent.Color => "COLOR",
                VertexComponent.Normal => "NORMAL",
                VertexComponent.TexCoord => "TEXCOORD",
                VertexComponent.TexCoord0 => "TEXCOORD0",
                VertexComponent.TexCoord1 => "TEXCOORD1",
                VertexComponent.TexCoord2 => "TEXCOORD2",
                VertexComponent.TexCoord3 => "TEXCOORD3",
                VertexComponent.TexCoord4 => "TEXCOORD4",
                VertexComponent.TexCoord5 => "TEXCOORD5",
                VertexComponent.TexCoord6 => "TEXCOORD6",
                VertexComponent.TexCoord7 => "TEXCOORD7",
                VertexComponent.Attrib0 => "ATTRIB0",
                VertexComponent.Attrib1 => "ATTRIB1",
                VertexComponent.Attrib2 => "ATTRIB2",
                VertexComponent.Attrib3 => "ATTRIB3",
                VertexComponent.Attrib4 => "ATTRIB4",
                VertexComponent.Attrib5 => "ATTRIB5",
                VertexComponent.Attrib6 => "ATTRIB6",
                VertexComponent.Attrib7 => "ATTRIB7",
                VertexComponent.Attrib8 => "ATTRIB8",
                VertexComponent.Attrib9 => "ATTRIB9",
                VertexComponent.Attrib10 => "ATTRIB10",
                VertexComponent.Attrib11 => "ATTRIB11",
                VertexComponent.Attrib12 => "ATTRIB12",
                VertexComponent.Attrib13 => "ATTRIB13",
                VertexComponent.Attrib14 => "ATTRIB14",
                VertexComponent.Attrib15 => "ATTRIB15",
                _ => throw new NotSupportedException()
            };
        }

        public static string GetComponentName(VertexComponent component)
        {
            // where is tangent?
            return component switch
            {
                VertexComponent.None => "none",
                VertexComponent.Vertex => "vertex",
                VertexComponent.Color => "color",
                VertexComponent.Normal => "normal",
                VertexComponent.TexCoord => "tc",
                VertexComponent.TexCoord0 => "tc",
                VertexComponent.TexCoord1 => "tc2",
                VertexComponent.TexCoord2 => "tc3",
                VertexComponent.TexCoord3 => "tc4",
                VertexComponent.TexCoord4 => "tc4",
                VertexComponent.TexCoord5 => "tc5",
                VertexComponent.TexCoord6 => "tc6",
                VertexComponent.TexCoord7 => "tc7",
                VertexComponent.Attrib0 => "at0",
                VertexComponent.Attrib1 => "at1",
                VertexComponent.Attrib2 => "at2",
                VertexComponent.Attrib3 => "at3",
                VertexComponent.Attrib4 => "at4",
                VertexComponent.Attrib5 => "at5",
                VertexComponent.Attrib6 => "at6",
                VertexComponent.Attrib7 => "at7",
                VertexComponent.Attrib8 => "at8",
                VertexComponent.Attrib9 => "at9",
                VertexComponent.Attrib10 => "at10",
                VertexComponent.Attrib11 => "at11",
                VertexComponent.Attrib12 => "at12",
                VertexComponent.Attrib13 => "at13",
                VertexComponent.Attrib14 => "at14",
                VertexComponent.Attrib15 => "at15",
                _ => throw new NotSupportedException()
            };
        }

        public static string GetConstantBufferParamTypeName(int rowCount, int columnCount, ShaderParamType paramType, bool isMatrix)
        {
            string name = $"unknownType";
            string baseName = paramType.ToString().ToLower();

            // sometimes it's column long, sometimes it's row long
            if (columnCount == 1)
            {
                if (rowCount == 1)
                {
                    name = $"{baseName}";
                }

                if (rowCount == 2)
                {
                    name = $"{baseName}2";
                }

                if (rowCount == 3)
                {
                    name = $"{baseName}3";
                }

                if (rowCount == 4)
                {
                    name = $"{baseName}4";
                }
            }
            else if (columnCount == 2)
            {
                if (rowCount == 1)
                {
                    name = $"{baseName}2";
                }
            }
            else if (columnCount == 3)
            {
                if (rowCount == 1)
                {
                    name = $"{baseName}3";
                }
            }
            else if (columnCount == 4)
            {
                if (rowCount == 4 && isMatrix)
                {
                    name = $"{baseName}4x4";
                }
                else if (rowCount == 1)
                {
                    name = $"{baseName}4";
                }
            }

            return name;
        }

        public static string GetISGNInputName(ISGN.Input input)
        {
            string type;
            if (input.index > 0)
            {
                type = input.name + input.index;
            }
            else
            {
                type = input.name;
            }

            string name = input.name switch
            {
                "SV_POSITION" => "position",
                "SV_Position" => "position",
                "SV_IsFrontFace" => "facing",
                "POSITION" => "vertex",
                _ => type.ToLower(),
            };
            return name;
        }

        public static string GetOSGNOutputName(OSGN.Output output)
        {
            string type;
            if (output.index > 0)
            {
                type = output.name + output.index;
            }
            else
            {
                type = output.name;
            }

            if (HasSpecialInputOutputName(output.name))
            {
                return GetSpecialInputOutputName(output.name);
            }

            string name = output.name switch
            {
                "SV_POSITION" => "position",
                "POSITION" => "vertex",
                _ => type.ToLower(),
            };

            return name;
        }

        public static bool HasSpecialInputOutputName(string typeName) => GetSpecialInputOutputName(typeName) != string.Empty;
        public static string GetSpecialInputOutputName(string typeName)
        {
            switch (typeName)
            {
                case "SV_Depth":
                {
                    return "oDepth";
                }
                case "SV_Coverage":
                {
                    return "oMask";
                }
                case "SV_DepthGreaterEqual":
                {
                    return "oDepthGE";
                }
                case "SV_DepthLessEqual":
                {
                    return "oDepthLE";
                }
                case "SV_StencilRef":
                {
                    return "oStencilRef"; // not in 3dmigoto
                }
            }

            return string.Empty;
        }

        public static bool HasSpecialInputOutputName(USILOperandType operandType) => GetSpecialInputOutputName(operandType) != string.Empty;
        public static string GetSpecialInputOutputName(USILOperandType operandType)
        {
            switch (operandType)
            {
                case USILOperandType.InputCoverageMask:
                {
                    return "vCoverage";
                }
                case USILOperandType.InputThreadGroupID:
                {
                    return "vThreadGroupID";
                }
                case USILOperandType.InputThreadID:
                {
                    return "vThreadID";
                }
                case USILOperandType.InputThreadIDInGroup:
                {
                    return "vThreadIDInGroup";
                }
                case USILOperandType.InputThreadIDInGroupFlattened:
                {
                    return "vThreadIDInGroupFlattened";
                }
                case USILOperandType.InputPrimitiveID:
                {
                    return "vPrim";
                }
                case USILOperandType.InputForkInstanceID:
                {
                    return "vForkInstanceID";
                }
                case USILOperandType.InputGSInstanceID:
                {
                    return "vGSInstanceID";
                }
                case USILOperandType.InputDomainPoint:
                {
                    return "vDomain";
                }
                case USILOperandType.OutputControlPointID:
                {
                    return "outputControlPointID"; // not in 3dmigoto
                }
                case USILOperandType.OutputDepth:
                {
                    return "oDepth";
                }
                case USILOperandType.OutputCoverageMask:
                {
                    return "oMask";
                }
                case USILOperandType.OutputDepthGreaterEqual:
                {
                    return "oDepthGE";
                }
                case USILOperandType.OutputDepthLessEqual:
                {
                    return "oDepthLE";
                }
                case USILOperandType.StencilRef:
                {
                    return "oStencilRef"; // not in 3dmigoto
                }
            }

            return string.Empty;
        }

        public static string GetISGNFormatName(ISGN.Input input)
        {
            int maskSize = GetMaskSize(input.mask);
            return ((FormatType)input.format).ToString() + (maskSize != 1 ? maskSize : "");
        }

        public static string GetOSGNFormatName(OSGN.Output output)
        {
            int maskSize = GetMaskSize(output.mask);
            return ((FormatType)output.format).ToString() + (maskSize != 1 ? maskSize : "");
        }

        public static int GetMaskSize(byte mask)
        {
            int p = 0;
            for (int i = 0; i < 4; i++)
            {
                if ((mask >> i & 1) == 1)
                {
                    p++;
                }
            }
            return p;
        }
    }
}
