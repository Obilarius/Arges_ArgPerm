﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ARPS.ViewModels
{
    public class ScheduleViewModel : BaseViewModel
    {
        #region Commands

        #region Search
        /// <summary>
        /// Der Command wenn der Benutzer nach einem User suchen will
        /// </summary>
        public ICommand SearchUserCommand { get; set; }

        /// <summary>
        /// Der Command um die Suche nach einem User zu löschen
        /// </summary>
        public ICommand ClearSearchUserCommand { get; set; }

        /// <summary>
        /// Der Command wenn der Benutzer nach einer Gruppe suchen will
        /// </summary>
        public ICommand SearchGroupCommand { get; set; }

        /// <summary>
        /// Der Command um die Suche nach einer Gruppe zu löschen
        /// </summary>
        public ICommand ClearSearchGroupCommand { get; set; }

        #endregion

        /// <summary>
        /// Der Command wenn ein anderer User ausgewählt wird
        /// </summary>
        public ICommand UserSelectionChangedCommand { get; set; }

        public ICommand AddGroupToPlanCommand { get; set; }

        #endregion

        #region Propertys

        /// <summary>
        /// Der selektierte User
        /// </summary>
        private UserPrincipal _selectedUser { get; set; }
        public UserPrincipal SelectedUser
        {
            get { return _selectedUser; }
            set
            {
                if (value == _selectedUser)
                    return;

                _selectedUser = value;

                if (value != null)
                    // Ruft die Funktion die zb. die UserInfos aktualisiert
                    UserSelectionChanged();
            }
        }

        /// <summary>
        /// Enthält die Informationen über den selektierten User
        /// </summary>
        public ObservableCollection<UserInfoEntry> UserInfos { get; set; }

        /// <summary>
        /// Die Gruppen in dennen der selektierte User Mitglied ist
        /// </summary>
        public ObservableCollection<GroupPrincipal> SelectedUserGroups { get; set; }

        #region User and Group Lists

        /// <summary>
        /// Hält alle AD User
        /// </summary>
        public List<UserPrincipal> AllUsers { get; set; }

        /// <summary>
        /// Hält die gefilterten AD User
        /// </summary>
        public ObservableCollection<UserPrincipal> UsersFiltered { get; set; }

        /// <summary>
        /// Hält alle AD Gruppen
        /// </summary>
        public List<GroupPrincipal> AllGroups { get; set; }

        /// <summary>
        /// Hält die gefilterten AD Gruppen
        /// </summary>
        public ObservableCollection<GroupPrincipal> GroupsFiltered { get; set; }

        #endregion

        #region SearchText

        /// <summary>
        /// Der letzte Suchtext der gesucht wurde
        /// </summary>
        private string LastSearchTextUser { get; set; }

        /// <summary>
        /// Der Suchtext nach dem die User gefiltert werden
        /// </summary>
        public string SearchTextUser { get; set; }

        /// <summary>
        /// Der letzte Suchtext der gesucht wurde
        /// </summary>
        private string LastSearchTextGroup { get; set; }

        /// <summary>
        /// Der Suchtext nach dem die Gruppen gefiltert werden
        /// </summary>
        public string SearchTextGroup { get; set; }

        #endregion

        #endregion

        #region Constructor

        /// <summary>
        /// Standard Konstruktor
        /// </summary>
        public ScheduleViewModel()
        {
            // Erstelle Commands
            SearchUserCommand = new RelayCommand(SearchUser);
            ClearSearchUserCommand = new RelayCommand(ClearSearchUser);
            SearchGroupCommand = new RelayCommand(SearchGroup);
            ClearSearchGroupCommand = new RelayCommand(ClearSearchGroup);
            AddGroupToPlanCommand = new RelayCommand(AddGroupToPlan);

            // Liest alle User udn Gruppen aus und speichert sie in den Listen
            AllUsers = GetAllADUsers();
            UsersFiltered = new ObservableCollection<UserPrincipal>(AllUsers);

            AllGroups = GetAllADGroups();
            GroupsFiltered = new ObservableCollection<GroupPrincipal>(AllGroups);
        }

        #endregion

        public void AddGroupToPlan(GroupPrincipal result)
        {

        }

        /// <summary>
        /// Wenn ein neuer User selektiert wird
        /// </summary>
        public void UserSelectionChanged()
        {
            // Erstellt eine neue Liste mit den neuen Infos
            UserInfos = new ObservableCollection<UserInfoEntry>
            {
                new UserInfoEntry("Name", SelectedUser.DisplayName),
                new UserInfoEntry("Email", SelectedUser.EmailAddress),
                new UserInfoEntry("Letzter Login", SelectedUser.LastLogon.ToString())
            };

            List<GroupPrincipal> tmpList = new List<GroupPrincipal>(); ;
            // Liest alle Gruppen aus in dennen der SelectedUser Mitglied ist
            PrincipalSearchResult<Principal> grp = SelectedUser.GetGroups();
            // Geht über alle Gruppen und fügt sie zur Liste hinzu
            foreach (var g in grp)
            {
                tmpList.Add(g as GroupPrincipal);
            }
            // Sortiert die Gruppen
            tmpList.Sort( (x,y) => x.Name.CompareTo(y.Name) );

            // Setzt SelectedUserGroups auf die Ausgelesene Liste
            SelectedUserGroups = new ObservableCollection<GroupPrincipal>(tmpList);
        }

        #region UserSearch

        /// <summary>
        /// Sucht den User und filtert die Ansicht
        /// </summary>
        public void SearchUser()
        {
            // Prüft damit nicht der selbe Text nochmal gesucht wird
            if ((string.IsNullOrEmpty(LastSearchTextUser) && string.IsNullOrEmpty(SearchTextUser)) ||
                string.Equals(LastSearchTextUser, SearchTextUser))
                return;

            // Wenn wir keinen Suchtext haben
            if (string.IsNullOrEmpty(SearchTextUser))
            {
                // Mache gefilterte Liste gleich Allen Usern
                UsersFiltered = new ObservableCollection<UserPrincipal>(AllUsers);

                // Setze letzte Suche
                LastSearchTextUser = SearchTextUser;

                return;
            }

            // Finde alle Items die den Text enthalten
            UsersFiltered = new ObservableCollection<UserPrincipal>(
                AllUsers.Where(x => x.Name.ToLower().Contains(SearchTextUser.ToLower())));

            // Setze letzte Suche
            LastSearchTextUser = SearchTextUser;
        }

        /// <summary>
        /// Löscht die Suche und zeigt wieder alle User an
        /// </summary>
        public void ClearSearchUser()
        {
            // Prüft ob SearchText schon leer ist 
            if (!string.IsNullOrEmpty(SearchTextUser))
            {
                // Löscht den Suchtext
                SearchTextUser = string.Empty;

                // Zeigt wieder alle User an
                UsersFiltered = new ObservableCollection<UserPrincipal>(AllUsers);
            }
        }

        #endregion

        #region GroupSearch

        /// <summary>
        /// Sucht die Gruppe und filtert die Ansicht
        /// </summary>
        public void SearchGroup()
        {
            // Prüft damit nicht der selbe Text nochmal gesucht wird
            if ((string.IsNullOrEmpty(LastSearchTextGroup) && string.IsNullOrEmpty(SearchTextGroup)) ||
                string.Equals(LastSearchTextGroup, SearchTextGroup))
                return;

            // Wenn wir keinen Suchtext haben
            if (string.IsNullOrEmpty(SearchTextGroup))
            {
                // Mache gefilterte Liste gleich Allen Gruppen
                GroupsFiltered = new ObservableCollection<GroupPrincipal>(AllGroups);

                // Setze letzte Suche
                LastSearchTextGroup = SearchTextGroup;

                return;
            }

            // Finde alle Items die den Text enthalten
            GroupsFiltered = new ObservableCollection<GroupPrincipal>(
                AllGroups.Where(x => x.Name.ToLower().Contains(SearchTextGroup.ToLower())));

            // Setze letzte Suche
            LastSearchTextGroup = SearchTextGroup;
        }

        /// <summary>
        /// Löscht die Suche und zeigt wieder alle Gruppen an
        /// </summary>
        public void ClearSearchGroup()
        {
            // Prüft ob SearchText schon leer ist 
            if (!string.IsNullOrEmpty(SearchTextGroup))
            {
                // Löscht den Suchtext
                SearchTextGroup = string.Empty;

                // Zeigt wieder alle User an
                GroupsFiltered = new ObservableCollection<GroupPrincipal>(AllGroups);
            }
        }

        #endregion

        #region Get Infos from AD

        /// <summary>
        /// Liest das AD aus und gibt eine Liste mit allen aktivien Usern zurück
        /// </summary>
        /// <returns></returns>
        List<UserPrincipal> GetAllADUsers()
        {
            // create your domain context
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain);
            // define a "query-by-example" principal - here, we search for a UserPrincipal 
            UserPrincipal qbeUser = new UserPrincipal(ctx);
            // create your principal searcher passing in the QBE principal    
            PrincipalSearcher srch = new PrincipalSearcher(qbeUser);

            List<UserPrincipal> lst = new List<UserPrincipal>();
            // find all matches
            foreach (var found in srch.FindAll())
            {
                UserPrincipal user = found as UserPrincipal;

                if (user != null)
                {
                    if (user.Enabled.Value)
                        lst.Add(user);
                }
            }
            lst.Sort( (x,y)=>x.Name.CompareTo(y.Name) );
            return lst;
        }

        /// <summary>
        /// Liest das AD aus und gibt eine Liste mit allen Gruppen zurück
        /// </summary>
        /// <returns></returns>
        List<GroupPrincipal> GetAllADGroups()
        {
            // create your domain context
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain);
            GroupPrincipal qbeGrp = new GroupPrincipal(ctx);
            // create your principal searcher passing in the QBE principal    
            PrincipalSearcher srch = new PrincipalSearcher(qbeGrp);

            List<GroupPrincipal> lst = new List<GroupPrincipal>();
            // find all matches
            foreach (var found in srch.FindAll())
            {
                GroupPrincipal grp = found as GroupPrincipal;

                if (grp != null)
                    lst.Add(grp);
            }
            lst.Sort((x, y) => x.Name.CompareTo(y.Name));
            return lst;
        }

        #endregion
    }
}
