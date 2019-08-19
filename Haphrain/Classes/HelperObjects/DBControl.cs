using IBM.Data.DB2.Core;

namespace Haphrain.Classes.HelperObjects
{
    internal static class DBControl
    {
        static DB2ConnectionStringBuilder sBuilder = new DB2ConnectionStringBuilder();
        static DB2Connection conn = new DB2Connection();
        internal static DBSettings dbSettings;
        

        internal static void UpdateDB(string sql)
        {
            sBuilder.Database = dbSettings.db;
            sBuilder.UserID = dbSettings.username;
            sBuilder.Password = dbSettings.password;
            sBuilder.Server = dbSettings.host + ":" + dbSettings.port;
            conn.ConnectionString = sBuilder.ConnectionString;

            using (conn)
            {
                conn.Open();

                DB2Command cmd = new DB2Command(sql, conn);
                cmd.ExecuteNonQuery();

                conn.Close(); conn.Dispose();
            }
        }
    }
}
