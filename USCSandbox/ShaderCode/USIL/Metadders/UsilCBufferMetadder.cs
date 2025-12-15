using USCSandbox.Metadata;
using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderMetadata;

namespace USCSandbox.ShaderCode.USIL.Metadders;
public class UsilCBufferMetadder : IUsilOptimizer
{
    public bool Run(UShaderProgram shader, ShaderParameters shaderParams)
    {
        List<UsilInstruction> instructions = shader.Instructions;
        foreach (UsilInstruction instruction in instructions)
        {
            if (instruction.DestOperand != null)
            {
                UseMetadata(instruction.DestOperand, shaderParams);
            }

            foreach (UsilOperand operand in instruction.SrcOperands)
            {
                UseMetadata(operand, shaderParams);
            }
        }
        return true; // any changes made?
    }

    private static void UseMetadata(UsilOperand operand, ShaderParameters shaderParams)
    {
        if (operand.OperandType == UsilOperandType.ConstantBuffer)
        {
            int cbRegIdx = operand.RegisterIndex;
            int cbArrIdx = operand.ArrayIndex;

            List<int> operandMaskAddresses = new();
            foreach (int operandMask in operand.Mask)
            {
                operandMaskAddresses.Add(cbArrIdx * 16 + operandMask * 4);
            }

            HashSet<ConstantBufferParameter> cbParams = new HashSet<ConstantBufferParameter>();
            List<int> cbMasks = new List<int>();
            int cbParamIndex = 0;

            ConstantBuffer constantBuffer;
            ConstantBufferBinding? binding = shaderParams.ConstBindings.FirstOrDefault(b => b.Index == cbRegIdx);
            if (binding == null)
            {
                // Fallback. Might work? But probably not reliable.
                if (cbRegIdx < 0 || cbRegIdx >= shaderParams.ConstantBuffers.Count)
                {
                    // I know I know... hopefully we have at least one cbuf
                    constantBuffer = shaderParams.ConstantBuffers[0];
                }
                else
                {
                    constantBuffer = shaderParams.ConstantBuffers[cbRegIdx];
                }
            }
            else
            {
                constantBuffer = shaderParams.ConstantBuffers.First(b => b.Name == binding.Name);
            }

            // Search children fields
            foreach (ConstantBufferParameter param in constantBuffer.CBParams)
            {
                int paramCbStart = param.Index;
                int paramCbElementSize = param.Rows * param.Columns * 4;
                int paramCbTotalSize = param.Rows * param.Columns * 4 * (param.ArraySize == 0 ? 1 : param.ArraySize);
                int paramCbEnd = paramCbStart + paramCbTotalSize;

                foreach (int operandMaskAddress in operandMaskAddresses)
                {
                    if (operandMaskAddress >= paramCbStart && operandMaskAddress < paramCbEnd)
                    {
                        cbParams.Add(param);

                        int maskIndex = (operandMaskAddress - paramCbStart) / 4;
                        if (param.IsMatrix)
                        {
                            maskIndex %= 4;
                        }

                        else if (param.ArraySize > 1)
                        {
                            cbParamIndex = (operandMaskAddress - paramCbStart) / paramCbElementSize;
                            maskIndex -= 4 * cbParamIndex;
                        }

                        cbMasks.Add(maskIndex);
                    }
                }
            }

            // Search children structs and its fields
            foreach (StructParameter stParam in constantBuffer.StructParams)
            {
                foreach (ConstantBufferParameter cbParam in stParam.CBParams)
                {
                    int paramCbStart = cbParam.Index;
                    int paramCbSize = cbParam.Rows * cbParam.Columns * 4;
                    int paramCbEnd = paramCbStart + paramCbSize;

                    foreach (int operandMaskAddress in operandMaskAddresses)
                    {
                        if (operandMaskAddress >= paramCbStart && operandMaskAddress < paramCbEnd)
                        {
                            cbParams.Add(cbParam);

                            int maskIndex = (operandMaskAddress - paramCbStart) / 4;
                            if (cbParam.IsMatrix)
                            {
                                maskIndex %= 4;
                            }
                            cbMasks.Add(maskIndex);
                        }
                    }
                }
            }

            // Multiple params got opto'd into one operation
            if (cbParams.Count > 1)
            {
                operand.OperandType = UsilOperandType.Multiple;
                operand.Children = new UsilOperand[cbParams.Count];

                int i = 0;
                List<string> paramStrs = new List<string>();
                foreach (ConstantBufferParameter param in cbParams)
                {
                    UsilOperand childOperand = new UsilOperand();
                    childOperand.OperandType = UsilOperandType.ConstantBuffer;

                    // apparently switch has column long, whereas directx has row long
                    int maxRowOrColumnLength = Math.Max(param.Rows, param.Columns);

                    childOperand.Mask = MatchMaskToConstantBuffer(operand.Mask, param.Index, maxRowOrColumnLength);
                    childOperand.MetadataName = param.ParamName;
                    childOperand.MetadataNameAssigned = true;
                    childOperand.ArrayRelative = operand.ArrayRelative;
                    childOperand.ArrayIndex -= param.Index / 16;
                    childOperand.MetadataNameWithArray = operand.ArrayRelative != null && (!param.IsMatrix || param.ArraySize > 1);

                    operand.Children[i++] = childOperand;
                }
            }
            else if (cbParams.Count == 1)
            {
                ConstantBufferParameter param = cbParams.First();

                operand.ArrayIndex -= param.Index / 16;

                // apparently switch has column long, whereas directx has row long
                int maxRowOrColumnLength = Math.Max(param.Rows, param.Columns);

                if (param.IsMatrix)
                {
                    if (param.ArraySize > 0)
                    {
                        // example of 4x4 matrix: matrix[5] -> matrix[1][1] (matrix[1]._m01._m11._m21._m31)
                        operand.ArrayRelative = new UsilOperand(operand.ArrayIndex / maxRowOrColumnLength);
                        operand.ArrayIndex %= maxRowOrColumnLength;
                    }

                    operand.OperandType = UsilOperandType.Matrix;
                    operand.TransposeMatrix = true;
                }

                operand.Mask = cbMasks.ToArray();
                operand.MetadataName = param.ParamName;
                operand.MetadataNameAssigned = true;
                operand.MetadataNameWithArray = param.ArraySize > 1;

                if (cbMasks.Count == maxRowOrColumnLength && !param.IsMatrix)
                {
                    operand.DisplayMask = false;
                }
            }
        }
    }

    private static int[] MatchMaskToConstantBuffer(int[] mask, int pos, int size)
    {
        int offset = pos / 4 % 4;
        List<int> result = new List<int>();
        for (int i = 0; i < mask.Length; i++)
        {
            if (mask[i] >= offset && mask[i] < offset + size)
            {
                result.Add(mask[i] - offset);
            }
        }
        return result.ToArray();
    }
}
