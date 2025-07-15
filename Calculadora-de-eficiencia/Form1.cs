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
            panel_archivo.Visible = true;
            panel_archivo.BringToFront();
            panel_inicio.Visible = false;
            panel_inicio.SendToBack();
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
