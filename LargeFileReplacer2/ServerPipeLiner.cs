using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;

namespace LargeFileReplacer2
{
    public class ServerPipeliner:Pipeliner
    {
        public string ClientHandleString { get { return pipedServer.GetClientHandleAsString(); } }
        AnonymousPipeServerStream pipedServer;
        protected void SetWriter() { SetWriter(new StreamWriter(pipedServer = new AnonymousPipeServerStream(PipeDirection.Out))); }
    }
}
