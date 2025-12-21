using System.Globalization;
using System.Text;
using USCSandbox.ShaderCode.USIL;

namespace USCSandbox.ShaderCode.UShader;
public class UShaderFunctionToHlsl
{
    private UShaderProgram _shader;
    private readonly StringBuilder _stringBuilder = new();
    private string _baseIndent;
    private string _indent;
    private int _indentLevel;

    private delegate void InstHandler(UsilInstruction inst);
    private Dictionary<UsilInstructionType, InstHandler> _instructionHandlers;

    public UShaderFunctionToHlsl(UShaderProgram shader, int indentDepth)
    {
        _shader = shader;

        _baseIndent = new string(' ', indentDepth * 4);
        _indent = new string(' ', 4);

        _instructionHandlers = new()
        {
            { UsilInstructionType.Move, new InstHandler(HandleMove) },
            { UsilInstructionType.MoveConditional, new InstHandler(HandleMoveConditional) },
            { UsilInstructionType.Add, new InstHandler(HandleAdd) },
            { UsilInstructionType.Subtract, new InstHandler(HandleSubtract) },
            { UsilInstructionType.Multiply, new InstHandler(HandleMultiply) },
            { UsilInstructionType.Divide, new InstHandler(HandleDivide) },
            { UsilInstructionType.MultiplyAdd, new InstHandler(HandleMultiplyAdd) },
            { UsilInstructionType.And, new InstHandler(HandleAnd) },
            { UsilInstructionType.Or, new InstHandler(HandleOr) },
            { UsilInstructionType.Xor, new InstHandler(HandleXor) },
            { UsilInstructionType.Not, new InstHandler(HandleNot) },
            { UsilInstructionType.Minimum, new InstHandler(HandleMinimum) },
            { UsilInstructionType.Maximum, new InstHandler(HandleMaximum) },
            { UsilInstructionType.SquareRoot, new InstHandler(HandleSquareRoot) },
            { UsilInstructionType.SquareRootReciprocal, new InstHandler(HandleSquareRootReciprocal) },
            { UsilInstructionType.Logarithm2, new InstHandler(HandleLogarithm2) },
            { UsilInstructionType.ToThePower, new InstHandler(HandleToThePower) },
            { UsilInstructionType.Reciprocal, new InstHandler(HandleReciprocal) },
            { UsilInstructionType.Fractional, new InstHandler(HandleFractional) },
            { UsilInstructionType.Floor, new InstHandler(HandleFloor) },
            { UsilInstructionType.Ceiling, new InstHandler(HandleCeiling) },
            { UsilInstructionType.Round, new InstHandler(HandleRound) },
            { UsilInstructionType.Truncate, new InstHandler(HandleTruncate) },
            { UsilInstructionType.IntToFloat, new InstHandler(HandleIntToFloat) },
            { UsilInstructionType.FloatToInt, new InstHandler(HandleFloatToInt) },
            // { UsilInstructionType.FloatToUInt, new InstHandler(HandleFloatToInt) },
            { UsilInstructionType.Negate, new InstHandler(HandleNegate) },
            { UsilInstructionType.Clamp, new InstHandler(HandleClamp) },
            { UsilInstructionType.Sine, new InstHandler(HandleSine) },
            { UsilInstructionType.Cosine, new InstHandler(HandleCosine) },
            { UsilInstructionType.ShiftLeft, new InstHandler(HandleShiftLeft) },
            { UsilInstructionType.ShiftRight, new InstHandler(HandleShiftRight) },
            { UsilInstructionType.DotProduct2, new InstHandler(HandleDotProduct) },
            { UsilInstructionType.DotProduct3, new InstHandler(HandleDotProduct) },
            { UsilInstructionType.DotProduct4, new InstHandler(HandleDotProduct) },
            { UsilInstructionType.Sample, new InstHandler(HandleSample) },
            { UsilInstructionType.SampleComparison, new InstHandler(HandleSample) },
            { UsilInstructionType.SampleComparisonLODZero, new InstHandler(HandleSample) },
            { UsilInstructionType.SampleLOD, new InstHandler(HandleSampleLOD) },
            { UsilInstructionType.SampleDerivative, new InstHandler(HandleSampleDerivative) },
            { UsilInstructionType.LoadResource, new InstHandler(HandleLoadResource) },
            { UsilInstructionType.LoadResourceMultisampled, new InstHandler(HandleLoadResource) },
            { UsilInstructionType.LoadResourceStructured, new InstHandler(HandleLoadResourceStructured) },
            { UsilInstructionType.Discard, new InstHandler(HandleDiscard) },
            { UsilInstructionType.ResourceDimensionInfo, new InstHandler(HandleResourceDimensionInfo) },
            { UsilInstructionType.SampleCountInfo, new InstHandler(HandleSampleCountInfo) },
            { UsilInstructionType.GetDimensions, new InstHandler(HandleResourceDimensionInfo) },
            { UsilInstructionType.DerivativeRenderTargetX, new InstHandler(HandleDerivativeRenderTarget) },
            { UsilInstructionType.DerivativeRenderTargetY, new InstHandler(HandleDerivativeRenderTarget) },
            { UsilInstructionType.DerivativeRenderTargetXCoarse, new InstHandler(HandleDerivativeRenderTarget) },
            { UsilInstructionType.DerivativeRenderTargetYCoarse, new InstHandler(HandleDerivativeRenderTarget) },
            { UsilInstructionType.DerivativeRenderTargetXFine, new InstHandler(HandleDerivativeRenderTarget) },
            { UsilInstructionType.DerivativeRenderTargetYFine, new InstHandler(HandleDerivativeRenderTarget) },
            { UsilInstructionType.IfFalse, new InstHandler(HandleIf) },
            { UsilInstructionType.IfTrue, new InstHandler(HandleIf) },
            { UsilInstructionType.Else, new InstHandler(HandleElse) },
            { UsilInstructionType.EndIf, new InstHandler(HandleEndIf) },
            { UsilInstructionType.Loop, new InstHandler(HandleLoop) },
            { UsilInstructionType.EndLoop, new InstHandler(HandleEndLoop) },
            { UsilInstructionType.Break, new InstHandler(HandleBreak) },
            { UsilInstructionType.Continue, new InstHandler(HandleContinue) },
            { UsilInstructionType.ForLoop, new InstHandler(HandleForLoop) },
            { UsilInstructionType.Switch, new InstHandler(HandleSwitch) },
            { UsilInstructionType.Case, new InstHandler(HandleCase) },
            { UsilInstructionType.Default, new InstHandler(HandleDefault) },
            { UsilInstructionType.EndSwitch, new InstHandler(HandleEndSwitch) },
            { UsilInstructionType.Equal, new InstHandler(HandleEqual) },
            { UsilInstructionType.NotEqual, new InstHandler(HandleNotEqual) },
            { UsilInstructionType.LessThan, new InstHandler(HandleLessThan) },
            { UsilInstructionType.LessThanOrEqual, new InstHandler(HandleLessThanOrEqual) },
            { UsilInstructionType.GreaterThan, new InstHandler(HandleGreaterThan) },
            { UsilInstructionType.GreaterThanOrEqual, new InstHandler(HandleGreaterThanOrEqual) },
            { UsilInstructionType.Return, new InstHandler(HandleReturn) },
            // extra
            { UsilInstructionType.MultiplyMatrixByVector, new InstHandler(MultiplyMatrixByVector) },
            { UsilInstructionType.Comment, new InstHandler(HandleComment) }
        };
    }

