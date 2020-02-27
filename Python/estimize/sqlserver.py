import pyodbc
from collections import namedtuple

connection_string = "driver={ODBC Driver 17 for SQL Server};server=ROSE.arvixe.com;database=aarc_dev_ab;UID=aarc_dev_rc;PWD=30percent"

# USE[aarc_dev_ab]
# GO

# /****** Object:  Table[dbo].[ServiceStats]    Script Date: 03/01/2020 16: 24: 37 ** ****/
# SET ANSI_NULLS ON
# GO

# SET QUOTED_IDENTIFIER ON
# GO

# CREATE TABLE[dbo].[ServiceStats](
#     [Instance][nvarchar](255) NOT NULL,
#     [Start][datetime] NULL,
#     [Updated][datetime] NULL,
#     [Service][nvarchar](32) NOT NULL,
#     [Status][nvarchar](16) NOT NULL,
#     [Symbol][nvarchar](16) NOT NULL,
#     [NewRows][int] NOT NULL,
#     [UpdatedRows][int] NOT NULL,
#     [UnchangedRows][int] NOT NULL,
#     [TotalRows][int] NOT NULL,
#     [Message][nvarchar](255) NULL,
#     CONSTRAINT[PK_dbo.ServiceStats] PRIMARY KEY CLUSTERED
#     (
#         [Instance] ASC
#     )WITH(PAD_INDEX=OFF, STATISTICS_NORECOMPUTE=OFF, IGNORE_DUP_KEY=OFF, ALLOW_ROW_LOCKS=ON, ALLOW_PAGE_LOCKS=ON) ON[PRIMARY]
# ) ON[PRIMARY]
# GO

TableDef = namedtuple('TableDef', "name columns")


def conn():
    return pyodbc.connect(connection_string)


def check_table(conn, table_def):
    cursor = conn.cursor()
    cursor.execute("USE[aarc_dev_ab]")
    cursor.execute("""IF OBJECT_ID(N'{}', N'U') IS NOT NULL
                      SELECT 1 AS res ELSE SELECT 0 AS res""".format(table_def.name))
    res = cursor.fetchone()
    print("table exists {} {}".format(table_def.name, res))
    return res


def create_table(conn, table_def):
    cursor = conn.cursor()
    cursor.execute("USE[aarc_dev_ab]")
    name = table_def.name
    columns = table_def.columns
    if check_table(conn, table_def)[0] == 1:
        return
    create_str = "CREATE TABLE[dbo].[{}](".format(name)
    for (name1, type) in columns:
        if type == "nvarchar":
            create_str += "[{}][{}](255),".format(name1, type)
        else:
            create_str += "[{}][{}],".format(name1, type)
    create_str += "CONSTRAINT[PK_dbo.{}] PRIMARY KEY CLUSTERED ([{}],[{}] ASC) WITH(PAD_INDEX=OFF, STATISTICS_NORECOMPUTE=OFF, IGNORE_DUP_KEY=OFF, ALLOW_ROW_LOCKS=ON, ALLOW_PAGE_LOCKS=ON) ON[PRIMARY]".format(
        name, columns[0][0], columns[1][0])
    create_str += ") ON [PRIMARY]"
    print(create_str)
    cursor.execute(create_str)
    conn.commit()


def insert(conn, table_def, row):
    cursor = conn.cursor()
    cursor.execute("USE[aarc_dev_ab]")
    name = table_def.name
    columns = table_def.columns
    insert_str = "INSERT INTO [dbo].[{}] (".format(name)
    insert_str += ",".join((name for (name, _) in columns)) + ")"
    insert_str += " VALUES ("
    insert_str += ",".join(('NULL' if col is None else str(col) if type(col).__name__ == "int" or type(
        col).__name__ == 'float' else "'" + col + "'" for col in row)) + ")"
    print(insert_str)
    cursor.execute(insert_str)


def select():
    pass


def delete(conn, table_def, row):
    cursor = conn.cursor()
    cursor.execute("USE[aarc_dev_ab]")
    name = table_def.name
    columns = table_def.columns
    value = row[0] if type(row[0]).__name__ == "int" or type(
        row[0]).__name__ == 'float' else "'" + row[0] + "'"
    value1 = row[1] if type(row[1]).__name__ == "int" or type(
        row[1]).__name__ == 'float' else "'" + row[1] + "'"
    delete_str = "DELETE FROM [dbo].[{}] WHERE {} = {} AND {} = {}".format(
        name, columns[0][0], value, columns[1][0], value1)
    print(delete_str)
    cursor.execute(delete_str)


if __name__ == "__main__":
    table = TableDef("foo", [('id', 'int'), ('value', 'nvarchar')])
    # create_table(table)
    conn = pyodbc.connect(connection_string)
    # insert(conn, table, [234, 'bar'])
    # delete(conn, table, [234])
    print(check_table(conn, table))
    conn.commit()
