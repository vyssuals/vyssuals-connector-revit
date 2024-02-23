using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace VyssualsConnectorRevit
{
    public class RevitElementFilter
    {
        // public property to store the parameter names and their types in a dictionary
        static public Dictionary<string, string> parametersInfo = new Dictionary<string, string>();

        static public List<VyssualsElement> CreateVyssualsElements()
        {
            /* Element elem = App.CurrentDoc.GetElement(new ElementId(767264));
             return new List<VyssualsElement>()
             {
                 CreateVyssualsElement(elem, parameterNameList)
             };*/
            List<VyssualsElement> collector = new FilteredElementCollector(App.CurrentDoc)
                  .WhereElementIsNotElementType()
                  .WhereElementIsViewIndependent()
                  .Where(x => (x.Category != null) && x.GetTypeId() != null)
                  .Select(elem => CreateVyssualsElement(elem)).ToList();
            return collector;
        }


        static private VyssualsElement CreateVyssualsElement(Element elem)
        {
            // Create a dictionary to store the parameters
            Dictionary<string, object> parameterDictionary = new Dictionary<string, object>();

            // Iterate over each parameter in the element
            foreach (Parameter param in elem.ParametersMap)
            {
                // add the parameter name and type to the dictionary parametersInfo
                parametersInfo[param.Definition.Name] = param.StorageType.ToString();
                // Add the parameter name and value to the dictionary
                parameterDictionary[param.Definition.Name] = param.AsValueString();
            }

            decimal _area = (decimal)GetBasicValue(elem, "Area");
            decimal _volume = (decimal)GetBasicValue(elem, "Volume");
            decimal _length = (decimal)GetBasicValue(elem, "Length");

            return new VyssualsElement(elem.Id.ToString(), parameterDictionary, _length, _area, _volume);
        }

        static private double GetBasicValue(Element elem, string parameterName)
        {
            Parameter parameter = elem.LookupParameter(parameterName);
            return ParameterHelper.ToMetricValue(parameter);
        }

    }
}
