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
        private ObservableCollection<VyssualsElement> _elements;
        public ObservableCollection<VyssualsElement> Elements
        {
            get { return _elements; }
            set
            {
                if (_elements != null)
                {
                    // Unsubscribe from the old collection's event
                    _elements.CollectionChanged -= Elements_CollectionChanged;
                }

                if (_elements != null)
                {
                    // Subscribe to the new collection's event
                    _elements.CollectionChanged += Elements_CollectionChanged;
                }

                _elements = value;
            }
        }

        public List<HeaderData> headerData = new List<HeaderData>();
        public HashSet<string> uniqueParameterNames = new HashSet<string>();
        private readonly FilteredElementCollector _viewCollector = new FilteredElementCollector(App.Doc, App.Doc.ActiveView.Id);

        private void UpdateCollection(Action action)
        {
            this.Elements.CollectionChanged -= Elements_CollectionChanged; // Unsubscribe
            action();
            this.Elements.CollectionChanged += Elements_CollectionChanged; // Subscribe
        }

        private void Elements_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Debug.WriteLine($"Elements changed: {e.Action}");

            if (e.OldItems != null)
            {
                Debug.WriteLine($"Old items count: {e.OldItems.Count}");
            }

            if (e.NewItems != null)
            {
                Debug.WriteLine($"New items count: {e.NewItems.Count}");
            }
        }

        public void CollectElements()
        {
            Debug.WriteLine("Collecting elements");
            var newElements = GetElements(this._viewCollector);
            UpdateCollection(() =>
            {
                this.Elements.Clear();
                foreach (var element in newElements)
                {
                    this.Elements.Add(element);
                }
            });
        }

        public void AddElements(ICollection<ElementId> elementIds)
        {
            Debug.WriteLine("Adding elements");
            var newElements = GetElements(this._viewCollector.IntersectWith(IdCollector(elementIds)));
            UpdateCollection(() =>
            {
                foreach (var element in newElements)
                {
                    this.Elements.Add(element);
                }
            });
        }

        public void UpdateElements(ICollection<ElementId> elementIds)
        {
            Debug.WriteLine("Updating elements");
            List<VyssualsElement> updatedElements = GetElements(this._viewCollector.IntersectWith(IdCollector(elementIds)));
            var updatedElementsDict = updatedElements.ToDictionary(e => e.id, e => e);

            UpdateCollection(() =>
            {
                for (int i = 0; i < this.Elements.Count; i++)
                {
                    var element = this.Elements[i];
                    if (updatedElementsDict.ContainsKey(element.id))
                    {
                        this.Elements[i] = updatedElementsDict[element.id];
                    }
                }
            });
        }

        public void RemoveElements(List<string> elementIds)
        {
            Debug.WriteLine("Removing elements");
            var elementsToRemove = this.Elements.Where(element => elementIds.Contains(element.id)).ToList();
            UpdateCollection(() =>
            {
                foreach (var element in elementsToRemove)
                {
                    this.Elements.Remove(element);
                }
            });
        }

        private FilteredElementCollector IdCollector(ICollection<ElementId> elementIds)
        {
            return new FilteredElementCollector(App.Doc, elementIds);
        }

        private List<VyssualsElement> GetElements(FilteredElementCollector collector)
        {
            return new ObservableCollection<VyssualsElement>(collector.WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .Where(x => (x.Category != null) && x.GetTypeId() != null)
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
