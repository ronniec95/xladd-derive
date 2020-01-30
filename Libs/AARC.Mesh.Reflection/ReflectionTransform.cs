using System.Collections.Generic;
using System.Reflection;

// Used by DataTransforms (MultipleInputsAttribute)
// Also used by OptioniserForm ()
namespace AARC.Mesh.Reflection
{
    public class ReflectionTransform
    {
        public MethodInfo Method;
        public string Name;
        public List<ReflectionParam> Inputs = new List<ReflectionParam>();
        public List<ReflectionParam> Outputs = new List<ReflectionParam>();
        public bool AllowsMultipleInputs;
    }
}
