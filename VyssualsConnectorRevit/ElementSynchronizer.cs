using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB.Events;
using System.Linq;
using System;

namespace Vyssuals.ConnectorRevit
{
    public class ElementSynchronizer
    {
        public ElementProcessor ElementProcessor;
        private Document _document;
        private bool _syncEnabled = false;

        public event EventHandler ElementsChanged;

        private System.Timers.Timer debounceTimer;
        private HashSet<ElementId> addedElementIds = new HashSet<ElementId>();
        private HashSet<ElementId> modifiedElementIds = new HashSet<ElementId>();
        private HashSet<ElementId> deletedElementIds = new HashSet<ElementId>();

        public ElementSynchronizer(ElementProcessor elementProcessor, Document document)
        {
            this.ElementProcessor = elementProcessor;
            this._document = document;
            this._document.Application.DocumentChanged += OnDocumentChanged;

            // Initialize the timer with a debounce interval of 500 milliseconds
            debounceTimer = new System.Timers.Timer(500);
            debounceTimer.Elapsed += OnDebounceTimerElapsed;
            debounceTimer.AutoReset = false;  // So the timer only triggers once unless reset
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

            // Reset the timer every time the event is fired
            debounceTimer.Stop();
            debounceTimer.Start();
        }

        private void OnDebounceTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
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