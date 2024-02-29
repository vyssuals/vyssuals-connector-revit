using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;

namespace Vyssuals.ConnectorRevit
{
    public class ElementManager
    {
        // public property to store the parameter names and their types in a dictionary
        public List<VyssualsElement> elements;
        public List<HeaderData> headerData = new List<HeaderData>();
        // add a set to keep track of unique parameter names
        public HashSet<string> uniqueParameterNames = new HashSet<string>();

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

            ProcessParameters(instanceParameters, parameterDictionary);
            ProcessParameters(typeParameters, parameterDictionary);

            return new VyssualsElement(elem.Id.ToString(), parameterDictionary);
        }

        private void ProcessParameters(List<Parameter> parameters, Dictionary<string, object> parameterDictionary)
        {
            foreach (Parameter param in parameters)
            {

                parameterDictionary[param.Definition.Name] = GetParameterValue(param);
                if (uniqueParameterNames.Contains(param.Definition.Name))
                {
                    continue;
                }
                uniqueParameterNames.Add(param.Definition.Name);

                this.headerData.Add(new HeaderData
                {
                    name = param.Definition.Name,
                    type = MapStorageType(param.StorageType),
                    unitSymbol = GetUnitSymbol(param)
                });
            }

        }

        private string GetUnitSymbol(Parameter param)
        {
            if (param.StorageType == StorageType.Double)
            {
                ForgeTypeId unitTypeId = param.GetUnitTypeId();
                if (UnitUtils.IsUnit(unitTypeId))
                {

                    return FormatUnit(UnitUtils.GetTypeCatalogStringForUnit(unitTypeId));
                }
            }
            if (param.StorageType == StorageType.Integer)
            {
                return "Integer";
            }

            return "";
        }


        private string FormatUnit(string unit)
        {
           // remove underscore, replace with space. then use title case
            return ToTitleCase(unit.Replace("_", " "));
            
        }

        private string ToTitleCase(string str)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
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

        private string GetParameterValue(Parameter param)
        {
            switch (param.StorageType)
            {
                case StorageType.Double:
                    return param.AsDouble().ToString();
                case StorageType.Integer:
                    return param.AsInteger().ToString();
                default:
                    return param.AsString();
            }
        }
    }
}
