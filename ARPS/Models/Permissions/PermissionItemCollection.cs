﻿using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ARPS
{
    public class PermissionItemCollection : BindableBase
    {
        

        /// <summary>
        /// Hällt die Items die angezeigt werden
        /// </summary>
        public ObservableCollection<PermissionItem> DisplayedItems { get; set; }

        /// <summary>
        /// Hällt alle Gruppen zum selektierten User
        /// </summary>
        List<ADElement> AllGroups { get; set; }

        

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="userSid"></param>
        public PermissionItemCollection(string userSid)
        {
            

            // Liest alle Gruppen des Users aus
            AllGroups = new List<ADElement>(ADStructure.GetGroupsFromUser(userSid));
            AllGroups.Add(ADStructure.GetADUser(userSid));


        }


        /// <summary>
        /// Füllt die Shares in ein Übergeordnetes Server Element und fügt alles dem Property DisplayedItems hinzu
        /// </summary>
        public void FillItemsWithShares()
        {
            var Shares = new ObservableCollection<PermissionItem>();

            // erstellt eine MSSQL Verbindung und öffnet Sie
            var mssql = new MsSql();
            mssql.Open();

            foreach (ADElement group in AllGroups)
            {
                // Der SQL Befehl um alle Ordner abzurufen die root sind
                string sql = $"SELECT ace.*, s.* " +
                    $"FROM [ARPS_Test].[fs].[aces] ace " +
                    $"LEFT JOIN ARPS_Test.fs.acls acl " +
                    $"ON acl._ace_id = ace._ace_id " +
                    $"JOIN ARPS_Test.fs.shares s " +
                    $"ON acl._path_id = s._path_id " +
                    $"WHERE acl._type = 1 " +
                    $"AND ace._sid = @Sid";

                // Sendet den SQL Befehl an den SQL Server
                SqlCommand cmd = new SqlCommand(sql, mssql.Con);

                //Parameter anhängen
                cmd.Parameters.AddWithValue("@Sid", group.SID);

                // Benutzt den SQL Reader um über alle Zeilen der Abfrage zu gehen
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int _ace_id = (int)reader["_ace_id"];
                        string _sid = reader["_sid"].ToString();
                        FileSystemRights _rights = (FileSystemRights)reader["_rights"];
                        bool _type = Convert.ToBoolean(reader["_type"]);
                        string _fsr = reader["_fsr"].ToString();
                        bool _is_inherited = Convert.ToBoolean(reader["_is_inherited"]);
                        int _inheritance_flags = (int)reader["_inheritance_flags"];
                        int _propagation_flags = (int)reader["_propagation_flags"];

                        int _path_id = (int)reader["_path_id"];
                        string _unc_path_name = reader["_unc_path_name"].ToString();
                        string _owner_sid = reader["_owner_sid"].ToString();
                        bool _has_children = Convert.ToBoolean(reader["_has_children"]);
                        long _size = (long)reader["_size"];
                        string _path_name = reader["_path_name"].ToString();
                        string _display_name = reader["_display_name"].ToString();
                        string _remark = reader["_remark"].ToString();
                        string _share_type = reader["_share_type"].ToString();
                        bool _hidden = Convert.ToBoolean(reader["_hidden"]);

                        // Legt für die ausgelesene Zeile ein neues PermissionItem an in dem alle Infos über diesen Ordner gespeichert sind
                        PermissionItem newPI = new PermissionItem(_path_id, _unc_path_name, _owner_sid, _has_children, _size, _hidden, AllGroups, DirectoryItemType.SharedFolder);
                        
                        // Legt für die ausgelesene Zeile ein neues ACE an in dem alle Infos über das Rechte Objekt angeben sind
                        DirectoryACE newACE = new DirectoryACE(_sid, _rights, _type, _fsr, _is_inherited, _inheritance_flags, _propagation_flags);


                        // Falls keine Rechte in diesem Datensatz vergeben werden oder wenn die Rechte nur auf Unterordner gelten
                        // wird der Datensatz nicht hinzugefügt
                        if (newACE.Rights <= 0)
                            continue;

                        // Prüft ob das PermissionItem schon in der Liste vorhanden ist.
                        PermissionItem value = Shares.FirstOrDefault(item => item.PathID == newPI.PathID);

                        // Falls der Pfad schon vorhanden ist wird das neue ACE Object dem bestehenden PermissionItem hinzugefügt
                        if (value != null)
                        {
                            // Das neue ACE Objekt wird dem Permission Item (Ordner) hinzugefügt
                            value.directoryACEs.Add(newACE);
                        }
                        // Falls das PermissionItem noch nicht vorhanden ist, wird das PerItem hinzugefügt und das neue Ace wird ihm hinzugefügt
                        else
                        {
                            // Fügt das neue ACE dem neuen PerItem hinzu
                            newPI.directoryACEs.Add(newACE);

                            // Fügt das neue PerItem der Collection hinzu
                            Shares.Add(newPI);
                        }
                    }
                }
            }

            // Sortiert die Liste mit Shares
            var sortedShares = from item in Shares
                               orderby item.FolderName
                               select item;

            DisplayedItems = new ObservableCollection<PermissionItem>();
            foreach (var share in sortedShares)
            {
                // Sucht ob der Server des Share Element schon vorhanden ist
                PermissionItem value = DisplayedItems.FirstOrDefault(item => item.ServerName == share.ServerName);
                // Falls der Server noch nicht vorhanden ist
                if (value == null)
                {
                    // Ein neuer Server wird erstellt
                    var newServer = new PermissionItem(share.ServerName);

                    // Das aktuelle Share Element wird dem Server als Kind hinzugefügt
                    newServer.Children.Add(share);

                    // Der neue Server wird der Liste hinzugefügt
                    DisplayedItems.Add(newServer);
                }
                else
                {
                    // Falls der Server schon vorhanden ist wird das Share Element dem Server als Kind hinzugefügt
                    value.Children.Add(share);
                }

            }

            // Schließt die MSSQL verbindung
            mssql.Close();
        }

        

    }
}
