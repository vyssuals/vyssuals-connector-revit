using System;
using System.Collections;
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
        public List<HeaderData> HeaderData = new List<HeaderData>();
        public List<string> VisibleElementIds { get; set; }
        public HashSet<string> uniqueParameterNames = new HashSet<string>();
        public string Timestamp { get; set; }
        private ElementId _viewId = App.Doc.ActiveView.Id;
        private Document _doc = App.Doc;
        private ElementMulticategoryFilter excludeCategoryFilter = new ElementMulticategoryFilter(new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Cameras
            },
            true);

        public ElementProcessor()
        {
            this.Timestamp = TimestampHelper.Now();
        }

        public ICollection<ElementId> GetVisibleElementIds()
        {
            var elementIds = ViewCollector().WhereElementIsViewIndependent().ToElementIds();
            this.VisibleElementIds = elementIds.Select(x => x.ToString()).ToList();
            return elementIds;
        }

        public void CollectElements()
        {
            Debug.WriteLine("Collecting elements");
            this.Timestamp = TimestampHelper.Now();
            this.Elements = GetElements(ViewCollector());
            this.VisibleElementIds = this.Elements.Select(x => x.id).ToList();              
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
                .WherePasses(excludeCategoryFilter)
                .Select(elem => CreateVyssualsElement(elem))
                .Where(x => x != null)
                .ToList());
        }


        private VyssualsElement CreateVyssualsElement(Element elem)
        {
            if (elem == null || elem.Category == null || elem.GetTypeId() == null) return null;
            if (elem is FamilyInstance familyInstance && familyInstance.SuperComponent != null) return null;

            Dictionary<string, object> parameterDictionary = new Dictionary<string, object>();

            var instanceParameters = elem.ParametersMap.Cast<Parameter>()
                //.Where(x => x.StorageType != StorageType.ElementId)
                .ToList();

            var type = App.Doc.GetElement(elem.GetTypeId());
            var typeParameters = new List<Parameter>();
            if (type != null)
            {
                typeParameters = type.ParametersMap.Cast<Parameter>()
                //.Where(x => x.StorageType != StorageType.ElementId)
                .ToList();
            }

            if (instanceParameters.Count == 0 && typeParameters.Count == 0)
            {
                Debug.WriteLine($"No parameters found for element: {elem.Id}");
                return new VyssualsElement(elem.Id.ToString(), this.Timestamp, parameterDictionary);
            }

            ProcessParameters(instanceParameters, parameterDictionary);

            if (typeParameters.Count > 0)
            {
                ProcessParameters(typeParameters, parameterDictionary);
            }

            ProcessProperties(elem, parameterDictionary);

            return new VyssualsElement(elem.Id.ToString(), this.Timestamp, parameterDictionary);
        }

        private void ProcessProperties(Element element, Dictionary<string, object> parameterDictionary)
        {
            var properties = new Dictionary<string, string>
            {
                { "Name", element.Name },
                { "Category", element.Category.Name },
                { "Level", element.LevelId != ElementId.InvalidElementId ? _doc.GetElement(element.LevelId).Name : "No Level"},
                { "Workset", element.WorksetId.IntegerValue != 0 ? _doc.GetWorksetTable().GetWorkset(element.WorksetId).Name : "No Workset" },
                { "DesignOption", element.DesignOption != null ? element.DesignOption.Name : "No Design Option" }
            };

            foreach (var property in properties)
            {
                if (string.IsNullOrEmpty(property.Value)) continue;
                parameterDictionary[property.Key] = property.Value;
                AddHeaderData(property.Key, "string", "");
            }
        }

        private void ProcessParameters(List<Parameter> parameters, Dictionary<string, object> parameterDictionary)
        {
            foreach (Parameter param in parameters)
            {
                var paramName = param.Definition.Name;
                if (!param.HasValue) continue;

                object paramValue;
                string storageType;
                switch (param.StorageType)
                {
                    case StorageType.Double:
                        paramValue = UnitUtils.ConvertFromInternalUnits(param.AsDouble(), param.GetUnitTypeId());
                        storageType = "number";
                        break;

                    case StorageType.Integer:
                        var dataType = param.Definition.GetDataType();
                        if (dataType == SpecTypeId.Boolean.YesNo)
                        {   
                            paramValue = param.AsInteger();
                            if ((int)paramValue == 0)
                            {
                                paramValue = "No";
                            }
                            else
                            {
                                paramValue = "Yes";
                            }
                            storageType = "string";
                            break;
                        }

                        paramValue = param.AsValueString();
                        storageType = "string";

                        if (paramValue == null || string.IsNullOrEmpty(paramValue?.ToString()))
                        {
                            paramValue = param.AsInteger();
                            storageType = "number";
                            break;
                        }

                        // try to convert to integer
                        if (int.TryParse(paramValue?.ToString(), out int paramInt))
                        {
                            // successfully converted to integer, meaning the parameter value we're interested in is an integer
                            paramValue = paramInt;
                            storageType = "number";
                            break;
                        }

                        break;

                    default:
                        paramValue = param.AsValueString();
                        storageType = "string";
                        if (paramValue == null || string.IsNullOrEmpty(paramValue?.ToString())) continue;
                        break;

                }

                parameterDictionary[paramName] = paramValue;
                AddHeaderData(paramName, storageType, GetUnitSymbol(param));
            }
        }

        private void AddHeaderData(string name, string type, string unitSymbol)
        {
            if (this.uniqueParameterNames.Contains(name)) return;
            this.HeaderData.Add(new HeaderData
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
            if (!param.HasValue) return "";
            switch (param.StorageType)
            {
                case StorageType.Double:
                    return UnitUtils.ConvertFromInternalUnits(param.AsDouble(), param.GetUnitTypeId()).ToString();
                case StorageType.Integer:
                    return param.AsInteger().ToString();
                default:
                    return param.AsString();
            }
        }
    }
}
