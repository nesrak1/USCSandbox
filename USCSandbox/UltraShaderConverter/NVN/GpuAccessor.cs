using Ryujinx.Graphics.Shader;
using System.Runtime.InteropServices;

namespace USCSandbox.UltraShaderConverter.NVN
{
    public class GpuAccessor : IGpuAccessor
    {
        private readonly byte[]? _data;

        public GpuAccessor(byte[] data)
        {
            _data = data;
        }

        public ReadOnlySpan<ulong> GetCode(ulong address, int minimumSize)
        {
            return MemoryMarshal.Cast<byte, ulong>(new ReadOnlySpan<byte>(_data)[checked((int)address)..]);
        }
    }
}
