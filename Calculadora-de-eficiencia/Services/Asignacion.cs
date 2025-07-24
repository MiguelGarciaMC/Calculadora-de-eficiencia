using Calculadora_de_eficiencia.Services;
using Calculadora_de_eficiencia.Utils;
using MathNet.Symbolics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
        var expresionesInferiores = new List<string>();
        var expresionesPromedios = new List<string>();

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
                expresionesPromedios.Add(simplificadaClase);
            }

            var metodosPublicos = clase.Members.OfType<MethodDeclarationSyntax>().ToList();


            if (metodosPublicos.Count == 0)
            {
                ConsolaVirtual.Escribir($"[INFO] La clase {clase.Identifier.Text} no contiene métodos.");
            }

            foreach (var metodo in metodosPublicos)
            {
                ConsolaVirtual.ListaInferioresGlobales?.Clear();
                ConsolaVirtual.Escribir($"\n--- MÉTODO DETECTADO: {metodo.Identifier.Text} en {clase.Identifier.Text} ---");
                string resultadoMetodo = ObtenerExpresionManual(metodo);
                ConsolaVirtual.Escribir($"T(n) = {resultadoMetodo}");
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

                // Construir la expresión promedio usando las cotas promedio recolectadas
                string expresionPromedio = ConstruirExpresionPromedio(resultadoMetodo);
                string simplificadaPromedio = ObtenerCotaSuperior(expresionPromedio, false);
                expresionesPromedios.Add(simplificadaPromedio);
            }
        }

        ConsolaVirtual.Escribir("\n\nTotal de operaciones para la cota superior T(n) simplificadas");
        foreach (var expr in expresionesSimplificadas)
        {
            ConsolaVirtual.Escribir("T(n) simplificada ~ " + expr);
        }

        // Suma y muestra total formal incluyendo recursividad
        Operaciones.SumarYMostrarTotalFormal(expresionesSimplificadas);

        // Combinar expresiones para análisis de recurrencia y cota
        string expresionTotal = string.Join(" + ", expresionesSimplificadas);

        // Mostrar recurrencia y cota
        Operaciones.MostrarRecurrenciaYCota(expresionTotal);

        ConsolaVirtual.Escribir("\n\nTotal de operaciones para la cota inferior T(n) simplificadas");
        foreach (var expr in expresionesInferiores)
        {
            ConsolaVirtual.Escribir("T(n) simplificada ~ " + expr);
        }

        ConsolaVirtual.Escribir("\n\nTotal de operaciones para la cota promedio T(n) simplificadas");

        foreach (var expr in expresionesPromedios)
        {
            ConsolaVirtual.Escribir("T(n) simplificada ~ " + expr);

            // Detectar si es fracción pura tipo "a/b"
            if (expr.Contains("/") && !expr.Contains("n") && !expr.Contains("+") && !expr.Contains("*"))
            {
                var partes = expr.Split('/');
                if (partes.Length == 2 &&
                    double.TryParse(partes[0], out double num) &&
                    double.TryParse(partes[1], out double den) &&
                    Math.Abs(den) > 1e-6)
                {
                    double resultado = num / den;
                    ConsolaVirtual.Escribir($"    Fracción: {expr} = {resultado:0.###}");
                }
            }
        }

        // --- Suma total ---
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

        // --- Suma total formal PROMEDIO ---
        var sumaPorExponenteProm = new Dictionary<int, double>(); // Cambio: usar double en lugar de int
        double constantesProm = 0; // Cambio: usar double

        foreach (var expr in expresionesPromedios)
        {
            //ConsolaVirtual.Escribir($"Procesando expresión promedio: {expr}");

            // Manejar fracciones y expresiones complejas
            var terminos = ParsearExpresionCompleta(expr);

            foreach (var termino in terminos)
            {
                if (termino.potencia == 0) // término constante
                {
                    constantesProm += termino.coeficiente;
                }
                else // término con potencia de n
                {
                    if (!sumaPorExponenteProm.ContainsKey(termino.potencia))
                        sumaPorExponenteProm[termino.potencia] = 0;

                    sumaPorExponenteProm[termino.potencia] += termino.coeficiente;
                }
            }
        }

        ConsolaVirtual.Escribir("\nSuma PROMEDIO");

        // Construir resultado
        var partesPromedio = new List<string>();

        // Agregar constante si existe
        if (Math.Abs(constantesProm) > 0.001)
        {
            if (Math.Abs(constantesProm - Math.Round(constantesProm)) < 0.001)
                partesPromedio.Add(Math.Round(constantesProm).ToString());
            else
                partesPromedio.Add(constantesProm.ToString("F2"));
        }

        // Agregar términos con potencias de n
        foreach (var kvp in sumaPorExponenteProm.OrderBy(k => k.Key))
        {
            string coefStr;
            if (Math.Abs(kvp.Value - Math.Round(kvp.Value)) < 0.001)
                coefStr = Math.Round(kvp.Value).ToString();
            else
                coefStr = kvp.Value.ToString("F2");

            if (kvp.Key == 1)
                partesPromedio.Add($"{coefStr}*n^{kvp.Key}");
            else
                partesPromedio.Add($"{coefStr}*n^{kvp.Key}");
        }

        string totalProm = partesPromedio.Any() ? string.Join(" + ", partesPromedio) : "0";
        ConsolaVirtual.Escribir($"T(n) Cota promedio simplificada = {totalProm}");
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
            if (hijo is MethodDeclarationSyntax) continue;
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

                // Detectar si incremento es multiplicativo (logaritmico)
                bool esLogaritmico = false;
                foreach (var inc in forStmt.Incrementors)
                {
                    var incStr = inc.ToString();
                    if (incStr.Contains("*= 2") || incStr.Contains("*=2") || incStr.Contains("*= n") || incStr.Contains("= j * 2") || incStr.Contains("= j*2"))
                    {
                        esLogaritmico = true;
                        break;
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
                {
                    if (esLogaritmico)
                    {
                        // El ciclo se ejecuta log(n) veces
                        resultado.Add($"Log(n)[{cuerpo}]");
                    }
                    else
                    {
                        // El ciclo se ejecuta n veces
                        resultado.Add($"n[{cuerpo}]");
                    }
                }
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
            case TryStatementSyntax tryStmt:
                ConsolaVirtual.Escribir("→ Inicia try");
                resultado.Add("1");  // La entrada al try cuenta como una operación constante.
                string cuerpoTry = ObtenerExpresionManual(tryStmt.Block);
                if (!string.IsNullOrWhiteSpace(cuerpoTry))
                    resultado.Add($"({cuerpoTry})");  // Analizar cuerpo del try
                ConsolaVirtual.Escribir("→ Finaliza try");
                // Catches
                foreach (var catchClause in tryStmt.Catches)
                {
                    ConsolaVirtual.Escribir("→ Inicia catch");
                    resultado.Add("1");  // La entrada al catch también puede considerarse como una operación constante.
                    string cuerpoCatch = ObtenerExpresionManual(catchClause.Block);
                    if (!string.IsNullOrWhiteSpace(cuerpoCatch))
                        resultado.Add($"({cuerpoCatch})");  // Analizar cuerpo del catch
                    ConsolaVirtual.Escribir("→ Finaliza catch");
                }
                // Finally
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

                var tnIndividuales = new List<(string tipo, string formula, int valor)>();
                var resultadoIf = new List<string>();

                // Procesar condición del if
                var condIf = new List<string>();
                ProcesarExpresion(ifStmt.Condition, condIf);
                resultadoIf.AddRange(condIf);

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

                // Variables para manejar condiciones acumulativas
                var condicionesAcumuladas = new List<string>(condIf);
                var elseNodo = ifStmt.Else;
                int contadorElseIf = 1;

                while (elseNodo != null)
                {
                    if (elseNodo.Statement is IfStatementSyntax elseIfStmt)
                    {
                        ConsolaVirtual.Escribir($"→ Inicia else if {contadorElseIf}");

                        var resultadoElseIf = new List<string>();

                        // Agregar condiciones acumuladas (las condiciones previas que deben evaluarse)
                        resultadoElseIf.AddRange(condicionesAcumuladas);

                        // Procesar condición actual del else if
                        var condElseIf = new List<string>();
                        ProcesarExpresion(elseIfStmt.Condition, condElseIf);
                        resultadoElseIf.AddRange(condElseIf);

                        // Agregar la condición actual a las acumuladas para el siguiente else if
                        condicionesAcumuladas.AddRange(condElseIf);

                        // Procesar cuerpo del else if
                        string cuerpoElseIf = ObtenerExpresionManual(elseIfStmt.Statement);
                        if (!string.IsNullOrWhiteSpace(cuerpoElseIf))
                            resultadoElseIf.Add($"({cuerpoElseIf})");

                        ConsolaVirtual.Escribir($"→ Finaliza else if {contadorElseIf}");

                        string expresionElseIf = string.Join(" + ", resultadoElseIf);
                        string tElseIfSimplificada = ResolverFormulaSilenciosa(expresionElseIf);
                        ConsolaVirtual.Escribir($"**_T(n) del else if {contadorElseIf} = {expresionElseIf}_**");
                        ConsolaVirtual.Escribir($"**_T(n) del else if {contadorElseIf} simplificada ~ {tElseIfSimplificada}_**");
                        tnIndividuales.Add(($"else if {contadorElseIf}", tElseIfSimplificada, EvaluarComplejidad(tElseIfSimplificada)));

                        elseNodo = elseIfStmt.Else;
                        contadorElseIf++;
                    }
                    else
                    {
                        ConsolaVirtual.Escribir("→ Inicia else");

                        var resultadoElse = new List<string>();

                        // Agregar todas las condiciones acumuladas
                        resultadoElse.AddRange(condicionesAcumuladas);

                        // Procesar cuerpo del else
                        string cuerpoElse = ObtenerExpresionManual(elseNodo.Statement);
                        if (!string.IsNullOrWhiteSpace(cuerpoElse))
                            resultadoElse.Add($"({cuerpoElse})");

                        ConsolaVirtual.Escribir("→ Finaliza else");

                        string expresionElse = string.Join(" + ", resultadoElse);
                        string tElseSimplificada = ResolverFormulaSilenciosa(expresionElse);
                        ConsolaVirtual.Escribir($"**_T(n) del else = {expresionElse}_**");
                        ConsolaVirtual.Escribir($"**_T(n) del else simplificada ~ {tElseSimplificada}_**");
                        tnIndividuales.Add(("else", tElseSimplificada, EvaluarComplejidad(tElseSimplificada)));

                        break;
                    }
                }

                // Análisis final - SOLO al final del procesamiento completo del if-else
                if (tnIndividuales.Any())
                {
                    var mayor = tnIndividuales.MaxBy(t => t.valor);
                    var menor = tnIndividuales.MinBy(t => t.valor);

                    ConsolaVirtual.Escribir("");
                    ConsolaVirtual.Escribir("--- Comparación de T(n) individuales ---");
                    ConsolaVirtual.Escribir($"🟥 Cota superior → bloque **{mayor.tipo}** con T(n) = {mayor.formula}");
                    ConsolaVirtual.Escribir($"🟩 Cota inferior → bloque **{menor.tipo}** con T(n) = {menor.formula}");

                    // Para el resultado principal, usar la cota superior
                    resultado.Add(mayor.formula);

                    // Limpiar y establecer las listas globales SOLO para este if-else
                    ConsolaVirtual.ListaSuperioresGlobales?.Clear();
                    ConsolaVirtual.ListaInferioresGlobales?.Clear();
                    ConsolaVirtual.ListaPromediosGlobales?.Clear();

                    ConsolaVirtual.ListaSuperioresGlobales?.Add(mayor.formula);
                    ConsolaVirtual.ListaInferioresGlobales?.Add(menor.formula);

                    // Para el promedio, usar solo las ramas de ESTE nivel
                    ConsolaVirtual.ListaPromediosGlobales ??= new List<string>();
                    ConsolaVirtual.ListaPromediosGlobales.AddRange(tnIndividuales.Select(t => t.formula));
                }
                break;

            case SwitchStatementSyntax switchStmt:
                ConsolaVirtual.Escribir($"[switch] Detectado: switch ␦ valor: {valoresOperacion["switch"]}");
                resultado.Add(valoresOperacion["switch"]);
                int valorSwitch = int.Parse(valoresOperacion["switch"]);
                int acumuladoEncabezados = 0;
                int indiceCase = 1;
                foreach (var section in switchStmt.Sections)
                {
                    ConsolaVirtual.Escribir("→ Inicia case");
                    ConsolaVirtual.Escribir($"→ Detectado: case ␦ valor: {valoresOperacion["case"]}");
                    resultado.Add(valoresOperacion["case"]);
                    acumuladoEncabezados += int.Parse(valoresOperacion["case"]);
                    int operacionesInternas = 0;
                    foreach (var statement in section.Statements)
                    {
                        var expresiones = ObtenerExpresionManualPorTipo(statement);
                        foreach (var ex in expresiones)
                        {
                            resultado.Add($"({ex})");
                            if (int.TryParse(ex, out int valorOp))
                            {
                                operacionesInternas += valorOp;
                            }
                        }
                    }
                    ConsolaVirtual.Escribir($"→ Operaciones internas del case: {operacionesInternas}");
                    int costoCase = valorSwitch + acumuladoEncabezados + operacionesInternas;
                    ConsolaVirtual.Escribir($"→ Costo acumulado tras este case: {costoCase}");
                    resultado.Add(costoCase.ToString());
                    ConsolaVirtual.Escribir("→ Finaliza case");
                    indiceCase++;
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
        ConsolaVirtual.Escribir("Expandida: " + expr);
        if (expr.Contains("T("))
        {
            ConsolaVirtual.Escribir("Expresión recursiva detectada. No se resolverá con MathNet.\n");
            return expresion;
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



    private string ConstruirExpresionPromedio(string expresionOriginal)
    {
        if (ConsolaVirtual.ListaPromediosGlobales == null || !ConsolaVirtual.ListaPromediosGlobales.Any())
        {
            return expresionOriginal; // Si no hay múltiples ramas, el promedio es igual a la expresión original
        }

        if (ConsolaVirtual.ListaSuperioresGlobales == null || !ConsolaVirtual.ListaSuperioresGlobales.Any())
        {
            return expresionOriginal;
        }

        // Calcular el promedio de todas las ramas del if-else
        string cotaSuperior = ConsolaVirtual.ListaSuperioresGlobales.First();
        string promedioCalculado = CalcularPromedioRamas(ConsolaVirtual.ListaPromediosGlobales);

        // Sustituir la cota superior por el promedio calculado
        if (expresionOriginal.Contains(cotaSuperior))
        {
            return expresionOriginal.Replace(cotaSuperior, promedioCalculado);
        }

        return expresionOriginal;
    }

    // Método corregido para calcular el promedio
    private string CalcularPromedioRamas(List<string> ramas)
    {
        if (ramas == null || !ramas.Any())
            return "0";

        if (ramas.Count == 1)
            return ramas.First();

        try
        {
            // En lugar de evaluar con n=1, trabajamos directamente con las fórmulas simbólicas
            ConsolaVirtual.Escribir($"\n--- Calculando promedio de {ramas.Count} ramas ---");

            // Mostrar las ramas individuales
            for (int i = 0; i < ramas.Count; i++)
            {
                ConsolaVirtual.Escribir($"Rama {i + 1}: {ramas[i]}");
            }

            // Crear la expresión de suma de todas las ramas
            string sumaTotal = string.Join(" + ", ramas.Select(r => $"({r})"));
            ConsolaVirtual.Escribir($"Suma total: {sumaTotal}");

            // Dividir entre el número de ramas para obtener el promedio
            string expresionPromedio = $"({sumaTotal})/{ramas.Count}";
            ConsolaVirtual.Escribir($"Expresión promedio: {expresionPromedio}");

            // Simplificar usando MathNet.Symbolics
            try
            {
                // Preparar la expresión para MathNet
                string exprParaMathNet = expresionPromedio
                    .Replace("n(", "n*(")
                    .Replace("[", "(")
                    .Replace("]", ")");

                var parsed = Infix.ParseOrThrow(exprParaMathNet);
                var simplificada = Algebraic.Expand(parsed);
                string resultado = Infix.Format(simplificada);

                ConsolaVirtual.Escribir($"Promedio simplificado: {resultado}");
                return resultado;
            }
            catch (Exception ex)
            {
                ConsolaVirtual.Escribir($"Error en simplificación simbólica: {ex.Message}");

                // Fallback: crear el promedio manualmente
                return CrearPromedioManual(ramas);
            }
        }
        catch (Exception ex)
        {
            ConsolaVirtual.Escribir($"Error general calculando promedio: {ex.Message}");
            // En caso de error total, usar la primera rama como fallback
            return ramas.First();
        }
    }

    private string CrearPromedioManual(List<string> ramas)
    {
        try
        {
            // Crear un diccionario para agrupar términos por potencia de n
            var terminosPorPotencia = new Dictionary<int, List<double>>(); // potencia -> lista de coeficientes
            var constantesTotales = new List<double>();

            foreach (var rama in ramas)
            {
                // Parsear cada rama para extraer términos
                var terminos = ParsearExpresionCompleta(rama);

                foreach (var termino in terminos)
                {
                    if (termino.potencia == 0) // término constante
                    {
                        constantesTotales.Add(termino.coeficiente);
                    }
                    else
                    {
                        if (!terminosPorPotencia.ContainsKey(termino.potencia))
                            terminosPorPotencia[termino.potencia] = new List<double>();

                        terminosPorPotencia[termino.potencia].Add(termino.coeficiente);
                    }
                }
            }

            // Calcular promedios
            var resultadoTerminos = new List<string>();

            // Agregar constante promedio
            if (constantesTotales.Any())
            {
                double promedioConstante = constantesTotales.Average();
                if (Math.Abs(promedioConstante - Math.Round(promedioConstante)) < 0.001)
                    resultadoTerminos.Add(Math.Round(promedioConstante).ToString());
                else
                    resultadoTerminos.Add(promedioConstante.ToString("F2"));
            }

            // Agregar términos con n
            foreach (var kvp in terminosPorPotencia.OrderBy(x => x.Key))
            {
                double promedioCoef = kvp.Value.Average();

                string coefStr;
                if (Math.Abs(promedioCoef - Math.Round(promedioCoef)) < 0.001)
                    coefStr = Math.Round(promedioCoef).ToString();
                else
                    coefStr = promedioCoef.ToString("F2");

                if (kvp.Key == 1)
                    resultadoTerminos.Add($"{coefStr}*n");
                else
                    resultadoTerminos.Add($"{coefStr}*n^{kvp.Key}");
            }

            return string.Join(" + ", resultadoTerminos);
        }
        catch
        {
            // Si falla todo, devolver la expresión como fracción
            return $"({string.Join(" + ", ramas)})/{ramas.Count}";
        }
    }

    private List<(double coeficiente, int potencia)> ParsearExpresionCompleta(string expresion)
    {
        var terminos = new List<(double coeficiente, int potencia)>();

        try
        {
            //ConsolaVirtual.Escribir($"  Parseando: {expresion}");

            // Manejar fracciones simples (ej: 48/5)
            if (expresion.Contains("/") && !expresion.Contains("+") && !expresion.Contains("n"))
            {
                var segmentos = expresion.Split('/');
                if (segmentos.Length == 2 && double.TryParse(segmentos[0], out double numerador) &&
                    double.TryParse(segmentos[1], out double denominador))
                {
                    double valor = numerador / denominador;
                    terminos.Add((valor, 0));
                    //ConsolaVirtual.Escribir($"    Fracción: {numerador}/{denominador} = {valor}");
                    return terminos;
                }
            }

            // Intentar evaluar con MathNet.Symbolics para casos complejos
            try
            {
                string exprParaEvaluar = expresion
                    .Replace("n(", "n*(")
                    .Replace("[", "(")
                    .Replace("]", ")");

                // Para fracciones complejas, intentar simplificar primero
                var parsed = Infix.ParseOrThrow(exprParaEvaluar);
                var simplificada = Algebraic.Expand(parsed);
                string resultado = Infix.Format(simplificada);

                //ConsolaVirtual.Escribir($"    Simplificada: {resultado}");

                // Ahora parsear el resultado simplificado
                return ParsearTerminosBasicos(resultado);
            }
            catch
            {
                // Si MathNet falla, usar parsing manual
                return ParsearTerminosBasicos(expresion);
            }
        }
        catch (Exception ex)
        {
            //ConsolaVirtual.Escribir($"    Error parseando '{expresion}': {ex.Message}");
            // En caso de error, asumir que es una constante con valor 1
            terminos.Add((1, 0));
        }

        return terminos;
    }

    private List<(double coeficiente, int potencia)> ParsearTerminosBasicos(string formula)
    {
        var terminos = new List<(double coeficiente, int potencia)>();

        try
        {
            // Limpiar y dividir por +, pero preservar signos negativos
            formula = formula.Replace(" ", "");
            var elementosFormula = new List<string>();

            // Dividir manteniendo signos
            string[] segmentosFormula = formula.Split('+');
            foreach (string frag in segmentosFormula)
            {
                if (!string.IsNullOrEmpty(frag.Trim()))
                {
                    elementosFormula.Add(frag.Trim());
                }
            }

            foreach (var elemento in elementosFormula)
            {
                if (string.IsNullOrEmpty(elemento)) continue;

                //ConsolaVirtual.Escribir($"    Analizando término: '{elemento}'");

                if (elemento.Contains("n^"))
                {
                    // Término con potencia (ej: 5*n^2 o n^3)
                    var partesN = elemento.Split(new[] { "*n^" }, StringSplitOptions.None);
                    if (partesN.Length == 2)
                    {
                        double coef = string.IsNullOrEmpty(partesN[0]) ? 1 : double.Parse(partesN[0]);
                        int pot = int.Parse(partesN[1]);
                        terminos.Add((coef, pot));
                        //ConsolaVirtual.Escribir($"      → Coef: {coef}, Pot: {pot}");
                    }
                    else if (elemento.StartsWith("n^"))
                    {
                        int pot = int.Parse(elemento.Substring(2));
                        terminos.Add((1, pot));
                        //ConsolaVirtual.Escribir($"      → Coef: 1, Pot: {pot}");
                    }
                }
                else if (elemento.Contains("*n") && !elemento.Contains("^"))
                {
                    // Término lineal (ej: 5*n)
                    var coefStr = elemento.Replace("*n", "");
                    double coef = string.IsNullOrEmpty(coefStr) ? 1 : double.Parse(coefStr);
                    terminos.Add((coef, 1));
                    //ConsolaVirtual.Escribir($"      → Coef: {coef}, Pot: 1");
                }
                else if (elemento == "n")
                {
                    terminos.Add((1, 1));
                    //ConsolaVirtual.Escribir($"      → Coef: 1, Pot: 1");
                }
                else
                {
                    // Término constante (incluyendo fracciones decimales)
                    if (double.TryParse(elemento, out double constante))
                    {
                        terminos.Add((constante, 0));
                        //ConsolaVirtual.Escribir($"      → Constante: {constante}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ConsolaVirtual.Escribir($"    Error en parsing básico: {ex.Message}");
            terminos.Add((1, 0)); // fallback
        }

        return terminos;
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
}
