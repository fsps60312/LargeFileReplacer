using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace LargeFileReplacer2
{
    public class StreamWritePipe:Pipeliner
    {
        public StreamWritePipe(string handleString,Stream fileStream)
        {
            Trace.Assert(fileStream.CanWrite);
            SetReader(handleString);
            SetWriter(new StreamWriter(fileStream));
        }
        protected override void PostProcess()
        {
            StatusString = "Write-OK";
        }
    }
}
