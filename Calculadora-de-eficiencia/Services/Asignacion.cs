using System;
using System.Collections.Generic;
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
        { "comparacion", "1" }
    };

    public void Recorrer(SyntaxNode nodo)
    {
        string resultado = ObtenerExpresionManual(nodo);
        ConsolaVirtual.Escribir("\n--- Expansión algebraica paso a paso ---");
        ConsolaVirtual.Escribir("T(n) = " + resultado);

        ResolverFormula(resultado);
    }

    private string ObtenerExpresionManual(SyntaxNode nodo)
    {
        var resultado = new List<string>();

        foreach (var hijo in nodo.ChildNodes())
        {
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
        }

        return string.Join(" + ", resultado);
    }

    private void ProcesarExpresion(ExpressionSyntax expr, List<string> resultado)
    {
        // ⚠️ Caso nuevo: paréntesis
        if (expr is ParenthesizedExpressionSyntax parentesis)
        {
            // Recurse into the inner expression
            ProcesarExpresion(parentesis.Expression, resultado);
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
            ConsolaVirtual.Escribir("T(n) simplificada ~ " + Infix.Format(simplificada));

            ConsolaVirtual.Escribir("\n--- Análisis de cotas ---");
            string simpl = Infix.Format(simplificada);
            if (simpl.Contains("n^2"))
                ConsolaVirtual.Escribir("Cota superior: O(n^2)\nCota promedio: aproximadamente cuadrática\nCota inferior: O(1)");
            else if (simpl.Contains("n"))
                ConsolaVirtual.Escribir("Cota superior: O(n)\nCota promedio: aproximadamente lineal\nCota inferior: O(1)");
            else
                ConsolaVirtual.Escribir("Cota superior: O(1)\nCota promedio: constante\nCota inferior: O(1)");
        }
        catch (Exception ex)
        {
            ConsolaVirtual.Escribir(" Error al resolver la expresión: " + ex.Message);
        }
    }
}