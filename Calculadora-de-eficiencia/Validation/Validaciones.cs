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
    }
}
