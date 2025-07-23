using Calculadora_de_eficiencia.Services;
using Calculadora_de_eficiencia.Utils;
using MathNet.Symbolics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Expr = MathNet.Symbolics.SymbolicExpression;

// Heredar de CSharpSyntaxWalker para un recorrido eficiente del AST
public class Asignacion : CSharpSyntaxWalker
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
        { "foreach", "n" }, // Representa la N iteraciones
        { "switch", "1" },
        { "case", "1" }
    };

    // Usaremos una pila para manejar la complejidad de bloques anidados
    private Stack<List<string>> currentBlockExpressions = new Stack<List<string>>();
    private List<string> globalSimplifiedExpressions = new List<string>();

    public void Recorrer(SyntaxNode nodo)
    {
        ConsolaVirtual.Escribir("\n--- Análisis separado por clases y métodos públicos ---");

        // --- Validación de errores de sintaxis ---
        var errores = nodo.GetDiagnostics()
            .Where(diag => diag.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (errores.Any())
        {
            ConsolaVirtual.Escribir("\n[ADVERTENCIA] Se encontraron errores de sintaxis en el código fuente.");
            ConsolaVirtual.Escribir("El análisis de complejidad para las secciones afectadas podría ser incompleto o inexacto.");
            ConsolaVirtual.Escribir("Por favor, revise y corrija los siguientes errores:");
            foreach (var error in errores)
            {
                var ubicacion = error.Location.GetLineSpan().StartLinePosition;
                ConsolaVirtual.Escribir($"→ {error.GetMessage()} (línea {ubicacion.Line + 1}, columna {ubicacion.Character + 1})");
            }
        }

        // Inicializar la pila con la lista de expresiones para el nivel global/archivo
        currentBlockExpressions.Push(new List<string>());

        // Iniciar el recorrido del AST
        Visit(nodo);

        // Al finalizar el recorrido global, sumar las expresiones restantes
        var totalGlobalExpression = string.Join(" + ", currentBlockExpressions.Pop());
        if (!string.IsNullOrWhiteSpace(totalGlobalExpression))
        {
            string simplifiedGlobal = ResolverFormula(totalGlobalExpression);
            globalSimplifiedExpressions.Add(simplifiedGlobal);
        }

        ConsolaVirtual.Escribir("\n\n--- Total de T(n) simplificada ---");
        foreach (var expr in globalSimplifiedExpressions)
        {
            ConsolaVirtual.Escribir("T(n) simplificada ~ " + expr);
        }

        // --- Suma total formal ---
        Operaciones.SumarYMostrarTotalFormal(globalSimplifiedExpressions);
    }

    // --- Sobrescribir métodos Visit de CSharpSyntaxWalker ---

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        ConsolaVirtual.Escribir($"\n===== CLASE DETECTADA: {node.Identifier.Text} =====");
        currentBlockExpressions.Push(new List<string>()); // Nuevo contexto para la clase

        // Visitar miembros de la clase, pero no los métodos públicos de inmediato
        // Esto permite que ObtenerExpresionManual_SoloCuerpoClase maneje solo el cuerpo de la clase
        base.VisitClassDeclaration(node);

        var claseExpressions = currentBlockExpressions.Pop();
        string resultadoClase = string.Join(" + ", claseExpressions);

        ConsolaVirtual.Escribir($"\n--- Operaciones internas de la clase {node.Identifier.Text} ---");
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
            globalSimplifiedExpressions.Add(simplificadaClase);
        }
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Solo analizamos métodos públicos o métodos sueltos al inicio del programa
        // Si ya estamos dentro de una clase, solo procesamos los públicos aquí
        if (node.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)) || node.Parent is CompilationUnitSyntax)
        {
            ConsolaVirtual.Escribir($"\n--- MÉTODO DETECTADO: {node.Identifier.Text} ---");
            currentBlockExpressions.Push(new List<string>());

            if (node.Body == null)
            {
                ConsolaVirtual.Escribir("[ADVERTENCIA] El método no tiene cuerpo (posible error de sintaxis). No se analizará su complejidad interna.");
            }
            else
            {
                base.VisitMethodDeclaration(node); // Visitar el cuerpo del método
            }

            var methodExpressions = currentBlockExpressions.Pop();
            string resultadoMetodo = string.Join(" + ", methodExpressions);

            ConsolaVirtual.Escribir($"T(n) = {resultadoMetodo}");
            int totalOperacionesMetodo = resultadoMetodo.Split('+').Select(x => x.Trim()).Count(x => !string.IsNullOrEmpty(x));
            ConsolaVirtual.Escribir($"Total de operaciones detectadas en el método: {totalOperacionesMetodo}");

            string simplificadaMetodo = ResolverFormula(resultadoMetodo);
            globalSimplifiedExpressions.Add(simplificadaMetodo);
        }
        else
        {
            // Para métodos privados o protegidos, o métodos dentro de clases que no queremos analizar a fondo
            base.VisitMethodDeclaration(node); // Seguir visitando sus hijos por si hay declaraciones, etc.
        }
    }

    public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
    {
        foreach (var variable in node.Declaration.Variables)
        {
            if (variable.Initializer != null)
            {
                ConsolaVirtual.Escribir($"[{variable}] Detectado: declaración + asignación (local) ␦ valor: {valoresOperacion["declaracion"]} + {valoresOperacion["asignacion"]}");
                currentBlockExpressions.Peek().Add(valoresOperacion["declaracion"]);
                currentBlockExpressions.Peek().Add(valoresOperacion["asignacion"]);
                ProcesarExpresion(variable.Initializer.Value);
            }
            else
            {
                ConsolaVirtual.Escribir($"[{variable}] Detectado: declaración (local) ␦ valor: {valoresOperacion["declaracion"]}");
                currentBlockExpressions.Peek().Add(valoresOperacion["declaracion"]);
            }
        }
        // No llamar a base.VisitLocalDeclarationStatement(node); ya que hemos procesado sus hijos
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        foreach (var variable in node.Declaration.Variables)
        {
            if (variable.Initializer != null)
            {
                ConsolaVirtual.Escribir($"[{variable}] Detectado: declaración + asignación (campo) ␦ valor: {valoresOperacion["declaracion"]} + {valoresOperacion["asignacion"]}");
                currentBlockExpressions.Peek().Add(valoresOperacion["declaracion"]);
                currentBlockExpressions.Peek().Add(valoresOperacion["asignacion"]);
                ProcesarExpresion(variable.Initializer.Value);
            }
            else
            {
                ConsolaVirtual.Escribir($"[{variable}] Detectado: declaración (campo) ␦ valor: {valoresOperacion["declaracion"]}");
                currentBlockExpressions.Peek().Add(valoresOperacion["declaracion"]);
            }
        }
        // No llamar a base.VisitFieldDeclaration(node);
    }

    public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        ConsolaVirtual.Escribir($"[{node}] Detectado: asignación ␦ valor: {valoresOperacion["asignacion"]}");
        currentBlockExpressions.Peek().Add(valoresOperacion["asignacion"]);
        ProcesarExpresion(node.Right); // Procesar la expresión del lado derecho
        // No llamar a base.VisitAssignmentExpression(node);
    }

    public override void VisitForStatement(ForStatementSyntax node)
    {
        currentBlockExpressions.Push(new List<string>()); // Nuevo contexto para el cuerpo del for

        // Inicialización
        if (node.Declaration != null)
        {
            foreach (var variable in node.Declaration.Variables)
            {
                if (variable.Initializer != null)
                {
                    ConsolaVirtual.Escribir($"[{variable}] Detectado: declaración + asignación (for) ␦ valor: {valoresOperacion["declaracion"]} + {valoresOperacion["asignacion"]}");
                    currentBlockExpressions.Peek().Add(valoresOperacion["declaracion"]);
                    currentBlockExpressions.Peek().Add(valoresOperacion["asignacion"]);
                    ProcesarExpresion(variable.Initializer.Value);
                }
                else
                {
                    ConsolaVirtual.Escribir($"[{variable}] Detectado: declaración (for) ␦ valor: {valoresOperacion["declaracion"]}");
                    currentBlockExpressions.Peek().Add(valoresOperacion["declaracion"]);
                }
            }
        }
        foreach (var init in node.Initializers)
        {
            ConsolaVirtual.Escribir($"[{init}] Detectado: for - inicialización ␦ valor: {valoresOperacion["for_inicializacion"]}");
            currentBlockExpressions.Peek().Add(valoresOperacion["for_inicializacion"]);
            ProcesarExpresion(init); // Procesar expresiones dentro de la inicialización
        }

        // Condición
        if (node.Condition != null)
        {
            ConsolaVirtual.Escribir($"[{node.Condition}] Detectado: for - comparación ␦ valor: {valoresOperacion["for_comparacion"]}");
            currentBlockExpressions.Peek().Add(valoresOperacion["for_comparacion"]);
            ProcesarExpresion(node.Condition, omitirComparaciones: true); // No contar la comparación de nuevo si ya está en for_comparacion
        }

        // Incrementos
        foreach (var inc in node.Incrementors)
        {
            ConsolaVirtual.Escribir($"[{inc}] Detectado: for - incremento ␦ valor: {valoresOperacion["for_incremento"]}");
            currentBlockExpressions.Peek().Add(valoresOperacion["for_incremento"]);
            ProcesarExpresion(inc); // Procesar expresiones dentro del incremento
        }

        // Cuerpo del for (se visita recursivamente)
        // La complejidad del cuerpo se multiplica por N
        ConsolaVirtual.Escribir("→ Inicia for cuerpo");
        currentBlockExpressions.Push(new List<string>()); // Nuevo contexto para el cuerpo del loop
        Visit(node.Statement); // Visitar el cuerpo del for
        string cuerpoFor = string.Join(" + ", currentBlockExpressions.Pop());
        ConsolaVirtual.Escribir("→ Finaliza for cuerpo");

        if (!string.IsNullOrWhiteSpace(cuerpoFor))
        {
            // El costo total del for es la suma de sus partes + N veces el cuerpo
            // Suma todo lo que se acumuló dentro del contexto del for y luego agrega n[]
            string forBlockCost = string.Join(" + ", currentBlockExpressions.Pop());
            currentBlockExpressions.Peek().Add($"({forBlockCost}) + n * ({cuerpoFor})");
        }
        else
        {
            // Si el cuerpo está vacío, solo sumamos lo que se acumuló dentro del contexto del for
            string forBlockCost = string.Join(" + ", currentBlockExpressions.Pop());
            if (!string.IsNullOrWhiteSpace(forBlockCost))
            {
                currentBlockExpressions.Peek().Add($"({forBlockCost})");
            }
        }
    }

    public override void VisitForEachStatement(ForEachStatementSyntax node)
    {
        ConsolaVirtual.Escribir($"[{node.Identifier.Text} in {node.Expression}] Detectado: foreach ␦ valor: n");
        currentBlockExpressions.Peek().Add(valoresOperacion["foreach"]); // Representa el costo base del foreach (N iteraciones)

        ConsolaVirtual.Escribir("→ Inicia foreach cuerpo");
        currentBlockExpressions.Push(new List<string>());
        Visit(node.Statement); // Visitar el cuerpo del foreach
        string cuerpoForeach = string.Join(" + ", currentBlockExpressions.Pop());
        ConsolaVirtual.Escribir("→ Finaliza foreach cuerpo");

        if (!string.IsNullOrWhiteSpace(cuerpoForeach))
        {
            // El costo total del foreach es la suma de sus partes + N veces el cuerpo
            currentBlockExpressions.Peek().Add($"n * ({cuerpoForeach})");
        }
    }

    public override void VisitTryStatement(TryStatementSyntax node)
    {
        ConsolaVirtual.Escribir("→ Inicia try");
        currentBlockExpressions.Peek().Add("1"); // La entrada al try

        currentBlockExpressions.Push(new List<string>());
        Visit(node.Block);
        string cuerpoTry = string.Join(" + ", currentBlockExpressions.Pop());
        if (!string.IsNullOrWhiteSpace(cuerpoTry)) currentBlockExpressions.Peek().Add($"({cuerpoTry})");
        ConsolaVirtual.Escribir("→ Finaliza try");

        foreach (var catchClause in node.Catches)
        {
            ConsolaVirtual.Escribir("→ Inicia catch");
            currentBlockExpressions.Peek().Add("1"); // La entrada al catch

            currentBlockExpressions.Push(new List<string>());
            Visit(catchClause.Block);
            string cuerpoCatch = string.Join(" + ", currentBlockExpressions.Pop());
            if (!string.IsNullOrWhiteSpace(cuerpoCatch)) currentBlockExpressions.Peek().Add($"({cuerpoCatch})");
            ConsolaVirtual.Escribir("→ Finaliza catch");
        }

        if (node.Finally != null)
        {
            ConsolaVirtual.Escribir("→ Inicia finally");
            currentBlockExpressions.Peek().Add("1"); // La entrada al finally

            currentBlockExpressions.Push(new List<string>());
            Visit(node.Finally.Block);
            string cuerpoFinally = string.Join(" + ", currentBlockExpressions.Pop());
            if (!string.IsNullOrWhiteSpace(cuerpoFinally)) currentBlockExpressions.Peek().Add($"({cuerpoFinally})");
            ConsolaVirtual.Escribir("→ Finaliza finally");
        }
    }

    public override void VisitWhileStatement(WhileStatementSyntax node)
    {
        ConsolaVirtual.Escribir($"[{node.Condition}] Detectado: while - comparación ␦ valor: {valoresOperacion["while_comparacion"]}");
        currentBlockExpressions.Peek().Add(valoresOperacion["while_comparacion"]);
        ProcesarExpresion(node.Condition, omitirComparaciones: true);

        ConsolaVirtual.Escribir("→ Inicia while cuerpo");
        currentBlockExpressions.Push(new List<string>());
        Visit(node.Statement); // Visitar el cuerpo del while
        string cuerpoWhile = string.Join(" + ", currentBlockExpressions.Pop());
        ConsolaVirtual.Escribir("→ Finaliza while cuerpo");

        if (!string.IsNullOrWhiteSpace(cuerpoWhile))
        {
            currentBlockExpressions.Peek().Add($"n * ({cuerpoWhile})");
        }
    }

    public override void VisitDoStatement(DoStatementSyntax node)
    {
        ConsolaVirtual.Escribir($"[{node.Condition}] Detectado: do-while - comparación ␦ valor: {valoresOperacion["dowhile_comparacion"]}");
        currentBlockExpressions.Peek().Add(valoresOperacion["dowhile_comparacion"]);
        ProcesarExpresion(node.Condition, omitirComparaciones: true);

        ConsolaVirtual.Escribir("→ Inicia do-while cuerpo");
        currentBlockExpressions.Push(new List<string>());
        Visit(node.Statement); // Visitar el cuerpo del do-while
        string cuerpoDoWhile = string.Join(" + ", currentBlockExpressions.Pop());
        ConsolaVirtual.Escribir("→ Finaliza do-while cuerpo");

        if (!string.IsNullOrWhiteSpace(cuerpoDoWhile))
        {
            currentBlockExpressions.Peek().Add($"n * ({cuerpoDoWhile})");
        }
    }

    public override void VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        if (node.Expression is InvocationExpressionSyntax llamada &&
            llamada.Expression.ToString().Contains("Console.WriteLine"))
        {
            ConsolaVirtual.Escribir($"[{node}] Detectado: Console.WriteLine ␦ valor: {valoresOperacion["console_write"]}");
            currentBlockExpressions.Peek().Add(valoresOperacion["console_write"]);

            foreach (var arg in llamada.ArgumentList.Arguments)
            {
                ProcesarExpresion(arg.Expression);
            }
        }
        else if (node.Expression is AssignmentExpressionSyntax)
        {
            base.VisitExpressionStatement(node); // Deja que el walker maneje la asignación por su cuenta
        }
        else if (node.Expression is PostfixUnaryExpressionSyntax postUnary &&
                 (postUnary.IsKind(SyntaxKind.PostIncrementExpression) ||
                  postUnary.IsKind(SyntaxKind.PostDecrementExpression)))
        {
            ConsolaVirtual.Escribir($"[{postUnary}] Detectado: incremento/decremento ␦ valor: 1");
            currentBlockExpressions.Peek().Add("1");
        }
        else if (node.Expression is PrefixUnaryExpressionSyntax preUnary &&
                 (preUnary.IsKind(SyntaxKind.PreIncrementExpression) ||
                  preUnary.IsKind(SyntaxKind.PreDecrementExpression)))
        {
            ConsolaVirtual.Escribir($"[{preUnary}] Detectado: incremento/decremento ␦ valor: 1");
            currentBlockExpressions.Peek().Add("1");
        }
        else if (node.Expression is InvocationExpressionSyntax invocacion)
        {
            // Llamada general a un método (posiblemente recursiva)
            ProcesarLlamadaRecursiva(invocacion, currentBlockExpressions.Peek(), node);
        }
        else
        {
            // Otras expresiones
            ProcesarExpresion(node.Expression);
        }
    }


    public override void VisitIfStatement(IfStatementSyntax node)
    {
        ConsolaVirtual.Escribir("→ Inicia if");

        // Procesar la condición del IF
        ProcesarExpresion(node.Condition); // Costo de la condición
        List<string> branchExpressions = new(); // Para acumular expresiones de cada rama

        // Recorrer el cuerpo del IF
        currentBlockExpressions.Push(new List<string>());
        Visit(node.Statement);
        string ifBody = string.Join(" + ", currentBlockExpressions.Pop());
        if (!string.IsNullOrWhiteSpace(ifBody)) branchExpressions.Add($"({ifBody})");

        // Recorrer else-if y else
        ElseClauseSyntax elseNode = node.Else;
        while (elseNode != null)
        {
            if (elseNode.Statement is IfStatementSyntax elseIfStmt)
            {
                ConsolaVirtual.Escribir("→ Inicia else if");
                ProcesarExpresion(elseIfStmt.Condition); // Costo de la condición del else-if

                currentBlockExpressions.Push(new List<string>());
                Visit(elseIfStmt.Statement);
                string elseIfBody = string.Join(" + ", currentBlockExpressions.Pop());
                if (!string.IsNullOrWhiteSpace(elseIfBody)) branchExpressions.Add($"({elseIfBody})");
                ConsolaVirtual.Escribir("→ Finaliza else if");

                elseNode = elseIfStmt.Else;
            }
            else
            {
                ConsolaVirtual.Escribir("→ Inicia else");
                currentBlockExpressions.Push(new List<string>());
                Visit(elseNode.Statement);
                string elseBody = string.Join(" + ", currentBlockExpressions.Pop());
                if (!string.IsNullOrWhiteSpace(elseBody)) branchExpressions.Add($"({elseBody})");
                ConsolaVirtual.Escribir("→ Finaliza else");
                break; // Es el último else, salimos del bucle
            }
        }

        // Evaluar y elegir el camino más costoso
        ConsolaVirtual.Escribir("→ Evaluando caminos posibles de if/else:");
        List<(string expr, int peso)> caminosEvaluados = new();
        foreach (var camino in branchExpressions)
        {
            string simplificado = ResolverFormula(camino);
            int peso = EvaluarComplejidad(simplificado);
            caminosEvaluados.Add((simplificado, peso));
            ConsolaVirtual.Escribir($"→ Camino posible: {simplificado}  → peso estimado: {peso}");
        }

        if (caminosEvaluados.Any())
        {
            var max = caminosEvaluados.MaxBy(x => x.peso);
            var min = caminosEvaluados.MinBy(x => x.peso);

            ConsolaVirtual.Escribir($"→ Cota superior por camino más costoso: {max.expr}");
            ConsolaVirtual.Escribir($"→ Cota inferior por camino más barato: {min.expr}");

            currentBlockExpressions.Peek().Add($"({max.expr})"); // Elegimos el camino de mayor peso para la suma total
        }

        ConsolaVirtual.Escribir("→ Finaliza if");
    }

    public override void VisitSwitchStatement(SwitchStatementSyntax node)
    {
        ConsolaVirtual.Escribir($"[switch] Detectado: switch ␦ valor: {valoresOperacion["switch"]}");
        currentBlockExpressions.Peek().Add(valoresOperacion["switch"]);

        List<string> caseExpressions = new(); // Para acumular las expresiones de cada caso

        foreach (var section in node.Sections)
        {
            ConsolaVirtual.Escribir("→ Inicia case");
            currentBlockExpressions.Peek().Add(valoresOperacion["case"]); // Costo de la evaluación del caso

            currentBlockExpressions.Push(new List<string>());
            foreach (var statement in section.Statements)
            {
                Visit(statement); // Visitar las sentencias dentro del caso
            }
            string caseBody = string.Join(" + ", currentBlockExpressions.Pop());
            if (!string.IsNullOrWhiteSpace(caseBody)) caseExpressions.Add($"({caseBody})");

            ConsolaVirtual.Escribir("→ Finaliza case");
        }

        // Para switch, asumimos el peor caso (el caso más costoso)
        if (caseExpressions.Any())
        {
            List<(string expr, int peso)> casesEvaluados = new();
            foreach (var caseExpr in caseExpressions)
            {
                string simplificado = ResolverFormula(caseExpr);
                int peso = EvaluarComplejidad(simplificado);
                casesEvaluados.Add((simplificado, peso));
            }
            var maxCase = casesEvaluados.MaxBy(x => x.peso);
            currentBlockExpressions.Peek().Add($"({maxCase.expr})"); // Sumamos el caso más costoso
        }
    }


    // Método auxiliar para procesar expresiones recursivamente
    private void ProcesarExpresion(ExpressionSyntax expr, bool omitirComparaciones = false)
    {
        if (expr == null) return;

        if (expr is ParenthesizedExpressionSyntax parentesis)
        {
            ProcesarExpresion(parentesis.Expression);
        }
        else if (expr is ElementAccessExpressionSyntax acceso)
        {
            ConsolaVirtual.Escribir($"[{acceso}] Detectado: acceso a arreglo ␦ valor: {valoresOperacion["acceso_arreglo"]}");
            currentBlockExpressions.Peek().Add(valoresOperacion["acceso_arreglo"]);

            foreach (var arg in acceso.ArgumentList.Arguments)
            {
                ProcesarExpresion(arg.Expression);
            }
            ProcesarExpresion(acceso.Expression); // Recurre sobre la base
        }
        else if (expr is BinaryExpressionSyntax bin)
        {
            ProcesarExpresion(bin.Left);
            ProcesarExpresion(bin.Right);

            if (bin.IsKind(SyntaxKind.AddExpression) ||
                bin.IsKind(SyntaxKind.SubtractExpression) ||
                bin.IsKind(SyntaxKind.MultiplyExpression) ||
                bin.IsKind(SyntaxKind.DivideExpression))
            {
                ConsolaVirtual.Escribir($"[{bin}] Detectado: operación aritmética ␦ valor: {valoresOperacion["aritmetica"]}");
                currentBlockExpressions.Peek().Add(valoresOperacion["aritmetica"]);
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
                    currentBlockExpressions.Peek().Add(valoresOperacion["comparacion"]);
                }
            }
            else if (bin.IsKind(SyntaxKind.LogicalAndExpression) ||
                     bin.IsKind(SyntaxKind.LogicalOrExpression))
            {
                ConsolaVirtual.Escribir($"[{bin}] Detectado: operación lógica ␦ valor: {valoresOperacion["logica"]}");
                currentBlockExpressions.Peek().Add(valoresOperacion["logica"]);
            }
        }
        else if (expr is InvocationExpressionSyntax invocacion)
        {
            // Detección de llamada recursiva dentro de expresión
            ConsolaVirtual.Escribir($"[{invocacion}] Detectado: posible llamada a función");

            ProcesarLlamadaRecursiva(invocacion, currentBlockExpressions.Peek(), expr); // ← Usamos el contexto actual como 'expr'
        }
    }


    // --- Métodos de Ayuda

    public string ResolverFormula(string expresion)
    {
        if (string.IsNullOrWhiteSpace(expresion))
        {
            ConsolaVirtual.Escribir("T(n) vacía, no hay operaciones detectadas.");
            return "0";
        }

        string expr = expresion.Replace("]", ")")
                               .Replace("[", "(")
                               .Replace("n(", "n*(");

        ConsolaVirtual.Escribir("\n--- Resolución simbólica y análisis de límite ---");
        ConsolaVirtual.Escribir("Expresión formateada: " + expr);

        try
        {
            var parsed = Infix.ParseOrThrow(expr);
            var expanded = Algebraic.Expand(parsed);
            string result = Infix.Format(expanded);

            Operaciones.AnalizarLimite(result);

            return result;
        }
        catch (Exception ex)
        {
            ConsolaVirtual.Escribir($"[ERROR] No se pudo expandir la fórmula: {ex.Message}");
            return expr; // fallback
        }
    }

    private int EvaluarComplejidad(string formula)
    {
        try
        {
            var parsed = Infix.ParseOrThrow(formula.Replace("n(", "n*(").Replace("[", "(").Replace("]", ")"));
            var simplificada = Algebraic.Expand(parsed);
            string result = Infix.Format(simplificada);

            int score = 0;
            // Modificación para manejar mejor log(n) y n log n en la evaluación de score
            foreach (var term in result.Replace(" ", "").Split('+'))
            {
                if (term.Contains("n!")) return int.MaxValue - 1; // Factorial es el más alto
                if (Regex.IsMatch(term, @"\d*\*\d+\^n|\d+\^n")) return int.MaxValue; // Exponencial

                if (term.Contains("n*Log("))
                {
                    score += 50; // n log n
                }
                else if (term.Contains("n^"))
                {
                    var match = Regex.Match(term, @"n\^(\d+(\.\d+)?)");
                    if (match.Success && double.TryParse(match.Groups[1].Value, out double exponent))
                    {
                        score += (int)(exponent * 100); // n^k
                    }
                }
                else if (term.Contains("n"))
                {
                    score += 10; // n
                }
                else if (term.Contains("Log("))
                {
                    score += 5; // log n
                }
                else
                {
                    if (int.TryParse(term, out int c))
                    {
                        score += c; // constante
                    }
                }
            }
            return score;
        }
        catch
        {
            return int.MaxValue; // Si falla, consideramos que no se puede comparar bien
        }
    }

    private void SumarYMostrarTotalFormal(List<string> expresionesSimplificadas)
    {
        var sumaPorGrado = new Dictionary<string, int>();

        foreach (var expr in expresionesSimplificadas)
        {
            var partes = expr.Replace(" ", "").Split('+');

            foreach (var parte in partes)
            {
                if (string.IsNullOrWhiteSpace(parte)) continue;

                if (parte.Contains("n!")) { sumaPorGrado["n!"] = int.MaxValue; continue; } // Una vez que hay un factorial, domina
                if (Regex.IsMatch(parte, @"\d*\*\d+\^n|\d+\^n")) { sumaPorGrado["b^n"] = int.MaxValue; continue; } // Exponencial

                var nLogNMatch = Regex.Match(parte, @"^(\d+)?\*?n\*Log\(n\)$", RegexOptions.IgnoreCase);
                if (nLogNMatch.Success)
                {
                    int coef = nLogNMatch.Groups[1].Success ? int.Parse(nLogNMatch.Groups[1].Value) : 1;
                    if (!sumaPorGrado.ContainsKey("n*Log(n)")) sumaPorGrado["n*Log(n)"] = 0;
                    sumaPorGrado["n*Log(n)"] += coef;
                    continue;
                }

                var polynomialMatch = Regex.Match(parte, @"^(\d+)?\*?n\^(\d+(\.\d+)?)$", RegexOptions.IgnoreCase);
                if (polynomialMatch.Success)
                {
                    int coef = polynomialMatch.Groups[1].Success ? int.Parse(polynomialMatch.Groups[1].Value) : 1;
                    string grado = $"n^{polynomialMatch.Groups[2].Value}";
                    if (!sumaPorGrado.ContainsKey(grado)) sumaPorGrado[grado] = 0;
                    sumaPorGrado[grado] += coef;
                    continue;
                }

                var linearMatch = Regex.Match(parte, @"^(\d+)?\*?n$", RegexOptions.IgnoreCase);
                if (linearMatch.Success)
                {
                    int coef = linearMatch.Groups[1].Success ? int.Parse(linearMatch.Groups[1].Value) : 1;
                    if (!sumaPorGrado.ContainsKey("n")) sumaPorGrado["n"] = 0;
                    sumaPorGrado["n"] += coef;
                    continue;
                }

                var logMatch = Regex.Match(parte, @"^(\d+)?\*?Log\(n\)$", RegexOptions.IgnoreCase);
                if (logMatch.Success)
                {
                    int coef = logMatch.Groups[1].Success ? int.Parse(logMatch.Groups[1].Value) : 1;
                    if (!sumaPorGrado.ContainsKey("Log(n)")) sumaPorGrado["Log(n)"] = 0;
                    sumaPorGrado["Log(n)"] += coef;
                    continue;
                }

                // Es una constante
                if (int.TryParse(parte, out int constante))
                {
                    if (!sumaPorGrado.ContainsKey("1")) sumaPorGrado["1"] = 0;
                    sumaPorGrado["1"] += constante;
                }
            }
        }

        ConsolaVirtual.Escribir("\nSuma Total Final:");

        if (sumaPorGrado.Count == 0)
        {
            ConsolaVirtual.Escribir("T(n) simplificada total = 0");
        }
        else
        {
            StringBuilder totalFinal = new StringBuilder("T(n) simplificada total = ");
            bool esPrimero = true;

            // Ordenar por complejidad para la presentación
            var ordenado = sumaPorGrado.OrderByDescending(p => GetOrderScore(p.Key));

            foreach (var par in ordenado)
            {
                int coef = par.Value;
                string key = par.Key;

                if (coef == 0 && key != "0") continue; // No mostrar términos con coeficiente cero, a menos que sea el 0 literal

                if (!esPrimero)
                    totalFinal.Append(" + ");
                else
                    esPrimero = false;

                if (key == "1")
                    totalFinal.Append($"{coef}");
                else if (key == "n" || key == "n*Log(n)" || key == "Log(n)" || key == "n!" || key == "b^n")
                {
                    totalFinal.Append(coef == 1 ? key : $"{coef}*{key}");
                }
                else if (key.StartsWith("n^"))
                {
                    totalFinal.Append(coef == 1 ? key : $"{coef}*{key}");
                }
            }

            // Si después de todos los cálculos, el totalFinal es solo "T(n) simplificada total = "
            // y no hay ningún término, significa que es 0
            if (totalFinal.ToString().Equals("T(n) simplificada total = ") || totalFinal.ToString().Equals("T(n) simplificada total = 0"))
            {
                ConsolaVirtual.Escribir("T(n) simplificada total = 0");
            }
            else
            {
                ConsolaVirtual.Escribir(totalFinal.ToString());
            }

            // --- Análisis de Límite y Cota para el total final ---
            // Reconstruir la expresión para el análisis de límite si es necesario
            string finalExprForLimit = string.Join(" + ", ordenado.Select(p => {
                if (p.Key == "1") return p.Value.ToString();
                return p.Value == 1 ? p.Key : $"{p.Value}*{p.Key}";
            }).Where(s => !string.IsNullOrEmpty(s)));

            if (!string.IsNullOrWhiteSpace(finalExprForLimit))
            {
                Operaciones.AnalizarLimite(finalExprForLimit);
            }
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

    private void ProcesarLlamadaRecursiva(InvocationExpressionSyntax invocacion, List<string> resultado, SyntaxNode contexto, int nivel = 0)
    {
        if (nivel > 10)
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
                if (!resultado.Contains(rec))
                    resultado.Add(rec);

                // ✅ Agregar al stack de expresiones para que aparezca en el conteo
                currentBlockExpressions.Peek().Add(rec);
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

        var args = llamada.ArgumentList.Arguments.Select(a => a.Expression.ToString().Replace(" ", "")).ToList();
        ConsolaVirtual.Escribir($"[DEBUG] Analizando llamada recursiva a {metodoActual} con argumentos: [{string.Join(", ", args)}]");

        if (metodoActual == "MergeSort" && args.Count >= 3)
        {
            string izq = args[1], der = args[2];
            string nSize = $"({der}-{izq}+1)";
            ConsolaVirtual.Escribir($"[DEBUG] Detectada recursión MergeSort, tamaño subproblema ~ n/2 donde n={nSize}");
            resultado.Add("T(n / 2)");
            resultado.Add("T(n / 2)");
            return resultado;
        }

        if (metodoActual is "BusquedaBinaria" or "BinarySearch")
        {
            ConsolaVirtual.Escribir($"[DEBUG] Detectada recursión tipo búsqueda binaria");
            resultado.Add("T(n / 2)");
            return resultado;
        }

        foreach (var arg in args)
        {
            if (ignorarArgumentos.Contains(arg))
            {
                ConsolaVirtual.Escribir($"[DEBUG] Ignorando argumento no‑recursivo: '{arg}'");
                continue;
            }

            switch (arg)
            {
                case "n": resultado.Add("T(n)"); break;
                case "n-1": resultado.Add("T(n - 1)"); break;
                case "n-2": resultado.Add("T(n - 2)"); break;
                case "n/2": resultado.Add("T(n / 2)"); break;
                case "n/2-1": resultado.Add("T(n / 2 - 1)"); break;
                case "n/2+1": resultado.Add("T(n / 2 + 1)"); break;
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

}