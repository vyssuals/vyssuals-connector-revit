using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;

namespace Vyssuals.ConnectorRevit
{
    public class Synchronizer
    {
        public event EventHandler ElementsChanged;
        public DataUpdate DataUpdate = new DataUpdate();

        private bool _syncEnabled = false;
        private bool _documentChanged = false;

        private HashSet<ElementId> _changedElementIds = new HashSet<ElementId>();

        public Synchronizer()
        {
            App.UiApp.Idling += OnApplicationIdling;
            App.Doc.Application.DocumentChanged += OnDocumentChanged;
        }

        protected virtual void OnElementsChanged()
        {
            ElementsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            if (!_syncEnabled) return;

            _changedElementIds.UnionWith(e.GetAddedElementIds());
            _changedElementIds.UnionWith(e.GetModifiedElementIds());
            _documentChanged = true;
        }

        public void OnApplicationIdling(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
        {
            if (!_syncEnabled || 
                App.ActiveView.IsInTemporaryViewMode(TemporaryViewMode.RevealHiddenElements) || 
                !_documentChanged) return;

            var processor = new Processor();
            this.DataUpdate = processor.GetNewData(_changedElementIds);

            EmitEventCleanly();
        }

        private void EmitEventCleanly()
        {
            if (this.DataUpdate.Elements.Count > 0 || 
                this.DataUpdate.VisibleElements.Count > 0)
            {
                OnElementsChanged();
                Debug.WriteLine("Elements changed");
            }

            _changedElementIds.Clear();
            _documentChanged = false;
        }

        public void EnableSync()
        {
            Debug.WriteLine("Enabling sync");
            var processor = new Processor();
            this.DataUpdate = processor.GetAllData();
            this._syncEnabled = true;

            EmitEventCleanly();
        }

        public void DisableSync()
        {
            Debug.WriteLine("Disabling sync");

            _changedElementIds.Clear();
            _syncEnabled = false;
            _documentChanged = false;
        }

        public void UnsubscribeFromEvents()
        {
            App.Doc.Application.DocumentChanged -= OnDocumentChanged;
            App.UiApp.Idling -= OnApplicationIdling;
        }
    }
}
