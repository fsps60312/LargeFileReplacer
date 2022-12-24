using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;

namespace LargeFileReplacer2
{
    class ReplacePipe:AutoPipeliner
    {
        public TargetsDef Targets;
        public string replaceTo = "";
        public ReplacePipe(string handleString,TargetsDef targets) : base(handleString)
        {
            this.Targets = targets;
        }
        protected override void EatChunk(char[] buffer, int n)
        {
            for(int i=0;i<n;i++)
            {
                if (Targets.IsMatch(buffer[i])) Write(replaceTo);
                else Write(buffer[i]);
            }
            Flush();
        }
    }
}
