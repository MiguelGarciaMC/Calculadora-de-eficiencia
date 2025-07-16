using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Calculadora_de_eficiencia.Services
{
    internal class Operaciones
    {
        public static Dictionary<string, int> ContarOperaciones(string rutaArchivo)
        {
            string contenido = File.ReadAllText(rutaArchivo);

            //Patrones de operaciones básicas
            var patrones = new Dictionary<string, string>
            {

                { "Suma (+)", @"\+" },
                { "Resta (-)", @"\-" },
                { "Multiplicación (*)", @"\*" },
                { "División (/)", @"\/" },
                { "Módulo (%)", @"\%" },
                { "Igualdad (==)", @"\=\=" },
                { "Diferente (!=)", @"\!\=" },
                { "Mayor (>)", @">" },
                { "Menor (<)", @"<" },
                { "AND lógico (&&)", @"&&" },
                { "OR lógico (||)", @"\|\|" },
                { "Negación (!)", @"\!" },
                { "Asignación (=)", @"(?<!=)=(?!=)" },
                { "Incremento (++)", @"\+\+" },
                { "Decremento (--)", @"\-\-" }

            };

            var resultados = new Dictionary<string, int>();

            foreach (var par in patrones)
            {
                int conteo = Regex.Matches(contenido, par.Value, RegexOptions.Compiled).Count;
                resultados[par.Key] = conteo;
            }
            return resultados;
        }
    }
}
