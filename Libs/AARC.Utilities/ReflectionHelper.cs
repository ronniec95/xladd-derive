using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace AARC.Utilities
{
    public static class ReflectionHelper
    {
        /// <summary>
        /// Public property comparison
        /// </summary>
        public static bool IsEqualTo<T>(this T a, T b) where T : class
        {
            return a.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).All(prop => a.GetPropertyValue(prop.Name).Equals(b.GetPropertyValue(prop.Name)));
        }

        public static T ClonePublic<T>(T obj) where T : class, new()
        {   // Type type = Nullable.GetUnderlyingType(t.Item2) ?? t.Item2;
            T cloned = new T();
            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                cloned.SetPropertyValue(prop.Name, obj.GetPropertyValue(prop.Name));

            return cloned;
        }

        public static List<string> GetKeyAttributeNames(Type t)
        {
            return t.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(KeyAttribute))).Select(x => x.Name).ToList();
        }

        public static List<string> GetMethodNames(object o, BindingFlags flags, string[] ignoreMethods)
        {
            List<string> ourMethods = new List<string>();

            /*
            You can call MemberInfo.GetCustomAttributes() to get any custom attributes defined on a member of a Type.
            You can get the MemberInfo for the property by doing something like this:
                PropertyInfo prop = typeof(Group).GetProperty("UserExistsInGroup", BindingFlags.Public | BindingFlags.Static);

            var property = typeof(Group).GetProperty("UserExistsInGroup");
            var attribute = property.GetCustomAttributes(typeof(DescriptionAttribute), true)[0];
            var description = (DescriptionAttribute)attribute;
            var text = description.Description;            
            */

            // so, only want methods that 
            // a] return void
            // b] have no parameters OR parameters of type:
            //      System.String ticker, System.DateTime startDate, System.DateTime endDate
            //      System.Boolean timeseries
            //      System.String ticker, System.DateTime startDate



            MethodInfo[] methodInfos = o.GetType().GetMethods(flags);
            foreach (var method in methodInfos)
            {
                // ignore
                if (ignoreMethods.Contains(method.Name))
                    continue;

                var parameters = method.GetParameters();
                string parameterDescriptions = "";

                if (parameters.Any())
                {
                    // decide if the parameters are OK or not...
                    continue;

                    // if ok then...
                    // parameterDescriptions = string.Join(", ", parameters.Select(x => x.ParameterType + " " + x.Name).ToArray());
                }

                /*
                OR
                var descriptions = (DescriptionAttribute[])type.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (descriptions.Length == 0)
                {
                    return null;
                }
                return descriptions[0].Description;
                */

                // TODO: Can put custom attributes - 
                // https://msdn.microsoft.com/en-us/library/sw480ze8.aspx
                // and https://msdn.microsoft.com/en-us/library/84c42s56(v=vs.110).aspx
                // thus = can choose GUI elements to display based upon custom attributes of methods
                // thus can easily add methods without changing GUI manually!??!??

                // any extra description attribute??
                string description = "";
                var customAttributes = method.GetCustomAttributesData();
                if (customAttributes.Count > 0)
                {
                    DescriptionAttribute descriptionAttribute = (DescriptionAttribute)method.GetCustomAttributes(typeof(DescriptionAttribute), true)[0];
                    description = descriptionAttribute.Description;
                    //foreach (var attribute in customAttributes)
                    //    if (attribute.AttributeType == typeof(DescriptionAttribute))
                    //        description = (DescriptionAttribute)attribute.AttributeType;
                }

                Console.WriteLine("{0} {1} ({2}) [{3}]", method.ReturnType, method.Name, parameterDescriptions, description);

                ourMethods.Add(method.Name);
            }

            // need descriptions into labelMethod

            return ourMethods;
        }

        public static List<string> GetPropertyNames<T>() where T : class
        {
            return typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(prop => prop.Name).ToList();
        }

        public static List<Tuple<string, Type>> GetPublicPropertyNamesAndTypes<T>() where T : class, new()
        {
            T obj = new T();
            return obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(prop => new Tuple<string, Type>(prop.Name, prop.PropertyType)).ToList();
        }

        public static object GetPropertyValue(this object x, string propertyName)
        {
            PropertyInfo pi = x.GetType().GetProperty(propertyName);
            return pi?.GetValue(x, null);
        }

        public static DataTable CreateDataTableExpando(List<ExpandoObject> objects, string name)
        {
            DataTable table = new DataTable(name);

            if (objects.Count > 0)
            {
                ExpandoObject o = objects[0];
                foreach (KeyValuePair<string, object> property in o)
                {
                    //Console.WriteLine(property.Key + ": " + property.Value);
                    table.Columns.Add(new DataColumn(property.Key, property.Value.GetType()));
                }
            }

            foreach (ExpandoObject o in objects)
            {
                DataRow dr = table.NewRow();
                foreach (KeyValuePair<string, object> property in o)
                    dr[property.Key] = property.Value;
                table.Rows.Add(dr);
            }

            return table;
        }

        // Note: There might be better/faster ways to do this...
        // e.g. http://stackoverflow.com/questions/564366/convert-generic-list-enumerable-to-datatable
        public static DataTable CreateDataTable<T>(List<T> objects, string name) where T : class, new()
        {
            DataTable table = new DataTable(name);

            List<Tuple<string, Type>> ntList = GetPublicPropertyNamesAndTypes<T>();
            foreach (var t in ntList)
            {
                // handle possible nullable...
                Type type = Nullable.GetUnderlyingType(t.Item2) ?? t.Item2;
                table.Columns.Add(new DataColumn(t.Item1, type));
            }

            foreach (var o in objects)
            {
                DataRow dr = table.NewRow();
                foreach (var t in ntList)
                    dr[t.Item1] = o.GetPropertyValue(t.Item1);
                table.Rows.Add(dr);
            }

            return table;
        }

        public static DataTable CreateDataTableAnon(List<object> objects, string name)
        {
            DataTable table = new DataTable(name);
            List<Tuple<string, Type>> ntList = objects[0].GetType().GetProperties().Select(prop => new Tuple<string, Type>(prop.Name, prop.PropertyType)).ToList();

            foreach (var t in ntList)
            {
                // handle possible nullable...
                Type type = Nullable.GetUnderlyingType(t.Item2) ?? t.Item2;
                table.Columns.Add(new DataColumn(t.Item1, type));
            }

            foreach (var o in objects)
            {
                DataRow dr = table.NewRow();
                foreach (var t in ntList)
                    dr[t.Item1] = o.GetPropertyValue(t.Item1);
                table.Rows.Add(dr);
            }

            return table;
        }

        public static DataTable CreateDataTable<T>(List<T> objects, string name, string[] propertyNames) where T : class, new()
        {
            // Note: insert columns into table in same order as propertyNames!
            DataTable table = new DataTable(name);

            List<DataColumn> columns = new List<DataColumn>();
            List<Tuple<string, Type>> ntList = GetPublicPropertyNamesAndTypes<T>();
            foreach (var t in ntList)
            {
                if (propertyNames.Contains(t.Item1))
                {
                    // handle possible nullable...
                    Type type = Nullable.GetUnderlyingType(t.Item2) ?? t.Item2;
                    //table.Columns.Add();
                    columns.Add(new DataColumn(t.Item1, type));
                }
            }

            foreach (string columnName in propertyNames)
            {
                DataColumn c = columns.FirstOrDefault(x => x.ColumnName == columnName);
                if (c != null)
                    table.Columns.Add(c);
            }

            foreach (var o in objects)
            {
                DataRow dr = table.NewRow();
                foreach (var t in ntList)
                {
                    if (propertyNames.Contains(t.Item1))
                        dr[t.Item1] = o.GetPropertyValue(t.Item1);
                }
                table.Rows.Add(dr);
            }

            return table;
        }
    }

    // todo: put somewhere: might be useful.. e.g. as new ComparisonExtension<T>( (x,y) => some x y comparison )
    // which turns a Comparison<T> into IComparer<T> thus linq to get IComparer<T> for where needed
    public class ExampleUsage
    {
        public static void Example()
        {
            List<int> q = new List<int>() { 1, 2, 3 };
            var s = q.OrderBy(x => x, new ComparisonExtension<int>((i1, i2) => i1.CompareTo(i2)));
            Console.WriteLine(string.Join(",", s.Select(x => x.ToString())));
        }
    }

    public class ComparisonExtension<T> : IComparer<T>
    {
        private readonly Comparison<T> _comparison;

        public ComparisonExtension(Comparison<T> comparison)
        {
            _comparison = comparison;
        }

        public int Compare(T x, T y)
        {
            return _comparison(x, y);
        }
    }

}
