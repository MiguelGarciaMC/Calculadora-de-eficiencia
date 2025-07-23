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

        ConsolaVirtual.Escribir("\n--- Análisis separado por clases y métodos públicos ---");

        var clases = nodo.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

        if (clases.Count == 0)
        {
            ConsolaVirtual.Escribir("\n[INFO] No se encontraron clases.");
            return;
        }

        var expresionesSimplificadas = new List<string>();
        var expresionesInferiores = new List<string>();

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

                string simplificadaClase = ObtenerCotaSuperior(resultadoClase);
                expresionesSimplificadas.Add(simplificadaClase);
                expresionesInferiores.Add(simplificadaClase);
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
                ConsolaVirtual.ListaInferioresGlobales?.Clear();

                string resultadoMetodo = ObtenerExpresionManual(metodo);

                int totalOperacionesMetodo = resultadoMetodo.Split('+').Select(x => x.Trim()).Count(x => !string.IsNullOrEmpty(x));
                ConsolaVirtual.Escribir($"Total de operaciones detectadas en el método: {totalOperacionesMetodo}");

                string simplificadaMetodo = ObtenerCotaSuperior(resultadoMetodo);
                expresionesSimplificadas.Add(simplificadaMetodo);

                // Construir la expresión inferior usando las cotas inferiores recolectadas
                string expresionInferior = ConstruirExpresionInferior(resultadoMetodo);
                string simplificadaInferior = ObtenerCotaSuperior(expresionInferior, false); // Usar la misma función pero sin imprimir
                                                                                             // Imprimir el análisis simbólico de la fórmula de cota inferior
                ConsolaVirtual.Escribir("\n--- Resolución simbólica de cota inferior (con MathNet.Symbolics) ---");
                ConsolaVirtual.Escribir("Expandida: " + expresionInferior);
                ConsolaVirtual.Escribir("T(n) simplificada ~ " + simplificadaInferior);
                expresionesInferiores.Add(simplificadaInferior);
            }
        }

        ConsolaVirtual.Escribir("\n\nTotal de operaciones para la cota superior T(n) simplificadas");
        foreach (var expr in expresionesSimplificadas)
        {
            ConsolaVirtual.Escribir("T(n) simplificada ~ " + expr);
        }

        ConsolaVirtual.Escribir("\n\nTotal de operaciones para la cota inferior T(n) simplificadas");
        foreach (var expr in expresionesInferiores)
        {
            ConsolaVirtual.Escribir("T(n) simplificada ~ " + expr);
        }

        // --- Suma total formal ---
        var sumaPorExponente = new Dictionary<int, int>(); // clave: exponente, valor: coeficiente
        int constantes = 0;


        foreach (var expr in expresionesSimplificadas)
        {
            var partes = expr.Replace(" ", "").Split('+');

            foreach (var parte in partes)
            {
                if (parte.Contains("n"))
                {
                    if (parte.Contains("^"))
                    {
                        var partesExp = parte.Split("*n^");
                        int coef = partesExp.Length == 2 ? int.Parse(partesExp[0]) : 1;
                        int exponente = int.Parse(partesExp[1]);

                        if (!sumaPorExponente.ContainsKey(exponente))
                            sumaPorExponente[exponente] = 0;

                        sumaPorExponente[exponente] += coef;
                    }
                    else if (parte.Contains("*n"))
                    {
                        var coef = int.Parse(parte.Replace("*n", ""));
                        if (!sumaPorExponente.ContainsKey(1))
                            sumaPorExponente[1] = 0;

                        sumaPorExponente[1] += coef;
                    }
                    else if (parte == "n")
                    {
                        if (!sumaPorExponente.ContainsKey(1))
                            sumaPorExponente[1] = 0;

                        sumaPorExponente[1] += 1;
                    }
                }
                else
                {
                    if (int.TryParse(parte, out int constante))
                        constantes += constante;
                }
            }
        }

        ConsolaVirtual.Escribir("\nSuma SUPERIOR");
        string total = constantes.ToString();

        foreach (var kvp in sumaPorExponente.OrderBy(k => k.Key))
        {
            total += $" + {kvp.Value}*n^{kvp.Key}";
        }

        ConsolaVirtual.Escribir($"T(n) Cota superior simplificada = {total}");

        // --- Suma total formal INFERIOR ---
        var sumaPorExponenteInf = new Dictionary<int, int>(); // clave: exponente, valor: coeficiente
        int constantesInf = 0;

        foreach (var expr in expresionesInferiores)
        {
            var partes = expr.Replace(" ", "").Split('+');

            foreach (var parte in partes)
            {
                if (parte.Contains("n"))
                {
                    if (parte.Contains("^"))
                    {
                        var partesExp = parte.Split("*n^");
                        int coef = partesExp.Length == 2 ? int.Parse(partesExp[0]) : 1;
                        int exponente = int.Parse(partesExp[1]);

                        if (!sumaPorExponenteInf.ContainsKey(exponente))
                            sumaPorExponenteInf[exponente] = 0;

                        sumaPorExponenteInf[exponente] += coef;
                    }
                    else if (parte.Contains("*n"))
                    {
                        var coef = int.Parse(parte.Replace("*n", ""));
                        if (!sumaPorExponenteInf.ContainsKey(1))
                            sumaPorExponenteInf[1] = 0;

                        sumaPorExponenteInf[1] += coef;
                    }
                    else if (parte == "n")
                    {
                        if (!sumaPorExponenteInf.ContainsKey(1))
                            sumaPorExponenteInf[1] = 0;

                        sumaPorExponenteInf[1] += 1;
                    }
                }
                else
                {
                    if (int.TryParse(parte, out int constante))
                        constantesInf += constante;
                }
            }
        }

        ConsolaVirtual.Escribir("\nSuma INFERIOR");
        string totalInf = constantesInf.ToString();

        foreach (var kvp in sumaPorExponenteInf.OrderBy(k => k.Key))
        {
            totalInf += $" + {kvp.Value}*n^{kvp.Key}";
        }

        ConsolaVirtual.Escribir($"T(n) Cota inferior simplificada = {totalInf}");


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

                var tnIndividuales = new List<(string tipo, string formula, int valor)>(); // Ej: ("if", 4)
                var resultadoIf = new List<string>();
                var condicionesPrevias = new List<string>(); // Guardará condiciones para else if y else

                // Procesar condición del if
                var condIf = new List<string>();
                ProcesarExpresion(ifStmt.Condition, condIf);
                resultadoIf.AddRange(condIf);         // Se usa en el resultado global
                condicionesPrevias.AddRange(condIf);

                // Procesar cuerpo del if
                string cuerpoIf = ObtenerExpresionManual(ifStmt.Statement);
                if (!string.IsNullOrWhiteSpace(cuerpoIf))
                    resultadoIf.Add($"({cuerpoIf})");

                ConsolaVirtual.Escribir("→ Finaliza if");

                string expresionIf = string.Join(" + ", resultadoIf);
                string tIfSimplificada = ResolverFormulaSilenciosa(expresionIf);
                ConsolaVirtual.Escribir($"**_T(n) del if = {expresionIf}_**");
                ConsolaVirtual.Escribir($"**_T(n) del if simplificada ~ {tIfSimplificada}_**");
                tnIndividuales.Add(("if", tIfSimplificada, EvaluarComplejidad(tIfSimplificada)));



                var elseNodo = ifStmt.Else;

                while (elseNodo != null)
                {
                    if (elseNodo.Statement is IfStatementSyntax elseIfStmt)
                    {
                        ConsolaVirtual.Escribir("→ Inicia else if");

                        var resultadoElseIf = new List<string>();
                        var condicionesSoloParaImpresion = new List<string>(condicionesPrevias); // Clon

                        // Procesar condición actual del else if
                        var condElseIf = new List<string>();
                        ProcesarExpresion(elseIfStmt.Condition, condElseIf);
                        resultadoElseIf.AddRange(condElseIf);
                        condicionesPrevias.AddRange(condElseIf);


                        // Procesar cuerpo del else if
                        string cuerpoElseIf = ObtenerExpresionManual(elseIfStmt.Statement);
                        if (!string.IsNullOrWhiteSpace(cuerpoElseIf))
                            resultadoElseIf.Add($"({cuerpoElseIf})");

                        ConsolaVirtual.Escribir("→ Finaliza else if");

                        // Fórmula individual = condiciones previas + condición actual + cuerpo
                        var resultadoTotalImpresion = new List<string>();
                        resultadoTotalImpresion.AddRange(condicionesSoloParaImpresion);
                        resultadoTotalImpresion.AddRange(resultadoElseIf);

                        string expresionElseIf = string.Join(" + ", resultadoTotalImpresion);
                        string tElseIfSimplificada = ResolverFormulaSilenciosa(expresionElseIf);
                        ConsolaVirtual.Escribir($"**_T(n) del else if = {expresionElseIf}_**");
                        ConsolaVirtual.Escribir($"**_T(n) del else if simplificada ~ {tElseIfSimplificada}_**");
                        tnIndividuales.Add(("else if", tElseIfSimplificada, EvaluarComplejidad(tElseIfSimplificada)));


                        elseNodo = elseIfStmt.Else;
                    }
                    else
                    {
                        ConsolaVirtual.Escribir("→ Inicia else");

                        var resultadoElse = new List<string>();
                        var condicionesSoloParaImpresion = new List<string>(condicionesPrevias); // Clon

                        // Procesar cuerpo del else
                        string cuerpoElse = ObtenerExpresionManual(elseNodo.Statement);
                        if (!string.IsNullOrWhiteSpace(cuerpoElse))
                            resultadoElse.Add($"({cuerpoElse})");

                        ConsolaVirtual.Escribir("→ Finaliza else");

                        // Fórmula individual = condiciones previas + cuerpo
                        var resultadoTotalImpresion = new List<string>();
                        resultadoTotalImpresion.AddRange(condicionesSoloParaImpresion);
                        resultadoTotalImpresion.AddRange(resultadoElse);

                        string expresionElse = string.Join(" + ", resultadoTotalImpresion);
                        string tElseSimplificada = ResolverFormulaSilenciosa(expresionElse);
                        ConsolaVirtual.Escribir($"**_T(n) del else = {expresionElse}_**");
                        ConsolaVirtual.Escribir($"**_T(n) del else simplificada ~ {tElseSimplificada}_**");
                        tnIndividuales.Add(("else", tElseSimplificada, EvaluarComplejidad(tElseSimplificada)));

                        if (tnIndividuales.Any())
                        {
                            var mayor = tnIndividuales.MaxBy(t => t.valor);
                            var menor = tnIndividuales.MinBy(t => t.valor);

                            ConsolaVirtual.Escribir("");
                            ConsolaVirtual.Escribir("--- Comparación de T(n) individuales ---");
                            ConsolaVirtual.Escribir($"🟥 Cota superior → bloque **{mayor.tipo}** con T(n) = {mayor.formula}");
                            ConsolaVirtual.Escribir($"🟩 Cota inferior → bloque **{menor.tipo}** con T(n) = {menor.formula}");

                            resultado.Add(mayor.formula);

                            ConsolaVirtual.ListaSuperioresGlobales?.Clear();
                            ConsolaVirtual.ListaInferioresGlobales?.Clear();
                            ConsolaVirtual.ListaSuperioresGlobales?.Add(mayor.formula);
                            ConsolaVirtual.ListaInferioresGlobales?.Add(menor.formula);

                            // 🟦 Ahora guardamos las ramas individuales para el promedio
                            ConsolaVirtual.ListaPromediosGlobales ??= new List<string>();
                            ConsolaVirtual.ListaPromediosGlobales.AddRange(tnIndividuales.Select(t => t.formula));
                        }
                        break;
                    }

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

    public string ObtenerCotaSuperior(string expresion, bool imprimirDetalles = true)
    {
        //Detección para operaciones vacias
        if (string.IsNullOrWhiteSpace(expresion))
        {
            if (imprimirDetalles)
                ConsolaVirtual.Escribir("T(n) vacía no hay operaciones detectadas.");
            return "0";
        }

        string expr = expresion.Replace("]", ")")
                               .Replace("[", "(")
                               .Replace("n(", "n*(");

        if (imprimirDetalles)
        {
            ConsolaVirtual.Escribir("\n--- Resolución simbólica (con MathNet.Symbolics) ---");
            ConsolaVirtual.Escribir("Expandida: " + expr);
        }

        try
        {
            var parsed = Infix.ParseOrThrow(expr);
            var simplificada = Algebraic.Expand(parsed);
            string resultado = Infix.Format(simplificada);

            if (imprimirDetalles)
            {
                // Ya no imprimimos las cotas aquí, solo mostramos la fórmula simplificada.
                ConsolaVirtual.Escribir("T(n) simplificada ~ " + resultado);
            }


            return resultado;
        }
        catch (Exception ex)
        {
            if (imprimirDetalles)
                ConsolaVirtual.Escribir(" Error al resolver la expresión: " + ex.Message);
            return "Error";
        }
    }

    private string ConstruirExpresionInferior(string expresionOriginal)
    {
        if (ConsolaVirtual.ListaInferioresGlobales == null || ConsolaVirtual.ListaSuperioresGlobales == null)
            return expresionOriginal;

        if (!ConsolaVirtual.ListaInferioresGlobales.Any() || !ConsolaVirtual.ListaSuperioresGlobales.Any())
            return expresionOriginal;

        string cotaSuperior = ConsolaVirtual.ListaSuperioresGlobales.First();
        string cotaInferior = ConsolaVirtual.ListaInferioresGlobales.First();

        // Sustituir la fórmula completa de la cota superior por la cota inferior
        if (expresionOriginal.Contains(cotaSuperior))
        {
            return expresionOriginal.Replace(cotaSuperior, cotaInferior);
        }

        return expresionOriginal;
    }



    private string ResolverFormulaSilenciosa(string expresion)
    {
        if (string.IsNullOrWhiteSpace(expresion))
            return "0";

        string expr = expresion.Replace("]", ")")
                               .Replace("[", "(")
                               .Replace("n(", "n*(");

        try
        {
            var parsed = Infix.ParseOrThrow(expr);
            var simplificada = Algebraic.Expand(parsed);
            return Infix.Format(simplificada);
        }
        catch
        {
            return "Error";
        }
    }

}

