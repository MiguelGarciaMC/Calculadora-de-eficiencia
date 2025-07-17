using Calculadora_de_eficiencia.Utils;
using Calculadora_de_eficiencia.Validation;
using Microsoft.CodeAnalysis.CSharp;

namespace Calculadora_de_eficiencia
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //Variables de clase
        private string rutaArchivoCargado;
        private string contenidoArchivoCargado;

        private void Form1_Load(object sender, EventArgs e)
        {
            panel_archivo.Visible = false;
            panel_archivo.SendToBack();
            panel_inicio.Visible = true;
            panel_inicio.BringToFront();
        }

        private void boton_cargar_Click(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Title = "Seleccionar archivo",
                Filter = "Archivos de extensi�n .cs (*.cs)|*.cs"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                rutaArchivoCargado = openFileDialog.FileName;
                string mensajeError;

                // Validar que el archivo tiene contenido
                if (!Validaciones.TieneContenido(rutaArchivoCargado, out mensajeError))
                {
                    MessageBox.Show(mensajeError, "Archivo inv�lido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validar que el archivo parece c�digo C#
                if (!Validaciones.EsCodigoCSharpBasico(rutaArchivoCargado))
                {
                    MessageBox.Show("El archivo no parece contener c�digo C# v�lido.", "Validaci�n", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //Guardar contenido en memoria
                contenidoArchivoCargado = File.ReadAllText(rutaArchivoCargado);

                // Mostrar nombre del archivo
                label_descargarArchivo.Text = Path.GetFileName(rutaArchivoCargado);

                // Cambiar al panel de archivo
                panel_archivo.Visible = true;
                panel_archivo.BringToFront();
                panel_inicio.Visible = false;
                panel_inicio.SendToBack();

                // Confirmaci�n
                MessageBox.Show("Archivo cargado con �xito:\n" + rutaArchivoCargado, "�xito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No se seleccion� ning�n archivo", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }



        private void boton_regresar_Click_1(object sender, EventArgs e)
        {
            panel_archivo.Visible = false;
            panel_archivo.SendToBack();
            panel_inicio.Visible = true;
            panel_inicio.BringToFront();
            richTextBox1.Clear();
        }

        private void boton_evaluar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(rutaArchivoCargado))
            {
                MessageBox.Show("Primero debes cargar un archivo.", "Atenci�n", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Limpiar consola virtual
                ConsolaVirtual.Limpiar();

                // Obtener el �rbol de sintaxis del archivo cargado
                string codigoFuente = File.ReadAllText(rutaArchivoCargado);
                var tree = CSharpSyntaxTree.ParseText(codigoFuente);
                var root = tree.GetRoot();

                // Ejecutar an�lisis
                var analizador = new Asignacion();
                analizador.Recorrer(root);

                // Mostrar salida
                richTextBox1.Clear();
                richTextBox1.AppendText("Resultados de operaciones encontradas:\n\n");
                richTextBox1.AppendText(ConsolaVirtual.ObtenerTodo());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocurri� un error al evaluar el archivo:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void boton_archivo_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(richTextBox1.Text))
            {
                MessageBox.Show("No hay resultados para guardar. asegurate de haber evaluado un archivo.",
                    "Atenci�n", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Guardar resultados",
                Filter = "Archivo de texto (*.txt)|*.txt",
                FileName = "Resultados_" + Path.GetFileNameWithoutExtension(rutaArchivoCargado) + ".txt",
                DefaultExt = "txt",
                AddExtension = true, //Asegura que la extenci�n sea .txt si el usuario no la incluye
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string rutaFinal = saveFileDialog.FileName;

                //Asegura que el archivo termine en .txt
                if (!rutaFinal.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    rutaFinal += ".txt";
                }

                try
                {
                    File.WriteAllText(rutaFinal, richTextBox1.Text);
                    MessageBox.Show("Resultados guardados correctamente en:\n" + rutaFinal,
                        "�xito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al guardar el archivo:\n" + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
