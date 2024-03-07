using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vyssuals.ConnectorRevit
{
    public class Processor
    {
        private ElementMulticategoryFilter _excludeCategoryFilter = new ElementMulticategoryFilter(new List<BuiltInCategory>
        {
                BuiltInCategory.OST_Cameras
        },
        true);

        private readonly HashSet<string> _uniqueParameterNames = new HashSet<string>();
        private readonly List<HeaderData> _headerData = new List<HeaderData>();

        public ICollection<ElementId> GetVisibleElementIds()
        {
            return ViewCollector().WhereElementIsViewIndependent().ToElementIds();
        }

        public DataUpdate GetAllData()
        {
            var Elements = GetElements(ViewCollector());
            return new DataUpdate()
            {
                Elements = Elements, 
                HeaderData = _headerData,
                VisibleElements = Elements.Select(x => x.id).ToList()
            };
        }

        public DataUpdate GetNewData(ICollection<ElementId> changedElementIds)
        {
            var viewElementIds = ViewCollector().ToElementIds();
            var intersectedElementIds = viewElementIds.Intersect(changedElementIds).ToList();
            return new DataUpdate()
            {
                Elements = GetElements(IdCollector(intersectedElementIds)),
                HeaderData = _headerData,
                VisibleElements = viewElementIds.Select(x => x.Value.ToString()).ToList()
            };
        }

        private FilteredElementCollector IdCollector(ICollection<ElementId> elementIds)
        {
            return new FilteredElementCollector(App.Doc, elementIds).WhereElementIsNotElementType();

        }
        private FilteredElementCollector ViewCollector()
        {
            return new FilteredElementCollector(App.Doc, App.ActiveView.Id).WhereElementIsNotElementType();
        }

        private List<VyssualsElement> GetElements(FilteredElementCollector collector)
        {
            _uniqueParameterNames.Clear();
            return new List<VyssualsElement>(collector.WhereElementIsViewIndependent()
                .WherePasses(_excludeCategoryFilter)
                .Select(elem => CreateVyssualsElement(elem))
                .Where(x => x != null)
                .ToList());
        }

        private VyssualsElement CreateVyssualsElement(Element elem)
        {
            if (elem == null || elem.Category == null || elem.GetTypeId() == null) return null;
            if (elem is FamilyInstance familyInstance && familyInstance.SuperComponent != null) return null;

            Dictionary<string, object> parameterDictionary = new Dictionary<string, object>();
            List<Parameter> parameters = new List<Parameter>();

            parameters.AddRange(elem.ParametersMap.Cast<Parameter>()
                .Where(x => x.StorageType != StorageType.ElementId)
                .ToList());

            var type = App.Doc.GetElement(elem.GetTypeId());
            if (type != null)
            {
                parameters.AddRange(type.ParametersMap.Cast<Parameter>()
                .Where(x => x.StorageType != StorageType.ElementId)
                .ToList());
            }

            if (parameters.Count == 0)
            {
                Debug.WriteLine($"No parameters found for element: {elem.Id}");
                return new VyssualsElement(elem.Id.ToString(), parameterDictionary);
            }

            ProcessParameters(parameters, parameterDictionary);
            ProcessProperties(elem, parameterDictionary);

            return new VyssualsElement(elem.Id.ToString(), parameterDictionary);
        }

        private void ProcessParameters(List<Parameter> parameters, Dictionary<string, object> parameterDictionary)
        {
            if (parameters.Count == 0) return;
            foreach (Parameter param in parameters)
            {
                if (!param.HasValue) continue;
                parameterDictionary[param.Definition.Name] = GetParameterValue(param);
                AddHeaderData(param.Definition.Name, MapStorageType(param.StorageType), GetUnitSymbol(param));
            }
        }

        private void ProcessProperties(Element element, Dictionary<string, object> parameterDictionary)
        {
            var properties = new Dictionary<string, string>
            {
                { "Name", element.Name },
                { "Category", element.Category.Name },
                { "Level", element.LevelId != ElementId.InvalidElementId ? App.Doc.GetElement(element.LevelId).Name : "No Level"},
                { "Workset", element.WorksetId.IntegerValue != 0 ? App.Doc.GetWorksetTable().GetWorkset(element.WorksetId).Name : "No Workset" },
                { "DesignOption", element.DesignOption != null ? element.DesignOption.Name : "No Design Option" }
            };

            foreach (var property in properties)
            {
                var value = property.Value;
                if (value == null || value == "") continue;
                parameterDictionary[property.Key] = value;
                AddHeaderData(property.Key, "string", "");
            }
        }

        private void AddHeaderData(string name, string type, string unitSymbol)
        {
            if (this._uniqueParameterNames.Contains(name)) return;
            this._headerData.Add(new HeaderData
            {
                name = name,
                type = type,
                unitSymbol = unitSymbol
            });
            this._uniqueParameterNames.Add(name);
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
