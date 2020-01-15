using System;
namespace AARC.Graph.Test
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
