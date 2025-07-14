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
            imagen_titulo = new PictureBox();
            boton_cargar = new Button();
            panel_inicio = new Panel();
            panel_archivo = new Panel();
            label_nombre_archivo = new Label();
            label_descripcion = new Label();
            label_titulo = new Label();
            boton_evaluar = new Button();
            boton_descargar = new Button();
            label_resultado = new Label();
            boton_regresar = new Button();
            ((System.ComponentModel.ISupportInitialize)imagen_titulo).BeginInit();
            panel_inicio.SuspendLayout();
            panel_archivo.SuspendLayout();
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
            // 
            // panel_inicio
            // 
            panel_inicio.Controls.Add(panel_archivo);
            panel_inicio.Controls.Add(label_descripcion);
            panel_inicio.Controls.Add(label_titulo);
            panel_inicio.Controls.Add(imagen_titulo);
            panel_inicio.Controls.Add(boton_cargar);
            panel_inicio.Location = new Point(-1, -1);
            panel_inicio.Name = "panel_inicio";
            panel_inicio.Size = new Size(801, 452);
            panel_inicio.TabIndex = 4;
            // 
            // panel_archivo
            // 
            panel_archivo.BackColor = SystemColors.Window;
            panel_archivo.Controls.Add(boton_regresar);
            panel_archivo.Controls.Add(label_resultado);
            panel_archivo.Controls.Add(boton_descargar);
            panel_archivo.Controls.Add(boton_evaluar);
            panel_archivo.Controls.Add(label_nombre_archivo);
            panel_archivo.Location = new Point(3, 0);
            panel_archivo.Name = "panel_archivo";
            panel_archivo.Size = new Size(801, 452);
            panel_archivo.TabIndex = 7;
            // 
            // label_nombre_archivo
            // 
            label_nombre_archivo.AutoSize = true;
            label_nombre_archivo.Location = new Point(319, 54);
            label_nombre_archivo.Name = "label_nombre_archivo";
            label_nombre_archivo.Size = new Size(121, 15);
            label_nombre_archivo.TabIndex = 0;
            label_nombre_archivo.Text = "Nombre del archivo...";
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
            // label_titulo
            // 
            label_titulo.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label_titulo.Location = new Point(21, 36);
            label_titulo.Name = "label_titulo";
            label_titulo.Size = new Size(308, 121);
            label_titulo.TabIndex = 4;
            label_titulo.Text = "!Bienvenido a nuesta calculadora de eficiencia!";
            // 
            // boton_evaluar
            // 
            boton_evaluar.Location = new Point(335, 100);
            boton_evaluar.Name = "boton_evaluar";
            boton_evaluar.Size = new Size(75, 23);
            boton_evaluar.TabIndex = 1;
            boton_evaluar.Text = "Evaluar";
            boton_evaluar.UseVisualStyleBackColor = true;
            // 
            // boton_descargar
            // 
            boton_descargar.Location = new Point(505, 230);
            boton_descargar.Name = "boton_descargar";
            boton_descargar.Size = new Size(151, 23);
            boton_descargar.TabIndex = 2;
            boton_descargar.Text = "Descargar txt con resultado";
            boton_descargar.UseVisualStyleBackColor = true;
            // 
            // label_resultado
            // 
            label_resultado.AutoSize = true;
            label_resultado.Location = new Point(91, 230);
            label_resultado.Name = "label_resultado";
            label_resultado.Size = new Size(59, 15);
            label_resultado.TabIndex = 3;
            label_resultado.Text = "Resultado";
            // 
            // boton_regresar
            // 
            boton_regresar.Location = new Point(335, 377);
            boton_regresar.Name = "boton_regresar";
            boton_regresar.Size = new Size(75, 23);
            boton_regresar.TabIndex = 4;
            boton_regresar.Text = "Regresar";
            boton_regresar.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Window;
            ClientSize = new Size(800, 450);
            Controls.Add(panel_inicio);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)imagen_titulo).EndInit();
            panel_inicio.ResumeLayout(false);
            panel_archivo.ResumeLayout(false);
            panel_archivo.PerformLayout();
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
        private Label label_nombre_archivo;
        private Button boton_evaluar;
        private Button boton_regresar;
        private Label label_resultado;
        private Button boton_descargar;
    }
}
