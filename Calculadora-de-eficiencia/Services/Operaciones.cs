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

        public enum ComplexityOrder
        {
            Constant,
            Logarithmic,
            Linear,
            NLogN,
            Quadratic,
            Polynomial, // Para n^k donde k > 2
            Exponential,
            Factorial,
            Unknown
        }

        public static void AnalizarLimite(string expresion)
        {
            if (string.IsNullOrWhiteSpace(expresion))
            {
                ConsolaVirtual.Escribir("Expresión vacía. No hay análisis de límite.");
                return;
            }

            ConsolaVirtual.Escribir("\n--- Análisis de Complejidad Asintótica ---");

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

                // Nuevo método para determinar el orden y el límite
                var (order, exponent) = GetComplexityOrderAndExponent(simplificadaStr);

                // Calcular Límite
                string limite = CalcularLimiteBasadoEnOrden(order, exponent);
                ConsolaVirtual.Escribir($"Límite cuando n → ∞: {limite}");

                // Estimar Cota Asintótica (Big O)
                EstimarCotaBasadoEnOrden(order, exponent);
            }
            catch (Exception ex)
            {
                ConsolaVirtual.Escribir($"Error al analizar complejidad: {ex.Message}");
                ConsolaVirtual.Escribir($"Detalles: {ex.StackTrace}");
            }
        }

        private static (ComplexityOrder order, double exponent) GetComplexityOrderAndExponent(string expr)
        {
            expr = expr.Replace(" ", ""); // Eliminar espacios para el análisis de regex

            if (expr.Contains("n!")) return (ComplexityOrder.Factorial, 0);
            if (Regex.IsMatch(expr, @"(Pow\(\d+(\.\d*)?,\s*n\)|(\d+(\.\d*)?)\^n)")) return (ComplexityOrder.Exponential, 0);

            // Buscar el mayor exponente polinomial
            MatchCollection polynomialMatches = Regex.Matches(expr, @"n\^(\d+(\.\d+)?)");
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

            if (maxExponent > 0)
            {
                if (maxExponent == 1) return (ComplexityOrder.Linear, 1);
                if (maxExponent == 2) return (ComplexityOrder.Quadratic, 2);
                return (ComplexityOrder.Polynomial, maxExponent); // Para k > 2 o fracciones
            }

            if (expr.Contains("n*Log(")) return (ComplexityOrder.NLogN, 0);
            if (Regex.IsMatch(expr, @"\b(?:[a-zA-Z]*n(?![\^\!]))\b")) return (ComplexityOrder.Linear, 1); // Captura n simple que no fue elevado a potencia
            if (expr.Contains("Log(")) return (ComplexityOrder.Logarithmic, 0);

            // Si no contiene 'n' y no fue capturado por los patrones anteriores
            return (ComplexityOrder.Constant, 0);
        }

        private static string CalcularLimiteBasadoEnOrden(ComplexityOrder order, double exponent)
        {
            return order switch
            {
                ComplexityOrder.Factorial => "∞ (crecimiento factorial)",
                ComplexityOrder.Exponential => "∞ (crecimiento exponencial)",
                ComplexityOrder.Polynomial => $"∞ (crecimiento polinomial de grado {exponent})",
                ComplexityOrder.Quadratic => "∞ (crecimiento cuadrático)",
                ComplexityOrder.NLogN => "∞ (crecimiento n log n)",
                ComplexityOrder.Linear => "∞ (crecimiento lineal)",
                ComplexityOrder.Logarithmic => "∞ (crecimiento logarítmico)",
                ComplexityOrder.Constant => "C (constante)",
                _ => "Comportamiento indeterminado"
            };
        }

        private static void EstimarCotaBasadoEnOrden(ComplexityOrder order, double exponent)
        {
            ConsolaVirtual.Escribir("\n--- Cota asintótica (Big O) ---");

            switch (order)
            {
                case ComplexityOrder.Factorial:
                    ConsolaVirtual.Escribir("Cota superior: O(n!)");
                    break;
                case ComplexityOrder.Exponential:
                    ConsolaVirtual.Escribir("Cota superior: O(b^n) - Exponencial");
                    break;
                case ComplexityOrder.Polynomial:
                    ConsolaVirtual.Escribir($"Cota superior: O(n^{exponent}) - Polinomial");
                    break;
                case ComplexityOrder.Quadratic:
                    ConsolaVirtual.Escribir("Cota superior: O(n^2) - Cuadrática");
                    break;
                case ComplexityOrder.NLogN:
                    ConsolaVirtual.Escribir("Cota superior: O(n log n)");
                    break;
                case ComplexityOrder.Linear:
                    ConsolaVirtual.Escribir("Cota superior: O(n) - Lineal");
                    break;
                case ComplexityOrder.Logarithmic:
                    ConsolaVirtual.Escribir("Cota superior: O(log n) - Logarítmica");
                    break;
                case ComplexityOrder.Constant:
                    ConsolaVirtual.Escribir("Cota superior: O(1) - Constante");
                    break;
                default:
                    ConsolaVirtual.Escribir("Cota superior: Indeterminada");
                    break;
            }
        }
    }
}