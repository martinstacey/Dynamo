using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Dynamo.Configuration;
using Dynamo.Graph;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Notes;
using Dynamo.Selection;
using Dynamo.Utilities;
using Dynamo.Wpf.ViewModels.Core;
using Newtonsoft.Json;
using DynCmd = Dynamo.Models.DynamoModel;

namespace Dynamo.ViewModels
{
    public partial class NoteViewModel: ViewModelBase
    {
        private int DISTANCE_TO_PINNED_NODE = 12;
        private int DISTANCE_TO_PINNED_NODE_WITH_WARNING = 32;

        #region Events

        public event EventHandler RequestsSelection;
        public virtual void OnRequestsSelection(Object sender, EventArgs e)
        {
            if (RequestsSelection != null)
            {
                RequestsSelection(this, e);
            }
        }

        #endregion

        #region Properties

        private NoteModel _model;

        [JsonIgnore]
        public readonly WorkspaceViewModel WorkspaceViewModel;
        private int zIndex = Configurations.NodeStartZIndex; // initialize the start Z-Index of a note to the same as that of a node
        internal static int StaticZIndex = Configurations.NodeStartZIndex;

        [JsonIgnore]
        public NoteModel Model
        {
            get { return _model; }
            set 
            { 
                _model = value;
                RaisePropertyChanged("Model");
            }
        }

        /// <summary>
        /// Element's left position is two-way bound to this value
        /// </summary>
        [JsonIgnore]
        public double Left
        {
            get { return _model.X; }
            set
            {
                _model.X = value;
                RaisePropertyChanged("Left");
            }
        }

        /// <summary>
        /// Element's top position is two-way bound to this value
        /// </summary>
        [JsonIgnore]
        public double Top
        {
            get { return _model.Y; }
            set
            {
                _model.Y = value;
                RaisePropertyChanged("Top");
            }
        }

        /// <summary>
        /// ZIndex represents the order on the z-plane in which the notes and other objects appear. 
        /// </summary>
        [JsonIgnore]
        public int ZIndex
         {

            get { return zIndex; }
            set { zIndex = value; RaisePropertyChanged("ZIndex"); }
        }

        [JsonIgnore]
        public string Text
        {
            get { return _model.Text; }
            set { _model.Text = value; }
        }

        [JsonIgnore]
        public bool IsSelected
        {
            get { return _model.IsSelected; }
        }

        private NodeModel pinNode;
        public NodeModel PinNode
        {
            get { return _model.PinNode; }
        }

        #endregion

        public NoteViewModel(WorkspaceViewModel workspaceViewModel, NoteModel model)
        {
            this.WorkspaceViewModel = workspaceViewModel;
            _model = model;
            model.PropertyChanged += note_PropertyChanged;
            DynamoSelection.Instance.Selection.CollectionChanged += SelectionOnCollectionChanged;
            ZIndex = ++StaticZIndex; // places the note on top of all nodes/notes

        }

        public override void Dispose()
        {
            _model.PropertyChanged -= note_PropertyChanged;
            DynamoSelection.Instance.Selection.CollectionChanged -= SelectionOnCollectionChanged;
        }

        private void SelectionOnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CreateGroupCommand.RaiseCanExecuteChanged();
            AddToGroupCommand.RaiseCanExecuteChanged();
            UngroupCommand.RaiseCanExecuteChanged();
        }

        private void Select(object parameter)
        {
            OnRequestsSelection(this, EventArgs.Empty);
        }

        public void UpdateSizeFromView(double w, double h)
        {
            this._model.SetSize(w,h);     
        }

        private bool CanSelect(object parameter)
        {
            if (!DynamoSelection.Instance.Selection.Contains(_model))
            {
                return true;
            }
            return false;
        }

