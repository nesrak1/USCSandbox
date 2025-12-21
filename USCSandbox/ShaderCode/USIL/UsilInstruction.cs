using System.Text;

namespace USCSandbox.ShaderCode.USIL;
public class UsilInstruction
{
    public UsilInstructionType InstructionType;
    public UsilOperand? DestOperand;
    public List<UsilOperand> SrcOperands = [];
    public bool Saturate;
    public bool Commented;

    public bool IsIntVariant;
    public bool IsIntUnsigned;

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(InstructionType.ToString());

        sb.Append(' ');

        if (Saturate)
        {
            sb.Append("saturate(");
        }

        if (DestOperand != null)
        {
            sb.Append(DestOperand.ToString());

            if (SrcOperands.Count > 0)
            {
                sb.Append(", ");
            }
        }

        sb.Append(string.Join(", ", SrcOperands));

        if (Saturate)
        {
            sb.Append(')');
        }

        return sb.ToString();
    }

    // todo: all of them
    public bool IsComparisonType()
    {
        switch (InstructionType)
        {
            case UsilInstructionType.Equal:
            case UsilInstructionType.NotEqual:
            case UsilInstructionType.LessThan:
            case UsilInstructionType.LessThanOrEqual:
            case UsilInstructionType.GreaterThan:
            case UsilInstructionType.GreaterThanOrEqual:
                return true;
            default:
                return false;
        }
    }

    public bool IsSampleType()
    {
        switch (InstructionType)
        {
            case UsilInstructionType.Sample:
            case UsilInstructionType.SampleComparison:
            case UsilInstructionType.SampleLOD:
            case UsilInstructionType.SampleLODBias:
            case UsilInstructionType.SampleComparisonLODZero:
            case UsilInstructionType.SampleDerivative:
                return true;
            default:
                return false;
        }
    }
}