    public string WriteStruct()
    {
        _stringBuilder.Clear();

        if (_shader.ShaderFunctionType == UShaderFunctionType.Vertex)
        {
            AppendLine("struct appdata");
            AppendLine("{");
            _indentLevel++;
            foreach (UsilInputOutput input in _shader.Inputs)
            {
                AppendLine($"{input.Format} {input.Name} : {input.Type};");
            }
            _indentLevel--;
            AppendLine("};");

            AppendLine("struct v2f");
            AppendLine("{");
            _indentLevel++;
            foreach (UsilInputOutput output in _shader.Outputs)
            {
                AppendLine($"{output.Format} {output.Name} : {output.Type};");
            }
            _indentLevel--;
            AppendLine("};");
        }
        else if (_shader.ShaderFunctionType == UShaderFunctionType.Fragment)
        {
            AppendLine("struct fout");
            AppendLine("{");
            _indentLevel++;
            foreach (UsilInputOutput output in _shader.Outputs)
            {
                AppendLine($"{output.Format} {output.Name} : {output.Type};");
            }
            _indentLevel--;
            AppendLine("};");
        }

        return _stringBuilder.ToString();
    }

    public string WriteFunction()
    {
        _stringBuilder.Clear();

        WriteFunctionDefinition();
        {
            WriteLocals();
            foreach (UsilInstruction inst in _shader.Instructions)
            {
                if (_instructionHandlers.ContainsKey(inst.InstructionType))
                {
                    _instructionHandlers[inst.InstructionType](inst);
                }
            }
        }
        _indentLevel--;
        AppendLine("}");

        return _stringBuilder.ToString();
    }

