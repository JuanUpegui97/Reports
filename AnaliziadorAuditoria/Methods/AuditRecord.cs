using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions; // Necesario para la limpieza
using System.Xml;
using System.Xml.Linq;

namespace AnalizadorAuditoria.Methods
{
    public class AuditRecord

    {
        // para guardar los datos que viene de la base de datos 
        public int Id { get; set; }
        public string Status { get; set; }
        public string XmlOld { get; set; }
        public string XmlNew { get; set; }
        public decimal Fecha { get; set; } 
        public decimal Hora { get; set; }  
        public string Archivo { get; set; }

        // convierte los campos PR_REG_ANTX y PR_REG_ACTX en diccionarios, quedan facil para manipular
        public Dictionary<string, string> OldAttributes => ParseWithCleaning(XmlOld);
        public Dictionary<string, string> NewAttributes => ParseWithCleaning(XmlNew);


        //Metodo que realiza esta limpieza de datos y los convierte en diccionario
        private Dictionary<string, string> ParseWithCleaning(string xml)
        {
            if (string.IsNullOrEmpty(xml)) // devuelve diccionario vacio
                return new Dictionary<string, string>();

            string cleanedXml = CleanInvalidXmlChars(xml); // coge el diccionario y lo limpia de caracteres erroneos

            // convierte el regsitro en clave y valor osea en un diccionario

            return XDocument.Parse(cleanedXml)
                .Root.Element("row")
                .Attributes()
                .ToDictionary(attr => attr.Name.LocalName, attr => attr.Value);
        }

        // segun el estado le asigna algo mas detallado para el pdf
        public string GetOperationName()
        {
            switch (Status)
            {
                case "W": return "INSERT (WRITE)";
                case "R": return "UPDATE (REWRITE)";
                case "D": return "DELETE";
                default: return "Desconocida";
            }
        }

        // limpia el xml de caracteres invalidos 
        private string CleanInvalidXmlChars(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // limpia los caracteres invalidos 
            string cleanedText = Regex.Replace(text, @"&#x[0-1]?[0-9a-fA-F];", "");

            // Verifica cada caracter y si es invalido lo elimina
            var stringBuilder = new StringBuilder();
            foreach (char c in cleanedText)
            {
                if (XmlConvert.IsXmlChar(c))
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString();
        }


        // formateo de hora y fecha 
        public string GetFormattedTimestamp()
        {
            try
            {
                
                string dateStr = Fecha.ToString();
                string formattedDate = $"{dateStr.Substring(6, 2)}/{dateStr.Substring(4, 2)}/{dateStr.Substring(0, 4)}";
                string timeStr = Hora.ToString().PadLeft(8, '0');
                string formattedTime = $"{timeStr.Substring(0, 2)}:{timeStr.Substring(2, 2)}:{timeStr.Substring(4, 2)}";

                return $"{formattedDate} {formattedTime}";
            }
            catch
            {
                return "Fecha/Hora inválida";
            }
        }
    }
}