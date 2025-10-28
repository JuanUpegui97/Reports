using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnalizadorAuditoria.Methods; 
using AnalizadorAuditoria.Reports; 
using Microsoft.Extensions.Configuration;

namespace AnalizadorAuditoria
{
    public class Program
    {
        static void Main(string[] args)
        {
            //bandera para cobol si el programa funciono 
            int exitCode = 0;
            try
            {
                //conexion sql
                string connectionString = "";
                try
                {

                    // extrae la cadena de conexion BD de appsettings.json
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .Build();
                    //la conexion de cadea queda lista para ser utilzida
                    connectionString = configuration.GetConnectionString("DefaultConnection");
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        Console.Error.WriteLine("Error Crítico: No se encontró 'DefaultConnection' en appsettings.json.");
                        exitCode = 1; Environment.Exit(exitCode);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"\nError al leer la configuración: {ex.Message}");
                    exitCode = 1; Environment.Exit(exitCode);
                }
           
                //Diccionario para guardar key y valor de los parametros
                var filters = new Dictionary<string, string>();
                

                if (args.Length == 0)
                {
                    Console.Error.WriteLine("Error: No se proporcionaron argumentos de filtro.");
                    exitCode = 1; Environment.Exit(exitCode);
                }

                //Se procesan los parametro ingresados 
                for (int i = 0; i < args.Length; i++)
                {


                    //Se divide arg = key y nextArg = valor
                    string arg = args[i].ToLower();
                    string nextArg = (i + 1 < args.Length) ? args[i + 1] : null;

                    //Para revisar como estan entrando los parametros
                    //Console.WriteLine($"arg: {arg}, nextArg: {nextArg}");
                    //Console.ReadLine();

                    switch (arg)
                    {
                        case "-searchxml":
                        case "-fechaini":
                        case "-fechafin":
                        case "-programa":
                        case "-usuarioadm":
                        case "-archivo":
                        case "-estado":
                        
       

                            //si cumple ambas condiciones se guarda en diccionario en filters
                            if (nextArg != null && !nextArg.StartsWith("-"))
                            {
                                filters[arg] = nextArg;
                                i++;
                            }
                            else
                            {
                                Console.Error.WriteLine($"Error: Falta el valor para el argumento {arg}.");
                                exitCode = 1; Environment.Exit(exitCode);
                            }
                            break;

                        default:
                            Console.WriteLine($"Advertencia: Argumento '{arg}' desconocido, será ignorado.");
                            break; 
                    }
                }

               
                if (filters.Count == 0)
                {


                    RegistrarError($"Hacen falta parametros");
                    exitCode = 1; Environment.Exit(exitCode);
                }

                // ruta para generar el pdf 
                string outputFolder = @"C:\sxg5db\Lst\Reportes"; 
                string pdfFileName = ""; 
                string pdfFilePath = "";

                // con la clase Directory podemos manipular archivos en IO
                //Verifica si la ruta existe o si no la crea 
                try { if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder); }
                catch (Exception ex)
                {
                    RegistrarError($"No se pudo crear el directorio: {ex.Message}");
                    exitCode = 1; Environment.Exit(exitCode);
                }

                
                string reportTitle = "Auditoría";
                // Se crea una lista con propiedades de AuditRecord para guradar la informacion que va traer el metodo AuditHistoryFinder
                List<AuditRecord> history;

                //Se crea un instancia y se envia la cadena conexion BD para realaizar la consulta
                var historyFinder = new AuditHistoryFinder(connectionString);

                // Logica para buscar por cualquier campo del XML 
                if (filters.Count == 1 && filters.ContainsKey("-searchxml"))
                {
                    string userValue = filters["-searchxml"];
                    reportTitle += $" | Historial Usuario : {userValue}";
                    //Console.WriteLine($"Buscando historial (LIKE en XML) para '{userValue}'...");
                    history = historyFinder.FindHistoryByXmlLikeSearch(userValue);
                    // Generar nombre de archivo PDF
                    pdfFileName = $"Auditoria_Usuario_{userValue}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                }
                else
                {
                    
                    string filterDesc = ""; 
                    //Para recorrer todos los parametro enviados por el usuario
                    // Logica para Buscar registro con mas de un parametro
                    foreach (var filter in filters)
                    {
                        //Arma nombre para mostrar en CMD
                        reportTitle += $" | {filter.Key.Substring(1)}: {filter.Value}";
                        //Arma el nombre para el archivo PDF
                        filterDesc += $"_{filter.Key.Substring(1)}_{filter.Value}"; 
                    }
                    Console.WriteLine($"Buscando registros con filtros: {reportTitle}");            
                    history = historyFinder.FindHistoryByFilters(filters);
                    pdfFileName = $"Auditoria_Filtros{filterDesc}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                }

                //Construye todo la ruta completa nombre del PDF y donde va quedar
                pdfFilePath = Path.Combine(outputFolder, pdfFileName); 

                // Procesar resultados y generar PDF enviando 3 parametros
                if (history.Count == 0)
                {
                    Console.WriteLine("No se encontraron registros.");
                }
                else
                {
                    Console.WriteLine($"Se encontraron {history.Count} registros. Generando PDF...");
                    var reportGenerator = new PdfReportGenerator();
                    reportGenerator.Generate(history, reportTitle, pdfFilePath);
                    Console.WriteLine($"PDF generado en: {pdfFilePath}");
                }
            }
            catch (Exception ex)
            {
                RegistrarError($"Excepción inesperada: {ex.Message}");
                exitCode = 1;
            }
            finally
            {
                Environment.Exit(exitCode);
            }
        }

        // Método auxiliar MostrarUso 
        static void RegistrarError(string mensaje)
        {
            string folderPath = @"C:\sxg5db\Lst\Reportes"; // Solo la carpeta
            string logPath = Path.Combine(folderPath, "error_log.txt"); // Ruta completa del archivo

            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ERROR: {mensaje}";

            try
            {
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"No se pudo escribir el log: {ex.Message}");
            }
        }


    }
}