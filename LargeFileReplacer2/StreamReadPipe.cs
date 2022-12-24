using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace LargeFileReplacer2
{
    public class StreamReadPipe:ServerPipeliner
    {
        public StreamReadPipe(Stream fileStream)
        {
            Trace.Assert(fileStream.CanRead);
            TotalProgress = fileStream.Length;
            SetReader(new StreamReader(fileStream));
            SetWriter();
        }
        protected override void PostProcess()
        {
            StatusString = "Read-OK";
        }
    }
}
