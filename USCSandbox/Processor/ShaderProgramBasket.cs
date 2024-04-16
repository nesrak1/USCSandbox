namespace USCSandbox.Processor
{
    public class ShaderProgramBasket
    {
        public SerializedProgramInfo ProgramInfo;
        public SerializedSubProgramInfo SubProgramInfo;
        public int ParameterBlobIndex;

        public ShaderProgramBasket(
            SerializedProgramInfo programInfo, SerializedSubProgramInfo subProgramInfo, int parameterBlobIndex)
        {
            ProgramInfo = programInfo;
            SubProgramInfo = subProgramInfo;
            ParameterBlobIndex = parameterBlobIndex;
        }
    }
}