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
                Filter = "Archivos de extensión .cs (*.cs)|*.cs"
            };

            // Mostrar el dialogo
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string rutaArchivo = openFileDialog.FileName;
                string contenido = File.ReadAllText(rutaArchivo);

                //Asignar ruta al label
                label_descargarArchivo.Text = Path.GetFileName(rutaArchivo);

                // Cambiar al panel de archivo
                panel_archivo.Visible = true;
                panel_archivo.BringToFront();
                panel_inicio.Visible = false;
                panel_inicio.SendToBack();

                // Mensaje de confirmación
                MessageBox.Show("Archivo cargado con exito:\n" + rutaArchivo, "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No se selecciono ningun archivo", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
