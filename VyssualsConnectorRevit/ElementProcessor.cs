using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Autodesk.Revit.DB;

namespace Vyssuals.ConnectorRevit
{
    public class ElementProcessor
    {
        public List<VyssualsElement> Elements = new List<VyssualsElement>();
        public List<HeaderData> headerData = new List<HeaderData>();
        public HashSet<string> uniqueParameterNames = new HashSet<string>();
        private ElementId _viewId = App.Doc.ActiveView.Id;
        private Document _doc = App.Doc;

        private ElementMulticategoryFilter excludeCategoryFilter = new ElementMulticategoryFilter(new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Cameras
            },
            true);

        public ICollection<ElementId> GetVisibleElementIds()
        {
            return ViewCollector().WhereElementIsViewIndependent().ToElementIds();
        }

        public void CollectElements()
        {
            Debug.WriteLine("Collecting elements");
            this.Elements = GetElements(ViewCollector());
        }

        public void AddElements(ICollection<ElementId> elementIds)
        {
            var viewElementIds = ViewCollector().ToElementIds();
            var idElementIds = IdCollector(elementIds).ToElementIds();
            var intersectedElementIds = viewElementIds.Intersect(idElementIds).ToList();

            if (intersectedElementIds.Count > 0)
            {
                Debug.WriteLine("Intersected elements");
                this.Elements.AddRange(GetElements(IdCollector(intersectedElementIds)));
            }
            else
            {
                Debug.WriteLine("No intersected elements");
            }
            Debug.WriteLine($"New element count: {this.Elements.Count}");
        }

        public void UpdateElements(ICollection<ElementId> elementIds)
        {
            Debug.WriteLine("Updating elements");
            Debug.WriteLine(this.Elements.Count);
            List<VyssualsElement> updatedElements = GetElements(ViewCollector().IntersectWith(IdCollector(elementIds)));
            var updatedElementsDict = updatedElements.ToDictionary(e => e.id, e => e);

            for (int i = 0; i < this.Elements.Count; i++)
            {
                var element = this.Elements[i];
                if (updatedElementsDict.ContainsKey(element.id))
                {
                    this.Elements[i] = updatedElementsDict[element.id];
                }
            }
            Debug.WriteLine(this.Elements.Count);
        }

        public void RemoveElements(List<string> elementIds)
        {
            Debug.WriteLine("Removing elements");
            Debug.WriteLine(this.Elements.Count);
            this.Elements = this.Elements.Where(element => !elementIds.Contains(element.id)).ToList();
            Debug.WriteLine(this.Elements.Count);
        }

        private FilteredElementCollector IdCollector(ICollection<ElementId> elementIds)
        {
            return new FilteredElementCollector(App.Doc, elementIds).WhereElementIsNotElementType();
                
        }
        private FilteredElementCollector ViewCollector()
        {
            return new FilteredElementCollector(App.Doc, this._viewId).WhereElementIsNotElementType();
        }

        private List<VyssualsElement> GetElements(FilteredElementCollector collector)
        {
            return new List<VyssualsElement>(collector.WhereElementIsViewIndependent()
                //.WherePasses(excludeCategoryFilter)
                .Where(x => (x.Category != null ) && x.GetTypeId() != null)
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
            var typeParameters = new List<Parameter>();
            if (type != null)
            {
                typeParameters = type.ParametersMap.Cast<Parameter>()
                .Where(x => x.StorageType != StorageType.ElementId)
                .ToList();
            }

            if (instanceParameters.Count == 0 && typeParameters.Count == 0)
            {
                Debug.WriteLine($"No parameters found for element: {elem.Id}");
                return new VyssualsElement(elem.Id.ToString(), parameterDictionary);
            }

            ProcessParameters(instanceParameters, parameterDictionary);

            if (typeParameters.Count > 0)
            {
                ProcessParameters(typeParameters, parameterDictionary);
            }

            ProcessProperties(elem, parameterDictionary);

            return new VyssualsElement(elem.Id.ToString(), parameterDictionary);
        }

        private void ProcessProperties(Element element, Dictionary<string, object> parameterDictionary)
        {
            var properties = new Dictionary<string, string>
            {
                { "Element Name", element.Name },
                { "Category", element.Category.Name },
                { "Level", element.LevelId != ElementId.InvalidElementId ? _doc.GetElement(element.LevelId).Name : "No Level"},
                { "Workset", element.WorksetId.IntegerValue != 0 ? _doc.GetWorksetTable().GetWorkset(element.WorksetId).Name : "No Workset" },
                { "DesignOption", element.DesignOption != null ? element.DesignOption.Name : "No Design Option" }
            };

            foreach (var property in properties)
            {
                parameterDictionary[property.Key] = property.Value;
                AddHeaderData(property.Key, "string", "");
            }
        }

        private void ProcessParameters(List<Parameter> parameters, Dictionary<string, object> parameterDictionary)
        {
            foreach (Parameter param in parameters)
            {
                parameterDictionary[param.Definition.Name] = GetParameterValue(param);
                AddHeaderData(param.Definition.Name, MapStorageType(param.StorageType), GetUnitSymbol(param));
            }

        }

        private void AddHeaderData(string name, string type, string unitSymbol)
        {
            if (this.uniqueParameterNames.Contains(name)) return;
            this.headerData.Add(new HeaderData
            {
                name = name,
                type = type,
                unitSymbol = unitSymbol
            });
            this.uniqueParameterNames.Add(name);
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
