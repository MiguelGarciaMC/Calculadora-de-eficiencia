using System.Collections.Generic;
using System;

namespace Calculadora_de_eficiencia.Utils
{
    public static class ConsolaVirtual
    {
        private static List<string> mensajes = new();

        public static void Escribir(string texto)
        {
            mensajes.Add(texto);
        }

        public static string ObtenerTodo()
        {
            return string.Join(Environment.NewLine, mensajes);
        }

        public static void Limpiar()
        {
            mensajes.Clear();
        }
    }
}
