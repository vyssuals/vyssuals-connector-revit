using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Vyssuals.ConnectorRevit
{
    public class ElementPainter
    {
        private List<ElementId> _paintedElements = new List<ElementId>();
        public void PaintElements(List<ColorInformation> colors)
        { 
            var patternId = GetPatternId();
            var view = App.Doc.ActiveView;

            using (Transaction t = new Transaction(App.Doc, "Vyssuals: Set Temporary Colors"))
            {
                t.Start();
                this.Clean(this._paintedElements);
                foreach (ColorInformation colorInfo in colors)
                {
                    Color color = HexToRevitRgb(colorInfo.color);
                    Color colorDarker = DarkenRevitRgb(color, 0.8);
                    var overrideSettings = new OverrideGraphicSettings();
                    overrideSettings.SetProjectionLineColor(colorDarker);
                    overrideSettings.SetSurfaceTransparency(0);
                    overrideSettings.SetSurfaceForegroundPatternId(patternId);
                    overrideSettings.SetSurfaceForegroundPatternColor(color);

                    var elementsIds = StringsToElementIds(colorInfo.ids);   
                    this._paintedElements.AddRange(elementsIds);

                    foreach (ElementId elementId in elementsIds)
                    {
                        view.SetElementOverrides(elementId, overrideSettings);
                    }
                }
                t.Commit();
            }
        }

        public void ClearPaintedElements()
        {
            using (Transaction t = new Transaction(App.Doc, "Vyssuals: Clear Temporary Colors"))
            {
                t.Start();
                this.Clean(this._paintedElements);
                t.Commit();
            }
            this._paintedElements.Clear();
        }

        private void Clean(List<ElementId> elements)
        {
            var view = App.Doc.ActiveView;
            var cleanOverrides = new OverrideGraphicSettings();
            foreach (ElementId elementId in this._paintedElements)
            {
                view.SetElementOverrides(elementId, cleanOverrides);
            }
        }

        private Color HexToRevitRgb(string hex)
        {
            // Remove '#' if it's present
            if (hex.IndexOf('#') != -1)
                hex = hex.Replace("#", "");

            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            return new Color(r, g, b);
        }

        private Color DarkenRevitRgb(Color color, double factor)
        {
            byte r = (byte)Math.Min(255, color.Red * factor);
            byte g = (byte)Math.Min(255, color.Green * factor);
            byte b = (byte)Math.Min(255, color.Blue * factor);

            return new Color(r, g, b);
        }

        private ElementId GetPatternId()
        {
            FilteredElementCollector collector = new FilteredElementCollector(App.Doc);
            ICollection<Element> patterns = collector.OfClass(typeof(FillPatternElement)).ToElements();
            return patterns.FirstOrDefault().Id;
        }

        private List<ElementId> StringsToElementIds(List<string> elementIds)
        {
            List<ElementId> ids = new List<ElementId>();
            foreach (string id in elementIds)
            {
                ids.Add(new ElementId(long.Parse(id)));
            }
            return ids;
        }
    }
}
