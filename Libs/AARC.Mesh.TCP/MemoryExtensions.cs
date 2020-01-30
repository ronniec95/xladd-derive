using System;
using System.IO.Pipelines;

namespace AARC.Mesh.TCP
{
    public static class MemoryExtensions
    {
        private const int INT24SIZE = (sizeof(UInt32) - 1);
        public const int FRAMELENGTHSIZE = INT24SIZE;
        private const int METADATALENGTHSIZE = INT24SIZE;
        public const int MESSAGEFRAMESIZE = INT24SIZE;

        public static Memory<byte> GetMemory(this PipeWriter output, out Memory<byte> memoryframe, bool haslength = false) { memoryframe = output.GetMemory(); return haslength ? memoryframe : memoryframe.Slice(MESSAGEFRAMESIZE); }
    }
}
