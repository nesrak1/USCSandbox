using Ryujinx.Graphics.Shader;
using System.Runtime.InteropServices;

namespace USCSandbox.ShaderCode.Converters.NVN
{
    public class GpuAccessor : IGpuAccessor
    {
        private const int DefaultArrayLength = 32;

        private readonly byte[] _data;

        private int _texturesCount;
        private int _imagesCount;

        public GpuAccessor(byte[] data)
        {
            _data = data;
            _texturesCount = 0;
            _imagesCount = 0;
        }

        public SetBindingPair CreateConstantBufferBinding(int index)
        {
            return new SetBindingPair(0, index + 1);
        }

        public SetBindingPair CreateImageBinding(int count, bool isBuffer)
        {
            int binding = _imagesCount;

            _imagesCount += count;

            return new SetBindingPair(3, binding);
        }

        public SetBindingPair CreateStorageBufferBinding(int index)
        {
            return new SetBindingPair(1, index);
        }

        public SetBindingPair CreateTextureBinding(int count, bool isBuffer)
        {
            int binding = _texturesCount;

            _texturesCount += count;

            return new SetBindingPair(2, binding);
        }

        public ReadOnlySpan<ulong> GetCode(ulong address, int minimumSize)
        {
            return MemoryMarshal.Cast<byte, ulong>(new ReadOnlySpan<byte>(_data)[(int)address..]);
        }

        public int QuerySamplerArrayLengthFromPool()
        {
            return DefaultArrayLength;
        }

        public int QueryTextureArrayLengthFromBuffer(int slot)
        {
            return DefaultArrayLength;
        }

        public int QueryTextureArrayLengthFromPool()
        {
            return DefaultArrayLength;
        }
    }
}
