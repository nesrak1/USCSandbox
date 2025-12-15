namespace USCSandbox.ShaderCode.USIL;
public static class UsilOptimizerUtil
{
    public static bool DoOpcodesMatch(List<UsilInstruction> insts, int startIndex, UsilInstructionType[] instTypes)
    {
        if (startIndex + instTypes.Length > insts.Count)
        {
            return false;
        }

        for (int i = 0; i < instTypes.Length; i++)
        {
            if (insts[startIndex + i].InstructionType != instTypes[i])
            {
                return false;
            }
        }
        return true;
    }

    public static bool DoMasksMatch(UsilOperand operand, int[] mask)
    {
        if (operand.Mask.Length != mask.Length)
        {
            return false;
        }

        for (int i = 0; i < mask.Length; i++)
        {
            if (operand.Mask[i] != mask[i])
            {
                return false;
            }
        }
        return true;
    }
}
