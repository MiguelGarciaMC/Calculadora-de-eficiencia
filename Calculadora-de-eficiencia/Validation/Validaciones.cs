using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calculadora_de_eficiencia.Validation
{
    internal class Validaciones
    {
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
