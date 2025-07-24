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
                //ConsolaVirtual.Escribir("Expresión vacía. No hay análisis de límite.");
                return;
            }

            //ConsolaVirtual.Escribir("\n--- Análisis de Complejidad Asintótica ---");

            try
            {
                string exprStr = expresion.Replace("]", ")")
                                          .Replace("[", "(")
                                          .Replace("n(", "n*(");

                //ConsolaVirtual.Escribir($"Expresión cruda: {exprStr}");

                var parsed = Expr.Parse(exprStr);
                Expression exprMath = parsed.Expression;
                Expression simplified = Algebraic.Expand(exprMath);

                string simplificadaStr = Infix.Format(simplified);
                //ConsolaVirtual.Escribir("Expresión simplificada: " + simplificadaStr);

                // Nuevo método para determinar el orden y el límite
                var (order, exponent) = GetComplexityOrderAndExponent(simplificadaStr);

                // Calcular Límite
                string limite = CalcularLimiteBasadoEnOrden(order, exponent);
                //ConsolaVirtual.Escribir($"Límite cuando n → ∞: {limite}");

                // Estimar Cota Asintótica (Big O)
                EstimarCotaBasadoEnOrden(order, exponent);
            }
            catch (Exception ex)
            {
                //ConsolaVirtual.Escribir($"Error al analizar complejidad: {ex.Message}");
                //ConsolaVirtual.Escribir($"Detalles: {ex.StackTrace}");
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
            //ConsolaVirtual.Escribir("\n--- Cota asintótica (Big O) ---");

            switch (order)
            {
                case ComplexityOrder.Factorial:
                    //ConsolaVirtual.Escribir("Cota superior: O(n!)");
                    break;
                case ComplexityOrder.Exponential:
                    //ConsolaVirtual.Escribir("Cota superior: O(b^n) - Exponencial");
                    break;
                case ComplexityOrder.Polynomial:
                    //ConsolaVirtual.Escribir($"Cota superior: O(n^{exponent}) - Polinomial");
                    break;
                case ComplexityOrder.Quadratic:
                    //ConsolaVirtual.Escribir("Cota superior: O(n^2) - Cuadrática");
                    break;
                case ComplexityOrder.NLogN:
                    //ConsolaVirtual.Escribir("Cota superior: O(n log n)");
                    break;
                case ComplexityOrder.Linear:
                    //ConsolaVirtual.Escribir("Cota superior: O(n) - Lineal");
                    break;
                case ComplexityOrder.Logarithmic:
                    //ConsolaVirtual.Escribir("Cota superior: O(log n) - Logarítmica");
                    break;
                case ComplexityOrder.Constant:
                    //ConsolaVirtual.Escribir("Cota superior: O(1) - Constante");
                    break;
                default:
                    //ConsolaVirtual.Escribir("Cota superior: Indeterminada");
                    break;
            }
        }
        public static void SumarYMostrarTotalFormal(List<string> expresionesSimplificadas)
        {
            var sumaPorGrado = new Dictionary<string, int>();
            var terminosRecursivos = new List<string>();

            foreach (var expr in expresionesSimplificadas)
            {
                var partes = expr.Replace(" ", "").Split('+');

                foreach (var parte in partes)
                {
                    if (string.IsNullOrWhiteSpace(parte)) continue;

                    // Detectar llamadas recursivas tipo T(n-1), T(n), etc.
                    if (Regex.IsMatch(parte, @"T\(.+\)"))
                    {
                        // Agregar a lista de recursividad (sin sumar coeficientes)
                        if (!terminosRecursivos.Contains(parte))
                            terminosRecursivos.Add(parte);
                        continue;
                    }

                    if (parte.Contains("n!")) { sumaPorGrado["n!"] = int.MaxValue; continue; }
                    if (Regex.IsMatch(parte, @"\d*\*\d+\^n|\d+\^n")) { sumaPorGrado["b^n"] = int.MaxValue; continue; }

                    var nLogNMatch = Regex.Match(parte, @"^(\d+)?\*?n\*Log\(n\)$", RegexOptions.IgnoreCase);
                    if (nLogNMatch.Success)
                    {
                        int coef = nLogNMatch.Groups[1].Success ? int.Parse(nLogNMatch.Groups[1].Value) : 1;
                        sumaPorGrado["n*Log(n)"] = sumaPorGrado.GetValueOrDefault("n*Log(n)", 0) + coef;
                        continue;
                    }

                    var polynomialMatch = Regex.Match(parte, @"^(\d+)?\*?n\^(\d+(\.\d+)?)$", RegexOptions.IgnoreCase);
                    if (polynomialMatch.Success)
                    {
                        int coef = polynomialMatch.Groups[1].Success ? int.Parse(polynomialMatch.Groups[1].Value) : 1;
                        string grado = $"n^{polynomialMatch.Groups[2].Value}";
                        sumaPorGrado[grado] = sumaPorGrado.GetValueOrDefault(grado, 0) + coef;
                        continue;
                    }

                    var linearMatch = Regex.Match(parte, @"^(\d+)?\*?n$", RegexOptions.IgnoreCase);
                    if (linearMatch.Success)
                    {
                        int coef = linearMatch.Groups[1].Success ? int.Parse(linearMatch.Groups[1].Value) : 1;
                        sumaPorGrado["n"] = sumaPorGrado.GetValueOrDefault("n", 0) + coef;
                        continue;
                    }

                    var logMatch = Regex.Match(parte, @"^(\d+)?\*?Log\(n\)$", RegexOptions.IgnoreCase);
                    if (logMatch.Success)
                    {
                        int coef = logMatch.Groups[1].Success ? int.Parse(logMatch.Groups[1].Value) : 1;
                        sumaPorGrado["Log(n)"] = sumaPorGrado.GetValueOrDefault("Log(n)", 0) + coef;
                        continue;
                    }

                    if (int.TryParse(parte, out int constante))
                    {
                        sumaPorGrado["1"] = sumaPorGrado.GetValueOrDefault("1", 0) + constante;
                    }
                }
            }

            var totalFinal = new StringBuilder("T(n) simplificada total = ");
            bool esPrimero = true;

            var ordenado = sumaPorGrado.OrderByDescending(p => GetOrderScore(p.Key));

            foreach (var par in ordenado)
            {
                int coef = par.Value;
                string key = par.Key;

                if (coef == 0) continue;

                if (!esPrimero) totalFinal.Append(" + ");
                esPrimero = false;

                if (key == "1")
                    totalFinal.Append($"{coef}");
                else
                    totalFinal.Append(coef == 1 ? key : $"{coef}*{key}");
            }

            // Agregar términos recursivos exactamente como están, separados con " + "
            foreach (var term in terminosRecursivos)
            {
                if (!esPrimero) totalFinal.Append(" + ");
                totalFinal.Append(term);
                esPrimero = false;
            }

            if (esPrimero)
            {
                //ConsolaVirtual.Escribir("T(n) simplificada total = 0");
            }
                
            else
                //ConsolaVirtual.Escribir(totalFinal.ToString());

            // Opcional: analizar límite sólo si no hay recursividad
            if (terminosRecursivos.Count == 0)
            {
                string finalExprForLimit = string.Join(" + ", ordenado.Select(p =>
                {
                    if (p.Key == "1") return p.Value.ToString();
                    return p.Value == 1 ? p.Key : $"{p.Value}*{p.Key}";
                }));

                if (!string.IsNullOrWhiteSpace(finalExprForLimit))
                {
                    Operaciones.AnalizarLimite(finalExprForLimit);
                }
            }
            else
            {
                //ConsolaVirtual.Escribir("\n[⚠️] Advertencia en el análisis del límite por presencia de términos recursivos.");
            }
        }

        public static void MostrarRecurrenciaYCota(string expresion)
        {
            //ConsolaVirtual.Escribir($"T(n) = {expresion}");

            // Detectar patrones simples de recurrencia y mostrar cota
            if (Regex.IsMatch(expresion, @"T\(n\s*-\s*1\)"))
            {
                //ConsolaVirtual.Escribir("Cota estimada: O(n) (recurrencia lineal tipo T(n) = T(n-1) + c)");
            }
            else if (Regex.IsMatch(expresion, @"T\(n\s*/\s*2\)"))
            {
                if (Regex.IsMatch(expresion, @"\+.*n\s*\*?\s*log\(n\)", RegexOptions.IgnoreCase))
                {
                    //ConsolaVirtual.Escribir("Cota estimada: O(n log n) (recurrencia tipo divide y vencerás)");
                }
                    
                else if (Regex.IsMatch(expresion, @"\+.*n", RegexOptions.IgnoreCase))
                {
                    //ConsolaVirtual.Escribir("Cota estimada: O(n) (recurrencia tipo T(n) = 2T(n/2) + n)\");
                }

                else
                {
                    //ConsolaVirtual.Escribir("Cota estimada: O(log n) (recurrencia tipo T(n) = T(n/2) + c)");
                }

            }
            else if (Regex.IsMatch(expresion, @"T\(n\)"))
            {
                //ConsolaVirtual.Escribir("Cota estimada: No determinada (expresión con términos recursivos generales).");
            }
            else
            {
                //ConsolaVirtual.Escribir("No se detectaron términos recursivos para analizar.");
            }
        }

        // Auxiliar para ordenar la suma total
        private static int GetOrderScore(string key)
        {
            if (key == "n!") return 1000;
            if (key == "b^n") return 900;
            if (key.StartsWith("n^"))
            {
                var match = Regex.Match(key, @"n\^(\d+(\.\d+)?)");
                if (match.Success && double.TryParse(match.Groups[1].Value, out double exponent))
                {
                    return (int)(exponent * 100);
                }
            }
            if (key == "n*Log(n)") return 80;
            if (key == "n") return 50;
            if (key == "Log(n)") return 10;
            if (key == "1") return 1;
            return 0;
        }
        public static void CalcularAsintotas(string expresionSimplificada)
        {
            ConsolaVirtual.Escribir("\n=== Operaciones (sacando asíntota) ===");

            // Limpiar la expresión para el análisis
            string expr = expresionSimplificada.Replace("~", "").Trim();

            if (string.IsNullOrWhiteSpace(expr) || expr.Contains("T("))
            {
                ConsolaVirtual.Escribir("No se puede calcular asíntota para expresiones recursivas o vacías.");
                return;
            }

            // Determinar el término dominante
            string cotaSuperior, cotaInferior, cotaPromedio;

            if (expr.Contains("n^3"))
            {
                cotaSuperior = "O(n³)";
                cotaInferior = "Ω(n³)";
                cotaPromedio = "Θ(n³)";
            }
            else if (expr.Contains("n^2"))
            {
                cotaSuperior = "O(n²)";
                cotaInferior = "Ω(n²)";
                cotaPromedio = "Θ(n²)";
            }
            else if (expr.Contains("n*log(n)") || expr.Contains("n log n"))
            {
                cotaSuperior = "O(n log n)";
                cotaInferior = "Ω(n log n)";
                cotaPromedio = "Θ(n log n)";
            }
            else if (expr.Contains("n"))
            {
                cotaSuperior = "O(n)";
                cotaInferior = "Ω(n)";
                cotaPromedio = "Θ(n)";
            }
            else
            {
                cotaSuperior = "O(1)";
                cotaInferior = "Ω(1)";
                cotaPromedio = "Θ(1)";
            }

            ConsolaVirtual.Escribir($"lim(n→∞) T(n) = {cotaSuperior}");
            ConsolaVirtual.Escribir($"Cota superior sustituida: {cotaSuperior}");
            ConsolaVirtual.Escribir($"Cota inferior sustituida: {cotaInferior}");
            ConsolaVirtual.Escribir($"Cota promedio sustituida: {cotaPromedio}");
        }

    }
}