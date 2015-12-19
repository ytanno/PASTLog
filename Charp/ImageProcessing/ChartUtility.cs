using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShoNS.Array;
using System.Threading.Tasks;
using System.Threading;
using OpenCvSharp;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;


namespace LabImg
{
	/// <summary>
	/// チャート表示の関数クラス
	/// </summary>
	public static class ChartUtility
	{
		/// <summary>
		/// XY-plotの散布図で点集合を表示
		/// </summary>
		/// <param name="chart"></param>
		/// <param name="plot1"></param>
		/// <param name="plot2"></param>
		/// <param name="minX"></param>
		/// <param name="minY"></param>
		/// <param name="maxX"></param>
		/// <param name="maxY"></param>
		public static void PlotChartXY(Chart chart, int[] plot1, int[] plot2, int minX, int maxX, int minY, int maxY)
		{
			//init series
			Series series1 = new Series("plot1");

			//setType
			series1.ChartType = SeriesChartType.Point;

			for (int i = 0; i < plot2.Length; i++)
				series1.Points.AddXY(plot1[i], plot2[i]);

			//setValueLabel
			series1.IsValueShownAsLabel = false;

			//setPointsize
			series1.MarkerSize = 5;

			//setMarkerType
			series1.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;

			//setMaxAndMin
			ChartArea ca = new ChartArea();
			ca.AxisX.Maximum = maxX;
			ca.AxisX.Minimum = minX;
			ca.AxisY.Maximum = maxY;
			ca.AxisY.Minimum = minY;
			//ca.AxisX.Title.


			//add series in Chart
			chart.Series.Clear();
			chart.Series.Add(series1);

			//add chartArea in Chart
			chart.ChartAreas.Clear();
			chart.ChartAreas.Add(ca);

		}
		/// <summary>
		/// 散布図表示
		/// </summary>
		/// <param name="chart"></param>
		/// <param name="plot1"></param>
		/// <param name="plot2"></param>
		/// <param name="minX"></param>
		/// <param name="maxX"></param>
		/// <param name="minY"></param>
		/// <param name="maxY"></param>
		public static void PlotChartXY(Chart chart, FloatArray plot1, FloatArray plot2, float minX, float maxX, float minY, float maxY)
		{
			//init series
			Series series1 = new Series("plot1");

			//setType
			series1.ChartType = SeriesChartType.Point;

			for (int i = 0; i < plot2.Length; i++)
				series1.Points.AddXY(plot1[i], plot2[i]);

			//setValueLabel
			series1.IsValueShownAsLabel = false;

			//setPointsize
			series1.MarkerSize = 5;

			//setMarkerType
			series1.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;

			//setMaxAndMin
			ChartArea ca = new ChartArea();
			ca.AxisX.Maximum = maxX;
			ca.AxisX.Minimum = minX;
			ca.AxisY.Maximum = maxY;
			ca.AxisY.Minimum = minY;
			//ca.AxisX.Title.


			//add series in Chart
			chart.Series.Clear();
			chart.Series.Add(series1);

			//add chartArea in Chart
			chart.ChartAreas.Clear();
			chart.ChartAreas.Add(ca);

		}
		/// <summary>
		/// 散布図表示
		/// </summary>
		/// <param name="chart"></param>
		/// <param name="plot1"></param>
		/// <param name="plot2"></param>
		/// <param name="minX"></param>
		/// <param name="maxX"></param>
		/// <param name="minY"></param>
		/// <param name="maxY"></param>
		public static void PlotChartXY(Chart chart, IplImage plot1, IplImage plot2, float minX, float maxX, float minY, float maxY)
		{
			//init series
			Series series1 = new Series("plot1");

			//setType
			series1.ChartType = SeriesChartType.Point;

			//int L = plot1.Width * plot1.Height;

			for (int i = 0; i < plot1.Height; i++)
				for (int j = 0; j < plot1.Width;j++ )
					series1.Points.AddXY(plot1[i,j].Val0, plot2[i,j].Val0);

			//setValueLabel
			series1.IsValueShownAsLabel = false;

			//setPointsize
			series1.MarkerSize = 5;

			//setMarkerType
			series1.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;

			//setMaxAndMin
			ChartArea ca = new ChartArea();
			ca.AxisX.Maximum = maxX;
			ca.AxisX.Minimum = minX;
			ca.AxisY.Maximum = maxY;
			ca.AxisY.Minimum = minY;
			//ca.AxisX.Title.


			//add series in Chart
			chart.Series.Clear();
			chart.Series.Add(series1);

			//add chartArea in Chart
			chart.ChartAreas.Clear();
			chart.ChartAreas.Add(ca);

		}
		/// <summary>
		/// 二本の折れ線をプロット
		/// </summary>
		/// <param name="chart"></param>
		/// <param name="plot1"></param>
		/// <param name="minX"></param>
		/// <param name="minY"></param>
		/// <param name="maxX"></param>
		/// <param name="maxY"></param>
		public static void PlotChart(Chart chart, float[] plot1,  float minX, float maxX, float minY, float maxY)
		{
			//init series
			Series series1 = new Series("plot1");
//			Series series2 = new Series("plot2");

			//setType
			series1.ChartType = SeriesChartType.Point;
//			series2.ChartType = SeriesChartType.Point;

			//setValue
			for (int i = 0; i < plot1.Length; i++)
				series1.Points.AddXY(i, plot1[i]);

//			for (int i = 0; i < plot2.Length; i++)
//				series2.Points.AddXY(i, plot2[i]);

			//setValueLabel
			series1.IsValueShownAsLabel = true;
//			series2.IsValueShownAsLabel = true;

			//setPointsize
			series1.MarkerSize = 10;
//			series2.MarkerSize = 10;

			//setMarkerType
			series1.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;
//			series2.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;//

			//setMaxAndMin
			ChartArea ca = new ChartArea();
			ca.AxisX.Maximum = maxX;
			ca.AxisX.Minimum = minX;
			ca.AxisY.Maximum = maxY;
			ca.AxisY.Minimum = minY;


			//add series in Chart
			chart.Series.Clear();
			chart.Series.Add(series1);
//			chart.Series.Add(series2);

			//add chartArea in Chart
			chart.ChartAreas.Clear();
			chart.ChartAreas.Add(ca);
		}
	}
}
