using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Haphrain.Classes.MortyGame
{
    internal class MortyGame
    {
        internal List<Character> MortyList = new List<Character>();
        internal MortyGame(DBSettings dbSettings)
        {
            SqlConnectionStringBuilder sBuilder = new SqlConnectionStringBuilder();
            sBuilder.InitialCatalog = dbSettings.db;
            sBuilder.UserID = dbSettings.username;
            sBuilder.Password = dbSettings.password;
            sBuilder.DataSource = dbSettings.host + @"\" + dbSettings.instance + "," + dbSettings.port;
            sBuilder.ConnectTimeout = 30;
            sBuilder.IntegratedSecurity = false;
            SqlConnection conn = new SqlConnection();

            conn.ConnectionString = sBuilder.ConnectionString;

            using (conn)
            {
                conn.Open();

                #region Get Base Mortys
                string sql = "SELECT C.mortyID, C.mortyName, T.typeName, C.mortyRarity";
                sql += ", C.mortyHP, C.mortyATK, C.mortyDEF, C.mortySPD, C.evolveAmount";
                sql += ", C.evolvesTo, D.dimensionName, C.raritySort";
                sql += " FROM mortyCharacters C";
                sql += " LEFT JOIN mortyDimensions D ON C.mortyDimension = D.dimensionID LEFT JOIN mortyTypes T ON C.mortyType = T.typeID;";
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    Character c = new Character()
                    {
                        CharID = short.Parse((short.Parse(dr.GetValue(0).ToString()) + 1).ToString()),
                        CharName = dr.GetValue(1).ToString(),
                        Type = dr.GetValue(2).ToString(),
                        Rarity = dr.GetValue(3).ToString(),
                        HP = short.Parse(dr.GetValue(4).ToString()),
                        ATK = short.Parse(dr.GetValue(5).ToString()),
                        DEF = short.Parse(dr.GetValue(6).ToString()),
                        SPD = short.Parse(dr.GetValue(7).ToString()),
                        NeededToEvolve = short.Parse(dr.GetValue(8).ToString()),
                        EvolvesTo = null,
                        Dimension = dr.GetValue(10).ToString(),
                        RaritySort = int.Parse(dr.GetValue(11).ToString())
                    };

                    MortyList.Add(c);
                }
                dr.Close();
                #endregion

                conn.Close();
                conn.Dispose();

                foreach (Character c in MortyList)
                {
                    c.StatTotal = c.ATK + c.DEF + c.HP + c.SPD;
                    if (c.NeededToEvolve > 0)
                        c.EvolvesTo = MortyList.SingleOrDefault(x => x.CharID == c.CharID + 1);
                }
            }
        }
    }
}
