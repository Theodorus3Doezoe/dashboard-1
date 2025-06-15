
namespace dashboard.Component;

public partial class Module : ContentView
{
    public Module()
    {
        InitializeComponent();
        TemperatureLabel.Text = "";
        HeartLabel.Text = "";
    }

    public void SetHeartLabel(string heart)
    {
        HeartLabel.Text = heart;
    }
    public void SetTemperature(string temp)
    {
        TemperatureLabel.Text = temp;
    }
    
}
