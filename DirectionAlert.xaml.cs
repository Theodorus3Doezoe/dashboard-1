namespace dashboard.Component;

public partial class DirectionAlert : ContentView
{
    public DirectionAlert()
    {
        InitializeComponent();
    }

    public void SetDirection(string value)
    {
        DirectionLabel.Text = $"Schotdetectie: {value}";
    }
}
