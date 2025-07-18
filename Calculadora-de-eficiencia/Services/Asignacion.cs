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
        { "comparacion", "1" },
        // Nuevos costos para if/else
        { "if_condicion", "1" }, // Costo de evaluar la condición del if
        { "else_if_condicion", "1" }, // Costo de evaluar la condición del else if
        { "else_bloque", "1" } // Costo base por el bloque else (podría ser 0 si solo cuenta el contenido)
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

        // Manejar BlockSyntax (cuerpos de métodos, bucles, if/else)
        if (nodo is BlockSyntax block)
        {
            foreach (var sentencia in block.Statements)
            {
                string costoSentencia = ObtenerExpresionManual(sentencia);
                if (!string.IsNullOrWhiteSpace(costoSentencia))
                {
                    resultado.Add(costoSentencia);
                }
            }
        }
        // Manejar LocalDeclarationStatementSyntax (e.g., int x = 1;)
        else if (nodo is LocalDeclarationStatementSyntax decl)
        {
            foreach (var variable in decl.Declaration.Variables)
            {
                ConsolaVirtual.Escribir($"[{variable}] Detectado: declaración (local) ␦ valor: {valoresOperacion["declaracion"]}");
                resultado.Add(valoresOperacion["declaracion"]);

                if (variable.Initializer != null)
                {
                    ConsolaVirtual.Escribir($"[{variable}] Detectado: asignación (local) ␦ valor: {valoresOperacion["asignacion"]}");
                    resultado.Add(valoresOperacion["asignacion"]);
                    ProcesarExpresion(variable.Initializer.Value, resultado);
                }
            }
        }
        // Manejar FieldDeclarationSyntax (e.g., private int myField = 0;)
        else if (nodo is FieldDeclarationSyntax campo)
        {
            foreach (var variable in campo.Declaration.Variables)
            {
                ConsolaVirtual.Escribir($"[{variable}] Detectado: declaración (campo) ␦ valor: {valoresOperacion["declaracion"]}");
                resultado.Add(valoresOperacion["declaracion"]);

                if (variable.Initializer != null)
                {
                    ConsolaVirtual.Escribir($"[{variable}] Detectado: asignación (campo) ␦ valor: {valoresOperacion["asignacion"]}");
                    resultado.Add(valoresOperacion["asignacion"]);
                    ProcesarExpresion(variable.Initializer.Value, resultado);
                }
            }
        }
        // Manejar AssignmentExpressionSyntax (e.g., x = 5;)
        else if (nodo is AssignmentExpressionSyntax assign)
        {
            ConsolaVirtual.Escribir($"[{assign}] Detectado: asignación ␦ valor: {valoresOperacion["asignacion"]}");
            resultado.Add(valoresOperacion["asignacion"]);
            ProcesarExpresion(assign.Right, resultado);
        }
        // Manejar ForStatementSyntax
        else if (nodo is ForStatementSyntax forStmt)
        {
            ConsolaVirtual.Escribir($"[{forStmt.Initializers}] Detectado: for - inicialización ␦ valor: {valoresOperacion["for_inicializacion"]}");
            resultado.Add(valoresOperacion["for_inicializacion"]); // Esto es 1 (por ej. 'int i = 0')

            // Costo de la comparación del 'for'. Esto ya es 'n + 1'
            // y abarca tanto la operación de comparación como la naturaleza de bucle.
            ConsolaVirtual.Escribir($"[{forStmt.Condition}] Detectado: for - comparación ␦ valor: {valoresOperacion["for_comparacion"]}");
            resultado.Add(valoresOperacion["for_comparacion"]); // Este es 'n + 1'

            // **Asegúrate de que NO haya una llamada adicional a ProcesarExpresion para forStmt.Condition aquí.**
            // Por ejemplo, NO debería haber una línea como: ProcesarExpresion(forStmt.Condition, resultado);
            // Si tienes esa línea, ELIMÍNALA. <--- ¡Esto es clave y ya lo tienes bien!

            ConsolaVirtual.Escribir($"[{forStmt.Incrementors}] Detectado: for - incremento ␦ valor: {valoresOperacion["for_incremento"]}");
            resultado.Add(valoresOperacion["for_incremento"]); // Este es 'n'

            string cuerpoFor = ObtenerExpresionManual(forStmt.Statement);
            if (!string.IsNullOrWhiteSpace(cuerpoFor))
            {
                resultado.Add($"n[{cuerpoFor}]");
            }
        }
        // Manejar IfStatementSyntax (aquí está la CLAVE)
        else if (nodo is IfStatementSyntax ifStmt)
        {
            // 1. Costo de la condición del IF
            ConsolaVirtual.Escribir($"[{ifStmt.Condition}] Detectado: if - condición ␦ valor: {valoresOperacion["if_condicion"]}");
            resultado.Add(valoresOperacion["if_condicion"]);
            ProcesarExpresion(ifStmt.Condition, resultado);

            // 2. Costo del bloque IF
            string cuerpoIf = ObtenerExpresionManual(ifStmt.Statement);
            if (!string.IsNullOrWhiteSpace(cuerpoIf))
            {
                resultado.Add($"({cuerpoIf})");
            }

            // 3. Manejar la cláusula ELSE (si existe)
            if (ifStmt.Else != null)
            {
                // EL CAMBIO PRINCIPAL AQUÍ:
                // La cláusula ElseClauseSyntax (ifStmt.Else) tiene un 'Statement'.
                // Si es un 'else if', su Statement será otro IfStatementSyntax.
                // Si es un 'else' final, su Statement será un BlockSyntax.
                // Vamos a procesar este 'Statement' recursivamente.
                // PERO, necesitamos añadir el costo del 'else if' o 'else' en este nivel
                // ANTES de que el recursor siga.

                // Si el Statement del Else es otro IfStatement (es decir, un "else if")
                if (ifStmt.Else.Statement is IfStatementSyntax subIfStmt)
                {
                    // Agrega el costo de la condición del ELSE IF específicamente
                    ConsolaVirtual.Escribir($"[{subIfStmt.Condition}] Detectado: else if - condición ␦ valor: {valoresOperacion["else_if_condicion"]}");
                    resultado.Add(valoresOperacion["else_if_condicion"]);
                    ProcesarExpresion(subIfStmt.Condition, resultado);

                    // Y luego procesa el cuerpo de ese ELSE IF
                    string cuerpoElseIf = ObtenerExpresionManual(subIfStmt.Statement);
                    if (!string.IsNullOrWhiteSpace(cuerpoElseIf))
                    {
                        resultado.Add($"({cuerpoElseIf})");
                    }

                    // IMPORTANTE: Si este 'else if' tiene su propio 'else' (es decir, un 'else' anidado después del 'else if'),
                    // necesitamos que ese también sea procesado. La recursión en ObtenerExpresionManual
                    // en el cuerpo del método principal ya no bastaría si estamos en un 'else if'.
                    // Por eso, la forma más limpia es hacer que el 'else if' también maneje su 'else'
                    // si existe, usando la misma lógica.
                    if (subIfStmt.Else != null)
                    {
                        string costoSubElse = ObtenerExpresionManual(subIfStmt.Else.Statement);
                        if (!string.IsNullOrWhiteSpace(costoSubElse))
                        {
                            // Aseguramos que el costo del bloque ELSE final (si viene después de un else if) se sume
                            if (subIfStmt.Else.Statement is BlockSyntax)
                            {
                                ConsolaVirtual.Escribir($"[{subIfStmt.Else.Statement}] Detectado: else - bloque ␦ valor: {valoresOperacion["else_bloque"]}");
                                resultado.Add(valoresOperacion["else_bloque"]);
                            }
                            resultado.Add($"({costoSubElse})");
                        }
                    }
                }
                // Si el Statement del Else es un BlockSyntax (es decir, un "else" final)
                else if (ifStmt.Else.Statement is BlockSyntax elseBlock)
                {
                    // Agrega el costo base del bloque ELSE
                    ConsolaVirtual.Escribir($"[{elseBlock}] Detectado: else - bloque ␦ valor: {valoresOperacion["else_bloque"]}");
                    resultado.Add(valoresOperacion["else_bloque"]);

                    // Y luego procesa el cuerpo del ELSE
                    string cuerpoElse = ObtenerExpresionManual(elseBlock);
                    if (!string.IsNullOrWhiteSpace(cuerpoElse))
                    {
                        resultado.Add($"({cuerpoElse})");
                    }
                }
            }
        }
        // Manejar ExpressionStatementSyntax (e.g., Console.WriteLine("Hello"); x = y + 1;)
        else if (nodo is ExpressionStatementSyntax exprStmt)
        {
            if (exprStmt.Expression is InvocationExpressionSyntax llamada &&
            llamada.Expression.ToString().Contains("Console.WriteLine"))
            {
                ConsolaVirtual.Escribir($"[{exprStmt}] Detectado: Console.WriteLine ␦ valor: {valoresOperacion["console_write"]}");
                resultado.Add(valoresOperacion["console_write"]);

                // !!! PROBABLE CAUSA DEL PROBLEMA AQUÍ !!!
                foreach (var arg in llamada.ArgumentList.Arguments)
                {
                    ProcesarExpresion(arg.Expression, resultado); // Esto debería estar funcionando
                }
            }
            else if (exprStmt.Expression is AssignmentExpressionSyntax exprAssign)
            {
                string costOfAssignment = ObtenerExpresionManual(exprAssign);
                if (!string.IsNullOrWhiteSpace(costOfAssignment))
                {
                    resultado.Add(costOfAssignment);
                }
            }
            else
            {
                ProcesarExpresion(exprStmt.Expression, resultado);
            }
        }
        // "Catch-all" para otros nodos contenedores o de estructura no específicos.
        // Asegura que se sigan explorando los sub-nodos para encontrar operaciones.
        else if (nodo.ChildNodes().Any())
        {
            foreach (var hijo in nodo.ChildNodes())
            {
                string sub = ObtenerExpresionManual(hijo);
                if (!string.IsNullOrWhiteSpace(sub))
                    resultado.Add(sub);
            }
        }

        return string.Join(" + ", resultado);
    }

    private void ProcesarExpresion(ExpressionSyntax expr, List<string> resultado)
    {
        if (expr is BinaryExpressionSyntax bin)
        {
            // 1. Procesar recursivamente los lados izquierdo y derecho primero
            ProcesarExpresion(bin.Left, resultado);
            ProcesarExpresion(bin.Right, resultado);

            // 2. LUEGO, agregar el costo para la operación binaria actual
            if (bin.IsKind(SyntaxKind.AddExpression) ||
                bin.IsKind(SyntaxKind.SubtractExpression) ||
                bin.IsKind(SyntaxKind.MultiplyExpression) || // <-- ¡Esta es la clave para 'i * j'!
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
        // --- NUEVAS LÍNEAS AQUÍ ---
        else if (expr is ParenthesizedExpressionSyntax parExpr)
        {
            // Si la expresión está entre paréntesis, procesa su contenido
            ProcesarExpresion(parExpr.Expression, resultado);
        }
        else if (expr is IdentifierNameSyntax || expr is LiteralExpressionSyntax)
        {
            // No agregamos costo para identificadores o literales por sí mismos,
            // ya que su "costo" se cuenta en las operaciones que los usan.
        }
        // --- FIN NUEVAS LÍNEAS ---
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