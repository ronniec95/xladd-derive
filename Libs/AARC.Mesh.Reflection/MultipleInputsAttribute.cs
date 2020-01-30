using System;
namespace AARC.Mesh.Reflection
{
    [AttributeUsage(AttributeTargets.Method)]//AttributeTargets.All)]
    public class MultipleInputsAttribute : Attribute
    {
        public MultipleInputsAttribute(bool allowsMultipleInputs)
        {
            AllowsMultipleInputs = allowsMultipleInputs;
        }

        // named parameter
        public bool AllowsMultipleInputs { get; set; }
    }
}
