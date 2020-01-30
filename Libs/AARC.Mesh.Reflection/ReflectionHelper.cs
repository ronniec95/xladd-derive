using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

// Used by DataTransforms (MultipleInputsAttribute)
// Also used by OptioniserForm ()
namespace AARC.Mesh.Reflection
{

    public class ReflectionHelper
    {
        public static void TestReflectionStuff<T>()
        {
            var ts = GetTransforms(typeof(T));
            //var ts = GetTransforms(typeof(PriceReturnsManipulator));
            StringBuilder sb = new StringBuilder();

            Console.WriteLine("Transforms:");
            foreach (var t in ts)
            {
                sb.Clear();
                sb.AppendLine();
                sb.AppendFormat(" {0}{1}", t.Name, Environment.NewLine);
                sb.Append(" Inputs: {");
                foreach (var p in t.Inputs)
                {
                    sb.AppendFormat(p.Optional ? " [{0}:{1}] " : " {0}:{1} ", p.Name, p.Type.ToString().Replace("System.", ""));
                }
                sb.AppendLine("}");
                sb.Append(" Outputs: ");
                foreach (var p in t.Outputs)
                {
                    sb.AppendFormat("{{ {0}:{1} }}", p.Name, p.Type.ToString().Replace("System.", ""));
                }
                Console.WriteLine(sb.ToString());
            }
            Console.WriteLine();
        }

        public static T[] GetInstanceArray<T>(int num)
        {
            T[] a = new T[num];
            for (int i = 0; i < num; i++)
            {
                Type t = typeof(T);
                T s = (T)Activator.CreateInstance(t);
                a[i] = s;
            }

            return a;
        }

        public static T GetInstance<T>()
        {
            Type t = typeof(T);
            T s = (T)Activator.CreateInstance(t);
            return s;
        }

        //public static void TestCode()
        //{
        //    Task<object> task = Task.Factory.StartNew(() => CallMethod(typeof (Program), "Bob", new List<object>()))
        //        .ContinueWith(previous => CallMethod(typeof (Program), "Bob", new List<object>{ previous.Result }))
        //        .ContinueWith(previous => CallMethod(typeof (Program), "Bob", new List<object> { previous.Result }));

        //    task.Wait();

        //    Console.WriteLine(task.Result);
        //}

        public static object CallMethod(MethodInfo method, List<object> arguments)
        {
            return method.Invoke(null, arguments.Count > 0 ? arguments.ToArray() : null);
        }

        public static object CallMethod(Type type, string methodName, List<object> arguments)
        {
            List<object> typeNameAndArguments = new List<object>();
            typeNameAndArguments.Add(type);
            typeNameAndArguments.Add(methodName);
            if (arguments != null)
                typeNameAndArguments.AddRange(arguments);

            return CallMethod(typeNameAndArguments);
        }

        public static object CallMethod(List<object> typeNameAndArguments)
        {
            Type type = typeNameAndArguments[0] as Type;
            string methodName = typeNameAndArguments[1] as string;
            object[] arguments = typeNameAndArguments.Count > 2 ? typeNameAndArguments.Skip(2).ToArray() : null;

            if (type == null || methodName == null)
                return null;

            // now need to get the correct version of the method using the provided types of the parameters... to avoid ambiguity..
            // http://stackoverflow.com/questions/1969411/avoiding-an-ambiguous-match-exception
            // https://msdn.microsoft.com/en-us/library/6hy0h0z1.aspx

            object result;
            if (arguments != null)
            {
                // has arguments
                Type[] types = arguments.Select(x => x.GetType()).ToArray();
                var method = type.GetMethod(methodName, types);
                result = method.Invoke(null, arguments);
            }
            else
            {
                // no arguments - use empty types...
                var method = type.GetMethod(methodName, Type.EmptyTypes);
                result = method.Invoke(null, null);
            }

            return result;
        }

        public static List<ReflectionTransform> GetTransforms(Type type)
        {
            List<ReflectionTransform> transforms = new List<ReflectionTransform>();

            // get the methods of t..
            // must specify BindingFlags.Instance or BindingFlags.Static in order to get a return
            MethodInfo[] methodInfos = type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            foreach (var method in methodInfos)
            {
                ReflectionTransform transform = new ReflectionTransform();
                transform.Method = method;
                transform.Name = method.Name;

                // Check for custom Attribute - allowing multiple inputs
                if (method.GetCustomAttributes(typeof(MultipleInputsAttribute), false).Length > 0)
                {
                    MultipleInputsAttribute mia = method.GetCustomAttributes(typeof(MultipleInputsAttribute), false)[0] as MultipleInputsAttribute;
                    if (mia != null)
                        transform.AllowsMultipleInputs = mia.AllowsMultipleInputs;
                }

                var parameters = method.GetParameters();
                foreach (var param in parameters)
                {
                    ReflectionParam p = new ReflectionParam { Name = param.Name, Type = param.ParameterType };
                    if (param.IsOptional)
                        p.Optional = true;

                    if (param.IsOut || param.IsRetval)
                        transform.Outputs.Add(p);
                    else
                        transform.Inputs.Add(p);
                }

                if (method.ReturnParameter != null)
                {
                    ReflectionParam ret = new ReflectionParam();
                    ret.Name = method.ReturnParameter.Name;
                    if (string.IsNullOrEmpty(ret.Name))
                        ret.Name = "return";
                    ret.Type = method.ReturnParameter.ParameterType;
                    transform.Outputs.Add(ret);
                }

                transforms.Add(transform);
            }

            return transforms;
        }

        //public static ReflectionTransform GeTransform()
        //{

        //}
    }
}
