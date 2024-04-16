namespace USCSandbox.Processor
{
    [Flags]
    public enum ColorWriteMask
    {
        None,
        Alpha = 1,
        Red = 2,
        Green = 4,
        Blue = 8,
        All = Red | Green | Blue | Alpha
    }
}