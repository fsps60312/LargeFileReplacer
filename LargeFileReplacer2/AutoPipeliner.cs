using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LargeFileReplacer2
{
    class AutoPipeliner:ServerPipeliner
    {
        public AutoPipeliner(string handleString)
        {
            SetReader(handleString);
            SetWriter();
        }
    }
}
