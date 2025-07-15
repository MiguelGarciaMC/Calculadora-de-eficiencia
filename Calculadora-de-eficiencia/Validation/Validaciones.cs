using System;
using System.IO;

namespace Calculadora_de_eficiencia.Validation
{
    internal static class Validaciones
    {
        /// <summary>
        /// Verifica si el contenido del archivo contiene elementos básicos de un código C#.
        /// Busca using, namespace y Console.
        /// </summary>
        /// <param name="rutaArchivo">Ruta completa del archivo a analizar.</param>
        /// <returns>True si parece código C#, False en caso contrario.</returns>
        public static bool EsCodigoCSharpBasico(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo))
            {
                throw new FileNotFoundException("El archivo no existe.");
            }

            string contenido = File.ReadAllText(rutaArchivo);

            bool contieneUsing = contenido.Contains("using ");
            bool contieneNamespace = contenido.Contains("namespace ");
            bool contieneConsole = contenido.Contains("Console.");

            int indicadoresDetectados = 0;
            if (contieneUsing) indicadoresDetectados++;
            if (contieneNamespace) indicadoresDetectados++;
            if (contieneConsole) indicadoresDetectados++;

            // Considerar como C# si encuentra al menos 2 de los 3 elementos.
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
