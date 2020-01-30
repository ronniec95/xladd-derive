using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AARC.RDS
{
    public static class MSSQLExtensions
    {
        public static void BulkInsert<T>(DbContext ctx, IEnumerable<T> Entities)
        {
            string connectionString = ctx.Database.GetDbConnection().ConnectionString;
            // Open a sourceConnection to the AdventureWorks database.
        }
    }
}
