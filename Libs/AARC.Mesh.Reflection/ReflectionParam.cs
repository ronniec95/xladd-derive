using System;

// Used by DataTransforms (MultipleInputsAttribute)
// Also used by OptioniserForm ()
namespace AARC.Mesh.Reflection
{
    // TODO: Actually, will need OUT params, vs Return.. as out params require the variable to be instantiated, and sent in...
    // this is how we get multiple outputs?
    // consider e.g. Decorrelator.DecorrelatedReturnsUisngAlpha(mr, or, out alpha, out beta)
    // might want e.g. formatString - {alpha} {beta} {ticker} etc... --> name for chart?
    // variable view? e.g. can simply plug in any input -> and it displays it as text? (format?)
    public class ReflectionParam
    {
        public string Name;
        public Type Type;
        public bool Optional;

        public static string ShortType(Type type)
        {
            // let's try to make the Type look a bit better for display purposes?
            // Examples: System.Windows.Forms.DataVisualization.Charting.Chart
            // System.Double[]
            // System.Collections.Generic.List`1[AARC.Model.Stock]
            // System.Collections.Generic.IEnumerable`1[System.DateTime]
            // so... for simplicity every time we have a System.Collections.Generic .. 
            // remove System.

            string s = type.ToString();
            s = s.Replace("System.Collections.Generic.", "");
            s = s.Replace("System.", "");
            s = s.Replace("Windows.Forms.DataVisualization.", "");
            s = s.Replace("`1", "");

            return s;
        }

        public override string ToString()
        {
            //return Optional ? $"{Name}:{ShortType()}" : $"{Name}:{ShortType()}*";
            return Optional ? $"{Name}~" : $"{Name}";
        }
    }
}
