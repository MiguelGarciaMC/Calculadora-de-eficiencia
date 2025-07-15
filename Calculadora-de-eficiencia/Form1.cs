using Calculadora_de_eficiencia.Validation;

namespace Calculadora_de_eficiencia
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

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
                string rutaArchivo = openFileDialog.FileName;
                string mensajeError;

                // Validar que el archivo tiene contenido
                if (!Validaciones.TieneContenido(rutaArchivo, out mensajeError))
                {
                    MessageBox.Show(mensajeError, "Archivo inv�lido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validar que el archivo parece c�digo C#
                if (!Validaciones.EsCodigoCSharpBasico(rutaArchivo))
                {
                    MessageBox.Show("El archivo no parece contener c�digo C# v�lido.", "Validaci�n", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Mostrar nombre del archivo
                label_descargarArchivo.Text = Path.GetFileName(rutaArchivo);

                // Cambiar al panel de archivo
                panel_archivo.Visible = true;
                panel_archivo.BringToFront();
                panel_inicio.Visible = false;
                panel_inicio.SendToBack();

                // Confirmaci�n
                MessageBox.Show("Archivo cargado con �xito:\n" + rutaArchivo, "�xito", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        }
    }
}
