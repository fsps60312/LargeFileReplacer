using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace LargeFileReplacer2
{
    public class FileReadPipe:Pipeliner
    {
        public string ClientHandleString { get { return pipedServer.GetClientHandleAsString(); } }
        AnonymousPipeServerStream pipedServer;
        public FileReadPipe(FileStream fileStream)
        {
            Trace.Assert(fileStream.CanRead);
            TotalProgress = fileStream.Length;
            SetReader(new StreamReader(fileStream));
            SetWriter(new StreamWriter(pipedServer = new AnonymousPipeServerStream(PipeDirection.Out)));
        }
    }
}
