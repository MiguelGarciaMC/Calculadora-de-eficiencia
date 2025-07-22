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
        { "while_comparacion", "n + 1" },
        { "dowhile_comparacion", "n + 1" },
        { "acceso_arreglo", "1" },
        { "foreach", "n" },
        { "switch", "1" },
        { "case", "1" }
    };

    public void Recorrer(SyntaxNode nodo)
    {
        ConsolaVirtual.Escribir("\n--- Análisis separado por clases y métodos ---");

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

                string simplificadaClase = ResolverFormula(resultadoClase);
                expresionesSimplificadas.Add(simplificadaClase);
            }

            var metodosPublicos = clase.Members
                .OfType<MethodDeclarationSyntax>()
                .ToList();

            if (metodosPublicos.Count == 0)
            {
                ConsolaVirtual.Escribir($"[INFO] La clase {clase.Identifier.Text} no contiene métodos públicos.");
            }

            foreach (var metodo in metodosPublicos)
            {
                ConsolaVirtual.Escribir($"\n--- MÉTODO DETECTADO: {metodo.Identifier.Text} en {clase.Identifier.Text} ---");

                string resultadoMetodo = ObtenerExpresionManual(metodo);
                
                ConsolaVirtual.Escribir($"T(n) = {resultadoMetodo}");

                int totalOperacionesMetodo = resultadoMetodo.Split('+').Select(x => x.Trim()).Count(x => !string.IsNullOrEmpty(x));
                ConsolaVirtual.Escribir($"Total de operaciones detectadas en el método: {totalOperacionesMetodo}");

                string simplificadaMetodo = ResolverFormula(resultadoMetodo);
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

    private int EvaluarComplejidad(string formula)
    {
        try
        {
            var parsed = Infix.ParseOrThrow(formula.Replace("n(", "n*(").Replace("[", "(").Replace("]", ")"));
            var simplificada = Algebraic.Expand(parsed);
            string result = Infix.Format(simplificada);

            // Solo cuenta n^2 como 100, n como 10, y constante como 1 para comparación
            int score = 0;
            foreach (var term in result.Replace(" ", "").Split('+'))
            {
                if (term.Contains("n^2"))
                {
                    int coef = term.Contains("*") ? int.Parse(term.Split("*")[0]) : 1;
                    score += coef * 100;
                }
                else if (term.Contains("n"))
                {
                    int coef = term.Contains("*") ? int.Parse(term.Split("*")[0]) : 1;
                    score += coef * 10;
                }
                else
                {
                    int.TryParse(term, out int c);
                    score += c;
                }
            }
            return score;
        }
        catch
        {
            return int.MaxValue;  // Si falla, consideramos que no se puede comparar bien
        }
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
        //ConsolaVirtual.Escribir($"→ Analizando nodo tipo: {hijo.Kind()}");
        var resultado = new List<string>();

        switch (hijo)
        {
            case LocalDeclarationStatementSyntax decl:
                foreach (var variable in decl.Declaration.Variables)
                {
                    if (variable.Initializer != null)
                    {
                        ConsolaVirtual.Escribir($"[{variable}] Detectado: declaración + asignación (local) ␦ valor: {valoresOperacion["declaracion"]} + {valoresOperacion["asignacion"]}");
                        resultado.Add(valoresOperacion["declaracion"]);
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
                        ConsolaVirtual.Escribir($"[{variable}] Detectado: declaración + asignación (campo) ␦ valor: {valoresOperacion["declaracion"]} + {valoresOperacion["asignacion"]}");
                        resultado.Add(valoresOperacion["declaracion"]);
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
                // Si hay declaración dentro del for (ej: int i = 0)
                if (forStmt.Declaration != null)
                {
                    foreach (var variable in forStmt.Declaration.Variables)
                    {
                        if (variable.Initializer != null)
                        {
                            ConsolaVirtual.Escribir($"[{variable}] Detectado: declaración + asignación (for) ␦ valor: {valoresOperacion["declaracion"]} + {valoresOperacion["asignacion"]}");
                            resultado.Add(valoresOperacion["declaracion"]);
                            resultado.Add(valoresOperacion["asignacion"]);
                            ProcesarExpresion(variable.Initializer.Value, resultado);
                        }
                        else
                        {
                            ConsolaVirtual.Escribir($"[{variable}] Detectado: declaración (for) ␦ valor: {valoresOperacion["declaracion"]}");
                            resultado.Add(valoresOperacion["declaracion"]);
                        }
                    }
                }

                // Inicializadores adicionales (ej: i = 0, j = 0, etc.)
                foreach (var init in forStmt.Initializers)
                {
                    ConsolaVirtual.Escribir($"[{init}] Detectado: for - inicialización ␦ valor: {valoresOperacion["for_inicializacion"]}");
                    resultado.Add(valoresOperacion["for_inicializacion"]);
                }

                // Condición del for
                if (forStmt.Condition != null)
                {
                    ConsolaVirtual.Escribir($"[{forStmt.Condition}] Detectado: for - comparación ␦ valor: {valoresOperacion["for_comparacion"]}");
                    resultado.Add(valoresOperacion["for_comparacion"]);
                    ProcesarExpresion(forStmt.Condition, resultado, omitirComparaciones: true);

                }

                // Incrementos
                foreach (var inc in forStmt.Incrementors)
                {
                    ConsolaVirtual.Escribir($"[{inc}] Detectado: for - incremento ␦ valor: {valoresOperacion["for_incremento"]}");
                    resultado.Add(valoresOperacion["for_incremento"]);
                }

                // Cuerpo del for
                string cuerpo = ObtenerExpresionManual(forStmt.Statement);
                if (!string.IsNullOrWhiteSpace(cuerpo))
                    resultado.Add($"n[{cuerpo}]");

                break;

            case ForEachStatementSyntax foreachStmt:
                ConsolaVirtual.Escribir($"[{foreachStmt.Identifier.Text} in {foreachStmt.Expression}] Detectado: foreach ␦ valor: n");
                resultado.Add("n");  // Cabecera foreach

                ConsolaVirtual.Escribir("→ Inicia foreach cuerpo");
                string cuerpoForeach = ObtenerExpresionManual(foreachStmt.Statement);

                if (!string.IsNullOrWhiteSpace(cuerpoForeach))
                    resultado.Add($"n[{cuerpoForeach}]");  // El cuerpo del foreach se repite n veces

                ConsolaVirtual.Escribir("→ Finaliza foreach cuerpo");
                break;
            ///Try
            case TryStatementSyntax tryStmt:
                ConsolaVirtual.Escribir("→ Inicia try");
                resultado.Add("1");  // La entrada al try cuenta como una operación constante.

                string cuerpoTry = ObtenerExpresionManual(tryStmt.Block);
                if (!string.IsNullOrWhiteSpace(cuerpoTry))
                    resultado.Add($"({cuerpoTry})");  // Analizar cuerpo del try

                ConsolaVirtual.Escribir("→ Finaliza try");
                ///Catches
                foreach (var catchClause in tryStmt.Catches)
                {
                    ConsolaVirtual.Escribir("→ Inicia catch");
                    resultado.Add("1");  // La entrada al catch también puede considerarse como una operación constante.

                    string cuerpoCatch = ObtenerExpresionManual(catchClause.Block);
                    if (!string.IsNullOrWhiteSpace(cuerpoCatch))
                        resultado.Add($"({cuerpoCatch})");  // Analizar cuerpo del catch

                    ConsolaVirtual.Escribir("→ Finaliza catch");
                }
                ///Finally
                if (tryStmt.Finally != null)
                {
                    ConsolaVirtual.Escribir("→ Inicia finally");
                    resultado.Add("1");  // La entrada al finally también puede considerarse como operación simple.

                    string cuerpoFinally = ObtenerExpresionManual(tryStmt.Finally.Block);
                    if (!string.IsNullOrWhiteSpace(cuerpoFinally))
                        resultado.Add($"({cuerpoFinally})");  // Analizar cuerpo del finally

                    ConsolaVirtual.Escribir("→ Finaliza finally");
                }

                break;

            case WhileStatementSyntax whileStmt:
                ConsolaVirtual.Escribir($"[{whileStmt.Condition}] Detectado: while - comparación ␦ valor: {valoresOperacion["while_comparacion"]}");
                resultado.Add(valoresOperacion["while_comparacion"]);
                ProcesarExpresion(whileStmt.Condition, resultado, omitirComparaciones: true);

                string cuerpoWhile = ObtenerExpresionManual(whileStmt.Statement);
                if (!string.IsNullOrWhiteSpace(cuerpoWhile)) resultado.Add($"n[{cuerpoWhile}]");
                break;

            case DoStatementSyntax doStmt:
                ConsolaVirtual.Escribir($"[{doStmt.Condition}] Detectado: do-while - comparación ␦ valor: {valoresOperacion["dowhile_comparacion"]}");
                resultado.Add(valoresOperacion["dowhile_comparacion"]);
                ProcesarExpresion(doStmt.Condition, resultado, omitirComparaciones: true);


                string cuerpoDoWhile = ObtenerExpresionManual(doStmt.Statement);
                if (!string.IsNullOrWhiteSpace(cuerpoDoWhile)) resultado.Add($"n[{cuerpoDoWhile}]");
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
                else if (exprStmt.Expression is InvocationExpressionSyntax invocacion)
                {
                    var metodoActual = ObtenerMetodoContenedor(hijo)?.Identifier.Text;
                    string nombreLlamado = invocacion.Expression is MemberAccessExpressionSyntax memberAccess
                        ? memberAccess.Name.Identifier.Text
                        : invocacion.Expression is IdentifierNameSyntax identifier
                            ? identifier.Identifier.Text
                            : invocacion.Expression.ToString();

                    // Solo procesamos si es llamada recursiva Y no se ha visitado antes
                    int llamadasRecursivasActuales = resultado.Count(r => r.StartsWith("T("));
                    if (nombreLlamado == metodoActual)
                    {
                        ConsolaVirtual.Escribir($"[INFO] Llamada recursiva detectada a {nombreLlamado}");

                        var recs = ObtenerRecurrenciasDesdeLlamada(invocacion, hijo);
                        foreach (var rec in recs)
                        {
                            if (!resultado.Contains(rec))
                                resultado.Add(rec);
                        }
                    }
                    else if (nombreLlamado != metodoActual)
                    {
                        ConsolaVirtual.Escribir($"[INFO] Llamada a función no recursiva: {nombreLlamado}");
                    }
                }
                else if (exprStmt.Expression is PostfixUnaryExpressionSyntax postUnary &&
                         (postUnary.IsKind(SyntaxKind.PostIncrementExpression) ||
                          postUnary.IsKind(SyntaxKind.PostDecrementExpression)))
                {
                    ConsolaVirtual.Escribir($"[{postUnary}] Detectado: incremento/decremento ␦ valor: 1");
                    resultado.Add("1");
                }
                else if (exprStmt.Expression is PrefixUnaryExpressionSyntax preUnary &&
                         (preUnary.IsKind(SyntaxKind.PreIncrementExpression) ||
                          preUnary.IsKind(SyntaxKind.PreDecrementExpression)))
                {
                    ConsolaVirtual.Escribir($"[{preUnary}] Detectado: incremento/decremento ␦ valor: 1");
                    resultado.Add("1");
                }
                break;

            case IfStatementSyntax ifStmt:
                ConsolaVirtual.Escribir("→ Inicia if");
                ProcesarExpresion(ifStmt.Condition, resultado);  // Procesa condición del if

                string cuerpoIf = ObtenerExpresionManual(ifStmt.Statement);
                if (!string.IsNullOrWhiteSpace(cuerpoIf))
                    resultado.Add($"({cuerpoIf})");
                ConsolaVirtual.Escribir("→ Finaliza if");

                var elseNodo = ifStmt.Else;

                while (elseNodo != null)
                {
                    if (elseNodo.Statement is IfStatementSyntax elseIfStmt)
                    {
                        ConsolaVirtual.Escribir("→ Inicia else if");
                        ProcesarExpresion(elseIfStmt.Condition, resultado);  // Procesa condición del else if

                        string cuerpoElseIf = ObtenerExpresionManual(elseIfStmt.Statement);
                        if (!string.IsNullOrWhiteSpace(cuerpoElseIf))
                            resultado.Add($"({cuerpoElseIf})");
                        ConsolaVirtual.Escribir("→ Finaliza else if");

                        elseNodo = elseIfStmt.Else;
                    }
                    else
                    {
                        ConsolaVirtual.Escribir("→ Inicia else");
                        string cuerpoElse = ObtenerExpresionManual(elseNodo.Statement);
                        if (!string.IsNullOrWhiteSpace(cuerpoElse))
                            resultado.Add($"({cuerpoElse})");
                        ConsolaVirtual.Escribir("→ Finaliza else");
                        break;
                    }
                }
                break;




            case SwitchStatementSyntax switchStmt:
                ConsolaVirtual.Escribir($"[switch] Detectado: switch ␦ valor: {valoresOperacion["switch"]}");
                resultado.Add(valoresOperacion["switch"]);

                foreach (var section in switchStmt.Sections)
                {
                    ConsolaVirtual.Escribir("→ Inicia case");
                    ConsolaVirtual.Escribir($"→ Detectado: case ␦ valor: {valoresOperacion["case"]}");
                    resultado.Add(valoresOperacion["case"]);

                    foreach (var statement in section.Statements)
                    {
                        var expresiones = ObtenerExpresionManualPorTipo(statement);
                        foreach (var ex in expresiones)
                        {
                            resultado.Add($"({ex})");
                        }
                    }


                    ConsolaVirtual.Escribir("→ Finaliza case");
                }


                break;
            case ReturnStatementSyntax returnStmt:
                if (returnStmt.Expression != null)
                {
                    // Procesar cualquier expresión dentro del return
                    ProcesarExpresion(returnStmt.Expression, resultado);
                }
                break;

            default:
                //ConsolaVirtual.Escribir($"[{hijo}] Nodo no clasificado directamente, se analiza internamente.");
                string sub = ObtenerExpresionManual(hijo);
                if (!string.IsNullOrWhiteSpace(sub))
                {
                    //ConsolaVirtual.Escribir($"→ Subexpresión encontrada: {sub}");
                    resultado.Add(sub);
                }
                break;

        }
        return resultado;
    }

    private void ProcesarExpresion(ExpressionSyntax expr, List<string> resultado, bool omitirComparaciones = false)
    {
        //  Caso nuevo: paréntesis
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
        else if (expr is InvocationExpressionSyntax invocacion)
        {
            // Verificar si es llamada recursiva
            ProcesarLlamadaRecursiva(invocacion, resultado, expr);
            // También procesar argumentos de la llamada (SOLO SI NO ES RECURSIVA)
            var metodoActual = ObtenerMetodoContenedor(expr)?.Identifier.Text;
            string nombreLlamado = invocacion.Expression is MemberAccessExpressionSyntax memberAccess
                ? memberAccess.Name.Identifier.Text
                : invocacion.Expression is IdentifierNameSyntax identifier
                    ? identifier.Identifier.Text
                    : invocacion.Expression.ToString();
            // Si NO es llamada recursiva, procesamos los argumentos normalmente
            if (nombreLlamado != metodoActual)
            {
                foreach (var arg in invocacion.ArgumentList.Arguments)
                {
                    ProcesarExpresion(arg.Expression, resultado);
                }
            }
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
                if (!omitirComparaciones)
                {
                    ConsolaVirtual.Escribir($"[{bin}] Detectado: comparación lógica ␦ valor: {valoresOperacion["comparacion"]}");
                    resultado.Add(valoresOperacion["comparacion"]);
                }
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

    public string ResolverFormula(string expresion)
    {
        ConsolaVirtual.Escribir("\n--- Resolución simbólica (con MathNet.Symbolics) ---");

        //Detección para operaciones vacias
        if (string.IsNullOrWhiteSpace(expresion))
        {
            ConsolaVirtual.Escribir("T(n) vacía no hay operaciones detectadas.");
            return "0";
        }

        string expr = expresion.Replace("]", ")")
                               .Replace("[", "(")
                               .Replace("n(", "n*(");
        ConsolaVirtual.Escribir("Expandida: " + expr);
        if (expr.Contains("T("))
        {
            ConsolaVirtual.Escribir("Expresión recursiva detectada. No se resolverá con MathNet.\n");
            return "0";
        }

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

    private List<string> ObtenerRecurrenciasDesdeLlamada(InvocationExpressionSyntax llamada, SyntaxNode contexto)
    {
        var resultado = new List<string>();
        var ignorarArgumentos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "arr", "valor", "left", "right", "target"
        };
        var metodoActual = ObtenerMetodoContenedor(llamada)?.Identifier.Text;
        if (metodoActual == null)
        {
            ConsolaVirtual.Escribir("[WARN] Método contenedor no encontrado durante análisis de recurrencias.");
            return resultado;
        }
        // Extraemos los argumentos textuales
        var args = llamada.ArgumentList.Arguments.Select(a => a.Expression.ToString().Replace(" ", "")).ToList();
        ConsolaVirtual.Escribir($"[DEBUG] Analizando llamada recursiva a {metodoActual} con argumentos: [{string.Join(", ", args)}]");

        // Caso especial para MergeSort(arr, izq, der)
        if (metodoActual == "MergeSort" && args.Count >= 3)
        {
            string izq = args[1], der = args[2];
            // Tamaño total n = der - izq + 1
            string nSize = $"({der}-{izq}+1)";
            ConsolaVirtual.Escribir($"[DEBUG] Detectada recursión MergeSort, tamaño subproblema ~ n/2 donde n={nSize}");
            // Agregamos dos llamadas recursivas T(n/2)
            resultado.Add("T(n / 2)");
            return resultado;
        }

        // Caso especial para búsqueda binaria
        if (metodoActual is "BusquedaBinaria" or "BinarySearch")
        {
            ConsolaVirtual.Escribir($"[DEBUG] Detectada recursión tipo búsqueda binaria");
            resultado.Add("T(n / 2)");
            return resultado;
        }

        // Lógica genérica para otros métodos recursivos
        foreach (var arg in args)
        {
            if (ignorarArgumentos.Contains(arg))
            {
                ConsolaVirtual.Escribir($"[DEBUG] Ignorando argumento no‑recursivo: '{arg}'");
                continue;
            }
            switch (arg)
            {
                case "n":
                    resultado.Add("T(n)");
                    break;
                case "n-1":
                    resultado.Add("T(n - 1)");
                    break;
                case "n-2":
                    resultado.Add("T(n - 2)");
                    break;
                case "n/2":
                    resultado.Add("T(n / 2)");
                    break;
                case "n/2-1":
                    resultado.Add("T(n / 2 - 1)");
                    break;
                case "n/2+1":
                    resultado.Add("T(n / 2 + 1)");
                    break;
                default:
                    ConsolaVirtual.Escribir($"[DEBUG] Argumento no reconocido en llamada recursiva: {arg}");
                    break;
            }
        }
        if (!resultado.Any())
        {
            ConsolaVirtual.Escribir("[WARN] No se detectaron patrones recursivos reconocibles.");
        }
        return resultado;
    }

    private MethodDeclarationSyntax ObtenerMetodoContenedor(SyntaxNode nodo)
    {
        return nodo.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
    }

    // Método auxiliar para detectar llamadas recursivas en cualquier expresión
    private void ProcesarLlamadaRecursiva(InvocationExpressionSyntax invocacion, List<string> resultado, SyntaxNode contexto, int nivel = 0)
    {
        if (nivel > 10) // Protección contra demasiada recursión
        {
            ConsolaVirtual.Escribir("[WARN] Límite de profundidad alcanzado en llamada recursiva. Se detiene.");
            return;
        }
        var metodoActual = ObtenerMetodoContenedor(contexto)?.Identifier.Text;
        if (metodoActual == null)
        {
            ConsolaVirtual.Escribir("[WARN] No se pudo determinar el método contenedor para la llamada.");
            return;
        }
        string nombreLlamado = invocacion.Expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Name.Identifier.Text
            : invocacion.Expression is IdentifierNameSyntax identifier
                ? identifier.Identifier.Text
                : invocacion.Expression.ToString();
        if (nombreLlamado == metodoActual)
        {
            ConsolaVirtual.Escribir($"[INFO] Llamada recursiva detectada en expresión: {nombreLlamado}");
            var recs = ObtenerRecurrenciasDesdeLlamada(invocacion, contexto);
            foreach (var rec in recs)
            {
                if (!resultado.Contains(rec)) // Evitar duplicados
                    resultado.Add(rec);
            }
        }
        else
        {
            ConsolaVirtual.Escribir($"[INFO] Llamada a función no recursiva: {nombreLlamado}");
        }
    }
}

