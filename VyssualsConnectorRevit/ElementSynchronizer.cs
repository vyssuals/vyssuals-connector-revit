using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB.Events;
using System.Linq;
using System;
using Autodesk.Revit.UI;

namespace Vyssuals.ConnectorRevit
{
    public class ElementSynchronizer
    {
        public ElementProcessor ElementProcessor;
        private readonly Document _document;
        private readonly UIApplication _uiApp = App.UiApp;
        private readonly View _activeView = App.Doc.ActiveView;
        private bool _syncEnabled = false;

        public event EventHandler ElementsChanged;

        private HashSet<ElementId> addedElementIds = new HashSet<ElementId>();
        private HashSet<ElementId> modifiedElementIds = new HashSet<ElementId>();
        private HashSet<ElementId> deletedElementIds = new HashSet<ElementId>();

        public ElementSynchronizer(ElementProcessor elementProcessor, Document document)
        {
            this.ElementProcessor = elementProcessor;
            this._document = document;
            this._document.Application.DocumentChanged += OnDocumentChanged;
            this._uiApp.Idling += OnApplicationIdling;
        }

        protected virtual void OnElementsChanged()
        {
            ElementsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            if (!_syncEnabled) return;
            Debug.WriteLine("Document changed");

            // Add the ElementIds to the respective collections
            addedElementIds.UnionWith(e.GetAddedElementIds());
            modifiedElementIds.UnionWith(e.GetModifiedElementIds());
            deletedElementIds.UnionWith(e.GetDeletedElementIds());
        }

        private void OnApplicationIdling(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
        {
            if (_activeView.IsInTemporaryViewMode(TemporaryViewMode.RevealHiddenElements)) return;

            var visibleElementIds = ElementProcessor.GetVisibleElementIds();
            // get the difference between visible elements and ElementProcessor.Elements. treat all invisible elements as deleted
            deletedElementIds.UnionWith(ElementProcessor.Elements.Select(x => x.id).Except(visibleElementIds.Select(x => x.ToString())).Select(x => new ElementId(long.Parse(x))));

            // get the difference between visible elements and ElementProcessor.Elements. treat all items in visibleElements that are not in ElementProcessor.Elements as added
            addedElementIds.UnionWith(visibleElementIds.Except(ElementProcessor.Elements.Select(x => new ElementId(long.Parse(x.id)))));

            modifiedElementIds.ExceptWith(deletedElementIds);
            modifiedElementIds.ExceptWith(addedElementIds);
            bool elementsChanged = false;

            if (modifiedElementIds.Count > 0)
            {
                UpdateElements(modifiedElementIds.ToList());
                modifiedElementIds.Clear();
                elementsChanged = true;
            }

            if (deletedElementIds.Count > 0)
            {
                RemoveElements(deletedElementIds.ToList());
                deletedElementIds.Clear();
                elementsChanged = true;
            }

            if (addedElementIds.Count > 0)
            {
                AddElements(addedElementIds.ToList());
                addedElementIds.Clear();
                elementsChanged = true;
            }

            if (elementsChanged)
            {
                OnElementsChanged();
            }
        }

        public void EnableSync()
        {
            Debug.WriteLine("Enabling sync");
            if (_activeView.IsInTemporaryViewMode(TemporaryViewMode.RevealHiddenElements)) return;
            this.ElementProcessor.CollectElements();
            _syncEnabled = true;
            OnElementsChanged();
        }

        public void DisableSync()
        {
            Debug.WriteLine("Disabling sync");
            this.ElementProcessor.Elements = null;
            _syncEnabled = false;
        }

        public void AddElements(List<ElementId> elementIds)
        {
            Debug.WriteLine("Add elements detected");

            this.ElementProcessor.AddElements(elementIds);
        }

        public void RemoveElements(List<ElementId> ElementIds)
        {
            Debug.WriteLine("Remove elements detected");

            this.ElementProcessor.RemoveElements(ElementIds.Select(x => x.ToString()).ToList());
        }

        public void UpdateElements(List<ElementId> elementIds)
        {
            Debug.WriteLine("Update elements detected");

            var modified = elementIds.Where(id => this.ElementProcessor.Elements.Any(x => x.id == id.ToString())).ToList();
            var other = elementIds.Except(modified).ToList();

            if (modified.Count > 0) { this.ElementProcessor.UpdateElements(modified); }
            if (other.Count > 0) { this.ElementProcessor.AddElements(other); }
        }
    }
}