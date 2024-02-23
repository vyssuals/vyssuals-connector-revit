﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Diagnostics;

namespace VyssualsConnectorRevit
{
    [Transaction(TransactionMode.Manual)]
    public class ExternalEventHandler : IExternalEventHandler
    {
        private readonly ExternalEvent exEvent;
        private Action ActionToExecute { get; set; }

        public ExternalEventHandler()
        {
            exEvent = ExternalEvent.Create(this);
        }

        public void Execute(UIApplication uiapp)
        {
            try
            {
                ActionToExecute?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        public void Raise(Action actionToExecute)
        {
            ActionToExecute = actionToExecute;
            exEvent.Raise();
        }

        public string GetName()
        {
            return "GeneralEventHandler";
        }
    }
}