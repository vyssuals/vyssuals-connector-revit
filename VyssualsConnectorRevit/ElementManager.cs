using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;

namespace Vyssuals.ConnectorRevit
{
    public class ElementManager
    {
        // public property to store the parameter names and their types in a dictionary
        public Dictionary<string, string> parametersInfo = new Dictionary<string, string>();
        public List<VyssualsElement> elements;

        public void GatherInitialData()
        {
            Debug.WriteLine($"Gathering initial data from View: {App.Doc.ActiveView.Name}");
            this.elements = new FilteredElementCollector(App.Doc, App.Doc.ActiveView.Id)
                 .WhereElementIsNotElementType()
                 .WhereElementIsViewIndependent()
                 .Where(x => (x.Category != null) && x.GetTypeId() != null)
                 .Select(elem => CreateVyssualsElement(elem))
                 .ToList();
            Debug.WriteLine($"Gathered {elements.Count} elements.");
        }


        private VyssualsElement CreateVyssualsElement(Element elem)
        {
            Dictionary<string, object> parameterDictionary = new Dictionary<string, object>();

            var instanceParameters = elem.ParametersMap.Cast<Parameter>()
                .Where(x => x.StorageType != StorageType.ElementId)
                .ToList();

            var type = App.Doc.GetElement(elem.GetTypeId());
            var typeParameters = type.ParametersMap.Cast<Parameter>()
                .Where(x => x.StorageType != StorageType.ElementId)
                .ToList();

            if (instanceParameters.Count == 0 && typeParameters.Count == 0)
            {
                Debug.WriteLine($"No parameters found for element: {elem.Id}");
                return new VyssualsElement(elem.Id.ToString(), parameterDictionary);
            }

            AddParametersToDictionary(instanceParameters, parameterDictionary);
            AddParametersToDictionary(typeParameters, parameterDictionary);

            return new VyssualsElement(elem.Id.ToString(), parameterDictionary);
        }

        private void AddParametersToDictionary(List<Parameter> parameters, Dictionary<string, object> parameterDictionary)
        {
            foreach (Parameter param in parameters)
            {
                parametersInfo[param.Definition.Name] = MapStorageType(param.StorageType);
                parameterDictionary[param.Definition.Name] = param.AsValueString();
            }
        }

        private string MapStorageType(StorageType storageType)
        {
            switch (storageType)
            {
                case StorageType.Double:
                    return "number";
                case StorageType.Integer:
                    return "number";
                default:
                    return "string";
            }
        }
    }
}
