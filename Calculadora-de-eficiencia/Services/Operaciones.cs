using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MathNet.Symbolics;
using Expr = MathNet.Symbolics.SymbolicExpression;
using Calculadora_de_eficiencia.Utils;

namespace Calculadora_de_eficiencia.Services
{
    public static class Operaciones
    {
        private static readonly Expression n = Expression.Symbol("n");

        public static void AnalizarLimite(string expresion)
        {
            if (string.IsNullOrWhiteSpace(expresion))
            {
                ConsolaVirtual.Escribir("Expresión vacía. No hay análisis de límite.");
                return;
            }

            ConsolaVirtual.Escribir("\n--- Análisis cuando n → ∞ ---");

            try
            {
                string exprStr = expresion.Replace("]", ")")
                                          .Replace("[", "(")
                                          .Replace("n(", "n*(");

                ConsolaVirtual.Escribir($"Expresión cruda: {exprStr}");

                var parsed = Expr.Parse(exprStr);
                Expression exprMath = parsed.Expression;
                Expression simplified = Algebraic.Expand(exprMath);

                string simplificadaStr = Infix.Format(simplified);
                ConsolaVirtual.Escribir("Expresión simplificada: " + simplificadaStr);

                string limite = CalcularLimite(simplificadaStr);
                ConsolaVirtual.Escribir($"Límite cuando n → ∞: {limite}");

                EstimarCota(simplificadaStr); // Aquí se llama al método
            }
            catch (Exception ex)
            {
                ConsolaVirtual.Escribir($"Error al analizar límite: {ex.Message}");
                ConsolaVirtual.Escribir($"Detalles: {ex.StackTrace}");
            }
        }

        private static string CalcularLimite(string exprFormateada)
        {
            string s = exprFormateada.Replace(" ", "");

            if (s.Contains("n!"))
                return "∞ (crecimiento factorial)";

            if (Regex.IsMatch(s, @"(Pow\(\d+(\.\d*)?,\s*n\)|(\d+(\.\d*)?)\^n)"))
                return "∞ (crecimiento exponencial)";

            // Modificación clave: buscar el n^k con el k más grande
            MatchCollection polynomialMatches = Regex.Matches(s, @"n\^(\d+(\.\d+)?)");
            double maxExponent = 0.0;
            if (polynomialMatches.Count > 0)
            {
                foreach (Match match in polynomialMatches)
                {
                    if (double.TryParse(match.Groups[1].Value, out double currentExponent))
                    {
                        if (currentExponent > maxExponent)
                        {
                            maxExponent = currentExponent;
                        }
                    }
                }
            }

            if (maxExponent > 2)
                return $"∞ (crecimiento polinomial de grado {maxExponent})";
            if (maxExponent == 2)
                return "∞ (crecimiento cuadrático)";
            if (maxExponent == 1)
                return "∞ (crecimiento lineal)";
            if (maxExponent > 0 && maxExponent < 1)
                return "∞ (crecimiento sublineal/raíz)";


            if (s.Contains("n*Log("))
                return "∞ (crecimiento n log n)";

            if (Regex.IsMatch(s, @"\b(?:[a-zA-Z]*n(?![\^\!]))\b"))
                return "∞ (crecimiento lineal)";

            if (s.Contains("Log("))
                return "∞ (crecimiento logarítmico)";

            if (!s.Contains("n"))
            {
                if (double.TryParse(s, out double constantValue))
                {
                    if (Math.Abs(constantValue) < 0.0001) return "0 (tiende a cero)";
                    return constantValue.ToString() + " (constante)";
                }
                return "C (constante)";
            }

            return exprFormateada + " (comportamiento indeterminado)";
        }

        private static void EstimarCota(string exprFormateada)
        {
            ConsolaVirtual.Escribir("\n--- Cota asintótica (Big O) ---");

            string expr = exprFormateada.Replace(" ", "");

            if (expr.Contains("n!"))
            {
                ConsolaVirtual.Escribir("Cota superior: O(n!)");
            }
            else if (Regex.IsMatch(expr, @"(Pow\(\d+(\.\d*)?,\s*n\)|(\d+(\.\d*)?)\^n)"))
            {
                ConsolaVirtual.Escribir("Cota superior: O(b^n) - Exponencial");
            }
            // Primero, buscamos *todas* las ocurrencias de n^k y encontramos el exponente máximo.
            else if (Regex.IsMatch(expr, @"n\^(\d+(\.\d+)?)")) // Si contiene cualquier n elevado a una potencia
            {
                MatchCollection matches = Regex.Matches(expr, @"n\^(\d+(\.\d+)?)");
                double maxExponent = 0.0; // Inicializamos el exponente máximo

                foreach (Match match in matches)
                {
                    if (double.TryParse(match.Groups[1].Value, out double currentExponent))
                    {
                        if (currentExponent > maxExponent)
                        {
                            maxExponent = currentExponent;
                        }
                    }
                }

                // Ahora que tenemos el exponente máximo, clasificamos
                if (maxExponent > 2)
                {
                    ConsolaVirtual.Escribir($"Cota superior: O(n^{maxExponent}) - Polinomial (grado alto)");
                }
                else if (maxExponent == 2)
                {
                    ConsolaVirtual.Escribir("Cota superior: O(n^2) - Cuadrática");
                }
                else if (maxExponent > 1)
                {
                    ConsolaVirtual.Escribir($"Cota superior: O(n^{maxExponent}) - Polinomial (grado intermedio)");
                }
                else if (maxExponent == 1)
                {
                    ConsolaVirtual.Escribir("Cota superior: O(n) - Lineal");
                }
                else // Exponente menor o igual a 0, o entre 0 y 1
                {
                    ConsolaVirtual.Escribir("Cota superior: O(1) - Constante (sublineal o constante)");
                }
            }
            
            else if (expr.Contains("n*Log("))
            {
                ConsolaVirtual.Escribir("Cota superior: O(n log n)");
            }
            else if (Regex.IsMatch(expr, @"\b(?:[a-zA-Z]*n(?![\^\!]))\b"))
            {
                ConsolaVirtual.Escribir("Cota superior: O(n) - Lineal");
            }
            else if (expr.Contains("Log("))
            {
                ConsolaVirtual.Escribir("Cota superior: O(log n) - Logarítmica");
            }
            else
            {
                ConsolaVirtual.Escribir("Cota superior: O(1) - Constante");
            }
        }
    }
}