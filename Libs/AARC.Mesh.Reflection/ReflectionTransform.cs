using System;
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
        public readonly List<ReflectionParam> Inputs = new List<ReflectionParam>();
        public readonly List<ReflectionParam> Outputs = new List<ReflectionParam>();
        
        public bool AllowsMultipleInputs;

        public object[] Invoke(params object[] parametersArray)
        {
            // invoke the method now.. which involes some trickery if it's generic - do that later
            if (Method.ContainsGenericParameters)
                throw new NotImplementedException();

            // Note: Method is always supposed to be static - otherwise we need an object
            // Note: which we can organise later - e.g. provide the (instance) object in this Invoke call
            System.Diagnostics.Debug.Assert(Method.IsStatic);

            object result = Method.Invoke(null, parametersArray);

            // get the out parameters, and insert everything into the object array
            List<object> outObjects = new List<object>(Outputs.Count);

            // this should ensure that the outputObjects are in the same order as the Outputs
            foreach (var rp in Outputs)
                outObjects.Add(rp.Return ? result : parametersArray[rp.Position]);

            return outObjects.ToArray();
        }
    }
}