    private void WriteFunctionDefinition()
    {
        if (_shader.ShaderFunctionType == UShaderFunctionType.Vertex)
        {
            AppendLine($"{UsilConstants.VERT_TO_FRAG_STRUCT_NAME} vert(appdata {UsilConstants.VERT_INPUT_NAME})");
        }
        else
        {
            var frontFace = _shader.Inputs.FirstOrDefault(i => i.Type == "SV_IsFrontFace");
            string args = $"{UsilConstants.VERT_TO_FRAG_STRUCT_NAME} {UsilConstants.FRAG_INPUT_NAME}";
            if (frontFace is not null)
            {
                // not part of v2f
                args += $", {frontFace.Format} {frontFace.Name}: VFACE";
            }
            AppendLine($"{UsilConstants.FRAG_OUTPUT_STRUCT_NAME} frag({args})");
        }
        AppendLine("{");
        _indentLevel++;
    }

    private void WriteLocals()
    {
        foreach (UsilLocal local in _shader.Locals)
        {
            if (local.DefaultValues.Count > 0 && local.IsArray)
            {
                AppendLine($"{local.Type} {local.Name}[{local.DefaultValues.Count}] = {{");
                if (local.DefaultValues.Count > 0)
                {
                    _indentLevel++;
                    for (int i = 0; i < local.DefaultValues.Count; i++)
                    {
                        UsilOperand operand = local.DefaultValues[i];
                        string comma = i != local.DefaultValues.Count - 1 ? "," : "";
                        AppendLine($"{operand}{comma}");
                    }
                    _indentLevel--;
                }
                AppendLine("};");
            }
            else
            {
                AppendLine($"{local.Type} {local.Name};");
            }
        }
    }

