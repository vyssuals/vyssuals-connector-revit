using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB.Events;
using System.Linq;
using System.Collections.ObjectModel;

namespace Vyssuals.ConnectorRevit
{
    public class ElementSynchronizer
    {
        public ElementProcessor ElementProcessor;
        private Document _document;
        private bool _syncEnabled = false;

        public ElementSynchronizer(ElementProcessor elementProcessor, Document document)
        {
            this.ElementProcessor = elementProcessor;
            this._document = document;
            this._document.Application.DocumentChanged += OnDocumentChanged;
        }

        private void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            if (!_syncEnabled) return;

            List<ElementId> modifiedElementIds = e.GetModifiedElementIds().ToList();
            if (modifiedElementIds.Count > 0)
            {
                UpdateElements(modifiedElementIds);
            }

            List<ElementId> deletedElementIds = e.GetDeletedElementIds().ToList();
            if (deletedElementIds.Count > 0)
            {
                RemoveElements(deletedElementIds);
            }
        }

        public void EnableSync()
        {
            this.ElementProcessor.CollectElements();
            _syncEnabled = true;
        }

        public void DisableSync()
        {
            this.ElementProcessor.Elements = new ObservableCollection<VyssualsElement>();
            _syncEnabled = false;
        }

        public void RemoveElements(List<ElementId> ElementIds)
        {
            Debug.WriteLine("Removing elements");

            this.ElementProcessor.RemoveElements(ElementIds.Select(x => x.ToString()).ToList());
        }

        public void UpdateElements(List<ElementId> elementIds)
        {
            Debug.WriteLine("Updating elements");

            var added = elementIds.Where(id => !this.ElementProcessor.Elements.Any(x => x.id == id.ToString())).ToList();
            var modified = elementIds.Where(id => this.ElementProcessor.Elements.Any(x => x.id == id.ToString())).ToList();

            if (added.Count > 0) { this.ElementProcessor.AddElements(added); }
            if (modified.Count > 0) { this.ElementProcessor.UpdateElements(modified); }
        }
    }
}