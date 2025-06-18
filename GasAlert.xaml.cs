namespace dashboard.Component;

public partial class GasAlert : ContentView
{
    public GasAlert()
    {
        InitializeComponent();
    }

    public void SetGas(string value)
    {
        GasLabel.Text = $"Gasdetectie: {value}";
    }
}
