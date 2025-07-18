using System;
using System.Collections.Generic;
using System.Linq;
using Calculadora_de_eficiencia.Utils;
using MathNet.Symbolics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class Asignacion
{
    private readonly Dictionary<string, string> valoresOperacion = new()
    {
        { "declaracion", "1" },
        { "asignacion", "1" },
        { "for_inicializacion", "1" },
        { "for_comparacion", "n + 1" },
        { "for_incremento", "n" },
        { "aritmetica", "1" },
        { "logica", "1" },
        { "console_write", "1" },
        { "comparacion", "1" },
        { "acceso_arreglo", "1" }
    };

    public void Recorrer(SyntaxNode nodo)
    {
        ConsolaVirtual.Escribir("\n--- Análisis separado por clases y métodos públicos ---");

        var clases = nodo.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

        if (clases.Count == 0)
        {
            ConsolaVirtual.Escribir("\n[INFO] No se encontraron clases.");
            return;
        }

        var expresionesSimplificadas = new List<string>();

        foreach (var clase in clases)
        {
            ConsolaVirtual.Escribir($"\n===== CLASE DETECTADA: {clase.Identifier.Text} =====");

            string resultadoClase = ObtenerExpresionManual_SoloCuerpoClase(clase);

            ConsolaVirtual.Escribir($"\n--- Operaciones internas de la clase {clase.Identifier.Text} ---");
            if (string.IsNullOrWhiteSpace(resultadoClase))
            {
                ConsolaVirtual.Escribir("T(n) = 0");
                ConsolaVirtual.Escribir($"Total de operaciones detectadas en la clase: 0");
            }
            else
            {
                ConsolaVirtual.Escribir("T(n) = " + resultadoClase);

                int totalOperacionesClase = resultadoClase.Split('+').Select(x => x.Trim()).Count(x => !string.IsNullOrEmpty(x));
                ConsolaVirtual.Escribir($"Total de operaciones detectadas en la clase: {totalOperacionesClase}");

                string simplificadaClase = ResolverFormulaYObtener(resultadoClase);
                expresionesSimplificadas.Add(simplificadaClase);
            }

            var metodosPublicos = clase.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)))
                .ToList();

            if (metodosPublicos.Count == 0)
            {
                ConsolaVirtual.Escribir($"[INFO] La clase {clase.Identifier.Text} no contiene métodos públicos.");
            }

            foreach (var metodo in metodosPublicos)
            {
                ConsolaVirtual.Escribir($"\n--- MÉTODO PÚBLICO DETECTADO: {metodo.Identifier.Text} en {clase.Identifier.Text} ---");

                string resultadoMetodo = ObtenerExpresionManual(metodo);

                ConsolaVirtual.Escribir($"T(n) = {resultadoMetodo}");

                int totalOperacionesMetodo = resultadoMetodo.Split('+').Select(x => x.Trim()).Count(x => !string.IsNullOrEmpty(x));
                ConsolaVirtual.Escribir($"Total de operaciones detectadas en el método: {totalOperacionesMetodo}");

                string simplificadaMetodo = ResolverFormulaYObtener(resultadoMetodo);
                expresionesSimplificadas.Add(simplificadaMetodo);
            }
        }

        ConsolaVirtual.Escribir("\n\ntotal DE T(n) simplificada");
        foreach (var expr in expresionesSimplificadas)
        {
            ConsolaVirtual.Escribir("T(n) simplificada ~ " + expr);
        }

        // --- Suma total formal ---
        int totalConstante = 0;
        int totalLineal = 0;
        int totalCuadratica = 0;

        foreach (var expr in expresionesSimplificadas)
        {
            var partes = expr.Replace(" ", "").Split('+');

            foreach (var parte in partes)
            {
                if (parte == "n")
                    totalLineal += 1;
                else if (parte == "n^2")
                    totalCuadratica += 1;
                else if (parte.EndsWith("*n^2"))
                {
                    int coef = int.Parse(parte.Replace("*n^2", ""));
                    totalCuadratica += coef;
                }
                else if (parte.EndsWith("*n"))
                {
                    int coef = int.Parse(parte.Replace("*n", ""));
                    totalLineal += coef;
                }
                else
                {
                    if (int.TryParse(parte, out int constante))
                        totalConstante += constante;
                }
            }
        }

        ConsolaVirtual.Escribir("\nSuma");
        ConsolaVirtual.Escribir($"T(n) simplificada total= {totalConstante} + {totalLineal}*n + {totalCuadratica}*n^2");
    }

    private string ObtenerExpresionManual_SoloCuerpoClase(SyntaxNode nodo)
    {
        var resultado = new List<string>();

        foreach (var hijo in nodo.ChildNodes())
        {
            if (hijo is MethodDeclarationSyntax)
                continue;

            resultado.AddRange(ObtenerExpresionManualPorTipo(hijo));
        }

        return string.Join(" + ", resultado);
    }

    private string ObtenerExpresionManual(SyntaxNode nodo)
    {
        var resultado = new List<string>();

        foreach (var hijo in nodo.ChildNodes())
        {
            resultado.AddRange(ObtenerExpresionManualPorTipo(hijo));
        }

        return string.Join(" + ", resultado);
    }

    private List<string> ObtenerExpresionManualPorTipo(SyntaxNode hijo)
    {
        var resultado = new List<string>();

        switch (hijo)
        {
            case LocalDeclarationStatementSyntax decl:
                foreach (var variable in decl.Declaration.Variables)
                {
                    if (variable.Initializer != null)
                    {
                        ConsolaVirtual.Escribir($"[{variable}] Detectado: asignación (local) ␦ valor: {valoresOperacion["asignacion"]}");
                        resultado.Add(valoresOperacion["asignacion"]);
                        ProcesarExpresion(variable.Initializer.Value, resultado);
                    }
                    else
                    {
                        ConsolaVirtual.Escribir($"[{variable}] Detectado: declaración (local) ␦ valor: {valoresOperacion["declaracion"]}");
                        resultado.Add(valoresOperacion["declaracion"]);
                    }
                }
                break;

            case FieldDeclarationSyntax campo:
                foreach (var variable in campo.Declaration.Variables)
                {
                    if (variable.Initializer != null)
                    {
                        ConsolaVirtual.Escribir($"[{variable}] Detectado: asignación (campo) ␦ valor: {valoresOperacion["asignacion"]}");
                        resultado.Add(valoresOperacion["asignacion"]);
                        ProcesarExpresion(variable.Initializer.Value, resultado);
                    }
                    else
                    {
                        ConsolaVirtual.Escribir($"[{variable}] Detectado: declaración (campo) ␦ valor: {valoresOperacion["declaracion"]}");
                        resultado.Add(valoresOperacion["declaracion"]);
                    }
                }
                break;

            case AssignmentExpressionSyntax assign:
                ConsolaVirtual.Escribir($"[{assign}] Detectado: asignación ␦ valor: {valoresOperacion["asignacion"]}");
                resultado.Add(valoresOperacion["asignacion"]);
                ProcesarExpresion(assign.Right, resultado);
                break;

            case ForStatementSyntax forStmt:
                ConsolaVirtual.Escribir($"[{forStmt.Initializers}] Detectado: for - inicialización ␦ valor: {valoresOperacion["for_inicializacion"]}");
                resultado.Add(valoresOperacion["for_inicializacion"]);

                ConsolaVirtual.Escribir($"[{forStmt.Condition}] Detectado: for - comparación ␦ valor: {valoresOperacion["for_comparacion"]}");
                resultado.Add(valoresOperacion["for_comparacion"]);

                ConsolaVirtual.Escribir($"[{forStmt.Incrementors}] Detectado: for - incremento ␦ valor: {valoresOperacion["for_incremento"]}");
                resultado.Add(valoresOperacion["for_incremento"]);

                string cuerpo = ObtenerExpresionManual(forStmt.Statement);
                resultado.Add($"n[{cuerpo}]");
                break;

            case ExpressionStatementSyntax exprStmt:
                if (exprStmt.Expression is InvocationExpressionSyntax llamada &&
                    llamada.Expression.ToString().Contains("Console.WriteLine"))
                {
                    ConsolaVirtual.Escribir($"[{exprStmt}] Detectado: Console.WriteLine ␦ valor: {valoresOperacion["console_write"]}");
                    resultado.Add(valoresOperacion["console_write"]);

                    foreach (var arg in llamada.ArgumentList.Arguments)
                    {
                        ProcesarExpresion(arg.Expression, resultado);
                    }
                }
                else if (exprStmt.Expression is AssignmentExpressionSyntax exprAssign)
                {
                    ConsolaVirtual.Escribir($"[{exprAssign}] Detectado: asignación (expresión) ␦ valor: {valoresOperacion["asignacion"]}");
                    resultado.Add(valoresOperacion["asignacion"]);
                    ProcesarExpresion(exprAssign.Right, resultado);
                }
                break;

            default:
                string sub = ObtenerExpresionManual(hijo);
                if (!string.IsNullOrWhiteSpace(sub))
                    resultado.Add(sub);
                break;
        }

        return resultado;
    }

    private void ProcesarExpresion(ExpressionSyntax expr, List<string> resultado)
    {
        // ⚠️ Caso nuevo: paréntesis
        if (expr is ParenthesizedExpressionSyntax parentesis)
        {
            // Recurse into the inner expression
            ProcesarExpresion(parentesis.Expression, resultado);
        }
        else if (expr is ElementAccessExpressionSyntax acceso)
        {
            ConsolaVirtual.Escribir($"[{acceso}] Detectado: acceso a arreglo ␦ valor: {valoresOperacion["acceso_arreglo"]}");
            resultado.Add(valoresOperacion["acceso_arreglo"]);

            // Analizar todos los índices dentro de los corchetes
            foreach (var arg in acceso.ArgumentList.Arguments)
            {
                ProcesarExpresion(arg.Expression, resultado);
            }

            // Recurre sobre la expresión base (por ejemplo: tensor en tensor[1][2])
            ProcesarExpresion(acceso.Expression, resultado);
        }
        else if (expr is BinaryExpressionSyntax bin)
        {
            // Recorrer lado izquierdo y derecho primero
            ProcesarExpresion(bin.Left, resultado);
            ProcesarExpresion(bin.Right, resultado);

            // Clasificar tipo de operación
            if (bin.IsKind(SyntaxKind.AddExpression) ||
                bin.IsKind(SyntaxKind.SubtractExpression) ||
                bin.IsKind(SyntaxKind.MultiplyExpression) ||
                bin.IsKind(SyntaxKind.DivideExpression))
            {
                ConsolaVirtual.Escribir($"[{bin}] Detectado: operación aritmética ␦ valor: {valoresOperacion["aritmetica"]}");
                resultado.Add(valoresOperacion["aritmetica"]);
            }
            else if (bin.IsKind(SyntaxKind.EqualsExpression) ||
                     bin.IsKind(SyntaxKind.NotEqualsExpression) ||
                     bin.IsKind(SyntaxKind.LessThanExpression) ||
                     bin.IsKind(SyntaxKind.LessThanOrEqualExpression) ||
                     bin.IsKind(SyntaxKind.GreaterThanExpression) ||
                     bin.IsKind(SyntaxKind.GreaterThanOrEqualExpression))
            {
                ConsolaVirtual.Escribir($"[{bin}] Detectado: comparación lógica ␦ valor: {valoresOperacion["comparacion"]}");
                resultado.Add(valoresOperacion["comparacion"]);
            }
            else if (bin.IsKind(SyntaxKind.LogicalAndExpression) ||
                     bin.IsKind(SyntaxKind.LogicalOrExpression))
            {
                ConsolaVirtual.Escribir($"[{bin}] Detectado: operación lógica ␦ valor: {valoresOperacion["logica"]}");
                resultado.Add(valoresOperacion["logica"]);
            }
        }
        // Si quieres extender más tipos de expresiones, puedes seguir con otros `else if`
    }

    public void ResolverFormula(string expresion)
    {
        ConsolaVirtual.Escribir("\n--- Resolución simbólica (con MathNet.Symbolics) ---");

        string expr = expresion.Replace("]", ")")
                               .Replace("[", "(")
                               .Replace("n(", "n*(");

        ConsolaVirtual.Escribir("Expandida: " + expr);

        try
        {
            var parsed = Infix.ParseOrThrow(expr);
            var simplificada = Algebraic.Expand(parsed);
            string resultado = Infix.Format(simplificada);

            ConsolaVirtual.Escribir("T(n) simplificada ~ " + resultado);

            ConsolaVirtual.Escribir("\n--- Análisis de cotas ---");
            if (resultado.Contains("n^2"))
                ConsolaVirtual.Escribir("Cota superior: O(n^2)\nCota promedio: aproximadamente cuadrática\nCota inferior: O(1)");
            else if (resultado.Contains("n"))
                ConsolaVirtual.Escribir("Cota superior: O(n)\nCota promedio: aproximadamente lineal\nCota inferior: O(1)");
            else
                ConsolaVirtual.Escribir("Cota superior: O(1)\nCota promedio: constante\nCota inferior: O(1)");

            return resultado;
        }
        catch (Exception ex)
        {
            ConsolaVirtual.Escribir(" Error al resolver la expresión: " + ex.Message);
            return "Error";
        }
    }
}

