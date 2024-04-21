using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.UShader.Function;
using USCSandbox.Processor;

namespace AssetRipper.Export.Modules.Shaders.UltraShaderConverter.USIL.Metadders
{
    public class USILCBufferMetadder : IUSILOptimizer
    {
        public bool Run(UShaderProgram shader, ShaderParams shaderParams)
        {
            List<USILInstruction> instructions = shader.instructions;
            foreach (USILInstruction instruction in instructions)
            {
                if (instruction.destOperand != null)
                {
                    UseMetadata(instruction.destOperand, shaderParams);
                }

                foreach (USILOperand operand in instruction.srcOperands)
                {
                    UseMetadata(operand, shaderParams);
                }
            }
            return true; // any changes made?
        }

        private static void UseMetadata(USILOperand operand, ShaderParams shaderParams)
        {
            if (operand.operandType == USILOperandType.ConstantBuffer)
            {
                int cbRegIdx = operand.registerIndex;
                int cbArrIdx = operand.arrayIndex;

                List<int> operandMaskAddresses = new();
                foreach (int operandMask in operand.mask)
                {
                    operandMaskAddresses.Add(cbArrIdx * 16 + operandMask * 4);
                }

                HashSet<ConstantBufferParameter> cbParams = new HashSet<ConstantBufferParameter>();
                List<int> cbMasks = new List<int>();
                int cbParamIndex = 0;

                ConstantBuffer constantBuffer;
                BufferBinding? binding = shaderParams.ConstBindings.FirstOrDefault(b => b.Index == cbRegIdx);
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

                            if (param.ArraySize > 1)
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
                    operand.operandType = USILOperandType.Multiple;
                    operand.children = new USILOperand[cbParams.Count];

                    int i = 0;
                    List<string> paramStrs = new List<string>();
                    foreach (ConstantBufferParameter param in cbParams)
                    {
                        USILOperand childOperand = new USILOperand();
                        childOperand.operandType = USILOperandType.ConstantBuffer;

                        // apparently switch has column long, whereas directx has row long
                        int maxRowOrColumnLength = Math.Max(param.Rows, param.Columns);

                        childOperand.mask = MatchMaskToConstantBuffer(operand.mask, param.Index, maxRowOrColumnLength);
                        childOperand.metadataName = param.ParamName;
                        childOperand.metadataNameAssigned = true;
                        childOperand.arrayRelative = operand.arrayRelative;
                        childOperand.arrayIndex -= param.Index / 16;
                        childOperand.metadataNameWithArray = operand.arrayRelative != null && (!param.IsMatrix || param.ArraySize > 1);

                        operand.children[i++] = childOperand;
                    }
                }
                else if (cbParams.Count == 1)
                {
                    ConstantBufferParameter param = cbParams.First();

                    // Matrix
                    if (param.IsMatrix)
                    {
                        //int matrixIdx = cbArrIdx - param.Index / 16;

                        operand.operandType = USILOperandType.Matrix;
                        //operand.arrayIndex = matrixIdx;
                        operand.transposeMatrix = true;
                    }
                    //else
                    //{
                    operand.arrayIndex -= param.Index / 16;
                    //}

                    operand.mask = cbMasks.ToArray();
                    operand.metadataName = param.ParamName;
                    operand.metadataNameAssigned = true;
                    operand.metadataNameWithArray = param.ArraySize > 1;

                    // apparently switch has column long, whereas directx has row long
                    int maxRowOrColumnLength = Math.Max(param.Rows, param.Columns);
                    if (cbMasks.Count == maxRowOrColumnLength && !param.IsMatrix)
                    {
                        operand.displayMask = false;
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
}
