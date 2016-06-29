﻿using System;
using System.IO;
using System.Xml.Linq;
using Knightware.Diagnostics;
using Knightware.Primitives;

namespace Knightware.Core
{
    public delegate void XmlReadFailedHandler(object sender, string propertyName);

    public class XmlDeserializer
    {
        /// <summary>
        /// Event raised when a read request fails, returning a default value
        /// </summary>
        public event XmlReadFailedHandler ElementReadFailed;
        protected void OnElementReadFailed(string elementName)
        {
            if (ElementReadFailed != null)
                ElementReadFailed(this, elementName);
        }

        public XDocument GetXDocument(Stream xmlFileStream, bool skipXmlDeclaration = true)
        {
            if (skipXmlDeclaration)
            {
                //HACK:  Pass over the header (this passes the XML declaration which specifies an encoding of 'us-ascii', which isn't supported on Windows Phone
                // <?xml version="1.0" encoding="us-ascii"?>
                xmlFileStream.Seek(42, SeekOrigin.Begin);
            }

            try
            {
                return XDocument.Load(xmlFileStream);
            }
            catch (Exception ex)
            {
                TraceQueue.Trace(null, TracingLevel.Warning, "{0} occurred while loading file stream: {1}",
                    ex.GetType().Name, ex.Message);

                return null;
            }
        }

        public int Read(XElement parent, string elementName, int defaultValue)
        {
            return Read(parent, elementName, defaultValue, (value) =>
                {
                    int response;
                    return int.TryParse(value, out response) ? response : ReturnDefaultValue(elementName, defaultValue);
                });
        }

        public float Read(XElement parent, string elementName, float defaultValue)
        {
            return Read(parent, elementName, defaultValue, (value) =>
            {
                float response;
                return float.TryParse(value, out response) ? response : ReturnDefaultValue(elementName, defaultValue);
            });
        }
        
        public bool Read(XElement parent, string elementName, bool defaultValue)
        {
            return Read(parent, elementName, defaultValue, (value) =>
            {
                if (value == "1")
                    return true;
                else if (value == "0")
                    return false;
                else
                {
                    bool response;
                    return bool.TryParse(value, out response) ? response : ReturnDefaultValue(elementName, defaultValue);
                }
            });
        }

        public string Read(XElement parent, string elementName, string defaultValue = "")
        {
            return Read(parent, elementName, defaultValue, (value) => value);
        }

        public TEnum ReadEnum<TEnum>(XElement parent, string elementName, TEnum defaultValue)
            where TEnum : struct
        {
            return Read(parent, elementName, defaultValue, (value) =>
                {
                    TEnum response;
                    return Enum.TryParse(value, out response) ? response : ReturnDefaultValue(elementName, defaultValue);
                });
        }

        protected T Read<T>(XElement parent, string elementName, T defaultValue, Func<string, T> parser)
        {
            try
            {
                XElement element = ReadElement(parent, elementName);
                if (element != null)
                    return parser(element.Value);
            }
            catch (Exception ex)
            {
                TraceQueue.Trace(null, TracingLevel.Warning, "{0} occurred while deserializing '{1}'.  Returning default value.",
                    ex.GetType().Name, ex.Message);
            }
            return ReturnDefaultValue(elementName, defaultValue);
        }

        public XElement ReadElement(XElement parent, string elementName)
        {
            if (parent == null)
                return null;

            try
            {
                return parent.Element(elementName);                
            }
            catch (Exception ex)
            {
                TraceQueue.Trace(null, TracingLevel.Warning, "{0} occurred while deserializing '{1}'.  Returning default value.",
                    ex.GetType().Name, ex.Message);

                return null;
            }
        }

        protected T ReturnDefaultValue<T>(string elementName, T defaultValue)
        {
            OnElementReadFailed(elementName);
            return defaultValue;
        }
    }
}
