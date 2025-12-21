using System.Globalization;
using USCSandbox.ShaderCode.UShader;

namespace USCSandbox.ShaderCode.USIL;
public class UsilOperand
{
    public UsilOperandType OperandType;

    public int[] ImmInt = Array.Empty<int>();
    public float[] ImmFloat = Array.Empty<float>();

    public bool AbsoluteValue;
    public bool Negative;
    public bool TransposeMatrix;

    public int RegisterIndex;
    public int ArrayIndex;
    public UsilOperand? ArrayRelative;

    public string? MetadataName;
    public bool MetadataNameAssigned;
    public bool MetadataNameWithArray;

    public string Comment = string.Empty;

    public UsilOperand[] Children = Array.Empty<UsilOperand>();

    public int[] Mask;
    public bool DisplayMask;

    public UsilOperand()
    {
        OperandType = UsilOperandType.None;

        AbsoluteValue = false;
        Negative = false;
        TransposeMatrix = false;

        RegisterIndex = 0;
        ArrayIndex = 0;
        ArrayRelative = null;

        MetadataName = null;
        MetadataNameAssigned = false;
        MetadataNameWithArray = false;

        Mask = Array.Empty<int>();
        DisplayMask = true;
    }

    public UsilOperand(UsilOperand original)
    {
        OperandType = original.OperandType;

        ImmInt = original.ImmInt;
        ImmFloat = original.ImmFloat;

        AbsoluteValue = original.AbsoluteValue;
        Negative = original.Negative;
        TransposeMatrix = original.TransposeMatrix;

        RegisterIndex = original.RegisterIndex;
        ArrayIndex = original.ArrayIndex;

        if (original.ArrayRelative != null)
        {
            ArrayRelative = new UsilOperand(original.ArrayRelative);
        }
        else
        {
            ArrayRelative = null;
        }

        MetadataName = original.MetadataName;
        MetadataNameAssigned = original.MetadataNameAssigned;
        MetadataNameWithArray = original.MetadataNameWithArray;

        Comment = original.Comment;

        Children = new UsilOperand[original.Children.Length];
        for (int i = 0; i < original.Children.Length; i++)
        {
            Children[i] = new UsilOperand(original.Children[i]);
        }

        Mask = original.Mask.ToArray();
        DisplayMask = original.DisplayMask;
    }

    public UsilOperand(int value)
    {
        OperandType = UsilOperandType.ImmediateInt;
        ImmInt = new[] { value };
        Mask = new[] { 0 };
    }

    public UsilOperand(float value)
    {
        OperandType = UsilOperandType.ImmediateFloat;
        ImmFloat = new[] { value };
        Mask = new[] { 0 };
    }

    public int GetValueCount()
    {
        switch (OperandType)
        {
            case UsilOperandType.ImmediateFloat:
                return ImmFloat.Length;
            case UsilOperandType.ImmediateInt:
                return ImmInt.Length;
            case UsilOperandType.Multiple:
                int multipleSum = 0;
                foreach (UsilOperand operand in Children)
                {
                    multipleSum += operand.GetValueCount();
                }
                return multipleSum;
            default:
                return Mask.Length;
        }
    }

    public override string ToString()
    {
        return ToString(false);
    }

