using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.DirectXDisassembler.Blocks;

namespace AssetRipper.Export.Modules.Shaders.UltraShaderConverter.DirectXDisassembler
{
    internal class DirectXDisassemblyHelper
    {
        public static string DisassembleInstruction(SHDRInstruction inst, ref int ifDepth)
        {
            if (ifDepth > 0 && (inst.opcode == Opcode.endif || inst.opcode == Opcode.@else))
            {
                ifDepth -= 2;
            }
            string ifPadding = new string(' ', ifDepth);
            if (inst.opcode == Opcode.@if || inst.opcode == Opcode.@else)
            {
                ifDepth += 2;
            }

            string line = ifPadding + inst.opcode.ToString();
            bool firstOp = true;
            foreach (SHDRInstructionOperand op in inst.operands)
            {
                if (firstOp)
                {
                    line += " ";
                    firstOp = false;
                }
                else
                {
                    line += ", ";
                }

                if (op.swizzle != null && op.swizzle.Length > 0)
                {
                    char[] swizChars = { 'x', 'y', 'z', 'w' };
                    string swizStr = "";
                    for (int i = 0; i < op.swizzle.Length; i++)
                    {
                        swizStr += swizChars[op.swizzle[i]];
                    }

                    line += GetOperandName(op);
                    line += "." + swizStr;
                }
                else
                {
                    line += GetOperandName(op);
                }
            }

            if (SHDRInstruction.IsDeclaration(inst.opcode) && inst.declData != null)
            {
                if (firstOp)
                {
                    line += " ";
                }
                switch (inst.opcode)
                {
                    case Opcode.dcl_globalFlags:
                    {
                        string ps = "";
                        int globalFlags = inst.declData.globalFlags;
                        if ((globalFlags & 1) == 1)
                            ps += ", refactoringAllowed";
                        if ((globalFlags & 2) == 2)
                            ps += ", enableDoublePrecisionFloatOps";
                        if ((globalFlags & 4) == 4)
                            ps += ", forceEarlyDepthStencil";
                        if ((globalFlags & 8) == 8)
                            ps += ", enableRawAndStructuredBuffers";
                        if ((globalFlags & 16) == 16)
                            ps += ", skipOptimization";
                        if ((globalFlags & 32) == 32)
                            ps += ", enableMinimumPrecision";
                        if ((globalFlags & 64) == 64)
                            ps += ", enable11_1DoubleExtensions";
                        if ((globalFlags & 128) == 128)
                            ps += ", enable11_1ShaderExtensions";
                        line += ps.TrimStart(',');
                        break;
                    }
                    case Opcode.dcl_constantbuffer:
                    {
                        if (inst.declData.constantBufferType == ConstantBufferType.DynamicIndexed)
                            line += ", dynamicIndexed";
                        else
                            line += ", immediateIndexed";
                        break;
                    }
                    case Opcode.dcl_temps:
                    {
                        line += inst.declData.numTemps;
                        break;
                    }
                    case Opcode.dcl_input_sgv:
                    case Opcode.dcl_input_ps_sgv:
                    case Opcode.dcl_output_siv:
                    {
                        line += ", " + inst.declData.nameToken;
                        break;
                    }
                    case Opcode.dcl_input_ps:
                    {
                        line = $"{line.Substring(0, line.IndexOf(' '))} {inst.declData.interpolation} {line.Substring(line.IndexOf(' ') + 1)}";
                        break;
                    }
                    case Opcode.dcl_sampler:
                    {
                        line += $"s{inst.declData.samplerIndex},";

                        if (inst.declData.samplerMode == SamplerMode.Default)
                            line += " mode_default";
                        else
                            line += " mode_comparison";
                        break;
                    }
                    //todo : assembleSystemValue
                }
            }

            return line;
        }

        public static string GetOperandName(SHDRInstructionOperand op)
        {
            string prefix = "";
            if (op.extended)
            {
                if (((op.extendedData & 0x40) >> 6) == 1)
                {
                    prefix += "-";
                }
                if (((op.extendedData & 0x80) >> 7) == 1)
                {
                    prefix += "|";
                }
            }
            switch (op.operand)
            {
                case Operand.ConstantBuffer:
                    return $"{prefix}cb{op.arraySizes[0]}[{op.arraySizes[1]}]";
                case Operand.Input:
                    return $"{prefix}v{op.arraySizes[0]}";
                case Operand.Output:
                    if (op.arraySizes.Length == 0) //why does this happen???
                        return $"{prefix}o";
                    else
                        return $"{prefix}o{op.arraySizes[0]}";
                case Operand.Immediate32:
                {
                    if (op.immValues.Length == 1)
                        return $"{prefix}l({(float)op.immValues[0]})";
                    if (op.immValues.Length == 4)
                        return $"{prefix}l({(float)op.immValues[0]}, {(float)op.immValues[1]}, {(float)op.immValues[2]}, {(float)op.immValues[3]})";
                    else
                        return $"{prefix}l()";
                }
                case Operand.Temp:
                    if (op.arraySizes.Length > 0)
                        return $"{prefix}r{op.arraySizes[0]}";
                    else
                        return $"{prefix}r0";
                case Operand.Resource:
                    return $"{prefix}t{op.arraySizes[0]}";
                case Operand.Sampler:
                    return $"{prefix}s{op.arraySizes[0]}";
                default:
                    return prefix + op.operand.ToString();
            }
        }
    }
}
