using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FalconWpf
{
    public class XmlManager
    {

        static public void SaveXml(string strName, object obj)
        {
            XmlDocument xml = new XmlDocument();
            XmlElement rootxel = xml.CreateElement("Root");
            XmlElement childxel = fn_GetXmlFromObject(xml, obj);
            rootxel.AppendChild(childxel);
            xml.AppendChild(rootxel);
            xml.Save(strName);
        }

        static private XmlElement fn_GetXmlFromObject(XmlDocument xml, object obj)
        {
            XmlElement xel = xml.CreateElement(obj.GetType().Name);
            List<PropertyInfo> list = new List<PropertyInfo>();
            list.AddRange(obj.GetType().GetProperties());
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].PropertyType.BaseType.Name == "IPropertyChanged")
                {
                    var tempClass = list[i].GetValue(obj);
                    XmlElement temp = fn_GetXmlFromObject(xml, tempClass);
                    xel.AppendChild(temp);
                }
                else
                {
                    XmlElement temp = xml.CreateElement(list[i].Name);
                    temp.InnerText = list[i].GetValue(obj).ToString();
                    xel.AppendChild(temp);
                }
            }
            return xel;
        }

        static public void LoadXml(string strName, object obj)
        {
            XmlDocument xml = new XmlDocument();

            xml.Load(strName);

            var root = xml.DocumentElement.ChildNodes[0];
            if (root != null)
            {
                List<PropertyInfo> list = new List<PropertyInfo>();
                list.AddRange(obj.GetType().GetProperties());

                var recipe = root.ChildNodes;
                for (int i = 0; i < recipe.Count; i++)
                {
                    try
                    {
                        fn_GetObjectFromXml(recipe[i], list, obj);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        static private void fn_GetObjectFromXml(XmlNode xel, List<PropertyInfo> list, object obj)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (xel.ChildNodes.Count == 1)
                {
                    if (xel.Name == list[i].Name)
                    {
                        var tempValue = Convert.ChangeType(xel.InnerText, list[i].PropertyType);
                        list[i].SetValue(obj, tempValue);
                        break;
                    }
                }
                else
                {
                    if (xel.Name == list[i].PropertyType.Name)
                    {
                        for (int j = 0; j < xel.ChildNodes.Count; j++)
                        {
                            var tempClass = list[i].GetValue(obj);
                            List<PropertyInfo> listchild = new List<PropertyInfo>();
                            listchild.AddRange(tempClass.GetType().GetProperties());
                            fn_GetObjectFromXml(xel.ChildNodes[j], listchild, tempClass);
                        }
                    }
                }
            }
        }
    }
}
