using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchemaZen.model
{
    public class Role : IScriptable, INameable
    {
        public string Name { get; set; }
        public string Script { get; set; }

        public string ScriptCreate() {
            return Script;
        }
    }
}
