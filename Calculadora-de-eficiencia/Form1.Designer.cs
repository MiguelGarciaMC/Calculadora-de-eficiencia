namespace Calculadora_de_eficiencia
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            imagen_titulo = new PictureBox();
            boton_cargar = new Button();
            panel_inicio = new Panel();
            label_titulo = new Label();
            label_descripcion = new Label();
            panel_archivo = new Panel();
            richTextBox1 = new RichTextBox();
            pictureBox1 = new PictureBox();
            boton_evaluar = new Button();
            laber_resultado = new Label();
            boton_archivo = new Button();
            label_descargarArchivo = new Label();
            boton_regresar = new Button();
            ((System.ComponentModel.ISupportInitialize)imagen_titulo).BeginInit();
            panel_inicio.SuspendLayout();
            panel_archivo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // imagen_titulo
            // 
            imagen_titulo.Image = Properties.Resources.Imagen_de_portada_removebg_preview;
            imagen_titulo.Location = new Point(381, 20);
            imagen_titulo.Name = "imagen_titulo";
            imagen_titulo.Size = new Size(406, 361);
            imagen_titulo.SizeMode = PictureBoxSizeMode.StretchImage;
            imagen_titulo.TabIndex = 2;
            imagen_titulo.TabStop = false;
            // 
            // boton_cargar
            // 
            boton_cargar.Location = new Point(100, 358);
            boton_cargar.Name = "boton_cargar";
            boton_cargar.Size = new Size(128, 23);
            boton_cargar.TabIndex = 3;
            boton_cargar.Text = "Cargar archivo .CS";
            boton_cargar.UseVisualStyleBackColor = true;
            boton_cargar.Click += boton_cargar_Click;
            // 
            // panel_inicio
            // 
            panel_inicio.Controls.Add(label_titulo);
            panel_inicio.Controls.Add(label_descripcion);
            panel_inicio.Controls.Add(imagen_titulo);
            panel_inicio.Controls.Add(boton_cargar);
            panel_inicio.Location = new Point(-1, -1);
            panel_inicio.Name = "panel_inicio";
            panel_inicio.Size = new Size(801, 452);
            panel_inicio.TabIndex = 4;
            // 
            // label_titulo
            // 
            label_titulo.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label_titulo.Location = new Point(21, 36);
            label_titulo.Name = "label_titulo";
            label_titulo.Size = new Size(308, 121);
            label_titulo.TabIndex = 4;
            label_titulo.Text = "!Bienvenido a nuesta calculadora de eficiencia!";
            // 
            // label_descripcion
            // 
            label_descripcion.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label_descripcion.Location = new Point(21, 193);
            label_descripcion.Name = "label_descripcion";
            label_descripcion.Size = new Size(308, 122);
            label_descripcion.TabIndex = 5;
            label_descripcion.Text = "Esta herramienta ha sido creada para ayudarte a analizar el rendimiento y la complejidad de tus fragmentos de código para que puedas tener una idea clara de su eficiencia.";
            // 
            // panel_archivo
            // 
            panel_archivo.BackColor = SystemColors.Window;
            panel_archivo.Controls.Add(richTextBox1);
            panel_archivo.Controls.Add(pictureBox1);
            panel_archivo.Controls.Add(boton_evaluar);
            panel_archivo.Controls.Add(laber_resultado);
            panel_archivo.Controls.Add(boton_archivo);
            panel_archivo.Controls.Add(label_descargarArchivo);
            panel_archivo.Controls.Add(boton_regresar);
            panel_archivo.Location = new Point(0, 0);
            panel_archivo.Name = "panel_archivo";
            panel_archivo.Size = new Size(801, 452);
            panel_archivo.TabIndex = 6;
            panel_archivo.Visible = false;
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new Point(32, 147);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(236, 233);
            richTextBox1.TabIndex = 6;
            richTextBox1.Text = "";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.Captura_de_pantalla_2025_07_16_091731;
            pictureBox1.Location = new Point(699, 12);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(87, 65);
            pictureBox1.TabIndex = 5;
            pictureBox1.TabStop = false;
            // 
            // boton_evaluar
            // 
            boton_evaluar.Location = new Point(380, 92);
            boton_evaluar.Name = "boton_evaluar";
            boton_evaluar.Size = new Size(75, 23);
            boton_evaluar.TabIndex = 4;
            boton_evaluar.Text = "Evaluar";
            boton_evaluar.UseVisualStyleBackColor = true;
            boton_evaluar.Click += boton_evaluar_Click;
            // 
            // laber_resultado
            // 
            laber_resultado.AutoSize = true;
            laber_resultado.Location = new Point(86, 112);
            laber_resultado.Name = "laber_resultado";
            laber_resultado.Size = new Size(64, 15);
            laber_resultado.TabIndex = 3;
            laber_resultado.Text = "Resultados";
            // 
            // boton_archivo
            // 
            boton_archivo.Location = new Point(624, 193);
            boton_archivo.Name = "boton_archivo";
            boton_archivo.Size = new Size(133, 23);
            boton_archivo.TabIndex = 2;
            boton_archivo.Text = "Descargar archivo txt";
            boton_archivo.UseVisualStyleBackColor = true;
            boton_archivo.Click += boton_archivo_Click;
            // 
            // label_descargarArchivo
            // 
            label_descargarArchivo.AutoEllipsis = true;
            label_descargarArchivo.AutoSize = true;
            label_descargarArchivo.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            label_descargarArchivo.Location = new Point(334, 45);
            label_descargarArchivo.Name = "label_descargarArchivo";
            label_descargarArchivo.Size = new Size(198, 25);
            label_descargarArchivo.TabIndex = 1;
            label_descargarArchivo.Text = "Nombre del archivo...";
            label_descargarArchivo.TextAlign = ContentAlignment.TopCenter;
            // 
            // boton_regresar
            // 
            boton_regresar.Location = new Point(336, 357);
            boton_regresar.Name = "boton_regresar";
            boton_regresar.Size = new Size(75, 23);
            boton_regresar.TabIndex = 0;
            boton_regresar.Text = "Regresar";
            boton_regresar.UseVisualStyleBackColor = true;
            boton_regresar.Click += boton_regresar_Click_1;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Window;
            ClientSize = new Size(800, 450);
            Controls.Add(panel_archivo);
            Controls.Add(panel_inicio);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "Calculadora de eficiencia";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)imagen_titulo).EndInit();
            panel_inicio.ResumeLayout(false);
            panel_archivo.ResumeLayout(false);
            panel_archivo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TextBox textBox1;
        private TextBox textBox2;
        private PictureBox imagen_titulo;
        private Button boton_cargar;
        private Panel panel_inicio;
        private Label label2;
        private Label label_titulo;
        private Label label_descripcion;
        private Panel panel_archivo;
        private Button boton_regresar;
        private Label label_descargarArchivo;
        private Button boton_archivo;
        private Label laber_resultado;
        private Button boton_evaluar;
        private PictureBox pictureBox1;
        private RichTextBox richTextBox1;
    }
}
