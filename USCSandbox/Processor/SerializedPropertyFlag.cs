namespace USCSandbox.Processor
{
    [Flags]
    public enum SerializedPropertyFlag : uint
    {
        None,
        HideInInspector = 1 << 0,
        PerRendererData = 1 << 1,
        NoScaleOffset = 1 << 2,
        Normal = 1 << 3,
        HDR = 1 << 4,
        Gamma = 1 << 5,
        NonModifiableTextureData = 1 << 6,
        MainTexture = 1 << 7,
        MainColor = 1 << 8
    }
}