using System.Windows;
using System.Windows.Media.Animation;

namespace WotModpackLoader
{
    public partial class ModInstalledWindow : Window
    {
        public ModInstalledWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                this.Focus();
                // Animacja pojawiania się okna
                if (MainBorder != null)
                {
                    var sb = new Storyboard();

                    // Animacja przezroczystości
                    var opacityAnim = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(340)))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(opacityAnim, MainBorder);
                    Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));

                    // Animacja skali X
                    var scaleAnimX = new DoubleAnimation(0.93, 1, new Duration(TimeSpan.FromMilliseconds(340)))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(scaleAnimX, MainBorder);
                    Storyboard.SetTargetProperty(scaleAnimX, new PropertyPath("RenderTransform.ScaleX"));

                    // Animacja skali Y
                    var scaleAnimY = new DoubleAnimation(0.93, 1, new Duration(TimeSpan.FromMilliseconds(340)))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(scaleAnimY, MainBorder);
                    Storyboard.SetTargetProperty(scaleAnimY, new PropertyPath("RenderTransform.ScaleY"));

                    sb.Children.Add(opacityAnim);
                    sb.Children.Add(scaleAnimX);
                    sb.Children.Add(scaleAnimY);

                    sb.Begin();
                }
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}