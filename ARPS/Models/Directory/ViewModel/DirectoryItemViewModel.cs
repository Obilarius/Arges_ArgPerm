﻿using ARPS.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ARPS
{
    public class DirectoryItemViewModel : BaseViewModel
    {
        #region Public Propertys

        /// <summary>
        /// Die ID (aus der MSSQL Datenbank) des Items
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Der absolute Pfad zum diesem Item
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Das ist der Name des Items
        /// </summary>
        public string Name { get { return DirectoryStructure.GetFolderName(this.FullPath); } }

        /// <summary>
        /// Der Typ des Items
        /// </summary>
        public DirectoryItemType Type { get; set; }

        /// <summary>
        /// Dei SID des Besitzers
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Eine Liste mit allen Kindelementen (Unterordnern) von diesem Item
        /// </summary>
        public ObservableCollection<DirectoryItemViewModel> Children { get; set; }

        /// <summary>
        /// Sagt uns ob das aktuelle Element Kinder hat und dammit aufgeklappt werden kann
        /// </summary>
        public bool CanExpand { get { return DirectoryStructure.HasChild(this.Id); } }

        /// <summary>
        /// Sagt uns ob das aktuelle Item aufgeklappt ist oder nicht
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return this.Children?.Count(f => f != null) > 0;
            }
            set
            {
                // Wenn uns das UI mitteilt das Verzeichnis aufzuklappen
                if (value == true)
                    // Finde alle Kinder
                    Expand();
                // Wenn uns das UI mitteilt das Verzeichnis einzuklappen
                else
                    this.ClearChildren();
            }
        }

        #region Selected Item

        private bool _isSelected;
        /// <summary>
        /// Sagt uns ob das aktuelle Item selektiert ist oder nicht
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    if (_isSelected)
                    {
                        SelectedItem = this;
                    }
                }
            }
        }

        private DirectoryItemViewModel _selectedItem = null;
        /// <summary>
        /// Speichert das selektierte Item 
        /// </summary>
        public DirectoryItemViewModel SelectedItem
        {
            get { return _selectedItem; }
            private set
            {
                // Falls ein anderes Item selektiert wurde
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnSelectedItemChanged();
                }
            }
        }

        /// <summary>
        /// Wird ausgeführt wenn ein anderes Item selektiert wurde
        /// </summary>
        private void OnSelectedItemChanged()
        {
            
        }

        #endregion

        #endregion

        #region Public Commands

        /// <summary>
        /// Der Befehl um das Item aufzuklappen
        /// </summary>
        public ICommand ExpandCommand { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default Constructor
        /// </summary>
        public DirectoryItemViewModel(int id, string fullPath, DirectoryItemType type, string owner)
        {
            // Erstelle Commands
            this.ExpandCommand = new RelayCommand(Expand);

            // Setze Propertys
            this.Id = id;
            this.FullPath = fullPath;
            this.Type = type;
            this.Owner = owner;

            if (CanExpand || this.Type == DirectoryItemType.Server)
            {
                this.Children = new ObservableCollection<DirectoryItemViewModel>
                {
                    null
                };
            }
                
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Löscht alle Kinder in der Liste. Fügt ein Dummy Item hinzu damit das Symbol zum aufklappen angezeit wird, falls es nötig ist.
        /// </summary>
        private void ClearChildren()
        {
            // Prüft ob das aktuelle Item aktuell Kinder hat
            bool hasChilds = (this.Children.Count() > 0) ? true : false;

            // Löscht alle Items aus der Liste
            this.Children = new ObservableCollection<DirectoryItemViewModel>();

            // Fügt ein Dummy Item hinzu, falls das Item vorher Kinder hatte
            if(hasChilds)
                this.Children.Add(null);
        }

        #endregion

        /// <summary>
        /// Klappt das Verzeichnis auf und findet alle Kinder
        /// </summary>
        private void Expand()
        {
            // Item hat keine Kinder und kann daher nicht aufgeklappt werden
            if (this.CanExpand == false)
                return;

            // Finde alle Kinder
            var children = (this.Id < 0) ? DirectoryStructure.GetChildren(this.FullPath) : DirectoryStructure.GetChildren(this.Id);
            this.Children = new ObservableCollection<DirectoryItemViewModel>(
                children.Select(c => new DirectoryItemViewModel(c.Id, c.FullPath, c.Type, c.Owner)));
        }
    }
}
