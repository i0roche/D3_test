using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Svg.Skia;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private SKControl skControl;
        private SKSvg svgData;

        // 地図の表示位置と倍率を管理する行列 
        private SKMatrix currentMatrix = SKMatrix.CreateIdentity();

        // マウス操作用の変数
        private bool isDragging = false;
        private Point lastMousePos;

        // 地図を画面に合わせてズーム・移動させるメソッド
        private void FitToScreen()
        {
            if (svgData?.Picture == null) return;

            // 地図（SVG）の実際の描画範囲を取得
            var svgRect = svgData.Picture.CullRect;

            // 画面（キャンバス）のサイズを取得
            var viewWidth = skControl.Width;
            var viewHeight = skControl.Height;

            // 縦横どちらに合わせて縮小するか
            float scaleX = viewWidth / svgRect.Width;
            float scaleY = viewHeight / svgRect.Height;
            float scale = Math.Min(scaleX, scaleY) * 0.9f; // 0.9を掛けて少し余白をもたせる

            // 真ん中に持ってくるための移動量
            float translateX = (viewWidth - svgRect.Width * scale) / 2f - svgRect.Left * scale;
            float translateY = (viewHeight - svgRect.Height * scale) / 2f - svgRect.Top * scale;

            // 行列（Matrix）を作成して適用
            currentMatrix = SKMatrix.CreateIdentity();
            currentMatrix = currentMatrix.PostConcat(SKMatrix.CreateScale(scale, scale));
            currentMatrix = currentMatrix.PostConcat(SKMatrix.CreateTranslation(translateX, translateY));

            skControl.Invalidate();
        }

        public Form1()
        {
            InitializeComponent();
            this.Text = "Map App (Google Maps Style)";
            this.Size = new Size(1000, 700);

            skControl = new SKControl();
            skControl.Dock = DockStyle.Fill;
            skControl.PaintSurface += OnPaintSurface;

            // マウスイベントを登録
            skControl.MouseDown += OnMouseDown;
            skControl.MouseMove += OnMouseMove;
            skControl.MouseUp += OnMouseUp;
            skControl.MouseWheel += OnMouseWheel; // ホイールでズーム

            this.Controls.Add(skControl);

            LoadSvg();
        }

        private void LoadSvg()
        {
            try
            {
                svgData = new SKSvg();
                svgData.Load("map.svg");

                FitToScreen(); // 画面に合わせて最初の表示を調整
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(new SKColor(0xF0, 0xF2, 0xF5));

            if (svgData != null && svgData.Picture != null)
            {
                // 計算した倍率と位置をセットして描画
                canvas.SetMatrix(currentMatrix);
                canvas.DrawPicture(svgData.Picture);
            }
        }


        // マウスを押した時ドラッグ開始
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastMousePos = e.Location;
            }
        }

        // マウスを動かした時地図をズラす
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                // どれだけ動いたか計算
                float dx = e.Location.X - lastMousePos.X;
                float dy = e.Location.Y - lastMousePos.Y;

                // 地図の位置を更新
                var translation = SKMatrix.CreateTranslation(dx, dy);
                currentMatrix = currentMatrix.PostConcat(translation);

                lastMousePos = e.Location;

                skControl.Invalidate();
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        // ホイールを回した時：カーソルの位置を中心にズーム
        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            // 拡大倍率（1.1倍ずつ大きく/小さくする）
            float scaleFactor = e.Delta > 0 ? 1.1f : 0.9f;

            // マウスカーソルの位置を中心に拡大縮小する
            var scale = SKMatrix.CreateScale(scaleFactor, scaleFactor, e.Location.X, e.Location.Y);
            currentMatrix = currentMatrix.PostConcat(scale);

            skControl.Invalidate(); // 再描画
        }
    }
}