﻿using ARPSMSSQL;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace ARPSDeamon
{
    public static class FSWorker
    {
        /// <summary>
        /// Liest den jeweiligen Ordner und rekursive alle Unterordner ab und speichert die Informationen in der Datenbank
        /// </summary>
        /// <param name="dInfo">Das DirectoryInfo Object des Ordners der verarbeitet werden soll</param>
        /// <param name="parentId">Die ID des übergeortneten Ordners</param>
        public static void GetDirectorySecurity(DirectoryInfo dInfo, int parentId)
        {
            if (dInfo == null)
                return;

            //baut eine SQL Verbindung auf
            MsSql mssql = new MsSql();
            mssql.Open();

            // Der volle Pfad
            string _path_name = dInfo.FullName;
            // Die ID des übergeordneten Ordners
            int _parent_path_id = parentId;
            // Berechnet den Hash und speichert ihn
            string _path_hash = Helper.Hash(_path_name);
            // Ob der Ordner in der ersten Ebene ist oder nicht
            int _is_root = (_parent_path_id == -1) ? 1 : 0;

            // Die Ebene des Ordners
            int _scan_deepth = _path_name.TrimStart('\\').Split('\\').Count() - 1;


            // Ausgabe: Alle Pafde bis zur 3. Ebene werden ausgegeben
            //Index++;
            //if (_scan_deepth <= 5)
            //{
            //    //Console.WriteLine(_path_name);
            //    float percent = (float)Index / (float)Count * 100f;
            //    Console.WriteLine(percent.ToString("n2") + $" % of {Count} -- {_path_name}");
            //}



            #region Prüfung ob schon vorhanden ist
            // Der SQL Befehl zum überprüfen ob der jeweilige Eintrag schon vorhanden ist
            string sql = $"SELECT _path_id FROM fs.dirs WHERE _path_hash = @PathHash";
            SqlCommand cmd = new SqlCommand(sql, mssql.Con);
            // Der Hash wird als Parameter an den SQL Befehl gehängt
            cmd.Parameters.AddWithValue("@PathHash", _path_hash);

            // Öffnet die SQL Verbindung, führt die Abfrage durch und schließt die Verbindung wieder
            mssql.Open();
            // Falls es den abgefragten Datensatz schon gibt, bekommt man in index die ID des Datensatzen, sonnst null
            var _path_id = cmd.ExecuteScalar();
            mssql.Close();
            #endregion

            #region Größenberechnung

            // Es gibt die Zeile
            long _size;
            if (_path_id != null)
            {
                sql = $"SELECT _size FROM fs.dirs WHERE _path_id = @PathId";
                cmd = new SqlCommand(sql, mssql.Con);
                // Der Hash wird als Parameter an den SQL Befehl gehängt
                cmd.Parameters.AddWithValue("@PathId", _path_id);

                // Öffnet die SQL Verbindung, führt die Abfrage durch und schließt die Verbindung wieder
                mssql.Open();
                // Falls es den abgefragten Datensatz schon gibt, bekommt man in index die ID des Datensatzen, sonnst null
                var size = cmd.ExecuteScalar();
                mssql.Close();

                if ((long)size == 0 || size == null)
                    _size = DirSize(dInfo);
                else
                    _size = (long)size;
            }
            else
            {
                _size = DirSize(dInfo);
            }
            #endregion

            // Liest alle Unterordner in ein Array
            DirectoryInfo[] childs;
            try
            {
                childs = dInfo.GetDirectories();
            }
            catch (Exception)
            {
                childs = new DirectoryInfo[1];
            }

            // Liest die Infos über den Besitzer aus
            string _owner_sid = "0";
            DirectorySecurity dSecurity = null;
            try
            {
                dSecurity = dInfo.GetAccessControl();
                IdentityReference owner = dSecurity.GetOwner(typeof(SecurityIdentifier));  // FÜR SID
                _owner_sid = owner.Value;
            }
            catch (Exception) { }

            // Ob der Ordner Unterordner hat oder nicht
            int _has_children = (childs.Length > 0) ? 1 : 0;

            // Hash ist noch nicht vorhanden
            if (_path_id == null)
            {
                // Der SQL Befehl zum INSERT in die Datenbank
                sql = $"INSERT INTO fs.dirs(_path_name, _owner_sid, _path_hash, _parent_path_id, _is_root, _has_children, _scan_deepth, _size) " +
                      $"OUTPUT INSERTED._path_id " +
                      $"VALUES (@PathName, @OwnerSid, @PathHash, @ParentPathId, @IsRoot, @HasChildren, @ScanDeepth, @Size) ";

                cmd = new SqlCommand(sql, mssql.Con);

                // Hängt die Parameter an
                cmd.Parameters.AddWithValue("@PathName", _path_name);
                cmd.Parameters.AddWithValue("@OwnerSid", _owner_sid);
                cmd.Parameters.AddWithValue("@PathHash", _path_hash);
                cmd.Parameters.AddWithValue("@ParentPathId", _parent_path_id);
                cmd.Parameters.AddWithValue("@IsRoot", _is_root);
                cmd.Parameters.AddWithValue("@HasChildren", _has_children);
                cmd.Parameters.AddWithValue("@ScanDeepth", _scan_deepth);
                cmd.Parameters.AddWithValue("@Size", _size);

                // Öffnet die SQL Verbindung
                mssql.Open();
                // Führt die Query aus
                _path_id = (int)cmd.ExecuteScalar();
                //Schließt die Verbindung
                mssql.Close();
            }
            // Hash ist noch nicht vorhanden
            else
            {
                // SQL Befehl zum Updaten des Eintrags
                sql = $"UPDATE fs.dirs " +
                      $"SET _path_name = @PathName, _owner_sid = @OwnerSid, _path_hash = @PathHash, _parent_path_id = @ParentPathId, " +
                      $"_is_root = @IsRoot, _has_children = @HasChildren, _scan_deepth = @ScanDeepth, _size = @Size " +
                      $"WHERE _path_id = @PathId";

                cmd = new SqlCommand(sql, mssql.Con);

                // Hängt die Parameter an
                cmd.Parameters.AddWithValue("@PathName", _path_name);
                cmd.Parameters.AddWithValue("@OwnerSid", _owner_sid);
                cmd.Parameters.AddWithValue("@PathHash", _path_hash);
                cmd.Parameters.AddWithValue("@ParentPathId", _parent_path_id);
                cmd.Parameters.AddWithValue("@IsRoot", _is_root);
                cmd.Parameters.AddWithValue("@HasChildren", _has_children);
                cmd.Parameters.AddWithValue("@ScanDeepth", _scan_deepth);
                cmd.Parameters.AddWithValue("@Size", _size);
                cmd.Parameters.AddWithValue("@PathId", (int)_path_id);

                // Öffnet die SQL Verbindung
                mssql.Open();
                // Führt die Query aus
                cmd.ExecuteNonQuery();
                //Schließt die Verbindung
                mssql.Close();
            }

            // Ruft die ACL zum jeweiligen Ordner ab und schreib diese in die Datenbank
            if (dSecurity != null)
                GetACEs(dSecurity, (int)_path_id);

            // Geht über alle Unterordner (Kinder) und ruft die Funktion rekursiv auf
            foreach (DirectoryInfo child in childs)
            {
                GetDirectorySecurity(child, (int)_path_id);
            }

        }

        /// <summary>
        /// Liest die ACEs des übergebenen Ordners durch und schreib diese in die beiden Datenbanken aces und acls
        /// </summary>
        /// <param name="dSecurity">Das DirecrotySecurity Objekt des Ordners</param>
        /// <param name="_path_id">Die ID des Ordnerelements aus der dirs Datenbank</param>
        static void GetACEs(DirectorySecurity dSecurity, int _path_id)
        {
            AuthorizationRuleCollection acl = dSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier)); // FÜR SID
            //AuthorizationRuleCollection acl = dSecurity.GetAccessRules(true, true, typeof(NTAccount));        // FÜR NAMEN (ARGES/walzenbach)

            //baut eine SQL Verbindung auf
            MsSql mssql = new MsSql();
            mssql.Open();

            foreach (FileSystemAccessRule ace in acl)
            {
                //if (dirId == -1)
                //    return;

                //Liest die einzelnen Values aus dem ACE aus
                string _sid = ace.IdentityReference.Value;
                string _fsr = ace.FileSystemRights.ToString();
                int _rights = (int)ace.FileSystemRights;
                int _type = (int)ace.AccessControlType;
                int _is_inherited = ace.IsInherited ? 1 : 0;
                int _inheritance_flags = (int)ace.InheritanceFlags;
                int _propagation_flags = (int)ace.PropagationFlags;
                string _ace_hash = Helper.Hash(_sid + _rights + _type + _is_inherited + _inheritance_flags + _propagation_flags);


                #region Prüfung ob schon vorhanden ist
                // Der SQL Befehl zum überprüfen ob der jeweilige Eintrag schon vorhanden ist
                string sql = $"SELECT _ace_id FROM fs.aces WHERE _ace_hash = @AceHash";
                SqlCommand cmd = new SqlCommand(sql, mssql.Con);
                // Der Hash wird als Parameter an den SQL Befehl gehängt
                cmd.Parameters.AddWithValue("@AceHash", _ace_hash);

                // Öffnet die SQL Verbindung, führt die Abfrage durch und schließt die Verbindung wieder
                mssql.Open();
                // Falls es den abgefragten Datensatz schon gibt, bekommt man in index die ID des Datensatzen, sonnst null
                var _ace_id = cmd.ExecuteScalar();
                mssql.Close();
                #endregion


                // ACE Hash ist noch nicht vorhanden
                if (_ace_id == null)
                {
                    // Der SQL Befehl zum INSERT in die Datenbank
                    sql = $"INSERT INTO fs.aces(_sid, _fsr, _rights, _type, _is_inherited, _inheritance_flags, _propagation_flags, _ace_hash) " +
                          $"OUTPUT INSERTED._ace_id " +
                          $"VALUES (@Sid, @Fsr, @Rights, @Type, @IsInherited, @InheritanceFlags, @PropagationFlags, @AceHash) ";

                    cmd = new SqlCommand(sql, mssql.Con);

                    // Hängt die Parameter an
                    cmd.Parameters.AddWithValue("@Sid", _sid);
                    cmd.Parameters.AddWithValue("@Fsr", _fsr);
                    cmd.Parameters.AddWithValue("@Rights", _rights);
                    cmd.Parameters.AddWithValue("@Type", _type);
                    cmd.Parameters.AddWithValue("@IsInherited", _is_inherited);
                    cmd.Parameters.AddWithValue("@InheritanceFlags", _inheritance_flags);
                    cmd.Parameters.AddWithValue("@PropagationFlags", _propagation_flags);
                    cmd.Parameters.AddWithValue("@AceHash", _ace_hash);

                    // Öffnet die SQL Verbindung
                    mssql.Open();
                    // Führt die Query aus
                    _ace_id = (int)cmd.ExecuteScalar();

                    //Schließt die Verbindung
                    mssql.Close();
                }
                // Hash ist noch nicht vorhanden
                else
                {
                    // SQL Befehl zum Updaten des Eintrags
                    sql = $"UPDATE fs.aces " +
                          $"SET _sid = @Sid, _fsr = @Fsr, _rights = @Rights, _type = @Type, _is_inherited = @IsInherited, " +
                          $"_inheritance_flags = @InheritanceFlags, _propagation_flags = @PropagationFlags, _ace_hash = @AceHash " +
                          $"WHERE _ace_id = @AceId";

                    cmd = new SqlCommand(sql, mssql.Con);

                    // Hängt die Parameter an
                    cmd.Parameters.AddWithValue("@Sid", _sid);
                    cmd.Parameters.AddWithValue("@Fsr", _fsr);
                    cmd.Parameters.AddWithValue("@Rights", _rights);
                    cmd.Parameters.AddWithValue("@Type", _type);
                    cmd.Parameters.AddWithValue("@IsInherited", _is_inherited);
                    cmd.Parameters.AddWithValue("@InheritanceFlags", _inheritance_flags);
                    cmd.Parameters.AddWithValue("@PropagationFlags", _propagation_flags);
                    cmd.Parameters.AddWithValue("@AceHash", _ace_hash);
                    cmd.Parameters.AddWithValue("@AceId", (int)_ace_id);

                    // Öffnet die SQL Verbindung
                    mssql.Open();
                    // Führt die Query aus
                    cmd.ExecuteNonQuery();
                    //Schließt die Verbindung
                    mssql.Close();
                }


                #region Eintragung in die ACL Datenbank
                sql = $"INSERT INTO fs.acls (_path_id, _ace_id) SELECT @PathId, @AceId " +
                    $"WHERE NOT EXISTS(SELECT * FROM fs.acls WHERE _path_id = @PathId AND _ace_id = @AceId)";

                cmd = new SqlCommand(sql, mssql.Con);

                // Die Ids werden als Parameter an den SQL Befehl gehängt
                cmd.Parameters.AddWithValue("@PathId", _path_id);
                cmd.Parameters.AddWithValue("@AceId", _ace_id);

                // Öffnet die SQL Verbindung, führt die Abfrage durch und schließt die Verbindung wieder
                mssql.Open();
                cmd.ExecuteNonQuery();
                mssql.Close();
                #endregion
            }
        }

        /// <summary>
        /// Berechnet die Ordnergröße in Bytes
        /// </summary>
        /// <param name="d">DirectoryInfo Object</param>
        /// <returns>Größe in Bytes</returns>
        static long DirSize(DirectoryInfo d)
        {
            long size = 0;

            FileInfo[] fis;
            // Add file sizes.
            try
            {
                fis = d.GetFiles();
            }
            catch (Exception)
            {
                return size;
            }

            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di);
            }
            return size;
        }
    }
}