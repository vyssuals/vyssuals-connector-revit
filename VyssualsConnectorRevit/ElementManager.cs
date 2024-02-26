using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;

namespace VyssualsConnectorRevit
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
        }


        private VyssualsElement CreateVyssualsElement(Element elem)
        {
            // Create a dictionary to store the parameters
            Dictionary<string, object> parameterDictionary = new Dictionary<string, object>();
            var parameters = elem.ParametersMap;

            if (parameters.Size == 0)
            {
                Debug.WriteLine($"No parameters found for element: {elem.Id}");
                return new VyssualsElement(elem.Id.ToString(), parameterDictionary, 0, 0, 0);
            }

            // Iterate over each parameter in the element
            foreach (Parameter param in parameters)
            {
                //Debug.WriteLine($"Category: {elem.Category.Name} | Id: {elem.Id} | param: {param.Definition.Name}");

                // add the parameter name and type to the dictionary parametersInfo
                parametersInfo[param.Definition.Name] = param.StorageType.ToString();
                // Add the parameter name and value to the dictionary
                parameterDictionary[param.Definition.Name] = param.AsValueString();
            }

            //decimal _area = (decimal)GetBasicValue(elem, "Area");
            //decimal _volume = (decimal)GetBasicValue(elem, "Volume");
            //decimal _length = (decimal)GetBasicValue(elem, "Length");

            return new VyssualsElement(elem.Id.ToString(), parameterDictionary, 0, 0, 0);
        }

        private double GetBasicValue(Element elem, string parameterName)
        {
            Parameter parameter = elem.LookupParameter(parameterName);
            return ParameterHelper.ToMetricValue(parameter);
        }

        public void LogElements()
        {
            foreach (VyssualsElement element in elements)
            {
                Debug.WriteLine($"Element ID: {element.Id}");
                Debug.WriteLine($"Length: {element.Length}");
                Debug.WriteLine($"Area: {element.Area}");
                Debug.WriteLine($"Volume: {element.Volume}");
            }
        }

    }
}
