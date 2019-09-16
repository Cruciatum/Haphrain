using System.Data.SqlClient;

namespace Haphrain.Classes.HelperObjects
{
    internal static class DBControl
    {
        static SqlConnectionStringBuilder sBuilder = new SqlConnectionStringBuilder();
        static SqlConnection conn = new SqlConnection();
        internal static DBSettings dbSettings;
        

        internal static void UpdateDB(string sql)
        {
            //Load prefix & options from DB
            sBuilder.InitialCatalog = dbSettings.db;
            sBuilder.UserID = dbSettings.username;
            sBuilder.Password = dbSettings.password;
            sBuilder.DataSource = dbSettings.host + @"\" + dbSettings.instance + "," + dbSettings.port;
            sBuilder.ConnectTimeout = 30;
            sBuilder.IntegratedSecurity = false;
            conn.ConnectionString = sBuilder.ConnectionString;

            using (conn)
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.ExecuteNonQuery();

                conn.Close(); conn.Dispose();
            }
        }
    }
}