    public string ToString(bool forceHideMask)
    {
        string prefix = "";
        string body = "";
        string suffix = "";

        bool displayMaskOverride = DisplayMask;
        if (forceHideMask)
        {
            displayMaskOverride = false;
        }

        if (!MetadataNameAssigned)
        {
            prefix = GetTypeShortForm(OperandType);
        }

        if (AbsoluteValue)
        {
            prefix = $"abs({prefix}";
        }

        if (Negative)
        {
            prefix = $"-{prefix}";
        }

        if (MetadataNameAssigned)
        {
            body = MetadataName ?? "";
        }
        else
        {
            switch (OperandType)
            {
                case UsilOperandType.None:
                {
                    body = "none";
                    break;
                }
                case UsilOperandType.Null:
                {
                    body = "null";
                    break;
                }
                case UsilOperandType.Comment:
                {
                    body = Comment;
                    break;
                }
                case UsilOperandType.TempRegister:
                case UsilOperandType.InputRegister:
                case UsilOperandType.OutputRegister:
                case UsilOperandType.ResourceRegister:
                case UsilOperandType.SamplerRegister:
                case UsilOperandType.Sampler2D:
                case UsilOperandType.Sampler3D:
                case UsilOperandType.SamplerCube:
                {
                    body = $"{RegisterIndex}";
                    break;
                }
                case UsilOperandType.IndexableTempRegister:
                {
                    body = $"{RegisterIndex}[{ArrayIndex}]";
                    break;
                }
                case UsilOperandType.ConstantBuffer:
                case UsilOperandType.Matrix:
                {
                    body = $"{RegisterIndex}";
                    break;
                }
                case UsilOperandType.ImmediateConstantBuffer:
                {
                    body = "";
                    break;
                }
                case UsilOperandType.ImmediateInt:
                {
                    if (ImmInt.Length == 1)
                    {
                        body = $"{ImmInt[0]}";
                    }
                    else
                    {
                        body += $"int{ImmInt.Length}(";
                        for (int i = 0; i < ImmInt.Length; i++)
                        {
                            if (i != ImmInt.Length - 1)
                            {
                                body += $"{ImmInt[i]}, ";
                            }
                            else
                            {
                                body += $"{ImmInt[i]}";
                            }
                        }
                        body += ")";
                    }
                    break;
                }
                case UsilOperandType.ImmediateFloat:
                {
                    if (ImmFloat.Length == 1)
                    {
                        // todo: check if number can't possibly be expressed as float and write in hex.
                        // todo: float precision isn't correct atm. add precision check somewhere.
                        body = $"{ImmFloat[0].ToString("0.0#######", CultureInfo.InvariantCulture)}";
                    }
                    else
                    {
                        // todo: if all numbers are the same and it matches the mask, use it only once
                        body += $"float{ImmFloat.Length}(";
                        for (int i = 0; i < ImmFloat.Length; i++)
                        {
                            if (i != ImmFloat.Length - 1)
                            {
                                body += $"{ImmFloat[i].ToString("0.0#######", CultureInfo.InvariantCulture)}, ";
                            }
                            else
                            {
                                body += $"{ImmFloat[i].ToString("0.0#######", CultureInfo.InvariantCulture)}";
                            }
                        }
                        body += ")";
                    }
                    break;
                }
                case UsilOperandType.Multiple:
                {
                    body += $"float{GetValueCount()}({string.Join(", ", Children.ToList())})";
                    break;
                }

                default:
                {
                    if (HlslNamingUtils.HasSpecialInputOutputName(OperandType))
                    {
                        body = HlslNamingUtils.GetSpecialInputOutputName(OperandType);
                    }
                    break;
                }
            }
        }

        if (!MetadataNameAssigned || MetadataNameWithArray)
        {
            switch (OperandType)
            {
                case UsilOperandType.ConstantBuffer:
                case UsilOperandType.Matrix:
                {
                    if (ArrayRelative != null)
                    {
                        if (ArrayIndex == 0)
                        {
                            body += $"[{ArrayRelative}]";
                        }
                        else
                        {
                            body += $"[{ArrayRelative} + {ArrayIndex}]";
                        }
                    }
                    else
                    {
                        body += $"[{ArrayIndex}]";
                    }
                    break;
                }
                case UsilOperandType.ImmediateConstantBuffer:
                {
                    body += $"[{ArrayRelative} + {ArrayIndex}]";
                    break;
                }
            }
        }

        if (OperandType != UsilOperandType.ImmediateFloat &&
            OperandType != UsilOperandType.ImmediateInt &&
            OperandType != UsilOperandType.Multiple &&
            !HlslNamingUtils.HasSpecialInputOutputName(OperandType) &&
            displayMaskOverride)
        {
            if (Mask.Length > 0)
            {
                suffix += ".";
            }

            if (OperandType == UsilOperandType.Matrix)
            {
                string[] charArray = TransposeMatrix ? UsilConstants.TMATRIX_MASK_CHARS : UsilConstants.MATRIX_MASK_CHARS;
                for (int i = 0; i < Mask.Length; i++)
                {
                    suffix += charArray[ArrayIndex * 4 + Mask[i]];
                }
            }
            else
            {
                for (int i = 0; i < Mask.Length; i++)
                {
                    suffix += UsilConstants.MASK_CHARS[Mask[i]];
                }
            }

            if (suffix == ".xyzw")
            {
                suffix = "";
            }
        }

        if (AbsoluteValue)
        {
            suffix = $"{suffix})";
        }

        return $"{prefix}{body}{suffix}";
    }

    public static string GetTypeShortForm(UsilOperandType operandType)
    {
        return operandType switch
        {
            UsilOperandType.TempRegister => "tmp",
            UsilOperandType.IndexableTempRegister => "xtmp",
            UsilOperandType.InputRegister => "in",
            UsilOperandType.OutputRegister => "out",
            UsilOperandType.ResourceRegister => "rsc",
            UsilOperandType.SamplerRegister => "smp",
            UsilOperandType.ConstantBuffer => "cb",
            UsilOperandType.ImmediateConstantBuffer => "icb",
            _ => ""
        };
    }
}