        //respond to changes on the model's properties
        void note_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "X":
                    RaisePropertyChanged("Left");
                    break;
                case "Y":
                    RaisePropertyChanged("Top");
                    break;
                case "Text":
                    RaisePropertyChanged("Text");
                    break;
                case "IsSelected":
                    RaisePropertyChanged("IsSelected");
                    break;

            }
            
        }

        private void CreateGroup(object parameters)
        {
            WorkspaceViewModel.DynamoViewModel.AddAnnotationCommand.Execute(null);
        }

        private bool CanCreateGroup(object parameters)
        {
            var groups = WorkspaceViewModel.Model.Annotations;
            //Create Group should be disabled when a group is selected
            if (groups != null && groups.Any(x => x.IsSelected))
            {
                return false;
            }

            //Create Group should be disabled when a node selected is already in a group
            if (!groups.Any(x => x.IsSelected))
            {
                var modelSelected = DynamoSelection.Instance.Selection.OfType<ModelBase>().Where(x => x.IsSelected);
                foreach (var model in modelSelected)
                {
                    if (groups.ContainsModel(model.GUID))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void UngroupNote(object parameters)
        {
            WorkspaceViewModel.DynamoViewModel.UngroupModelCommand.Execute(null);
        }

        private bool CanUngroupNote(object parameters)
        {
            var groups = WorkspaceViewModel.Model.Annotations;
            if (!groups.Any(x => x.IsSelected))
            {
                return (groups.ContainsModel(Model.GUID));
            }
            return false;
        }

        private void AddToGroup(object parameters)
        {
            WorkspaceViewModel.DynamoViewModel.AddModelsToGroupModelCommand.Execute(null);
        }

        private bool CanAddToGroup(object parameters)
        {
            var groups = WorkspaceViewModel.Model.Annotations;
            if (groups.Any(x => x.IsSelected))
            {
                return !(groups.ContainsModel(Model.GUID));
            }
            return false;
        }

        private void PinToNode(object parameters)
        {
            
            var nodeToPin = DynamoSelection.Instance.Selection
                .OfType<NodeModel>()
                .First();

            MoveNoteAbovePinNode(nodeToPin);

            var nodeViewModel = WorkspaceViewModel.Nodes.Where(x => x.Id == nodeToPin.GUID).FirstOrDefault();
            nodeViewModel.RequestsSelection += NodeViewModel_RequestsSelection;
            nodeViewModel.PropertyChanged += NodeViewModel_PropertyChanged;
            nodeViewModel.NodeModel.PropertyChanged += NodeModel_PropertyChanged;           
            Model.PinNode = nodeToPin;
            ZIndex = Convert.ToInt32(nodeViewModel.ErrorBubble.ZIndex - 1);
            RaisePropertyChanged(nameof(PinNode));
        }

        private void NodeModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "State")
            {
                MoveNoteAbovePinNode(Model.PinNode);
            }
            if (e.PropertyName == "Position")
            {
                MoveNoteAbovePinNode(Model.PinNode);
            }
        }

        private void NodeViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ZIndex")
            {
                var node = sender as NodeViewModel;
                ZIndex = Convert.ToInt32(node.ErrorBubble.ZIndex - 1);
            }
        }

        private bool CanPinToNode(object parameters)
        {
            try
            {
                var nodeToPin = DynamoSelection.Instance.Selection
                    .OfType<NodeModel>()
                    .First();
                return nodeToPin != null && Model.PinNode==null;
            }
            catch
            {
                return false;
            }
        }

        private void UnpinFromNode(object parameters)
        {
            var nodeViewModel = WorkspaceViewModel.Nodes.Where(x => x.Id == Model.PinNode.GUID).FirstOrDefault();
            nodeViewModel.RequestsSelection -= NodeViewModel_RequestsSelection;
            nodeViewModel.NodeModel.PropertyChanged -= NodeModel_PropertyChanged;
            Model.PinNode = null;
            RaisePropertyChanged(nameof(PinNode));
        }

        public void MoveNoteAbovePinNode(NodeModel nodeTopin)
        {
            var distanceToNode = DISTANCE_TO_PINNED_NODE;
            if (nodeTopin.State == ElementState.Error || nodeTopin.State == ElementState.Warning) distanceToNode = DISTANCE_TO_PINNED_NODE_WITH_WARNING;
            Model.CenterX = nodeTopin.CenterX;
            Model.CenterY = nodeTopin.CenterY - (nodeTopin.Height * 0.5) - (Model.Height * 0.5) - distanceToNode;
        }


        private void NodeViewModel_RequestsSelection(object sender, EventArgs e)
        {

            if (!(sender is NodeViewModel node) || node.Id != PinNode.GUID)
            {
                return;
            }

            DynamoSelection.Instance.Selection.AddUnique(Model);
        }

        internal void SelectNoteAndPinNode()
        {
            System.Guid noteGuid = Model.GUID;
            if (Model.PinNode == null)
            {
                WorkspaceViewModel.DynamoViewModel.ExecuteCommand(
                    new DynCmd.SelectModelCommand(noteGuid, Keyboard.Modifiers.AsDynamoType()));
            }
            else
            {
                Guid nodeGuid = Model.PinNode.GUID;
                var selectionGuids = new List<Guid> { noteGuid, nodeGuid };
                WorkspaceViewModel.DynamoViewModel.ExecuteCommand(
                    new DynCmd.SelectModelCommand(selectionGuids, Keyboard.Modifiers.AsDynamoType()));
            }
        }
    }
}