    private void HandleMove(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = WrapSaturate(inst, $"{srcOps[0]}");
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleMoveConditional(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = WrapSaturate(inst, $"{srcOps[0]} ? {srcOps[1]} : {srcOps[2]}");
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleAdd(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = WrapSaturate(inst, $"{srcOps[0]} + {srcOps[1]}");
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleSubtract(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = WrapSaturate(inst, $"{srcOps[0]} - {srcOps[1]}");
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleMultiply(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = WrapSaturate(inst, $"{srcOps[0]} * {srcOps[1]}");
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleDivide(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = WrapSaturate(inst, $"{srcOps[0]} / {srcOps[1]}");
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleMultiplyAdd(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = WrapSaturate(inst, $"{srcOps[0]} * {srcOps[1]} + {srcOps[2]}");
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleAnd(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        int op0UintSize = srcOps[0].GetValueCount();
        int op1UintSize = srcOps[1].GetValueCount();
        string value = $"uint{op0UintSize}({srcOps[0]}) & uint{op1UintSize}({srcOps[1]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleOr(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        int op0UintSize = srcOps[0].GetValueCount();
        int op1UintSize = srcOps[1].GetValueCount();
        string value = $"uint{op0UintSize}({srcOps[0]}) | uint{op1UintSize}({srcOps[1]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleXor(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        int op0UintSize = srcOps[0].GetValueCount();
        int op1UintSize = srcOps[1].GetValueCount();
        string value = $"uint{op0UintSize}({srcOps[0]}) ^ uint{op1UintSize}({srcOps[1]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleNot(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        int op0UintSize = srcOps[0].GetValueCount();
        string value = $"~uint{op0UintSize}({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleMinimum(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"min({srcOps[0]}, {srcOps[1]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleMaximum(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"max({srcOps[0]}, {srcOps[1]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleSquareRoot(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"sqrt({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleSquareRootReciprocal(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"rsqrt({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleLogarithm2(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"log({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleToThePower(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"pow({srcOps[0]}, {srcOps[1]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleReciprocal(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"rcp({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleFractional(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"frac({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleFloor(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"floor({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleCeiling(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"ceil({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleRound(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"round({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleTruncate(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"trunc({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleIntToFloat(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"floor({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleFloatToInt(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"asint({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleNegate(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"-{srcOps[0]}";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleClamp(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = inst.InstructionType == UsilInstructionType.ClampUInt
            ? $"clamp(uint({srcOps[0]}), uint({srcOps[1]}), uint({srcOps[2]}))"
            : $"clamp({srcOps[0]}, {srcOps[1]}, {srcOps[2]})";

        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleSine(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"sin({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleCosine(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"cos({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleShiftLeft(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        UsilOperand srcOp0 = srcOps[0];
        UsilOperand srcOp1 = srcOps[1];

        // temp fix to prevent compile errors, still inaccurate
        int op0IntSize = srcOp0.GetValueCount();
        int op1IntSize = srcOp1.GetValueCount();

        string op0Text, op1Text;

        if (srcOp0.OperandType == UsilOperandType.ImmediateInt)
        {
            op0Text = $"{srcOp0}";
        }
        else
        {
            op0Text = $"int{op0IntSize}({srcOp0})";
        }

        if (srcOp1.OperandType == UsilOperandType.ImmediateInt)
        {
            op1Text = $"{srcOp1}";
        }
        else
        {
            op1Text = $"int{op1IntSize}({srcOp1})";
        }

        string value = $"float{op0IntSize}({op0Text} << {op1Text})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleShiftRight(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        UsilOperand srcOp0 = srcOps[0];
        UsilOperand srcOp1 = srcOps[1];

        // temp fix to prevent compile errors, still inaccurate
        int op0IntSize = srcOp0.GetValueCount();
        int op1IntSize = srcOp1.GetValueCount();

        string op0Text, op1Text;

        if (srcOp0.OperandType == UsilOperandType.ImmediateInt)
        {
            op0Text = $"{srcOp0}";
        }
        else
        {
            op0Text = $"int{op0IntSize}({srcOp0})";
        }

        if (srcOp1.OperandType == UsilOperandType.ImmediateInt)
        {
            op1Text = $"{srcOp1}";
        }
        else
        {
            op1Text = $"int{op1IntSize}({srcOp1})";
        }

        string value = $"float{op0IntSize}({op0Text} >> {op1Text})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleDotProduct(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = WrapSaturate(inst, $"dot({srcOps[0]}, {srcOps[1]})");
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleSample(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        UsilOperand textureOperand = srcOps[2];
        int samplerTypeIdx = inst.InstructionType == UsilInstructionType.Sample ? 3 : 4;
        bool samplerType = srcOps[samplerTypeIdx].ImmInt[0] == 1;
        string args = $"{srcOps[2]}, {srcOps[0]}";
        string value;
        if (!samplerType)
        {
            value = textureOperand.OperandType switch
            {
                UsilOperandType.Sampler2D => $"tex2D({args})",
                UsilOperandType.Sampler3D => $"tex3D({args})",
                UsilOperandType.SamplerCube => $"texCUBE({args})",
                UsilOperandType.Sampler2DArray => $"UNITY_SAMPLE_TEX2DARRAY({args})",
                UsilOperandType.SamplerCubeArray => $"UNITY_SAMPLE_TEXCUBEARRAY({args})",
                _ => $"texND({args})" // unknown real type
            };
        }
        else
        {
            args = $"{srcOps[2]}, {args}";
            value = textureOperand.OperandType switch
            {
                UsilOperandType.Sampler2D => $"UNITY_SAMPLE_TEX2D_SAMPLER({args})",
                UsilOperandType.Sampler3D => $"UNITY_SAMPLE_TEX3D_SAMPLER({args})",
                UsilOperandType.SamplerCube => $"UNITY_SAMPLE_TEXCUBE_SAMPLER({args})",
                UsilOperandType.Sampler2DArray => $"UNITY_SAMPLE_TEX2DARRAY_SAMPLER({args})",
                UsilOperandType.SamplerCubeArray => $"UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER({args})",
                _ => $"texND({args})" // unknown real type
            };
        }
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleSampleLOD(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        UsilOperand textureOperand = srcOps[2];
        bool samplerType = srcOps[4].ImmInt[0] == 1;
        string args;
        if (srcOps[0].Mask.Length == 2) // texture2d
        {
            args = $"{srcOps[2]}, float4({srcOps[0]}, 0, {srcOps[3]})";
        }
        else
        {
            args = $"{srcOps[2]}, float4({srcOps[0]}, {srcOps[3]})";
        }

        string value;
        if (!samplerType)
        {
            value = textureOperand.OperandType switch
            {
                UsilOperandType.Sampler2D => $"tex2Dlod({args})",
                UsilOperandType.Sampler3D => $"tex3Dlod({args})",
                UsilOperandType.SamplerCube => $"texCUBElod({args})",
                UsilOperandType.Sampler2DArray => $"UNITY_SAMPLE_TEX2DARRAY_LOD({args})",
                UsilOperandType.SamplerCubeArray => $"UNITY_SAMPLE_TEXCUBEARRAY_LOD({args})",
                _ => $"texNDlod({args})" // unknown real type
            };
        }
        else
        {
            args = $"{srcOps[2]}, {args}";
            value = textureOperand.OperandType switch
            {
                UsilOperandType.Sampler2D => $"UNITY_SAMPLE_TEX2D_SAMPLER({args})",
                UsilOperandType.Sampler3D => $"UNITY_SAMPLE_TEX3D_SAMPLER({args})",
                UsilOperandType.SamplerCube => $"UNITY_SAMPLE_TEXCUBE_SAMPLER({args})",
                UsilOperandType.Sampler2DArray => $"UNITY_SAMPLE_TEX2DARRAY_SAMPLER({args})",
                UsilOperandType.SamplerCubeArray => $"UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER({args})",
                _ => $"texND({args})" // unknown real type
            };
        }
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleSampleDerivative(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        UsilOperand textureOperand = srcOps[2];
        string value;
        string args = $"{srcOps[2]}, {srcOps[0]}, {srcOps[3]}, {srcOps[4]}";
        value = textureOperand.OperandType switch
        {
            UsilOperandType.Sampler2D => $"tex2Dgrad({args})",
            UsilOperandType.Sampler3D => $"tex3Dgrad({args})",
            UsilOperandType.SamplerCube => $"texCUBEgrad({args})",
            _ => $"texNDgrad({args})" // unknown real type
        };
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleLoadResource(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string args = $"{srcOps[1]}, {srcOps[0]}";
        string value = $"Load({args})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleLoadResourceStructured(UsilInstruction inst)
    {
        // todo (won't work because struct doesn't exist)
        // DXDecompiler: ((float4[arraySize])_Buffer.Load(srcAddress))[srcByteOffset / 16];
        // 3DMigoto: _Buffer[srcAddress].val[srcByteOffset/4]; (with /4 literally part of the output lmao)
        // yo idk
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"((float4[1]){srcOps[2]}.Load({srcOps[0]}))[{srcOps[1].ImmInt[0] / 16}]";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleDiscard(UsilInstruction inst)
    {
        string comment = CommentString(inst);
        AppendLine($"{comment}discard;");
    }

    private void HandleResourceDimensionInfo(UsilInstruction inst)
    {
        // assumes resinfo_extra exists
        List<UsilOperand> srcOps = inst.SrcOperands;

        UsilOperand usilResource = srcOps[0];
        UsilOperand usilMipLevel = srcOps[1];
        UsilOperand usilWidth = srcOps[2];
        UsilOperand usilHeight = srcOps[3];
        UsilOperand usilDepthOrArraySize = srcOps[4];
        UsilOperand usilMipCount = srcOps[5];

        List<string> args = new List<string>();

        if (usilMipLevel.ImmFloat[0] == 0 && usilMipCount.OperandType == UsilOperandType.Null)
        {
            // shorter version (not checking the compiler did this correctly!)
            args.Add(usilWidth.ToString());

            if (usilHeight.OperandType != UsilOperandType.Null)
            {
                args.Add(usilHeight.ToString());
            }

            if (usilDepthOrArraySize.OperandType != UsilOperandType.Null)
            {
                args.Add(usilDepthOrArraySize.ToString());
            }
        }
        else
        {
            args.Add(usilMipLevel.ToString());
            args.Add(usilWidth.ToString());

            if (usilHeight.OperandType != UsilOperandType.Null)
            {
                args.Add(usilHeight.ToString());
            }

            if (usilDepthOrArraySize.OperandType != UsilOperandType.Null)
            {
                args.Add(usilDepthOrArraySize.ToString());
            }

            if (usilMipCount.OperandType != UsilOperandType.Null)
            {
                args.Add(usilMipCount.ToString());
            }
            else
            {
                args.Add("resinfo_extra");
            }
        }

        string call = $"GetDimensions({string.Join(", ", args)})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{usilResource}.{call};");
    }

    private void HandleSampleCountInfo(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"{srcOps[0]} = GetRenderTargetSampleCount()";
        string comment = CommentString(inst);
        AppendLine($"{comment}{value};");
    }

    private void HandleDerivativeRenderTarget(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string fun = inst.InstructionType switch
        {
            UsilInstructionType.DerivativeRenderTargetX => "ddx",
            UsilInstructionType.DerivativeRenderTargetY => "ddy",
            UsilInstructionType.DerivativeRenderTargetXCoarse => "ddx_coarse",
            UsilInstructionType.DerivativeRenderTargetYCoarse => "ddy_coarse",
            UsilInstructionType.DerivativeRenderTargetXFine => "ddx_fine",
            UsilInstructionType.DerivativeRenderTargetYFine => "ddy_fine",
            _ => "dd?"
        };
        string value = $"{fun}({srcOps[0]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleIf(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string comment = CommentString(inst);
        if (inst.InstructionType == UsilInstructionType.IfTrue)
        {
            AppendLine($"{comment}if ({srcOps[0]}) {{");
        }
        else
        {
            AppendLine($"{comment}if (!({srcOps[0]})) {{");
        }

        _indentLevel++;
    }

    private void HandleElse(UsilInstruction inst)
    {
        _indentLevel--;
        string comment = CommentString(inst);
        AppendLine($"{comment}}} else {{");
        _indentLevel++;
    }

    private void HandleEndIf(UsilInstruction inst)
    {
        _indentLevel--;
        string comment = CommentString(inst);
        AppendLine($"{comment}}}");
    }

    private void HandleLoop(UsilInstruction inst)
    {
        // this can create bad optos and should be
        // replaced with USILXXXLoopOptimizer if possible.
        string comment = CommentString(inst);
        AppendLine($"{comment}while (true) {{");
        _indentLevel++;
    }

    private void HandleEndLoop(UsilInstruction inst)
    {
        _indentLevel--;
        string comment = CommentString(inst);
        AppendLine($"{comment}}}");
    }

    private void HandleBreak(UsilInstruction inst)
    {
        string comment = CommentString(inst);
        AppendLine($"{comment}break;");
    }

    private void HandleContinue(UsilInstruction inst)
    {
        string comment = CommentString(inst);
        AppendLine($"{comment}continue;");
    }

    private void HandleForLoop(UsilInstruction inst)
    {
        string comment = CommentString(inst);

        UsilOperand iterRegOp = inst.SrcOperands[0];
        UsilOperand compOp = inst.SrcOperands[1];
        UsilInstructionType compType = (UsilInstructionType)inst.SrcOperands[2].ImmInt[0];
        UsilNumberType numberType = (UsilNumberType)inst.SrcOperands[3].ImmInt[0];
        float addCount = inst.SrcOperands[4].ImmFloat[0]; // todo use an int instead of float when int incremented?
        int depth = inst.SrcOperands[5].ImmInt[0];

        string numberTypeName = numberType switch
        {
            UsilNumberType.Float => "float",
            UsilNumberType.Int => "int",
            UsilNumberType.UnsignedInt => "unsigned int",
            _ => "?"
        };

        string iterName = depth < UsilConstants.ITER_CHARS.Length
            ? UsilConstants.ITER_CHARS[depth].ToString()
            : $"iter{depth}";

        string compText = compType switch
        {
            UsilInstructionType.Equal => "==",
            UsilInstructionType.NotEqual => "!=",
            UsilInstructionType.GreaterThan => ">",
            UsilInstructionType.GreaterThanOrEqual => ">=",
            UsilInstructionType.LessThan => "<",
            UsilInstructionType.LessThanOrEqual => "<=",
            _ => "?"
        };

        AppendLine(
            $"{comment}for ({numberTypeName} {iterName} = {iterRegOp}; " +
            $"{iterName} {compText} {compOp}; " +
            $"{iterName} += {addCount.ToString(CultureInfo.InvariantCulture)}) {{"
        );

        _indentLevel++;
    }

    private void HandleSwitch(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string comment = CommentString(inst);
        AppendLine($"{comment}switch ({srcOps[0]}) {{");

        _indentLevel++;
    }

    private void HandleCase(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string comment = CommentString(inst);
        AppendLine($"{comment}case {srcOps[0]}:");
    }

    private void HandleDefault(UsilInstruction inst)
    {
        string comment = CommentString(inst);
        AppendLine($"{comment}default:");
    }

    private void HandleEndSwitch(UsilInstruction inst)
    {
        _indentLevel--;
        string comment = CommentString(inst);
        AppendLine($"{comment}}}");
    }

    private void HandleEqual(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"{srcOps[0]} == {srcOps[1]}";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleNotEqual(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"{srcOps[0]} != {srcOps[1]}";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleLessThan(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"{srcOps[0]} < {srcOps[1]}";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleLessThanOrEqual(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"{srcOps[0]} <= {srcOps[1]}";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleGreaterThan(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"{srcOps[0]} > {srcOps[1]}";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleGreaterThanOrEqual(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"{srcOps[0]} >= {srcOps[1]}";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleReturn(UsilInstruction inst)
    {
        string outputName = _shader.ShaderFunctionType switch
        {
            UShaderFunctionType.Vertex => UsilConstants.VERT_OUTPUT_LOCAL_NAME,
            UShaderFunctionType.Fragment => UsilConstants.FRAG_OUTPUT_LOCAL_NAME,
            _ => "o" // ?
        };

        string value = $"return {outputName}";
        string comment = CommentString(inst);
        AppendLine($"{comment}{value};");
    }

    private void MultiplyMatrixByVector(UsilInstruction inst)
    {
        List<UsilOperand> srcOps = inst.SrcOperands;
        string value = $"mul({srcOps[0]}, {srcOps[1]})";
        string comment = CommentString(inst);
        AppendLine($"{comment}{inst.DestOperand} = {value};");
    }

    private void HandleComment(UsilInstruction inst)
    {
        AppendLine($"//{inst.DestOperand.Comment};");
    }

    private string WrapSaturate(UsilInstruction inst, string str)
    {
        if (inst.Saturate)
        {
            str = $"saturate({str})";
        }
        return str;
    }

    private void AppendLine(string line)
    {
        _stringBuilder.Append(_baseIndent);

        for (int i = 0; i < _indentLevel; i++)
        {
            _stringBuilder.Append(_indent);
        }

        _stringBuilder.AppendLine(line);
    }

    // this is awful
    private string CommentString(UsilInstruction inst)
    {
        return inst.Commented ? "//" : "";
    }
}
