using System;                                                          
using System.IO;
  
namespace Calculadora_de_eficiencia.Validation
{
    internal static class Validaciones
    {
        /// Coconi Verificar si el contenido del archivo en .cs contiene elementos básicos de un código C#.
        /// Palabras clave Console.Clear,WebApplicaion.CreateBuilder,builder.Services.AddRazorPages,app.Run
        /// <param name="rutaArchivo">Ruta completa del archivo a analizar.</param>
        public static bool EsCodigoCSharpBasico(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo))
            {
                throw new FileNotFoundException("El archivo no existe.");
            }

            string contenido = File.ReadAllText(rutaArchivo);

            bool contieneUsing = contenido.Contains("using ");
            bool contieneNamespace = contenido.Contains("namespace ");
            bool contieneConsoleWriteLine = contenido.Contains("Console.WriteLine");
            bool contieneConsoleReadLine = contenido.Contains("Console.ReadLine");
            bool contieneConsoleWrite = contenido.Contains("Console.Write");
            bool contieneConsoleErrorWriteLine = contenido.Contains("Console.Error.WriteLine");
            bool contieneConsoleRead = contenido.Contains("Console.Read");
            bool contieneConsoleReadKey = contenido.Contains("Console.ReadKey");
            bool contieneConsoleWriteAsync = contenido.Contains("Console.WriteAsync");
            bool contieneConsoleBeep = contenido.Contains("Console.Beep");
            bool contieneConsoleClear = contenido.Contains("Console.Clear");
            bool contieneWebApplication = contenido.Contains("WebApplicaion.CreateBuilder");
            bool contieneBuilderServices = contenido.Contains("builder.Services.AddRazorPages");
            bool contieneAppRun = contenido.Contains("app.Run");

            int indicadoresDetectados = 0;
            if (contieneUsing) indicadoresDetectados++;
            if (contieneNamespace) indicadoresDetectados++;
            if (contieneConsoleWriteLine) indicadoresDetectados++;
            if (contieneConsoleWrite) indicadoresDetectados++;
            if (contieneConsoleReadLine) indicadoresDetectados++;
            if (contieneConsoleErrorWriteLine) indicadoresDetectados++;
            if (contieneConsoleRead) indicadoresDetectados++;
            if (contieneConsoleReadKey) indicadoresDetectados++;
            if (contieneConsoleWriteAsync) indicadoresDetectados++;
            if (contieneConsoleBeep) indicadoresDetectados++;
            if (contieneConsoleClear) indicadoresDetectados++;
            if (contieneWebApplication) indicadoresDetectados++;
            if (contieneBuilderServices) indicadoresDetectados++;
            if (contieneAppRun) indicadoresDetectados++; 

            // si encuentra al menos 2 de los 3 elementos que contien un programa en C#.
            return indicadoresDetectados >= 2;
        } 
        //Validar que el documento no este vacío
        public static bool TieneContenido(string filePath, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    errorMessage = "La ruta del archivo está vacía.";
                    return false;
                }
               
                if (!File.Exists(filePath))
                {
                    errorMessage = "El archivo no existe.";
                    return false;
                }

                string contenido = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(contenido))
                {
                    errorMessage = "El archivo está vacío o no contiene texto legible.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error al leer el archivo: {ex.Message}";
                return false;
            }
        }
    }
}
