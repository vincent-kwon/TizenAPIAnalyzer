using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;


namespace TizenCAPIAnalyzer
{
    enum NativeAPITypes
    {
        NATIVE_PLATFORM_PUBLIC = 0,
        NATIVE_PLATFORM_INHOUSE = 1,
        NATIVE_PRODUCT_PUBLIC = 2,
        NATIVE_PRODUCT_INHOUSE = 3
    }

    enum SQLitePlatformOrNot
    {
        FALSE = 0,
        TRUE = 1,
    }

    class TizenNativeAPIDBHandler
    {
        // Files
        const string kPublicNativeAPIDbFile = "native_api.db3";
        const string kPlatformPublicNativeAPIWhiteListFile = "platform_public_native_API_list.cs";
        const string kPlatformInhouseNativeAPIWhiteListFile = "platform_inhouse_native_API_list.cs";
        //const string kWearableAppUsedAPIListFile = "wearable_app_used_API_list.cs";
        const string kWearableAppUsedNonFilteredNoDupListFile = "non-filtered-app-used-api-remove-duplicate.cs";

        // Tables
        const string kPlatformPublicNativeAPITable = "PublicNativeAPI";
        const string kWearableAPITable = "WearbleAppUsedAPIsTable";

        public void CreatePlatformNativeAPIsTable()
        {
            Console.WriteLine("CreatePlatformPublicNativeAPIsTable DB");
            string createQuery = @"CREATE TABLE IF NOT EXISTS
                                  [" + kPlatformPublicNativeAPITable + @"] (
                                  [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                  [APIName] NVARCHAR(2048) NOT NULL ,
                                  [APIPackage] NVARCHAR(2048) NOT NULL,
                                  [IsPlatform] BOOLEAN NOT NULL,
                                  [APIType] INTEGER NOT NULL)";

            System.Data.SQLite.SQLiteConnection.CreateFile(kPublicNativeAPIDbFile);
            using (System.Data.SQLite.SQLiteConnection conn = new System.Data.SQLite.SQLiteConnection("data source=" + kPublicNativeAPIDbFile))
            {
                using (System.Data.SQLite.SQLiteCommand cmd = new System.Data.SQLite.SQLiteCommand(conn))
                {

                    conn.Open();
                    cmd.CommandText = createQuery;
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        private void CreateWearbleAppUsedAPIsTable()
        {
            Console.WriteLine("CreateAppUsedAPIsTable DB");
            string createQuery = @"CREATE TABLE IF NOT EXISTS
                                  [" + kWearableAPITable + @"] (
                                  [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                  [AppName] NVARCHAR(2048) NOT NULL,
                                  [APIName] NVARCHAR(2048) NOT NULL)";
#if TEST_APP_API_ONLU
            //System.Data.SQLite.SQLiteConnection.CreateFile(kPublicNativeAPIDbFile);
#endif
            using (System.Data.SQLite.SQLiteConnection conn = new System.Data.SQLite.SQLiteConnection("data source=" + kPublicNativeAPIDbFile))
            {
                using (System.Data.SQLite.SQLiteCommand cmd = new System.Data.SQLite.SQLiteCommand(conn))
                {

                    conn.Open();
                    cmd.CommandText = createQuery;
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public void InsertPlatformPublicNativeAPIs(int publicity)
        {
            using (System.Data.SQLite.SQLiteConnection conn = new System.Data.SQLite.SQLiteConnection("data source=" + kPublicNativeAPIDbFile))
            {
                using (System.Data.SQLite.SQLiteCommand cmd = new System.Data.SQLite.SQLiteCommand(conn))
                {
                    // Re-open file
                    conn.Open();
                    string fileName = "";
                    if (publicity == 0) {
                        fileName = kPlatformPublicNativeAPIWhiteListFile;
                    }
                    else {
                        fileName = kPlatformInhouseNativeAPIWhiteListFile;
                    }
                        
                    StreamReader sr = new StreamReader(new FileStream(fileName, FileMode.Open));
                    int count = 0;
                    while (sr.EndOfStream == false)
                    {
                        string line = sr.ReadLine();
                        // Remove extra white spaces
                        var words = line.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                        string insertQuery = "INSERT INTO " + kPlatformPublicNativeAPITable + " (APIPackage, APIName, IsPlatform, APIType) ";
                        if (words.Count() == 1)
                        {
                            string whiteSpaceRemovedStr = words[0];
                            // Split by tab
                            words = whiteSpaceRemovedStr.Split('\t');
                        }

                        // Words must more than 2
                        if (words.Count() >= 2)
                        {
                            string apiPackage = words.ElementAt(0);
                            string apiName = "";
                            for (int i = 1; i < words.Count(); i++) {
                                apiName += words.ElementAt(i);
                            }
                            insertQuery += string.Format("VALUES ( '{0}', '{1}', {2}, {3})", apiPackage, apiName, 1, publicity);
                            cmd.CommandText = insertQuery;
                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            Console.WriteLine("Less than 2 words. Break now:");
                            break;
                        }
                        count++;
                    }
                    conn.Close();
                }
            }
        }

        public void InsertWearbleAppUsedAPIs()
        {
            using (System.Data.SQLite.SQLiteConnection conn = new System.Data.SQLite.SQLiteConnection("data source=" + kPublicNativeAPIDbFile))
            {
                using (System.Data.SQLite.SQLiteCommand cmd = new System.Data.SQLite.SQLiteCommand(conn))
                {
                    // Re-open file
                    conn.Open();

                    StreamReader sr = new StreamReader(new FileStream(kWearableAppUsedNonFilteredNoDupListFile, FileMode.Open));
                    int count = 0;
                    while (sr.EndOfStream == false)
                    {
                        string line = sr.ReadLine();
                        // Remove extra white spaces
                        var words = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        // One line only includes tab but no space.
                        if (words.Count() == 1)
                        {
                            string whiteSpaceRemovedStr = words[0];
                            // Split by tab
                            words = whiteSpaceRemovedStr.Split('\t');
                        }

                        string insertQuery = "INSERT INTO " + kWearableAPITable + " (AppName, APIName) ";
                        if (words.Count() >= 2)
                        {
                            string appName = words.ElementAt(0);
                            string apiName = "";
                            for (int i = 1; i < words.Count(); i++)
                            {
                                apiName += words.ElementAt(i);
                            }
                            insertQuery += string.Format("VALUES ( '{0}', '{1}' )", appName, apiName);
                            cmd.CommandText = insertQuery;
                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            Console.WriteLine("Less than 2 words. Break now:");
                            break;
                        }
                        count++;
                    }
                    conn.Close();
                }
            }
        }

        public void ReadFromPlatformAPIsTable()
        {
            using (System.Data.SQLite.SQLiteConnection conn = new System.Data.SQLite.SQLiteConnection("data source=" + kPublicNativeAPIDbFile))
            {
                using (System.Data.SQLite.SQLiteCommand cmd = new System.Data.SQLite.SQLiteCommand(conn))
                {
                    // Re-open file
                    conn.Open();
                    string queryText = "select [APIPackage], [APIName], [IsPlatform], [APIType] from " + kPlatformPublicNativeAPITable;
                    cmd.CommandText = queryText;
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Console.WriteLine(reader["APIPackage"] + " : " + reader["APIName"]);
                    }
                    conn.Close();
                }
            }
        }

        public void ReadFromWearableAppUsedAPIs()
        {
            using (System.Data.SQLite.SQLiteConnection conn = new System.Data.SQLite.SQLiteConnection("data source=" + kPublicNativeAPIDbFile))
            {
                using (System.Data.SQLite.SQLiteCommand cmd = new System.Data.SQLite.SQLiteCommand(conn))
                {
                    // Re-open file
                    conn.Open();
                    string queryText = "select [AppName], [APIName] from " + kWearableAPITable;
                    cmd.CommandText = queryText;
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Console.WriteLine(reader["AppName"] + " : " + reader["APIName"]);
                    }
                    conn.Close();
                }
            }
        }


        static void Main(string[] args)
        {
            TizenNativeAPIDBHandler p = new TizenNativeAPIDBHandler();
            p.CreatePlatformNativeAPIsTable();
            p.InsertPlatformPublicNativeAPIs((int)NativeAPITypes.NATIVE_PLATFORM_PUBLIC);
            p.InsertPlatformPublicNativeAPIs((int)NativeAPITypes.NATIVE_PLATFORM_INHOUSE);
            //p.ReadFromPlatformAPIsTable();
            p.CreateWearbleAppUsedAPIsTable();
            p.InsertWearbleAppUsedAPIs();
            //p.ReadFromWearableAppUsedAPIs();
            Console.WriteLine("Done.....");
            Console.ReadLine();
        }
    }
}

