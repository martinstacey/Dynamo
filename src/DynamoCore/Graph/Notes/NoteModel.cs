using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using Dynamo.Graph.Nodes;
using Dynamo.Selection;
using Dynamo.Utilities;

namespace Dynamo.Graph.Notes
{
    /// <summary>
    /// NoteModel represents notes in Dynamo.
    /// </summary>
    public class NoteModel : ModelBase
    {
        private int DISTANCE_TO_PINNED_NODE = 24;

        private string text;
        /// <summary>
        /// Returns the text inside the note.
        /// </summary>
        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                RaisePropertyChanged("Text");
            }
        }

        public event EventHandler OnPinSet;
        private NodeModel pinNode;
        public NodeModel PinNode
        {
            get { return pinNode; }
            set
            {
                OnPinSet(this, EventArgs.Empty);
                pinNode = value;
                if (value != null)
                {
                    RaisePropertyChanged("PinNode");
                    RaisePropertyChanged("IsPinned");
                    pinNode.PropertyChanged += OnPropertyChanged;
                    MoveNoteAbovePinNode(pinNode);
                }
            }
        }

        /// <summary>
        /// Creates NoteModel.
        /// </summary>
        /// <param name="x">X coordinate of note.</param>
        /// <param name="y">Y coordinate of note.</param>
        /// <param name="text">Text of note</param>
        /// <param name="guid">Unique id of note</param>
        public NoteModel(double x, double y, string text, Guid guid)
        {
            X = x;
            Y = y;
            Text = text;
            GUID = guid;
            
        }
        void OnPropertyChanged(object sender, PropertyChangedEventArgs blabla)
        {
            MoveNoteAbovePinNode(pinNode);
        }

        void OnNotePropertyChanged(object sender, PropertyChangedEventArgs blabla)
        {
            MovePinNodeBellowNote(pinNode);
        }

        public void MovePinNodeBellowNote(NodeModel pinNode)
        {
            if (pinNode == null) return;
            pinNode.CenterX = CenterX;
            pinNode.CenterY = CenterY + pinNode.Height * 0.5 + DISTANCE_TO_PINNED_NODE;
        }

        public void MoveNoteAbovePinNode(NodeModel nodeTopin)
        {
            if (nodeTopin == null) return;
            CenterX = nodeTopin.CenterX;
            CenterY = nodeTopin.CenterY - nodeTopin.Height * 0.5 - DISTANCE_TO_PINNED_NODE;
        }

        #region Command Framework Supporting Methods

        protected override bool UpdateValueCore(UpdateValueParams updateValueParams)
        {
            string name = updateValueParams.PropertyName;
            string value = updateValueParams.PropertyValue;

            if (name != "Text") 
                return base.UpdateValueCore(updateValueParams);
            
            Text = value;
            return true;
        }        
        #endregion

        #region Serialization/Deserialization Methods

        protected override void SerializeCore(XmlElement element, SaveContext context)
        {
            var helper = new XmlElementHelper(element);
            helper.SetAttribute("guid", GUID);
            helper.SetAttribute("text", Text);
            helper.SetAttribute("x", X);
            helper.SetAttribute("y", Y);
        }

        protected override void DeserializeCore(XmlElement nodeElement, SaveContext context)
        {
            var helper = new XmlElementHelper(nodeElement);
            GUID = helper.ReadGuid("guid", GUID);
            Text = helper.ReadString("text", "New Note");
            X = helper.ReadDouble("x", 0.0);
            Y = helper.ReadDouble("y", 0.0);

            // Notify listeners that the position of the note has changed, 
            // then parent group will also redraw itself.
            ReportPosition();
        }

        #endregion
    }
}
