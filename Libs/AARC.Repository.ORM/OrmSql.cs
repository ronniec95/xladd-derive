using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AARC.Repository.ORM
{
    using AARC.Utilities;
    public class OrmSql<T> where T : class
    {
        public string InsertSql { get; private set; }
        public string UpdateSql { get; private set; }
        public string SelectSql { get; private set; }
        List<string> _keys;

        public OrmSql(string table)
        {
            _keys = ReflectionHelper.GetKeyAttributeNames(typeof(T));
            var names = ReflectionHelper.GetPropertyNames<T>();
            var insertsql = new StringBuilder($"insert into {table} (");
            var updatesql = new StringBuilder($"update {table} set ");
            var selectsql = new StringBuilder("select ");

            var key = _keys.FirstOrDefault();
            var arguments = new StringBuilder("values (");
            var first = true;
            foreach (var name in names)
                if (name != key)
                {
                    if (first)
                        first = false;
                    else
                    {
                        insertsql.Append(',');
                        updatesql.Append(',');
                        selectsql.Append(',');
                        arguments.Append(',');
                    }
                    var column = $"[{name}]";
                    insertsql.Append(column);
                    selectsql.Append(column);
                    updatesql.AppendLine($"[{name}] = @{name}");
                    arguments.Append($"@{name}");
                }

            selectsql.Append($",{key} from {table}");
            insertsql.AppendLine(")");
            arguments.Append(')');
            insertsql.Append(arguments);
            updatesql.AppendLine($" where [{key}] = @{key}");

            InsertSql = insertsql.ToString();
            UpdateSql = updatesql.ToString();
            SelectSql = selectsql.ToString();
        }
    }
}
