using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;

namespace Vyssuals.ConnectorRevit
{
    public class ElementProcessor
    {
        public List<VyssualsElement> Elements;

        public event EventHandler ElementsChanged;

        public List<HeaderData> headerData = new List<HeaderData>();
        public HashSet<string> uniqueParameterNames = new HashSet<string>();
        private readonly FilteredElementCollector _viewCollector = new FilteredElementCollector(App.Doc, App.Doc.ActiveView.Id).WhereElementIsNotElementType().WhereElementIsViewIndependent();
        protected virtual void OnElementsChanged()
        {
            ElementsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void CollectElements()
        {
            Debug.WriteLine("Collecting elements");
            this.Elements = GetElements(this._viewCollector);
            OnElementsChanged();
        }

        public void AddElements(ICollection<ElementId> elementIds)
        {
            Debug.WriteLine("Adding elements");
            this.Elements.AddRange(GetElements(this._viewCollector.IntersectWith(IdCollector(elementIds))));
            OnElementsChanged();
        }

        public void UpdateElements(ICollection<ElementId> elementIds)
        {
            Debug.WriteLine("Updating elements");
            List<VyssualsElement> updatedElements = GetElements(this._viewCollector.IntersectWith(IdCollector(elementIds)));
            var updatedElementsDict = updatedElements.ToDictionary(e => e.id, e => e);

            for (int i = 0; i < this.Elements.Count; i++)
            {
                var element = this.Elements[i];
                if (updatedElementsDict.ContainsKey(element.id))
                {
                    this.Elements[i] = updatedElementsDict[element.id];
                }
            }
            OnElementsChanged();
        }

        public void RemoveElements(List<string> elementIds)
        {
            Debug.WriteLine("Removing elements");
            this.Elements = this.Elements.Where(element => !elementIds.Contains(element.id)).ToList();
            OnElementsChanged();
        }

        private FilteredElementCollector IdCollector(ICollection<ElementId> elementIds)
        {
            return new FilteredElementCollector(App.Doc, elementIds).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent();
        }

        private List<VyssualsElement> GetElements(FilteredElementCollector collector)
        {
            return new List<VyssualsElement>(collector.Where(x => (x.Category != null) && x.GetTypeId() != null)
                .Select(elem => CreateVyssualsElement(elem)))
                .ToList();
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
