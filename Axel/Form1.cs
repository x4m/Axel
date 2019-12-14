using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraCharts;
using NCalc;

namespace Axel
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RebuildDS();
            Draw();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            RebuildDS();
        }

        private void RebuildDS()
        {
            var old = dataGridView1.DataSource as AxelData ?? new AxelData();
            var ad = new AxelData();
            label1.Text = "y";
            for (int i = 0; i < numericUpDown1.Value; i++)
            {
                var key = label1.Text;
                HaveParameter(ad, key, old, i == 0 ? 1 : 0);
                label1.Text += "'";
            }

            label1.Text += "=";

            HaveParameter(ad, "Xmin", old, 0);
            HaveParameter(ad, "Xmax", old, 10);
            HaveParameter(ad, "Шагов", old, 1000);
            HaveParameter(ad, "Ymin", old, -10000);
            HaveParameter(ad, "Ymax", old, 10000);

            ad.Order = (int) numericUpDown1.Value;
            ad.Expression = textBox1.Text;
            dataGridView1.DataSource = ad;
        }

        private static void HaveParameter(AxelData ad, string key, AxelData old, double def = 0)
        {
            ad.Add(new AxelRow()
            {
                Key = key,
                Value = old.Where(k => k.Key == key).Select(k => (double?) k.Value).FirstOrDefault() ?? def
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RebuildDS();
            Draw();
        }

        private void Draw()
        {
            chartControl1.BeginInit();
            var series = this.chartControl1.Series.First();
            series.ActualPoints.Clear();
            foreach (var tuple in ((AxelData) dataGridView1.DataSource).Compute())
            {
                series.ActualPoints.Add(new SeriesPoint(tuple.Item1, tuple.Item2));
            }

            chartControl1.EndInit();
        }
    }

    public class AxelRow
    {
        public string Key { get; set; }
        public double Value { get; set; }
    }

    public class AxelData : List<AxelRow>
    {
        public int Order { get; set; }
        public string Expression { get; set; }

        public IEnumerable<Tuple<double, double>> Compute()
        {
            double x = Get("Xmin");
            double xmax = Get("Xmax");
            double ymin = Get("Ymin");
            double ymax = Get("Ymax");
            double steps = Get("Шагов");
            double[] y = new double[Order + 1];
            string literal = "y";
            for (int i = 0; i < Order; i++)
            {
                y[i] = Get(literal);
                literal += "'";
            }

            double step = (xmax - x) / steps;
            if (step <= 0)
                yield break;

            var expr = new Expression(Expression);
            if (expr.HasErrors())
                yield break;
            expr.EvaluateParameter += delegate(string name, ParameterArgs args)
            {
                switch (name)
                {
                    case "x":
                        args.Result = x;
                        break;
                    case "y":
                        args.Result = y[0];
                        break;
                    case "dt":
                        args.Result = step;
                        break;
                }

                if (name.StartsWith("y"))
                {
                    var num = name.Substring(1);
                    int idx;
                    if (int.TryParse(num, out idx) && idx <= Order)
                    {
                        args.Result = y[idx];
                    }
                }
            };

            for (; x <= xmax; x += step)
            {
                y[Order] = Convert.ToDouble(expr.Evaluate());
                for (int i = 0; i < Order; i++)
                {
                    y[i] += y[i + 1] * step;
                }

                if (y[0] >= ymin && y[0] <= ymax)
                    yield return Tuple.Create(x, y[0]);
            }

            yield return Tuple.Create(x, y[0]);

            try
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка рассчёта\n" + ex.ToString(), "Ошибка");
            }
        }

        private double Get(string key)
        {
            return this.Where(k => k.Key == key).Select(k => k.Value).FirstOrDefault();
        }
    }
}