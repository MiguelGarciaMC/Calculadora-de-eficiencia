using MathNet.Numerics;
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
                // Aritméticos
                { "Suma (+)", @"\+" },
                { "Resta (-)", @"\-" },
                { "Multiplicación (*)", @"\*" },
                { "División (/)", @"\/" },
                { "Módulo (%)", @"\%" },
                { "Incremento (++)", @"\+\+" },
                { "Decremento (--)", @"\-\-" },

                // Aritmético-Asignación
                { "Suma y asignación (+=)", @"\+=" },
                { "Resta y asignación (-=)", @"\-=" },
                { "Multiplicación y asignación (*=)", @"\*=" },
                { "División y asignación (/=)", @"\/=" },
                { "Módulo y asignación (%=)", @"%=" },
                { "Desplazamiento izquierda y asignación (<<=)", @"<<=" },
                { "Desplazamiento derecha y asignación (>>=)", @">>=" },

                // Relacionales / Comparación
                { "Igualdad (==)", @"\=\=" },
                { "Diferente (!=)", @"\!\=" },
                { "Mayor (>)", @"(?<![=])>(?![=])" },
                { "Menor (<)", @"(?<![=])<(?![=])" },
                { "Mayor o igual (>=)", @">=" },
                { "Menor o igual (<=)", @"<=" },

                // Lógicos
                { "AND lógico (&&)", @"&&" },
                { "OR lógico (||)", @"\|\|" },
                { "Negación (!)", @"(?<!!)\!(?![=])" },

                // Asignación
                { "Asignación (=)", @"(?<!=)=(?!=)" },

                // Condicional Ternario
                { "Operador ternario (?:)", @"\?.*:" },

                // Operadores de Control
                { "Condicional if", @"\bif\b" },
                { "Condicional else", @"\belse\b" },
                { "Bucle while", @"\bwhile\b" },
                { "Bucle for", @"\bfor\b" },
                { "Estructura switch", @"\bswitch\b" },
                { "Retorno return", @"\breturn\b" },

                //   Operadores Bit a Bit
                { "AND bit a bit (&)", @"(?<!&)&(?!&)" },
                { "OR bit a bit (|)", @"(?<!\|)\|(?!\|)" },
                { "XOR bit a bit (^)", @"\^" },
                { "Desplazamiento izquierda (<<)", @"<<" },
                { "Desplazamiento derecha (>>)", @">>" },

                //   Null-Coalescing
                { "Null-coalescing (??)", @"\?\?" },
                { "Null-coalescing assignment (??=)", @"\?\?=" },

                 //   Operadores de Tipo / Patrón
                { "Verificación de tipo (is)", @"\bis\b" },
                { "Conversión de tipo (as)", @"\bas\b" },
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
